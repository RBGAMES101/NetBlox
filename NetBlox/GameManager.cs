﻿global using Color = Raylib_cs.Color;
using MoonSharp.Interpreter;
using NetBlox.Instances;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Structs;
using System.Net;
using System.Reflection;
using System.Runtime;

namespace NetBlox
{
	public delegate void InstanceEventHandler(Instance inst);

	/// <summary>
	/// Represents a NetBlox game. Believe it or not, but one NetBlox process can run multiple games at once (in theory)
	/// </summary>
	public class GameManager
	{
		public List<Instance> AllInstances = [];
		public Dictionary<char, Action> Verbs = [];
		public NetworkIdentity CurrentIdentity = new();
		public RenderManager? RenderManager;
		public PhysicsManager? PhysicsManager;
		public NetworkManager? NetworkManager;
		public DataModel CurrentRoot = null!;
		public bool IsStudio = false;
		public bool IsRunning = true;
		public bool ShuttingDown = false;
		public bool ProhibitProcessing = false;
		public bool ProhibitScripts = false;
		public bool MainManager = false;
		public bool UsePublicService = false;
		public string QueuedTeleportAddress = "";
		public string ManagerName = "";
		public string Username = ""; // bye bye DevDevDev
		public event EventHandler? ShutdownEvent;
		public event InstanceEventHandler? AddedInstance;
		public bool AllowReplication = false;

		public void InvokeAddedEvent(Instance inst)
		{
			if (AddedInstance != null && AllowReplication)
				AddedInstance(inst);
		}
		public void LoadAllCoreScripts()
		{
			string[] files = Directory.GetFiles(AppManager.ContentFolder + "scripts");
			for (int i = 0; i < files.Length; i++)
			{
				CoreScript cs = new(this);
				string cont = File.ReadAllText(files[i]);
				cs.Source = cont;
				cs.Name = Path.GetFileNameWithoutExtension(files[i]);
				cs.Parent = CurrentRoot.GetService<CoreGui>();
			}
		}
		public void Start(GameConfiguration gc, string[] args, Action<string, GameManager> servercallback)
		{
			ulong pid = ulong.MaxValue;
			string rbxlinit = "";
			LogManager.LogInfo("Initializing NetBlox...");

			NetworkManager = new(this, gc.AsServer, gc.AsClient);
			CurrentIdentity.Reset();
			IsStudio = gc.AsStudio;

			// common thingies
			for (int i = 0; i < args.Length; i++)
			{
				switch (args[i])
				{
					case "--placeor":
						{
							CurrentIdentity.PlaceName = args[++i];
							break;
						}
					case "--univor":
						{
							CurrentIdentity.UniverseName = args[++i];
							break;
						}
					case "--maxplayers":
						{
							CurrentIdentity.MaxPlayerCount = uint.Parse(args[++i]);
							break;
						}
					case "--rbxl":
						{
							rbxlinit = args[++i];
							break;
						}
				}
			}

			LogManager.LogInfo("Initializing verbs...");
			Verbs.Add(',', () => RenderManager.DisableAllGuis = !RenderManager.DisableAllGuis);

			LogManager.LogInfo("Initializing internal scripts...");
			CurrentRoot = new DataModel(this);

			ProhibitProcessing = gc.ProhibitProcessing;
			ProhibitScripts = gc.ProhibitScripts;

			if (NetworkManager.IsServer)
			{
				LogManager.LogInfo("Creating main services...");
				CurrentRoot.GetService<Workspace>();
				CurrentRoot.GetService<Players>();
				CurrentRoot.GetService<Lighting>();
				CurrentRoot.GetService<ReplicatedStorage>();
				CurrentRoot.GetService<ReplicatedFirst>();
				CurrentRoot.GetService<StarterGui>();
				CurrentRoot.GetService<StarterPack>();
				CurrentRoot.GetService<ServerStorage>();
				CurrentRoot.GetService<ScriptContext>();
				CurrentRoot.GetService<PlatformService>();
				CurrentRoot.GetService<UserInputService>();
				CurrentRoot.GetService<Debris>();
			}

			var rs = CurrentRoot.GetService<RunService>();
			var cg = CurrentRoot.GetService<CoreGui>();
			rs.Parent = CurrentRoot;
			cg.Parent = CurrentRoot;

			LuaRuntime.Setup(this, CurrentRoot);
			LoadAllCoreScripts();

			LogManager.LogInfo("Initializing PhysicsManager...");
			PhysicsManager = new(this);

			LogManager.LogInfo("Initializing RenderManager...");
			RenderManager = new(this, gc.SkipWindowCreation, !gc.DoNotRenderAtAll, gc.VersionMargin);

			if (NetworkManager.IsClient)
			{
				CurrentRoot.GetService<CoreGui>().ShowTeleportGui("", "", -1, -1);
				QueuedTeleportAddress = rbxlinit;
			}
			if (NetworkManager.IsServer)
			{
				AddedInstance += x =>
				{
					if (NetworkManager.Server == null) return;

					if (x.GetType().GetCustomAttribute<NotReplicatedAttribute>() == null)
						NetworkManager.AddReplication(x, NetworkManager.Replication.REPM_TOALL, NetworkManager.Replication.REPW_NEWINST);
				};
			}
			servercallback(rbxlinit, this);
		}
		public void Shutdown()
		{
			LogManager.LogInfo($"Shutting down GameManager \"{ManagerName}\"...");
			ShuttingDown = true;
			ShutdownEvent?.Invoke(new(), new());
			AppManager.GameManagers.Remove(this);

			if (AppManager.CurrentRenderManager == RenderManager)
				AppManager.CurrentRenderManager = null;

			if (RenderManager != null)
				RenderManager.Unload();
			RenderManager = null;

			if (MainManager)
				Environment.Exit(0);
		}
		public Instance? GetInstance(Guid id)
		{
			for (int i = 0; i < AllInstances.Count; i++)
			{
				if (AllInstances[i].UniqueID == id)
					return AllInstances[i];
			}
			return null;
		}
		public void ProcessInstance(Instance inst)
		{
			if (inst != null)
			{ // i was outsmarted
				if (inst.DestroyAt < DateTime.Now)
				{
					inst.Destroy();
					return;
				}

				inst.Process();

				var ch = inst.GetChildren();
				for (int i = 0; i < ch.Length; i++)
				{
					ProcessInstance(ch[i]);
				}
			}
		}
		public string FilterString(string text)
		{
			if (!UsePublicService)
			{
				return "lol";
			}
			return "c'est ma chatte";
		}
	}
}
