using System;
using System.Collections.Generic;

namespace WPFLauncher.Manager.Configuration.CppConfigure;

[Serializable]
public class WorldInfo
{
	public string level_id { get; set; } = "";

	public int game_type { get; set; }

	public int difficulty { get; set; } = 2;

	public int permission_level { get; set; } = 1;

	public bool cheat { get; set; }

	public bool other_cheat { get; set; }

	public CheatInfo cheat_info { get; set; } = new CheatInfo();

	public List<string> resource_packs { get; set; } = new List<string>();

	public List<string> behavior_packs { get; set; } = new List<string>();

	public string name { get; set; } = "";

	public int world_type { get; set; } = 1;

	public bool start_with_map { get; set; }

	public bool bonus_items { get; set; }

	public string seed { get; set; } = "";
}
