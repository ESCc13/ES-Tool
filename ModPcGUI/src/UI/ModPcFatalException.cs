using System;

/// <summary>原程序里 Environment.Exit 的位置改为抛此异常，由 GUI 捕获提示。
/// 放在全局命名空间，方便被各个老命名空间下的代码直接引用。</summary>
public sealed class ModPcFatalException : Exception
{
    public ModPcFatalException(string msg) : base(msg) { }
}
