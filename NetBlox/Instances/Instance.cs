﻿using MoonSharp.Interpreter;
using NetBlox.Runtime;
using NetBlox.Structs;
using Qu3e;
using System.Text.Json.Serialization;

namespace NetBlox.Instances
{
	public class Instance
	{
		[Lua([Security.Capability.None])]
		public bool Archivable { get; set; } = true;
		[Lua([Security.Capability.None])]
		public string ClassName => GetType().Name;
		[Lua([Security.Capability.None])]
		public string Name { get; set; }
		[Lua([Security.Capability.None])]
		[NotReplicated]
		public Instance? Parent
		{
			get => parent;
			set
			{
				if (WasDestroyed) return;

				if (parent != null)
				{
					parent.Children.Remove(this);
					if (Root.MainEnv != null)
						parent.ChildRemoved.Fire(DynValue.NewTable(LuaRuntime.MakeInstanceTable(this, GameManager)));
				}
				if (value != null)
				{
					parent = value;
					ParentID = parent.UniqueID;
					value.Children.Add(this);
					if (Root.MainEnv != null)
						value.ChildAdded.Fire(DynValue.NewTable(LuaRuntime.MakeInstanceTable(this, GameManager)));
				}
				else
				{
					parent = null;
					ParentID = Guid.Empty;
				}
			}
		}
		[NotReplicated]
		public List<string> Tags { get; set; } = new();
		[NotReplicated]
		public Guid ParentID { get; set; }
		[NotReplicated]
		public Guid UniqueID { get; set; }
		[Lua([Security.Capability.None])]
		[NotReplicated]
		public LuaSignal ChildAdded { get; init; } = new();
		[Lua([Security.Capability.None])]
		[NotReplicated]
		public LuaSignal ChildRemoved { get; init; } = new();
		[Lua([Security.Capability.None])]
		[NotReplicated]
		public LuaSignal Destroying { get; init; } = new();
		public virtual Security.Capability[] RequiredCapabilities => [];
		public bool WasDestroyed = false;
		public bool WasReplicated = false;
		public GameManager GameManager;
		public NetworkClient? NetworkOwner;
		public bool IAmOwner = false;
		public List<Instance> Children = new();
		public Dictionary<Script, Table> Tables = new();
		public DateTime DestroyAt = DateTime.MaxValue;
		private Instance? parent;
		protected DataModel Root => GameManager.CurrentRoot;

		public Instance(GameManager gm)
		{
			Name = ClassName;
			UniqueID = Guid.NewGuid();
			GameManager = gm;

			gm.AllInstances.Add(this);
			gm.InvokeAddedEvent(this);
		}
		public Instance(GameManager gm, Guid guid)
		{
			Name = ClassName;
			UniqueID = guid;
			WasReplicated = true;
			GameManager = gm;

			gm.AllInstances.Add(this);
			gm.InvokeAddedEvent(this);
		}

		public virtual void Process()
		{
			// process nothing
		}
		public virtual void RenderUI()
		{
			// render nothing
		}
		[Lua([Security.Capability.None])]
		public virtual void AddTag(string tag)
		{
			if (!Tags.Contains(tag))
				Tags.Add(tag);
		}
		[Lua([Security.Capability.None])]
		public virtual Instance? Clone()
		{
			if (!Archivable)
				return null;
			// i tried
			// maybe i did it
			Dictionary<Instance, Instance> clonemapping = new();
			List<Instance> dolater = new();

			Instance? DoClone(Instance? inst)
			{
				var clone = (Instance)Activator.CreateInstance(inst.GetType(), GameManager)!;
				var props = SerializationManager.GetAccessibleProperties(clone);
				for (int i = 0; i < props.Length; i++)
				{
					var prop = SerializationManager.GetProperty(inst, props[i]);
					var ptyp = SerializationManager.GetPropertyType(clone, props[i]);
					if (SerializationManager.IsReadonly(clone, props[i]))
						continue;
					if (ptyp.IsAssignableTo(typeof(Script))) continue;
					if (ptyp.IsAssignableTo(typeof(Scene))) continue; 
					if (ptyp.IsAssignableTo(typeof(Instance)) && prop != null)
					{
						var ogval = (Instance)prop;
						if (clonemapping.ContainsKey(ogval))
							SerializationManager.SetProperty(clone, props[i], clonemapping[ogval]);
						else
							dolater.Add(clone);
					}
					else
						SerializationManager.SetProperty(clone, props[i], prop);
				}

				clonemapping[inst] = clone;

				for (int i = 0; i < inst.Children.Count; i++)
					if (inst.Children[i].Archivable)
						DoClone(inst.Children[i]).Parent = clone;

				return clone;
			}

			for (int i = 0; i < dolater.Count; i++)
			{
				var inst = dolater[i];
				var props = SerializationManager.GetAccessibleProperties(inst);
				for (int j = 0; j < props.Length; j++)
				{
					var prop = SerializationManager.GetProperty(inst, props[j]);
					var ptyp = SerializationManager.GetPropertyType(inst, props[j]);
					if (SerializationManager.IsReadonly(inst, props[j]))
						continue;
					if (ptyp.IsAssignableTo(typeof(Instance)) && prop != null)
					{
						var ogval = (Instance)prop;
						SerializationManager.SetProperty(inst, props[j], clonemapping[ogval]); // i HOPE that every inst reference will be resolved this way
					}
				}
			}

			return DoClone(this);
		}
		[Lua([Security.Capability.None])]
		public virtual Instance ForceClone()
		{
			// i tried
			// maybe i did it
			Dictionary<Instance, Instance> clonemapping = new();
			List<Instance> dolater = new();

			Instance DoClone(Instance inst)
			{
				var clone = (Instance)Activator.CreateInstance(inst.GetType(), GameManager)!;
				var props = SerializationManager.GetAccessibleProperties(clone);
				for (int i = 0; i < props.Length; i++)
				{
					var prop = SerializationManager.GetProperty(inst, props[i]);
					var ptyp = SerializationManager.GetPropertyType(clone, props[i]);
					if (SerializationManager.IsReadonly(clone, props[i]))
						continue;
					if (ptyp.IsAssignableTo(typeof(Instance)) && prop != null)
					{
						var ogval = (Instance)prop;
						if (clonemapping.ContainsKey(ogval))
							SerializationManager.SetProperty(clone, props[i], clonemapping[ogval]);
						else
							dolater.Add(clone);
					}
					else
						SerializationManager.SetProperty(clone, props[i], prop);
				}

				clonemapping[inst] = clone;

				for (int i = 0; i < inst.Children.Count; i++)
					DoClone(inst.Children[i]).Parent = clone;

				return clone;
			}

			for (int i = 0; i < dolater.Count; i++)
			{
				var inst = dolater[i];
				var props = SerializationManager.GetAccessibleProperties(inst);
				for (int j = 0; j < props.Length; j++)
				{
					var prop = SerializationManager.GetProperty(inst, props[j]);
					var ptyp = SerializationManager.GetPropertyType(inst, props[j]);
					if (SerializationManager.IsReadonly(inst, props[j]))
						continue;
					if (ptyp.IsAssignableTo(typeof(Instance)) && prop != null)
					{
						var ogval = (Instance)prop;
						SerializationManager.SetProperty(inst, props[j], clonemapping[ogval]); // i HOPE that every inst reference will be resolved this way
					}
				}
			}

			return DoClone(this);
		}
		[Lua([Security.Capability.None])]
		public virtual void ClearAllChildren()
		{
			for (int i = 0; i < Children.Count; i++) Children[i].Destroy();
			Children.Clear();
		}
		[Lua([Security.Capability.None])]
		public virtual void Destroy()
		{
			if (!WasDestroyed)
			{
				Destroying.Fire();

				Parent = null;
				ClearAllChildren();
				GameManager.AllInstances.Remove(this);

				WasDestroyed = true;
			}
		}
		[Lua([Security.Capability.None])]
		public virtual Instance? FindFirstAncestor(string name)
		{
			if (Parent == null) return null;
			if (Parent.Name == name) return Parent;
			else return Parent.FindFirstAncestor(name);
		}
		[Lua([Security.Capability.None])]
		public virtual Instance? FindFirstAncestorOfClass(string cl)
		{
			if (Parent == null) return null;
			if (Parent.ClassName == cl) return Parent;
			else return Parent.FindFirstAncestorOfClass(cl);
		}
		[Lua([Security.Capability.None])]
		public virtual Instance? FindFirstAncestorWhichIsA(string cl)
		{
			if (Parent == null) return null;
			if (Parent.IsA(cl)) return Parent;
			else return Parent.FindFirstAncestorWhichIsA(cl);
		}
		[Lua([Security.Capability.None])]
		public virtual Instance? FindFirstChild(string name)
		{
			for (int i = 0; i < Children.Count; i++)
				if (Children[i].Name == name)
					return Children[i];

			return null;
		}
		[Lua([Security.Capability.None])]
		public virtual Instance? FindFirstChildOfClass(string cl)
		{
			for (int i = 0; i < Children.Count; i++)
				if (Children[i].ClassName == cl)
					return Children[i];

			return null;
		}
		[Lua([Security.Capability.None])]
		public virtual Instance? FindFirstChildWhichIsA(string cl)
		{
			for (int i = 0; i < Children.Count; i++)
				if (Children[i].IsA(cl))
					return Children[i];

			return null;
		}
		[Lua([Security.Capability.None])]
		public virtual Instance? FindFirstDescendant(string name)
		{
			for (int i = 0; i < Children.Count; i++)
				if (Children[i].Name == name)
					return Children[i];

			for (int i = 0; i < Children.Count; i++)
			{
				var child = Children[i];
				var descendant = child.FindFirstDescendant(name);
				if (descendant != null) return descendant;
			}

			return null;
		}
		[Lua([Security.Capability.None])]
		public virtual Instance[] GetChildren() => Children.ToArray();
		[Lua([Security.Capability.None])]
		public virtual Instance[] GetDescendants()
		{
			var list = new List<Instance>(Children);

			for (int i = 0; i < Children.Count; i++)
				list.AddRange(Children[i].GetDescendants());

			return list.ToArray();
		}
		[Lua([Security.Capability.None])]
		public virtual Instance[] GetAncestors()
		{
			if (Parent == null) return [];

			var list = new List<Instance>();
			var inst = Parent;

			while (inst != null)
			{
				list.Add(inst);
				inst = inst.Parent!;
			}

			return list.ToArray();
		}
		[Lua([Security.Capability.None])]
		public virtual string GetFullName()
		{
			if (parent == null) return Name;

			var strings = new List<string>();
			var inst = Parent!;

			strings.Add(Name);

			while (inst != null && !inst.IsA("DataModel"))
			{
				strings.Add(inst.Name);
				inst = inst.Parent!;
			}

			strings.Reverse();
			return string.Join('.', strings);
		}
		[Lua([Security.Capability.None])]
		public virtual bool IsDescendantOf(Instance instance) => GetAncestors().Contains(instance);
		[Lua([Security.Capability.None])]
		public virtual bool IsAncestorOf(Instance instance) => GetDescendants().Contains(instance);
		[Lua([Security.Capability.None])]
		public virtual string[] GetTags() => Tags.ToArray();
		[Lua([Security.Capability.None])]
		public virtual bool HasTag(string tag) => Tags.Contains(tag);
		[Lua([Security.Capability.None])]
		public virtual void RemoveTag(string tag) => Tags.Remove(tag);
		[Lua([Security.Capability.None])]
		public virtual bool IsA(string classname) => nameof(Instance) == classname;
		private void ChangeOwnershipImpl(GameManager gm)
		{
			GameManager.AllInstances.Remove(this);
			GameManager = gm;
			NetworkOwner = null;
			WasReplicated = false;
			WasDestroyed = false;
			GameManager.AllInstances.Add(this);

			for (int i = 0; i < Children.Count; i++)
			{
				Children[i].ChangeOwnershipImpl(gm);
			}
		}
		public void ChangeOwnership(GameManager gm)
		{
			Parent = null;
			ChangeOwnershipImpl(gm);
		}
		public int CountDescendants()
		{
			lock (Children)
			{
				int sum = Children.Count;
				for (int i = 0; i < Children.Count; i++)
					sum += Children[i].CountDescendants();
				return sum;
			}
		}
		[Lua([Security.Capability.None])]
		public LuaYield<Instance> WaitForChild(string name)
		{
			var n = new LuaYield<Instance>();

			for (int i = 0; i < Children.Count; i++)
			{
				if (name == Children[i].Name)
				{
					n.HasResult = true;
					n.Result = Children[i];
					return n;
				}
			}

			n.HasResult = false;
			n.Result = null;
			return n;
		}
		public void ReplicateProps()
		{
			if (GameManager.NetworkManager.RemoteConnection != null)
				GameManager.NetworkManager.AddReplication(this, NetworkManager.Replication.REPM_BUTOWNER, NetworkManager.Replication.REPW_PROPCHG, false);
		}
	}
}
