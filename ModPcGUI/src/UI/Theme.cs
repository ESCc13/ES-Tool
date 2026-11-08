using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ModPcGUI
{
    /// <summary>ES Tool 深色主题：单一强调色、细边框、无装饰</summary>
    public static class Theme
    {
        public static readonly Color Bg = Color.FromArgb(0x11, 0x13, 0x16);
        public static readonly Color Card = Color.FromArgb(0x19, 0x1C, 0x20);
        public static readonly Color Input = Color.FromArgb(0x0E, 0x10, 0x12);
        public static readonly Color Line = Color.FromArgb(0x2B, 0x2F, 0x34);
        public static readonly Color TextMain = Color.FromArgb(0xE9, 0xEC, 0xEF);
        public static readonly Color TextSub = Color.FromArgb(0x8B, 0x92, 0x9B);
        public static readonly Color Accent = Color.FromArgb(0x4C, 0x8D, 0xFF);
        public static readonly Color Green = Color.FromArgb(0x2E, 0x9E, 0x63);
        public static readonly Color Soft = Color.FromArgb(0x24, 0x28, 0x2D);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        /// <summary>让系统标题栏也变深色（Win10 1809+，老系统自动忽略）</summary>
        public static void DarkTitleBar(IntPtr h)
        {
            if (h == IntPtr.Zero) return;
            int v = 1;
            try { DwmSetWindowAttribute(h, 20, ref v, sizeof(int)); } catch { }
            try { DwmSetWindowAttribute(h, 19, ref v, sizeof(int)); } catch { }
        }

        private static void ComboDraw(object sender, DrawItemEventArgs e)
        {
            var cb = sender as ComboBox;
            if (cb == null || e.Index < 0) return;
            bool sel = (e.State & DrawItemState.Selected) != 0;
            using (var bg = new SolidBrush(sel ? Color.FromArgb(0x2A, 0x35, 0x48) : Input))
                e.Graphics.FillRectangle(bg, e.Bounds);
            string t = cb.GetItemText(cb.Items[e.Index]);
            using (var fg = new SolidBrush(sel ? Accent : TextMain))
                e.Graphics.DrawString(t, cb.Font, fg, e.Bounds.Left + 5, e.Bounds.Top + 2);
        }

        /// <summary>
        /// 递归统一输入类控件的深色外观。
        /// Button / Label / Panel 保留各自的显式配色，不在这里覆盖。
        /// </summary>
        public static void Apply(Control root)
        {
            if (root == null) return;

            var form = root as Form;
            if (form != null)
            {
                form.BackColor = Bg;
                form.ForeColor = TextMain;
                form.HandleCreated += (s, e) => DarkTitleBar(((Form)s).Handle);
                if (form.IsHandleCreated) DarkTitleBar(form.Handle);
            }

            var tb = root as TextBox;
            if (tb != null)
            {
                tb.BackColor = Input;
                tb.ForeColor = TextMain;
                if (tb.BorderStyle == BorderStyle.Fixed3D) tb.BorderStyle = BorderStyle.FixedSingle;
            }

            var cb = root as ComboBox;
            if (cb != null)
            {
                cb.BackColor = Input;
                cb.ForeColor = TextMain;
                cb.FlatStyle = FlatStyle.Flat;
                if (cb.DropDownStyle == ComboBoxStyle.DropDownList
                    && cb.DrawMode != DrawMode.OwnerDrawFixed)
                {
                    cb.DrawMode = DrawMode.OwnerDrawFixed;
                    cb.ItemHeight = 20;
                    cb.DrawItem += ComboDraw;
                }
            }

            var rt = root as RichTextBox;
            if (rt != null)
            {
                rt.BackColor = Card;
                rt.ForeColor = TextMain;
            }

            var tree = root as TreeView;
            if (tree != null)
            {
                tree.BackColor = Bg;
                tree.ForeColor = TextMain;
                tree.LineColor = Line;
            }

            var chk = root as CheckBox;
            if (chk != null) { chk.ForeColor = TextMain; chk.FlatStyle = FlatStyle.Flat; }

            var radio = root as RadioButton;
            if (radio != null) { radio.ForeColor = TextMain; radio.FlatStyle = FlatStyle.Flat; }

            var ss = root as StatusStrip;
            if (ss != null) ss.BackColor = Card;

            foreach (Control c in root.Controls) Apply(c);
        }
    }
}
