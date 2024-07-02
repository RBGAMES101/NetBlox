﻿using NetBlox.Common;
using Serilog;
using System.Diagnostics;
using System.IO.Pipes;
using System.Net;
using System.Text;

namespace NetBlox.PublicService
{
	/// <summary>
	/// Manages all servers running on local computer. Does not provide any kind of REST API, that's a job of <seealso cref="WebService"/>
	/// </summary>
	public class ServerService : Service
	{
		public override string Name => nameof(ServerService);
		public List<Server> RunningServers = new();
		private Task Task;
		private bool Running = false;

		public override void Start()
		{
			base.Start();

			Task = Task.Run(async () =>
			{
				Log.Information("ServerService: Successfully started!");

				while (Running) ;
			});
			Running = true;
		}
		public override void Stop()
		{
			Running = false;
			base.Stop();
		}
		public void ShutdownMatching(Predicate<Server> match)
		{
			for (int i = 0; i < RunningServers.Count; i++)
			{
				var server = RunningServers[i];
				if (match(server))
				{
					server.KickAll("Server had closed");
					server.ServerProcess.Kill();
				}
			}
		}
		public override bool IsRunning() => Running;
	}
	public class Server
	{
		public string CustomName = "";
		public Process ServerProcess;
		public long ServerId;
		public long PlaceId;

		public Server(Place plc)
		{
			CustomName = plc.Name;
			ServerId = Program.GetService<ServerService>().RunningServers.Count;
			PlaceId = plc.Id;
			ServerProcess = new Process()
			{
				StartInfo = new ProcessStartInfo()
				{
					CreateNoWindow = true,
					FileName = "NetBloxServer.exe"
				}
			};
			ServerProcess.Start();
		}
		public string Communicate(string cmd)
		{
			using (NamedPipeServerStream ss = new("netblox.rctl" + ServerProcess.Id, PipeDirection.InOut))
			{
				if (!ss.WaitForConnectionAsync().Wait(5000))
				{
					// instead we'll just kill the server
					ServerProcess.Kill();
					throw new Exception("Server had closed");
				}
				using (StreamWriter sw = new StreamWriter(ss)) sw.Write(cmd);
				while (!ss.IsMessageComplete) ;
				return ss.ReadToEnd();
			}
		}
		public void LoadXmlPlace(string xmldata) => Communicate("nb2-rctrl-load\n" + xmldata);
		public void RunScript(string scr) => Communicate("nb2-rctrl-runlua\n" + scr);
		public void Kick(User user, string msg) => Communicate("nb2-rctrl-kick\n" + user.Id + ";" + msg);
		public void KickAll(string msg) => GetPlayers().ToList().ForEach(x => Kick(x, msg));
		public User[] GetPlayers() => (from x in Communicate("nb2-rtcrl-getuids\n").Split(';') select Program.GetService<UserService>().GetUserByID(long.Parse(x)))
				.ToArray();
		public int GetPlayerCount() => Communicate("nb2-rtcrl-getuids\n").Split(';').Length;
	}
}
