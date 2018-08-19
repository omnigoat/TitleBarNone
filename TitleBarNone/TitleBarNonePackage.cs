using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Task = System.Threading.Tasks.Task;
using SettingsPageGrid = Atma.TitleBarNone.Settings.SettingsPageGrid;
using System.Collections.Generic;
using Atma.TitleBarNone.Resolvers;

namespace Atma.TitleBarNone
{
	public enum VsEditingMode
	{
		Nothing,
		Document,
		Solution
	}

	[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
	[Guid(PackageGuidString)]
	[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
	[ProvideAutoLoad(UIContextGuids.NoSolution)]
	[ProvideOptionPage(typeof(SettingsPageGrid), "Title Bar None", "Settings", 101, 1000, true)]
	public class TitleBarNonePackage : AsyncPackage
	{
		public const string PackageGuidString = "16599b2d-db6e-49cd-a76e-2b6da7343bcc";

		public const string SolutionSettingsOverrideExtension = ".rn.xml";
		public const string PathTag = "Path";
		public const string SolutionNameTag = "solution-name";
		public const string SolutionPatternTag = "solution-pattern";

		public const string DefaultPatternIfNothingOpen = "hooray:|$ide-name|";
		public const string DefaultPatternIfDocumentOpen = "hooray:|$document-name - $ide-name|";
		public const string DefaultPatternIfSolutionOpen = "hooray:|$solution-name?ide-mode{ $} - $ide-name|";

		public TitleBarNonePackage()
		{
		}

		private void OnSolutionChanged(SolutionResolver.CallbackReason reason)
		{
			UpdateTitle();
		}

		private void OnIDEChanged(IDEResolver.CallbackReason reason, IDEResolver.IDEState state)
		{
			if (reason == IDEResolver.CallbackReason.ModeChanged)
				m_Mode = state.Mode;

			UpdateTitle();
		}

		public DTE2 DTE { get; private set; }
		public string Pattern
		{
			get
			{
				if (DTE.Solution.IsOpen)
					return PatternIfSolutionOpened;
				else if (DTE.Documents.Count > 0)
					return PatternIfDocumentOpened;
				else
					return PatternIfNothingOpened;
			}
		}

		private IEnumerable<SettingsFrame> SettingsFrames
		{
			get
			{
				var list = new List<SettingsFrame> { m_SettingsFromSolutionDir, m_SettingsFromUserDir };
				list.AddRange(m_SettingsFrameVsPreds);
				list.Add(m_SettingsFromVs);
				return list;
			}
		}

		private string PatternIfNothingOpened => SettingsFrames.Where(x => x.FormatIfNothingOpened != null).FirstOrDefault()?.FormatIfNothingOpened?.Pattern ?? "";
		private string PatternIfDocumentOpened => SettingsFrames.Where(x => x.FormatIfDocumentOpened != null).FirstOrDefault()?.FormatIfDocumentOpened?.Pattern ?? "";
		private string PatternIfSolutionOpened => SettingsFrames.Where(x => x.FormatIfSolutionOpened != null).FirstOrDefault()?.FormatIfSolutionOpened?.Pattern ?? "";

		internal SettingsPageGrid UISettings { get; private set; }


		protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
		{
			// initialize the DTE and bind events
			DTE = await GetServiceAsync(typeof(DTE)) as DTE2;

			var sr = SolutionResolver.Create(DTE, OnSolutionChanged);

			m_Resolvers = new List<Resolver>
			{
				IDEResolver.Create(DTE, OnIDEChanged),
				sr
			};


			//DTE.Events.DTEEvents.OnStartupComplete += DTEEvents_OnStartupComplete;
			//DTE.Events.DTEEvents.ModeChanged += DTEEvents_ModeChanged;

			m_WindowEvents = DTE.Events.WindowEvents;
			//m_WindowEvents.WindowCreated += WindowEvents_WindowCreated;

			m_SolutionEvents = DTE.Events.SolutionEvents;
			m_SolutionEvents.Opened += OnSolutionOpened;
			m_SolutionEvents.AfterClosing += SolutionUpdated;

			// switch to UI thread
			await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			// get UI settings hooks
			UISettings = GetDialogPage(typeof(SettingsPageGrid)) as SettingsPageGrid;
			UISettings.SettingsChanged += VsSettingsChanged;
			VsSettingsChanged(UISettings, EventArgs.Empty);
		}

		private void VsSettingsChanged(object sender, EventArgs e)
		{
			var settings = sender as SettingsPageGrid;

			bool requiresUpdate =
				(m_SettingsFromVs.FormatIfNothingOpened != settings.PatternIfNothingOpen) ||
				(m_SettingsFromVs.FormatIfDocumentOpened != settings.PatternIfDocumentOpen) ||
				(m_SettingsFromVs.FormatIfSolutionOpened != settings.PatternIfSolutionOpen);

			m_SettingsFromVs.FormatIfNothingOpened = settings.PatternIfNothingOpen;
			m_SettingsFromVs.FormatIfDocumentOpened = settings.PatternIfDocumentOpen;
			m_SettingsFromVs.FormatIfSolutionOpened = settings.PatternIfSolutionOpen;

			if (requiresUpdate)
				UpdateTitle();
		}

		private void DTEEvents_OnStartupComplete()
		{
			if (DTE.Solution.IsOpen)
				m_EditingMode = VsEditingMode.Solution;
			else if (DTE.Documents.Count > 0)
				m_EditingMode = VsEditingMode.Document;
			else
				m_EditingMode = VsEditingMode.Nothing;

			ChangeWindowTitle("lulz");
		}

		private void DTEEvents_ModeChanged(vsIDEMode LastMode)
		{
			UpdateTitle();
		}

		private void OnSolutionOpened()
		{
			// create a filesystem informant
			var directory = System.IO.Path.GetDirectoryName(DTE.Solution.FileName);
			var filename = ".titlebar";
			var config_file = System.IO.Path.Combine(directory, filename);

			if (System.IO.File.Exists(config_file))
			{
				m_SettingsFromSolutionDir.Reset(new Settings.FileSystemChangeProvider(config_file));
			}

			// loop through custom settings
		}

		private void SolutionUpdated()
		{
			UpdateTitle();
		}

		private void UpdateTitle()
		{
			string pattern = Pattern;

			var state = new VsState()
			{
				Mode = m_Mode,
				Solution = DTE.Solution
			};

			int i = 0;
			if (ParseImpl(out string transformed, state, pattern, ref i, null))
			{
				ChangeWindowTitle(transformed);
			}
		}

		private bool ParseImpl(out string transformed, VsState state, string pattern, ref int i, string singleDollar)
		{
			transformed = "";

			// begin pattern parsing
			while (i != pattern.Length)
			{
				// escape sequences
				if (pattern[i] == '\\')
				{
					++i;
					if (i == pattern.Length)
						break;
					transformed += pattern[i];
					++i;
				}
				// predicates
				else if (pattern[i] == '?' && ParseQuestion(out string r, state, pattern, ref i, singleDollar))
				{
					transformed += r;
				}
				// dollars
				else if (pattern[i] == '$' && ParseDollar(out string r2, state, pattern, ref i, singleDollar))
				{
					transformed += r2;
				}
				else
				{
					transformed += pattern[i];
					++i;
				}
			}

			return true;
		}


		private bool ParseQuestion(out string result, VsState state, string pattern, ref int i, string singleDollar)
		{
			var tag = new string(pattern
				.Substring(i + 1)
				.TakeWhile(x => x >= 'a' && x <= 'z' || x == '-')
				.ToArray());

			i += 1 + tag.Length;

			bool valid = m_Resolvers
				.FirstOrDefault(x => x.Applicable(tag))
				?.ResolveBoolean(state, tag) ?? false;

			if (i == pattern.Length)
			{
				result = null;
				return valid;
			}

			// look for braced group {....}, and skip if question was bad
			if (pattern[i] == '{')
			{
				if (!valid)
				{
					while (i != pattern.Length)
					{
						++i;
						if (pattern[i] == '}')
						{
							++i;
							break;
						}
					}

					result = null;
					return false;
				}
				else
				{
					var transformed_tag = m_Resolvers
						.FirstOrDefault(x => x.Applicable(tag))
						?.Resolve(state, tag);

					var inner = new string(pattern
						.Substring(i + 1)
						.TakeWhile(x => x != '}')
						.ToArray());

					i += 1 + inner.Length + 1;

					int j = 0;
					ParseImpl(out result, state, inner, ref j, transformed_tag);
				}
			}
			else
			{
				result = null;
				return false;
			}

			return true;
		}

		private bool ParseDollar(out string result, VsState state, string pattern, ref int i, string singleDollar)
		{
			var tag = new string(pattern
				.Substring(i + 1)
				.TakeWhile(x => x >= 'a' && x <= 'z' || x == '-')
				.ToArray());

			i += 1 + tag.Length;

			if (tag.Length == 0 && singleDollar != null)
			{
				result = singleDollar;
				return true;
			}

			result = m_Resolvers
				.FirstOrDefault(x => x.Applicable(tag))
				?.Resolve(state, tag);

			return result != null;
		}

		private void ChangeWindowTitle(string title)
		{
			if (DTE.MainWindow == null)
				return;

			try
			{
				ThreadHelper.Generic.Invoke(() =>
				{
					System.Windows.Application.Current.MainWindow.Title = DTE.MainWindow.Caption;
					if (System.Windows.Application.Current.MainWindow.Title != title)
						System.Windows.Application.Current.MainWindow.Title = title;
				});
			}
			catch (Exception e)
			{
				Debug.Print(e.Message);
			}
		}

		// apparently these could get garbage collected otherwise
		private SolutionEvents m_SolutionEvents;
		private WindowEvents m_WindowEvents;
		private VsEditingMode m_EditingMode = VsEditingMode.Nothing;
		private dbgDebugMode m_Mode = dbgDebugMode.dbgDesignMode;

		class SettingsFrame : IDisposable
		{
			public Settings.TitleBarFormat FormatIfNothingOpened = null;
			public Settings.TitleBarFormat FormatIfDocumentOpened = null;
			public Settings.TitleBarFormat FormatIfSolutionOpened = null;

			public void Reset(Settings.ChangeProvider changeProvider)
			{
				if (m_ChangeProvider != null)
					m_ChangeProvider.Changed -= OnSettingsChanged;

				m_ChangeProvider = changeProvider;

				if (m_ChangeProvider != null)
					m_ChangeProvider.Changed += OnSettingsChanged;
			}

			private void OnSettingsChanged(Settings.TitleBarFormatTriplet triplet)
			{
				FormatIfNothingOpened = triplet.FormatIfNothingOpened;
				FormatIfDocumentOpened = triplet.FormatIfDocumentOpened;
				FormatIfSolutionOpened = triplet.FormatIfSolutionOpened;
			}

			public void Dispose()
			{
				m_ChangeProvider.Dispose();
			}

			private Settings.ChangeProvider m_ChangeProvider;
		}

		private SettingsFrame m_SettingsFromVs = new SettingsFrame();
		private List<SettingsFrame> m_SettingsFrameVsPreds = new List<SettingsFrame>();
		private SettingsFrame m_SettingsFromUserDir = new SettingsFrame();
		private SettingsFrame m_SettingsFromSolutionDir = new SettingsFrame();
		private List<SettingsFrame> m_SettingsStack = new List<SettingsFrame>();
		private List<Resolvers.Resolver> m_Resolvers;
	}
}
