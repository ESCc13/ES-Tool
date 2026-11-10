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
        public void WriteLine(string s) { Log(Level.ERROR, s); }
        public void WriteLine() { Emit("", true, ConsoleColor.Red); }
        public void Write(object o) { Emit(o?.ToString() ?? "", false, ConsoleColor.Red); }
        public void WriteLine(object o) { Log(Level.ERROR, o == null ? "" : o.ToString()); }
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

    // ================= 日志分级 =================
    //
    // 输出格式统一为：
    //     时时-分分-秒秒 | INFO | 内容
    // 级别：INFO / SUCCESS / WARNING / ERROR / FATAL / DEBUG
    //
    // 说明：Term.WriteLine 是历史遗留的「所有业务输出都走这里」的入口（全项目几百处），
    // 所以级别在这里按内容自动判定，而不是逐个调用点改——改几百处必漏，漏一处格式就不齐。
    // 需要强制级别（尤其是带堆栈的错误）用 Term.Log / Term.Error / Term.Fatal。

    public enum Level { INFO, SUCCESS, WARNING, ERROR, FATAL, DEBUG }

    /// <summary>日志时间戳：时时-分分-秒秒</summary>
    public static string Stamp() { return DateTime.Now.ToString("HH-mm-ss"); }

    /// <summary>给一行文本套上「时间 | 级别 | 内容」的外壳；空行原样返回。</summary>
    public static string Wrap(Level lvl, string msg)
    {
        return Stamp() + " | " + lvl + " | " + (msg ?? "");
    }

    /// <summary>按内容猜级别：成功/失败/警告各自有固定符号，其余按关键词兜底。</summary>
    public static Level GuessLevel(string s)
    {
        if (string.IsNullOrEmpty(s)) return Level.INFO;
        bool hasFail = s.Contains("✗") || s.Contains("失败") || s.Contains("错误")
                       || s.Contains("异常") || s.Contains("无法") || s.Contains("出错")
                       || s.Contains("Exception") || s.Contains("Error");
        bool hasOk = s.Contains("✓") || s.Contains("成功");
        if (hasFail) return Level.ERROR;
        if (hasOk) return Level.SUCCESS;
        if (s.Contains("⚠") || s.Contains("警告") || s.Contains("注意")) return Level.WARNING;
        if (s.Contains("[DEBUG]") || s.Contains("[dbg]")) return Level.DEBUG;
        return Level.INFO;
    }

    /// <summary>级别对应的显示颜色。</summary>
    public static ConsoleColor ColorOf(Level lvl)
    {
        switch (lvl)
        {
            case Level.SUCCESS: return ConsoleColor.Green;
            case Level.WARNING: return ConsoleColor.Yellow;
            case Level.ERROR: return ConsoleColor.Red;
            case Level.FATAL: return ConsoleColor.Magenta;
            case Level.DEBUG: return ConsoleColor.DarkGray;
            default: return ConsoleColor.Gray;
        }
    }

    /// <summary>
    /// 输出一行日志（强制级别）。多行文本会逐行加上外壳，保证堆栈每一行都带时间与级别。
    /// </summary>
    public static void Log(Level lvl, string msg)
    {
        if (msg == null) msg = "";
        string[] lines = msg.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        foreach (string line in lines)
        {
            Emit(line.Length == 0 ? "" : Wrap(lvl, line), true, ColorOf(lvl));
        }
    }

    public static void Info(string msg) { Log(Level.INFO, msg); }
    public static void Success(string msg) { Log(Level.SUCCESS, msg); }
    public static void Warn(string msg) { Log(Level.WARNING, msg); }
    public static void Debug(string msg) { Log(Level.DEBUG, msg); }
    public static void Err(string msg) { Log(Level.ERROR, msg); }

    /// <summary>错误 + 完整堆栈（类型、消息、StackTrace、InnerException 全给）。</summary>
    public static void Err(string msg, Exception ex)
    {
        Log(Level.ERROR, msg);
        if (ex != null) Log(Level.ERROR, ex.ToString());
    }

    public static void Fatal(string msg) { Log(Level.FATAL, msg); }

    public static void Fatal(string msg, Exception ex)
    {
        Log(Level.FATAL, msg);
        if (ex != null) Log(Level.FATAL, ex.ToString());
    }

    /// <summary>WriteLine 的统一出口：空行照旧，其余按内容分级并加上时间/级别外壳。</summary>
    private static void LogLineAuto(string s)
    {
        if (s == null) s = "";
        if (s.Length == 0) { Emit("", true, Cur()); return; }
        Log(GuessLevel(s), s);
    }

    private static readonly System.Text.RegularExpressions.Regex _stamped =
        new System.Text.RegularExpressions.Regex(
            @"^\d{2}-\d{2}-\d{2} \| (INFO|SUCCESS|WARNING|ERROR|FATAL|DEBUG) \| ",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    /// 把一段可能多行的文本整成统一日志格式；已经带外壳的行原样保留。
    /// GUI 里自己拼的日志（AppendLog）走这里，保证和 Term 输出一个样。
    /// </summary>
    public static string FormatBlock(string s)
    {
        if (string.IsNullOrEmpty(s)) return s ?? "";
        string norm = s.Replace("\r\n", "\n").Replace("\r", "\n");
        bool tailNl = norm.EndsWith("\n");
        string[] parts = norm.Split('\n');
        int n = parts.Length - (tailNl ? 1 : 0);
        var sb = new StringBuilder();
        for (int i = 0; i < n; i++)
        {
            string line = parts[i];
            if (line.Length == 0) { sb.Append("\r\n"); continue; }
            if (_stamped.IsMatch(line)) sb.Append(line);
            else sb.Append(Wrap(GuessLevel(line), line));
            sb.Append("\r\n");
        }
        return sb.ToString();
    }

    public static void Write(string s) { Emit(s, false, Cur()); }
    public static void Write(object o) { Emit(o == null ? "" : o.ToString(), false, Cur()); }
    public static void Write(char c) { Emit(c.ToString(), false, Cur()); }
    public static void Write(int i) { Emit(i.ToString(), false, Cur()); }
    public static void WriteLine() { Emit("", true, Cur()); }
    public static void WriteLine(string s) { LogLineAuto(s); }
    public static void WriteLine(object o) { LogLineAuto(o == null ? "" : o.ToString()); }
    public static void WriteLine(char c) { LogLineAuto(c.ToString()); }
    public static void WriteLine(int i) { LogLineAuto(i.ToString()); }
    public static void WriteLine(long i) { LogLineAuto(i.ToString()); }
    public static void WriteLine(bool b) { LogLineAuto(b.ToString()); }
    public static void WriteLine(double d) { LogLineAuto(d.ToString()); }
    public static void WriteLine(string fmt, params object[] args)
    {
        string s;
        try { s = string.Format(fmt, args); } catch { s = fmt; }
        LogLineAuto(s);
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

    /// <summary>
    /// 丢掉最多 count 行待处理输入，返回实际丢掉的行数。
    /// 用于「命令还没读参数就提前退出」的分支：GUI 已经把整串参数投进队列，
    /// 不消费就会串到后面那条命令上，表现成「点了按钮没反应、日志还一切正常」。
    /// 首行最多等 waitMs 毫秒（GUI 的多次 Submit 之间可能有时差），之后不再等待。
    /// </summary>
    public static int Discard(int count, int waitMs)
    {
        int got = 0;
        while (got < count)
        {
            string s;
            if (_queue.TryTake(out s, got == 0 ? Math.Max(0, waitMs) : 0)) got++;
            else break;
        }
        return got;
    }
}
