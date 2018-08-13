using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using TitleBarNone;

//http://stackoverflow.com/questions/24291249/dialogpage-string-array-not-persisted
//http://www.codeproject.com/Articles/351172/CodeStash-a-journey-into-the-dark-side-of-Visual-S

namespace Atma.TitleBarNone.Settings
{
	[ClassInterface(ClassInterfaceType.AutoDual)]
	public class SettingsPageGrid : DialogPage
	{
		[Category("Patterns")]
		[DisplayName("No document or solution open")]
		[Description("Default: [ideName]. See 'Supported Tags' section on the left for more guidance.")]
		[DefaultValue(TitleBarNonePackage.DefaultPatternIfNothingOpen)]
		[Editor(typeof(Editors.PatternEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string PatternIfNothingOpen { get; set; } = TitleBarNonePackage.DefaultPatternIfNothingOpen;

		[Category("Patterns")]
		[DisplayName("Document (no solution) open")]
		[Description("Default: TODO")]
		[DefaultValue(TitleBarNonePackage.DefaultPatternIfDocumentOpen)]
		[Editor(typeof(Editors.PatternEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[Editors.PreviewRequires(Editors.PreviewRequiresAttribute.Requirement.Document)]
		public string PatternIfDocumentOpen { get; set; } = TitleBarNonePackage.DefaultPatternIfDocumentOpen;

		[Category("Patterns")]
		[DisplayName("Solution in design mode")]
		[Description("Default: [parentPath]\\[solutionName] - [ideName]. See 'Supported tags' section on the left for more guidance.")]
		[DefaultValue(TitleBarNonePackage.DefaultPatternIfSolutionOpen)]
		[Editor(typeof(Editors.PatternEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[Editors.PreviewRequires(Editors.PreviewRequiresAttribute.Requirement.Solution)]
		public string PatternIfSolutionOpen { get; set; } = TitleBarNonePackage.DefaultPatternIfSolutionOpen;

#if false
		[Category("Source Control")]
		[DisplayName("Git binaries directory")]
		[Description("Default: Empty. Search windows PATH for git if empty.")]
		[Editor(typeof(Editors.FilePickerEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[Editors.FilePicker(true, Informers.GitInformer.ExecutableFilename, "Git executable (git.exe)|git.exe|All files (*.*)|*.*", 1)]
		[DefaultValue("")]
		public string GitDirectory { get; set; } = "";
#endif
#if false
		[Category("Source Control")]
		[DisplayName("Hg binaries directory")]
		[Description("Default: Empty. Search windows PATH for hg if empty.")]
		[Editor(typeof(FilePickerEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[FilePicker(true, HgBranchNameResolver.HgExecFn, "Hg executable(hg.exe)|hg.exe|All files(*.*)|*.*", 1)]
		[DefaultValue("")]
		public string HgDirectory { get; set; } = "";

		[Category("Source Control")]
		[DisplayName("SVN binaries directory")]
		[Description("Default: Empty. Search windows PATH for svn if empty.")]
		[Editor(typeof(FilePickerEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[FilePicker(true, SvnResolver.SvnExecFn, "SVN executable(svn.exe)|svn.exe|All files(*.*)|*.*", 1)]
		[DefaultValue("")]
		public string SvnDirectory { get; set; } = "";

		[Category("Source Control")]
		[DisplayName("SVN directory separator")]
		[Description("Default: '/'. Specify the character used to separate the SVN directories.")]
		[DefaultValue("/")]
		public string SvnDirectorySeparator { get; set; } = "/";

		[Category("Source Control")]
		[DisplayName("Versionr binaries directory")]
		[Description("Default: Empty. Search windows PATH for svn if empty.")]
		[Editor(typeof(FilePickerEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[FilePicker(true, VsrResolver.VsrExecFn, "Versionr executable(vsr.exe)|vsr.exe|All files(*.*)|*.*", 1)]
		[DefaultValue("")]
		public string VsrDirectory { get; set; } = "";

		[Category("Source Control")]
		[DisplayName("Versionr directory separator")]
		[Description("Default: '/'. Specify the character used to separate the Versionr directories.")]
		[DefaultValue("/")]
		public string VsrDirectorySeparator { get; set; } = "/";
#endif

		[Category("Debug")]
		[DisplayName("Enable debug mode")]
		[Description("Default: false. Set to true to activate debug output to Output window.")]
		[DefaultValue(false)]
		public bool EnableDebugMode { get; set; } = false;

		public event EventHandler SettingsChanged;

		protected override void OnApply(PageApplyEventArgs e)
		{
			base.OnApply(e);
			if (e.ApplyBehavior != ApplyKind.Apply)
				return;
			this.SettingsChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}