using System;
using System.Security.Cryptography;
using System.Text;

namespace ModPcGUI.PeAuth
{
    /// <summary>
    /// 网易网关的加密 / 签名 / 动态 token。
    /// 逐字节对照 Go 版 astra/netease/{crypto,token,pe}.go 移植。
    ///
    /// 两块核心：
    ///   1) ComputeDynamicToken —— 每个 x19 请求的 user-token 头
    ///   2) PeSignCompute       —— PE 认证的 16 字节签名
    /// 任何一个字节算错，网关都会返回验签失败。
    /// </summary>
    public static class PeCrypto
    {
        const string Alnum = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        const string TokenSalt = "0eGsBkhl";

        /// <summary>x19 网关加密体用的 16 个固定密钥（16 字节 ASCII）。</summary>
        static readonly string[] AesKeys = {
            "MK6mipwmOUedplb6", "OtEylfId6dyhrfdn", "VNbhn5mvUaQaeOo9", "bIEoQGQYjKd02U0J",
            "fuaJrPwaH2cfXXLP", "LEkdyiroouKQ4XN1", "jM1h27H4UROu427W", "DhReQada7gZybTDk",
            "ZGXfpSTYUvcdKqdY", "AZwKf7MWZrJpGR5W", "amuvbcHw38TcSyPU", "SI4QotspbjhyFdT0",
            "VP4dhjKnDGlSJtbB", "UXDZx4KhZywQ2tcn", "NIK73ZNvNqzva4kd", "WeiW7qU766Q1YQZI",
        };

        /// <summary>G79（PE / 安卓认证器）用的 16 个十六进制密钥。</summary>
        static readonly string[] KeysG79V12 = {
            "60F1E0D1FD635362430747215CF1C2FF", "EA5B62D27D0338374852C4B9469D7AC6",
            "17238D55501C5F020B155FB3303591E6", "8C5CEAE0F443E006A050266F73ADD5B0",
            "1C02CE22FB22F0E72060217418F351F3", "9A01773FEBB0CFE0EBDBF37F4D23C27F",
            "43F32300BF252CC320E2572ACE766367", "07F161011B3101F1ED0301735631E734",
            "0454E7707A5F37565601E100406060AF", "647554BAD3100C43C16660F002CC10F3",
            "E157213170F842382032564265B0B043", "914FC59311B04151393EF6896A847636",
            "0710C0205D224237025323265C145FA1", "054E6F01165267025C3111F562A921E9",
            "722D1789E792E2CA0D5322211FD0F5AE", "91F7C751FCF671F34943430772341799",
        };

        static readonly uint[] SignShiftTable = { 218760705, 235472130, 51054340, 84609027, 235603713 };

        static readonly uint[] SignConstantTable = {
            4129170786, 3225465664, 643717713, 3921069994, 2763975236, 1272893353, 4139469664,
            3200236656, 1700485571, 2399980690, 4293915773, 2240044497, 681279174, 3936430074,
            3572445317, 76029189, 568446438, 3275163606, 4107603335, 1163531501, 4129170786,
            3225465664, 643717713, 3921069994, 2763975236, 1272893353, 4139469664, 3200236656,
            1700485571, 2399980690, 4293915773, 2240044497, 681279174, 3936430074, 3572445317,
            76029189, 568446438, 3275163606, 4107603335, 1163531501,
        };

        static readonly Random Rng = new Random();

        // ───────────────────────── 基础工具 ─────────────────────────

        public static string Md5Hex(string s)
        {
            using (var md5 = MD5.Create())
            {
                var b = md5.ComputeHash(Encoding.UTF8.GetBytes(s));
                var sb = new StringBuilder(32);
                foreach (var x in b) sb.Append(x.ToString("x2"));
                return sb.ToString();
            }
        }

        public static byte[] Md5Raw(string s)
        {
            using (var md5 = MD5.Create())
                return md5.ComputeHash(Encoding.UTF8.GetBytes(s));
        }

        public static string RandAlnum(int n)
        {
            var sb = new StringBuilder(n);
            for (int i = 0; i < n; i++) sb.Append(Alnum[Rng.Next(Alnum.Length)]);
            return sb.ToString();
        }

        public static int RandIntn(int n)
        {
            if (n <= 0) return 0;
            lock (Rng) return Rng.Next(n);
        }

        public static string BytesToHex(byte[] b, bool upper = false)
        {
            var sb = new StringBuilder(b.Length * 2);
            foreach (var x in b) sb.Append(x.ToString(upper ? "X2" : "x2"));
            return sb.ToString();
        }

        public static byte[] HexToBytes(string s)
        {
            var clean = new StringBuilder(s.Length);
            foreach (var ch in s)
                if (ch != ' ' && ch != '\n' && ch != '\r' && ch != '\t') clean.Append(ch);
            var t = clean.ToString();
            if (t.Length % 2 != 0) throw new FormatException("hex 长度为奇数");
            var outb = new byte[t.Length / 2];
            for (int i = 0; i < outb.Length; i++)
                outb[i] = Convert.ToByte(t.Substring(i * 2, 2), 16);
            return outb;
        }

        /// <summary>把字符串的每个字节展开成 01 位串（高位在前）。</summary>
        static string ToBinary(string s)
        {
            var sb = new StringBuilder(s.Length * 8);
            foreach (var ch in s)
                for (int bit = 7; bit >= 0; bit--)
                    sb.Append((ch & (1 << bit)) != 0 ? '1' : '0');
            return sb.ToString();
        }

        // ──────────────────── ★ ComputeDynamicToken ────────────────────
        //
        // MD5(token) → 拼 body/salt/path → MD5 → 二进制左移 6 位 → 逐字节异或
        // → base64 → 取前 16 → '+'→'m'、'/'→'o' → 末尾加 '1'
        public static string ComputeDynamicToken(string path, string body, string token, bool tokenIsMd5Hex = false)
        {
            string tokenMd5 = tokenIsMd5Hex ? token : Md5Hex(token);
            if (tokenIsMd5Hex && tokenMd5.Length != 32)
                throw new ArgumentException("token_is_md5_hex 需要 32 位 hex");

            string raw = tokenMd5 + body + TokenSalt + path;
            raw = raw.TrimEnd('?');

            string buf = Md5Hex(raw);       // 32 个 hex 字符
            string bits = ToBinary(buf);    // 256 位

            // 循环左移 6 位
            bits = bits.Substring(6) + bits.Substring(0, 6);

            var bufBytes = Encoding.ASCII.GetBytes(buf);   // 32 字节
            for (int i = 0; i < bufBytes.Length; i++)
            {
                int mask = 0;
                for (int j = 0; j < 8; j++)
                    if (bits[i * 8 + (7 - j)] == '1') mask |= 1 << j;
                bufBytes[i] ^= (byte)mask;
            }

            string b64 = Convert.ToBase64String(bufBytes);
            if (b64.Length > 16) b64 = b64.Substring(0, 16);
            b64 = b64.Replace('+', 'm').Replace('/', 'o');
            return b64 + "1";
        }

        // ──────────────────── AES-CBC（不填充） ────────────────────

        public static byte[] AesCbcEncrypt(byte[] key, byte[] iv, byte[] plain)
        {
            if (plain.Length % 16 != 0) throw new ArgumentException("CBC 需要 16 字节对齐");
            using (var aes = Aes.Create())
            {
                aes.Key = key; aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.None;
                using (var enc = aes.CreateEncryptor())
                    return enc.TransformFinalBlock(plain, 0, plain.Length);
            }
        }

        public static byte[] AesCbcDecrypt(byte[] key, byte[] iv, byte[] ct)
        {
            if (ct.Length % 16 != 0) throw new ArgumentException("CBC 密文需 16 字节对齐");
            using (var aes = Aes.Create())
            {
                aes.Key = key; aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.None;
                using (var dec = aes.CreateDecryptor())
                    return dec.TransformFinalBlock(ct, 0, ct.Length);
            }
        }

        // ──────────────────── x19 请求体加解密 ────────────────────

        /// <summary>加密请求体：iv(16) + AES-CBC + flag(1)。</summary>
        public static byte[] EncryptBody(byte[] plaintext, int padLen, byte typ)
        {
            string pad = RandAlnum(padLen);
            string iv = RandAlnum(16);

            var buf = new System.Collections.Generic.List<byte>(plaintext);
            buf.AddRange(Encoding.ASCII.GetBytes(pad));
            while (buf.Count % 16 != 0) buf.Add(0);

            int slot = RandIntn(15);
            byte flag = (byte)((slot << 4) | typ);

            var ct = AesCbcEncrypt(Encoding.ASCII.GetBytes(AesKeys[slot]), Encoding.ASCII.GetBytes(iv), buf.ToArray());

            var outb = new byte[16 + ct.Length + 1];
            Buffer.BlockCopy(Encoding.ASCII.GetBytes(iv), 0, outb, 0, 16);
            Buffer.BlockCopy(ct, 0, outb, 16, ct.Length);
            outb[outb.Length - 1] = flag;
            return outb;
        }

        /// <summary>解密响应体（从尾部数 16 个非零字节，其余丢弃）。</summary>
        public static byte[] DecryptBody(byte[] blob)
        {
            if (blob.Length < 18) throw new ArgumentException("密文太短");
            var iv = new byte[16];
            Buffer.BlockCopy(blob, 0, iv, 0, 16);
            var ct = new byte[blob.Length - 17];
            Buffer.BlockCopy(blob, 16, ct, 0, ct.Length);
            byte flag = blob[blob.Length - 1];
            int slot = (flag >> 4) & 0xF;

            var pt = AesCbcDecrypt(Encoding.ASCII.GetBytes(AesKeys[slot]), iv, ct);

            int n = 0, i = pt.Length - 1;
            while (n < 16 && i >= 0) { if (pt[i] != 0) n++; i--; }
            var outb = new byte[i + 1];
            Buffer.BlockCopy(pt, 0, outb, 0, i + 1);
            return outb;
        }

        // ──────────────────── G79（PE 认证）加解密 ────────────────────

        static byte[] PickKeyG79(byte flag)
        {
            return HexToBytes(KeysG79V12[(flag >> 4) & 0xF]);
        }

        /// <summary>PE 认证请求体：iv(16) + AES-CBC + flag(1)。</summary>
        public static byte[] EncryptG79V12(byte[] plain)
        {
            int paddedLen = ((plain.Length + 16 + 15) / 16) * 16;
            var buf = new byte[paddedLen];
            Buffer.BlockCopy(plain, 0, buf, 0, plain.Length);

            var pad = Encoding.ASCII.GetBytes(RandAlnum(16));
            Buffer.BlockCopy(pad, 0, buf, plain.Length, Math.Min(16, paddedLen - plain.Length));

            byte flag = (byte)((RandIntn(15) << 4) | 0xC);
            var key = PickKeyG79(flag);
            var iv = Encoding.ASCII.GetBytes(RandAlnum(16));

            var ct = AesCbcEncrypt(key, iv, buf);

            var outb = new byte[16 + ct.Length + 1];
            Buffer.BlockCopy(iv, 0, outb, 0, 16);
            Buffer.BlockCopy(ct, 0, outb, 16, ct.Length);
            outb[outb.Length - 1] = flag;
            return outb;
        }

        static string StripX19Pad(byte[] pt)
        {
            if (pt.Length == 0) return "";
            int n = 0, i = pt.Length - 1;
            while (n < 16 && i >= 0) { if (pt[i] != 0) n++; i--; }
            return Encoding.UTF8.GetString(pt, 0, i + 1);
        }

        public static string DecryptG79V12(byte[] blob)
        {
            if (blob.Length < 18) throw new ArgumentException("g79 密文太短");
            byte flag = blob[blob.Length - 1];
            var key = PickKeyG79(flag);
            var iv = new byte[16];
            Buffer.BlockCopy(blob, 0, iv, 0, 16);
            var ct = new byte[blob.Length - 17];
            Buffer.BlockCopy(blob, 16, ct, 0, ct.Length);
            return StripX19Pad(AesCbcDecrypt(key, iv, ct));
        }

        /// <summary>大响应体变体：保留尾部 127 字节的额外信息。</summary>
        public static string DecryptG79V12Large(byte[] blob)
        {
            if (blob.Length < 18) throw new ArgumentException("g79 密文太短");
            byte flag = blob[blob.Length - 1];
            var key = PickKeyG79(flag);
            var iv = new byte[16];
            Buffer.BlockCopy(blob, 0, iv, 0, 16);
            var ct = new byte[blob.Length - 17];
            Buffer.BlockCopy(blob, 16, ct, 0, ct.Length);
            var pt = AesCbcDecrypt(key, iv, ct);

            if (pt.Length == 0) return "";
            int num = pt.Length - 1;
            while (num >= 0 && pt[num] == 0) num--;
            int keep = num - 127;
            if (keep < 0) keep = 0;
            return Encoding.UTF8.GetString(pt, 0, keep);
        }

        // ──────────────────── ★ PeSignCompute ────────────────────
        //
        // 自定义 32 位哈希：4 轮 4 状态，逐字对照 C++/Go 实现。
        // offset ∈ [0,4]，PE 认证固定用 (4, 7)。
        public static byte[] PeSignCompute(string message, int offset, int rounds)
        {
            if (string.IsNullOrEmpty(message) || offset < 0 || offset >= 5) return null;

            var msg = new System.Collections.Generic.List<byte>(Encoding.UTF8.GetBytes(message));
            while (msg.Count % 4 != 0) msg.Add((byte)'0');

            var list = new System.Collections.Generic.List<uint>(msg.Count / 4 + 64);
            for (int i = 0; i + 3 < msg.Count; i += 4)
                list.Add((uint)(msg[i] << 24 | msg[i + 1] << 16 | msg[i + 2] << 8 | msg[i + 3]));

            while (list.Count % 64 != 0) list.Add(2882398599u);

            uint shift = SignShiftTable[offset];
            int s0 = (int)(shift & 0x1F);
            int s1 = (int)((shift >> 8) & 0x1F);
            int s2 = (int)((shift >> 16) & 0x1F);
            int s3 = (int)((shift >> 24) & 0x1F);

            int bo = offset * 4;
            uint c0 = SignConstantTable[bo];
            uint c1 = SignConstantTable[bo + 1];
            uint c2 = SignConstantTable[bo + 2];
            uint c3 = SignConstantTable[bo + 3];

            uint a = 1732584193u, b = 4023233417u, c = 2562383102u, d = 271733878u;

            unchecked
            {
                for (int r = 0; r < rounds; r++)
                {
                    for (int k = 0; k + 3 < list.Count; k += 4)
                    {
                        uint w = list[k];
                        uint t0 = w + c0 + ((b & c) | (d & ~b)) + a;
                        a = b + (t0 << s0);
                        uint t1 = a + w + c1 + ((c & ~d) | (d & b));
                        b += t1 << s1;
                        uint t2 = a + w + c2 + (b ^ d ^ c);
                        c = b + (t2 << s2);
                        uint t3 = a + w + c3 + (c ^ (b | d));
                        d = b + (t3 << s3);
                    }
                }
            }

            var outb = new byte[16];
            PutLE(outb, 0, a);
            PutLE(outb, 4, b);
            PutLE(outb, 8, c);
            PutLE(outb, 12, d);
            return outb;
        }

        static void PutLE(byte[] buf, int off, uint v)
        {
            buf[off] = (byte)v;
            buf[off + 1] = (byte)(v >> 8);
            buf[off + 2] = (byte)(v >> 16);
            buf[off + 3] = (byte)(v >> 24);
        }
    }
}
