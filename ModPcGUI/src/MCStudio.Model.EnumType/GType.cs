using System;

namespace MCStudio.Model.EnumType;

[Serializable]
public enum GType
{
	NONE = -1,
	SINGLE_GAME = 1,
	NET_GAME = 2,
	MC_GAME = 7,
	SERVER_GAME = 8,
	LAN_GAME = 9,
	ONLINE_LOBBY_GAME = 10
}
