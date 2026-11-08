namespace WPFLauncher.Util;

public static class x19signHelper
{
	private const string key = "942894570397f6d1c9cca2535ad18a2b";

	private const string sign = "!x19sign!";

	public static string Encrypt(this string gis)
	{
		return "!x19sign!" + gis.a("942894570397f6d1c9cca2535ad18a2b");
	}

	public static string Decrypt(this string git)
	{
		if (!git.StartsWith("!x19sign!"))
		{
			return git;
		}
		return git.Remove(0, "!x19sign!".Length).b("942894570397f6d1c9cca2535ad18a2b");
	}

	public static bool c(this string giu)
	{
		return giu.StartsWith("!x19sign!");
	}
}
