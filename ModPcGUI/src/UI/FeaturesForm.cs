using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ModPcGUI
{
    /// <summary>更多功能：全部分类命令 + 参数表单 + 高级命令输入</summary>
    public sealed class FeaturesForm : Form
    {
        private static readonly Color Bg = Color.FromArgb(0x11, 0x13, 0x16);
        private static readonly Color Card = Color.FromArgb(0x19, 0x1C, 0x20);
        private static readonly Color Line = Color.FromArgb(0x2B, 0x2F, 0x34);
        private static readonly Color TextMain = Color.FromArgb(0xE9, 0xEC, 0xEF);
        private static readonly Color TextSub = Color.FromArgb(0x8B, 0x92, 0x9B);
        private static readonly Color Accent = Color.FromArgb(0x4C, 0x8D, 0xFF);

        private TreeView _tree;
        private Panel _formHost;
        private Label _lblTitle, _lblCmd, _lblHint;
        private Button _btnRun;
        private TextBox _txtRaw;
        private CmdDef _cur;
        private readonly List<TextBox> _inputs = new List<TextBox>();
        private readonly LogView _miniLog;

        public FeaturesForm(LogView sharedLog)
        {
            _miniLog = sharedLog;

            Text = "更多功能";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(980, 660);
            MinimumSize = new Size(820, 520);
            BackColor = Bg;
            Font = new Font("Microsoft YaHei UI", 9f);

            // ---------- 顶部 ----------
            var head = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.FromArgb(0x19, 0x1C, 0x20), Padding = new Padding(18, 0, 18, 0) };
            head.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Line), 0, head.Height - 1, head.Width, head.Height - 1);
            head.Controls.Add(new Label
            {
                Text = "更多功能",
                Font = new Font("Microsoft YaHei UI", 13f, FontStyle.Bold),
                ForeColor = TextMain,
                AutoSize = true,
                Location = new Point(18, 8)
            });
            head.Controls.Add(new Label
            {
                Text = "原程序全部 " + Commands.All.Count + " 条命令，选一条填参数即可执行",
                Font = new Font("Microsoft YaHei UI", 8.5f),
                ForeColor = TextSub,
                AutoSize = true,
                Location = new Point(20, 32)
            });

            // ---------- 左侧树 ----------
            var left = new Panel { Dock = DockStyle.Left, Width = 330, BackColor = Bg, Padding = new Padding(14, 12, 6, 14) };

            _tree = new TreeView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = Bg,
                Font = new Font("Microsoft YaHei UI", 9f),
                ForeColor = TextMain,
                ShowLines = false,
                FullRowSelect = true,
                HideSelection = false,
                Indent = 14
            };
            _tree.AfterSelect += (s, e) => ShowCmd(e.Node.Tag as CmdDef);
            left.Controls.Add(_tree);

            var searchBox = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(0, 0, 0, 8) };
            var tbSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Microsoft YaHei UI", 9f),
                ForeColor = TextSub,
                Text = "搜索命令 / 功能…"
            };
            tbSearch.GotFocus += (s, e) => { if (tbSearch.Text.StartsWith("搜索")) { tbSearch.Text = ""; tbSearch.ForeColor = TextMain; } };
            tbSearch.TextChanged += (s, e) =>
            {
                string q = tbSearch.Text.StartsWith("搜索") ? "" : tbSearch.Text.Trim();
                FillTree(q);
            };
            searchBox.Controls.Add(tbSearch);
            left.Controls.Add(searchBox);

            // ---------- 右侧 ----------
            var right = new Panel { Dock = DockStyle.Fill, BackColor = Bg, Padding = new Padding(6, 12, 14, 14) };

            // 底部：高级输入
            var rawBox = new Panel { Dock = DockStyle.Bottom, Height = 92, BackColor = Card, Padding = new Padding(12, 8, 12, 10) };
            rawBox.Paint += (s, e) =>
            {
                using (var p = new Pen(Line)) e.Graphics.DrawRectangle(p, 0, 0, rawBox.Width - 1, rawBox.Height - 1);
            };
            rawBox.Controls.Add(new Label
            {
                Text = "高级：直接发送命令（原版命令语法，↑↓ 翻历史）",
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font("Microsoft YaHei UI", 8.5f),
                ForeColor = TextSub
            });
            _txtRaw = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 10f)
            };
            _txtRaw.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { SendRaw(); e.SuppressKeyPress = true; }
            };
            rawBox.Controls.Add(_txtRaw);
            right.Controls.Add(rawBox);

            // 中部：参数表单
            var formCard = new Panel { Dock = DockStyle.Fill, BackColor = Card, Padding = new Padding(18, 14, 18, 14), AutoScroll = true };
            formCard.Paint += (s, e) =>
            {
                using (var p = new Pen(Line)) e.Graphics.DrawRectangle(p, 0, 0, formCard.Width - 1, formCard.Height - 1);
            };

            _lblTitle = new Label
            {
                Text = "选择左侧命令",
                Font = new Font("Microsoft YaHei UI", 12f, FontStyle.Bold),
                ForeColor = TextMain,
                Location = new Point(18, 14),
                AutoSize = true
            };
            _lblCmd = new Label
            {
                Text = "",
                Font = new Font("Consolas", 9f),
                ForeColor = TextSub,
                Location = new Point(20, 44),
                AutoSize = true
            };
            _lblHint = new Label
            {
                Text = "",
                Font = new Font("Microsoft YaHei UI", 8.5f),
                ForeColor = TextSub,
                Location = new Point(20, 66),
                Size = new Size(560, 18)
            };
            _formHost = new Panel
            {
                Location = new Point(18, 92),
                Size = new Size(560, 300),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _btnRun = new Button
            {
                Text = "执 行",
                Size = new Size(120, 36),
                Location = new Point(18, 400),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0x2A, 0x30, 0x36),
                ForeColor = Color.FromArgb(0x9A, 0xA2, 0xAC),
                Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnRun.FlatAppearance.BorderSize = 0;
            _btnRun.Click += (s, e) => RunCurrent();

            formCard.Controls.Add(_lblTitle);
            formCard.Controls.Add(_lblCmd);
            formCard.Controls.Add(_lblHint);
            formCard.Controls.Add(_formHost);
            formCard.Controls.Add(_btnRun);
            formCard.Resize += (s, e) =>
            {
                _formHost.Width = Math.Max(200, formCard.ClientSize.Width - 40);
                _btnRun.Top = _formHost.Bottom + 16;
            };
            right.Controls.Add(formCard);

            Controls.Add(right);
            Controls.Add(left);
            Controls.Add(head);

            FillTree("");
            Theme.Apply(this);
        }

        private void FillTree(string q)
        {
            _tree.BeginUpdate();
            try
            {
                _tree.Nodes.Clear();
                bool filtering = !string.IsNullOrWhiteSpace(q);
                string ql = filtering ? q.ToLowerInvariant() : "";

                foreach (var cat in Commands.CategoryOrder)
                {
                    var shown = new List<CmdDef>();
                    foreach (var d in Commands.ByCategory(cat))
                    {
                        if (!filtering
                            || d.Name.ToLowerInvariant().Contains(ql)
                            || d.Label.ToLowerInvariant().Contains(ql))
                            shown.Add(d);
                    }
                    if (shown.Count == 0) continue;

                    var node = new TreeNode(cat + "  (" + shown.Count + ")");
                    node.ForeColor = TextSub;
                    node.NodeFont = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold);
                    foreach (var d in shown)
                    {
                        var child = new TreeNode(d.Label) { Tag = d, ToolTipText = d.Name };
                        node.Nodes.Add(child);
                    }
                    _tree.Nodes.Add(node);
                    if (filtering) node.Expand();
                }
                if (!filtering && _tree.Nodes.Count > 1) _tree.Nodes[0].Expand();
            }
            finally
            {
                _tree.EndUpdate();
            }
        }

        private void ShowCmd(CmdDef def)
        {
            _cur = def;
            _formHost.Controls.Clear();
            _inputs.Clear();

            if (def == null)
            {
                _lblTitle.Text = "选择左侧命令";
                _lblCmd.Text = "";
                _lblHint.Text = "";
                _btnRun.BackColor = Color.FromArgb(0x2A, 0x30, 0x36);
                _btnRun.ForeColor = Color.FromArgb(0x9A, 0xA2, 0xAC);
                return;
            }

            _lblTitle.Text = def.Label;
            _lblCmd.Text = def.Name + "    ·    分类：" + def.Category;
            _lblHint.Text = def.Prompts.Length == 0
                ? "这条命令不需要参数，直接执行。"
                : "按顺序填写下面 " + def.Prompts.Length + " 项参数。";
            _btnRun.BackColor = Accent;
            _btnRun.ForeColor = Color.FromArgb(0xF2, 0xF4, 0xF6);

            int y = 0;
            for (int i = 0; i < def.Prompts.Length; i++)
            {
                string prompt = def.Prompts[i];
                bool multi = prompt.IndexOf("POST", StringComparison.OrdinalIgnoreCase) >= 0
                          || prompt.IndexOf("DATA", StringComparison.OrdinalIgnoreCase) >= 0
                          || prompt.IndexOf("message", StringComparison.OrdinalIgnoreCase) >= 0
                          || prompt.Contains("举报") || prompt.Contains("信息");
                int h = multi ? 70 : 24;

                var lb = new Label
                {
                    Text = (i + 1) + ". " + prompt,
                    ForeColor = Color.FromArgb(0xA0, 0xA6, 0xAD),
                    Location = new Point(0, y),
                    Size = new Size(_formHost.Width - 10, 18)
                };
                var tb = new TextBox
                {
                    Location = new Point(0, y + 20),
                    Size = new Size(_formHost.Width - 10, h),
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Consolas", 9.5f),
                    Multiline = multi,
                    ScrollBars = multi ? ScrollBars.Vertical : ScrollBars.None,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                _formHost.Controls.Add(lb);
                _formHost.Controls.Add(tb);
                _inputs.Add(tb);
                y += 20 + h + 10;
            }

            if (_inputs.Count > 0) _inputs[0].Focus();
        }

        private void RunCurrent()
        {
            if (_cur == null) { MessageBox.Show(this, "先在左侧选一条命令。", "ES Tool"); return; }
            if (!AppHost.Started)
            {
                MessageBox.Show(this, "还没进入控制台，请先在主界面登录。", "提示");
                return;
            }
            _miniLog.PushSystem("→ " + _cur.Name + (_inputs.Count > 0 ? "  （" + _inputs.Count + " 项参数）" : ""));
            Term.Submit(_cur.Name);
            foreach (var tb in _inputs) Term.Submit(tb.Text);
        }

        private void SendRaw()
        {
            string t = _txtRaw.Text.TrimEnd('\r', '\n');
            _txtRaw.Clear();
            if (t.Length == 0) return;
            if (!AppHost.Started)
            {
                MessageBox.Show(this, "还没进入控制台，请先在主界面登录。", "提示");
                return;
            }
            _miniLog.PushSystem("→ " + t);
            Term.Submit(t);
        }
    }
}
