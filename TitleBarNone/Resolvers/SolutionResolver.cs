using Atma.TitleBarNone.Settings;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Atma.TitleBarNone.Resolvers
{
	public class SolutionResolver : Resolver
	{
		public enum CallbackReason
		{
			SolutionOpened,
			SolutionClosed
		}

		internal static SolutionResolver Create(Models.SolutionModel solutionModel)
		{
			return new SolutionResolver(solutionModel);
		}

		public SolutionResolver(Models.SolutionModel solutionModel)
			: base(new [] { "solution", "solution-name", "solution-dir", "item-name" })
		{
			solutionModel.SolutionOpened += OnSolutionOpened;
		}

		public override ChangedDelegate Changed { get; set; }

		public override bool ResolveBoolean(VsState state, string tag)
		{
			return state.Solution != null;
		}

		public override string Resolve(VsState state, string tag)
		{
			if ((tag == "solution-name" || tag == "item-name") && state.Solution?.FullName != null)
				return Path.GetFileNameWithoutExtension(state.Solution.FullName);
			else if (tag == "solution-dir")
				return Path.GetFileName(Path.GetDirectoryName(state.Solution.FileName)) + "\\";
			else
				throw new InvalidOperationException();
		}

		public override int SatisfiesDependency(SettingsTriplet triplet)
		{
			return Regex.Match(solution.FullName, triplet.SolutionFilter).Success ? 1 : 0;
		}

		private void OnSolutionOpened(Solution solution)
		{
			this.solution = solution;
		}

		private void OnSolutionClosed()
		{
			this.solution = null;
		}

		private Solution solution;
	}
}
