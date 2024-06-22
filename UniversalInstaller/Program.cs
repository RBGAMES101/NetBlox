﻿using System;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UniversalInstaller
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			if (OperatingSystem.IsWindows())
			{
				Console.WriteLine("[+] Running on Windows, determining whether NetBlox is installed...");
				if (File.Exists("./NetBloxClient.exe"))
				{
					Console.WriteLine("[+] NetBlox is installed, checking its version...");
					var proc = Process.Start(new ProcessStartInfo()
					{
						FileName = Path.GetFullPath("./NetBloxClient.exe"),
						Arguments = "check",
						RedirectStandardOutput = true,
						CreateNoWindow = true
					});
					var ver = "";
					proc.OutputDataReceived += (x, y) =>
					{
						string target = y.Data;
						ver = target.Substring(target.IndexOf('('), target.IndexOf('(') - target.IndexOf(')'));
					};
					if (!proc.WaitForExit(500))
					{
						proc.Kill();
						Console.WriteLine("[+] NetBlox Client does not respond, downloading latest...");
						await DownloadLatest();
					}
					else
					{
						GetLatestVersion(ver);
					}
				}
				else
				{
					Console.WriteLine("[+] No NetBlox is installed, downloading it...");
					DownloadLatest().Wait();
				}
			}
			else
			{
				Console.WriteLine("[-] Bootstrapper does not currently support non-Windows systems...");
				Environment.Exit(1);
			}
		}
		static async Task<string> GetLatestVersion(string ver)
		{
			try
			{
				var hc = new HttpClient();
				var req = new HttpRequestMessage()
				{
					RequestUri = new Uri("https://raw.githubusercontent.com/AsertCreator/NetBlox/master/NetBlox.Common/Version.cs")
				};
				req.Headers.Add("User-Agent", "NetBloxInstallerv" +
					NetBlox.Common.Version.VersionMajor + "." + NetBlox.Common.Version.VersionMinor + "." + NetBlox.Common.Version.VersionPatch);

				var data = await ((await hc.SendAsync(req)).Content.ReadAsStringAsync());

				Console.WriteLine("Code: " + data);

				return data;
			}
			catch
			{
				Console.WriteLine($"[-] Unable to request GitHub for latest version, exiting...");
				Environment.Exit(1);
				return "";
			}
		}
		static async Task DownloadLatest()
		{
			try
			{
				var hc = new HttpClient();
				var req = new HttpRequestMessage()
				{
					RequestUri = new Uri("https://api.github.com/repos/AsertCreator/NetBlox/actions/artifacts")
				};
				req.Headers.Add("User-Agent", "NetBloxInstallerv" +
					NetBlox.Common.Version.VersionMajor + "." + NetBlox.Common.Version.VersionMinor + "." + NetBlox.Common.Version.VersionPatch);
				req.Headers.Add("Accept", "application/vnd.github+json");
				req.Headers.Add("Authorization", "Bearer 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"); // it doesn't have any scopes.
				req.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

				var data = await ((await hc.SendAsync(req)).Content.ReadAsStringAsync());
				var json = JsonDocument.Parse(data);

				try
				{
					var artifacts = json.RootElement.GetProperty("artifacts");
					var latest = artifacts[0].GetProperty("archive_download_url").GetString();

					Console.WriteLine($"[+] Downloading from {latest}...");
					req = new HttpRequestMessage()
					{
						RequestUri = new Uri(latest)
					};
					req.Headers.Add("User-Agent", "NetBloxInstallerv" +
						NetBlox.Common.Version.VersionMajor + "." + NetBlox.Common.Version.VersionMinor + "." + NetBlox.Common.Version.VersionPatch);
					req.Headers.Add("Accept", "application/vnd.github+json");
					req.Headers.Add("Authorization", "Bearer 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");
					req.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

					var str = (await hc.SendAsync(req)).Content.ReadAsStream();
					ZipArchive za = new(str);
					za.ExtractToDirectory(".");
					Console.WriteLine($"[+] Downloaded successfully!");
				}
				catch
				{
					Console.WriteLine($"[-] Unable to download latest version, though link is known, exiting...");
					Environment.Exit(1);
				}
			}
			catch
			{
				Console.WriteLine($"[-] Unable to request GitHub for latest version, exiting...");
				Environment.Exit(1);
			}
		}
	}
}
