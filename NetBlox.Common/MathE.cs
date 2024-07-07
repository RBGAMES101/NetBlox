﻿using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Common
{
	public static class MathE
	{
		public static float Lerp(float a, float b, float t) => a * (1 - t) + b * t;
		public static string Roll(string str, byte a)
		{
			var bytes = Encoding.ASCII.GetBytes(str);
			for (int i = 0; i < bytes.Length; i++)
				bytes[i] ^= a;
			return Encoding.ASCII.GetString(bytes);
		}
		public static string FormatSize(long bytes)
		{
			if (bytes < 1024) return bytes + " bytes";
			if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("F2") + " kbs";
			if (bytes < 1024 * 1024 * 1024) return (bytes / (1024.0 * 1024.0)).ToString("F2") + " mbs";
			return (bytes / (1024.0 * 1024.0 * 1024.0)).ToString("F2") + " gbs";
		}
	}
}
