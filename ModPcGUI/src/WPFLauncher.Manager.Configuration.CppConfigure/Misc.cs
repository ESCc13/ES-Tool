using System;

namespace WPFLauncher.Manager.Configuration.CppConfigure;

[Serializable]
public class Misc
{
	public int multiplayer_game_type { get; set; }

	public string auth_server_url { get; set; }

	public int launcher_port { get; set; }

	public string sensitive_word_file { get; set; } = "";
}
