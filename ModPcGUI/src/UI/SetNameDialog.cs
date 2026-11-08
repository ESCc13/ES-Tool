using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace ModPcGUI
{
    /// <summary>
    /// 新账号首次登录、服务端返回「还没有昵称」时弹的设置昵称窗口。
    /// 原程序是在控制台里 Term.ReadLine 让你敲昵称，GUI 没有控制台可敲，
    /// 所以用这个弹窗接管（text.app.NamePrompt 钩子）。
    ///
    /// 三个坑记一下，别再踩：
    /// 1. 必须是 WPF 窗口 + 从后台线程 Dispatcher.Invoke 弹出。WinForms Form.ShowDialog
    ///    会卡住 Dispatcher，PCL 的 ModAnimation 线程会刷几万条「调用线程无法访问此对象」。
    /// 2. 这里的按钮 / 输入框一律用原生 WPF 控件，不用 PCL.MyButton / PCL.MyTextBox。
    ///    PCL 的 MyTextBox 把 Text 做成了自定义依赖属性，赋值时不触发 WPF 的 TextChanged，
    ///    于是「确定」按钮的 IsEnabled 永远停在 false —— 点它一点反应都没有（用户报的正是这个）。
    ///    原生 TextBox 的 TextChanged 一定触发，逻辑才靠得住。
    /// 3. 拖动窗口只能挂在标题条上。挂在外层 Border 上时，鼠标事件从按钮冒泡上来会触发
    ///    DragMove 的模态拖拽循环，按钮就永远点不动。
    /// </summary>
    internal sealed class SetNameDialog : Window
    {
        private static readonly FontFamily UiFont =
            new FontFamily("Microsoft YaHei UI, Microsoft YaHei, 微软雅黑");

        private static ControlTemplate _tplPrimary;
        private static ControlTemplate _tplSecondary;
        private static ControlTemplate _tplClose;
        private static ControlTemplate _tplInput;

        private readonly TextBox _box;
        private readonly TextBlock _holder;
        private readonly Button _ok;
        private bool _done;

        internal string NickName
        {
            get { return (_box.Text ?? "").Trim(); }
        }

        private SetNameDialog(string suggest)
        {
            EnsureTemplates();

            Title = "设置昵称";
            Width = 440;
            Height = 274;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FontFamily = UiFont;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;

            // ── 外框：跟主界面同一套（白底 / #8899AA 描边 / 8px 圆角 / 柔和投影）──
            var frame = new Border
            {
                Background = Brushes.White,
                BorderBrush = B("#8899AA"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Effect = new DropShadowEffect
                {
                    BlurRadius = 16,
                    ShadowDepth = 0,
                    Opacity = 0.26,
                    Color = Colors.Black
                }
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // ── 标题条：PCL 主界面同款蓝 #1370F3（只有这里能拖窗口）──
            var header = new Border
            {
                Background = B("#1370F3"),
                CornerRadius = new CornerRadius(7, 7, 0, 0)
            };
            var headGrid = new Grid();
            headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            headGrid.Children.Add(new TextBlock
            {
                Text = "设置昵称",
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(14, 0, 0, 0)
            });

            var btnClose = new Button
            {
                Content = "✕",
                Width = 40,
                Height = 40,
                FontSize = 11,
                Foreground = Brushes.White,
                Cursor = Cursors.Hand,
                Focusable = false,
                Template = _tplClose
            };
            HookClick(btnClose, () => Finish(false));
            Grid.SetColumn(btnClose, 1);
            headGrid.Children.Add(btnClose);

            header.Child = headGrid;
            header.MouseLeftButtonDown += (s, e) =>
            {
                try
                {
                    if (e.ButtonState == MouseButtonState.Pressed && e.OriginalSource is TextBlock)
                        DragMove();
                }
                catch { }
            };
            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            // ── 正文：文字用主界面的 #343D4A / #8C8C8C ──
            var body = new Grid { Margin = new Thickness(22, 16, 22, 18) };
            body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // 标题
            body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // 说明
            body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // 输入框
            body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // 备注
            body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });// 按钮

            body.Children.Add(new TextBlock
            {
                Text = "这个账号还没有昵称",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = B("#343D4A"),
                Margin = new Thickness(0, 0, 0, 4)
            });

            var hint = new TextBlock
            {
                Text = "取个名字吧 —— 游戏内和联机大厅显示的就是它（1~16 字）。",
                FontSize = 11.5,
                Foreground = B("#8C8C8C"),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 17,
                Margin = new Thickness(0, 0, 0, 12)
            };
            Grid.SetRow(hint, 1);
            body.Children.Add(hint);

            // 输入框 + 占位提示
            var boxHost = new Grid { Height = 36 };
            _box = new TextBox
            {
                Height = 36,
                FontSize = 13,
                Padding = new Thickness(10, 0, 10, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                Foreground = B("#343D4A"),
                CaretBrush = B("#1370F3"),
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                Template = _tplInput,
                MaxLength = 16,
                Text = suggest ?? ""
            };
            boxHost.Children.Add(_box);

            _holder = new TextBlock
            {
                Text = "输入昵称，或者点左下角「随机昵称」",
                FontSize = 12.5,
                Foreground = B("#B4BFCC"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(11, 0, 0, 0),
                IsHitTestVisible = false,
                Visibility = string.IsNullOrEmpty(_box.Text) ? Visibility.Visible : Visibility.Collapsed
            };
            boxHost.Children.Add(_holder);
            Grid.SetRow(boxHost, 2);
            body.Children.Add(boxHost);

            var note = new TextBlock
            {
                Text = "被占用或不合规时会再弹一次；点「先跳过」等于暂时不设，之后可用 setname 命令补。",
                FontSize = 10.5,
                Foreground = B("#A8A8A8"),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 16,
                Margin = new Thickness(0, 8, 0, 0)
            };
            Grid.SetRow(note, 3);
            body.Children.Add(note);

            // 按钮行
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var rnd = MakeButton("随机昵称", false, 96);
            HookClick(rnd, () =>
            {
                // 原程序里输入 "rj" 会被服务端换成「NO + UID」
                _box.Text = "rj";
                _box.CaretIndex = _box.Text.Length;
                _box.Focus();
            });
            Grid.SetColumn(rnd, 0);
            row.Children.Add(rnd);

            var skip = MakeButton("先跳过", false, 88);
            skip.Margin = new Thickness(0, 0, 8, 0);
            HookClick(skip, () => Finish(false));
            Grid.SetColumn(skip, 2);
            row.Children.Add(skip);

            _ok = MakeButton("确定", true, 88);
            _ok.IsEnabled = false;
            HookClick(_ok, () =>
            {
                if (NickName.Length == 0) { _box.Focus(); return; }
                Finish(true);
            });
            Grid.SetColumn(_ok, 3);
            row.Children.Add(_ok);

            Grid.SetRow(row, 5);
            body.Children.Add(row);

            Grid.SetRow(body, 1);
            grid.Children.Add(body);

            frame.Child = grid;
            Content = frame;

            // 输入变化 → 占位提示开关 + 确定键可用性（原生 TextBox，一定会触发）
            _box.TextChanged += (s, e) =>
            {
                _holder.Visibility = string.IsNullOrEmpty(_box.Text)
                    ? Visibility.Visible : Visibility.Collapsed;
                _ok.IsEnabled = NickName.Length > 0;
            };

            Loaded += (s, e) =>
            {
                try
                {
                    _box.Focus();
                    _box.CaretIndex = _box.Text.Length;
                    Keyboard.Focus(_box);
                }
                catch { }
            };

            PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape) { Finish(false); e.Handled = true; }
                else if (e.Key == Key.Enter && e.OriginalSource is TextBox)
                {
                    // 输入法组字时不抢回车
                    if (InputMethod.GetIsInputMethodEnabled(_box) && _box.Text.Length == 0) return;
                    if (_ok.IsEnabled) { Finish(true); e.Handled = true; }
                }
            };
        }

        /// <summary>
        /// 统一的按钮点击挂载：Click 之外再加一道 PreviewMouseLeftButtonUp 兜底，
        /// Finish() 内部有 _done 幂等锁，两边都触发也只会关一次。
        /// MouseLeftButtonUp 兜底是为了防某些模板把 Click 吃掉。
        /// </summary>
        private static void HookClick(Button b, Action action)
        {
            b.Click += (s, e) => action();
            b.PreviewMouseLeftButtonUp += (s, e) =>
            {
                if (!b.IsEnabled) return;
                if (e.ChangedButton == MouseButton.Left) action();
            };
        }

        private static Button MakeButton(string text, bool primary, double width)
        {
            var b = new Button
            {
                Content = text,
                Height = 32,
                Width = width,
                FontSize = 12.5,
                FontFamily = UiFont,
                Foreground = primary ? Brushes.White : B("#343D4A"),
                Cursor = Cursors.Hand,
                Focusable = false,
                Template = primary ? _tplPrimary : _tplSecondary
            };
            return b;
        }

        private void Finish(bool ok)
        {
            if (_done) return;
            _done = true;
            try { DialogResult = ok; }
            catch { /* 非 ShowDialog 打开时赋值会抛，忽略 */ }
            try { Close(); } catch { }
        }

        /// <summary>
        /// 挂给 text.app.NamePrompt 的钩子：从登录线程切到 UI 线程弹窗。
        /// 拿到昵称返回昵称；取消/关闭返回 null。
        /// 没有 WPF 界面（--legacy）时返回 null，让 app.Name() 退回控制台 ReadLine。
        /// </summary>
        internal static string Ask()
        {
            var app = Application.Current;
            if (app == null) return null;

            try
            {
                return app.Dispatcher.Invoke(new Func<string>(() =>
                {
                    var dlg = new SetNameDialog(null);
                    var owner = app.MainWindow;
                    if (owner != null && owner.IsLoaded && !ReferenceEquals(owner, dlg))
                    {
                        dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        try { dlg.Owner = owner; } catch { }
                    }
                    return dlg.ShowDialog() == true ? dlg.NickName : null;
                }));
            }
            catch (Exception ex)
            {
                try { Term.WriteLine("✗ 设置昵称窗口异常：" + ex.Message); } catch { }
                return null;
            }
        }

        // ── 模板：一份 XAML 解析出四个 ControlTemplate，配色全部对齐主界面 ──
        private static void EnsureTemplates()
        {
            if (_tplPrimary != null) return;

            const string xaml = @"
<ResourceDictionary
    xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>

  <ControlTemplate x:Key='EsBtnPrimary' TargetType='Button'>
    <Border x:Name='bd' Background='#1370F3' BorderBrush='#1370F3'
            BorderThickness='1' CornerRadius='6' SnapsToDevicePixels='True'>
      <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'
                        Margin='{TemplateBinding Padding}'/>
    </Border>
    <ControlTemplate.Triggers>
      <Trigger Property='IsMouseOver' Value='True'>
        <Setter TargetName='bd' Property='Background' Value='#3184F6'/>
        <Setter TargetName='bd' Property='BorderBrush' Value='#3184F6'/>
      </Trigger>
      <Trigger Property='IsPressed' Value='True'>
        <Setter TargetName='bd' Property='Background' Value='#0C5FD8'/>
        <Setter TargetName='bd' Property='BorderBrush' Value='#0C5FD8'/>
      </Trigger>
      <Trigger Property='IsEnabled' Value='False'>
        <Setter TargetName='bd' Property='Background' Value='#B9CFEC'/>
        <Setter TargetName='bd' Property='BorderBrush' Value='#B9CFEC'/>
      </Trigger>
    </ControlTemplate.Triggers>
  </ControlTemplate>

  <ControlTemplate x:Key='EsBtnSecondary' TargetType='Button'>
    <Border x:Name='bd' Background='#FFFFFF' BorderBrush='#C6D2DF'
            BorderThickness='1' CornerRadius='6' SnapsToDevicePixels='True'>
      <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'
                        Margin='{TemplateBinding Padding}'/>
    </Border>
    <ControlTemplate.Triggers>
      <Trigger Property='IsMouseOver' Value='True'>
        <Setter TargetName='bd' Property='Background' Value='#EEF5FF'/>
        <Setter TargetName='bd' Property='BorderBrush' Value='#1370F3'/>
      </Trigger>
      <Trigger Property='IsPressed' Value='True'>
        <Setter TargetName='bd' Property='Background' Value='#D8E8FF'/>
      </Trigger>
      <Trigger Property='IsEnabled' Value='False'>
        <Setter TargetName='bd' Property='Opacity' Value='0.5'/>
      </Trigger>
    </ControlTemplate.Triggers>
  </ControlTemplate>

  <ControlTemplate x:Key='EsClose' TargetType='Button'>
    <Border x:Name='bd' Background='Transparent'>
      <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
    </Border>
    <ControlTemplate.Triggers>
      <Trigger Property='IsMouseOver' Value='True'>
        <Setter TargetName='bd' Property='Background' Value='#3184F6'/>
      </Trigger>
      <Trigger Property='IsPressed' Value='True'>
        <Setter TargetName='bd' Property='Background' Value='#0C5FD8'/>
      </Trigger>
    </ControlTemplate.Triggers>
  </ControlTemplate>

  <ControlTemplate x:Key='EsInput' TargetType='TextBox'>
    <Border x:Name='bd' Background='#FFFFFF' BorderBrush='#C6D2DF'
            BorderThickness='1' CornerRadius='6' SnapsToDevicePixels='True'>
      <ScrollViewer x:Name='PART_ContentHost'
                    Margin='{TemplateBinding Padding}' VerticalAlignment='Center'/>
    </Border>
    <ControlTemplate.Triggers>
      <Trigger Property='IsMouseOver' Value='True'>
        <Setter TargetName='bd' Property='BorderBrush' Value='#8FB5E4'/>
      </Trigger>
      <Trigger Property='IsKeyboardFocusWithin' Value='True'>
        <Setter TargetName='bd' Property='BorderBrush' Value='#1370F3'/>
        <Setter TargetName='bd' Property='BorderThickness' Value='1'/>
      </Trigger>
    </ControlTemplate.Triggers>
  </ControlTemplate>

</ResourceDictionary>";

            var rd = (ResourceDictionary)XamlReader.Parse(xaml);
            _tplPrimary = (ControlTemplate)rd["EsBtnPrimary"];
            _tplSecondary = (ControlTemplate)rd["EsBtnSecondary"];
            _tplClose = (ControlTemplate)rd["EsClose"];
            _tplInput = (ControlTemplate)rd["EsInput"];
        }

        private static SolidColorBrush B(string hex)
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex);
            brush.Freeze();
            return brush;
        }
    }
}
