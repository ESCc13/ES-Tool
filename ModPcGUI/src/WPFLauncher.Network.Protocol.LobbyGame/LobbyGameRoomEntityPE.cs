using System.Collections.Generic;

namespace WPFLauncher.Network.Protocol.LobbyGame;

public class LobbyGameRoomEntityPE
{
	public string entity_id { get; set; }

	public string room_name { get; set; }

	public string slogan { get; set; }

	public uint password { get; set; }

	public string res_id { get; set; }

	public byte max_count { get; set; }

	public uint allow_save { get; set; }

	public uint visibility { get; set; }

	public string owner_id { get; set; }

	public string save_id { get; set; }

	public string version { get; set; }

	public string world_id { get; set; }

	public string tag { get; set; }

	public string min_level { get; set; }

	public int order_id { get; set; }

	public uint game_status { get; set; }

	public long save_size { get; set; }

	public List<string> fids { get; set; }

	public string lobby_manifest_version { get; set; }

	public string behaviour_uuid { get; set; }

	public string playing_uuid { get; set; }

	public string team_id { get; set; }

	public string chat_group_id { get; set; }

	public string chat_group_ready { get; set; }

	public byte cur_num { get; set; }

	public List<string> member_uids { get; set; }
}
