﻿using MoonSharp.Interpreter;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using System.Diagnostics;

namespace NetBlox.Instances
{
	public class DataModel : ServiceProvider
	{
		public Dictionary<Scripts.ModuleScript, DynValue> LoadedModules = new();
		public static HttpClient HttpClient = new();
		public Script MainEnv = null!;

		public DataModel(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public bool IsLoaded()
		{
			return true;
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void Clear()
		{
			for (int i = 0; i < Children.Count; i++)
			{
				var child = Children[i];
				if (child is CoreGui) continue;
				else if (child is RunService || child is ScriptContext || child is UserInputService || child is Debris) child.ClearAllChildren();
				else child.Destroy();
			}
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void Shutdown()
		{
			GameManager.Shutdown();
		}
		[Lua([Security.Capability.CoreSecurity])]
		public string HttpGet(string url)
		{
			var task = HttpClient.GetAsync(url);
			task.Wait();
			var task2 = task.Result.Content.ReadAsStringAsync();
			task2.Wait();
			return task2.Result;
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(DataModel) == classname) return true;
			return base.IsA(classname);
		}
	}
}
