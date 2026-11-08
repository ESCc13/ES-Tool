using System;
using System.Collections.Generic;

namespace WPFLauncher.Manager.Configuration.CppConfigure;

[Serializable]
public class RoomInfo
{
	public string ip { get; set; } = "";

	public int port { get; set; }

	public string room_name { get; set; } = "";

	public string enter_password { get; set; } = "";

	public string token { get; set; } = "";

	public uint room_id { get; set; }

	public uint host_id { get; set; }

	public bool allow_pe { get; set; } = true;

	public int max_player { get; set; }

	public int visibility_mode { get; set; }

	public List<string> item_ids { get; set; } = new List<string>();

	public List<uint> tag_ids { get; set; }

	public int simple_game_version { get; set; }

	public string create_room_extra_bits { get; set; }
}
