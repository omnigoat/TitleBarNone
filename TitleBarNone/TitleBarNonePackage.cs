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

		internal Models.TitleBarData TitleBarData =>
			SettingsTriplets
				.Where(TripletDependenciesAreSatisfied)
				.Select(TitleBarFormatRightNow)
				.Aggregate(new Models.TitleBarData(), (acc, x) =>
				{
					acc.TitleBarBackgroundColor = acc.TitleBarBackgroundColor ?? x.BackgroundBrush;
					acc.TitleBarForegroundColor = acc.TitleBarForegroundColor ?? new System.Windows.Media.SolidColorBrush(x.Color ?? System.Drawing.SystemColors.ActiveCaption);
				});

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

		private Settings.TitleBarFormat TitleBarFormatRightNow(Settings.SettingsTriplet st)
		{
			if (DTE.Solution.IsOpen)
				return st.FormatIfSolutionOpened;
			else if (DTE.Documents.Count > 0)
				return st.FormatIfDocumentOpened;
			else
				return st.FormatIfNothingOpened;
		}

		private IEnumerable<Resolver> Resolvers => m_Resolvers.AsEnumerable().Reverse();

		private IEnumerable<Settings.SettingsTriplet> SettingsTriplets =>
			m_UserDirFileChangeProvider.Triplets
				.Concat(m_SolutionsFileChangeProvider?.Triplets ?? new List<Settings.SettingsTriplet>())
				.Concat(m_VsOptionsChangeProvider?.Triplets ?? new List<Settings.SettingsTriplet>())
				.Concat(m_DefaultsChangeProvider.Triplets);

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
			// switch to UI thread
			await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

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


			// get UI settings hooks
			UISettings = GetDialogPage(typeof(SettingsPageGrid)) as SettingsPageGrid;
			m_VsOptionsChangeProvider = new Settings.VsOptionsChangeProvider(UISettings);
			m_VsOptionsChangeProvider.Changed += UpdateTitleAsync;


			// async initialize window state in case this plugin loaded after the
			// IDE was brought up, because this plugin loads async to the UI
			Application.Current.Dispatcher.InvokeAsync(() =>
			{
				var (_, discovered) = WindowsLostAndDiscovered;
				foreach (var w in discovered)
					w.SetTitleBarColor(TitleBarColor);
			});
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

		private List<Models.TitleBarModel> knownWindowModels = new List<Models.TitleBarModel>();

		private Tuple<List<Models.TitleBarModel>, List<Models.TitleBarModel>> WindowsLostAndDiscovered
		{
			get
			{
				var seenWindows = Application.Current.Windows.Cast<System.Windows.Window>();

				var lost = knownWindowModels
					.Where(x => !seenWindows.Contains(x.Window))
					.ToList();

				var discovered = seenWindows
					.Except(knownWindowModels.Select(x => x.Window))
					.Select(x => Models.TitleBarModel.Make(DTE.Version, x))
					.Where(x => x != null)
					.ToList();

				var lostWindows = lost.Select(x => x.Window);

				knownWindowModels = knownWindowModels
					.Where(x => !lostWindows.Contains(x.Window))
					.Concat(discovered)
					.ToList();

				return Tuple.Create(lost, discovered);
			}
		}

		private void ChangeWindowTitleColor(System.Drawing.Color? color)
		{
			var (lost, _) = WindowsLostAndDiscovered;

			// set old models' colour back to original
			foreach (var x in lost)
			{
				x.SetTitleBarColor(null);
			}

			// colour all models
			foreach (var x in knownWindowModels)
			{
				x.SetTitleBarColor(color);
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
	}
}
