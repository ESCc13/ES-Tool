using System;

namespace Login.loginauth;

[Serializable]
internal class AuthenticationResponse
{
	public int code { get; set; }

	public string message { get; set; }

	public string details { get; set; }

	public AuthenticationResponseEntity entity { get; set; }
}
