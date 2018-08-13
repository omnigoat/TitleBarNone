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

namespace TitleBarNone
{
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

		public const string DefaultPatternIfNothingOpen = "$ide-name";
		public const string DefaultPatternIfDocumentOpen = "$document-name - $ide-name";
		public const string DefaultPatternIfSolutionOpen = "$solution-name ${$ide-mode }- $ide-name";

		protected enum VsEditingMode
		{
			Nothing,
			Document,
			Solution
		}

		public TitleBarNonePackage()
		{
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

		private string PatternIfNothingOpened => SettingsFrames.Where(x => x.PatternIfNothingOpened != null).FirstOrDefault()?.PatternIfNothingOpened ?? "";
		private string PatternIfDocumentOpened => SettingsFrames.Where(x => x.PatternIfDocumentOpened != null).FirstOrDefault()?.PatternIfDocumentOpened ?? "";
		private string PatternIfSolutionOpened => SettingsFrames.Where(x => x.PatternIfSolutionOpened != null).FirstOrDefault()?.PatternIfSolutionOpened ?? "";

		internal SettingsPageGrid UISettings { get; private set; }


		protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
		{
			// initialize the DTE and bind events
			DTE = await GetServiceAsync(typeof(DTE)) as DTE2;

			DTE.Events.DTEEvents.OnStartupComplete += DTEEvents_OnStartupComplete;
			DTE.Events.DTEEvents.ModeChanged += DTEEvents_ModeChanged;

			m_WindowEvents = DTE.Events.WindowEvents;
			//m_WindowEvents.WindowCreated += WindowEvents_WindowCreated;

			m_SolutionEvents = DTE.Events.SolutionEvents;
			m_SolutionEvents.Opened += SolutionUpdated;
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
				(m_SettingsFromVs.PatternIfNothingOpened != settings.PatternIfNothingOpen) ||
				(m_SettingsFromVs.PatternIfDocumentOpened != settings.PatternIfDocumentOpen) ||
				(m_SettingsFromVs.PatternIfSolutionOpened != settings.PatternIfSolutionOpen);

			m_SettingsFromVs.PatternIfNothingOpened = settings.PatternIfNothingOpen;
			m_SettingsFromVs.PatternIfDocumentOpened = settings.PatternIfDocumentOpen;
			m_SettingsFromVs.PatternIfSolutionOpened = settings.PatternIfSolutionOpen;

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

		private void SolutionUpdated()
		{
			UpdateTitle();
		}

		private void UpdateTitle()
		{
			string pattern = Pattern;
			ChangeWindowTitle(pattern);
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

		class SettingsFrame
		{
			public string PatternIfNothingOpened = null;
			public string PatternIfDocumentOpened = null;
			public string PatternIfSolutionOpened = null;
		}

		private SettingsFrame m_SettingsFromVs = new SettingsFrame();
		private List<SettingsFrame> m_SettingsFrameVsPreds = new List<SettingsFrame>();
		private SettingsFrame m_SettingsFromUserDir = new SettingsFrame();
		private SettingsFrame m_SettingsFromSolutionDir = new SettingsFrame();
		private List<SettingsFrame> m_SettingsStack = new List<SettingsFrame>();
	}
}
