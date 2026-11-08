using System;

namespace WPFLauncher.Manager.Configuration.CppConfigure;

[Serializable]
public class AntiAddictionInfo
{
	public bool enable { get; set; }

	public int left_time { get; set; }

	public double exp_multiplier { get; set; } = 1.0;

	public double block_multplier { get; set; } = 1.0;

	public string first_message { get; set; } = "";
}
