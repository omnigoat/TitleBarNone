using EnvDTE;
using EnvDTE80;
using System;

namespace Atma.TitleBarNone.Resolvers
{
	public class IDEResolver : Resolver
	{
		public static IDEResolver Create(Models.IDEModel ideModel)
		{
			return new IDEResolver(ideModel);
		}

		public IDEResolver(Models.IDEModel ideModel)
			: base(new[] { "ide-name", "ide-mode" })
		{
			ideModel.IdeModeChanged += OnModeChanged;
			vsMode = ideModel.VsMode;
		}

		public override bool Available => true;

		public override ChangedDelegate Changed { get; set; }

		// IDEResolver is the final backup for the defaults
		public override int SatisfiesDependency(Settings.SettingsTriplet triplet)
		{
			int result = 0;
			if (triplet.SolutionFilter == "")
				result |= 0x1;
			if (triplet.Dependency == Settings.PatternDependency.None)
				result |= 0x2;

			return result;
		}

		public override bool ResolveBoolean(VsState state, string tag)
		{
			if (tag == "ide-mode")
				return (state.Mode != dbgDebugMode.dbgDesignMode);
			else
				return true;
		}

		public override string Resolve(VsState state, string tag)
		{
			if (tag == "ide-name")
				return "Microsoft Visual Studio";
			else if (tag == "ide-mode")
				return GetModeTitle(state);
			else
				return null;
		}

		private void OnModeChanged(dbgDebugMode mode)
		{
			if (mode != vsMode)
			{
				vsMode = mode;
				Changed?.Invoke(this);
			}
		}

		private string GetModeTitle(VsState state)
		{
			if (state.Mode == dbgDebugMode.dbgDesignMode)
				return string.Empty;
			else if (state.Mode == dbgDebugMode.dbgRunMode)
				return "(Running)";
			else
				return "(Debugging)";
		}

		private dbgDebugMode vsMode;
	}
}
