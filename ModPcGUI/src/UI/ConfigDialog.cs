using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace ModPcGUI
{
    /// <summary>config.json 可视化编辑</summary>
    public sealed class ConfigDialog : Form
    {
        private JObject _root;
        private readonly Dictionary<string, Control> _fields = new Dictionary<string, Control>();
        private readonly string _path;
        public bool Saved { get; private set; }

        private sealed class Spec
        {
            public string Key, Label, Kind;
            public Spec(string k, string l, string d) { Key = k; Label = l; Kind = d; }
        }

        private static readonly Spec[] Specs =
        {
            new Spec("path", "游戏路径（x64_mc 目录）", "text"),
            new Spec("Version", "客户端版本号", "text"),
            new Spec("url", "PC 认证服务器", "text"),
            new Spec("PE_url", "PE 认证服务器", "text"),
            new Spec("engineVersion", "引擎版本 engineVersion", "text"),
            new Spec("patchVersion", "补丁版本 patchVersion", "text"),
            new Spec("patch", "补丁哈希 patch", "text"),
            new Spec("libminecraftpe", "libminecraftpe", "text"),
            new Spec("offset", "offset", "int"),
            new Spec("rounds", "rounds", "int"),
            new Spec("item_ids", "Item ID 列表（逗号分隔）", "list"),
            new Spec("ban_id", "封禁 ID 列表（逗号分隔）", "list"),
            new Spec("InitializeName", "启动时初始化昵称", "bool"),
            new Spec("is_g79", "使用 PE 服务器（is_g79）", "bool"),
            new Spec("connect_log", "输出连接日志", "bool"),
            new Spec("link_connect", "启用 Link 连接", "bool"),
            new Spec("chat_connect", "启用聊天连接", "bool"),
        };

        public ConfigDialog()
        {
            Text = "设置 · config.json";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            ClientSize = new Size(620, 620);
            MinimumSize = new Size(520, 420);
            BackColor = Color.FromArgb(0x14, 0x16, 0x19);
            Font = new Font("Microsoft YaHei UI", 9f);

            _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

            var head = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.FromArgb(0x19, 0x1C, 0x20), Padding = new Padding(18, 10, 18, 8) };
            head.Controls.Add(new Label
            {
                Text = "config.json",
                Font = new Font("Microsoft YaHei UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0xE9, 0xEC, 0xEF),
                AutoSize = true,
                Location = new Point(18, 8)
            });
            head.Controls.Add(new Label
            {
                Text = _path,
                Font = new Font("Consolas", 8f),
                ForeColor = Color.FromArgb(0x6E, 0x76, 0x80),
                AutoSize = true,
                Location = new Point(20, 32)
            });

            var body = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(18, 12, 18, 12) };
            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.FromArgb(0x19, 0x1C, 0x20), Padding = new Padding(18, 10, 18, 10) };

            int y = 8;
            foreach (var sp in Specs)
            {
                var lb = new Label
                {
                    Text = sp.Label,
                    ForeColor = Color.FromArgb(0xA0, 0xA6, 0xAD),
                    Location = new Point(18, y),
                    Size = new Size(ClientSize.Width - 80, 18)
                };
                body.Controls.Add(lb);

                Control ctl;
                if (sp.Kind == "bool")
                {
                    var cb = new CheckBox
                    {
                        Location = new Point(20, y + 20),
                        Size = new Size(60, 22),
                        Text = "开启"
                    };
                    ctl = cb;
                    y += 20 + 22 + 10;
                }
                else
                {
                    var tb = new TextBox
                    {
                        Location = new Point(18, y + 20),
                        Size = new Size(ClientSize.Width - 60, 24),
                        BorderStyle = BorderStyle.FixedSingle,
                        Font = new Font("Consolas", 9.5f)
                    };
                    ctl = tb;
                    y += 20 + 24 + 12;
                }
                ctl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                body.Controls.Add(ctl);
                _fields[sp.Key] = ctl;
            }

            var save = new Button
            {
                Text = "保存",
                Dock = DockStyle.Right,
                Width = 96,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0x4C, 0x8D, 0xFF),
                ForeColor = Color.FromArgb(0xF2, 0xF4, 0xF6),
                Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold)
            };
            save.FlatAppearance.BorderSize = 0;
            save.Click += (s, e) => DoSave();

            var close = new Button
            {
                Text = "关闭",
                Dock = DockStyle.Right,
                Width = 86,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0x24, 0x28, 0x2D)
            };
            close.FlatAppearance.BorderSize = 0;
            close.Click += (s, e) => Close();

            bottom.Controls.Add(save);
            bottom.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 8 });
            bottom.Controls.Add(close);

            Controls.Add(body);
            Controls.Add(bottom);
            Controls.Add(head);

            Theme.Apply(this);
            Load += (s, e) => LoadConfig();
        }

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(_path))
                {
                    MessageBox.Show(this, "找不到 config.json：" + _path, "提示");
                    return;
                }
                _root = JObject.Parse(File.ReadAllText(_path));
                foreach (var sp in Specs)
                {
                    JToken tok = _root[sp.Key];
                    Control ctl;
                    if (!_fields.TryGetValue(sp.Key, out ctl)) continue;

                    if (ctl is CheckBox)
                    {
                        bool b = false;
                        try { b = tok != null && tok.Type == JTokenType.Boolean && (bool)tok; } catch { }
                        ((CheckBox)ctl).Checked = b;
                    }
                    else
                    {
                        string v = "";
                        if (tok != null)
                        {
                            if (tok.Type == JTokenType.Array)
                            {
                                var parts = new List<string>();
                                foreach (var t in tok) parts.Add(t.ToString());
                                v = string.Join(",", parts);
                            }
                            else v = tok.ToString();
                        }
                        ((TextBox)ctl).Text = v;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "读取失败：" + ex.Message, "错误");
            }
        }

        private void DoSave()
        {
            try
            {
                foreach (var sp in Specs)
                {
                    Control ctl;
                    if (!_fields.TryGetValue(sp.Key, out ctl)) continue;

                    if (ctl is CheckBox)
                    {
                        _root[sp.Key] = ((CheckBox)ctl).Checked;
                    }
                    else
                    {
                        string v = ((TextBox)ctl).Text.Trim();
                        if (sp.Kind == "list")
                        {
                            var arr = new JArray();
                            foreach (var p in v.Split(new[] { ',', '，', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                                arr.Add(p.Trim());
                            _root[sp.Key] = arr;
                        }
                        else if (sp.Kind == "int")
                        {
                            int n;
                            _root[sp.Key] = int.TryParse(v, out n) ? n : 0;
                        }
                        else
                        {
                            _root[sp.Key] = v;
                        }
                    }
                }

                if (File.Exists(_path))
                    File.Copy(_path, _path + ".bak", true);

                File.WriteAllText(_path, _root.ToString(Newtonsoft.Json.Formatting.None),
                    new System.Text.UTF8Encoding(false));

                Saved = true;
                MessageBox.Show(this, "已保存。\r\n（原文件备份为 config.json.bak）", "完成");
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "保存失败：" + ex.Message, "错误");
            }
        }
    }
}
