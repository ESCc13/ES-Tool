using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleAppLogin.NetEase;
using Newtonsoft.Json;
using text;

namespace Login.NetEase.DummyService;

internal class Padding
{
	public static List<PaddingClient> paddingClients = new List<PaddingClient>();

	public static bool DisplayEntity = false;

	public static int NullPadding = 5;

	public static void StartPadding(string name, string ip, string port)
	{
		app.start_stop = true;
		new Thread(() =>
		{
			while (app.start_stop)
			{
				try
				{
					PaddingEntity paddingEntity = JsonConvert.DeserializeObject<PaddingEntity>(Http.x19post("https://g79mclobt.minecraft.cn", "/rental-server/query/search-by-name", "{\"server_name\": \"" + name + "\", \"offset\": 0}"));
					int num = paddingEntity.entities[0].capacity - paddingEntity.entities[0].player_count;
					Function.ClientLog("Server Capacity:" + paddingEntity.entities[0].player_count);
					Function.ClientLog("Server NullPadding:" + num);
					if (paddingEntity.entities[0].player_count < paddingEntity.entities[0].capacity - NullPadding)
					{
						AddPadding(num - NullPadding, paddingEntity.entities[0].entity_id, ip, port);
					}
					if (num < NullPadding)
					{
						RPadding(paddingEntity.entities[0].player_count - (paddingEntity.entities[0].capacity - NullPadding));
					}
					Thread.Sleep(15000);
				}
				catch (Exception ex)
				{
					Term.WriteLine(ex.ToString());
				}
			}
		}).Start();
	}

	public static void AddPadding(int num, string sid, string ip, string port)
	{
		Function.ClientLog("Padding TaskNum:" + num);
		try
		{
			for (int i = 0; i < num; i++)
			{
				Random random = new Random();
				string cookiePath = DummySrv.strings[random.Next(0, DummySrv.strings.Count)];
				paddingClients.Add(new PaddingClient(cookiePath, sid, ip, port));
				Thread.Sleep(1000);
			}
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.ToString());
		}
	}

	public static void RPadding(int nu)
	{
		Function.ClientLog("Padding RemoveNum:" + nu);
		try
		{
			Random random = new Random();
			for (int i = 0; i < nu; i++)
			{
				if (paddingClients.Count != 0)
				{
					int index = random.Next(0, paddingClients.Count - 1);
					paddingClients[index].Close();
					paddingClients.RemoveAt(index);
				}
			}
		}
		catch (Exception ex)
		{
			Term.WriteLine(ex.ToString());
		}
	}
}
