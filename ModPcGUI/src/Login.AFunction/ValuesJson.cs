using System;

namespace Login.AFunction;

[Serializable]
internal class ValuesJson
{
	public string room_id { get; set; }

	public string user_info { get; set; }

	public string password { get; set; }

	public bool joined { get; set; }
}
