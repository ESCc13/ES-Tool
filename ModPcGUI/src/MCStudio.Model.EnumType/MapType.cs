using System.ComponentModel;

namespace MCStudio.Model.EnumType;

public enum MapType
{
	[Description("默认")]
	DEFAULT,
	[Description("超平坦")]
	FLAT,
	[Description("巨型生物系")]
	LARGEBIOMES,
	[Description("巨大化")]
	AMPLIFIED
}
