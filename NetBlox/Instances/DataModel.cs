﻿using NetBlox.Instances.Services;
using NetBlox.Runtime;

namespace NetBlox.Instances
{
	public class DataModel : Instance
	{
		[Lua]
		[Replicated]
		public string TestString { get; set; } = "Test";
		[Lua]
		public int PreferredFPS { get => GameManager.PreferredFPS; set => GameManager.SetPreferredFPS(value); }

		[Lua]
		public bool IsLoaded()
		{
			return true;
		}
		[Lua]
		public void Shutdown()
		{
			GameManager.MessageQueue.Enqueue(new Message()
			{
				Type = MessageType.Shutdown
			});
		}
		[Lua]
		public override bool IsA(string classname)
		{
			if (nameof(DataModel) == classname) return true;
			return base.IsA(classname);
		}
	}
}
