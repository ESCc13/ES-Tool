using System;

namespace Login.AFunction;

[Serializable]
internal class LobbyValue
{
	public string user_info { get; set; }

	public string item_ids { get; set; }

	public string password { get; set; }

	public int player_max { get; set; }

	public bool load { get; set; }
}
