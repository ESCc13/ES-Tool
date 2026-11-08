using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;

/// <summary>
/// 控制台门面：把所有原本走 Console 的输入输出重定向到 GUI。
/// 全局命名空间，任何命名空间下都能直接访问 Term.Xxx。
/// </summary>
public static class Term
{
    // ---- 事件（由 GUI 订阅，可能在后台线程触发） ----
    /// <summary>(文本, 是否换行, 颜色)</summary>
    public static event Action<string, bool, ConsoleColor> OnOutput;
    public static event Action OnClear;
    public static event Action<string> OnTitle;
    /// <summary>等待用户输入状态变化</summary>
    public static event Action<bool> OnWaiting;

    // ---- 输入队列 ----
    private static readonly BlockingCollection<string> _queue = new BlockingCollection<string>();
    private static volatile bool _waiting;

    private static readonly object _lock = new object();
    private static ConsoleColor _color = ConsoleColor.Gray;
    private static string _title = "ES Tool";

    public static bool IsWaiting { get { return _waiting; } }

    // ---- 兼容 Console 的属性 ----
    public static ConsoleColor ForegroundColor
    {
        get { lock (_lock) { return _color; } }
        set { lock (_lock) { _color = value; } }
    }

    public static ConsoleColor BackgroundColor { get; set; } = ConsoleColor.Black;

    public static string Title
    {
        get { return _title; }
        set
        {
            _title = value;
            try { OnTitle?.Invoke(value); } catch { }
        }
    }

    /// <summary>兼容 Console.Error.WriteLine(...)</summary>
    public static readonly ErrorSink Error = new ErrorSink();

    public sealed class ErrorSink
    {
        public void Write(string s) { Emit(s, false, ConsoleColor.Red); }
        public void WriteLine(string s) { Emit(s, true, ConsoleColor.Red); }
        public void WriteLine() { Emit("", true, ConsoleColor.Red); }
        public void Write(object o) { Emit(o?.ToString() ?? "", false, ConsoleColor.Red); }
        public void WriteLine(object o) { Emit(o?.ToString() ?? "", true, ConsoleColor.Red); }
    }

    // ---- 输出 ----
    private static readonly StringBuilder _line = new StringBuilder();

    /// <summary>当前尚未换行的那一行文本（通常就是等待输入的提示语）</summary>
    public static string LastPrompt
    {
        get { lock (_line) { return _line.ToString(); } }
    }

    private static void Emit(string s, bool newline, ConsoleColor color)
    {
        lock (_line)
        {
            if (newline) _line.Clear();
            else _line.Append(s ?? "");
        }
        try { OnOutput?.Invoke(s ?? "", newline, color); } catch { }
    }

    private static ConsoleColor Cur() { lock (_lock) { return _color; } }

    public static void Write(string s) { Emit(s, false, Cur()); }
    public static void Write(object o) { Emit(o == null ? "" : o.ToString(), false, Cur()); }
    public static void Write(char c) { Emit(c.ToString(), false, Cur()); }
    public static void Write(int i) { Emit(i.ToString(), false, Cur()); }
    public static void WriteLine() { Emit("", true, Cur()); }
    public static void WriteLine(string s) { Emit(s, true, Cur()); }
    public static void WriteLine(object o) { Emit(o == null ? "" : o.ToString(), true, Cur()); }
    public static void WriteLine(char c) { Emit(c.ToString(), true, Cur()); }
    public static void WriteLine(int i) { Emit(i.ToString(), true, Cur()); }
    public static void WriteLine(long i) { Emit(i.ToString(), true, Cur()); }
    public static void WriteLine(bool b) { Emit(b.ToString(), true, Cur()); }
    public static void WriteLine(double d) { Emit(d.ToString(), true, Cur()); }
    public static void WriteLine(string fmt, params object[] args)
    {
        string s;
        try { s = string.Format(fmt, args); } catch { s = fmt; }
        Emit(s, true, Cur());
    }
    public static void Write(string fmt, params object[] args)
    {
        string s;
        try { s = string.Format(fmt, args); } catch { s = fmt; }
        Emit(s, false, Cur());
    }

    public static void Clear()
    {
        try { OnClear?.Invoke(); } catch { }
    }

    // ---- 输入 ----
    /// <summary>GUI 或脚本投递一行输入</summary>
    public static void Submit(string line)
    {
        _queue.Add(line ?? "");
    }

    public static string ReadLine()
    {
        _waiting = true;
        try { OnWaiting?.Invoke(true); } catch { }
        string s;
        try
        {
            s = _queue.Take();
        }
        finally
        {
            _waiting = false;
            try { OnWaiting?.Invoke(false); } catch { }
        }
        // 回显，让日志像终端
        Emit(s, true, ConsoleColor.Cyan);
        return s;
    }

    public static ConsoleKeyInfo ReadKey()
    {
        _waiting = true;
        try { OnWaiting?.Invoke(true); } catch { }
        try
        {
            string s = _queue.Take();
            return new ConsoleKeyInfo(
                string.IsNullOrEmpty(s) ? ' ' : s[0],
                ConsoleKey.Enter, false, false, false);
        }
        finally
        {
            _waiting = false;
            try { OnWaiting?.Invoke(false); } catch { }
        }
    }

    public static ConsoleKeyInfo ReadKey(bool intercept) { return ReadKey(); }

    /// <summary>清空待处理输入（切换流程时用）</summary>
    public static void ClearInput()
    {
        while (_queue.TryTake(out _)) { }
    }
}
