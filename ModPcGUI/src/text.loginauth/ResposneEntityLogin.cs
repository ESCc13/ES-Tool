namespace text.loginauth;

internal class ResposneEntityLogin
{
	public string aid { get; set; }

	public int lock_time { get; set; }

	public int open_otp { get; set; }

	public int otp { get; set; }

	public string otp_token { get; set; }
}
