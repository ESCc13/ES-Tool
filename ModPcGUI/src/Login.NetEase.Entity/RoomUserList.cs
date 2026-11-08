namespace Login.NetEase.Entity;

internal class RoomUserList
{
	public int code { get; set; }

	public string message { get; set; }

	public string details { get; set; }

	public UserListEntity[] entities { get; set; }

	public int total { get; set; }
}
