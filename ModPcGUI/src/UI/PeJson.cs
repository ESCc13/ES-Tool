using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;

namespace ModPcGUI.PeAuth
{
    /// <summary>
    /// 网易响应的 JSON 处理。
    ///
    /// 用 net48 自带的 JavaScriptSerializer，不引第三方包。
    /// 重点是 <see cref="FindStringDeep"/> —— 对应 Go 版 lobby.go:156 的 findString：
    /// 网易把 server_host / server_port 塞在 entities[0] 或 data 里，
    /// 只查顶层永远是空字符串，然后误报「获取地址失败」。
    /// </summary>
    public static class PeJson
    {
        static readonly JavaScriptSerializer Ser = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        public static object Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return Ser.DeserializeObject(json); }
            catch { return null; }
        }

        public static string Serialize(object o)
        {
            return Ser.Serialize(o);
        }

        /// <summary>取字典里的字符串（数字也转字符串）。</summary>
        public static string Str(IDictionary<string, object> d, string key)
        {
            if (d == null || !d.ContainsKey(key) || d[key] == null) return "";
            var v = d[key];
            if (v is string s) return s;
            if (v is int i) return i.ToString(CultureInfo.InvariantCulture);
            if (v is long l) return l.ToString(CultureInfo.InvariantCulture);
            if (v is double db) return ((long)db).ToString(CultureInfo.InvariantCulture);
            if (v is bool b) return b ? "true" : "false";
            return v.ToString();
        }

        public static long Num(IDictionary<string, object> d, string key)
        {
            if (d == null || !d.ContainsKey(key) || d[key] == null) return 0;
            var v = d[key];
            if (v is int i) return i;
            if (v is long l) return l;
            if (v is double db) return (long)db;
            if (v is string s) { long n; return long.TryParse(s.Trim(), out n) ? n : 0; }
            return 0;
        }

        /// <summary>
        /// ★ 递归深搜字段（对应 Go 的 findString / findStringDeep）。
        /// 先查顶层，再按常见容器顺序（data/result/entities/...）下钻，最后遍历其余字段。
        /// </summary>
        public static string FindStringDeep(object root, string key, int depth = 0)
        {
            if (root == null || depth > 8) return "";

            if (root is IDictionary<string, object> d)
            {
                if (d.ContainsKey(key))
                {
                    var direct = Str(d, key);
                    if (!string.IsNullOrEmpty(direct)) return direct;
                }
                foreach (var container in new[] { "data", "result", "entities", "entity", "room", "server", "content", "detail" })
                {
                    if (d.ContainsKey(container))
                    {
                        var got = FindStringDeep(d[container], key, depth + 1);
                        if (!string.IsNullOrEmpty(got)) return got;
                    }
                }
                foreach (var kv in d)
                {
                    if (kv.Key == key) continue;
                    var got = FindStringDeep(kv.Value, key, depth + 1);
                    if (!string.IsNullOrEmpty(got)) return got;
                }
                return "";
            }

            if (root is IEnumerable list && !(root is string))
            {
                foreach (var item in list)
                {
                    var got = FindStringDeep(item, key, depth + 1);
                    if (!string.IsNullOrEmpty(got)) return got;
                }
            }
            return "";
        }

        /// <summary>第一个非空值（对应 Go 的 firstString）。</summary>
        public static string FirstString(params string[] vals)
        {
            foreach (var v in vals) if (!string.IsNullOrEmpty(v)) return v;
            return "";
        }

        /// <summary>转成字典（JavaScriptSerializer 返回的可能是 Dictionary&lt;string,object&gt;）。</summary>
        public static IDictionary<string, object> AsDict(object o)
        {
            return o as IDictionary<string, object>;
        }

        /// <summary>取 entities 数组（网易列表响应的固定字段）。</summary>
        public static List<object> AsList(object o)
        {
            if (o is IEnumerable e && !(o is string))
            {
                var outl = new List<object>();
                foreach (var x in e) outl.Add(x);
                return outl;
            }
            return new List<object>();
        }

        /// <summary>把响应里出现过的字段名收集起来（成功但没拿到地址时打日志用）。</summary>
        public static List<string> CollectKeys(object root)
        {
            var seen = new HashSet<string>();
            Collect(root, seen, 0);
            var list = new List<string>(seen);
            list.Sort(StringComparer.Ordinal);
            if (list.Count > 24) list = list.GetRange(0, 24);
            return list;
        }

        static void Collect(object v, HashSet<string> seen, int depth)
        {
            if (v == null || depth > 6) return;
            if (v is IDictionary<string, object> d)
            {
                foreach (var kv in d) { seen.Add(kv.Key); Collect(kv.Value, seen, depth + 1); }
            }
            else if (v is IEnumerable e && !(v is string))
            {
                foreach (var x in e) Collect(x, seen, depth + 1);
            }
        }
    }
}
