using System.Collections.Generic;
using MCStudio.Model.Config;
using MCStudio.Model.EnumType;
using MCStudio.Model.Game;
using Mcl.Core.Utils;

namespace Login.JavaHelper.JavaConfigHelper;

public class JavaGameConfig : Configuration
{
	public string GUID { get; set; } = string.Empty;

	public string MainComponentId { get; set; } = string.Empty;

	public string GameId { get; set; } = string.Empty;

	public string WorldName { get; set; } = string.Empty;

	public GameMode GameMode { get; set; }

	public GameVersion Version { get; set; }

	public GType Type { get; set; } = GType.NONE;

	public bool AllowCheat { get; set; }

	public string Seed { get; set; } = string.Empty;

	public bool GenerateBuildings { get; set; } = true;

	public ulong LastPlayTime { get; set; }

	public ulong TotalPlayTime { get; set; }

	public ulong UpdateTime { get; set; }

	public MapType MapType { get; set; }

	public bool BonusChest { get; set; }

	public string SavePath { get; set; }

	public string SkinPath { get; set; }

	public SkinMode SkinMode { get; set; }

	public GameStatus Status { get; set; }

	public List<string> InstallComponentIds { get; set; } = new List<string>();

	public Dictionary<string, ComponentPathInfo> LocalComponentPathsDict { get; set; } = new Dictionary<string, ComponentPathInfo>();

	public object LocalComponentPaths { get; set; }

	public List<string> OpenedVisualTextureList { get; set; } = new List<string>();

	public List<string> OpenedVisualShaderpackList { get; set; } = new List<string>();

	public List<RoleM> RoleList { get; set; } = new List<RoleM>();

	public bool IsNeverNote { get; set; }

	public bool IsAutoSync { get; set; }

	public bool IsNewGame { get; set; }

	public bool IsAllowPE { get; set; }

	public JavaType Java { get; set; } = JavaType.Java8;

	public long ServerID { get; set; }
}
