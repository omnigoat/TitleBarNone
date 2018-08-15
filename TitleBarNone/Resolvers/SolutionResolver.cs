using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;

namespace Atma.TitleBarNone.Resolvers
{
	public class SolutionResolver : Resolver
	{
		public enum CallbackReason
		{
			SolutionOpened,
			SolutionClosed
		}

		public SolutionResolver(DTE2 dte, Action<CallbackReason> callback)
			: base(new [] { "solution", "solution-name", "solution-dir" })
		{
			m_DTE = dte;
			m_Callback = callback;

			m_DTE.Events.SolutionEvents.Opened += () => m_Callback(CallbackReason.SolutionOpened);
			m_DTE.Events.SolutionEvents.AfterClosing += () => m_Callback(CallbackReason.SolutionClosed);
		}

		public static Resolver Create(DTE2 dte, Action<CallbackReason> callback)
		{
			return new SolutionResolver(dte, callback);
		}

		public override bool ResolveBoolean(VsState state, string tag)
		{
			return state.Solution != null;
		}

		public override string Resolve(VsState state, string tag)
		{
			if (tag == "solution-name" && state.Solution?.FullName != null)
				return state.Solution.FullName;
			else if (tag == "solution-dir")
				return Path.GetDirectoryName(state.Solution.FileName);
			else
				return "lmao";
		}

		private DTE2 m_DTE;
		private Action<CallbackReason> m_Callback;

		internal static Resolver Create(DTE2 dTE, object onSolutionChanged)
		{
			throw new NotImplementedException();
		}
	}
}
