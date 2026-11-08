using System;
using System.Drawing;
using System.Windows.Forms;

namespace ModPcGUI
{
    public sealed class AddAccountDialog : Form
    {
        private TextBox _name;
        private TextBox _cookie;

        public string AccountName { get { return _name.Text.Trim(); } }
        public string Cookie { get { return _cookie.Text.Trim(); } }

        public AddAccountDialog()
        {
            Text = "添加账号";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(560, 400);
            BackColor = Color.FromArgb(0x14, 0x16, 0x19);
            Font = new Font("Microsoft YaHei UI", 9f);

            var l1 = new Label { Text = "账号名称", ForeColor = Color.FromArgb(0xA0, 0xA6, 0xAD), Location = new Point(18, 16), AutoSize = true };
            _name = new TextBox
            {
                Location = new Point(18, 38),
                Size = new Size(ClientSize.Width - 36, 24),
                BorderStyle = BorderStyle.FixedSingle,
                Text = "账号" + DateTime.Now.ToString("MMdd")
            };

            var l2 = new Label
            {
                Text = "Sauth / Cookie（电脑版填 {json}，手机版填 ey 开头的 sauth，4399 填 SDK4399 开头）",
                ForeColor = Color.FromArgb(0xA0, 0xA6, 0xAD),
                Location = new Point(18, 74),
                AutoSize = false,
                Size = new Size(ClientSize.Width - 36, 20)
            };
            _cookie = new TextBox
            {
                Location = new Point(18, 96),
                Size = new Size(ClientSize.Width - 36, 240),
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9f),
                AcceptsReturn = true
            };

            var ok = new Button
            {
                Text = "保存",
                DialogResult = DialogResult.OK,
                Location = new Point(ClientSize.Width - 190, 350),
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0x4C, 0x8D, 0xFF),
                ForeColor = Color.FromArgb(0xF2, 0xF4, 0xF6)
            };
            ok.FlatAppearance.BorderSize = 0;
            ok.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_cookie.Text))
                {
                    MessageBox.Show(this, "Cookie 不能为空", "提示");
                    DialogResult = DialogResult.None;
                }
            };

            var cancel = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new Point(ClientSize.Width - 100, 350),
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0x24, 0x28, 0x2D)
            };
            cancel.FlatAppearance.BorderSize = 0;

            Controls.AddRange(new Control[] { l1, _name, l2, _cookie, ok, cancel });
            Theme.Apply(this);
            AcceptButton = ok;
            CancelButton = cancel;
            Shown += (s, e) => _cookie.Focus();
        }
    }
}
