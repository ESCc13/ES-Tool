using System;
using WPFLauncher.Network.Protocol.LobbyGame;

namespace Login.NetEase.Entity;

[Serializable]
internal class ListEntity
{
	public int code { get; set; }

	public string details { get; set; }

	public LobbyGameRoomEntity[] entities { get; set; }

	public string message { get; set; }

	public int total { get; set; }
}
