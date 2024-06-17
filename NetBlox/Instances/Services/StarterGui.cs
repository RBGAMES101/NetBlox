﻿using MoonSharp.Interpreter;
using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetBlox.Instances.Services
{
	public class StarterGui : Instance
	{
		public Dictionary<string, DynValue> RegisteredSetCallbacks = [];
		public Dictionary<string, DynValue> RegisteredGetCallbacks = [];

		public StarterGui(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(StarterGui) == classname) return true;
			return base.IsA(classname);
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void RegisterSetCore(string name, DynValue func)
		{
			if (func.Type != DataType.Function) throw new Exception($"RegisterSetCore only accepts functions");
			RegisteredSetCallbacks[name] = func;
		}
		[Lua([Security.Capability.CoreSecurity])]
		public void RegisterGetCore(string name, DynValue func)
		{
			if (func.Type != DataType.Function) throw new Exception($"RegisterGetCore only accepts functions");
			RegisteredGetCallbacks[name] = func;
		}
		[Lua([Security.Capability.None])]
		public void SetCore(string name, DynValue dv)
		{
			if (RegisteredSetCallbacks.ContainsKey(name)) LuaRuntime.Execute(RegisteredSetCallbacks[name], 
				(LuaRuntime.CurrentThread != null ? LuaRuntime.CurrentThread.Value.Level : 3), GameManager, null, [dv]);
			throw new Exception($"\"{name}\" has not been registered by CoreScripts");
		}
		[Lua([Security.Capability.None])]
		public DynValue GetCore(string name)
		{
			throw new Exception($"im not sure yielding works here");
		}
	}
}
