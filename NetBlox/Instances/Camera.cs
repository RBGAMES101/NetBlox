﻿using NetBlox.Runtime;
using Raylib_cs;
using System.Numerics;

namespace NetBlox.Instances
{
	[Creatable]
	public class Camera : Instance
	{
		[Lua([Security.Capability.None])]
		public Instance? CameraSubject { get; set; }
		public static Vector2 LastMousePosition;

		public Camera(GameManager ins) : base(ins) 
		{ }

		public override void Process()
		{
			if (CameraSubject == null || !CameraSubject.IsA("BasePart"))
			{
				GameManager.RenderManager.MainCamera.Position = new Vector3(0, 5, -6);
				GameManager.RenderManager.MainCamera.Target = Vector3.Zero;
			}
			else
			{
				var subject = CameraSubject as BasePart ?? throw new Exception("CameraSubject is not BasePart");

				// Camera rotation
				if (Raylib.IsKeyDown(KeyboardKey.Down)) Raylib.CameraPitch(ref GameManager.RenderManager.MainCamera, 0.03f, true, true, false);
				if (Raylib.IsKeyDown(KeyboardKey.Up)) Raylib.CameraPitch(ref GameManager.RenderManager.MainCamera, -0.03f, true, true, false);
				if (Raylib.IsKeyDown(KeyboardKey.Right)) Raylib.CameraYaw(ref GameManager.RenderManager.MainCamera, 0.03f, true);
				if (Raylib.IsKeyDown(KeyboardKey.Left)) Raylib.CameraYaw(ref GameManager.RenderManager.MainCamera, -0.03f, true);

				if (Raylib.IsMouseButtonDown(MouseButton.Right))
				{
					Vector2 mousePositionDelta = Raylib.GetMousePosition() - LastMousePosition;

					// Mouse support
					Raylib.CameraYaw(ref GameManager.RenderManager.MainCamera, -mousePositionDelta.X * 0.003f, true);
					Raylib.CameraPitch(ref GameManager.RenderManager.MainCamera, -mousePositionDelta.Y * 0.003f, true, true, false);

					Raylib.SetMousePosition((int)LastMousePosition.X, (int)LastMousePosition.Y);
				}

				// Zoom target distance
				Raylib.CameraMoveToTarget(ref GameManager.RenderManager.MainCamera, -Raylib.GetMouseWheelMove());
				if (Raylib.IsKeyDown(KeyboardKey.O)) Raylib.CameraMoveToTarget(ref GameManager.RenderManager.MainCamera, 0.2f);
				if (Raylib.IsKeyDown(KeyboardKey.I)) Raylib.CameraMoveToTarget(ref GameManager.RenderManager.MainCamera, -0.2f);

				var diff = GameManager.RenderManager.MainCamera.Target - GameManager.RenderManager.MainCamera.Position;

				GameManager.RenderManager.MainCamera.Position = subject.Position - diff;
				GameManager.RenderManager.MainCamera.Target = subject.Position;
				LastMousePosition = Raylib.GetMousePosition();
			}
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Camera) == classname) return true;
			return base.IsA(classname);
		}
	}
}
