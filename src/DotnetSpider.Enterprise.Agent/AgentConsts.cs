﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace DotnetSpider.Enterprise.Agent
{
	public class AgentConsts
	{
		public static string BaseDataDirectory { get; set; }
		public static string RunningLockPath { get; set; }
		public static string AgentIdPath { get; set; }
		public static string ProjectsDirectory { get; set; }
		public static string PackagesDirectory { get; set; }
		public static bool IsRunningOnWindows { get; }
		public static string AgentId { get; set; }
		public static string Ip { get; set; }
		public static string HostName { get; set; }
		public static string Os { get; set; }
		public const string Version = "1.0.0";

		public static void Save()
		{
			File.WriteAllText(AgentIdPath, $"{AgentId}{Environment.NewLine}");
		}

		public static void Load()
		{
			if (File.Exists(AgentIdPath))
			{
				var lines = File.ReadAllLines(AgentIdPath);
				AgentId = lines.FirstOrDefault();
			}
			if (string.IsNullOrEmpty(AgentId))
			{
				AgentId = Guid.NewGuid().ToString("N");
				Save();
			}
		}

		static AgentConsts()
		{
			IsRunningOnWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

			RunningLockPath = Path.Combine(AppContext.BaseDirectory, "agent.lock");
			AgentIdPath = Path.Combine(AppContext.BaseDirectory, "nodeId");
			ProjectsDirectory = Path.Combine(AppContext.BaseDirectory, "projects");
			PackagesDirectory = Path.Combine(AppContext.BaseDirectory, "packages");
			Os = RuntimeInformation.OSDescription;

			if (!Directory.Exists(ProjectsDirectory))
			{
				Directory.CreateDirectory(ProjectsDirectory);
			}
			if (!Directory.Exists(PackagesDirectory))
			{
				Directory.CreateDirectory(PackagesDirectory);
			}
			HostName = Dns.GetHostName();
			if (IsRunningOnWindows)
			{
				var addressList = Dns.GetHostAddressesAsync(HostName).Result;
				IPAddress localaddr = addressList.Where(a => a.AddressFamily == AddressFamily.InterNetwork).ToList()[0];
				Ip = localaddr.ToString();
			}
			else
			{
				Process process = new Process
				{
					StartInfo =
					{
						FileName = "ip",
						Arguments = "address",
						CreateNoWindow = false,
						RedirectStandardOutput = true,
						RedirectStandardInput = true
					}
				};
				process.Start();
				string info = process.StandardOutput.ReadToEnd();
				var lines = info.Split('\n');
				foreach (var line in lines)
				{
					var content = line.Trim();
					if (!string.IsNullOrEmpty(content) && Regex.IsMatch(content, @"^inet.*$"))
					{
						var str = Regex.Match(content, @"[0-9]{0,3}\.[0-9]{0,3}\.[0-9]{0,3}\.[0-9]{0,3}/[0-9]+").Value;
						if (!string.IsNullOrEmpty(str) && !str.Contains("127.0.0.1"))
						{
							Ip = str.Split('/')[0];
						}
					}
				}
				process.WaitForExit();
				process.Dispose();
			}
		}
	}
}