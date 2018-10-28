using Atma.TitleBarNone.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Atma.TitleBarNone.Resolvers
{
	class VsrResolver : Resolver
	{
		public static bool Required(out string outpath, string path)
		{
			outpath = GetAllParentDirectories(new DirectoryInfo(path))
				.SelectMany(x => x.GetDirectories())
				.FirstOrDefault(x => x.Name == ".versionr")?.FullName;

			return outpath != null;
		}

		public VsrResolver(string gitpath)
			: base(new[] { "vsr", "vsr-branch", "vsr-sha" })
		{
			m_VsrPath = gitpath;

			m_Watcher = new FileSystemWatcher(gitpath);
			m_Watcher.Changed += VsrFolderChanged;

			ReadInfo();
		}

		public override int SatisfiesDependency(SettingsTriplet triplet)
		{
			return triplet.Dependency == PatternDependency.Vsr ? 2 : 0;
		}

		private void VsrFolderChanged(object sender, FileSystemEventArgs e)
		{
			ReadInfo();
		}

		private void ReadInfo()
		{
			var p = new System.Diagnostics.Process()
			{
				StartInfo = new System.Diagnostics.ProcessStartInfo()
				{
					FileName = "vsr.exe",
					Arguments = "info",
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardOutput = true
				}
			};

			p.OutputDataReceived += VsrInfoReceived;
			p.Start();
			p.BeginOutputReadLine();
			p.WaitForExit();
		}


		private void VsrInfoReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
		{
			if (e.Data == null)
				return;
			
			var lines = e.Data.Split('\n');
				
			// parse branch
			{
				var match = Regex.Match(lines[0], "on branch \"([a-zA-Z0-9_-]+)\"");
				if (match.Success)
					m_VsrBranch = match.Groups[1].Value;
			}

			// prase SHA
			{
				var match = Regex.Match(lines[0], "Version ([a-fA-F0-9-]+)");
				if (match.Success)
					m_VsrSHA = match.Groups[1].Value;
			}
		}

		public override string Resolve(VsState state, string tag)
		{
			if (tag == "vsr-branch")
				return m_VsrBranch;
			else if (tag == "vsr-sha")
				return m_VsrSHA;
			else
				return "";
		}

		public override bool ResolveBoolean(VsState state, string tag)
		{
			return tag == "vsr";
		}

		private static IEnumerable<DirectoryInfo> GetAllParentDirectories(DirectoryInfo directoryToScan)
		{
			Stack<DirectoryInfo> ret = new Stack<DirectoryInfo>();
			GetAllParentDirectories(directoryToScan, ref ret);
			return ret;
		}

		private static void GetAllParentDirectories(DirectoryInfo directoryToScan, ref Stack<DirectoryInfo> directories)
		{
			if (directoryToScan == null || directoryToScan.Name == directoryToScan.Root.Name)
				return;

			directories.Push(directoryToScan);
			GetAllParentDirectories(directoryToScan.Parent, ref directories);
		}

		private readonly string m_VsrPath;
		private FileSystemWatcher m_Watcher;
		private string m_VsrBranch;
		private string m_VsrSHA;
	}
}
