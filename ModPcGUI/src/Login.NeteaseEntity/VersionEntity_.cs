namespace Login.NeteaseEntity;

internal class VersionEntity_
{
	internal class VersionEntites
	{
		public bool maintenance { get; set; }

		public string client_mc_version { get; set; }
	}

	public int code { get; set; }

	public string details { get; set; }

	public VersionEntites entitiy { get; set; }

	public string message { get; set; }

	public int total { get; set; }
}
