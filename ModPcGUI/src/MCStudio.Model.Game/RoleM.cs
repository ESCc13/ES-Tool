using System;
using Mcl.Core.Utils;

namespace MCStudio.Model.Game;

public class RoleM : IComparable
{
	public string Name { get; set; }

	public long CreatedTimeStamp { get; set; }

	int IComparable.CompareTo(object obj)
	{
		RoleM roleM = obj as RoleM;
		return (int)(CreatedTimeStamp - roleM.CreatedTimeStamp);
	}

	public RoleM(string name)
	{
		Name = name;
		CreatedTimeStamp = TimeHelper.GetUNIXTimeStamp();
	}
}
