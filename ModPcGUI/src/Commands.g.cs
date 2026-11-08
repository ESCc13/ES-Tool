// 自动生成：命令目录。勿手改。
using System.Collections.Generic;

namespace ModPcGUI
{
    public sealed class CmdDef
    {
        public string Name = "";
        public string Label = "";
        public string Category = "";
        public string[] Prompts = new string[0];
        public CmdDef(string n, string l, string c, string[] p)
        { Name = n; Label = l; Category = c; Prompts = p; }
    }

    public static class Commands
    {
        public static readonly string[] CategoryOrder = new string[] {
            "账号登录", "启动游戏", "联机房间", "搜索查询", "模组组件", "封禁举报", "服务器/锁服", "假人/机器人", "聊天/好友", "开发调试", "其他"
        };

        public static readonly List<CmdDef> All = new List<CmdDef>();

        static Commands()
        {
            Add("anew", "重新登录", "账号登录", new string[0]);
            Add("delete", "退出登录", "账号登录", new string[0]);
            Add("getdtoken", "获取DToken", "账号登录", new string[0]);
            Add("getgame", "获取游戏", "账号登录", new string[0]);
            Add("geth5token", "获取CPPtoken", "账号登录", new string[0]);
            Add("getname", "获得名字", "账号登录", new string[0]);
            Add("gettoken", "获取token", "账号登录", new string[0]);
            Add("help", "显示该列表", "账号登录", new string[0]);
            Add("nameupdate", "改名", "账号登录", new string[] { "输入新的名字：" });
            Add("setname", "起名", "账号登录", new string[] { "输入你要设置的昵称：" });
            Add("setpath", "设置游戏路径", "账号登录", new string[] { "输入windowsmc的路径：" });
            Add("updatetoken", "更新token", "账号登录", new string[0]);
            Add("version", "查看版本号", "账号登录", new string[0]);
            Add("authstart", "认证启动游戏", "启动游戏", new string[0]);
            Add("clear", "清屏", "启动游戏", new string[0]);
            Add("closeconnect", "关闭控制连接", "启动游戏", new string[0]);
            Add("cls", "清屏", "启动游戏", new string[0]);
            Add("color", "设置颜色", "启动游戏", new string[] { "输入色号：" });
            Add("gametype", "启动游戏设置模式", "启动游戏", new string[] { "multiplayer_game_type:", "IP:", "Port:" });
            Add("getallgame", "获取校验地址", "启动游戏", new string[0]);
            Add("getpeuid", "获取peuid", "启动游戏", new string[0]);
            Add("getrtxgame", "获取RTX版", "启动游戏", new string[0]);
            Add("getsign", "sign签名校验", "启动游戏", new string[0]);
            Add("getuid", "获取uid", "启动游戏", new string[0]);
            Add("ipstart", "启动游戏：自定义ip端口", "启动游戏", new string[0]);
            Add("portstart", "启动游戏（启动器端口）", "启动游戏", new string[] { "multiplayer_game_type:", "IP:", "Port:" });
            Add("senddata", "向游戏发送数据", "启动游戏", new string[] { "DATA:" });
            Add("start", "启动游戏", "启动游戏", new string[0]);
            Add("startconnect", "启动控制连接", "启动游戏", new string[] { "Port:" });
            Add("test", "获取64位版", "启动游戏", new string[0]);
            Add("title", "设置标题", "启动游戏", new string[] { "输入标题：" });
            Add("STOP", "停止进入房间", "联机房间", new string[0]);
            Add("additem", "启动游戏添加Itemid", "联机房间", new string[] { "输入物品id：" });
            Add("ajoin", "循环进入房间", "联机房间", new string[] { "是否接受条款：", "输入房间号：" });
            Add("ajoinrid", "使用entityid循环进房间", "联机房间", new string[] { "是否接受条款：", "输入房间entity_id：" });
            Add("closeroomupdate", "关闭房间自动更新", "联机房间", new string[0]);
            Add("command", "向房间发送指令", "联机房间", new string[] { "输入房间id：", "输入指令：" });
            Add("getip", "获取房间ip和端口", "联机房间", new string[0]);
            Add("ivjoin", "进房间（固定间隔）", "联机房间", new string[] { "输入房间id：" });
            Add("join", "进入房间", "联机房间", new string[] { "输入房间id：" });
            Add("joinPE", "进入手机版房间", "联机房间", new string[] { "输入房间id：" });
            Add("joinlobby", "进入手机版多人主城", "联机房间", new string[0]);
            Add("kick", "踢人", "联机房间", new string[] { "输入玩家UID：" });
            Add("kickall", "一键踢所有人", "联机房间", new string[0]);
            Add("left", "退出房间", "联机房间", new string[0]);
            Add("load", "载入地图", "联机房间", new string[] { "输入最大人数：" });
            Add("loadPE", "载入手机版地图", "联机房间", new string[] { "输入最大人数：" });
            Add("passwordjoin", "进密码房", "联机房间", new string[] { "输入房间id：", "输入房间密码：" });
            Add("purchase", "购买组件", "联机房间", new string[] { "输入物品id：" });
            Add("purchasePE", "购买手机版组件", "联机房间", new string[] { "输入物品id：" });
            Add("reset", "重置地图", "联机房间", new string[0]);
            Add("ritem", "删除Itemid", "联机房间", new string[] { "输入物品id：" });
            Add("save", "保存地图", "联机房间", new string[] { "输入保存名称：" });
            Add("setop", "转交房主", "联机房间", new string[] { "输入玩家UID：" });
            Add("setpassword", "房间密码热更新", "联机房间", new string[] { "输入新的密码（留空为无密码）：" });
            Add("startr", "联机大厅开房间", "联机房间", new string[] { "输入物品id：", "输入最大人数：" });
            Add("startrPE", "手机版联机大厅开房间", "联机房间", new string[] { "输入物品id：", "输入最大人数：" });
            Add("startroomupdate", "开启房间自动更新", "联机房间", new string[0]);
            Add("ujoin", "进入房间（不掉）", "联机房间", new string[] { "输入房间id：" });
            Add("visibility", "房间可见性", "联机房间", new string[] { "可见性：" });
            Add("visitorsave", "允许房客保存", "联机房间", new string[] { "是否允许保存(y,n)？：" });
            Add("wleft", "指定房间id退出房间", "联机房间", new string[] { "输入房间id：" });
            Add("wstartroomupdate", "通过房间id启动自动更新", "联机房间", new string[] { "输入房间entity_id：" });
            Add("fast", "手机版房间快速匹配", "搜索查询", new string[] { "输入物品id：" });
            Add("getroomlist", "获取房间列表", "搜索查询", new string[] { "输入物品id：" });
            Add("getserverip", "获取租赁服ip", "搜索查询", new string[] { "服务器号：", "服务器密码：" });
            Add("playerlist", "获取房间成员列表", "搜索查询", new string[0]);
            Add("quick", "手机版快速匹配", "搜索查询", new string[] { "输入物品id：" });
            Add("roomlist", "获取房间列表", "搜索查询", new string[] { "输入物品id：" });
            Add("roomlistPE", "获取手机版房间列表", "搜索查询", new string[] { "输入物品id：" });
            Add("search", "搜索房间号或联机大厅组件", "搜索查询", new string[] { "输入房间信息：" });
            Add("searchPE", "搜索手机版房间号或联机大厅组件", "搜索查询", new string[] { "输入房间信息：" });
            Add("searchitem", "搜索物品id", "搜索查询", new string[] { "输入物品id：" });
            Add("searchmod", "获取模组下载地址", "搜索查询", new string[] { "输入模组名称：" });
            Add("searchpemod", "获取模组下载地址", "搜索查询", new string[] { "输入模组名称：" });
            Add("searchroom", "通过房主名称搜索房间", "搜索查询", new string[] { "输入房间信息：" });
            Add("searchroomv2", "通过房主名称搜索房间", "搜索查询", new string[] { "输入房间信息：" });
            Add("searchserver", "搜索pe租赁服", "搜索查询", new string[] { "服务器号：" });
            Add("searchuid", "通过uid查找用户", "搜索查询", new string[] { "输入UID：" });
            Add("searchuser", "通过用户名查找uid", "搜索查询", new string[] { "输入用户名：" });
            Add("wplayerlist", "通过房间id获取房间成员列表", "搜索查询", new string[] { "输入房间entityid：" });
            Add("decryptpath", "解密模组的所有文件", "模组组件", new string[] { "path:", "key:", "uuid:" });
            Add("getiteminfo", "初始化解包", "模组组件", new string[] { "ITEM_ID:" });
            Add("getitemkey", "获取模组密钥", "模组组件", new string[] { "ITEM_ID:" });
            Add("getmod", "获取模组下载地址", "模组组件", new string[] { "ITEM_ID:" });
            Add("getpeitemkey", "获取手机版模组密钥", "模组组件", new string[] { "ITEM_ID:" });
            Add("getpemod", "获取模组下载地址", "模组组件", new string[] { "ITEM_ID:" });
            Add("ununzip", "解密模组全部文件", "模组组件", new string[] { "path:", "key:", "uuid:" });
            Add("ununzippath", "解密模组全部文件（路径）", "模组组件", new string[] { "path:", "key:", "uuid:" });
            Add("unzip", "解包mczip文件", "模组组件", new string[] { "JsonPath:" });
            Add("unzipmodjsonpath", "解包模组 JSON（路径）", "模组组件", new string[] { "path:", "key:", "uuid:" });
            Add("unzippath", "解包 mczip（文件路径）", "模组组件", new string[] { "Path:" });
            Add("ban", "封禁玩家", "封禁举报", new string[] { "输入玩家uid：" });
            Add("clearbanid", "清除所有ban", "封禁举报", new string[0]);
            Add("report", "点号", "封禁举报", new string[] { "输入uid：", "举报信息：" });
            Add("startban", "启动封禁", "封禁举报", new string[] { "输入房间entityid：" });
            Add("unban", "解ban", "封禁举报", new string[] { "输入玩家uid：" });
            Add("allserver", "本地联机服务器列表", "服务器/锁服", new string[0]);
            Add("asproom", "联大刷屏（房间+文件）", "服务器/锁服", new string[] { "输入入房间entityid：", "输入刷屏文件：" });
            Add("displayentity", "（FastBuilder）显示实体", "服务器/锁服", new string[] { "y/n:" });
            Add("displaywindow", "（FastBuilder）显示窗口", "服务器/锁服", new string[] { "y/n:" });
            Add("lockUID", "追踪崩服", "服务器/锁服", new string[0]);
            Add("lockserver", "锁服（随机小号）", "服务器/锁服", new string[0]);
            Add("lockserverv2", "（FastBuilder）联大崩服", "服务器/锁服", new string[] { "输入ip：", "输入端口：" });
            Add("lockserverv3", "（FastBuilder）租赁服锁服", "服务器/锁服", new string[0]);
            Add("lockserverv4", "（FastBuilder）联机大厅刷屏", "服务器/锁服", new string[0]);
            Add("lockserverv5", "（FastBuilder）联机大厅字符画", "服务器/锁服", new string[0]);
            Add("lockserverv6", "（FastBuilder）租赁服锁服", "服务器/锁服", new string[0]);
            Add("mkick", "本地联机踢人", "服务器/锁服", new string[] { "玩家uid:" });
            Add("padding", "租赁服人机填充", "服务器/锁服", new string[0]);
            Add("paddinglobby", "联大人机填充（锁服）", "服务器/锁服", new string[0]);
            Add("serverid", "设置服务器id（本地认证前置）", "服务器/锁服", new string[] { "id:" });
            Add("serverip", "设置服务器ip（FB认证前置）", "服务器/锁服", new string[] { "ip:" });
            Add("setauthserver", "（FastBuilder）设置认证服", "服务器/锁服", new string[] { "输入认证服：" });
            Add("setlockserverpassword", "设置锁服密码", "服务器/锁服", new string[] { "设置密码：" });
            Add("setnullpadding", "设置租赁服填充预留空位", "服务器/锁服", new string[0]);
            Add("setsleep", "设置刷屏间隔", "服务器/锁服", new string[] { "输入间隔：" });
            Add("setwebserver", "（FastBuilder）设置认证服v2", "服务器/锁服", new string[] { "输入认证服：" });
            Add("sproom", "（FastBuilder）联大循环刷屏", "服务器/锁服", new string[0]);
            Add("spserver", "（FastBuilder）联大循环刷屏", "服务器/锁服", new string[0]);
            Add("startserver", "启动本地authv2认证", "服务器/锁服", new string[] { "输入认证服务器端口：" });
            Add("startserverv2", "启动本地FastBuilder认证", "服务器/锁服", new string[] { "输入认证服务器端口：" });
            Add("startserverv3", "startserverv3", "服务器/锁服", new string[] { "输入认证服务器端口：" });
            Add("traceUID", "追踪锁服", "服务器/锁服", new string[] { "输入uid：" });
            Add("fastbjd", "一键布吉岛", "假人/机器人", new string[0]);
            Add("fastec", "一键EC", "假人/机器人", new string[0]);
            Add("getkey", "（ChatBot）刷卡密", "假人/机器人", new string[] { "输入文件名：", "输入数量：", "输入时间：", "输入初始向量：" });
            Add("initdummy", "（ChatBot）初始化", "假人/机器人", new string[0]);
            Add("kickplayer", "远程卡人（随机小号）", "假人/机器人", new string[] { "输入uid：" });
            Add("addfriend", "加好友", "聊天/好友", new string[] { "uid:" });
            Add("asenduid", "远程卡人", "聊天/好友", new string[0]);
            Add("asenduidpath", "远程卡人", "聊天/好友", new string[0]);
            Add("closechatconnect", "关闭聊天服务器连接", "聊天/好友", new string[0]);
            Add("closelinkconnect", "关闭Link服务器连接", "聊天/好友", new string[0]);
            Add("recvfriend", "接收好友消息", "聊天/好友", new string[] { "uid:" });
            Add("sendlink", "自定义link消息", "聊天/好友", new string[] { "message_hex:" });
            Add("sendlinkfiel", "自定义link消息", "聊天/好友", new string[0]);
            Add("sendlinkfile", "发送 Link 消息（文件）", "聊天/好友", new string[] { "message_path:" });
            Add("senduid", "发送好友消息", "聊天/好友", new string[0]);
            Add("senduidpath", "发送好友消息", "聊天/好友", new string[0]);
            Add("startchatconnect", "启动聊天服务器连接", "聊天/好友", new string[0]);
            Add("startlinkconnect", "启动Link服务器连接", "聊天/好友", new string[0]);
            Add("userlist", "生成用户索引文件", "聊天/好友", new string[] { "输入文件夹路径：" });
            Add("Epost", "开发功能：自定义接口调用（加密）", "开发调试", new string[] { "URL:", "POST:" });
            Add("Epostfile", "开发功能：自定义接口调用（加密）", "开发调试", new string[] { "URL:", "PATH:" });
            Add("auth", "测试认证服务器", "开发调试", new string[] { "URL:", "POST:" });
            Add("authpath", "测试认证服务器", "开发调试", new string[] { "URL:", "PATH:" });
            Add("authpathv2", "测试认证服务器v2", "开发调试", new string[] { "URL:", "PATH:" });
            Add("authv2", "测试认证服务器v2", "开发调试", new string[] { "URL:", "POST:" });
            Add("javaconnect", "Java 版连接", "开发调试", new string[] { "Port:", "IP:", "Port:", "Name:" });
            Add("javaddname", "Java 版设置昵称", "开发调试", new string[] { "输入gameid：", "输入游戏昵称：" });
            Add("javagetname", "Java 版获取昵称", "开发调试", new string[] { "输入gameid：" });
            Add("newchacha", "新建 ChaCha 加密", "开发调试", new string[] { "输入key：", "输入data：" });
            Add("newchacha_", "新建 ChaCha 加密 v2", "开发调试", new string[] { "输入key：", "输入data：" });
            Add("newchachafile", "ChaCha 文件加密", "开发调试", new string[] { "输入key：", "输入path：" });
            Add("newchachafile_", "ChaCha 文件加密 v2", "开发调试", new string[] { "输入key：", "输入path：", "$输入path：" });
            Add("peauth", "测试认证服务器", "开发调试", new string[] { "URL:", "POST:" });
            Add("peauthpath", "测试认证服务器", "开发调试", new string[] { "URL:", "PATH:" });
            Add("peauthpathv2", "测试认证服务器v2", "开发调试", new string[] { "URL:", "PATH:" });
            Add("peauthv2", "测试认证服务器v2", "开发调试", new string[] { "URL:", "POST:" });
            Add("post", "开发功能：自定义接口调用", "开发调试", new string[] { "URL:", "POST:" });
            Add("postfile", "开发功能：自定义接口调用", "开发调试", new string[] { "URL:", "PATH:" });
            Add("sendjava", "发送 Java 数据", "开发调试", new string[] { "GUID:", "WorldName:" });
            Add("sendjavav2", "sendjavav2", "开发调试", new string[] { "GUID:", "WorldName:" });
            Add("setposthost", "修改post服务器", "开发调试", new string[] { "PostHost:" });
            Add("spost", "自定义接口调用（修改用户信息）", "开发调试", new string[] { "URL:", "POST:", "UID:", "TOKEN:" });
            Add("spostfile", "自定义接口调用（修改用户信息）", "开发调试", new string[] { "URL:", "PATH:", "UID:", "TOKEN:" });
            Add("testEpost", "测试加密接口", "开发调试", new string[] { "URL:", "POST:", "EncryptType:" });
            Add("adduser", "添加账号（Sauth）", "其他", new string[0]);
            Add("chachareset", "重置 ChaCha 加密状态", "其他", new string[0]);
            Add("closeencrypt", "关闭请求加密", "其他", new string[0]);
            Add("cmccserver", "切换移动认证服", "其他", new string[0]);
            Add("ctccserver", "切换电信认证服", "其他", new string[0]);
            Add("cuccserver", "切换联通认证服", "其他", new string[0]);
            Add("getuuid", "生成 UUID", "其他", new string[0]);
            Add("linkserver", "Link 服务器", "其他", new string[0]);
            Add("linkserverexpr", "Link 服务器（表达式）", "其他", new string[0]);
            Add("loadadd", "载入地图（序号+人数）", "其他", new string[] { "输入序号（从零开始）：", "输入最大人数：" });
            Add("reloadconfig", "重载 config.json", "其他", new string[0]);
            Add("saveadd", "保存地图（名称+编号）", "其他", new string[] { "输入保存名称：", "保存num：" });
            Add("setroomid", "设置房间 entity_id", "其他", new string[] { "输入房间entity_id：" });
            Add("startencrypt", "开启请求加密", "其他", new string[0]);
            Add("startroom", "启动房间", "其他", new string[0]);
            Add("tasknum", "设置任务数量", "其他", new string[0]);
        }

        static void Add(string n, string l, string c, string[] p)
        { All.Add(new CmdDef(n, l, c, p)); }

        public static List<CmdDef> ByCategory(string cat)
        {
            var r = new List<CmdDef>();
            foreach (var d in All) if (d.Category == cat) r.Add(d);
            return r;
        }
    }
}