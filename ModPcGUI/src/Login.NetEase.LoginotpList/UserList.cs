using System;
using System.Collections.Generic;

namespace Login.NetEase.LoginotpList;

[Serializable]
internal class UserList
{
	public List<string> CookieList { get; set; } = new List<string>();
}
