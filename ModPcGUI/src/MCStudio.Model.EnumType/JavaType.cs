using System.ComponentModel;
using System.Runtime.Serialization;

namespace MCStudio.Model.EnumType;

public enum JavaType
{
	[Description("Java8")]
	[EnumMember(Value = "java8")]
	Java8 = 8,
	[Description("Java17")]
	[EnumMember(Value = "java17")]
	Java17 = 17
}
