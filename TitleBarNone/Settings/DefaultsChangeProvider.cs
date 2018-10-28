using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atma.TitleBarNone.Settings
{
	class DefaultsChangeProvider : ChangeProvider
	{
		public override List<SettingsTriplet> Triplets => new List<SettingsTriplet>
		{
			new SettingsTriplet {
				Dependency = PatternDependency.None,
				FormatIfNothingOpened = new TitleBarFormat(Defaults.PatternIfNothingOpen),
				FormatIfDocumentOpened = new TitleBarFormat(Defaults.PatternIfDocumentOpen),
				FormatIfSolutionOpened = new TitleBarFormat(Defaults.PatternIfSolutionOpen)
			}
		};

		public override event ChangedEvent Changed;

		protected override void DisposeImpl()
		{
		}
	}
}
