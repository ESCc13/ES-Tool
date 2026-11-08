using System;
using System.Collections.Generic;
using WPFLauncher.Network.Launcher;

namespace WPFLauncher.Manager.Configuration.CppConfigure;

[Serializable]
public class CppGameConfig
{
	private const string EXTENSION = ".cppconfig";

	public LaunchParams launch_params { get; set; } = new LaunchParams();

	public WorldInfo world_info { get; set; } = new WorldInfo();

	public RoomInfo room_info { get; set; } = new RoomInfo();

	public PlayerInfo player_info { get; set; } = new PlayerInfo();

	public SkinInfo skin_info { get; set; } = new SkinInfo();

	public AntiAddictionInfo anti_addiction_info { get; set; } = new AntiAddictionInfo();

	public Misc misc { get; set; } = new Misc();

	public string path { get; set; } = "";

	public string web_server_url { get; set; }

	public string core_server_url { get; set; }

	public SocialSetting social_setting { get; set; } = new SocialSetting();

	public List<string> vip_using_mod { get; set; } = new List<string>();

	public void a(string olj)
	{
		if (!room_info.item_ids.Contains(olj))
		{
			room_info.item_ids.Add(olj);
		}
	}
}
