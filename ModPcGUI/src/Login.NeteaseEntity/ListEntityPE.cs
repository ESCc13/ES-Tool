using WPFLauncher.Network.Protocol.LobbyGame;

namespace Login.NeteaseEntity;

internal class ListEntityPE
{
	public int code { get; set; }

	public string details { get; set; }

	public LobbyGameRoomEntityPE[] entities { get; set; }

	public string message { get; set; }

	public int total { get; set; }
}
