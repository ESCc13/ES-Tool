namespace Login.NetEase.DummyService;

internal class PaddingEntity
{
	internal class PaddingEntities
	{
		public string entity_id;

		public string name;

		public uint owner_id;

		public int visibility;

		public int status;

		public int icon_index;

		public int capacity;

		public string mc_version;

		public int player_count;

		public int like_num;

		public string server_type;

		public string offset;

		public string has_pwd;

		public string image_url;

		public string world_id;

		public string min_level;

		public bool pvp;

		public string server_name;
	}

	public int code;

	public string message;

	public string details;

	public PaddingEntities[] entities;

	public int total;
}
