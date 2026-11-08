namespace MCStudio.Network.GameControl;

public interface ICommunicable
{
	int GetPort();

	void InitCommunicate();

	void Stop();
}
