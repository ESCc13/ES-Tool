using System.Collections.Generic;

namespace Login.NetEase.DummyService.Entity;

internal class SrvDummyUserLists
{
	public List<SrvDummyUserListsEntity> user_lists { get; set; }

	public static bool IsUid(SrvDummyUserLists srvDummyUserLists, string uid)
	{
		bool result = false;
		for (int i = 0; i < srvDummyUserLists.user_lists.Count; i++)
		{
			if (srvDummyUserLists.user_lists[i].uid == uid)
			{
				Function.ClientLog("用户验证已通过");
				return true;
			}
		}
		Function.ClientLog("用户验证未通过");
		return result;
	}

	public static bool IsTimeOverdue(SrvDummyUserLists srvDummyUserLists, string uid)
	{
		int num = int.Parse(Function.GetTimeStamp());
		Function.ClientLog("TimeStamp:" + num);
		for (int i = 0; i < srvDummyUserLists.user_lists.Count; i++)
		{
			if (srvDummyUserLists.user_lists[i].uid == uid && num > srvDummyUserLists.user_lists[i].overdue_time)
			{
				Function.ClientLog("到期验证未通过");
				return true;
			}
		}
		Function.ClientLog("到期验证已通过");
		return false;
	}
}
