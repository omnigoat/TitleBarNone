using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Atma.TitleBarNone.Settings
{
	abstract class ChangeProvider : IDisposable
	{
		public delegate void ChangedEvent(TitleBarFormatTriplet triplet);

		public event ChangedEvent Changed;

		public void Dispose()
		{
			DisposeImpl();
		}

		protected void RaiseChangeEvent(TitleBarFormatTriplet triplet)
		{
			Changed.Invoke(triplet);
		}

		protected abstract void DisposeImpl();
	}

	//
	// FileSystemChangeProvider
	//
	class FileSystemChangeProvider : ChangeProvider
	{
		public FileSystemChangeProvider(string filepath)
		{
			FilePath = filepath;

			// file system watcher
			var directory = System.IO.Path.GetDirectoryName(FilePath);
			m_Watcher = new System.IO.FileSystemWatcher(directory);
			m_Watcher.Changed += Watcher_Changed;
		}

		public string FilePath;

		private void Watcher_Changed(object sender, System.IO.FileSystemEventArgs e)
		{
			ParseFile(out string nothing, out string document, out string solution, FilePath);

			RaiseChangeEvent(new TitleBarFormatTriplet()
			{
				FormatIfNothingOpened  = new TitleBarFormat("solution-file-nothing"),
				FormatIfDocumentOpened = new TitleBarFormat("solution-file-document"),
				FormatIfSolutionOpened = new TitleBarFormat("solution-file-solution")
			});
		}

		// IDisposable implementation
		protected override void DisposeImpl()
		{
			m_Watcher.Dispose();
		}

		void ParseFile(out string nothing, out string document, out string solution, string filepath)
		{
			nothing = null;
			document = null;
			solution = null;

			var lines = System.IO.File.ReadAllLines(filepath).ToList();
			foreach (var line in lines)
			{
				var nothing_result = m_NothingTitleRegex.Match(line);
				if (nothing_result.Success)
				{
					nothing = nothing_result.Groups[1].Value;
					continue;
				}

				var document_result = m_DocumentTitleRegex.Match(line);
				if (document_result.Success)
				{
					document = document_result.Groups[1].Value;
					continue;
				}

			}
		}

		private System.IO.FileSystemWatcher m_Watcher;
		private readonly Regex m_NothingTitleRegex = new Regex("nothing-title: (.+)$");
		private readonly Regex m_DocumentTitleRegex = new Regex("document-title: (.+)$");
		private readonly Regex m_SolutionTitleRegex = new Regex("solution-title: (.+)$");

		private Regex m_NothingColorRegex = new Regex("nothing-color: (.+)$");
		private Regex m_DocumentColorRegex = new Regex("document-color: (.+)$");
		private Regex m_SolutionColorRegex = new Regex("solution-color: (.+)$");

	}
}
