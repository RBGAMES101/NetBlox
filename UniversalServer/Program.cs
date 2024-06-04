﻿using NetBlox.Instances;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Server;
using NetBlox.Structs;

namespace NetBlox.Server
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			LogManager.LogInfo($"NetBlox Server ({AppManager.VersionMajor}.{AppManager.VersionMinor}.{AppManager.VersionPatch}) is running...");
			/*GameManager.Start(false, true, false, args, x =>
			{
				DataModel dm = new();
				RbxlParser.Load(x, Root);

				Root = dm;

				LuaRuntime.Setup(Root, false);

				NetworkManager.StartServer();

				GameManager.IsRunning = true;
			});
			return;*/
			var g = AppManager.CreateGame(new()
			{
				AsServer = true,
				SkipWindowCreation = true,
				DoNotRenderAtAll = true,
				GameName = "NetBlox Server"
			}, args, (x, gm) =>
			{
				Workspace ws = gm.CurrentRoot.GetService<Workspace>();
				ReplicatedStorage rs = gm.CurrentRoot.GetService<ReplicatedStorage>();
				ReplicatedFirst ri = gm.CurrentRoot.GetService<ReplicatedFirst>();
				Players pl = gm.CurrentRoot.GetService<Players>();
				LocalScript ls = new(gm);

				ws.ZoomToExtents();
				ws.Parent = gm.CurrentRoot;

				Part part = new(gm)
				{
					Parent = ws,
					Color = Color.DarkGreen,
					Position = new(0, -5, 0),
					Size = new(2048, 2, 2048),
					TopSurface = SurfaceType.Studs,
					Anchored = true
				};

				ls.Parent = ri;
				ls.Source = "print(\"HIIIIII\"); printidentity();";

				rs.Parent = gm.CurrentRoot;
				ri.Parent = gm.CurrentRoot;
				pl.Parent = gm.CurrentRoot;

				gm.CurrentIdentity.MaxPlayerCount = 8;
				gm.CurrentIdentity.PlaceName = "Default Place";
				gm.CurrentIdentity.UniverseName = "NetBlox Defaults";
				gm.CurrentIdentity.Author = "The Lord";
				gm.CurrentIdentity.PlaceID = 47384;
				gm.CurrentIdentity.UniverseID = 47384;

				gm.CurrentRoot.Name = gm.CurrentIdentity.PlaceName;

				gm.NetworkManager.StartServer();
			});
			g.MainManager = true;
			AppManager.SetRenderTarget(g);
			AppManager.Start();
		}
	}
}
