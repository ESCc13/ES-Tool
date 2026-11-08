using System;
using System.Net;

namespace JS4399MC;

public class JS4399HttpConfig
{
	public WebProxy Proxy { get; set; }

	public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10.0);
}
