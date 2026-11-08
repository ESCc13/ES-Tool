using System.ComponentModel;

namespace MCStudio.Model.EnumType;

public enum GameMode
{
	[Description("生存模式")]
	SURVIVAL,
	[Description("创造模式")]
	CREATIVE,
	[Description("冒险模式")]
	ADVENTURE,
	[Description("旁观模式")]
	SPECTATOR
}
