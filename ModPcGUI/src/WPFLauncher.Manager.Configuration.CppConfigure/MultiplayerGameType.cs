using System;

namespace WPFLauncher.Manager.Configuration.CppConfigure;

[Serializable]
internal enum MultiplayerGameType
{
	MC_GAME = 0,
	LAN_HOST_GAME = 1,
	LAN_GUEST_GAME = 2,
	LanLobbyServer = 3,
	LanLobbyClient = 4,
	RentalGame = 10,
	NetworkGame = 100,
	LobbyGame = 1000
}
