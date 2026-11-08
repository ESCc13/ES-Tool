using System;

namespace WPFLauncher.Manager.Configuration.CppConfigure;

[Serializable]
public class CheatInfo
{
	public bool pvp { get; set; } = true;

	public bool show_coordinates { get; set; }

	public bool always_day { get; set; }

	public bool daylight_cycle { get; set; } = true;

	public bool fire_spreads { get; set; } = true;

	public bool tnt_explodes { get; set; } = true;

	public bool keep_inventory { get; set; }

	public bool mob_spawn { get; set; } = true;

	public bool natural_regeneration { get; set; } = true;

	public bool mob_loot { get; set; } = true;

	public bool mob_griefing { get; set; } = true;

	public bool tile_drops { get; set; } = true;

	public bool entities_drop_loot { get; set; } = true;

	public bool weather_cycle { get; set; } = true;

	public bool command_blocks_enabled { get; set; } = true;

	public uint random_tick_speed { get; set; } = 1u;

	public bool experimental_gameplay { get; set; }

	public bool experimental_holiday { get; set; }

	public bool experimental_biomes { get; set; }

	public bool experimental_modding { get; set; }

	public bool fancy_bubbles { get; set; }
}
