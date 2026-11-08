using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace ModPcGUI
{
    /// <summary>联机大厅：地图（联机专区）</summary>
    internal sealed class LobbyMap
    {
        public string Id = "";
        public string Name = "";
        public string Author = "";
        public string Cover = "";
        public int Online;      // 当前在线人数
        public int Download;    // 下载/热度（不同接口给的不一定都有）
        public int Rooms;       // 当前开着的房间数（全服池才有）
        public string Edition = "PC";

        public string Title { get { return Name.Length > 0 ? Name : ("地图 " + ShortId(Id)); } }

        public string Detail
        {
            get
            {
                var sb = new System.Text.StringBuilder();
                if (Rooms > 0) sb.Append(Rooms).Append(" 间房　");
                if (Online > 0) sb.Append("在线 ").Append(Online).Append(" 人　");
                else if (Rooms > 0) sb.Append("无人在线　");
                if (Author.Length > 0) sb.Append("作者 ").Append(Author).Append("　");
                if (Download > 0) sb.Append("下载 ").Append(Download);
                return sb.ToString().TrimEnd('　');
            }
        }

        private static string ShortId(string id)
        {
            if (string.IsNullOrEmpty(id)) return "?";
            return id.Length <= 6 ? id : id.Substring(id.Length - 6);
        }
    }

    /// <summary>联机大厅：某个地图下的一间房</summary>
    internal sealed class LobbyRoom
    {
        public string Id = "";
        public string Name = "";        // 房间号（房名）
        public string ResId = "";
        public string MapId = "";
        public string Manifest = "";
        public string OwnerId = "";
        public string OwnerName = "";
        public string MapName = "";
        public int Members;
        public int MaxMembers;
        public bool HasPwd;
        public string Edition = "PC";

        public string Detail
        {
            get
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("房号 ").Append(Name.Length > 0 ? Name : Id);
                sb.Append("　房主 ").Append(OwnerName.Length > 0 ? OwnerName : (OwnerId.Length > 0 ? OwnerId : "未知"));
                sb.Append("　").Append(Members).Append("/").Append(MaxMembers > 0 ? MaxMembers : 10).Append(" 人");
                if (HasPwd) sb.Append("　有密码");
                return sb.ToString();
            }
        }
    }

    /// <summary>联机大厅：房间内的一名玩家</summary>
    internal sealed class LobbyMember
    {
        public string Uid = "";
        public string Nick = "";
    }

    /// <summary>
    /// 联机大厅浏览（地图列表 / 房间列表 / 玩家列表）。
    /// 全部走自己的登录会话，不依赖第三方中转：
    ///   PC 地图目录  POST x19 /item/query/search-by-keyword
    ///   PE 地图目录  POST g79 /pe-item/query/search-lobby-res-by-keyword
    ///   房间列表     POST x19|g79 /online-lobby-room/query/list-room-by-res-id
    ///   玩家列表     POST x19 /online-lobby-member/query/list-by-room-id
    /// </summary>
    internal static class LobbyHall
    {
        /// <summary>排序方式</summary>
        public const int SortHot = 0;      // 热门（接口默认）
        public const int SortOnline = 1;   // 在线人数
        public const int SortNew = 2;      // 最新

        // ───────────── 全服在线地图池（不带关键词，按真实在服人数）─────────────

        /// <summary>
        /// 抓全服当前在线房间（空房名搜索可列全部），按 res_id 聚合：
        /// 得到「哪张地图现在开着的房间最多、在线人数最多」。
        /// 这是最贴近「全服热门地图池」的数据，不依赖关键词。
        /// </summary>
        internal static List<LobbyMap> HotMaps(bool pe, int sort, int pages, Action<string> log)
        {
            if (log == null) log = delegate { };
            var byRes = new Dictionary<string, int[]>();   // resId → [房间数, 在线人数]
            int totalRooms = 0;

            pages = Math.Max(1, Math.Min(pages, 6));
            for (int p = 0; p < pages; p++)
            {
                string body = "{\"room_name\":\"\",\"res_id\":\"\",\"version\":\"" + Esc(RoomIpQuery.GameVersion()) +
                              "\",\"offset\":" + (p * 50) + ",\"length\":50}";
                string resp = RoomIpQuery.PostText(pe, "/online-lobby-room/query/search-by-name", body);
                var jo = ParseJo(resp);

                // PE 的在线房间目录在 g79mclobt（PostText(pe) 走的是 apigateway，可能拿不到），失败就换 host
                if (pe && (jo == null || jo["entities"] == null))
                    jo = ParseJo(RoomIpQuery.PostHostText("https://g79mclobt.minecraft.cn",
                        "/online-lobby-room/query/search-by-name", body));

                if (jo == null) { log("  第 " + (p + 1) + " 页无响应"); break; }

                int code = (int?)jo["code"] ?? -1;
                if (code == 2311)
                {
                    log("  搜索限速，等 5 秒重试…");
                    Thread.Sleep(5000);
                    p--;
                    continue;
                }

                var ents = jo["entities"] as JArray;
                if (ents == null || ents.Count == 0) break;

                foreach (var e in ents)
                {
                    var o = e as JObject;
                    if (o == null) continue;
                    totalRooms++;
                    string rs = (string)o["res_id"] ?? "";
                    if (rs.Length == 0) continue;
                    var mu = o["member_uids"] as JArray;
                    int mem = mu != null ? mu.Count : ((int?)o["cur_num"] ?? 0);
                    if (!byRes.ContainsKey(rs)) byRes[rs] = new int[2];
                    byRes[rs][0]++;
                    byRes[rs][1] += mem;
                }

                if (ents.Count < 50) break;      // 最后一页
                Thread.Sleep(1500);
            }

            log("  在线房间 " + totalRooms + " 间，涉及 " + byRes.Count + " 张地图");
            if (byRes.Count == 0) return new List<LobbyMap>();

            // 排序：热门=房间多的优先（房间一样再看人数）；在线人数=纯人数
            IEnumerable<KeyValuePair<string, int[]>> ordered =
                sort == SortOnline
                    ? byRes.OrderByDescending(x => x.Value[1]).ThenByDescending(x => x.Value[0])
                    : byRes.OrderByDescending(x => x.Value[0]).ThenByDescending(x => x.Value[1]);

            var picked = ordered.Take(40).ToList();
            var names = ResolveMapNames(picked.Select(x => x.Key).ToList(), pe, log);

            var list = new List<LobbyMap>();
            foreach (var kv in picked)
            {
                string nm;
                names.TryGetValue(kv.Key, out nm);
                list.Add(new LobbyMap
                {
                    Id = kv.Key,
                    Name = nm ?? "",
                    Rooms = kv.Value[0],
                    Online = kv.Value[1],
                    Edition = pe ? "PE" : "PC"
                });
            }
            return list;
        }

        /// <summary>res_id 批量反查地图名（g79 灰度清单接口，PC/PE 通用）；失败的单条再走 PC 组件接口。</summary>
        private static Dictionary<string, string> ResolveMapNames(List<string> resIds, bool pe, Action<string> log)
        {
            var map = new Dictionary<string, string>();
            if (resIds == null || resIds.Count == 0) return map;

            foreach (var chunk in Chunk(resIds, 10))
            {
                string q = string.Join(",", chunk.Select(x => "\"" + Esc(x) + "\""));
                try
                {
                    string resp = RoomIpQuery.PostHostText("https://g79mclobtgray.nie.netease.com",
                        "/pe-item/query/search-lobby-by-id-list", "{\"item_ids\":[" + q + "]}");
                    var jo = ParseJo(resp);
                    var ents = jo == null ? null : jo["entities"] as JArray;
                    if (ents != null)
                        foreach (var e in ents)
                        {
                            var o = e as JObject;
                            if (o == null) continue;
                            string iid = (string)o["iid"] ?? "";
                            string nm = (string)o["res_name"] ?? (string)o["name"] ?? "";
                            if (iid.Length > 0 && nm.Length > 0) map[iid] = nm;
                        }
                }
                catch (Exception ex) { log("  地图名反查失败：" + ex.Message); }
                Thread.Sleep(600);
            }

            // 还缺的，单独问 PC 组件接口
            foreach (var id in resIds.Where(x => !map.ContainsKey(x)).Take(12))
            {
                try
                {
                    string resp = RoomIpQuery.PostText(false, "/item/query/search-mcgame-item-list",
                        "{\"item_ids\":[\"" + Esc(id) + "\"],\"entity_ids\":[]}");
                    var jo = ParseJo(resp);
                    var ents = jo == null ? null : jo["entities"] as JArray;
                    if (ents != null && ents.Count > 0)
                    {
                        string nm = (string)(ents[0] as JObject)?["name"] ?? "";
                        string iid = (string)(ents[0] as JObject)?["iid"] ?? "";
                        if (iid == id && nm.Length > 0) map[id] = nm;
                    }
                }
                catch { }
            }

            log("  地图名解析 " + map.Count + "/" + resIds.Count);
            return map;
        }

        private static IEnumerable<List<T>> Chunk<T>(List<T> src, int size)
        {
            for (int i = 0; i < src.Count; i += size)
                yield return src.GetRange(i, Math.Min(size, src.Count - i));
        }

        // ───────────── 地图（联机专区）─────────────

        /// <summary>拉取联机专区（可玩的地图）列表。</summary>
        internal static List<LobbyMap> SearchMaps(string keyword, int sort, bool pe, int offset, int length,
                                                  Action<string> log)
        {
            if (log == null) log = delegate { };
            keyword = (keyword ?? "").Trim();
            var list = new List<LobbyMap>();

            string body;
            if (pe)
            {
                // 只用 sort_type=1：PE 换 sort_type 会返回另一批数据、丢掉 cur_num（在线人数）
                body = "{\"length\":" + length + ",\"keyword\":\"" + Esc(keyword) + "\",\"offset\":" + offset +
                       ",\"sort_type\":1,\"asc_flag\":1}";
            }
            else
            {
                // PC 也只能用 sort_type=1（换成 2/3 会变成「最新」等另一套排序）
                body = "{\"item_type\":2,\"keyword\":\"" + Esc(keyword) + "\",\"master_type_id\":\"0\"," +
                       "\"network_tag\":false,\"year\":0,\"sort_type\":1,\"order\":0," +
                       "\"price_type\":0,\"is_has\":false,\"offset\":" + offset + ",\"length\":" + length + "}";
            }

            string resp = pe
                ? RoomIpQuery.PostText(true, "/pe-item/query/search-lobby-res-by-keyword", body)
                : RoomIpQuery.PostText(false, "/item/query/search-by-keyword", body);

            var jo = ParseJo(resp);
            if (jo == null)
            {
                log("  地图列表无响应：" + Head(resp, 200));
                return list;
            }

            var ents = jo["entities"] as JArray;
            if (ents == null || ents.Count == 0)
            {
                log("  地图列表为空（code=" + (jo["code"] ?? "?") + "）：" + Head(resp, 300));
                return list;
            }

            foreach (var e in ents)
            {
                var o = e as JObject;
                if (o == null) continue;
                var m = new LobbyMap
                {
                    Id = First(o, "item_id", "iid", "res_id", "entity_id", "id"),
                    Name = First(o, "res_name", "name", "title"),
                    Author = First(o, "developer_name", "author", "author_name", "creator"),
                    Cover = First(o, "image_url", "title_image_url", "cover", "icon_url"),
                    Online = Num(o, "cur_num", "online_num", "online_count", "online"),
                    Download = Num(o, "download_num", "download_count", "like_num", "subscribe_num"),
                    Edition = pe ? "PE" : "PC"
                };
                if (m.Id.Length > 0) list.Add(m);
            }
            log("  地图 " + list.Count + " 个");

            // 在线人数排序：服务端不提供按在线排序（换 sort_type 会换数据集），这里按 cur_num 客户端排
            if (sort == SortOnline)
            {
                if (list.Any(x => x.Online > 0))
                    list.Sort((a, b) => b.Online.CompareTo(a.Online));
                else
                    log("  该端接口没有在线人数数据，保持原顺序");
            }
            return list;
        }

        // ───────────── 房间列表 ─────────────

        /// <summary>拉取某张地图（联机专区）下的房间列表。</summary>
        internal static List<LobbyRoom> ListRooms(string resId, bool pe, int offset, int length,
                                                  string mapName, Action<string> log)
        {
            if (log == null) log = delegate { };
            var list = new List<LobbyRoom>();
            if (string.IsNullOrEmpty(resId)) return list;

            string body = "{\"res_id\":\"" + Esc(resId) + "\",\"version\":\"" + Esc(RoomIpQuery.GameVersion()) +
                          "\",\"with_friend\":true,\"offset\":" + offset + ",\"length\":" + length + "}";
            string resp = RoomIpQuery.PostText(pe, "/online-lobby-room/query/list-room-by-res-id", body);

            var jo = ParseJo(resp);
            if (jo == null) { log("  房间列表无响应"); return list; }

            var ents = jo["entities"] as JArray;
            if (ents == null) { log("  该地图暂时没有房间（code=" + (jo["code"] ?? "?") + "）"); return list; }

            foreach (var e in ents)
            {
                var o = e as JObject;
                if (o == null) continue;
                var r = new LobbyRoom
                {
                    Id = (string)o["entity_id"] ?? "",
                    Name = (string)o["room_name"] ?? "",
                    ResId = (string)o["res_id"] ?? resId,
                    MapId = resId,
                    Manifest = (string)o["lobby_manifest_version"] ?? "",
                    OwnerId = (string)o["owner_id"] ?? "",
                    MapName = mapName ?? "",
                    MaxMembers = (int?)o["max_count"] ?? 10,
                    HasPwd = (int?)o["password"] == 1,
                    Edition = pe ? "PE" : "PC"
                };
                var mu = o["member_uids"] as JArray;
                r.Members = mu == null ? ((int?)o["cur_num"] ?? 0) : mu.Count;
                if (r.Id.Length > 0) list.Add(r);
            }
            log("  房间 " + list.Count + " 个");

            // 房主昵称（能拿到就补，失败不影响）
            FillOwnerNames(list, log);
            return list;
        }

        private static void FillOwnerNames(List<LobbyRoom> rooms, Action<string> log)
        {
            if (rooms == null || rooms.Count == 0) return;
            try
            {
                var names = RoomIpQuery.NicknamesOf(rooms.Select(r => r.OwnerId).Where(s => s.Length > 0).Distinct().Take(20));
                foreach (var r in rooms)
                {
                    string n;
                    if (names.TryGetValue(r.OwnerId, out n)) r.OwnerName = n;
                }
            }
            catch (Exception ex) { log("  房主昵称补全失败：" + ex.Message); }
        }

        // ───────────── 玩家列表 / 房间详情 ─────────────

        /// <summary>拉取房间内玩家（房间详情页用）。</summary>
        internal static List<LobbyMember> ListMembers(string roomId, bool pe, Action<string> log)
        {
            if (log == null) log = delegate { };
            var list = new List<LobbyMember>();
            if (string.IsNullOrEmpty(roomId)) return list;

            string body = "{\"room_id\":\"" + Esc(roomId) + "\"}";
            string resp = RoomIpQuery.PostText(pe, "/online-lobby-member/query/list-by-room-id", body);

            var jo = ParseJo(resp);
            if (jo == null) { log("  玩家列表无响应"); return list; }
            var ents = jo["entities"] as JArray;
            if (ents == null) { log("  玩家列表为空（code=" + (jo["code"] ?? "?") + "）"); return list; }

            foreach (var e in ents)
            {
                var o = e as JObject;
                if (o == null) continue;
                var m = new LobbyMember
                {
                    Uid = First(o, "member_id", "entity_id", "uid", "id"),
                    Nick = First(o, "nickname", "name")
                };
                if (m.Uid.Length > 0) list.Add(m);
            }

            if (list.Any(x => x.Nick.Length == 0))
            {
                try
                {
                    var names = RoomIpQuery.NicknamesOf(list.Where(x => x.Nick.Length == 0).Select(x => x.Uid));
                    foreach (var m in list)
                    {
                        string n;
                        if (m.Nick.Length == 0 && names.TryGetValue(m.Uid, out n)) m.Nick = n;
                    }
                }
                catch { }
            }
            log("  玩家 " + list.Count + " 人");
            return list;
        }

        // ───────────── 工具 ─────────────

        private static JObject ParseJo(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            try { return JObject.Parse(s); } catch { return null; }
        }

        private static string Head(string s, int n)
        {
            if (string.IsNullOrEmpty(s)) return "(空)";
            return s.Length <= n ? s : s.Substring(0, n) + "…";
        }

        private static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        private static string First(JObject o, params string[] keys)
        {
            foreach (var k in keys)
            {
                var t = o[k];
                if (t == null) continue;
                string s = t.Type == JTokenType.String ? (string)t : t.ToString();
                if (!string.IsNullOrEmpty(s)) return s;
            }
            return "";
        }

        private static int Num(JObject o, params string[] keys)
        {
            foreach (var k in keys)
            {
                var t = o[k];
                if (t == null) continue;
                int v;
                if (int.TryParse(t.ToString(), out v)) return v;
            }
            return 0;
        }
    }
}
