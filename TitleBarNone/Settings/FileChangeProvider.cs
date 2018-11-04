using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Media;

namespace Atma.TitleBarNone.Settings
{
	class FileChangeProvider : ChangeProvider
	{
		public FileChangeProvider(string filepath)
		{
			FilePath = filepath;
			if (!File.Exists(FilePath) || Path.GetFileName(FilePath) != Defaults.ConfgFileName)
				return;

			// file system watcher
			WatchingDirectory = Path.GetDirectoryName(FilePath);
			m_Watcher = new FileSystemWatcher(WatchingDirectory, Defaults.ConfgFileName);
			m_Watcher.Changed += Watcher_Changed;

			Watcher_Changed(null, new FileSystemEventArgs(WatcherChangeTypes.Created, WatchingDirectory, filepath));

			m_Watcher.EnableRaisingEvents = true;
		}

		public override event ChangedEvent Changed;
		public override List<SettingsTriplet> Triplets => m_Triplets;

		public string FilePath;

		protected virtual void Watcher_Changed(object sender, FileSystemEventArgs e)
		{
			var triplets = ParseFile(FilePath);

			if (!triplets.Equals(m_Triplets))
			{
				m_Triplets = triplets;
				Changed?.Invoke();
			}
		}

		// IDisposable implementation
		protected override void DisposeImpl()
		{
			m_Watcher?.Dispose();
		}

		List<SettingsTriplet> ParseFile(string filepath)
		{
			var triplets = new List<SettingsTriplet>();

			var file = new FileInfo(filepath);
			if (!file.Exists || file.Name != Defaults.ConfgFileName)
				return triplets;

			List<string> lines = new List<string>();
			for (int i = 0; i != 3; ++i)
			{
				try
				{
					lines = File.ReadAllLines(filepath).ToList();
					break;
				}
				catch (IOException)
				{
					Thread.Sleep(100);
				}
			}

			// split lines into groups. lines started with a dash are sub-elements
			List<List<string>> groups = lines
				.Where(x => !string.IsNullOrEmpty(x))
				.Aggregate(new List<List<string>>(), (acc, x) =>
				{
					if (Regex.Match(x, "^[a-z]").Success)
						acc.Add(new List<string>());
					acc.Last().Add(x);
					return acc;
				});

			// something something
			foreach (var group in groups)
			{
				try
				{
					ParseGroup(ref triplets, group);
				}
				catch (System.Exception)
				{ }
			}

			triplets.Reverse();

			return triplets;
		}

		void ParseGroup(ref List<SettingsTriplet> triplets, List<string> lines)
		{
			if (lines.Count == 0)
				return;

			if (lines[0].StartsWith("pattern-group"))
			{
				var triplet = new SettingsTriplet();
				ParsePatternGroup(triplet, lines[0]);

				string item = null, solution = null, document = null;
				Color color = Colors.Transparent;
				foreach (var line in lines.Skip(1))
				{
					ParsePattern(ref item, ref solution, ref document, ref color, line);
				}

				// solution/document override item
				solution = solution ?? item ?? "";
				document = document ?? item ?? "";

				triplet.FormatIfNothingOpened = null;
				triplet.FormatIfDocumentOpened = new TitleBarFormat(document, System.Drawing.Color.FromArgb(color.R, color.G, color.B));
				triplet.FormatIfSolutionOpened = new TitleBarFormat(solution, System.Drawing.Color.FromArgb(color.R, color.G, color.B));
				triplets.Add(triplet);
			}
		}

		void ParsePatternGroup(SettingsTriplet triplet, string line)
		{
			var m = Regex.Match(line, "^pattern-group(\\[(.+)\\])?\\s*:");
			if (m.Success)
			{
				if (m.Groups.Count > 1)
				{
					var filter = m.Groups[2].Value;
					ParseFilter(triplet, filter);
				}
			}
		}

		void ParseFilter(SettingsTriplet triplet, string filterString)
		{
			var filters = filterString
				.Split(new char[] { ',' })
				.Select(x => x.Trim());

			foreach (var filter in filters)
			{
				if (filter == "git")
					triplet.Dependency = PatternDependency.Git;
				else if (filter == "vsr")
					triplet.Dependency = PatternDependency.Vsr;
				else if (RegexMatch(out Match match, filter, "solution=~(.+)"))
					triplet.SolutionFilter = match.Groups[1].Value;
			}
		}

		void ParsePattern(ref string item, ref string solution, ref string document, ref Color color, string line)
		{
			if (RegexMatch(out Match matchItem, line, "\\s*-\\s+item-opened: (.+)"))
				item = matchItem.Groups[1].Value;
			else if (RegexMatch(out Match matchSolution, line, "\\s*-\\s+solution-opened: (.+)"))
				solution = matchSolution.Groups[1].Value;
			else if (RegexMatch(out Match matchDocument, line, "\\s*-\\s+document-opened: (.+)"))
				document = matchDocument.Groups[1].Value;
			else if (RegexMatch(out Match matchC, line, "\\s*-\\s+color: (.+)"))
				color = (Color)ColorConverter.ConvertFromString(matchC.Groups[1].Value);
		}

		private static bool RegexMatch(out Match match, string input, string pattern)
		{
			match = Regex.Match(input, pattern);
			return match.Success;
		}

		private string WatchingDirectory;
		private FileSystemWatcher m_Watcher;
		private readonly Regex m_NothingTitleRegex = new Regex("nothing-title: (.+)$");
		private readonly Regex m_DocumentTitleRegex = new Regex("document-title: (.+)$");
		private readonly Regex m_SolutionTitleRegex = new Regex("solution-title: (.+)$");

		private Regex m_NothingColorRegex = new Regex("nothing-color: (.+)$");
		private Regex m_DocumentColorRegex = new Regex("document-color: (.+)$");
		private Regex m_SolutionColorRegex = new Regex("solution-color: (.+)$");

		private List<SettingsTriplet> m_Triplets = new List<SettingsTriplet>();

	}
}
