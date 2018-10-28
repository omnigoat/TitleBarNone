using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atma.TitleBarNone.Settings
{
	class VsOptionsChangeProvider : ChangeProvider
	{
		public VsOptionsChangeProvider(SettingsPageGrid settingsPage)
		{
			SettingsPage = settingsPage;
			SettingsPage.SettingsChanged += SettingsPage_SettingsChanged;
		}

		public override event ChangedEvent Changed;
		public override List<SettingsTriplet> Triplets => new List<SettingsTriplet> { m_Triplet };

		private void SettingsPage_SettingsChanged(object sender, EventArgs e)
		{
			bool requiresUpdate =
				(m_Triplet.FormatIfNothingOpened != SettingsPage.PatternIfNothingOpen) ||
				(m_Triplet.FormatIfDocumentOpened != SettingsPage.PatternIfDocumentOpen) ||
				(m_Triplet.FormatIfSolutionOpened != SettingsPage.PatternIfSolutionOpen);

			m_Triplet.FormatIfNothingOpened = SettingsPage.PatternIfNothingOpen;
			m_Triplet.FormatIfDocumentOpened = SettingsPage.PatternIfDocumentOpen;
			m_Triplet.FormatIfSolutionOpened = SettingsPage.PatternIfSolutionOpen;

			if (requiresUpdate)
				Changed?.Invoke();
		}

		protected override void DisposeImpl()
		{
			// nothing to do
		}

		readonly SettingsPageGrid SettingsPage;
		private SettingsTriplet m_Triplet = new SettingsTriplet();
	}
}
