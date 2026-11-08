using System;

namespace WPFLauncher.Manager.Configuration.CppConfigure;

[Serializable]
public class PlayerInfo
{
	public uint user_id { get; set; }

	public string user_name { get; set; } = "";

	public string urs { get; set; } = "";
}
