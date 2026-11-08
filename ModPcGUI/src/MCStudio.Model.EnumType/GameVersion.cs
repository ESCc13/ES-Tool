using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MCStudio.Model.EnumType;

[DataContract]
[JsonConverter(typeof(StringEnumConverter))]
public enum GameVersion : uint
{
	[Description("")]
	[EnumMember(Value = "")]
	NONE = 0u,
	[Description("全版本")]
	[EnumMember(Value = "ALL")]
	ALL = 1u,
	[Description("基岩版")]
	[EnumMember(Value = "100.0.0")]
	V_CPP = 100000000u,
	[Description("光追版")]
	[EnumMember(Value = "200.0.0")]
	V_RTX = 200000000u,
	[Description("1.7.10")]
	[EnumMember(Value = "1.7.10")]
	V_1_7_10 = 1007010u,
	[Description("1.8")]
	[EnumMember(Value = "1.8")]
	V_1_8 = 1008000u,
	[Description("1.8.8")]
	[EnumMember(Value = "1.8.8")]
	V_1_8_8 = 1008008u,
	[Description("1.8.9")]
	[EnumMember(Value = "1.8.9")]
	V_1_8_9 = 1008009u,
	[Description("1.9.4")]
	[EnumMember(Value = "1.9.4")]
	V_1_9_4 = 1009004u,
	[Description("1.10.2")]
	[EnumMember(Value = "1.10.2")]
	V_1_10_2 = 1010002u,
	[Description("1.11.2")]
	[EnumMember(Value = "1.11.2")]
	V_1_11_2 = 1011002u,
	[Description("1.12")]
	[EnumMember(Value = "1.12")]
	V_1_12_0 = 1012000u,
	[Description("1.12.2")]
	[EnumMember(Value = "1.12.2")]
	V_1_12_2 = 1012002u,
	[Description("1.13.2")]
	[EnumMember(Value = "1.13.2")]
	V_1_13_2 = 1013002u,
	[Description("1.14.3")]
	[EnumMember(Value = "1.14.3")]
	V_1_14_3 = 1014003u,
	[Description("1.15")]
	[EnumMember(Value = "1.15")]
	V_1_15_0 = 1015000u,
	[Description("1.16")]
	[EnumMember(Value = "1.16")]
	V_1_16 = 1016000u,
	[Description("1.18")]
	[EnumMember(Value = "1.18")]
	V_1_18 = 1018000u,
	[Description("1.19")]
	[EnumMember(Value = "1.19")]
	V_1_19 = 1019000u,
	[Description("1.20")]
	[EnumMember(Value = "1.20")]
	V_1_20 = 1020000u,
	[Description("1.21")]
	[EnumMember(Value = "1.21")]
	V_1_21 = 1021000u,
	[Description("1.22")]
	[EnumMember(Value = "1.22")]
	V_1_22 = 1022000u,
	[Description("1.23")]
	[EnumMember(Value = "1.23")]
	V_1_23 = 1023000u
}
