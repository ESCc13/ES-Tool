namespace Login.NetEase.DummyService.Entity;

internal class RecvMessageEntity
{
	public uint uid { get; set; }

	public uint tgt { get; set; }

	public long chat_ver_id { get; set; }

	public byte platform { get; set; }

	public long sn { get; set; }

	public string words { get; set; }
}
