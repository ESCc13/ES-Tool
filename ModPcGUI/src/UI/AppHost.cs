using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ConsoleAppLogin.NetEase;
using FeverToSauth;
using Login.NetEase;
using Login.NetEase.LoginotpList;
using MCStudio.Network.Http;
using Newtonsoft.Json;
using text;

namespace ModPcGUI
{
    /// <summary>把原程序的控制台流程封装成 GUI 可调用的宿主</summary>
    public static class AppHost
    {
        public static volatile bool Started;      // 已进入命令循环
        public static volatile bool LoggingIn;    // 正在登录
        public static string AccountName = "";
        public static string LastError = "";
        public static bool Initialized;

        public static event Action StateChanged;

        private static void Notify()
        {
            try { StateChanged?.Invoke(); } catch { }
        }

        /// <summary>首屏初始化：工作目录、原生库、config.json</summary>
        public static void Init()
        {
            if (Initialized) return;
            Initialized = true;

            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            try
            {
                string fpd;
                CoreNative.CanLaunch("", "", out fpd);
            }
            catch (Exception ex)
            {
                Term.WriteLine("[init] CoreNative.CanLaunch 失败：" + ex.Message);
            }

            try
            {
                string cfg = app.SimpleRead("config.json");
                app.Temp = JsonConvert.DeserializeObject<Login.temp>(cfg);
                Term.WriteLine("[init] 已加载 config.json  版本 " + app.Temp.Version
                               + "  服务器 " + app.Temp.url);
            }
            catch (Exception ex)
            {
                Term.WriteLine("[init] config.json 读取失败：" + ex.Message);
            }
        }

        public static string UserDir
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user"); }
        }

        public static List<string> ListAccounts()
        {
            var list = new List<string>();
            try
            {
                string dir = UserDir;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                foreach (var f in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
                    list.Add(f);
            }
            catch { }
            return list;
        }

        public static void AddAccount(string name, string cookie)
        {
            string dir = UserDir;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string safe = string.IsNullOrWhiteSpace(name) ? ("账号" + DateTime.Now.ToString("MMddHHmmss")) : name;
            foreach (char c in Path.GetInvalidFileNameChars())
                safe = safe.Replace(c, '_');
            string path = Path.Combine(dir, safe + ".json");
            File.WriteAllText(path, cookie ?? "", System.Text.Encoding.UTF8);
            Term.WriteLine("[账号] 已保存：" + path);
        }

        public static void DeleteAccount(string path)
        {
            try { File.Delete(path); Term.WriteLine("[账号] 已删除：" + Path.GetFileName(path)); }
            catch (Exception ex) { Term.WriteLine("[账号] 删除失败：" + ex.Message); }
        }

        /// <summary>登录并在后台进入命令循环（阻塞式，需在线程中调用）</summary>
        public static void Login(string accountFile, string mode)
        {
            if (LoggingIn || Started) return;
            LoggingIn = true;
            Notify();

            try
            {
                string content = app.ReadJson(accountFile);
                if (string.IsNullOrEmpty(content))
                    throw new Exception("账号文件为空：" + accountFile);

                bool pe;
                if (mode == "pe") pe = true;
                else if (mode == "pc") pe = false;
                else pe = content.TrimStart().StartsWith("ey") || content.StartsWith("SDK4399");

                string otp = content;
                if (content.StartsWith("ey"))
                {
                    Term.WriteLine("[账号] 检测到手机版 sauth，正在转换…");
                    otp = FeverAuth.FeverToSauth(content);
                }

                AccountName = Path.GetFileNameWithoutExtension(accountFile);
                Term.WriteLine("正在登录中，请稍等…  (" + AccountName + (pe ? " / 手机版" : " / 电脑版") + ")");

                app.PE_Login = pe;
                if (pe || app.Temp.is_g79) Http.PE_Login(otp);
                else Http.Login(otp);

                if (app.Temp != null && app.Temp.InitializeName)
                {
                    Term.WriteLine("[账号] 检测到需要初始化昵称…");
                    // 图形界面里没有控制台可敲昵称：挂个弹窗钩子。
                    // --legacy / 纯控制台模式 Application.Current 为空，保持 null，仍走 Term.ReadLine。
                    if (System.Windows.Application.Current != null)
                        text.app.NamePrompt = SetNameDialog.Ask;
                    try
                    {
                        app.Name();
                    }
                    finally
                    {
                        text.app.NamePrompt = null;
                    }
                }

                Function.GetName(app.player_info);
                Term.WriteLine("你好！冒险家：" + app.displayName);
                Term.WriteLine("输入 help 查看全部命令，或点击左侧命令直接执行");

                Started = true;
                LoggingIn = false;
                Notify();
                app.Command();
            }
            catch (ModPcFatalException ex)
            {
                LastError = ex.Message;
                Term.WriteLine("✗ " + ex.Message);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                Term.WriteLine("✗ 登录异常：" + ex.Message);
                Term.WriteLine(ex.StackTrace);
            }
            finally
            {
                LoggingIn = false;
                Notify();
            }
        }

        /// <summary>离线进入命令循环（等同原程序输入 of）</summary>
        public static void Offline()
        {
            if (Started) return;
            try
            {
                Term.WriteLine("[模式] 离线控制台：未登录，仅本地功能可用");
                Started = true;
                Notify();
                app.Command();
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                Term.WriteLine("✗ " + ex.Message);
            }
            finally
            {
                Started = false;
                Notify();
            }
        }

        public static void KickOff(Action body, string threadName)
        {
            var t = new Thread(() =>
            {
                try { body(); }
                catch (Exception ex)
                {
                    Term.WriteLine("✗ 线程异常：" + ex.Message);
                }
            });
            t.IsBackground = true;
            t.Name = threadName;
            t.Start();
        }
    }
}
