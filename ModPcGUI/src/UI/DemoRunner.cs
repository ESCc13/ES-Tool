using System.Threading;

namespace ModPcGUI
{
    /// <summary>--demo：自动进入离线控制台并跑两条命令，用于快速验证链路</summary>
    internal static class DemoRunner
    {
        public static void Run()
        {
            var t = new Thread(() =>
            {
                Thread.Sleep(1200);
                Term.WriteLine("[demo] 自动进入离线控制台", System.ConsoleColor.DarkCyan);
                AppHost.KickOff(AppHost.Offline, "offline");

                Thread.Sleep(1600);
                Term.WriteLine("[demo] 发送 version", System.ConsoleColor.DarkCyan);
                Term.Submit("version");

                Thread.Sleep(900);
                Term.WriteLine("[demo] 发送 help", System.ConsoleColor.DarkCyan);
                Term.Submit("help");

                // 验证「带参数命令」链路：title 需要 1 个参数
                Thread.Sleep(900);
                Term.WriteLine("[demo] 发送 title + 参数", System.ConsoleColor.DarkCyan);
                Term.Submit("title");
                Term.Submit("ES Tool");
            });
            t.IsBackground = true;
            t.Name = "demo";
            t.Start();
        }
    }
}
