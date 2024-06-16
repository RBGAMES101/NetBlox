﻿using System.Diagnostics;
using System.Text;

namespace NetBlox
{
	public static class LogManager
	{
		public static StringBuilder Log = new();

		public static void LogInfo(string message)
		{
			string fm = $"[{DateTime.Now:R}][nb-info] {message}";
			Log.AppendLine(fm);
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(fm);
			Console.ResetColor();
		}
		public static void LogWarn(string message)
		{
			string fm = $"[{DateTime.Now:R}][nb-warn] {message}";
			Log.AppendLine(fm);
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(fm);
			Console.ResetColor();
		}
		public static void LogError(string message)
		{
			string fm = $"[{DateTime.Now:R}][nb-error] {message}";
			Log.AppendLine(fm);
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(fm);
			Console.ResetColor();
		}
	}
}
