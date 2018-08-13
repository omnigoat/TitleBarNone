using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.IO;
using EnvDTE;
using TitleBarNone;

namespace Atma.TitleBarNone.Controls
{
	public partial class PatternControl : UserControl
	{
		private TitleBarNonePackage package;
		private IWindowsFormsEditorService editorService;
		private Editors.PreviewRequiresAttribute.Requirement previewRequires;

		public PatternControl(TitleBarNonePackage package, IWindowsFormsEditorService editorService)
		{
			this.package = package;
			this.editorService = editorService;
			this.InitializeComponent();
			this.txEdit.Focus();
		}

		public string Pattern
		{
			get { return this.txEdit.Text; }
			set
			{
				this.txEdit.Text = value;
				this.txEdit.Focus();
				this.txEdit.Select(this.txEdit.Text.Length, 0); // set insert point to the end
				this.updatePreview();
			}
		}
		public string DefaultPattern { get; set; }

		public Editors.PreviewRequiresAttribute.Requirement PreviewRequires
		{
			get { return this.previewRequires; }
			set
			{
				this.previewRequires = value;
				this.updatePreview();
			}
		}

		private void lbMore_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left)
				return;
			this.ctxMenu.Show(Cursor.Position);

		}

		private void setToDefault_Click(object sender, EventArgs e)
		{
			this.txEdit.Text = this.DefaultPattern;
			this.txEdit.Select(this.txEdit.Text.Length, 0); // set insert point to the end
		}

		private void insertTag_Click(object sender, EventArgs e)
		{
			var m = (ToolStripMenuItem)sender;
			this.txEdit.Paste($"[{m.Tag}]");
		}

		private void ctxMenu_Opening(object sender, CancelEventArgs e)
		{
			var bHaveDefault = this.DefaultPattern != null;
			this.setToDefault.Visible = bHaveDefault;
			this.resetSep.Visible = bHaveDefault;

			this.insertTag.Visible = false;
			// remove old entries
			for (var i = this.ctxMenu.Items.Count - 1; i >= 0; i--)
				if (this.ctxMenu.Items[i].Tag is string)
					this.ctxMenu.Items.RemoveAt(i);
			var z = this.ctxMenu.Items.IndexOf(this.insertTag);

			//foreach (var tag in TitleBarNonePackage.CurrentPackage.SupportedTags)
			//{
			//	var LocalizedDescription = Resources.ResourceManager.GetString("tag_" + tag.Replace(":", ""));
			//	var m = new ToolStripMenuItem(string.Format(this.insertTag.Text, tag))
			//	{
			//		ToolTipText = LocalizedDescription,
			//		Tag = tag
			//	};
			//	m.Click += this.insertTag_Click;
			//	this.ctxMenu.Items.Insert(z++, m);
			//}
		}

		private void txEdit_TextChanged(object sender, EventArgs e)
		{
			this.timerUpdate.Stop();
			this.timerUpdate.Start();
		}

		private void timerUpdate_Tick(object sender, EventArgs e)
		{
			this.timerUpdate.Stop();
			this.updatePreview();

		}

		private void updatePreview()
		{
			this.BackColor = SystemColors.Control;
			var solution = package.DTE.Solution;
			var solutionFp = solution?.FullName;

			Document activeDocument = null;
			try
			{
				activeDocument = package.DTE.ActiveDocument;
			}
			catch
			{
				//Do nothing
			}
			Window activeWindow = null;
			try
			{
				activeWindow = package.DTE.ActiveWindow;
			}
			catch
			{
				//Do nothing
			}

#if false
			switch (this.PreviewRequires)
			{
				case Editors.PreviewRequiresAttribute.Requirement.Document:
				{
					if (!(solution == null || string.IsNullOrEmpty(solutionFp)))
					{
						this.lbPreview.Text = "please close solution to enable preview!";
						return;
					}

					var ad = package.GetActiveDocumentNameOrEmpty(activeDocument);
					if (string.IsNullOrEmpty(ad))
					{
						ad = package.GetActiveWindowNameOrEmpty(activeWindow);
					}
					if (string.IsNullOrEmpty(ad))
					{
						this.lbPreview.Text = "please load any document or open any window to enable preview!";
						return;
					}
					break;
				}

				case Editors.PreviewRequiresAttribute.Requirement.Solution:
				{
					if (solution == null || string.IsNullOrEmpty(solutionFp))
					{
						this.lbPreview.Text = "please load any solution to enable preview!";
						return;
					}
					break;
				}
			}

			var settings = TitleBarNonePackage.CurrentPackage.GetSettings(solutionFp);
			var pattern = this.txEdit.Text;
			this.lbPreview.Text = "preview: " + TitleBarNonePackage.CurrentPackage.GetNewTitle(solution, pattern, settings);
#endif
		}
	}
}