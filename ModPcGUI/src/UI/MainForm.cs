using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using ConsoleAppLogin.NetEase;
using Newtonsoft.Json.Linq;

namespace ModPcGUI
{
    /// <summary>启动器主界面</summary>
    public sealed class MainForm : Form
    {
        // ---------- 配色 ----------
        private static readonly Color Bg = Color.FromArgb(0x11, 0x13, 0x16);
        private static readonly Color Card = Color.FromArgb(0x19, 0x1C, 0x20);
        private static readonly Color Line = Color.FromArgb(0x2B, 0x2F, 0x34);
        private static readonly Color TextMain = Color.FromArgb(0xE9, 0xEC, 0xEF);
        private static readonly Color TextSub = Color.FromArgb(0x8B, 0x92, 0x9B);
        private static readonly Color Accent = Color.FromArgb(0x4C, 0x8D, 0xFF);
        private static readonly Color Green = Color.FromArgb(0x2E, 0x9E, 0x63);
        private static readonly Color Ok = Color.FromArgb(0x4A, 0xDE, 0x80);
        private static readonly Color Warn = Color.FromArgb(0xF0, 0xB4, 0x29);

        private static readonly Font FTitle = new Font("Microsoft YaHei UI", 15f, FontStyle.Bold);
        private static readonly Font FSub = new Font("Microsoft YaHei UI", 8.5f);
        private static readonly Font FNorm = new Font("Microsoft YaHei UI", 9f);
        private static readonly Font FBold = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold);
        private static readonly Font FBig = new Font("Microsoft YaHei UI", 11f, FontStyle.Bold);

        // ---------- 控件 ----------
        private LogView _log;
        private ComboBox _cmbAcc;
        private ComboBox _cmbMode;
        private Label _lblName, _lblState, _lblVer, _lblAccSub;
        private PictureBox _avatar;
        private TextBox _pathBox, _addrBox;
        private RadioButton _rbOfficial, _rbCustom;
        private ComboBox _cmbGameType;
        private Button _btnLogin, _btnAdd, _btnDel, _btnStart, _btnBjd, _btnEc, _btnMore, _btnOffline;
        private Button _btnSavePath, _btnBrowse;
        private Panel _launchCard;
        private ToolStripStatusLabel _stMsg;
        private ToolStripStatusLabel _stUid;

        private readonly List<AccountItem> _accounts = new List<AccountItem>();
        private readonly ToolTip _tip = new ToolTip();
        private JObject _ui;
        private readonly string _uiPath;

        private sealed class AccountItem
        {
            public string Path;
            public string Name;
            public override string ToString() { return Name; }
        }

        public MainForm()
        {
            Text = "ES Tool";
            StartPosition = FormStartPosition.CenterScreen;

            var wa = Screen.PrimaryScreen.WorkingArea;
            ClientSize = new Size(Math.Min(1120, (int)(wa.Width * 0.92)), Math.Min(780, (int)(wa.Height * 0.92)));
            MinimumSize = new Size(920, 620);
            BackColor = Bg;
            Font = FNorm;
            DoubleBuffered = true;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            _uiPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ui.json");

            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F1) { OpenFeatures(); e.Handled = true; }
                else if (e.KeyCode == Keys.F5) { DoLaunch(); e.Handled = true; }
            };

            BuildUi();
            HookTerm();
            Theme.Apply(this);
            HandleCreated += (s, e) => Theme.DarkTitleBar(Handle);
            AppHost.StateChanged += () => SafeInvoke(RefreshState);
        }

        // ============================================================
        //  界面
        // ============================================================
        private void BuildUi()
        {
            var status = new StatusStrip
            {
                BackColor = Color.FromArgb(0x19, 0x1C, 0x20),
                SizingGrip = false,
                Font = FSub,
                Padding = new Padding(10, 0, 10, 0)
            };
            _stMsg = new ToolStripStatusLabel("就绪") { ForeColor = TextSub };
            _stUid = new ToolStripStatusLabel("UID: -") { ForeColor = TextSub, Alignment = ToolStripItemAlignment.Right };
            status.Items.Add(_stMsg);
            status.Items.Add(new ToolStripStatusLabel { Spring = true });
            status.Items.Add(_stUid);

            // ---------- 顶栏 ----------
            var head = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Color.FromArgb(0x19, 0x1C, 0x20), Padding = new Padding(18, 0, 18, 0) };
            head.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Line), 0, head.Height - 1, head.Width, head.Height - 1);

            head.Controls.Add(new Label
            {
                Text = "ES Tool",
                Font = FTitle,
                ForeColor = TextMain,
                AutoSize = true,
                Location = new Point(20, 10)
            });
            head.Controls.Add(new Label
            {
                Text = "MINECRAFT 中国版  ·  客户端工具",
                Font = FSub,
                ForeColor = TextSub,
                AutoSize = true,
                Location = new Point(22, 38)
            });

            var rightState = new Panel { Dock = DockStyle.Right, Width = 320 };
            _lblState = new Label
            {
                Text = "● 未登录",
                Font = FBold,
                ForeColor = Color.FromArgb(0x6E, 0x76, 0x80),
                Dock = DockStyle.Top,
                Height = 34,
                TextAlign = ContentAlignment.MiddleRight
            };
            _lblVer = new Label
            {
                Text = "",
                Font = FSub,
                ForeColor = TextSub,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopRight
            };
            rightState.Controls.Add(_lblVer);
            rightState.Controls.Add(_lblState);
            head.Controls.Add(rightState);

            // ---------- 主体 ----------
            var body = new Panel { Dock = DockStyle.Fill, BackColor = Bg };
            var left = new Panel { Dock = DockStyle.Left, Width = 286, BackColor = Bg, Padding = new Padding(14, 12, 6, 12) };
            var right = new Panel { Dock = DockStyle.Fill, BackColor = Bg, Padding = new Padding(6, 12, 14, 12) };

            BuildLeft(left);
            BuildRight(right);

            body.Controls.Add(right);
            body.Controls.Add(left);

            Controls.Add(body);
            Controls.Add(head);
            Controls.Add(status);
        }

        private void BuildLeft(Panel host)
        {
            // 空白填充（先加）
            var spacer = new Panel { Dock = DockStyle.Fill, BackColor = Bg };
            host.Controls.Add(spacer);

            // 底部按钮
            var bottomRow = new Panel { Dock = DockStyle.Bottom, Height = 46, Padding = new Padding(0, 8, 0, 0) };
            _btnMore = FlatButton("更多功能 ›", Color.FromArgb(0x24, 0x28, 0x2D), TextMain, FNorm);
            _btnMore.Dock = DockStyle.Right;
            _btnMore.Width = 140;
            _btnMore.Click += (s, e) => OpenFeatures();

            _btnOffline = FlatButton("离线模式", Color.FromArgb(0x24, 0x28, 0x2D), TextMain, FSub);
            _btnOffline.Dock = DockStyle.Right;
            _btnOffline.Width = 98;
            _btnOffline.Click += (s, e) =>
            {
                if (AppHost.Started) { _log.PushSystem("已经在控制台中"); return; }
                _log.PushSystem("进入离线模式（未登录，仅本地功能可用）");
                AppHost.KickOff(AppHost.Offline, "offline");
            };
            bottomRow.Controls.Add(_btnOffline);
            bottomRow.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 8 });
            bottomRow.Controls.Add(_btnMore);
            host.Controls.Add(bottomRow);

            // 账号卡片
            var card = new Panel { Dock = DockStyle.Top, Height = 206, BackColor = Card };
            card.Paint += (s, e) =>
            {
                using (var p = new Pen(Line)) e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
            };

            // 极简头像：深色方块 + 账号首字母，不加载任何图片
            _avatar = new PictureBox
            {
                Location = new Point(16, 16),
                Size = new Size(48, 48),
                BackColor = Color.FromArgb(0x24, 0x28, 0x2D)
            };
            _avatar.Paint += (s, e) =>
            {
                string t = "E";
                try
                {
                    if (_lblName != null && !string.IsNullOrEmpty(_lblName.Text)
                        && _lblName.Text != "未登录")
                        t = _lblName.Text.Substring(0, 1).ToUpperInvariant();
                }
                catch { }
                using (var b = new SolidBrush(Accent))
                using (var f = new Font("Segoe UI", 17f, FontStyle.Bold))
                {
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    e.Graphics.DrawString(t, f, b,
                        new RectangleF(0, 0, _avatar.Width, _avatar.Height), sf);
                }
            };
            card.Controls.Add(_avatar);

            _lblName = new Label
            {
                Text = "未登录",
                Font = new Font("Microsoft YaHei UI", 12f, FontStyle.Bold),
                ForeColor = TextMain,
                Location = new Point(76, 20),
                Size = new Size(182, 24),
                AutoEllipsis = true
            };
            card.Controls.Add(_lblName);

            _lblAccSub = new Label
            {
                Text = "选择账号后点登录",
                Font = FSub,
                ForeColor = TextSub,
                Location = new Point(78, 46),
                Size = new Size(182, 18)
            };
            card.Controls.Add(_lblAccSub);

            _cmbAcc = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = FNorm,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(16, 88),
                Width = 150
            };
            _cmbAcc.SelectedIndexChanged += (s, e) => UpdateAccountHint();
            card.Controls.Add(_cmbAcc);

            _cmbMode = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = FNorm,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(172, 88),
                Width = 88
            };
            _cmbMode.Items.AddRange(new object[] { "自动识别", "电脑版", "手机版" });
            _cmbMode.SelectedIndex = 0;
            card.Controls.Add(_cmbMode);
            _tip.SetToolTip(_cmbMode, "自动：按 Sauth 格式判断（{…}=电脑版，ey…/SDK4399=手机版）\r\n识别错了就手动选「电脑版」或「手机版」");

            _btnLogin = FlatButton("登 录", Accent, Color.FromArgb(0xF2, 0xF4, 0xF6), FBold);
            _btnLogin.Location = new Point(16, 122);
            _btnLogin.Size = new Size(244, 38);
            _btnLogin.Click += (s, e) => DoLogin();
            Round(_btnLogin, 4);
            card.Controls.Add(_btnLogin);

            _btnAdd = FlatButton("添加账号", Color.FromArgb(0x24, 0x28, 0x2D), TextMain, FSub);
            _btnAdd.Location = new Point(16, 168);
            _btnAdd.Size = new Size(116, 26);
            _btnAdd.Click += (s, e) => DoAddAccount();
            card.Controls.Add(_btnAdd);

            _btnDel = FlatButton("删除账号", Color.FromArgb(0x24, 0x28, 0x2D), TextMain, FSub);
            _btnDel.Location = new Point(140, 168);
            _btnDel.Size = new Size(120, 26);
            _btnDel.Click += (s, e) => DoDeleteAccount();
            card.Controls.Add(_btnDel);

            host.Controls.Add(card);

        }

        private void BuildRight(Panel host)
        {
            // ---- 日志（Fill，先加）----
            var logHost = new Panel { Dock = DockStyle.Fill, BackColor = Bg, Padding = new Padding(0, 10, 0, 0) };
            var logFrame = new Panel { Dock = DockStyle.Fill, BackColor = Card, Padding = new Padding(12, 8, 6, 6) };
            logFrame.Paint += (s, e) =>
            {
                using (var p = new Pen(Line)) e.Graphics.DrawRectangle(p, 0, 0, logFrame.Width - 1, logFrame.Height - 1);
            };
            logFrame.Controls.Add(new Label
            {
                Text = "运行日志",
                Dock = DockStyle.Top,
                Height = 20,
                Font = FBold,
                ForeColor = TextSub
            });
            _log = new LogView { Dock = DockStyle.Fill };
            logFrame.Controls.Add(_log);
            logHost.Controls.Add(logFrame);
            host.Controls.Add(logHost);

            // ---- 操作按钮行 ----
            var btnRow = new Panel { Dock = DockStyle.Top, Height = 78, BackColor = Bg };

            _btnStart = FlatButton("启动游戏", Green, Color.FromArgb(0xF2, 0xF4, 0xF6), FBig);
            _btnStart.Location = new Point(0, 12);
            _btnStart.Size = new Size(210, 54);
            _btnStart.Click += (s, e) => DoLaunch();
            Round(_btnStart, 4);
            btnRow.Controls.Add(_btnStart);

            _btnBjd = FlatButton("一键布吉岛", Card, TextMain, FBold);
            _btnBjd.Location = new Point(224, 12);
            _btnBjd.Size = new Size(166, 54);
            _btnBjd.Click += (s, e) => DoQuick("fastbjd", "一键布吉岛");
            Round(_btnBjd, 4);
            btnRow.Controls.Add(_btnBjd);

            _btnEc = FlatButton("一键 EC", Card, TextMain, FBold);
            _btnEc.Location = new Point(402, 12);
            _btnEc.Size = new Size(166, 54);
            _btnEc.Click += (s, e) => DoQuick("fastec", "一键 EC");
            Round(_btnEc, 4);
            btnRow.Controls.Add(_btnEc);

            host.Controls.Add(btnRow);

            // ---- 启动设置卡 ----
            _launchCard = new Panel { Dock = DockStyle.Top, Height = 196, BackColor = Card };
            _launchCard.Paint += (s, e) =>
            {
                using (var p = new Pen(Line)) e.Graphics.DrawRectangle(p, 0, 0, _launchCard.Width - 1, _launchCard.Height - 1);
            };

            _launchCard.Controls.Add(new Label
            {
                Text = "启动设置",
                Font = new Font("Microsoft YaHei UI", 11f, FontStyle.Bold),
                ForeColor = TextMain,
                AutoSize = true,
                Location = new Point(18, 12)
            });

            // 游戏路径
            _launchCard.Controls.Add(new Label
            {
                Text = "游戏路径（x64_mc 目录）",
                Font = FSub,
                ForeColor = TextSub,
                AutoSize = true,
                Location = new Point(18, 42)
            });

            _pathBox = new TextBox
            {
                Location = new Point(18, 62),
                Height = 26,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _launchCard.Controls.Add(_pathBox);

            _btnBrowse = FlatButton("浏览", Color.FromArgb(0x24, 0x28, 0x2D), TextMain, FSub);
            _btnBrowse.Size = new Size(60, 26);
            _btnBrowse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnBrowse.Location = new Point(600, 62);
            _btnBrowse.Click += (s, e) => BrowsePath();
            _launchCard.Controls.Add(_btnBrowse);

            _btnSavePath = FlatButton("保存路径", Accent, Color.FromArgb(0xF2, 0xF4, 0xF6), FSub);
            _btnSavePath.Size = new Size(84, 26);
            _btnSavePath.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnSavePath.Location = new Point(668, 62);
            _btnSavePath.Click += (s, e) => SavePath();
            _launchCard.Controls.Add(_btnSavePath);

            // 服务器
            _launchCard.Controls.Add(new Label
            {
                Text = "服务器",
                Font = FSub,
                ForeColor = TextSub,
                AutoSize = true,
                Location = new Point(18, 100)
            });

            _rbOfficial = new RadioButton
            {
                Text = "官方联机大厅",
                Checked = true,
                AutoSize = true,
                Font = FNorm,
                ForeColor = TextMain,
                Location = new Point(76, 98)
            };
            _rbCustom = new RadioButton
            {
                Text = "自定义地址",
                AutoSize = true,
                Font = FNorm,
                ForeColor = TextMain,
                Location = new Point(196, 98)
            };
            _rbOfficial.CheckedChanged += (s, e) => UpdateAddrEnabled();
            _launchCard.Controls.Add(_rbOfficial);
            _launchCard.Controls.Add(_rbCustom);

            _cmbGameType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = FNorm,
                FlatStyle = FlatStyle.Flat,
                Width = 168,
                Location = new Point(320, 96),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _cmbGameType.Items.AddRange(new object[]
            {
                "第三方服务器 (100)",
                "官方大厅 (1000)",
                "局域网 (1)"
            });
            _cmbGameType.SelectedIndex = 0;
            _launchCard.Controls.Add(_cmbGameType);

            _addrBox = new TextBox
            {
                Text = "127.0.0.1:1000",
                Location = new Point(76, 130),
                Height = 26,
                Width = 320,
                Font = new Font("Consolas", 10f),
                BorderStyle = BorderStyle.FixedSingle
            };
            _launchCard.Controls.Add(_addrBox);

            _launchCard.Controls.Add(new Label
            {
                Text = "格式 IP:端口 —— 例如 127.0.0.1:1000 、 play.bjdmc.net:19132",
                Font = FSub,
                ForeColor = TextSub,
                AutoSize = true,
                Location = new Point(76, 162)
            });

            host.Controls.Add(_launchCard);

            _launchCard.Resize += (s, e) => LayoutLaunchCard();
            LayoutLaunchCard();
        }

        private void LayoutLaunchCard()
        {
            if (_launchCard == null) return;
            int w = _launchCard.ClientSize.Width;
            if (_btnSavePath != null) _btnSavePath.Left = w - 18 - 84;
            if (_btnBrowse != null) _btnBrowse.Left = w - 18 - 84 - 8 - 60;
            if (_pathBox != null) _pathBox.Width = Math.Max(120, w - 18 * 2 - 84 - 8 - 60 - 8);
            if (_cmbGameType != null) _cmbGameType.Left = w - 18 - _cmbGameType.Width;
        }

        // ============================================================
        //  启动器动作
        // ============================================================
        private bool Ready()
        {
            if (!AppHost.Started)
            {
                MessageBox.Show(this, "还没有登录。\r\n先在左侧选择账号点「登 录」，或用「离线模式」。", "提示");
                return false;
            }
            return true;
        }

        private void DoLaunch()
        {
            if (!Ready()) return;

            string path = _pathBox.Text.Trim();
            if (path.Length == 0)
            {
                MessageBox.Show(this, "先设置游戏路径（x64_mc 目录）。", "提示");
                return;
            }

            if (_rbOfficial.Checked)
            {
                _log.PushSystem("→ 启动游戏（官方联机大厅）");
                Term.Submit("start");
                _log.PushSystem("启动指令已发出，游戏窗口通常 3~10 秒内出现；没反应请看上面的日志");
                return;
            }

            string ip, portStr;
            if (!SplitAddr(_addrBox.Text, out ip, out portStr))
            {
                MessageBox.Show(this, "地址格式不对，应该像 127.0.0.1:1000", "提示");
                return;
            }

            int mode = 100;
            if (_cmbGameType.SelectedIndex == 1) mode = 1000;
            else if (_cmbGameType.SelectedIndex == 2) mode = 1;

            SaveUi();
            _log.PushSystem("→ 启动游戏（自定义 " + ip + ":" + portStr + "，模式 " + mode + "）");
            Term.Submit("gametype");
            Term.Submit(mode.ToString(CultureInfo.InvariantCulture));
            Term.Submit(ip);
            Term.Submit(portStr);
            _log.PushSystem("启动指令已发出，游戏窗口通常 3~10 秒内出现；没反应请看上面的日志");
        }

        private void DoQuick(string cmd, string label)
        {
            if (!Ready()) return;
            if (_pathBox.Text.Trim().Length == 0)
            {
                MessageBox.Show(this, "先设置游戏路径（x64_mc 目录）。", "提示");
                return;
            }
            _log.PushSystem("→ " + label + "（" + cmd + "）");
            Term.Submit(cmd);
        }

        private static bool SplitAddr(string raw, out string ip, out string port)
        {
            ip = null;
            port = null;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            string s = raw.Trim();

            int idx = s.LastIndexOf(':');
            if (idx > 0 && idx < s.Length - 1)
            {
                ip = s.Substring(0, idx).Trim();
                port = s.Substring(idx + 1).Trim();
            }
            else
            {
                var parts = s.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2) { ip = parts[0]; port = parts[1]; }
                else return false;
            }

            if (ip.Length == 0 || port.Length == 0) return false;
            int p;
            if (!int.TryParse(port, out p) || p <= 0 || p > 65535) return false;
            if (ip.StartsWith("[") && ip.EndsWith("]")) ip = ip.Substring(1, ip.Length - 2);
            return true;
        }

        private void BrowsePath()
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "选择 x64_mc 目录（里面有 Minecraft.Windows.exe）";
                string cur = _pathBox.Text.Trim();
                if (Directory.Exists(cur)) dlg.SelectedPath = cur;
                if (dlg.ShowDialog(this) == DialogResult.OK) _pathBox.Text = dlg.SelectedPath;
            }
        }

        private void SavePath()
        {
            string p = _pathBox.Text.Trim();
            if (p.Length == 0) { MessageBox.Show(this, "路径不能为空", "提示"); return; }
            if (!Directory.Exists(p))
            {
                MessageBox.Show(this, "目录不存在：\r\n" + p, "提示");
                return;
            }
            if (!Ready()) return;
            _log.PushSystem("→ 设置游戏路径：" + p);
            Term.Submit("setpath");
            Term.Submit(p);
            SaveUi();
        }

        private void UpdateAddrEnabled()
        {
            bool custom = _rbCustom.Checked;
            if (_addrBox != null) _addrBox.Enabled = custom;
            if (_cmbGameType != null) _cmbGameType.Enabled = custom;
        }

        private void OpenFeatures()
        {
            ShowFeatures();
        }

        /// <summary>打开「更多功能」窗口（也用于自动化测试）</summary>
        public void ShowFeatures()
        {
            using (var f = new FeaturesForm(_log))
            {
                f.ShowDialog(this);
            }
        }

        // ============================================================
        //  账号
        // ============================================================
        private void RefreshAccounts()
        {
            var files = AppHost.ListAccounts();
            var keep = _cmbAcc.SelectedItem as AccountItem;
            string keepPath = keep != null ? keep.Path : null;

            _accounts.Clear();
            _cmbAcc.Items.Clear();
            foreach (var f in files)
            {
                var it = new AccountItem { Path = f, Name = Path.GetFileNameWithoutExtension(f) };
                _accounts.Add(it);
                _cmbAcc.Items.Add(it);
            }

            if (_cmbAcc.Items.Count > 0)
            {
                int idx = 0;
                if (keepPath != null)
                {
                    for (int i = 0; i < _accounts.Count; i++)
                        if (_accounts[i].Path == keepPath) { idx = i; break; }
                }
                _cmbAcc.SelectedIndex = idx;
            }
            UpdateAccountHint();
        }

        private void UpdateAccountHint()
        {
            var it = _cmbAcc.SelectedItem as AccountItem;
            if (it != null)
            {
                _lblName.Text = it.Name;
                if (_lblAccSub != null) _lblAccSub.Text = GuessKind(it.Path);
                if (!AppHost.Started && !AppHost.LoggingIn)
                {
                    _lblState.Text = "● 未登录";
                    _lblState.ForeColor = Color.FromArgb(0x6E, 0x76, 0x80);
                }
            }
            else
            {
                _lblName.Text = "未登录";
                if (_lblAccSub != null) _lblAccSub.Text = "选择账号后点登录";
                _lblState.Text = "● 无账号";
                _lblState.ForeColor = Color.FromArgb(0x6E, 0x76, 0x80);
            }
        }

        private static string GuessKind(string path)
        {
            try
            {
                string s = File.ReadAllText(path).TrimStart();
                if (s.StartsWith("{")) return "电脑版 Sauth（JSON）";
                if (s.StartsWith("ey")) return "手机版 Sauth（ey…）";
                if (s.StartsWith("SDK4399")) return "4399 账号";
                if (s.Length == 0) return "文件为空";
                return "未知格式，可手动选类型";
            }
            catch
            {
                return "";
            }
        }

        private void DoAddAccount()
        {
            using (var dlg = new AddAccountDialog())
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                AppHost.AddAccount(dlg.AccountName, dlg.Cookie);
                RefreshAccounts();
                _log.PushSystem("账号已添加：" + dlg.AccountName);
            }
        }

        private void DoDeleteAccount()
        {
            var it = _cmbAcc.SelectedItem as AccountItem;
            if (it == null) return;
            if (MessageBox.Show(this, "删除账号 " + it.Name + " ？", "确认",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
            AppHost.DeleteAccount(it.Path);
            RefreshAccounts();
        }

        private void DoLogin()
        {
            if (AppHost.Started) { _log.PushSystem("已经在控制台中，无需重复登录"); return; }
            if (AppHost.LoggingIn) { _log.PushSystem("正在登录中，请稍候…"); return; }

            var it = _cmbAcc.SelectedItem as AccountItem;
            if (it == null)
            {
                MessageBox.Show(this, "还没有账号，先点「添加账号」导入 Sauth。", "提示");
                return;
            }

            string mode = "auto";
            if (_cmbMode.SelectedIndex == 1) mode = "pc";
            else if (_cmbMode.SelectedIndex == 2) mode = "pe";

            SaveUi();
            Term.ClearInput();
            _log.PushSystem("开始登录：" + it.Name + "（" + _cmbMode.Text + "）");
            string file = it.Path;
            AppHost.KickOff(() => AppHost.Login(file, mode), "login");
        }

        // ============================================================
        //  Term / 状态
        // ============================================================
        private void HookTerm()
        {
            Term.OnOutput += (text, nl, color) => _log?.Push(text, nl, color);
            Term.OnClear += () => SafeInvoke(() => _log.ClearLog());
            Term.OnTitle += t => SafeInvoke(() => { if (!string.IsNullOrEmpty(t)) Text = t + " · ES Tool"; });
            Term.OnWaiting += b => SafeInvoke(() => UpdateWaiting(b));
        }

        private void UpdateWaiting(bool waiting)
        {
            if (waiting)
            {
                string p = Term.LastPrompt;
                if (p != null && p.Length > 100) p = p.Substring(p.Length - 100);
                _stMsg.Text = "⏳ 等待输入：" + (string.IsNullOrWhiteSpace(p) ? "（在「更多功能」里操作）" : p.Trim());
                _stMsg.ForeColor = Warn;
            }
            else
            {
                _stMsg.ForeColor = TextSub;
                RefreshState();
            }
        }

        private void SafeInvoke(Action a)
        {
            if (IsDisposed || !IsHandleCreated) return;
            try
            {
                if (InvokeRequired) BeginInvoke(a);
                else a();
            }
            catch { }
        }

        private void RefreshState()
        {
            string uid = Http.UID;
            bool run = AppHost.Started;
            bool busy = AppHost.LoggingIn && !run;

            if (busy)
            {
                _lblState.Text = "● 登录中…";
                _lblState.ForeColor = Warn;
            }
            else if (run)
            {
                _lblState.Text = string.IsNullOrEmpty(AppHost.AccountName)
                    ? "● 已连接（离线）"
                    : "● 已连接 · " + AppHost.AccountName;
                _lblState.ForeColor = Ok;
            }
            else
            {
                _lblState.Text = "● 未登录";
                _lblState.ForeColor = Color.FromArgb(0x6E, 0x76, 0x80);
            }

            _stUid.Text = string.IsNullOrEmpty(uid) ? "UID: -" : "UID: " + uid;
            if (!Term.IsWaiting) _stMsg.Text = busy ? "登录中…" : (run ? "就绪 · 可以启动了" : "未登录");

            _btnLogin.Text = run ? "已登录" : (busy ? "登录中…" : "登 录");
            _btnLogin.BackColor = run ? Color.FromArgb(0x2A, 0x30, 0x36) : Accent;

            bool can = run;
            // 不用 Enabled=false：WinForms 禁用态会强制系统灰字，深色下几乎看不见。
            // 只用配色区分，点击时由 Ready() 提示「请先登录」。
            _btnStart.BackColor = can ? Green : Color.FromArgb(0x2A, 0x30, 0x36);
            _btnBjd.BackColor = can ? Card : Color.FromArgb(0x25, 0x29, 0x2E);
            _btnEc.BackColor = can ? Card : Color.FromArgb(0x25, 0x29, 0x2E);
            _btnBjd.ForeColor = can ? TextMain : Color.FromArgb(0x9A, 0xA2, 0xAC);
            _btnEc.ForeColor = can ? TextMain : Color.FromArgb(0x9A, 0xA2, 0xAC);
            _btnStart.ForeColor = Color.FromArgb(0xF2, 0xF4, 0xF6);
        }

        // ============================================================
        //  ui.json
        // ============================================================
        private void LoadUi()
        {
            try
            {
                if (File.Exists(_uiPath)) _ui = JObject.Parse(File.ReadAllText(_uiPath));
            }
            catch { }
            if (_ui == null) _ui = new JObject();

            string path = (string)_ui["path"];
            string addr = (string)_ui["addr"];
            int mode = _ui["mode"] != null ? (int)_ui["mode"] : 0;
            bool custom = _ui["custom"] != null && (bool)_ui["custom"];

            if (string.IsNullOrEmpty(path))
            {
                try { if (text.app.Temp != null) path = text.app.Temp.path; } catch { }
            }
            if (!string.IsNullOrEmpty(path)) _pathBox.Text = path;
            if (!string.IsNullOrEmpty(addr)) _addrBox.Text = addr;
            if (mode >= 0 && mode < _cmbGameType.Items.Count) _cmbGameType.SelectedIndex = mode;

            int lm = _ui["loginMode"] != null ? (int)_ui["loginMode"] : 0;
            if (lm >= 0 && lm < _cmbMode.Items.Count) _cmbMode.SelectedIndex = lm;

            if (custom) _rbCustom.Checked = true; else _rbOfficial.Checked = true;
            UpdateAddrEnabled();
        }

        private void SaveUi()
        {
            try
            {
                if (_ui == null) _ui = new JObject();
                _ui["path"] = _pathBox.Text.Trim();
                _ui["addr"] = _addrBox.Text.Trim();
                _ui["mode"] = _cmbGameType.SelectedIndex;
                _ui["custom"] = _rbCustom.Checked;
                _ui["loginMode"] = _cmbMode.SelectedIndex;
                if (_cmbAcc.SelectedItem != null) _ui["account"] = ((AccountItem)_cmbAcc.SelectedItem).Path;
                File.WriteAllText(_uiPath, _ui.ToString(), new System.Text.UTF8Encoding(false));
            }
            catch { }
        }

        // ============================================================
        //  启动
        // ============================================================
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try
            {
                try { AppHost.Init(); }
                catch (Exception ex) { _log.PushLine("[init] " + ex.Message, ConsoleColor.Red); }

                if (text.app.Temp != null)
                    _lblVer.Text = text.app.Temp.Version;

                RefreshAccounts();
                LoadUi();
                RefreshState();
                UpdateAccountHint();

                _log.PushLine("ES Tool 已就绪", ConsoleColor.DarkCyan);
                _log.PushLine("主界面 4 个常用功能，其余 " + Commands.All.Count + " 条命令在「更多功能」", ConsoleColor.DarkGray);
            }
            catch (Exception ex)
            {
                try
                {
                    File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"),
                        "[OnShown] " + ex + "\r\n", System.Text.Encoding.UTF8);
                }
                catch { }
                MessageBox.Show(this, "界面初始化异常：\r\n" + ex.Message, "ES Tool");
            }
        }

        // ============================================================
        //  控件辅助
        // ============================================================
        private static Button FlatButton(string text, Color back, Color fore, Font font)
        {
            var b = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = fore,
                Font = font,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = ControlPaint.Light(back, 0.12f);
            b.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(back, 0.06f);
            return b;
        }

        private static void Round(Control c, int r)
        {
            try
            {
                var gp = new GraphicsPath();
                var rc = new Rectangle(0, 0, c.Width, c.Height);
                gp.AddArc(rc.X, rc.Y, r, r, 180, 90);
                gp.AddArc(rc.Right - r, rc.Y, r, r, 270, 90);
                gp.AddArc(rc.Right - r, rc.Bottom - r, r, r, 0, 90);
                gp.AddArc(rc.X, rc.Bottom - r, r, r, 90, 90);
                gp.CloseFigure();
                c.Region = new Region(gp);
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { SaveUi(); } catch { }
            try { Http.CloseAuthserver(); } catch { }
            base.OnFormClosing(e);

            // 原程序有 40+ 个前台线程（登录保活、聊天连接、Dummy、RPC…），
            // 只关主窗口进程不会退出。强制结束，避免残留占用 Mutex 和 11451 端口。
            try { Environment.Exit(0); } catch { }
        }
    }
}
