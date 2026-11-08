using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ModPcGUI.PeAuth
{
    /// <summary>
    /// PE（手游）身份认证：sauth → /pe-authentication → PE entity_id + token。
    /// 逻辑照搬「房间IP查询-GUI界面版-20261108」NetEaseLogin.LoginPEAsync，
    /// 加密/签名用同包的 PeCrypto（与 Go 版逐字节交叉验证过）。
    /// PE 房间的购买、进房、取地址都必须用这个身份，PC 会话身份一律被服务端拒绝（12/16）。
    /// </summary>
    internal static class PeAuth
    {
        private const string CoreHostPE = "https://g79obtcore.minecraft.cn:8443";
        private const string UaSauth = "WPFLauncher/0.0.0.0";
        private const string GameIDX19 = "x19";

        private const string EngineVerPE = "3.8.15.292807";
        private const string PatchVerPE = "3.8.16.292826";
        private const string Hash1PE = "f879442b620c8e8540b7cadb0b425a7e";
        private const string Hash2PE = "cdd2eea871f2dd8dcef5ee9f87a35f3b2b3e7ca013bb30a74d822579860c042b";

        internal class Result
        {
            public string EntityId = "";
            public string Token = "";
            public string Nickname = "";
        }

        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        private static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        private static string Pick(Dictionary<string, string> d, params string[] keys)
        {
            foreach (var k in keys)
                if (d.ContainsKey(k) && !string.IsNullOrEmpty(d[k])) return d[k];
            return "";
        }

        /// <summary>把任意形态的 sauth JSON（含嵌套 sauth_json）摊平成字典。</summary>
        private static Dictionary<string, string> Flatten(string raw)
        {
            var d = new Dictionary<string, string>();
            try
            {
                var o = JObject.Parse(raw.TrimStart('\uFEFF'));
                foreach (var p in o.Properties())
                {
                    if (p.Name == "sauth_json")
                    {
                        // {"sauth_json":"{\"gameid\":...}"} 或 {"sauth_json":{...}}
                        if (p.Value.Type == JTokenType.String) Merge(d, Flatten((string)p.Value));
                        else if (p.Value is JObject) Merge(d, Flatten(p.Value.ToString()));
                        continue;
                    }
                    if (p.Value.Type == JTokenType.String) d[p.Name] = (string)p.Value;
                }
                foreach (var nest in new[] { "entity", "data", "result" })
                    if (o[nest] is JObject jo)
                        foreach (var p in jo.Properties())
                            if (p.Value.Type == JTokenType.String) d[p.Name] = (string)p.Value;
            }
            catch { }
            return d;
        }

        private static void Merge(Dictionary<string, string> dst, Dictionary<string, string> src)
        {
            foreach (var kv in src) dst[kv.Key] = kv.Value;
        }

        /// <summary>用账号文件里的 sauth 做 PE 登录。失败抛异常（带服务端原因）。</summary>
        public static Result Login(string accountFileContent, Action<string> log)
        {
            if (log == null) log = delegate { };
            var sauth = Flatten(accountFileContent);
            string sdkuid = Pick(sauth, "sdkuid", "uid", "user_id");
            string sessionid = Pick(sauth, "sessionid", "session_id");
            if (sdkuid == "" || sessionid == "")
                throw new Exception("sauth 缺少 sdkuid / sessionid");

            string udid = Pick(sauth, "udid", "device_id");
            if (udid == "") udid = sdkuid;
            string deviceid = Pick(sauth, "deviceid", "device_id");
            if (deviceid == "") deviceid = udid;
            string aim = Pick(sauth, "aim_info");
            if (aim == "") aim = "{\"aim\":\"127.0.0.1\",\"country\":\"CN\",\"tz\":\"+0800\",\"tzid\":\"\"}";
            string gameid = Pick(sauth, "gameid");
            if (gameid == "") gameid = GameIDX19;
            string loginChannel = Pick(sauth, "login_channel");
            if (loginChannel == "") loginChannel = "4399com";
            string appChannel = Pick(sauth, "app_channel");
            if (appChannel == "") appChannel = "4399com";
            string sdkVer = Pick(sauth, "sdk_version");
            if (sdkVer == "") sdkVer = "3.9.0";

            string peUdid = udid;
            if (peUdid.Length != 32) peUdid = PeCrypto.RandAlnum(32).ToUpperInvariant();

            string saData = "{\"app_channel\":\"netease\",\"app_ver\":\"3.9.7.297341\",\"core_num\":\"24\"," +
                "\"cpu_digit\":\"0\",\"cpu_hz\":\"\",\"cpu_name\":\"Intel(R) Core(TM) i7-14650HX\"," +
                "\"device_height\":\"2538\",\"device_model\":\"Win32\",\"device_width\":\"1472\"," +
                "\"disk\":\"\",\"emulator\":0,\"first_udid\":\"" + peUdid + "\",\"is_guest\":0," +
                "\"launcher_type\":\"PE_C++\",\"mac_addr\":\"02:00:00:00:00:00\"," +
                "\"network\":\"CHANNEL_UNKNOW\",\"os_name\":\"windows\",\"os_ver\":\"10.0.26100\"," +
                "\"ram\":\"16790933504\",\"rom\":\"\",\"root\":false,\"sdk_ver\":\"5.9.0\"," +
                "\"start_type\":\"default\",\"udid\":\"" + peUdid + "\"}";

            string sauthJson = "{\"gameid\":\"" + Esc(gameid) + "\",\"login_channel\":\"" + Esc(loginChannel) + "\"," +
                "\"app_channel\":\"" + Esc(appChannel) + "\",\"platform\":\"ad\"," +
                "\"sdkuid\":\"" + Esc(sdkuid) + "\",\"sessionid\":\"" + Esc(sessionid) + "\"," +
                "\"sdk_version\":\"" + Esc(sdkVer) + "\",\"udid\":\"" + peUdid + "\"," +
                "\"deviceid\":\"" + Esc(deviceid) + "\",\"aim_info\":\"" + Esc(aim) + "\"}";

            string seed = Guid.NewGuid().ToString();
            string message = EngineVerPE + Hash1PE + PatchVerPE + Hash2PE + seed;
            byte[] sig = PeCrypto.PeSignCompute(message, 4, 7);
            if (sig == null || sig.Length != 16) throw new Exception("PE 签名计算失败");

            string plain = "{\"sa_data\":" + saData + ",\"engine_version\":\"" + EngineVerPE + "\"," +
                "\"patch_version\":\"" + PatchVerPE + "\",\"message\":\"" + Esc(message) + "\"," +
                "\"sauth_json\":" + sauthJson + ",\"seed\":\"" + seed + "\"," +
                "\"sign\":\"" + Convert.ToBase64String(sig) + "\"}";

            byte[] blob = PeCrypto.EncryptG79V12(Encoding.UTF8.GetBytes(plain));
            string hexBody = PeCrypto.BytesToHex(blob, false);

            var req = new HttpRequestMessage(HttpMethod.Post, CoreHostPE + "/pe-authentication");
            req.Content = new StringContent(hexBody, Encoding.UTF8, "application/json");
            req.Headers.TryAddWithoutValidation("User-Agent", UaSauth);
            req.Headers.TryAddWithoutValidation("Accept", "*/*");

            var resp = Http.SendAsync(req).Result;
            int status = (int)resp.StatusCode;
            string text = resp.Content.ReadAsStringAsync().Result;
            if (string.IsNullOrEmpty(text)) throw new Exception("PE 认证无响应 (HTTP " + status + ")");

            string authBody = "";
            try
            {
                byte[] raw = PeCrypto.HexToBytes(text);
                try
                {
                    string s = PeCrypto.DecryptG79V12Large(raw);
                    if (!string.IsNullOrEmpty(s) && s[0] == '{') authBody = s;
                }
                catch { }
                if (authBody == "")
                {
                    try { authBody = PeCrypto.DecryptG79V12(raw); } catch { }
                }
            }
            catch { }
            if (authBody == "" && text.TrimStart().StartsWith("{")) authBody = text;
            if (authBody == "") throw new Exception("PE 响应解密失败（HTTP " + status + "）");

            var obj = PeJson.AsDict(PeJson.Parse(authBody));
            string code = PeJson.Str(obj, "code");
            if (code != "" && code != "0" && code != "200")
                throw new Exception("PE 登录失败 code=" + code + "：" + PeJson.Str(obj, "message"));

            var res = new Result
            {
                EntityId = PeJson.FirstString(
                    PeJson.FindStringDeep(obj, "entity_id"), PeJson.Str(obj, "entity_id")),
                Token = PeJson.FirstString(
                    PeJson.FindStringDeep(obj, "token"), PeJson.Str(obj, "token")),
                Nickname = PeJson.FirstString(
                    PeJson.FindStringDeep(obj, "nickname"), PeJson.FindStringDeep(obj, "name"))
            };
            if (res.EntityId == "" || res.Token == "") throw new Exception("PE 登录失败：响应里没有 entity_id/token");
            log("PE 身份 OK：uid=" + res.EntityId + (res.Nickname.Length > 0 ? " (" + res.Nickname + ")" : ""));
            return res;
        }
    }
}
