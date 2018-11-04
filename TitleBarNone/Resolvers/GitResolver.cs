using Atma.TitleBarNone.Settings;
using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Atma.TitleBarNone.Resolvers
{
	public class GitResolver : Resolver
	{
		public static bool Required(out string outpath, string path)
		{
			outpath = GetAllParentDirectories(new DirectoryInfo(path))
				.SelectMany(x => x.GetDirectories())
				.FirstOrDefault(x => x.Name == ".git")?.FullName;

			return outpath != null;
		}

		public GitResolver(string gitpath)
			: base(new[] { "git", "git-branch", "git-sha" })
		{
			m_GitPath = gitpath;

			m_Watcher = new FileSystemWatcher(gitpath);
			m_Watcher.Changed += GitFolderChanged;

			ReadBranch();
		}

		public override ChangedDelegate Changed { get; set; }

		public override int SatisfiesDependency(SettingsTriplet triplet)
		{
			return triplet.Dependency == PatternDependency.Git ? 2 : 0;
		}

		private void GitFolderChanged(object sender, FileSystemEventArgs e)
		{
			ReadBranch();
		}

		private void ReadBranch()
		{
			var p = new System.Diagnostics.Process()
			{
				StartInfo = new System.Diagnostics.ProcessStartInfo()
				{
					FileName = "git.exe",
					Arguments = "symbolic-ref -q --short HEAD",
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardOutput = true
				}
			};

			p.OutputDataReceived += GitBranchReceived;
			p.Start();
			p.BeginOutputReadLine();
			p.WaitForExit();
		}


		private void GitBranchReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
		{
			if (e.Data != null && m_GitBranch != e.Data)
			{
				m_GitBranch = e.Data;
				Changed?.Invoke(this);
			}
		}

		public override string Resolve(VsState state, string tag)
		{
			if (tag == "git-branch")
				return m_GitBranch;
			else
				return "";
		}

		public override bool ResolveBoolean(VsState state, string tag)
		{
			return tag == "git";
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

		private readonly string m_GitPath;
		private FileSystemWatcher m_Watcher;
		private string m_GitBranch = "";
	}
}
