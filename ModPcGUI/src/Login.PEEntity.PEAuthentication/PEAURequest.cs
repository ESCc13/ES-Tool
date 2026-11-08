namespace Login.PEEntity.PEAuthentication;

internal class PEAURequest
{
	public string engine_version { get; set; }

	public string extra_param { get; set; } = "extra";

	public string message { get; set; }

	public string patch_version { get; set; }

	public string pay_channel { get; set; } = "dashen_cloudgame";

	public string sa_data { get; set; }

	public object sauth_json { get; set; }

	public string seed { get; set; }

	public string sign { get; set; }
}
