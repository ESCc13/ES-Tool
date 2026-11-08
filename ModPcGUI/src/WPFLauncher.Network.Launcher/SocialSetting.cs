using System;

namespace WPFLauncher.Network.Launcher;

[Serializable]
public class SocialSetting
{
	public bool underage_mode { get; set; }

	public bool block_strangers { get; set; }

	public bool block_all_messages { get; set; }

	public bool block_reposted_and_commented { get; set; }

	public int message_visibility { get; set; }
}
