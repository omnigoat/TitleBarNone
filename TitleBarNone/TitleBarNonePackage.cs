using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;
using SettingsPageGrid = Atma.TitleBarNone.Settings.SettingsPageGrid;
using System.Collections.Generic;
using Atma.TitleBarNone.Resolvers;
using System.Windows;
using Atma.TitleBarNone.Utilities;
using Microsoft.VisualStudio;

namespace Atma.TitleBarNone
{
	public enum VsEditingMode
	{
		Nothing,
		Document,
		Solution
	}

	[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	//[ProvideService(typeof(]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
	[Guid(PackageGuidString)]
	[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
	//[ProvideAutoLoad(UIContextGuids.NoSolution)]
	[ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
	[ProvideOptionPage(typeof(SettingsPageGrid), "Title Bar None", "Settings", 101, 1000, true)]
	public class TitleBarNonePackage : AsyncPackage
	{
		public const string PackageGuidString = "16599b2d-db6e-49cd-a76e-2b6da7343bcc";

		public const string SolutionSettingsOverrideExtension = ".rn.xml";
		public const string PathTag = "Path";
		public const string SolutionNameTag = "solution-name";
		public const string SolutionPatternTag = "solution-pattern";

		public TitleBarNonePackage()
		{
		}

		public DTE DTE
		{
			get;
			private set;
		}

		public string Pattern
		{
			get
			{
				if (DTE.Solution.IsOpen)
					return PatternIfSolutionOpened?.Pattern ?? "";
				else if (DTE.Documents.Count > 0)
					return PatternIfDocumentOpened?.Pattern ?? "";
				else
					return PatternIfNothingOpened?.Pattern ?? "";
			}
		}

		public string TitleBarText
		{
			get
			{
				string pattern = Pattern;
				if (string.IsNullOrEmpty(pattern))
					return null;

				var state = new VsState()
				{
					Resolvers = Resolvers,
					Mode = m_Mode,
					Solution = DTE.Solution
				};

				if (Parsing.ParseFormatString(out string transformed, state, pattern))
				{
					return transformed;
				}

				return null;
			}
		}

		public System.Drawing.Color? TitleBarColor
		{
			get
			{
				if (DTE.Solution.IsOpen)
					return ColorIfSolutionOpened;
				else if (DTE.Documents.Count > 0)
					return ColorIfDocumentOpened;
				else
					return ColorIfNothingOpened;
			}
		}

		private IEnumerable<Resolver> Resolvers => m_Resolvers.AsEnumerable().Reverse();

		private IEnumerable<Settings.SettingsTriplet> SettingsTriplets
		{
			get
			{
				return m_UserDirFileChangeProvider.Triplets
					.Concat(m_SolutionsFileChangeProvider?.Triplets)
					.Concat(m_VsOptionsChangeProvider.Triplets)
					.Concat(m_DefaultsChangeProvider.Triplets)
					;
			}
		}

		private System.Drawing.Color? ColorIfNothingOpened => SettingsTriplets
			.Where(TripletDependenciesAreSatisfied)
			.Where(x => x.FormatIfNothingOpened?.Color != null)
			.FirstOrDefault()?.FormatIfNothingOpened?.Color;

		private System.Drawing.Color? ColorIfDocumentOpened => SettingsTriplets
			.Where(TripletDependenciesAreSatisfied)
			.Where(x => x.FormatIfDocumentOpened?.Color != null)
			.FirstOrDefault()?.FormatIfDocumentOpened?.Color;

		private System.Drawing.Color? ColorIfSolutionOpened => SettingsTriplets
			.Where(TripletDependenciesAreSatisfied)
			.Where(x => x.FormatIfSolutionOpened?.Color != null)
			.FirstOrDefault()?.FormatIfSolutionOpened?.Color;

		private Settings.TitleBarFormat PatternIfNothingOpened => SettingsTriplets
			.Where(TripletDependenciesAreSatisfied)
			.Where(x => !string.IsNullOrEmpty(x.FormatIfNothingOpened?.Pattern))
			.FirstOrDefault()?.FormatIfNothingOpened;

		private Settings.TitleBarFormat PatternIfDocumentOpened => SettingsTriplets
			.Where(TripletDependenciesAreSatisfied)
			.Where(x => !string.IsNullOrEmpty(x.FormatIfDocumentOpened?.Pattern))
			.FirstOrDefault()?.FormatIfDocumentOpened;

		private Settings.TitleBarFormat PatternIfSolutionOpened => SettingsTriplets
			.Where(TripletDependenciesAreSatisfied)
			.Where(x => !string.IsNullOrEmpty(x.FormatIfSolutionOpened?.Pattern))
			.FirstOrDefault()?.FormatIfSolutionOpened;

		private bool TripletDependenciesAreSatisfied(Settings.SettingsTriplet triplet)
		{
			return triplet.PatternDependencies.All(d => Resolvers.Any(r => r.SatisfiesDependency(d)));
		}

		protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
		{
			// initialize the DTE and bind events
			DTE = await GetServiceAsync(typeof(DTE)) as DTE;

			// create models of IDE/Solution
			ideModel = new Models.IDEModel(DTE);
			ideModel.WindowShown += OnWindowShowing;
			ideModel.IdeModeChanged += (dbgDebugMode mode) => m_Mode = mode;
			ideModel.StartupComplete += UpdateTitleAsync;

			solutionModel = new Models.SolutionModel(DTE);
			solutionModel.SolutionOpened += OnSolutionOpened;
			solutionModel.SolutionClosed += OnSolutionClosed;

			// create resolvers
			m_Resolvers = new List<Resolver>
			{
				IDEResolver.Create(ideModel),
				SolutionResolver.Create(solutionModel),
				GitResolver.Create(solutionModel),
				VsrResolver.Create(solutionModel),
				SvnResolver.Create(solutionModel)
			};

			foreach (var resolver in m_Resolvers)
			{
				resolver.Changed += (Resolver r) => UpdateTitleAsync();
			}

			// create settings readers for user-dir
			m_UserDirFileChangeProvider = new Settings.UserDirFileChangeProvider();
			m_UserDirFileChangeProvider.Changed += UpdateTitleAsync;

			// switch to UI thread
			await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			// get UI settings hooks
			UISettings = GetDialogPage(typeof(SettingsPageGrid)) as SettingsPageGrid;
			m_VsOptionsChangeProvider = new Settings.VsOptionsChangeProvider(UISettings);
			m_VsOptionsChangeProvider.Changed += UpdateTitleAsync;
		}

		private void OnSolutionOpened(Solution solution)
		{
			WriteOutput("OnSolutionOpened");

			// reset the solution-file settings file
			m_SolutionsFileChangeProvider = new Settings.SolutionFileChangeProvider(solution.FileName);

			UpdateTitleAsync();
		}

		private void OnSolutionClosed()
		{
			if (m_SolutionsFileChangeProvider != null)
				m_SolutionsFileChangeProvider.Dispose();

			UpdateTitleAsync();
		}

		public static void WriteOutput(string str, params object[] args)
		{
			try
			{
				Application.Current.Dispatcher?.Invoke(() =>
				{
					var outWindow = GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
					var generalPaneGuid = VSConstants.OutputWindowPaneGuid.DebugPane_guid;
					if (outWindow != null)
					{
						outWindow.GetPane(ref generalPaneGuid, out IVsOutputWindowPane generalPane);
						generalPane.OutputString("TitleBarNone: " + string.Format(str, args) + "\r\n");
						generalPane.Activate();
					}
				});
			}
			catch
			{
			}
		}

		private void OnWindowShowing(EnvDTE.Window Window)
		{
			ChangeWindowTitleColorAsync(TitleBarColor);
		}

		private void UpdateTitleAsync()
		{
			Application.Current?.Dispatcher?.InvokeAsync(() =>
			{
				ChangeWindowTitle(TitleBarText);
				ChangeWindowTitleColor(TitleBarColor);
			});
		}

		private Models.TitleBarModel MakeTitleBarModel(System.Windows.Window x)
		{
			if (IsMsvc2017)
				return new Models.TitleBarModel2017(x);
			else if (IsMsvc2019)
				return new Models.TitleBarModel2019(x);
			else
				return null;
		}

		private Tuple<List<System.Windows.Window>, List<System.Windows.Window>> WindowsLostAndDiscovered
		{
			get
			{
				var seenWindows = Application.Current.Windows.Cast<System.Windows.Window>();

				var lost = knownWindowModels
					.Select(x => x.Window)
					.Except(seenWindows)
					.ToList();

				var discovered = seenWindows
					.Except(windowsAndModels.Keys.ToList())
					.Select(x => MakeTitleBarModel(x))
					.ToDictionary(s => s.Window, s => s as Models.TitleBarModel);

				windowsAndModels = windowsAndModels.Concat(discovered).ToDictionary(s => s.Key, s => s.Value);

				return Tuple.Create(forgotten, discovered.Keys.ToList());
			}
		}

		private IEnumerable<System.Windows.Window> WindowsDiscovered =>
			




		private void ChangeWindowTitleColor(System.Drawing.Color? color)
		{
			var seenWindows = Application.Current.Windows.Cast<System.Windows.Window>();

			// remove old models, set their colour back to original
			foreach (var w in windowsAndModels.Keys.Except(seenWindows).ToList())
			{
				windowsAndModels[w]?.SetTitleBarColor(null);
				windowsAndModels.Remove(w);
			}

			// add new models
			var newWindows = seenWindows
				.Except(windowsAndModels.Keys.ToList())
				.Select(x => MakeTitleBarModel(x))
				.ToDictionary(s => s.Window, s => s as Models.TitleBarModel);

			windowsAndModels = windowsAndModels
				.Concat(newWindows)
				.ToDictionary(s => s.Key, s => s.Value);

			// colour all models
			foreach (var model in windowsAndModels.Values)
			{
				model.SetTitleBarColor(color);
			}
		}

		private void ChangeWindowTitleColorAsync(System.Drawing.Color? color)
		{
			Application.Current?.Dispatcher?.InvokeAsync(() =>
			{
				ChangeWindowTitleColor(color);
			});
		}

		private void ChangeWindowTitle(string title)
		{
			if (title == null)
			{
				Debug.Print("ChangeWindowTitle - exiting early because title == null");
				return;
			}

			if (Application.Current.MainWindow == null)
			{
				Debug.Print("ChangeWindowTitle - exiting early because Application.Current.MainWindow == null");
				return;
			}

			try
			{
				Application.Current.MainWindow.Title = DTE.MainWindow.Caption;
				if (Application.Current.MainWindow.Title != title)
					Application.Current.MainWindow.Title = title;
			}
			catch (Exception e)
			{
				Debug.Print(e.Message);
			}
		}

		private bool IsMsvc2017 => DTE.Version.StartsWith("15");
		private bool IsMsvc2019 => DTE.Version.StartsWith("16");

		// UI
		internal SettingsPageGrid UISettings { get; private set; }

		// models
		private Models.SolutionModel solutionModel;
		private Models.IDEModel ideModel;

		// apparently these could get garbage collected otherwise
		private dbgDebugMode m_Mode = dbgDebugMode.dbgDesignMode;

		private List<Resolver> m_Resolvers;

		private Settings.SolutionFileChangeProvider m_SolutionsFileChangeProvider;
		private Settings.UserDirFileChangeProvider m_UserDirFileChangeProvider;
		private Settings.VsOptionsChangeProvider m_VsOptionsChangeProvider;
		private Settings.DefaultsChangeProvider m_DefaultsChangeProvider = new Settings.DefaultsChangeProvider();

		private readonly int currentProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;

		//private Dictionary<System.Windows.Window, Models.TitleBarModel> windowsAndModels = new Dictionary<System.Windows.Window, Models.TitleBarModel>();
		private List<Models.TitleBarModel> knownWindowModels = new List<Models.TitleBarModel>();
	}
}
