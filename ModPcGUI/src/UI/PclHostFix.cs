using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Wpf = System.Windows;
using WpfC = System.Windows.Controls;
using WpfT = System.Windows.Threading;

namespace ModPcGUI
{
    /// <summary>
    /// 宿主与 PCL2 程序集之间的适配补丁。
    ///
    /// PCL2 是一套自带 FormMain 的完整应用，本宿主只借用它的控件与动画引擎。
    /// 有两处它默认成立的前提在我们进程里不成立，必须在这里补齐：
    ///
    /// 1) 动画帧的线程归属
    ///    PCL 的 AniStart() 会起一条独立线程，每帧通过 RunInUiWait 把 AniTimer
    ///    投递回 UI 线程。而 RunInUiWait 判断「是不是 UI 线程」用的是
    ///    ModBase.UiThreadId —— 一个在模块初始化时取当时线程 ID 的静态字段：
    ///        Private ReadOnly UiThreadId As Integer = Thread.CurrentThread.ManagedThreadId
    ///    它不是 Application.Current.Dispatcher，也不看控件属于哪个线程。
    ///    只要这个静态构造没落在主线程上，判定就会错；一旦错了，AniTimer 会在
    ///    动画线程里直接执行，AniRun 访问主线程创建的控件就抛
    ///    「调用线程无法访问此对象」（栈里是 AniRun → DependencyObject.GetValue），
    ///    所有颜色动画卡在 0% 反复重试，日志刷屏。
    ///    这里干脆不走那条线程：用 UI 线程上的 DispatcherTimer 直接驱动
    ///    ModAnimation.AniTimer，帧执行永远在 UI 线程，RunInUi 那套判定完全不参与。
    ///
    /// 2) 下拉框展开弹错（MyComboBox_DropDownOpened）
    ///        CType(Template.FindName("PanPopup", Me), Grid).Opacity = FrmMain.Opacity
    ///    FrmMain（ModMain.FrmMain As FormMain）在宿主里始终是 Nothing，
    ///    于是每次展开下拉框都抛 NullReferenceException 并弹「是否反馈此问题」。
    ///    这里摘掉 PCL 自己的开 / 关处理，换成不依赖 FrmMain 的等价实现：
    ///    展开时保存宽度、对齐到实际宽度、弹层透明度跟随宿主窗口；收起时还原宽度。
    ///    （两个必须一起摘：原实现靠 opened 记 RealWidth、closed 还原，
    ///      只摘一个会把宽度还原成 0。）
    /// </summary>
    internal static class PclHostFix
    {
        private static Assembly PclAssembly => typeof(PCL.MyButton).Assembly;

        private static bool _patchedLogged;
        private static WpfT.DispatcherTimer _aniTimer;

        // ---------------- 1) 动画帧：UI 线程驱动 ----------------

        /// <summary>
        /// 取代 <c>PCL.ModAnimation.AniStart()</c>：不起动画线程，
        /// 改用 UI 线程上的 DispatcherTimer 按 ~60fps 调 AniTimer。
        /// 返回 false 表示拿不到必要成员，调用方应回退到 AniStart()。
        /// </summary>
        internal static bool StartAnimationLoop()
        {
            try
            {
                var mod = PclAssembly.GetType("PCL.ModAnimation", false);
                if (mod == null) return false;

                var aniTimer = mod.GetMethod("AniTimer",
                    BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(double) }, null);
                var lastTick = mod.GetField("AniLastTick", BindingFlags.NonPublic | BindingFlags.Static);
                if (aniTimer == null || lastTick == null)
                {
                    Note($"StartAnimationLoop: 缺少 {(aniTimer == null ? "AniTimer" : "AniLastTick")}，回退 AniStart");
                    return false;
                }
                var running = mod.GetField("AniRunning", BindingFlags.Public | BindingFlags.Static);
                var speed = mod.GetProperty("AniSpeed", BindingFlags.Public | BindingFlags.Static);
                int frames = 0;

                running?.SetValue(null, true);            // PCL 靠它判断动画是否在跑
                long last = NowMs();
                lastTick.SetValue(null, last);            // 与 GetTimeMs 同一时钟（Stopwatch 单调毫秒）

                _aniTimer = new WpfT.DispatcherTimer(WpfT.DispatcherPriority.Render)
                {
                    Interval = TimeSpan.FromMilliseconds(16)
                };
                _aniTimer.Tick += (s, e) =>
                {
                    try
                    {
                        long now = NowMs();
                        long delta = now - last;
                        if (delta < 3) return;            // 与 PCL 原循环一致：太短的间隔跳过
                        last = now;
                        lastTick.SetValue(null, now);     // AniTimer 用它过滤「还没到开始时间」的组
                        double spd = 1.0;
                        try { if (speed?.GetValue(null, null) is double v) spd = v; } catch { }
                        aniTimer.Invoke(null, new object[] { (double)delta * spd });
                        if (++frames == 60) Note("动画帧循环心跳：已连续跑 60 帧");   // 自检用，只记一次
                    }
                    catch { }
                };
                _aniTimer.Start();
                Note("动画帧循环：改由 UI 线程 DispatcherTimer 驱动（不再起 PCL 动画线程）");
                return true;
            }
            catch (Exception ex)
            {
                Note("StartAnimationLoop 失败：" + ex.GetType().Name + " " + ex.Message + "，回退 AniStart");
                return false;
            }
        }

        /// <summary>与 PCL.ModBase.GetTimeMs 等价的单调毫秒。</summary>
        private static long NowMs() => Stopwatch.GetTimestamp() / (Stopwatch.Frequency / 1000L);

        // ---------------- 2) UI 线程判定（兜底修正） ----------------

        /// <summary>
        /// 把 PCL 的 UiThreadId 改成当前线程。动画帧已不依赖它，但 PCL 其他
        /// RunInUi 调用点（弹层、设置刷新等）仍会用它，值错了同样会跨线程。
        /// 字段是 private static readonly —— .NET Framework 允许反射写入。
        /// </summary>
        internal static bool FixUiThreadId()
        {
            try
            {
                var t = PclAssembly.GetType("PCL.ModBase", false);
                var f = t?.GetField("UiThreadId", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (f == null)
                {
                    Note("FixUiThreadId: 找不到 PCL.ModBase.UiThreadId，跳过");
                    return false;
                }
                int me = Thread.CurrentThread.ManagedThreadId;
                int cur = f.GetValue(null) is int c ? c : -1;
                if (cur == me)
                {
                    Note($"FixUiThreadId: 已是当前线程 {me}，无需修改");
                    return true;
                }
                f.SetValue(null, me);
                bool ok = f.GetValue(null) is int now && now == me;
                Note($"FixUiThreadId: {cur} -> {me}，{(ok ? "成功" : "写入未生效")}");
                return ok;
            }
            catch (Exception ex)
            {
                Note("FixUiThreadId 失败：" + ex.GetType().Name + " " + ex.Message);
                return false;
            }
        }

        // ---------------- 3) 下拉框展开 ----------------

        /// <summary>建一个已打过宿主补丁的 PCL 下拉框。</summary>
        internal static WpfC.ComboBox NewCombo()
        {
            var box = new PCL.MyComboBox();
            FixComboBox(box);
            return box;
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<object, object> _patchedBoxes =
            new System.Runtime.CompilerServices.ConditionalWeakTable<object, object>();

        /// <summary>
        /// 兜底：把一棵界面树里所有 PCL.MyComboBox 都打上补丁。
        ///
        /// 光靠「创建时走 NewCombo()」不稳 —— 漏一处就崩一处（联机大厅那两个下拉框
        /// 就是这么漏的）。这里在窗口 Loaded / 页面切换后统一扫一遍，
        /// 用弱表记住已处理的实例，重复扫描不会重复挂事件。
        /// </summary>
        internal static void PatchComboBoxes(Wpf.DependencyObject root)
        {
            if (root == null) return;
            int added = 0;
            try
            {
                var stack = new System.Collections.Generic.Stack<Wpf.DependencyObject>();
                stack.Push(root);
                int guard = 0;
                while (stack.Count > 0 && guard++ < 20000)
                {
                    var cur = stack.Pop();
                    if (cur == null) continue;

                    if (cur is PCL.MyComboBox box && !_patchedBoxes.TryGetValue(box, out _))
                    {
                        _patchedBoxes.Add(box, box);
                        FixComboBox(box);
                        added++;
                    }

                    // 逻辑树（页面内容是手搭的，逻辑树最全）
                    try
                    {
                        foreach (var ch in Wpf.LogicalTreeHelper.GetChildren(cur))
                            if (ch is Wpf.DependencyObject d) stack.Push(d);
                    }
                    catch { }

                    // 可视树（覆盖模板 / 数据模板里生成的实例）
                    try
                    {
                        int c = Wpf.Media.VisualTreeHelper.GetChildrenCount(cur);
                        for (int i = 0; i < c; i++) stack.Push(Wpf.Media.VisualTreeHelper.GetChild(cur, i));
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Note("PatchComboBoxes 失败：" + ex.GetType().Name + " " + ex.Message);
            }
            if (added > 0) Note($"PatchComboBoxes: 新接管 {added} 个下拉框");
        }

        /// <summary>
        /// 卸掉 PCL 的 DropDownOpened / DropDownClosed，再挂上等价但安全的实现。
        /// </summary>
        internal static void FixComboBox(WpfC.ComboBox box)
        {
            if (box == null) return;

            try
            {
                var t = typeof(PCL.MyComboBox);
                string a = Detach(box, "DropDownOpened",
                    t.GetMethod("MyComboBox_DropDownOpened", BindingFlags.Instance | BindingFlags.NonPublic));
                string b = Detach(box, "DropDownClosed",
                    t.GetMethod("MyComboBox_DropDownClosed", BindingFlags.Instance | BindingFlags.NonPublic));

                if (a == "method-missing" || a == "bind-failed" || a == "no-event")
                    Note($"FixComboBox: 摘除失败（opened={a}, closed={b}）");
                else if (!_patchedLogged)
                {
                    _patchedLogged = true;
                    Note($"FixComboBox: 已接管下拉框展开逻辑（{a}）");
                }
            }
            catch (Exception ex)
            {
                Note("FixComboBox 失败：" + ex.GetType().Name + " " + ex.Message);
            }

            double realWidth = 0;
            bool saved = false;

            box.DropDownOpened += (s, e) =>
            {
                try
                {
                    // 与 PCL 原逻辑一致：下拉层宽度绑 Width，Width 为 Auto(NaN) 时先落成实际宽度
                    realWidth = box.Width;
                    saved = true;
                    box.Width = box.ActualWidth;
                    // 原逻辑是 PanPopup.Opacity = FrmMain.Opacity，这里换成宿主窗口
                    if (box.Template?.FindName("PanPopup", box) is WpfC.Grid pan)
                    {
                        var win = Wpf.Window.GetWindow(box);
                        pan.Opacity = win?.Opacity ?? 1.0;
                    }
                }
                catch { }
            };

            box.DropDownClosed += (s, e) =>
            {
                try { if (saved) box.Width = realWidth; } catch { }
            };
        }

        /// <summary>把 PCL 挂在控件上的某个事件处理摘掉。</summary>
        private static string Detach(WpfC.ComboBox box, string eventName, MethodInfo handler)
        {
            if (handler == null) return "method-missing";
            var del = Delegate.CreateDelegate(typeof(EventHandler), box, handler, false);
            if (del == null)
                return "bind-failed(" + string.Join(",", handler.GetParameters().Select(p => p.ParameterType.Name)) + ")";

            // 路由事件：DropDownOpened / DropDownClosed 在 ComboBox 上是路由事件，
            // 但没有公开的 XxxEvent 字段可拿，从 EventManager 的注册表里按名字找。
            var re = FindRoutedEvent(eventName, typeof(WpfC.ComboBox));
            if (re != null)
            {
                box.RemoveHandler(re, del);
                return "routed:" + re.OwnerType.Name;
            }

            // WPF ComboBox 的 DropDownOpened / DropDownClosed 是按 EventPrivateKey
            // 存进 EventHandlersStore 的普通事件（没有委托字段、也不是路由事件）：
            //     Private Shared ReadOnly DropDownOpenedKey As New EventPrivateKey
            // 所以要从 UIElement 的 EventHandlersStore 里按 key 移除。
            var keyField = typeof(WpfC.ComboBox).GetField(eventName + "Key",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (keyField?.GetValue(null) is object key)
            {
                var store = typeof(Wpf.UIElement)
                    .GetProperty("EventHandlersStore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(box, null);
                if (store == null) return "no-store";

                var keyType = key.GetType();
                var remove = store.GetType()
                    .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .FirstOrDefault(m => m.Name == "Remove"
                                         && m.GetParameters().Length == 2
                                         && m.GetParameters()[0].ParameterType == keyType
                                         && typeof(Delegate).IsAssignableFrom(m.GetParameters()[1].ParameterType));
                if (remove == null) return "no-store-remove";

                int before = CountStore(store, key);
                remove.Invoke(store, new[] { key, (object)del });
                int after = CountStore(store, key);
                return $"key:{before}->{after}";
            }

            // 兜底：CLR 事件的委托字段
            for (var ty = box.GetType(); ty != null && ty != typeof(object); ty = ty.BaseType)
            {
                var evField = ty.GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (evField != null && evField.GetValue(box) is Delegate cur)
                {
                    evField.SetValue(box, Delegate.Remove(cur, del));
                    return "field:" + ty.Name;
                }
            }
            return "no-event";
        }

        /// <summary>数一下这个 key 上还挂着手多少个处理，用于校验是否真的摘掉。</summary>
        private static int CountStore(object store, object key)
        {
            try
            {
                var get = store.GetType()
                    .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .FirstOrDefault(m => m.Name == "Get"
                                         && m.GetParameters().Length == 1
                                         && m.GetParameters()[0].ParameterType == key.GetType());
                if (get == null) return -1;
                var v = get.Invoke(store, new[] { key });
                return v is Array arr ? arr.Length : (v == null ? 0 : 1);
            }
            catch { return -1; }
        }

        /// <summary>从 EventManager 已注册的路由事件里按名字找。</summary>
        private static Wpf.RoutedEvent FindRoutedEvent(string name, Type owner)
        {
            try
            {
                foreach (var re in Wpf.EventManager.GetRoutedEvents())
                {
                    if (re == null || re.Name != name) continue;
                    if (re.OwnerType != null && re.OwnerType.IsAssignableFrom(owner) == false
                        && owner.IsAssignableFrom(re.OwnerType) == false) continue;
                    return re;
                }
            }
            catch { }
            return null;
        }

        // ---------------- 日志 ----------------

        private static void Note(string msg)
        {
            try
            {
                File.AppendAllText(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "run.log"),
                    "[" + DateTime.Now.ToString("HH:mm:ss") + "] [host] " + msg + "\r\n",
                    System.Text.Encoding.UTF8);
            }
            catch { }
        }
    }
}
