using System;

namespace Login.loginauth;

[Serializable]
internal class AuthenticationDelete
{
	public string user_id { get; set; }

	public int logout_type { get; set; }
}
