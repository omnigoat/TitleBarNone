using Atma.TitleBarNone.Settings;
using EnvDTE;
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
		public static VsrResolver Create(Models.SolutionModel solutionModel)
		{
			return new VsrResolver(solutionModel);
		}

		public VsrResolver(Models.SolutionModel solutionModel)
			: base(new[] { "vsr", "vsr-branch", "vsr-sha" })
		{
			OnSolutionOpened(solutionModel.StartupSolution);

			solutionModel.SolutionOpened += OnSolutionOpened;
			solutionModel.SolutionClosed += OnSolutionClosed;
		}

		public override bool Available => vsrPath != null;

		public override ChangedDelegate Changed { get; set; }

		public override bool ResolveBoolean(VsState state, string tag)
		{
			return tag == "vsr";
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

		private void OnSolutionOpened(Solution solution)
		{
			if (string.IsNullOrEmpty(solution?.FileName))
				return;

			var solutionDir = new FileInfo(solution.FileName).Directory;

			vsrPath = GetAllParentDirectories(solutionDir)
				.SelectMany(x => x.GetDirectories())
				.FirstOrDefault(x => x.Name == ".versionr")?.FullName;

			if (vsrPath != null)
			{
				m_Watcher = new FileSystemWatcher(vsrPath)
				{
					IncludeSubdirectories = true
				};

				m_Watcher.Changed += VsrFolderChanged;
				m_Watcher.EnableRaisingEvents = true;

				ReadInfo();
			}
		}

		private void OnSolutionClosed()
		{
			vsrPath = null;
			if (m_Watcher != null)
			{
				m_Watcher.EnableRaisingEvents = false;
				m_Watcher.Dispose();
			}
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
					RedirectStandardOutput = true,
					WorkingDirectory = new DirectoryInfo(vsrPath).Parent.FullName.ToString()
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
			bool changed = false;

			// parse branch
			{
				var match = Regex.Match(lines[0], "on branch \"([a-zA-Z0-9_-]+)\"");
				if (match.Success && m_VsrBranch != match.Groups[1].Value)
				{
					m_VsrBranch = match.Groups[1].Value;
					changed = true;
				}
			}

			// parse SHA
			{
				var match = Regex.Match(lines[0], "Version ([a-fA-F0-9-]+)");
				if (match.Success)
				{
					m_VsrSHA = match.Groups[1].Value;
					changed = true;
				}
			}

			if (changed)
			{
				Changed?.Invoke(this);
			}
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

		private string vsrPath;
		private FileSystemWatcher m_Watcher;
		private string m_VsrBranch;
		private string m_VsrSHA;
	}
}
