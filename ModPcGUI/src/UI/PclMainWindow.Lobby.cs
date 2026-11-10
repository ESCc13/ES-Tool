using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows;
using Wpf = System.Windows;
using WpfC = System.Windows.Controls;

namespace ModPcGUI
{
    /// <summary>
    /// 「联机大厅」页：按地图（联机专区）浏览在线房间 → 看详情 / 玩家列表 → 一键查 IP 进服。
    /// 全部走自己的登录会话（LobbyHall 数据层），不依赖第三方中转站。
    /// </summary>
    public sealed partial class PclMainWindow
    {
        // 走 NewCombo() 工厂：PCL 自己的 DropDownOpened 会读 FrmMain.Opacity，
        // 宿主里 FrmMain 恒为 Nothing，展开必抛 NRE。直接用 new 就会漏补丁。
        private readonly PCL.MyComboBox _lbEdition = NewCombo();
        private readonly PCL.MyComboBox _lbSort = NewCombo();
        private readonly PCL.MyTextBox _lbKw = new PCL.MyTextBox();
        private PCL.MyButton _lbSearchBtn;
        private readonly WpfC.StackPanel _lbMapList = new WpfC.StackPanel();
        private readonly WpfC.StackPanel _lbRoomList = new WpfC.StackPanel();
        private readonly WpfC.TextBlock _lbStatus = new WpfC.TextBlock();
        private readonly WpfC.TextBlock _lbRoomTitle = new WpfC.TextBlock();

        private readonly List<LobbyMap> _lbMaps = new List<LobbyMap>();
        private readonly List<LobbyRoom> _lbRooms = new List<LobbyRoom>();
        private LobbyMap _lbSel;
        private bool _lbBusy;
        private bool _lbLoaded;   // 首次进页面自动跑一次全服池

        private bool LbPe { get { return _lbEdition.SelectedIndex == 1; } }

        // ───────────── 页面 ─────────────

        private Wpf.FrameworkElement BuildLobbyPage(SolidColorBrush blue, SolidColorBrush text, SolidColorBrush sub)
        {
            var sv = new WpfC.ScrollViewer { VerticalScrollBarVisibility = WpfC.ScrollBarVisibility.Auto };
            var root = new WpfC.StackPanel { Margin = new Wpf.Thickness(18, 14, 18, 14) };
            sv.Content = root;

            root.Children.Add(new WpfC.TextBlock
            {
                Text = "留空搜「全服在线地图池」= 按真实在服人数排；填关键词则按地图名搜。点地图看房间，进房自动查 IP 并启动",
                FontSize = 11,
                Foreground = sub,
                Margin = new Wpf.Thickness(2, 0, 0, 10)
            });

            // ── 顶部：平台 / 排序 / 搜索 ──
            var card = new PCL.MyCard { Title = "找地图", Margin = new Wpf.Thickness(0, 0, 0, 12) };
            var sp = new WpfC.StackPanel { Margin = new Wpf.Thickness(15, 33, 15, 12) };

            var row = new WpfC.Grid();
            row.ColumnDefinitions.Add(new WpfC.ColumnDefinition { Width = new Wpf.GridLength(96) });
            row.ColumnDefinitions.Add(new WpfC.ColumnDefinition { Width = new Wpf.GridLength(120) });
            row.ColumnDefinitions.Add(new WpfC.ColumnDefinition { Width = new Wpf.GridLength(1, Wpf.GridUnitType.Star) });
            row.ColumnDefinitions.Add(new WpfC.ColumnDefinition { Width = Wpf.GridLength.Auto });

            _lbEdition.Height = 30;
            _lbEdition.FontSize = 12;
            _lbEdition.Margin = new Wpf.Thickness(0, 0, 8, 0);
            _lbEdition.Items.Add(new PCL.MyComboBoxItem { Content = "PC 端" });
            _lbEdition.Items.Add(new PCL.MyComboBoxItem { Content = "PE 端" });
            _lbEdition.SelectedIndex = 0;
            _lbEdition.SelectionChanged += (s, e) => { if (!_lbBusy) LobbySearch(); };
            WpfC.Grid.SetColumn(_lbEdition, 0);
            row.Children.Add(_lbEdition);

            _lbSort.Height = 30;
            _lbSort.FontSize = 12;
            _lbSort.Margin = new Wpf.Thickness(0, 0, 8, 0);
            _lbSort.Items.Add(new PCL.MyComboBoxItem { Content = "热门推荐" });
            _lbSort.Items.Add(new PCL.MyComboBoxItem { Content = "在线人数" });
            _lbSort.SelectedIndex = 0;
            _lbSort.SelectionChanged += (s, e) => { if (!_lbBusy) LobbySearch(); };
            WpfC.Grid.SetColumn(_lbSort, 1);
            row.Children.Add(_lbSort);

            _lbKw.Height = 30;
            _lbKw.FontSize = 12;
            _lbKw.HintText = "留空 = 全服在线地图池；填关键词 = 按地图名搜";
            _lbKw.Margin = new Wpf.Thickness(0, 0, 8, 0);
            _lbKw.KeyDown += (s, e) =>
            {
                if (e.Key == Wpf.Input.Key.Enter) { LobbySearch(); e.Handled = true; }
            };
            WpfC.Grid.SetColumn(_lbKw, 2);
            row.Children.Add(_lbKw);

            _lbSearchBtn = new PCL.MyButton
            {
                Text = "搜索",
                Width = 72,
                Height = 30,
                ColorType = PCL.MyButton.ColorState.Highlight
            };
            WpfC.TextBlock.SetFontSize(_lbSearchBtn, 12);
            _lbSearchBtn.Click += (s, e) => LobbySearch();
            WpfC.Grid.SetColumn(_lbSearchBtn, 3);
            row.Children.Add(_lbSearchBtn);

            sp.Children.Add(row);

            _lbStatus.FontSize = 11.5;
            _lbStatus.Margin = new Wpf.Thickness(2, 8, 0, 0);
            _lbStatus.TextWrapping = Wpf.TextWrapping.Wrap;
            _lbStatus.Text = "输入地图名后点「搜索」";
            _lbStatus.Foreground = sub;
            sp.Children.Add(_lbStatus);

            card.Children.Add(sp);
            root.Children.Add(card);

            // ── 主体：左地图 / 右房间 ──
            var card2 = new PCL.MyCard { Title = "地图与房间", Margin = new Wpf.Thickness(0, 0, 0, 12) };
            var grid = new WpfC.Grid { Margin = new Wpf.Thickness(15, 33, 15, 12) };
            grid.ColumnDefinitions.Add(new WpfC.ColumnDefinition { Width = new Wpf.GridLength(210) });
            grid.ColumnDefinitions.Add(new WpfC.ColumnDefinition { Width = new Wpf.GridLength(1, Wpf.GridUnitType.Star) });

            var left = new WpfC.StackPanel();
            left.Children.Add(new WpfC.TextBlock
            {
                Text = "地图",
                FontSize = 11,
                Foreground = sub,
                Margin = new Wpf.Thickness(2, 0, 0, 6)
            });
            left.Children.Add(new WpfC.ScrollViewer
            {
                Height = 290,
                VerticalScrollBarVisibility = WpfC.ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = WpfC.ScrollBarVisibility.Disabled,
                Content = _lbMapList
            });
            WpfC.Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            var right = new WpfC.StackPanel { Margin = new Wpf.Thickness(12, 0, 0, 0) };
            _lbRoomTitle.Text = "房间";
            _lbRoomTitle.FontSize = 11;
            _lbRoomTitle.Foreground = sub;
            _lbRoomTitle.Margin = new Wpf.Thickness(2, 0, 0, 6);
            right.Children.Add(_lbRoomTitle);
            right.Children.Add(new WpfC.ScrollViewer
            {
                Height = 290,
                VerticalScrollBarVisibility = WpfC.ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = WpfC.ScrollBarVisibility.Disabled,
                Content = _lbRoomList
            });
            WpfC.Grid.SetColumn(right, 1);
            grid.Children.Add(right);

            card2.Children.Add(grid);
            root.Children.Add(card2);

            root.Children.Add(new PCL.MyHint
            {
                Text = "「热门推荐」按房间数排（同一张图开房越多越靠前），「在线人数」按当前在服总人数排；两个都基于全服实时房间统计。",
                Theme = PCL.MyHint.Themes.Blue,
                Margin = new Wpf.Thickness(0, 2, 0, 0)
            });

            return sv;
        }

        // ───────────── 搜索地图 ─────────────

        private void LobbySearch()
        {
            if (_lbBusy) return;
            if (!AppHost.Started)
            {
                LbStatus("请先在左边登录（大厅数据要用你的登录会话）", "#E0A23C");
                return;
            }

            string kw = (_lbKw.Text ?? "").Trim();
            bool pe = LbPe;
            int sort = _lbSort.SelectedIndex == 1 ? LobbyHall.SortOnline : LobbyHall.SortHot;

            _lbBusy = true;
            if (_lbSearchBtn != null) _lbSearchBtn.IsEnabled = false;
            LbStatus(kw.Length == 0
                ? "获取中…正在统计 " + (pe ? "PE" : "PC") + " 端全服在线房间（最多 150 间）"
                : "获取中…正在搜索 " + (pe ? "PE" : "PC") + " 端「" + kw + "」", "#4C8DFF");

            AppHost.KickOff(() =>
            {
                try
                {
                    List<LobbyMap> maps;
                    string head;
                    if (kw.Length == 0)
                    {
                        maps = LobbyHall.HotMaps(pe, sort, 3, s => LogUi("  [大厅] " + s + "\r\n"));
                        head = "全服在线地图池";
                    }
                    else
                    {
                        maps = LobbyHall.SearchMaps(kw, sort, pe, 0, 24, s => LogUi("  [大厅] " + s + "\r\n"));
                        head = "搜索「" + kw + "」";
                    }

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _lbBusy = false;
                        if (_lbSearchBtn != null) _lbSearchBtn.IsEnabled = true;

                        _lbMaps.Clear();
                        _lbMaps.AddRange(maps);
                        _lbSel = null;
                        RenderMaps();

                        if (maps.Count == 0)
                        {
                            LbStatus(kw.Length == 0
                                ? "现在这个端没有在线房间（或接口限速），过几秒再点一次"
                                : "没有搜到地图，换个关键词试试", "#E0A23C");
                            return;
                        }
                        LbStatus(head + "：共 " + maps.Count + " 张地图（" + (pe ? "PE" : "PC") + " 端），点左边任意一张看房间",
                            "#2E9E63");

                        SelectMap(_lbMaps[0]);
                    }));
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _lbBusy = false;
                        if (_lbSearchBtn != null) _lbSearchBtn.IsEnabled = true;
                        LbStatus("获取失败：" + ex.Message, "#E05A5A");
                    }));
                }
            }, "lobby-maps");
        }

        private void RenderMaps()
        {
            _lbMapList.Children.Clear();
            foreach (var m in _lbMaps)
            {
                var captured = m;
                bool sel = _lbSel != null && _lbSel.Id == m.Id;

                var sp = new WpfC.StackPanel();
                sp.Children.Add(new WpfC.TextBlock
                {
                    Text = m.Title,
                    FontSize = 12,
                    Foreground = Brush("#E9ECEF"),
                    TextTrimming = Wpf.TextTrimming.CharacterEllipsis
                });
                string d = m.Detail;
                if (d.Length > 0)
                    sp.Children.Add(new WpfC.TextBlock
                    {
                        Text = d,
                        FontSize = 10,
                        Margin = new Wpf.Thickness(0, 2, 0, 0),
                        Foreground = Brush("#8B929B"),
                        TextTrimming = Wpf.TextTrimming.CharacterEllipsis
                    });

                var row = new WpfC.Border
                {
                    Background = Brush(sel ? "#22303F" : "#1B1F23"),
                    BorderBrush = Brush(sel ? "#3C6FA8" : "#00000000"),
                    BorderThickness = new Wpf.Thickness(1),
                    CornerRadius = new Wpf.CornerRadius(3),
                    Padding = new Wpf.Thickness(8, 6, 8, 6),
                    Margin = new Wpf.Thickness(0, 0, 0, 3),
                    Cursor = Wpf.Input.Cursors.Hand,
                    Child = sp
                };
                row.MouseLeftButtonUp += (s, e) => SelectMap(captured);
                row.MouseEnter += (s, e) => { if (!( _lbSel != null && _lbSel.Id == captured.Id)) row.Background = Brush("#23282E"); };
                row.MouseLeave += (s, e) => { if (!(_lbSel != null && _lbSel.Id == captured.Id)) row.Background = Brush("#1B1F23"); };
                _lbMapList.Children.Add(row);
            }
        }

        private void SelectMap(LobbyMap m)
        {
            if (m == null) return;
            _lbSel = m;
            RenderMaps();
            _lbRoomTitle.Text = "房间 · " + m.Title;
            LobbyLoadRooms(m);
        }

        // ───────────── 房间列表 ─────────────

        private void LobbyLoadRooms(LobbyMap m)
        {
            bool pe = LbPe;
            _lbRoomList.Children.Clear();
            _lbRooms.Clear();
            _lbRoomList.Children.Add(new WpfC.TextBlock
            {
                Text = "获取中…",
                FontSize = 11,
                Foreground = Brush("#8B929B"),
                Margin = new Wpf.Thickness(4, 8, 0, 0)
            });

            AppHost.KickOff(() =>
            {
                List<LobbyRoom> rooms = null;
                string err = null;
                try { rooms = LobbyHall.ListRooms(m.Id, pe, 0, 30, m.Name, s => LogUi("  [大厅] " + s + "\r\n")); }
                catch (Exception ex) { err = ex.Message; }

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    _lbRoomList.Children.Clear();
                    if (err != null)
                    {
                        _lbRoomList.Children.Add(new WpfC.TextBlock
                        {
                            Text = "获取失败：" + err,
                            FontSize = 11,
                            Foreground = Brush("#E05A5A"),
                            TextWrapping = Wpf.TextWrapping.Wrap,
                            Margin = new Wpf.Thickness(4, 8, 0, 0)
                        });
                        return;
                    }
                    _lbRooms.AddRange(rooms);
                    RenderRooms();
                }));
            }, "lobby-rooms");
        }

        private void RenderRooms()
        {
            _lbRoomList.Children.Clear();

            if (_lbRooms.Count == 0)
            {
                _lbRoomList.Children.Add(new WpfC.TextBlock
                {
                    Text = "这张地图现在没有房间（或全部满员）",
                    FontSize = 11,
                    Foreground = Brush("#8B929B"),
                    TextWrapping = Wpf.TextWrapping.Wrap,
                    Margin = new Wpf.Thickness(4, 8, 0, 0)
                });
                return;
            }

            foreach (var r in _lbRooms)
            {
                var captured = r;

                var g = new WpfC.Grid();
                g.ColumnDefinitions.Add(new WpfC.ColumnDefinition { Width = new Wpf.GridLength(1, Wpf.GridUnitType.Star) });
                g.ColumnDefinitions.Add(new WpfC.ColumnDefinition { Width = Wpf.GridLength.Auto });
                g.ColumnDefinitions.Add(new WpfC.ColumnDefinition { Width = Wpf.GridLength.Auto });

                var sp = new WpfC.StackPanel { VerticalAlignment = Wpf.VerticalAlignment.Center };
                sp.Children.Add(new WpfC.TextBlock
                {
                    Text = "房号 " + (r.Name.Length > 0 ? r.Name : r.Id) +
                           "　" + r.Members + "/" + (r.MaxMembers > 0 ? r.MaxMembers : 10) + " 人" +
                           (r.HasPwd ? "　🔒" : ""),
                    FontSize = 12,
                    Foreground = Brush("#E9ECEF"),
                    TextTrimming = Wpf.TextTrimming.CharacterEllipsis
                });
                sp.Children.Add(new WpfC.TextBlock
                {
                    Text = "房主 " + (r.OwnerName.Length > 0 ? r.OwnerName : (r.OwnerId.Length > 0 ? r.OwnerId : "未知")) +
                           "　地图 " + (r.MapName.Length > 0 ? r.MapName : "未知"),
                    FontSize = 10,
                    Margin = new Wpf.Thickness(0, 2, 0, 0),
                    Foreground = Brush("#8B929B"),
                    TextTrimming = Wpf.TextTrimming.CharacterEllipsis
                });
                WpfC.Grid.SetColumn(sp, 0);
                g.Children.Add(sp);

                var bInfo = new PCL.MyButton
                {
                    Text = "详情",
                    Width = 52,
                    Height = 26,
                    Margin = new Wpf.Thickness(6, 0, 0, 0),
                    VerticalAlignment = Wpf.VerticalAlignment.Center
                };
                WpfC.TextBlock.SetFontSize(bInfo, 11);
                bInfo.Click += (s, e) => ShowRoomDetail(captured);
                WpfC.Grid.SetColumn(bInfo, 1);
                g.Children.Add(bInfo);

                var bEnter = new PCL.MyButton
                {
                    Text = "进入",
                    Width = 52,
                    Height = 26,
                    Margin = new Wpf.Thickness(6, 0, 0, 0),
                    ColorType = PCL.MyButton.ColorState.Highlight,
                    VerticalAlignment = Wpf.VerticalAlignment.Center
                };
                WpfC.TextBlock.SetFontSize(bEnter, 11);
                bEnter.Click += (s, e) => EnterLobbyRoom(captured);
                WpfC.Grid.SetColumn(bEnter, 2);
                g.Children.Add(bEnter);

                var row = new WpfC.Border
                {
                    Background = Brush("#1B1F23"),
                    CornerRadius = new Wpf.CornerRadius(3),
                    Padding = new Wpf.Thickness(8, 6, 8, 6),
                    Margin = new Wpf.Thickness(0, 0, 0, 3),
                    Child = g
                };
                _lbRoomList.Children.Add(row);
            }
        }

        // ───────────── 房间详情（玩家列表）─────────────

        private void ShowRoomDetail(LobbyRoom r)
        {
            if (r == null) return;

            var win = new Wpf.Window
            {
                Title = "房间详情 · " + (r.Name.Length > 0 ? r.Name : r.Id),
                Width = 400,
                Height = 430,
                Owner = this,
                WindowStartupLocation = Wpf.WindowStartupLocation.CenterOwner,
                Background = Brush("#121417"),
                ResizeMode = Wpf.ResizeMode.CanResizeWithGrip,
                ShowInTaskbar = false
            };

            var root = new WpfC.StackPanel { Margin = new Wpf.Thickness(16, 14, 16, 14) };

            Action<string, string, string> kv = (k, v, colorHex) =>
            {
                var line = new WpfC.TextBlock { FontSize = 11.5, Margin = new Wpf.Thickness(0, 0, 0, 3), TextWrapping = Wpf.TextWrapping.Wrap };
                line.Inlines.Add(new System.Windows.Documents.Run(k) { Foreground = Brush("#8B929B") });
                line.Inlines.Add(new System.Windows.Documents.Run(v) { Foreground = Brush(colorHex) });
                root.Children.Add(line);
            };

            kv("房号　　", r.Name.Length > 0 ? r.Name : r.Id, "#E9ECEF");
            kv("房间ID　", r.Id, "#8B929B");
            kv("大厅　　", r.Edition + " 端", r.Edition == "PE" ? "#5AC08A" : "#6FA8E8");
            kv("地图　　", r.MapName.Length > 0 ? r.MapName : "未知", "#E9ECEF");
            kv("房主　　", (r.OwnerName.Length > 0 ? r.OwnerName : "未知") + "（" + r.OwnerId + "）", "#E9ECEF");
            kv("人数　　", r.Members + " / " + (r.MaxMembers > 0 ? r.MaxMembers : 10), "#E9ECEF");
            if (r.HasPwd) kv("密码　　", "有密码（进入时用空密码尝试）", "#E0A23C");

            root.Children.Add(new WpfC.Border
            {
                Height = 1,
                Background = Brush("#2B2F34"),
                Margin = new Wpf.Thickness(0, 10, 0, 10)
            });

            var head = new WpfC.TextBlock
            {
                Text = "玩家列表",
                FontSize = 11,
                Foreground = Brush("#8B929B"),
                Margin = new Wpf.Thickness(0, 0, 0, 6)
            };
            root.Children.Add(head);

            var listPanel = new WpfC.StackPanel();
            var sc = new WpfC.ScrollViewer
            {
                Height = 150,
                VerticalScrollBarVisibility = WpfC.ScrollBarVisibility.Auto,
                Content = listPanel
            };
            root.Children.Add(sc);

            var tip = new WpfC.TextBlock
            {
                Text = "获取中…",
                FontSize = 11,
                Foreground = Brush("#8B929B"),
                Margin = new Wpf.Thickness(2, 4, 0, 0)
            };
            root.Children.Add(tip);

            var btnEnter = new PCL.MyButton
            {
                Text = "一键进入这个房间",
                Height = 32,
                Margin = new Wpf.Thickness(0, 12, 0, 0),
                ColorType = PCL.MyButton.ColorState.Highlight
            };
            WpfC.TextBlock.SetFontSize(btnEnter, 12);
            btnEnter.Click += (s, e) => { win.Close(); EnterLobbyRoom(r); };
            root.Children.Add(btnEnter);

            win.Content = root;
            win.Show();

            AppHost.KickOff(() =>
            {
                List<LobbyMember> mem = null;
                string err = null;
                try { mem = LobbyHall.ListMembers(r.Id, r.Edition == "PE", s => LogUi("  [大厅] " + s + "\r\n")); }
                catch (Exception ex) { err = ex.Message; }

                win.Dispatcher.BeginInvoke(new Action(() =>
                {
                    listPanel.Children.Clear();
                    if (err != null) { tip.Text = "玩家列表获取失败：" + err; tip.Foreground = Brush("#E05A5A"); return; }
                    if (mem == null || mem.Count == 0) { tip.Text = "接口没有返回玩家（可能刚开局/刚散场）"; return; }

                    tip.Text = "共 " + mem.Count + " 人";
                    int i = 1;
                    foreach (var m in mem)
                    {
                        listPanel.Children.Add(new WpfC.TextBlock
                        {
                            Text = (i++) + ".　" + (m.Nick.Length > 0 ? m.Nick : m.Uid) +
                                   (m.Nick.Length > 0 ? "（" + m.Uid + "）" : ""),
                            FontSize = 11.5,
                            Foreground = Brush("#E9ECEF"),
                            Margin = new Wpf.Thickness(2, 2, 0, 2),
                            TextTrimming = Wpf.TextTrimming.CharacterEllipsis
                        });
                    }
                }));
            }, "lobby-members");
        }

        // ───────────── 一键进服 ─────────────

        private void EnterLobbyRoom(LobbyRoom r)
        {
            if (r == null) return;
            if (!AppHost.Started)
            {
                LbStatus("请先登录再进房", "#E0A23C");
                return;
            }

            LbStatus("获取中…正在进「" + (r.Name.Length > 0 ? r.Name : r.Id) + "」取 IP", "#4C8DFF");

            var cand = new RoomCandidate
            {
                Lobby = r.Edition,
                EntityId = r.Id,
                Name = r.Name,
                ResId = r.ResId,
                Manifest = r.Manifest,
                OwnerId = r.OwnerId,
                OwnerName = r.OwnerName,
                MapName = r.MapName,
                Members = r.Members,
                MaxMembers = r.MaxMembers,
                HasPwd = r.HasPwd
            };

            AppHost.KickOff(() =>
            {
                try
                {
                    string addr = RoomIpQuery.QueryCandidate(cand, s => LogUi("  [大厅] " + s + "\r\n"));
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _addrBox.Text = addr;
                        _typeBox.SelectedIndex = 1;   // 官方大厅 (1000)
                        LbStatus("获取成功：" + addr + "，正在启动游戏…", "#2E9E63");
                        AppendLog("  ✓ " + addr + " 已填入地址（官方大厅 1000），开始启动\r\n");
                        DoLaunch();
                    }));
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        LbStatus("获取失败：" + ex.Message, "#E05A5A");
                        LogUi("✗ " + ex.Message + "\r\n");
                    }));
                }
            }, "lobby-enter");
        }

        private void LbStatus(string text, string colorHex)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => LbStatus(text, colorHex)));
                return;
            }
            _lbStatus.Text = text;
            _lbStatus.Foreground = Brush(colorHex);
        }
    }
}
