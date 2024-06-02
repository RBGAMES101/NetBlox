﻿using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Instances;
using System.Text.Json.Serialization;

namespace NetBlox.Instances
{
	[Creatable]
	public class Player : Instance
	{
		[Lua([Security.Capability.None])]
		[NotReplicated]
		public Instance? Character { get; set; }
		public bool IsLocalPlayer;

		public Player(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public void LoadCharacter()
		{
			var ch = new Character(GameManager);
			var workspace = Root.GetService<Workspace>();

			if (Character != null)
				Character.Destroy();

			ch.Parent = workspace;
			ch.Name = Name;

			if (workspace.MainCamera != null)
				(workspace.MainCamera as Camera)!.CameraSubject = ch;

			Character = ch;
		}
		[Lua([Security.Capability.None])]
		public void Kick(string msg)
		{
			if (IsLocalPlayer)
			{
				GameManager.NetworkManager.ServerConnection.Close(Network.Enums.CloseReason.ClientClosed);
				GameManager.RenderManager.ShowKickMessage(msg);
				// why not call lua api lol
				Root.GetService<RunService>().Pause();
			}
			else if (GameManager.NetworkManager.IsServer)
			{
				// do smth
			}
			else
			{
				// we're trying to kick another player from client
				throw new Exception("Cannot kick non-local player from client");
			}
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Player) == classname) return true;
			return base.IsA(classname);
		}
		[Lua([Security.Capability.None])]
		public override void Destroy() // also destroy character
		{
			base.Destroy();
			Character?.Destroy();
			Kick("Player has been removed from this DataModel");
		}
	}
}
