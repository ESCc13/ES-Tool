using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ModPcGUI
{
    /// <summary>带缓冲队列的日志输出控件（ES Tool 深色主题）</summary>
    public sealed class LogView : RichTextBox
    {
        private struct Entry
        {
            public string Text;
            public bool NewLine;
            public Color Color;
        }

        private readonly ConcurrentQueue<Entry> _buf = new ConcurrentQueue<Entry>();
        private readonly Timer _flush;
        private bool _autoScroll = true;
        private readonly object _lineLock = new object();
        private bool _lineHasText;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
        private const int WM_SETREDRAW = 0x000B;

        public LogView()
        {
            ReadOnly = true;
            BorderStyle = BorderStyle.None;
            BackColor = Theme.Card;
            ForeColor = Theme.TextMain;
            Font = new Font("Consolas", 9.75f, FontStyle.Regular, GraphicsUnit.Point);
            WordWrap = true;
            DetectUrls = false;
            HideSelection = false;
            ScrollBars = RichTextBoxScrollBars.Vertical;
            _flush = new Timer { Interval = 60 };
            _flush.Tick += (s, e) => Flush();
            _flush.Start();
        }

        public void Push(string text, bool newline, ConsoleColor color)
        {
            if (text == null) text = "";
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            lock (_lineLock)
            {
                if (text.Length > 0) _lineHasText = true;
                if (newline) _lineHasText = false;
            }
            _buf.Enqueue(new Entry { Text = text, NewLine = newline, Color = Map(color) });
        }

        public void PushLine(string text, ConsoleColor color = ConsoleColor.Gray)
        {
            Push(text, true, color);
        }

        public void PushSystem(string text)
        {
            bool needBreak;
            lock (_lineLock) { needBreak = _lineHasText; }
            if (needBreak) Push("", true, ConsoleColor.Gray);
            Push("› " + text, true, ConsoleColor.Cyan);
        }

        private void Flush()
        {
            if (_buf.IsEmpty) return;
            if (!IsHandleCreated) return;

            SendMessage(Handle, WM_SETREDRAW, (IntPtr)0, IntPtr.Zero);
            try
            {
                int guard = 0;
                Entry e;
                while (_buf.TryDequeue(out e) && guard++ < 4000)
                {
                    if (e.Text.Length == 0 && !e.NewLine) continue;
                    SelectionStart = TextLength;
                    SelectionLength = 0;
                    SelectionColor = e.Color;
                    SelectionFont = Font;
                    AppendText(e.Text);
                    if (e.NewLine) AppendText("\n");
                }
                TrimLines();
            }
            finally
            {
                SendMessage(Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
                Invalidate();
            }

            if (_autoScroll)
            {
                SelectionStart = TextLength;
                ScrollToCaret();
            }
        }

        private void TrimLines()
        {
            if (Lines.Length > 4000)
            {
                int cut = TextLength / 3;
                Select(0, cut);
                SelectedText = "";
            }
        }

        public void ClearLog()
        {
            Entry tmp;
            while (_buf.TryDequeue(out tmp)) { }
            Clear();
        }

        /// <summary>ConsoleColor → 深色背景下的可读颜色</summary>
        private static Color Map(ConsoleColor c)
        {
            switch (c)
            {
                case ConsoleColor.Black: return Color.FromArgb(0xC9, 0xCE, 0xD4);
                case ConsoleColor.DarkBlue: return Color.FromArgb(0x6E, 0x9B, 0xFF);
                case ConsoleColor.DarkGreen: return Color.FromArgb(0x4A, 0xDE, 0x80);
                case ConsoleColor.DarkCyan: return Color.FromArgb(0x4F, 0xD1, 0xC5);
                case ConsoleColor.DarkRed: return Color.FromArgb(0xFF, 0x80, 0x80);
                case ConsoleColor.DarkMagenta: return Color.FromArgb(0xD6, 0x8B, 0xFF);
                case ConsoleColor.DarkYellow: return Color.FromArgb(0xE8, 0xC5, 0x5A);
                case ConsoleColor.Gray: return Theme.TextMain;
                case ConsoleColor.DarkGray: return Color.FromArgb(0x7A, 0x82, 0x8C);
                case ConsoleColor.Blue: return Color.FromArgb(0x6E, 0x9B, 0xFF);
                case ConsoleColor.Green: return Color.FromArgb(0x4A, 0xDE, 0x80);
                case ConsoleColor.Cyan: return Color.FromArgb(0x4F, 0xD1, 0xC5);
                case ConsoleColor.Red: return Color.FromArgb(0xFF, 0x6B, 0x6B);
                case ConsoleColor.Magenta: return Color.FromArgb(0xD6, 0x8B, 0xFF);
                case ConsoleColor.Yellow: return Color.FromArgb(0xF0, 0xC4, 0x4C);
                case ConsoleColor.White: return Color.FromArgb(0xF2, 0xF4, 0xF6);
                default: return Theme.TextMain;
            }
        }
    }
}
