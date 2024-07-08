﻿using MoonSharp.Interpreter;
using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetBlox.Instances.Services
{
	public class RunService : Instance
	{
		[Lua([Security.Capability.None])]
		public LuaSignal Heartbeat { get; init; } = new();

		public RunService(GameManager gm) : base(gm) 
		{
			Name = "Run Service";
		}

		[Lua([Security.Capability.RobloxScriptSecurity])]
		public void Pause() => GameManager.IsRunning = false;
		[Lua([Security.Capability.RobloxScriptSecurity])]
		public void Run() => GameManager.IsRunning = true;
		[Lua([Security.Capability.RobloxScriptSecurity])]
		public void Stop() => GameManager.Shutdown();
		[Lua([Security.Capability.CoreSecurity])]
		public void SetProcessorPriority(int priority) => AppManager.GameProcessor.Priority = priority;
		[Lua([Security.Capability.CoreSecurity])]
		public void SetRendererPriority(int priority) => AppManager.GameRenderer.Priority = priority;
		[Lua([Security.Capability.None])]
		public bool IsClient() => GameManager.NetworkManager.IsClient;
		[Lua([Security.Capability.None])]
		public bool IsServer() => GameManager.NetworkManager.IsServer;
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(RunService) == classname) return true;
			return base.IsA(classname);
		}
		public override void Process()
		{
			base.Process();
			Heartbeat.Fire(DynValue.NewNumber(TaskScheduler.LastCycleTime.TotalSeconds));
		}
	}
}
