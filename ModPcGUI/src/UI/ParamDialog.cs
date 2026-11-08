using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ModPcGUI
{
    /// <summary>根据命令参数提示自动生成输入表单</summary>
    public sealed class ParamDialog : Form
    {
        private readonly List<TextBox> _boxes = new List<TextBox>();
        public string[] Values { get; private set; }

        private static readonly Dictionary<string, string> LastValues = new Dictionary<string, string>();

        public ParamDialog(CmdDef def)
        {
            Text = def.Label + "  —  " + def.Name;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(0x14, 0x16, 0x19);
            Font = new Font("Microsoft YaHei UI", 9f);
            ClientSize = new Size(520, 150);

            var head = new Label
            {
                Text = def.Label,
                Font = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0xE9, 0xEC, 0xEF),
                AutoSize = true,
                Location = new Point(18, 14)
            };
            Controls.Add(head);

            var sub = new Label
            {
                Text = "命令：" + def.Name + "    参数 " + def.Prompts.Length + " 项",
                Font = new Font("Consolas", 8.75f),
                ForeColor = Color.FromArgb(0x6E, 0x76, 0x80),
                AutoSize = true,
                Location = new Point(18, 40)
            };
            Controls.Add(sub);

            int y = 68;
            for (int i = 0; i < def.Prompts.Length; i++)
            {
                string prompt = def.Prompts[i];
                bool multi = LooksMultiline(prompt);
                int h = multi ? 76 : 26;

                var lb = new Label
                {
                    Text = prompt,
                    ForeColor = Color.FromArgb(0xA0, 0xA6, 0xAD),
                    AutoSize = false,
                    Location = new Point(18, y),
                    Size = new Size(ClientSize.Width - 40, 18)
                };
                Controls.Add(lb);

                var tb = new TextBox
                {
                    Location = new Point(18, y + 20),
                    Size = new Size(ClientSize.Width - 40, h),
                    Multiline = multi,
                    ScrollBars = multi ? ScrollBars.Vertical : ScrollBars.None,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Consolas", 9.5f),
                    Text = Lookup(def.Name, i)
                };
                if (multi) tb.AcceptsReturn = true;
                Controls.Add(tb);
                _boxes.Add(tb);

                y += 20 + h + 10;
            }

            int bottom = y + 6;
            ClientSize = new Size(520, bottom + 48);

            var ok = new Button
            {
                Text = "执行",
                DialogResult = DialogResult.OK,
                Location = new Point(ClientSize.Width - 190, bottom + 8),
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0x4C, 0x8D, 0xFF),
                ForeColor = Color.FromArgb(0xF2, 0xF4, 0xF6)
            };
            ok.FlatAppearance.BorderSize = 0;
            Controls.Add(ok);

            var cancel = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new Point(ClientSize.Width - 100, bottom + 8),
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0x24, 0x28, 0x2D)
            };
            cancel.FlatAppearance.BorderSize = 0;
            Controls.Add(cancel);

            Theme.Apply(this);
            AcceptButton = ok;
            CancelButton = cancel;

            ok.Click += (s, e) =>
            {
                Values = new string[_boxes.Count];
                for (int i = 0; i < _boxes.Count; i++)
                {
                    Values[i] = _boxes[i].Text;
                    LastValues[def.Name + "#" + i] = _boxes[i].Text;
                }
            };

            if (_boxes.Count > 0)
            {
                Shown += (s, e) => { _boxes[0].Focus(); _boxes[0].SelectAll(); };
            }
        }

        private static string Lookup(string cmd, int idx)
        {
            string v;
            return LastValues.TryGetValue(cmd + "#" + idx, out v) ? v : "";
        }

        private static bool LooksMultiline(string prompt)
        {
            if (string.IsNullOrEmpty(prompt)) return false;
            string p = prompt.ToLowerInvariant();
            return p.Contains("post") || p.Contains("data") || p.Contains("json")
                   || p.Contains("message") || p.Contains("举报") || p.Contains("信息")
                   || p.Contains("参数") && prompt.Length > 6;
        }
    }
}
