using System;
using System.Collections.Generic;

namespace MCStudio.Network;

public class SocketCallback
{
	private Dictionary<ushort, Action<byte[]>> m_receiveCallBacks;

	public Action<string> LostConnectSocketCallback { get; set; }

	public SocketCallback()
	{
		m_receiveCallBacks = new Dictionary<ushort, Action<byte[]>>();
	}

	public void RegisterReceiveCallBack(ushort sid, Action<byte[]> callback)
	{
		m_receiveCallBacks[sid] = callback;
	}

	public bool CallBack(ushort sid, byte[] paramlist)
	{
		if (!m_receiveCallBacks.ContainsKey(sid))
		{
			return false;
		}
		m_receiveCallBacks[sid](paramlist);
		return true;
	}
}
