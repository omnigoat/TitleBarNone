using EnvDTE;
using EnvDTE80;
using System;

namespace Atma.TitleBarNone.Resolvers
{
	public class IDEResolver : Resolver
	{
		public enum CallbackReason
		{
			StartupComplete,
			ModeChanged,
			ShutdownInitiated
		}

		public IDEResolver(DTE2 dte, Action<DTE2, CallbackReason> callback)
			: base(new[] { "ide-name" })
		{
			m_DTE = dte;
			m_Callback = callback;

			m_DTE.Events.DTEEvents.OnStartupComplete += () => m_Callback(m_DTE, CallbackReason.StartupComplete);
			m_DTE.Events.DTEEvents.ModeChanged += (vsIDEMode mode) => m_Callback(m_DTE, CallbackReason.ModeChanged);
			m_DTE.Events.DTEEvents.OnBeginShutdown += () => m_Callback(m_DTE, CallbackReason.ShutdownInitiated);
		}

		public static Resolver Create(DTE2 dte, Action<DTE2, CallbackReason> callback)
		{
			return new IDEResolver(dte, callback);
		}

		public override bool ResolveBoolean(VsState state, string tag)
		{
			return true; // ide always present
		}

		public override string Resolve(VsState state, string tag)
		{
			if (tag == "ide-name")
				return "Microsoft Visual Studio";
			else if (tag == "ide-name-and-mode")
				return "Microsoft Visual Studio" + GetModeTitle(state);
			else
				return null;
		}

		private string GetModeTitle(VsState state)
		{
			if (state.Mode == VsMode.Design)
				return "";
			else if (state.Mode == VsMode.Running)
				return " (Running)";
			else
				return " (Debugging)";
		}

		private DTE2 m_DTE;
		private Action<DTE2, CallbackReason> m_Callback;
	}
}
