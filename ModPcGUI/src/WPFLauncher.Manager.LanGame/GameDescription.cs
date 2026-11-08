using System;
using System.Collections.Generic;

namespace WPFLauncher.Manager.LanGame;

[Serializable]
public class GameDescription
{
	public string NickName { get; set; }

	public uint Version { get; set; }

	public string Guid { get; set; }

	public List<ulong> ComponetList { get; set; }

	public GameDescription()
	{
	}

	public GameDescription(uint nfq, string nfr, string nfs, List<ulong> nft)
	{
		Version = nfq;
		Guid = nfr;
		NickName = nfs;
		ComponetList = nft;
	}
}
