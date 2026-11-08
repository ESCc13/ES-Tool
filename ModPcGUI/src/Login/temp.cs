using System;
using System.Collections.Generic;

namespace Login;

[Serializable]
internal class temp
{
	public bool InitializeName { get; set; }

	public string Version { get; set; }

	public string path { get; set; }

	public string url { get; set; }

	public string PE_url { get; set; }

	public List<string> item_ids { get; set; } = new List<string>();

	public List<string> ban_id { get; set; } = new List<string>();

	public bool connect_log { get; set; }

	public string engineVersion { get; set; }

	public string patchVersion { get; set; }

	public bool is_g79 { get; set; }

	public bool link_connect { get; set; }

	public bool chat_connect { get; set; }

	public bool cppconfig_encryption { get; set; }

	public string libminecraftpe { get; set; }

	public string patch { get; set; }

	public int offset { get; set; }

	public int rounds { get; set; }
}
