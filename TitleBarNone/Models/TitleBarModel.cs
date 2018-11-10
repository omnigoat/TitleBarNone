using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace Atma.TitleBarNone.Models
{
	internal class TitleBarModel
	{
		public TitleBarModel(Window window)
		{
			this.Window = window;

			try
			{
				// set title bar of main window
				if (window == Application.Current.MainWindow)
				{
					var windowContentPresenter = VisualTreeHelper.GetChild(window, 0);
					var rootGrid = VisualTreeHelper.GetChild(windowContentPresenter, 0);

					this.titleBarContainer = VisualTreeHelper.GetChild(rootGrid, 0);

					var dockPanel = VisualTreeHelper.GetChild(this.titleBarContainer, 0);
					this.titleBarTextBox = VisualTreeHelper.GetChild(dockPanel, 3) as TextBlock;
				}
				// haha, do something else?
				else
				{
					var windowContentPresenter = VisualTreeHelper.GetChild(window, 0);
					var rootGrid = VisualTreeHelper.GetChild(windowContentPresenter, 0);
					if (VisualTreeHelper.GetChildrenCount(rootGrid) == 0)
						return;

					var rootDockPanel = VisualTreeHelper.GetChild(rootGrid, 0);
					var titleBarContainer = VisualTreeHelper.GetChild(rootDockPanel, 0);
					var titleBar = VisualTreeHelper.GetChild(titleBarContainer, 0);
					var border = VisualTreeHelper.GetChild(titleBar, 0);
					var contentPresenter = VisualTreeHelper.GetChild(border, 0);
					var grid = VisualTreeHelper.GetChild(contentPresenter, 0);

					this.titleBarContainer = grid;

					this.titleBarTextBox = VisualTreeHelper.GetChild(grid, 1) as TextBlock;
				}

				if (this.titleBarContainer != null)
				{
					System.Reflection.PropertyInfo propertyInfo = this.titleBarContainer.GetType().GetProperty(ColorPropertyName);
					this.defaultBackgroundValue = propertyInfo.GetValue(this.titleBarContainer) as Brush;
				}

				if (this.titleBarTextBox != null)
				{
					this.defaultTextForeground = this.titleBarTextBox.Foreground;
				}
			}
			catch
			{
			}
		}

		public Window Window { get; internal set; }

		public void SetTitleBarColor(System.Drawing.Color? color)
		{
			try
			{
				CalculateColors(color, out Brush backgroundColor, out Brush textColor);
				
				if (titleBarContainer != null)
				{
					System.Reflection.PropertyInfo propertyInfo = titleBarContainer.GetType().GetProperty(ColorPropertyName);
					propertyInfo.SetValue(titleBarContainer, backgroundColor, null);
				}

				if (titleBarTextBox != null)
				{
					titleBarTextBox.Foreground = textColor;
				}
			}
			catch
			{
				System.Diagnostics.Debug.Fail("TitleBarModel.SetTitleBarColor - couldn't :(");
			}
		}

		public void CalculateColors(System.Drawing.Color? color, out Brush backgroundColor, out Brush textColor)
		{
			if (!color.HasValue)
			{
				backgroundColor = defaultBackgroundValue;
				textColor = defaultTextForeground;
			}
			else
			{
				var c = color.Value;

				backgroundColor = new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));

				float luminance = 0.299f * c.R + 0.587f * c.G + 0.114f * c.B;
				if (luminance > 128.0f)
					textColor = new SolidColorBrush(Color.FromRgb(0, 0, 0));
				else
					textColor = new SolidColorBrush(Color.FromRgb(255, 255, 255));
			}
		}

		private DependencyObject titleBarContainer = null;
		private TextBlock titleBarTextBox = null;

		private readonly Brush defaultBackgroundValue;
		private readonly Brush defaultTextForeground;

		private const string ColorPropertyName = "Background";
	}
}
