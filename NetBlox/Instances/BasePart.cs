﻿using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Structs;
using Qu3e;
using System.Numerics;

namespace NetBlox.Instances
{
	public struct PartRenderCache
	{
		public int DirtyCounter;
		public Dictionary<Vector3, float>? AFSCache;
	}
	public class BasePart : PVInstance, I3DRenderable
	{
		[Lua([Security.Capability.None])]
		public bool Anchored
		{
			get => _anchored;
			set
			{
				_anchored = false; // temporary

				if (Body != null)
				{
					if (!_anchored)
					{
						Body.Flags &= ~BodyFlags.eStatic;
						Body.Flags |= BodyFlags.eDynamic;
					}
					else
					{
						Body.Flags &= ~BodyFlags.eDynamic;
						Body.Flags |= BodyFlags.eStatic;
					}
				}
			}
		}
		[Lua([Security.Capability.None])]
		public bool Locked { get; set; }
		[Lua([Security.Capability.None])]
		public SurfaceType FrontSurface { get; set; }
		[Lua([Security.Capability.None])]
		public SurfaceType BackSurface { get; set; }
		[Lua([Security.Capability.None])]
		public SurfaceType TopSurface { get; set; } = SurfaceType.Studs;
		[Lua([Security.Capability.None])]
		public SurfaceType BottomSurface { get; set; }
		[Lua([Security.Capability.None])]
		public SurfaceType LeftSurface { get; set; }
		[Lua([Security.Capability.None])]
		public SurfaceType RightSurface { get; set; }
		[Lua([Security.Capability.None])]
		public Color Color3 { get; set; } = Color.Gray;
		[Lua([Security.Capability.None])]
		public BrickColor BrickColor
		{
			get => _brickColor;
			set
			{
				_brickColor = value;
				Color3 = _brickColor.Color;
			}
		}
		private BrickColor _brickColor;
		[Lua([Security.Capability.None])]
		public Vector3 Position
		{
			get => _position;
			set
			{
				if (_position == value)
					return;
				_position = value;
				Body?.SetTransform(value);
			}
		}
		[Lua([Security.Capability.None])]
		public Vector3 Rotation
		{
			get => _rotation;
			set
			{
				if (_rotation == value)
					return;
				_rotation = value;
				Body?.SetTransform(_position, value);
			}
		}
		[Lua([Security.Capability.None])]
		public Vector3 Size
		{
			get => _size;
			set
			{
				_size = value;
				if (Box != null)
				{
					BoxDef = new BoxDef();
					BoxDef.Set(Qu3e.Transform.Identity, _size);
					Body.RemoveBox(Box);
					var Tx = Box.local;
					Box = Body.AddBox(BoxDef);
					Box.SetUserdata(this);
					Box.local = Tx;
				}
			}
		}
		[Lua([Security.Capability.None])]
		public Vector3 size { get => Size; set => Size = value; }
		[Lua([Security.Capability.None])]
		public bool CanCollide { get; set; } = true;
		[Lua([Security.Capability.None])]
		public bool CanTouch { get; set; } = true;
		[Lua([Security.Capability.None])]
		public double Transparency { get; set; } = 0;
		[Lua([Security.Capability.None])]
		public Vector3 Velocity
		{
			get => _lastvelocity;
			set
			{
				if (_lastvelocity == value)
					return;
				_lastvelocity = value;
				Body?.SetLinearVelocity(_lastvelocity);
			}
		}
		public PartRenderCache RenderCache = new();
		public Lighting? LocalLighing;
		public bool IsGrounded = false;
		public BoxDef BoxDef;
		public Body Body;
		public Box? Box;
		public Vector3 _size;
		public Vector3 _lastvelocity;

		public BasePart(GameManager ins) : base(ins)
		{
			var works = Root.GetService<Workspace>(true);
			if (works != null)
			{
				Scene sc = works.Scene;
				BodyDef bodyDef = new();
				bodyDef.position.Set(0, 0, 0);
				bodyDef.bodyType = !Anchored ? BodyType.eDynamicBody : BodyType.eStaticBody;
				Body body = sc.CreateBody(bodyDef);
				BoxDef = new BoxDef();
				BoxDef.Set(Qu3e.Transform.Identity, Size);
				Box = body.AddBox(BoxDef);
				Box.SetUserdata(this);
				Body = body;

				GameManager.PhysicsManager.Actors.Add(this);
			}
		}
		public virtual void Render()
		{
			if (LocalLighing != null && LocalLighing.WasDestroyed)
				LocalLighing = null;

			if (LocalLighing == null)
			{
				LocalLighing = Root.GetService<Lighting>(true);
				return;  // now parts REQUIRE Lighting service to be present in order to render (because sun)
			}
		}
		public override void Process() => base.Process();
		public override bool IsA(string classname) => nameof(BasePart) == classname || base.IsA(classname);
		public override void Destroy()
		{
			if (Body != null)
			{
				GameManager.PhysicsManager.Actors.Remove(this);
				Scene sc = GameManager.CurrentRoot.GetService<Workspace>().Scene;
				lock (sc)
				{
					sc.RemoveBody(Body);
					Body = null!;
					Box = null!;
				}
			}
			base.Destroy();
		}
		public override void SetPivot(CFrame pivot)
		{
			Position = pivot.Position;
			Rotation = pivot.Rotation;
			PivotOffset = default;
		}
	}
}
