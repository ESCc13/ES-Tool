using System;
using System.Text.RegularExpressions;
using System.Threading;
using ConsoleAppLogin.NetEase;
using Login.NetEase.RPCServer;
using Login.loginauth;
using Newtonsoft.Json;
using Noya.LocalServer.Common.Utilities;
using text;

namespace Login.NetEase.DummyService;

internal class RentalSwipe
{
	private int _processID;

	private RPCPort rPCPort;

	private AuthenticationResponse authenticationResponse;

	public static int Sleep;

	private void RecvPid(byte[] array)
	{
		_processID = BitConverter.ToInt32(array, 0);
		Term.WriteLine("已获取pid" + _processID);
	}

	public RentalSwipe(string cookiePath, string netease_sid, string ip, string port, string spfile)
	{
		new Thread(() =>
		{
			try
			{
				string sauth = app.ReadJson(cookiePath);
				Function.ClientLog("SwipeSleep:" + Sleep);
				string value = Http.PE_GetLoginResponse(sauth);
				authenticationResponse = JsonConvert.DeserializeObject<AuthenticationResponse>(value);
				Function.ClientLog("PE-AUTH RESPONSE:" + Regex.Unescape(authenticationResponse.message));
				new Thread(() =>
				{
					try
					{
						Thread.Sleep(60000);
						Http.UpdateAuthotp("https://g79obtcore.minecraft.cn:8443", authenticationResponse.entity.token);
					}
					catch (Exception)
					{
					}
				}).Start();
				string commonName = Function.GetCommonName(authenticationResponse.entity.entity_id, authenticationResponse.entity.token);
				rPCPort = new RPCPort();
				rPCPort.RegisterReceiveCallBack(0, RecvPid);
				while (app.start_stop)
				{
					app.InvokeCmd("start FastBuilder.exe --uid " + authenticationResponse.entity.entity_id + " --sid " + netease_sid + ":RentalGame --name " + commonName + " --version " + app.Temp.Version + " --engineVersion " + app.Temp.engineVersion + " --patchVersion " + app.Temp.patchVersion + " --url " + LockServer.AuthServerUrl + " --authurl https://g79authobt.minecraft.cn --ip " + ip + " --port " + port + " --token " + Convert.ToBase64String(HashHelper.SafeCompleteMD5(authenticationResponse.entity.token)) + " --g79 --typ sp --spfile " + spfile + " --sleep " + Sleep + " --LauncherPort " + rPCPort.LauncherControlPort);
				}
				DummySrv.strings.Add(cookiePath);
			}
			catch (Exception ex)
			{
				Term.WriteLine(ex.ToString());
			}
		}).Start();
	}
}
