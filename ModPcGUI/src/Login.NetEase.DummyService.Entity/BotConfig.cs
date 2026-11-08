using System.Collections.Generic;

namespace Login.NetEase.DummyService.Entity;

internal class BotConfig
{
	public int dummy_level { get; set; }

	public List<WhiteList> white_list { get; set; }
}
