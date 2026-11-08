using System;

namespace text.loginauth;

[Serializable]
internal class LoginotpResposne
{
	public int code { get; set; }

	public string detalis { get; set; }

	public ResposneEntityLogin entity { get; set; }

	public string message { get; set; }
}
