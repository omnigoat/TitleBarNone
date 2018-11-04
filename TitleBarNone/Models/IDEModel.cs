using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atma.TitleBarNone.Models
{
	public class IDEModel
	{
		public IDEModel(DTE dte)
		{
			this.dte = dte;

			dteEvents = this.dte.Events.DTEEvents;
			dteEvents.OnStartupComplete += () => StartupComplete?.Invoke();
			dteEvents.OnBeginShutdown += () => ShutdownInitiated?.Invoke();

			debuggerEvents = this.dte.Events.DebuggerEvents;
			debuggerEvents.OnEnterDesignMode += (dbgEventReason e) => OnModeChanged(dbgDebugMode.dbgDesignMode);
			debuggerEvents.OnEnterRunMode += (dbgEventReason e) => OnModeChanged(dbgDebugMode.dbgRunMode);
			debuggerEvents.OnEnterBreakMode += (dbgEventReason e, ref dbgExecutionAction action) => OnModeChanged(dbgDebugMode.dbgBreakMode);

			VsMode = this.dte.Debugger.CurrentMode;
		}

		public dbgDebugMode VsMode { get; set; }

		public delegate void StartupCompleteDelegate();
		public delegate void ShutdownInitiatedDelegate();
		public delegate void IdeModeChangedDelegate(dbgDebugMode mode);

		public event StartupCompleteDelegate StartupComplete;
		public event ShutdownInitiatedDelegate ShutdownInitiated;
		public event IdeModeChangedDelegate IdeModeChanged;

		private void OnModeChanged(dbgDebugMode mode)
		{
			VsMode = mode;
			IdeModeChanged?.Invoke(VsMode);
		}

		private DTE dte;
		private DTEEvents dteEvents;
		private DebuggerEvents debuggerEvents;
	}
}
