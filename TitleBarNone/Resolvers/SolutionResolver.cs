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

		internal static Resolver Create(DTE2 dte, Action<CallbackReason> callback = null)
		{
			return new SolutionResolver(dte, callback);
		}

		public SolutionResolver(DTE2 dte, Action<CallbackReason> callback)
			: base(new [] { "solution", "solution-name", "solution-dir" })
		{
			m_DTE = dte;
			m_Callback = callback;

			m_DTE.Events.SolutionEvents.Opened += OnSolutionOpened;
			m_DTE.Events.SolutionEvents.AfterClosing += OnSolutionClosed;
		}

		public delegate void SolutionOpenedDelegate(string filepath);
		public delegate void SolutionClosedDelegate();

		public event SolutionOpenedDelegate SolutionOpened;
		public event SolutionClosedDelegate SolutionClosed;

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

		private void OnSolutionOpened()
		{
			SolutionOpened.Invoke(m_DTE.Solution.FileName);
			m_Callback?.Invoke(CallbackReason.SolutionOpened);
		}

		private void OnSolutionClosed()
		{
			SolutionClosed.Invoke();
			m_Callback?.Invoke(CallbackReason.SolutionClosed);
		}

		private DTE2 m_DTE;
		private Action<CallbackReason> m_Callback;
	}
}
