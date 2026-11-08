using System;
using System.Collections.Generic;

namespace Login.loginauth;

[Serializable]
internal class AuthenticationResponseEntity
{
	public string entity_id { get; set; }

	public string account { get; set; }

	public string token { get; set; }

	public string sead { get; set; }

	public bool hasMessage { get; set; }

	public string aid { get; set; }

	public string sdkuid { get; set; }

	public string access_token { get; set; }

	public string unisdk_login_json { get; set; }

	public int verify_status { get; set; }

	public bool hasGmail { get; set; }

	public bool is_register { get; set; }

	public List<string> autopatch { get; set; }

	public string env { get; set; }

	public int last_server_up_time { get; set; }

	public string min_engine_version { get; set; }

	public string min_patch_version { get; set; }
}
