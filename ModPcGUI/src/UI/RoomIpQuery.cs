using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace ModPcGUI
{
    /// <summary>
    /// 房间/租赁服/山头 IP 查询核心（无 UI 依赖）。
    /// 逻辑移植自「房间IP查询-GUI界面版-20261108」LobbyApi.cs，接 ES 的登录会话（Http.UID + 动态 token）。
    ///
    /// 大厅房间完整链路（PC / PE 双模式）：
    ///   房名 → search-by-name 解析 entity_id + res_id + lobby_manifest_version
    ///   → room-enter（12015 自动重解析 / 501 自动购买 / 限速退避重试）
    ///   → game-enter 取 server_host/server_port
    ///   → leave-room 退房（PE 带 team_quit:false）
    /// </summary>
    internal static class RoomIpQuery
    {
        private const string PcGateway = "https://x19apigatewayobt.nie.netease.com";
        private const string PeGateway = "https://g79apigatewayobt.minecraft.cn";
        private const string PeWeb = "https://g79mclobt.minecraft.cn";
        private const string RoomListVer = "1.21.130";
        private static readonly int[] Backoff = { 1200, 2500, 3500, 4500 };

        // ───────────── 响应 ─────────────

        private class Resp
        {
            public JObject Raw;
            public int Code = -999;
            public string Msg = "";
        }

        private static Resp Parse(string text)
        {
            var r = new Resp();
            if (string.IsNullOrEmpty(text)) return r;
            try
            {
                r.Raw = JObject.Parse(text);
                r.Code = (int?)r.Raw["code"] ?? -999;
                string m = (string)r.Raw["message"];
                if (string.IsNullOrEmpty(m)) m = (string)r.Raw["msg"];
                r.Msg = m ?? "";
            }
            catch { }
            return r;
        }

        // PC 网关（x19apigatewayobt）
        private static Resp PostPc(string path, string body)
        {
            return Parse(ConsoleAppLogin.NetEase.Http.postAPIPC(body ?? "", path, false));
        }

        // PE 网关（g79apigatewayobt）
        private static Resp PostPe(string path, string body)
        {
            return Parse(ConsoleAppLogin.NetEase.Http.postAPI(body ?? "", path, false));
        }

        // 任意 host（g79mclobt.minecraft.cn 等）
        private static Resp PostHost(string host, string path, string body)
        {
            return Parse(ConsoleAppLogin.NetEase.Http.postAPIHost(body ?? "", path, host));
        }

        private static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        // ───────────── JSON 深搜 ─────────────

        private static void CollectProps(JToken t, List<JProperty> acc)
        {
            if (t == null) return;
            var o = t as JObject;
            if (o != null)
            {
                foreach (var p in o.Properties())
                {
                    acc.Add(p);
                    CollectProps(p.Value, acc);
                }
                return;
            }
            var a = t as JArray;
            if (a != null)
                foreach (var i in a) CollectProps(i, acc);
        }

        /// <summary>按 key 优先级深搜第一个非空值（网易把地址塞在 entity/data 各层不一）。</summary>
        private static string FirstStr(JObject o, params string[] keys)
        {
            if (o == null) return "";
            var all = new List<JProperty>();
            CollectProps(o, all);
            foreach (var k in keys)
                foreach (var p in all)
                    if (p.Name == k && p.Value != null &&
                        (p.Value.Type == JTokenType.String ||
                         p.Value.Type == JTokenType.Integer ||
                         p.Value.Type == JTokenType.Float))
                    {
                        string s = p.Value.ToString();
                        if (s.Length > 0) return s;
                    }
            return "";
        }

        private static bool IsRateLimited(Resp r)
        {
            if (r == null) return false;
            if (r.Code == 429 || r.Code == 502 || r.Code == 503 || r.Code == 504) return true;
            if (r.Code == 12022) return true;
            string m = r.Msg ?? "";
            return m.Contains("频繁") || m.Contains("too frequent") || m.Contains("rate limit");
        }

        // ───────────── 搜索 ─────────────

        private class RoomHit
        {
            public string EntityId = "", Name = "", ResId = "", Manifest = "";
            public string OwnerId = "";
            public bool HasPwd;
            public int Members, MaxMembers;
        }

        /// <summary>房间在该大厅里根本不存在（才允许切另一个大厅；存在但满员/密码不切，避免进错同名房）</summary>
        private class HallNotFoundEx : Exception
        {
            public HallNotFoundEx(string m) : base(m) { }
        }

        internal static string GameVersion()
        {
            string ver = "";
            try { ver = text.app.Temp.Version; } catch { }
            if (string.IsNullOrEmpty(ver)) { try { ver = Login.NetEase.Function.GetVersion(); } catch { } }
            if (string.IsNullOrEmpty(ver)) ver = RoomListVer;
            return ver;
        }

        private static List<RoomHit> SearchRooms(string keyword, bool pe, Action<string> log)
        {
            var list = new List<RoomHit>();
            string body = "{\"room_name\":\"" + Esc(keyword) + "\",\"res_id\":\"\",\"version\":\"" +
                          Esc(GameVersion()) + "\",\"offset\":0,\"length\":50}";
            Resp r = null;
            for (int i = 0; i < 3; i++)
            {
                if (i > 0) { log("  搜索限速，等 5 秒重试…"); Thread.Sleep(5000); }
                r = pe
                    ? PostHost(PeWeb, "/online-lobby-room/query/search-by-name", body)
                    : PostPc("/online-lobby-room/query/search-by-name", body);
                if (r.Code == 0 || r.Code != 2311) break;
            }
            if (r == null || r.Raw == null) return list;
            var ents = r.Raw["entities"] as JArray;
            if (ents == null) return list;
            foreach (var e in ents)
            {
                var o = e as JObject;
                if (o == null) continue;
                var h = new RoomHit
                {
                    EntityId = (string)o["entity_id"] ?? "",
                    Name = (string)o["room_name"] ?? "",
                    ResId = (string)o["res_id"] ?? "",
                    Manifest = (string)o["lobby_manifest_version"] ?? "",
                    OwnerId = (string)o["owner_id"] ?? "",
                    HasPwd = (int?)o["password"] == 1,
                    MaxMembers = (int?)o["max_count"] ?? 0
                };
                var mu = o["member_uids"] as JArray;
                if (mu != null) h.Members = mu.Count;
                if (h.EntityId.Length > 0) list.Add(h);
            }
            return list;
        }

        // ───────────── 购买 ─────────────

        /// <summary>
        /// 领/买联机专区资源。
        /// PC：/user-item-purchase，先按原程序硬编码 739563429（本机实测 code=0），失败再用自己的 UID。
        /// PE：/pe-purchase-item/（g79mclobt）。
        /// </summary>
        private static void Purchase(string resId, bool pe, Action<string> log)
        {
            if (string.IsNullOrEmpty(resId)) throw new Exception("缺少 res_id，无法购买联机专区资源");

            if (pe)
            {
                // PE 购买必须用 PE 身份（PC 会话的身份买 PE 物品一律 12/16）。
                string pb = "{\"item_id\": \"" + Esc(resId) + "\", \"expertcomment_info\": " +
                            "{\"expertcomment_id\": \"0\", \"video_url\": \"0\", \"expert_id\": \"0\"}, " +
                            "\"buy_path\": \"\\u9996\\u9875-\\u8054\\u673a\\u5927\\u5385-\\u8be6\\u60c5\\u9875:" +
                            Esc(resId) + "\", \"coupon_ids\": [], \"cdk_code\": \"\"}";
                Resp pr;
                if (EnsurePeSession(log)) pr = PostPeAs(PeWeb, "/pe-purchase-item/", pb);
                else pr = PostHost(PeWeb, "/pe-purchase-item/", pb);
                log("  PE 端点购买 code=" + pr.Code + " " + pr.Msg);
                if (pr.Code == 0 || pr.Code == 502) return;
                log("  PE 端点不通，改用 PC 账本购买（同一 item id 体系）…");
            }

            string self = (ConsoleAppLogin.NetEase.Http.UID + "") ?? "";
            // 自己 UID 是正主（PE 物品用 739563429 会「目标找不到」）；
            // 739563429 只对原外挂那批 PC 联机专区物品有效，作为兜底。
            string[] buyers = { self, "739563429" };
            foreach (var uid in buyers)
            {
                if (string.IsNullOrEmpty(uid)) continue;
                string pb = "{\"entity_id\":0,\"item_id\":\"" + Esc(resId) + "\",\"item_level\":0," +
                            "\"user_id\":\"" + Esc(uid) + "\",\"purchase_time\":0,\"last_play_time\":0," +
                            "\"total_play_time\":0,\"receiver_id\":\"\",\"buy_path\":\"PC_H5_COMPONENT_DETAIL\"," +
                            "\"coupon_ids\":null,\"diamond\":0,\"activity_name\":\"\",\"batch_count\":1}";
                var pr = PostPc("/user-item-purchase", pb);
                log("  购买(user_id=" + uid + ") code=" + pr.Code + " " + pr.Msg);
                if (pr.Code == 44) // 购买太频繁 → 等一会儿重试一次
                {
                    log("  购买限频，等 20 秒重试…");
                    Thread.Sleep(20000);
                    pr = PostPc("/user-item-purchase", pb);
                    log("  购买重试 code=" + pr.Code + " " + pr.Msg);
                }
                if (pr.Code == 0 || pr.Code == 502) return; // 0=成功 502=已拥有
            }
        }

        /// <summary>PE 购买变体试错（自测用）：同一 res 试多种 payload，返回第一个 code=0/502 的说明。</summary>
        internal static string PeBuyProbe(string resId, Action<string> log)
        {
            var variants = new List<KeyValuePair<string, string>>();
            string esc = Esc(resId);
            variants.Add(new KeyValuePair<string, string>("A 原码形态",
                "{\"item_id\": \"" + esc + "\", \"expertcomment_info\": {\"expertcomment_id\": \"0\", \"video_url\": \"0\", \"expert_id\": \"0\"}, " +
                "\"buy_path\": \"\\u9996\\u9875-\\u8054\\u673a\\u5927\\u5385-\\u8be6\\u60c5\\u9875:" + esc + "\", \"coupon_ids\": [], \"cdk_code\": \"\"}"));
            variants.Add(new KeyValuePair<string, string>("B item_id 数值",
                "{\"item_id\": " + esc + ", \"expertcomment_info\": {\"expertcomment_id\": \"0\", \"video_url\": \"0\", \"expert_id\": \"0\"}, " +
                "\"buy_path\": \"\\u9996\\u9875-\\u8054\\u673a\\u5927\\u5385-\\u8be6\\u60c5\\u9875:" + esc + "\", \"coupon_ids\": [], \"cdk_code\": \"\"}"));
            variants.Add(new KeyValuePair<string, string>("C 加 channel_id",
                "{\"item_id\": \"" + esc + "\", \"channel_id\": 5, \"expertcomment_info\": {\"expertcomment_id\": \"0\", \"video_url\": \"0\", \"expert_id\": \"0\"}, " +
                "\"buy_path\": \"\\u9996\\u9875-\\u8054\\u673a\\u5927\\u5385-\\u8be6\\u60c5\\u9875:" + esc + "\", \"coupon_ids\": [], \"cdk_code\": \"\"}"));
            variants.Add(new KeyValuePair<string, string>("D ASCII buy_path",
                "{\"item_id\": \"" + esc + "\", \"expertcomment_info\": {\"expertcomment_id\": \"0\", \"video_url\": \"0\", \"expert_id\": \"0\"}, " +
                "\"buy_path\": \"PC_H5_COMPONENT_DETAIL\", \"coupon_ids\": [], \"cdk_code\": \"\"}"));
            variants.Add(new KeyValuePair<string, string>("E 加 diamond/batch",
                "{\"item_id\": \"" + esc + "\", \"expertcomment_info\": {\"expertcomment_id\": \"0\", \"video_url\": \"0\", \"expert_id\": \"0\"}, " +
                "\"buy_path\": \"\\u9996\\u9875-\\u8054\\u673a\\u5927\\u5385-\\u8be6\\u60c5\\u9875:" + esc + "\", \"coupon_ids\": [], \"cdk_code\": \"\", " +
                "\"diamond\": 0, \"batch_count\": 1}"));

            foreach (var v in variants)
            {
                var r1 = PostHost(PeWeb, "/pe-purchase-item/", v.Value);
                log("  " + v.Key + " @g79mclobt → code=" + r1.Code + " " + r1.Msg);
                if (r1.Code == 0 || r1.Code == 502) return v.Key;
                var r2 = PostPe("/pe-purchase-item/", v.Value);
                log("  " + v.Key + " @g79apigateway → code=" + r2.Code + " " + r2.Msg);
                if (r2.Code == 0 || r2.Code == 502) return v.Key + " @g79apigateway";
            }
            return "";
        }

        /// <summary>PE 房间/物品清单 dump（自测用）。</summary>
        internal static void PeDump(string roomName, Action<string> log)
        {
            string body = "{\"room_name\":\"" + Esc(roomName) + "\",\"res_id\":\"\",\"version\":\"" +
                          Esc(GameVersion()) + "\",\"offset\":0,\"length\":5}";
            string raw = ConsoleAppLogin.NetEase.Http.postAPIHost(body,
                "/online-lobby-room/query/search-by-name", PeWeb);
            log("PE 搜索原始响应：" + (raw == null ? "null" : raw));

            string resId = "";
            try
            {
                var jo = JObject.Parse(raw);
                var e0 = jo["entities"] != null && jo["entities"].HasValues ? jo["entities"][0] : null;
                if (e0 != null) resId = (string)e0["res_id"] ?? "";
            }
            catch { }
            log("房间 res_id=" + resId);

            const string Gray = "https://g79mclobtgray.nie.netease.com";
            string byId = "{\"item_ids\":[\"" + Esc(resId) + "\"]}";
            log("按 id 查物品(gray)：" + (ConsoleAppLogin.NetEase.Http.postAPIHost(byId,
                "/pe-item/query/search-lobby-by-id-list", Gray) ?? "null"));
            log("按 id 查物品(mclobt)：" + (ConsoleAppLogin.NetEase.Http.postAPIHost(byId,
                "/pe-item/query/search-lobby-by-id-list", PeWeb) ?? "null"));

            string buyBody = "{\"item_id\": \"" + Esc(resId) + "\", \"expertcomment_info\": " +
                             "{\"expertcomment_id\": \"0\", \"video_url\": \"0\", \"expert_id\": \"0\"}, " +
                             "\"buy_path\": \"\\u9996\\u9875-\\u8054\\u673a\\u5927\\u5385-\\u8be6\\u60c5\\u9875:" + Esc(resId) +
                             "\", \"coupon_ids\": [], \"cdk_code\": \"\"}";
            log("购买(gray)：" + (ConsoleAppLogin.NetEase.Http.postAPIHost(buyBody, "/pe-purchase-item/", Gray) ?? "null"));

            // 试：用 PC 的购买接口买 PE 物品（同一 item 账本）
            string self = (ConsoleAppLogin.NetEase.Http.UID + "") ?? "";
            foreach (var uid in new[] { "739563429", self })
            {
                string pcb = "{\"entity_id\":0,\"item_id\":\"" + Esc(resId) + "\",\"item_level\":0,\"user_id\":\"" +
                             Esc(uid) + "\",\"purchase_time\":0,\"last_play_time\":0,\"total_play_time\":0," +
                             "\"receiver_id\":\"\",\"buy_path\":\"PC_H5_COMPONENT_DETAIL\",\"coupon_ids\":null," +
                             "\"diamond\":0,\"activity_name\":\"\",\"batch_count\":1}";
                log("PC网关购买(user=" + uid + ")：" +
                    (ConsoleAppLogin.NetEase.Http.postAPIPC(pcb, "/user-item-purchase", false) ?? "null"));
            }

            string zoneBody = "{\"channel_id\":5,\"length\":50,\"filter_my_favorite\":true,\"first_type_id\":6," +
                              "\"order_flag\":1,\"offset\":0,\"asc_flag\":0,\"filter_type\":[],\"download_iids\":[]}";
            string zone = ConsoleAppLogin.NetEase.Http.postAPI(zoneBody, "/pe-user-item-purchase", false);
            log("PE 已购清单：" + (zone == null ? "null" : (zone.Length > 1200 ? zone.Substring(0, 1200) + "…" : zone)));
        }

        // ───────────── PE 身份会话 ─────────────

        private static bool _peTried;
        private static string _peUid = "", _peToken = "";
        private static readonly System.Net.Http.HttpClient PeHttp =
            new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        /// <summary>用账号 sauth 做 PE 认证，拿 PE 身份 + PE token（PE 购买/进房必须用这个身份）。</summary>
        internal static bool EnsurePeSession(Action<string> log)
        {
            if (_peTried) return _peToken.Length > 0;
            _peTried = true;
            try
            {
                var accs = AppHost.ListAccounts();
                if (accs.Count == 0) { log("PE 身份：没有账号文件"); return false; }
                string raw = File.ReadAllText(accs[0]);
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        var r = PeAuth.PeAuth.Login(raw, log);
                        _peUid = r.EntityId;
                        _peToken = r.Token;
                        return _peToken.Length > 0;
                    }
                    catch (Exception ex)
                    {
                        string m = ex.Message ?? "";
                        bool retry = m.Contains("2100") || m.Contains("频繁") || m.Contains("稍后")
                                  || m.Contains("没有 entity_id") || m.Contains("超时") || m.Contains("timeout");
                        if (retry && i < 2)
                        {
                            log("PE 登录限频/异常（" + m + "），等 25 秒重试…");
                            Thread.Sleep(25000);
                            continue;
                        }
                        throw;
                    }
                }
            }
            catch (Exception ex) { log("PE 身份登录失败：" + ex.Message); }
            return false;
        }

        /// <summary>用 PE 身份 POST（PE token 自己算 user-token，不走原生 PC 会话）。</summary>
        private static Resp PostPeAs(string host, string path, string body)
        {
            var r = new Resp();
            if (_peToken.Length == 0) return r;
            try
            {
                string ut = PeAuth.PeCrypto.ComputeDynamicToken(path, body ?? "", _peToken);
                var req = new System.Net.Http.HttpRequestMessage(
                    System.Net.Http.HttpMethod.Post, host + path);
                req.Content = new System.Net.Http.StringContent(body ?? "", System.Text.Encoding.UTF8, "application/json");
                req.Headers.TryAddWithoutValidation("user-id", _peUid);
                req.Headers.TryAddWithoutValidation("user-token", ut);
                req.Headers.TryAddWithoutValidation("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                var resp = PeHttp.SendAsync(req).Result;
                return Parse(resp.Content.ReadAsStringAsync().Result);
            }
            catch (Exception ex)
            {
                r.Msg = "PE 请求异常：" + ex.Message;
                return r;
            }
        }

        /// <summary>PE 请求：有 PE 身份就用 PE token，否则退回全局会话。</summary>
        private static Resp PeCall(string path, string body)
        {
            if (_peToken.Length > 0) return PostPeAs(PeGateway, path, body);
            return PostPe(path, body);
        }

        private static void Leave(string rid, bool pe)
        {
            try
            {
                if (pe)
                    PeCall("/online-lobby-room-enter/leave-room",
                        "{\"room_id\":\"" + Esc(rid) + "\",\"team_quit\":false}");
                else
                    PostPc("/online-lobby-room-enter/leave-room",
                        "{\"room_id\":\"" + Esc(rid) + "\"}");
            }
            catch { }
        }

        // ───────────── 大厅进房 ─────────────

        /// <summary>单个大厅模式（PC / PE）走完整进房流程，返回 "ip:port"，失败抛异常。</summary>
        private static string TryLobby(string roomRef, bool pe, Action<string> log)
        {
            string tag = pe ? "[PE] " : "[PC] ";
            string rid = roomRef, resId = "", manifest = "";

            // PE 模式：先拿 PE 身份（PE 进房/购买都用它）
            if (pe) EnsurePeSession(log);

            // 短号（房名）先搜出 entity_id / res_id / manifest
            if (roomRef.Length < 15)
            {
                log(tag + "按房名搜索 " + roomRef + " …");
                var hits = SearchRooms(roomRef, pe, log);
                var hit = hits.FirstOrDefault(x => x.Name == roomRef) ?? hits.FirstOrDefault();
                if (hit == null)
                    throw new HallNotFoundEx("搜不到房间 " + roomRef + "（可能已关闭 / 已改名 / 不在" + (pe ? "PE" : "PC") + "大厅）");
                rid = hit.EntityId;
                resId = hit.ResId;
                manifest = hit.Manifest;
                log(tag + "解析到「" + hit.Name + "」 id=" + rid +
                    (hit.HasPwd ? "（有密码，将用空密码尝试）" : "") +
                    (hit.Members > 0 ? " 在线 " + hit.Members : "") +
                    (resId.Length > 0 ? "  资源=" + resId : ""));
            }

            return EnterAndGet(rid, resId, manifest, roomRef, pe, log);
        }

        /// <summary>已知房间标识（entity_id / res_id / manifest）时的进房取地址流程。</summary>
        private static string EnterAndGet(string rid, string resId, string manifest,
                                          string roomRef, bool pe, Action<string> log)
        {
            string tag = pe ? "[PE] " : "[PC] ";
            bool purchased = false;
            bool leftStale = false;   // 是否已处理过「已经在房间内」的残留座位
            string lastMsg = "";

            for (int attempt = 0; attempt < 5; attempt++)
            {
                if (attempt > 0)
                {
                    int wait = Backoff[Math.Min(attempt - 1, Backoff.Length - 1)];
                    log("进房受限，" + (wait / 1000.0).ToString("0.0") + " 秒后重试（" + (attempt + 1) + "/5）…");
                    Thread.Sleep(wait);
                }

                string enterBody = pe
                    ? "{\"room_id\":\"" + Esc(rid) + "\",\"password\":\"\"" +
                      (manifest.Length > 0 ? ",\"lobby_manifest_version\":\"" + Esc(manifest) + "\"" : "") +
                      ",\"check_visibilily\":false}"
                    : "{\"room_id\":\"" + Esc(rid) + "\",\"password\":\"\",\"check_visibilily\":false}";
                Resp r = pe
                    ? PeCall("/online-lobby-room-enter", enterBody)
                    : PostPc("/online-lobby-room-enter", enterBody);

                // 12015 / 无法找到 → 说明传的不是 entity_id，重新按房名解析一次
                if (r.Code == 12015 || (r.Msg ?? "").Contains("无法找到"))
                {
                    log(tag + "找不到该房间，重新按房名解析…");
                    var hits2 = SearchRooms(roomRef, pe, log);
                    var h2 = hits2.FirstOrDefault(x => x.Name == roomRef) ?? hits2.FirstOrDefault();
                    if (h2 != null)
                    {
                        if (h2.EntityId != rid) { rid = h2.EntityId; log(tag + "改用 entity_id=" + rid); }
                        if (resId.Length == 0) resId = h2.ResId;
                        if (manifest.Length == 0) manifest = h2.Manifest;
                        enterBody = pe
                            ? "{\"room_id\":\"" + Esc(rid) + "\",\"password\":\"\"" +
                              (manifest.Length > 0 ? ",\"lobby_manifest_version\":\"" + Esc(manifest) + "\"" : "") +
                              ",\"check_visibilily\":false}"
                            : "{\"room_id\":\"" + Esc(rid) + "\",\"password\":\"\",\"check_visibilily\":false}";
                        r = pe
                            ? PeCall("/online-lobby-room-enter", enterBody)
                            : PostPc("/online-lobby-room-enter", enterBody);
                    }
                    else throw new HallNotFoundEx("找不到该房间 " + roomRef + "（搜索无结果，房间可能已关闭）");
                }

                // 501 物品还未获得 → 自动买资源后重试
                if (r.Code == 501 && !purchased)
                {
                    log(tag + "物品未获得，自动购买联机专区资源…");
                    try { Purchase(resId, pe, log); }
                    catch (Exception pex) { log("  购买失败：" + pex.Message); }
                    purchased = true;
                    Thread.Sleep(1200);
                    r = pe
                        ? PeCall("/online-lobby-room-enter", enterBody)
                        : PostPc("/online-lobby-room-enter", enterBody);
                }

                if (r.Code != 0)
                {
                    lastMsg = (r.Msg ?? "").Length > 0 ? r.Msg : ("code=" + r.Code);

                    // 上次查询没退干净（进程被 kill 等），座位还挂着 → 先退再进
                    if (!leftStale && lastMsg.Contains("已经在房间"))
                    {
                        leftStale = true;
                        log(tag + "上次没退干净（" + lastMsg + "），先退房再重进…");
                        Leave(rid, pe);
                        Thread.Sleep(800);
                        if (attempt + 1 < 5) continue;
                    }

                    bool retryable = IsRateLimited(r) || (r.Code == 501 && purchased);
                    if (retryable && attempt + 1 < 5) continue;
                    throw new Exception("进房失败（code=" + r.Code + "）：" + lastMsg);
                }

                log(tag + "已进房，取服务器地址…");
                Resp g = pe
                    ? PeCall("/online-lobby-game-enter", "")
                    : PostPc("/online-lobby-game-enter", "");
                string ip = FirstStr(g.Raw, "server_host", "mcserver_host", "host");
                string port = FirstStr(g.Raw, "server_port", "mcserver_port", "port");
                Leave(rid, pe); // 必须退房，否则下次进同房会「已在房间中」

                if (ip.Length > 0 && port.Length > 0)
                {
                    log(tag + "拿到地址 " + ip + ":" + port);
                    return ip + ":" + port;
                }

                lastMsg = (g.Msg ?? "").Length > 0 ? g.Msg : ("code=" + g.Code);
                if (IsRateLimited(g) && attempt + 1 < 5) continue;
                throw new Exception("取地址失败：" + lastMsg);
            }

            throw new Exception("进房失败：" + (lastMsg.Length > 0 ? lastMsg : "重试耗尽"));
        }

        // ───────────── 对外入口 ─────────────

        /// <summary>
        /// kind = "room"   大厅房间：PC 大厅优先，失败自动切 PE 大厅
        /// kind = "rental" 租赁服：/rental-server-world-enter/get
        /// kind = "realms" 山头：/domain-server/req-enter-domain-server
        /// 成功返回 "ip:port"；失败抛异常（带可读原因）。必须在登录成功（AppHost.Started）后调用。
        /// </summary>
        public static string Query(string rid, string kind, Action<string> log)
        {
            if (log == null) log = delegate { };
            rid = (rid ?? "").Trim();
            if (rid.Length == 0) throw new Exception("房间号为空");

            if (kind == "rental")
            {
                log("查询租赁服 " + rid + " …");
                string[] hosts = { PeWeb, PcGateway };
                string lastMsg = "";
                foreach (var host in hosts)
                {
                    string resp = ConsoleAppLogin.NetEase.Http.x19post(host,
                        "/rental-server-world-enter/get",
                        "{\"server_id\": \"" + Esc(rid) + "\", \"pwd\": \"\"}");
                    if (string.IsNullOrEmpty(resp)) { lastMsg = "网关无响应"; continue; }
                    var jo = JObject.Parse(resp);
                    string ip = FirstStr(jo, "mcserver_host", "server_host", "host");
                    string port = FirstStr(jo, "mcserver_port", "server_port", "port");
                    if (ip.Length > 0 && port.Length > 0) return ip + ":" + port;
                    string msg = FirstStr(jo, "message", "msg");
                    lastMsg = msg.Length > 0 ? msg : "租赁服没有返回地址";
                }
                throw new Exception("租赁服查询失败：" + lastMsg);
            }

            if (kind == "realms")
            {
                log("查询山头 " + rid + " …");
                string[] hosts = { PeWeb, PcGateway };
                string lastMsg = "";
                foreach (var host in hosts)
                {
                    string resp = ConsoleAppLogin.NetEase.Http.x19post(host,
                        "/domain-server/req-enter-domain-server",
                        "{\"sid\": \"" + Esc(rid) + "\"}");
                    if (string.IsNullOrEmpty(resp)) { lastMsg = "网关无响应"; continue; }
                    var jo = JObject.Parse(resp);
                    string ip = FirstStr(jo, "server_host", "host");
                    string port = FirstStr(jo, "server_port", "port");
                    if (ip.Length > 0 && port.Length > 0) return ip + ":" + port;
                    string msg = FirstStr(jo, "message", "msg");
                    lastMsg = msg.Length > 0 ? msg : "山头没有返回地址";
                }
                throw new Exception("山头查询失败：" + lastMsg);
            }

            // 大厅房间：PC 优先（ES 会话就是 PC）。
            // 只有「PC 大厅根本没有这个房」才切 PE 大厅；
            // 房间存在但满员/密码 → 直接报错，绝不进 PE 里的同名房。
            try
            {
                return TryLobby(rid, false, log);
            }
            catch (HallNotFoundEx pcex)
            {
                log("PC 大厅找不到该房间 → 改试 PE 大厅…");
                try
                {
                    return TryLobby(rid, true, log);
                }
                catch (Exception peex)
                {
                    throw new Exception("PC：" + pcex.Message + " ｜ PE：" + peex.Message);
                }
            }
        }

        /// <summary>联机大厅用：原始 POST 文本返回（pe=true 走 PE 网关，优先用 PE 身份签名）。</summary>
        internal static string PostText(bool pe, string path, string body)
        {
            try
            {
                if (pe)
                {
                    if (_peToken.Length == 0) EnsurePeSession(delegate { });
                    if (_peToken.Length > 0)
                    {
                        string ut = PeAuth.PeCrypto.ComputeDynamicToken(path, body ?? "", _peToken);
                        var req = new System.Net.Http.HttpRequestMessage(
                            System.Net.Http.HttpMethod.Post, PeGateway + path);
                        req.Content = new System.Net.Http.StringContent(body ?? "", System.Text.Encoding.UTF8, "application/json");
                        req.Headers.TryAddWithoutValidation("user-id", _peUid);
                        req.Headers.TryAddWithoutValidation("user-token", ut);
                        req.Headers.TryAddWithoutValidation("User-Agent",
                            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                        var resp = PeHttp.SendAsync(req).Result;
                        return resp.Content.ReadAsStringAsync().Result;
                    }
                    return ConsoleAppLogin.NetEase.Http.postAPI(body ?? "", path, false);
                }
                return ConsoleAppLogin.NetEase.Http.postAPIPC(body ?? "", path, false);
            }
            catch (Exception ex) { return "{\"code\":-1,\"message\":\"" + ex.Message.Replace("\"", "'") + "\"}"; }
        }

        /// <summary>联机大厅用：指定 host 的原始 POST 文本返回。</summary>
        internal static string PostHostText(string host, string path, string body)
        {
            try { return ConsoleAppLogin.NetEase.Http.postAPIHost(body ?? "", path, host) ?? ""; }
            catch (Exception ex) { return "{\"code\":-1,\"message\":\"" + ex.Message.Replace("\"", "'") + "\"}"; }
        }

        /// <summary>批量查昵称（带缓存，校验返回 id 与目标一致）。</summary>
        internal static Dictionary<string, string> NicknamesOf(IEnumerable<string> uids)
        {
            var map = new Dictionary<string, string>();
            foreach (var uid in uids.Where(s => !string.IsNullOrEmpty(s)).Distinct().Take(30))
            {
                string n = NickOf(uid);
                if (n.Length > 0) map[uid] = n;
            }
            return map;
        }

        /// <summary>联机大厅用：拿 PE 身份（供 UI 判断能不能看 PE 房间）。</summary>
        internal static bool EnsurePeSessionForUi(Action<string> log)
        {
            return EnsurePeSession(log);
        }

        // ───────────── 候选房间（同名房可能在 PC / PE 两个大厅同时存在）─────────────

        private static readonly Dictionary<string, string> _zoneNames = new Dictionary<string, string>();
        private static bool _zoneLoaded;
        private static readonly Dictionary<string, string> _nickCache = new Dictionary<string, string>();

        /// <summary>
        /// 在 PC / PE 两个大厅里同时搜同一个房名，返回全部候选。
        /// 每个候选带：大厅、房间号、房主、地图名、人数、是否密码房。
        /// </summary>
        internal static List<RoomCandidate> SearchAll(string keyword, Action<string> log)
        {
            if (log == null) log = delegate { };
            var list = new List<RoomCandidate>();
            keyword = (keyword ?? "").Trim();
            if (keyword.Length == 0) return list;

            log("[PC] 搜索「" + keyword + "」…");
            try
            {
                foreach (var h in SearchRooms(keyword, false, log))
                    list.Add(ToCandidate(h, "PC"));
            }
            catch (Exception ex) { log("[PC] 搜索失败：" + ex.Message); }

            // PE 大厅：搜索走 g79mclobt，需要 PE 身份才能稳定拿到结果
            try
            {
                if (EnsurePeSession(log))
                {
                    log("[PE] 搜索「" + keyword + "」…");
                    foreach (var h in SearchRooms(keyword, true, log))
                        list.Add(ToCandidate(h, "PE"));
                }
                else log("[PE] 没有 PE 身份，跳过 PE 大厅");
            }
            catch (Exception ex) { log("[PE] 搜索失败：" + ex.Message); }

            log("两个大厅共 " + list.Count + " 个候选");
            Enrich(list, log);
            return list;
        }

        private static RoomCandidate ToCandidate(RoomHit h, string lobby)
        {
            return new RoomCandidate
            {
                Lobby = lobby,
                EntityId = h.EntityId,
                Name = h.Name,
                ResId = h.ResId,
                Manifest = h.Manifest,
                OwnerId = h.OwnerId,
                Members = h.Members,
                MaxMembers = h.MaxMembers,
                HasPwd = h.HasPwd
            };
        }

        /// <summary>查询指定的候选房间（跳过按房名重新搜索，直接用它自己的大厅 / 房间号）。</summary>
        internal static string QueryCandidate(RoomCandidate c, Action<string> log)
        {
            if (c == null) throw new Exception("没有选中房间");
            if (log == null) log = delegate { };
            bool pe = c.Lobby == "PE";
            if (pe && !EnsurePeSession(log)) throw new Exception("PE 身份不可用");
            log("查询「" + c.Name + "」（" + c.Lobby + " 大厅，" + c.EntityId + "）…");
            return EnterAndGet(c.EntityId, c.ResId, c.Manifest, c.Name, pe, log);
        }

        /// <summary>补房主昵称 + 地图名（失败只留空，不影响查询）。</summary>
        private static void Enrich(List<RoomCandidate> list, Action<string> log)
        {
            if (list.Count == 0) return;

            if (list.Any(c => c.ResId.Length > 0))
            {
                EnsureZoneMap(log);
                log("  地图库 " + _zoneNames.Count + " 条");
                foreach (var c in list)
                {
                    string n;
                    if (c.ResId.Length > 0 && _zoneNames.TryGetValue(c.ResId, out n) && !string.IsNullOrEmpty(n))
                        c.MapName = n;
                }
            }

            foreach (var id in list.Select(c => c.OwnerId).Where(s => s.Length > 0).Distinct())
            {
                string nick = NickOf(id);
                if (nick.Length == 0) continue;
                foreach (var c in list)
                    if (c.OwnerId == id) c.OwnerName = nick;
            }
        }

        /// <summary>
        /// 查房主昵称。
        /// 注意：PC 的 /user-detail 会忽略 entity_id 直接返回自己（踩过），
        /// 只有 PE 的 /user-detail/query/other 才是按 uid 查别人，且必须校验返回的 id。
        /// </summary>
        private static string NickOf(string uid)
        {
            string cached;
            if (_nickCache.TryGetValue(uid, out cached)) return cached;

            string nick = "";
            try
            {
                var r = PeCall("/user-detail/query/other",
                    "{\"entity_id\":\"" + Esc(uid) + "\",\"is_need_recharge_benefit\":1}");
                var ent = r.Raw == null ? null : r.Raw["entity"] as JObject;
                if (ent != null)
                {
                    string id = (ent["id"] == null ? "" : ent["id"].ToString());
                    if (id == uid) nick = (string)ent["nickname"] ?? (string)ent["name"] ?? "";
                }
            }
            catch { }

            _nickCache[uid] = nick;
            return nick;
        }

        /// <summary>联机专区 ID → 地图名（PC 查已购分区，PE 查专区列表），进程内缓存。</summary>
        private static void EnsureZoneMap(Action<string> log)
        {
            if (_zoneLoaded) return;
            _zoneLoaded = true;
            try
            {
                // PC：先查自己拥有的联机专区 item_id，再批量换成名字
                var ids = new List<string>();
                var r1 = PostPc("/user-item-purchase/query/search-by-user",
                    "{\"item_type\":2,\"master_type_id\":\"11\",\"beginTime\":0,\"endTime\":0," +
                    "\"offset\":0,\"length\":32767}");
                var a1 = r1.Raw == null ? null : r1.Raw["entities"] as JArray;
                if (a1 != null)
                    foreach (var e in a1)
                    {
                        var iid = (string)(e as JObject)?["item_id"];
                        if (!string.IsNullOrEmpty(iid)) ids.Add(iid);
                    }
                if (ids.Count > 0)
                {
                    string quoted = string.Join(",", ids.Select(x => "\"" + Esc(x) + "\""));
                    var r2 = PostPc("/item-buy-list/query/search-mcgame-items",
                        "{\"item_ids\":[" + quoted + "],\"entity_ids\":[" + quoted + "]}");
                    var a2 = r2.Raw == null ? null : r2.Raw["entities"] as JArray;
                    if (a2 != null)
                        foreach (var e in a2)
                        {
                            var o = e as JObject;
                            if (o == null) continue;
                            string iid = (string)o["iid"] ?? "";
                            string nm = (string)o["name"] ?? (string)o["res_name"] ?? "";
                            if (iid.Length > 0 && nm.Length > 0) _zoneNames[iid] = nm;
                        }
                }
            }
            catch (Exception ex) { log("  PC 地图名加载失败（不影响查询）：" + ex.Message); }

            try
            {
                // PE：专区列表（必须用 PE 身份，走全局 PC 会话拿不到东西）
                var rp = PeCall("/pe-user-item-purchase",
                    "{\"channel_id\":5,\"length\":599,\"filter_my_favorite\":false,\"first_type_id\":6," +
                    "\"order_flag\":1,\"offset\":0,\"asc_flag\":0,\"filter_type\":[],\"download_iids\":[]}");
                var ap = rp.Raw == null ? null : rp.Raw["entities"] as JArray;
                if (ap != null)
                    foreach (var e in ap)
                    {
                        var o = e as JObject;
                        if (o == null) continue;
                        string iid = (string)o["item_id"] ?? "";
                        string nm = (string)o["res_name"] ?? (string)o["name"] ?? "";
                        if (iid.Length > 0 && nm.Length > 0) _zoneNames[iid] = nm;
                    }
            }
            catch (Exception ex) { log("  PE 地图名加载失败（不影响查询）：" + ex.Message); }
        }
    }

    /// <summary>一个可查询的房间候选（PC / PE 大厅各是独立命名空间，同名房可能同时存在）。</summary>
    internal sealed class RoomCandidate
    {
        public string Lobby = "";      // "PC" / "PE"
        public string EntityId = "";
        public string Name = "";
        public string ResId = "";
        public string Manifest = "";
        public string OwnerId = "";
        public string OwnerName = "";
        public string MapName = "";
        public int Members;
        public int MaxMembers;
        public bool HasPwd;

        public string Title
        {
            get { return "「" + Name + "」  " + Lobby + " 大厅"; }
        }

        public string Detail
        {
            get
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("房间号 ").Append(EntityId.Length > 0 ? EntityId : "未知");
                sb.Append("　房主 ").Append(OwnerName.Length > 0 ? OwnerName
                    : (OwnerId.Length > 0 ? OwnerId : "未知"));
                sb.Append("　地图 ").Append(MapName.Length > 0 ? MapName : "未知");
                if (MaxMembers > 0) sb.Append("　").Append(Members).Append("/").Append(MaxMembers).Append(" 人");
                else if (Members > 0) sb.Append("　在线 ").Append(Members);
                if (HasPwd) sb.Append("　有密码");
                return sb.ToString();
            }
        }
    }

    /// <summary>
    /// --roomtest &lt;id&gt; [room|rental|realms] 或 --roomtest find room：无头自测。
    /// 用 user\ 目录第一个账号真实登录，查询房间地址，结果写 roomtest.log 后退出。
    /// </summary>
    internal static class RoomTest
    {
        public static void Run(string[] args)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string logFile = Path.Combine(baseDir, "roomtest.log");
            var sb = new System.Text.StringBuilder();
            Action<string> w = s =>
            {
                sb.AppendLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + s);
                try { File.WriteAllText(logFile, sb.ToString(), System.Text.Encoding.UTF8); } catch { }
            };

            try
            {
                string id = args.Length > 1 ? args[1] : "";
                string kind = args.Length > 2 ? args[2] : "room";
                w("roomtest 开始：id=" + id + " kind=" + kind);

                AppHost.Init();
                var accs = AppHost.ListAccounts();
                if (accs.Count == 0) { w("✗ 没有账号文件（user\\ 目录为空）"); return; }
                string accFile = accs[0];
                w("账号：" + Path.GetFileNameWithoutExtension(accFile));

                // 后台登录（Login 成功后会进 app.Command 循环阻塞，Started=true 即视为可用）
                AppHost.KickOff(() => AppHost.Login(accFile, "auto"), "roomtest-login");
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (!AppHost.Started && sw.Elapsed.TotalSeconds < 60)
                {
                    Thread.Sleep(500);
                    if (sw.Elapsed.TotalSeconds % 5 < 0.5) w("登录中… " + (int)sw.Elapsed.TotalSeconds + "s");
                }
                if (!AppHost.Started)
                {
                    w("✗ 登录失败/超时（60s）。LastError=" + AppHost.LastError);
                    return;
                }
                w("登录成功，UID=" + ConsoleAppLogin.NetEase.Http.UID);

                // find 模式：先搜出真实在线房间，再拿第一个走完整查询流程（验证链路能出 IP）
                if (id == "pebuy")
                {
                    string res = args.Length > 2 ? args[2] : "";
                    w("PE 购买试错：res=" + res);
                    string win = RoomIpQuery.PeBuyProbe(res, s => w(s));
                    w(win.Length > 0 ? "✓ 可用变体：" + win : "✗ 全部变体失败");
                    return;
                }

                if (id == "pedump")
                {
                    string rn = args.Length > 2 ? args[2] : "";
                    w("PE dump 房间：" + rn);
                    RoomIpQuery.PeDump(rn, s => w(s));
                    return;
                }

                // hot 模式：全服在线地图池（UI「留空搜索」走的就是这个）
                if (id == "hot")
                {
                    string ed = args.Length > 2 ? args[2] : "pc";
                    int sort = args.Length > 3 ? int.Parse(args[3]) : 0;
                    int pg = args.Length > 4 ? int.Parse(args[4]) : 3;
                    var maps = LobbyHall.HotMaps(ed == "pe", sort, pg, s => w(s));
                    w("=== " + ed.ToUpper() + " sort=" + sort + " 地图池 " + maps.Count + " ===");
                    foreach (var m in maps.Take(15)) w("· " + m.Id + " | " + m.Title + " | " + m.Detail);
                    if (maps.Count > 0)
                    {
                        var rs = LobbyHall.ListRooms(maps[0].Id, ed == "pe", 0, 10, maps[0].Name, s => w(s));
                        foreach (var r in rs.Take(3)) w("   → " + r.Detail);
                    }
                    return;
                }

                // pool 模式：抓全服在线房间 → 按 res_id 聚合，看能不能做「在线人数最高的地图池」
                if (id == "pool")
                {
                    string ed = args.Length > 2 ? args[2] : "pc";
                    int pages = args.Length > 3 ? int.Parse(args[3]) : 2;
                    bool pe = ed == "pe";
                    var agg = new Dictionary<string, int[]>();
                    int totalRooms = 0;
                    for (int p = 0; p < pages; p++)
                    {
                        string body = "{\"room_name\":\"\",\"res_id\":\"\",\"version\":\"" + RoomIpQuery.GameVersion() +
                                      "\",\"offset\":" + (p * 50) + ",\"length\":50}";
                        string resp = pe
                            ? ConsoleAppLogin.NetEase.Http.postAPIHost(body, "/online-lobby-room/query/search-by-name", "https://g79mclobt.minecraft.cn")
                            : ConsoleAppLogin.NetEase.Http.postAPIPC(body, "/online-lobby-room/query/search-by-name", false);
                        var jo = JObject.Parse(string.IsNullOrEmpty(resp) ? "{}" : resp);
                        var ents = jo["entities"] as JArray;
                        w("[第" + (p + 1) + "页] code=" + jo["code"] + " 房间=" + (ents == null ? 0 : ents.Count) + " total=" + jo["total"]);
                        if (ents == null || ents.Count == 0) break;
                        foreach (var e in ents)
                        {
                            var o = e as JObject;
                            if (o == null) continue;
                            totalRooms++;
                            string rs = (string)o["res_id"] ?? "";
                            if (rs.Length == 0) continue;
                            int mem = (o["member_uids"] as JArray)?.Count ?? ((int?)o["cur_num"] ?? 0);
                            if (!agg.ContainsKey(rs)) agg[rs] = new int[2];
                            agg[rs][0]++;
                            agg[rs][1] += mem;
                        }
                        Thread.Sleep(1200);
                    }
                    w("总房间 " + totalRooms + "，涉及地图 " + agg.Count);
                    foreach (var kv in agg.OrderByDescending(x => x.Value[1]).ThenByDescending(x => x.Value[0]).Take(12))
                        w("· " + kv.Key + "  房间 " + kv.Value[0] + "  人数 " + kv.Value[1]);

                    // 试按 id 列表反查地图名
                    var ids = agg.Keys.Take(5).ToList();
                    if (ids.Count > 0)
                    {
                        string q = string.Join(",", ids.Select(x => "\"" + x + "\""));
                        string r1 = pe
                            ? ConsoleAppLogin.NetEase.Http.postAPIHost("{\"item_ids\":[" + q + "]}", "/pe-item/query/search-lobby-by-id-list", "https://g79mclobtgray.nie.netease.com")
                            : ConsoleAppLogin.NetEase.Http.postAPIPC("{\"item_ids\":[" + q + "],\"entity_ids\":[" + q + "]}", "/item/query/search-mcgame-item-list", false);
                        w("=== 反查名字 " + (pe ? "PE" : "PC") + " ===");
                        w(string.IsNullOrEmpty(r1) ? "(空)" : (r1.Length > 500 ? r1.Substring(0, 500) + "…" : r1));

                        if (!pe)
                        {
                            string r2 = ConsoleAppLogin.NetEase.Http.postAPIHost("{\"item_ids\":[" + q + "]}", "/pe-item/query/search-lobby-by-id-list", "https://g79mclobtgray.nie.netease.com");
                            w("=== 反查名字 PC→g79gray ===");
                            w(string.IsNullOrEmpty(r2) ? "(空)" : (r2.Length > 500 ? r2.Substring(0, 500) + "…" : r2));
                        }
                    }
                    return;
                }

                // probe 模式：试若干候选路径，找 PC 端「联机专区列表（带在线人数）」
                if (id == "probe")
                {
                    string kw = args.Length > 2 ? args[2] : "生存";
                    string b1 = "{\"length\":5,\"keyword\":\"" + kw + "\",\"offset\":0,\"sort_type\":1,\"asc_flag\":1}";
                    string[][] tries = {
                        new[]{"pc","/item/query/search-lobby-res-by-keyword", b1},
                        new[]{"pc","/online-lobby-res/query/search-by-keyword", b1},
                        new[]{"pc","/online-lobby-zone/query/search-by-keyword", b1},
                        new[]{"pc","/pe-item/query/search-lobby-res-by-keyword", b1},
                        new[]{"pc","/item/query/search-lobby-res", b1},
                        new[]{"pc","/online-lobby-room/query/search-zone", b1},
                    };
                    foreach (var t in tries)
                    {
                        string r = RoomIpQuery.PostText(t[0] == "pe", t[1], t[2]);
                        string head = string.IsNullOrEmpty(r) ? "(空)" : (r.Length > 240 ? r.Substring(0, 240) + "…" : r);
                        w("[" + t[1] + "] " + head);
                        Thread.Sleep(1200);
                    }
                    return;
                }

                // mraw 模式：dump 地图接口原始返回 + 第一个实体全部字段
                if (id == "mraw")
                {
                    string kw = args.Length > 2 ? args[2] : "";
                    string ed = args.Length > 3 ? args[3] : "pe";
                    var variants = ed == "pe"
                        ? new List<string> {
                            "{\"length\":5,\"keyword\":\"" + kw + "\",\"offset\":0,\"sort_type\":1,\"asc_flag\":1}" }
                        : new List<string> {
                            "{\"item_type\":2,\"keyword\":\"" + kw + "\",\"master_type_id\":\"0\",\"network_tag\":false,\"year\":0,\"sort_type\":1,\"order\":0,\"price_type\":0,\"is_has\":false,\"offset\":0,\"length\":5}",
                            "{\"item_type\":2,\"keyword\":\"" + kw + "\",\"master_type_id\":\"11\",\"network_tag\":true,\"year\":0,\"sort_type\":1,\"order\":0,\"price_type\":0,\"is_has\":false,\"offset\":0,\"length\":5}",
                            "{\"item_type\":3,\"keyword\":\"" + kw + "\",\"master_type_id\":\"0\",\"network_tag\":false,\"year\":0,\"sort_type\":1,\"order\":0,\"price_type\":0,\"is_has\":false,\"offset\":0,\"length\":5}",
                            "{\"item_type\":1,\"keyword\":\"" + kw + "\",\"master_type_id\":\"0\",\"network_tag\":false,\"year\":0,\"sort_type\":1,\"order\":0,\"price_type\":0,\"is_has\":false,\"offset\":0,\"length\":5}" };
                    foreach (var vb in variants)
                    {
                        string path = ed == "pe" ? "/pe-item/query/search-lobby-res-by-keyword" : "/item/query/search-by-keyword";
                        string r = RoomIpQuery.PostText(ed == "pe", path, vb);
                        w("--- " + ed.ToUpper() + " " + vb.Substring(0, Math.Min(90, vb.Length)) + "… ---");
                        var jo = JObject.Parse(string.IsNullOrEmpty(r) ? "{}" : r);
                        var ents = jo["entities"] as JArray;
                        if (ents == null || ents.Count == 0) { w("  (空) code=" + jo["code"]); continue; }
                        w(((JObject)ents[0]).ToString(Newtonsoft.Json.Formatting.Indented));
                        break;
                    }
                    return;
                }

                // maps / rooms / members：联机大厅数据层自测
                if (id == "maps")
                {
                    string kw = args.Length > 2 ? args[2] : "";
                    string ed = args.Length > 3 ? args[3] : "pc";
                    int sort = args.Length > 4 ? int.Parse(args[4]) : 0;
                    foreach (int st in new[] { sort })
                    {
                        var ms = LobbyHall.SearchMaps(kw, st, ed == "pe", 0, 20, s => w(s));
                        w("=== " + ed.ToUpper() + " sort=" + st + " 地图 " + ms.Count + " ===");
                        foreach (var m in ms.Take(12)) w("· " + m.Id + " | " + m.Title + " | " + m.Detail);
                    }
                    return;
                }

                if (id == "rooms")
                {
                    string resid = args.Length > 2 ? args[2] : "";
                    string ed = args.Length > 3 ? args[3] : "pc";
                    string mapName = args.Length > 4 ? args[4] : "";
                    var rs = LobbyHall.ListRooms(resid, ed == "pe", 0, 20, mapName, s => w(s));
                    w("=== " + ed.ToUpper() + " 房间 " + rs.Count + " ===");
                    foreach (var r in rs) w("· " + r.Id + " | " + r.Name + " | " + r.Detail);
                    if (rs.Count > 0)
                    {
                        var mem = LobbyHall.ListMembers(rs[0].Id, ed == "pe", s => w(s));
                        w("=== 玩家 " + mem.Count + " ===");
                        foreach (var m in mem) w("· " + m.Uid + " " + m.Nick);
                    }
                    return;
                }

                if (id == "members")
                {
                    string rid2 = args.Length > 2 ? args[2] : "";
                    string ed = args.Length > 3 ? args[3] : "pc";
                    var mem = LobbyHall.ListMembers(rid2, ed == "pe", s => w(s));
                    w("=== 玩家 " + mem.Count + " ===");
                    foreach (var m in mem) w("· " + m.Uid + " " + m.Nick);
                    return;
                }

                // raw 模式：dump search-by-name 返回的第一个 entity 全部字段（排查房主/地图名从哪来）
                if (id == "raw")
                {
                    string kw = args.Length > 2 ? args[2] : "";
                    foreach (bool pe in new[] { false, true })
                    {
                        if (pe && !RoomIpQuery.EnsurePeSession(s => w(s))) continue;
                        string body = "{\"room_name\":\"" + kw + "\",\"res_id\":\"\",\"version\":\"" +
                                      RoomIpQuery.GameVersion() + "\",\"offset\":0,\"length\":50}";
                        string resp = pe
                            ? ConsoleAppLogin.NetEase.Http.postAPIHost(body, "/online-lobby-room/query/search-by-name", "https://g79mclobt.minecraft.cn")
                            : ConsoleAppLogin.NetEase.Http.postAPIPC(body, "/online-lobby-room/query/search-by-name", false);
                        w("=== " + (pe ? "PE" : "PC") + " raw ===");
                        var jo = JObject.Parse(string.IsNullOrEmpty(resp) ? "{}" : resp);
                        var ents = jo["entities"] as JArray;
                        if (ents == null || ents.Count == 0) { w("(无 entity)"); continue; }
                        w(((JObject)ents[0]).ToString(Newtonsoft.Json.Formatting.Indented));
                        break;
                    }
                    return;
                }

                // zones 模式：找「res_id → 地图名」到底从哪个接口能拿到
                if (id == "zones")
                {
                    string resid = args.Length > 2 ? args[2] : "";
                    w("目标 res_id=" + resid);

                    Action<string, string, string> probe = (tag, host, body) =>
                    {
                        try
                        {
                            string s = host.Length > 0
                                ? ConsoleAppLogin.NetEase.Http.postAPIHost(body, "/pe-user-item-purchase", host)
                                : ConsoleAppLogin.NetEase.Http.postAPIPC(body, "/pe-user-item-purchase", false);
                            if (s == null) { w("[" + tag + "] null"); return; }
                            var jo = JObject.Parse(s);
                            var ents = jo["entities"] as JArray;
                            int n = ents == null ? 0 : ents.Count;
                            string hit = "";
                            if (ents != null)
                                foreach (var e in ents)
                                {
                                    var o = e as JObject;
                                    if (o == null) continue;
                                    string iid = (string)o["item_id"] ?? "";
                                    string nm = (string)o["res_name"] ?? (string)o["name"] ?? "";
                                    if (iid == resid) hit = "★命中 " + iid + " = " + nm;
                                }
                            w("[" + tag + "] code=" + jo["code"] + " 条数=" + n + " " + hit);
                            if (n > 0)
                            {
                                var o0 = ents[0] as JObject;
                                w("    样例：" + ((string)o0["item_id"] ?? "?") + " = " + ((string)o0["res_name"] ?? (string)o0["name"] ?? "?"));
                            }
                        }
                        catch (Exception ex) { w("[" + tag + "] 异常：" + ex.Message); }
                        Thread.Sleep(1200);
                    };

                    probe("PE fav=true ", "https://g79apigatewayobt.minecraft.cn",
                        "{\"channel_id\":5,\"length\":599,\"filter_my_favorite\":true,\"first_type_id\":6,\"order_flag\":1,\"offset\":0,\"asc_flag\":0,\"filter_type\":[],\"download_iids\":[]}");
                    probe("PE fav=false", "https://g79apigatewayobt.minecraft.cn",
                        "{\"channel_id\":5,\"length\":599,\"filter_my_favorite\":false,\"first_type_id\":6,\"order_flag\":1,\"offset\":0,\"asc_flag\":0,\"filter_type\":[],\"download_iids\":[]}");
                    probe("PE len=30   ", "https://g79apigatewayobt.minecraft.cn",
                        "{\"channel_id\":5,\"length\":30,\"filter_my_favorite\":false,\"first_type_id\":6,\"order_flag\":1,\"offset\":0,\"asc_flag\":0,\"filter_type\":[],\"download_iids\":[]}");
                    return;
                }

                // dbg 模式：试几种「查别人昵称 / 地图名」的接口，看哪个真能拿到
                if (id == "dbg")
                {
                    string uid = args.Length > 2 ? args[2] : "";
                    string resid = args.Length > 3 ? args[3] : "";
                    w("探测 uid=" + uid + " res_id=" + resid);

                    string[][] variants =
                    {
                        new[]{"PE other",   "{\"entity_id\":\"" + uid + "\",\"is_need_recharge_benefit\":1}"},
                        new[]{"PE other[]", "{\"entity_ids\":[\"" + uid + "\"],\"is_need_recharge_benefit\":1}"},
                        new[]{"PC detail",  "{\"entity_id\":\"" + uid + "\"}"},
                        new[]{"PC detail0", ""},
                    };
                    foreach (var v in variants)
                    {
                        try
                        {
                            string resp = v[0].StartsWith("PE")
                                ? ConsoleAppLogin.NetEase.Http.postAPIHost(v[1], "/user-detail/query/other", "https://g79apigatewayobt.minecraft.cn")
                                : ConsoleAppLogin.NetEase.Http.postAPIPC(v[1], "/user-detail", false);
                            string s = resp ?? "null";
                            w("[" + v[0] + "] " + (s.Length > 400 ? s.Substring(0, 400) + "…" : s));
                        }
                        catch (Exception ex) { w("[" + v[0] + "] 异常：" + ex.Message); }
                        Thread.Sleep(1500);
                    }

                    if (resid.Length > 0)
                    {
                        w("--- 地图名探测 ---");
                        string body = "{\"item_ids\":[\"" + resid + "\"],\"entity_ids\":[\"" + resid + "\"]}";
                        try
                        {
                            string s1 = ConsoleAppLogin.NetEase.Http.postAPIPC(body, "/item-buy-list/query/search-mcgame-items", false) ?? "null";
                            w("[PC mcgame-items] " + (s1.Length > 400 ? s1.Substring(0, 400) + "…" : s1));
                        }
                        catch (Exception ex) { w("[PC mcgame-items] 异常：" + ex.Message); }
                        try
                        {
                            string pb = "{\"item_ids\":[\"" + resid + "\"],\"channel_id\":5}";
                            w("[PE item] " + (ConsoleAppLogin.NetEase.Http.postAPIHost(pb, "/pe-item-detail/query/get", "https://g79apigatewayobt.minecraft.cn") ?? "null"));
                        }
                        catch (Exception ex) { w("[PE item] 异常：" + ex.Message); }
                    }
                    return;
                }

                // cands 模式：验证「跨大厅搜索候选 + 按候选查询」链路（对应 UI 的候选列表）
                if (id == "cands")
                {
                    string kw = args.Length > 2 ? args[2] : "";
                    w("候选搜索：" + kw);
                    var list = RoomIpQuery.SearchAll(kw, s => w(s));
                    w("共 " + list.Count + " 个候选");
                    foreach (var c in list) w("· " + c.Title + " ｜ " + c.Detail);
                    if (list.Count > 0)
                    {
                        w("→ 查询第一个：" + list[0].Title);
                        string a = RoomIpQuery.QueryCandidate(list[0], s => w("  [查询] " + s));
                        w("✓ 结果：" + a);
                    }
                    return;
                }

                if (id == "find")
                {
                    w("游戏版本：" + RoomIpQuery.GameVersion());
                    string listBody = "{\"room_name\":\"\",\"res_id\":\"\",\"version\":\"" +
                                      RoomIpQuery.GameVersion() + "\",\"offset\":0,\"length\":10}";
                    string resp = ConsoleAppLogin.NetEase.Http.postAPIPC(
                        listBody, "/online-lobby-room/query/search-by-name", false);
                    w("在线房间列表 → " + (resp == null ? "null" : (resp.Length > 300 ? resp.Substring(0, 300) + "…" : resp)));
                    var jo = JObject.Parse(resp ?? "{}");
                    var ents = jo["entities"] as JArray;
                    if (ents == null || ents.Count == 0) { w("✗ 没搜到在线房间"); return; }

                    int tried = 0;
                    Exception lastEx = null;
                    foreach (var e in ents)
                    {
                        var eo = e as JObject;
                        if (eo == null) continue;
                        if ((int?)eo["password"] == 1) continue;
                        string nm = (string)eo["room_name"] ?? "";
                        if (nm.Length == 0) continue;
                        if (++tried > 5) break;
                        if (tried > 1) { w("等 6 秒避开限速…"); Thread.Sleep(6000); }
                        w("试第 " + tried + " 个房间 " + nm + " …");
                        try
                        {
                            string addr = RoomIpQuery.Query(nm, "room", s => w("  [查询] " + s));
                            w("✓ 结果：" + addr);
                            return;
                        }
                        catch (Exception ce) { lastEx = ce; w("  失败：" + ce.Message + "，换下一个"); }
                    }
                    w("✗ 候选全部失败" + (lastEx != null ? "，最后错误：" + lastEx.Message : ""));
                    return;
                }

                string addr2 = RoomIpQuery.Query(id, kind, s => w("  [查询] " + s));
                w("✓ 结果：" + addr2);
            }
            catch (Exception ex)
            {
                w("✗ " + ex.Message);
            }
            finally
            {
                try { ConsoleAppLogin.NetEase.Http.CloseAuthserver(); } catch { }
                Environment.Exit(0);
            }
        }
    }
}
