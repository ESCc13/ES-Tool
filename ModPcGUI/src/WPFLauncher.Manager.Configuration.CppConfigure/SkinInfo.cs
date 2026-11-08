using System;

namespace WPFLauncher.Manager.Configuration.CppConfigure;

[Serializable]
public class SkinInfo
{
	private string m_skinId = "";

	public string skin { get; set; } = "";

	public string md5 { get; set; } = "";

	public bool slim { get; set; }

	public string skin_iid
	{
		get
		{
			return m_skinId;
		}
		set
		{
			if (value == "10000")
			{
				value = "-1";
			}
			else if (value == "10001")
			{
				value = "-2";
			}
			m_skinId = value;
		}
	}
}
