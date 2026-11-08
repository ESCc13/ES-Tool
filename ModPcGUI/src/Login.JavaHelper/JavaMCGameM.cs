using Login.JavaHelper.JavaConfigHelper;
using MCStudio.Network;
using text;

namespace Login.JavaHelper;

internal class JavaMCGameM
{
	public void StartExistGame(JavaGameConfig newWorldconfig, bool isLanGame, int otherType = 0, bool isCheat = false)
	{
		byte[] message = SimplePack.Pack((ushort)518, (byte)otherType, isCheat, BoolToByte(isLanGame), newWorldconfig.GUID, newWorldconfig.WorldName);
		app.GameRpcPort.SendControlData(message);
	}

	public void StartNewGameV2(JavaGameConfig newWorldconfig, bool isLanGame, int otherType = -1, bool otherCheat = false)
	{
		if (otherType == -1)
		{
			otherType = (int)newWorldconfig.GameMode;
		}
		byte[] message = SimplePack.Pack((ushort)1030, (byte)newWorldconfig.GameMode, (byte)otherType, newWorldconfig.AllowCheat, otherCheat, BoolToByte(newWorldconfig.GenerateBuildings), BoolToByte(newWorldconfig.BonusChest), (byte)newWorldconfig.MapType, BoolToByte(isLanGame), newWorldconfig.Seed, newWorldconfig.GUID, newWorldconfig.WorldName);
		app.GameRpcPort.SendControlData(message);
	}

	private byte BoolToByte(bool b)
	{
		if (!b)
		{
			return 0;
		}
		return 1;
	}
}
