using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

namespace Atma.TitleBarNone.Models
{
	static class UIElementExtensions
	{
		public static T GetElement<T>(this UIElement root, string name = null, int max_depth = int.MaxValue) where T : class
		{
			DependencyObject find(DependencyObject r, int depth)
			{
				if (depth == 0) return null;
				var c = VisualTreeHelper.GetChildrenCount(r);
				for (int i = 0; i < c; ++i)
				{
					var e = VisualTreeHelper.GetChild(r, i);
					if (e is T)
					{
						if (name == null || (e as FrameworkElement).Name == name)
							return e;
					}
					e = find(e, depth - 1);
					if (e != null) return e;
				}
				return null;
			}

			return find(root, max_depth) as T;
		}

	}

	internal abstract class TitleBarModel
	{
		public TitleBarModel(Window window)
		{
			this.Window = window;
		}

		public Window Window { get; private set; }

		public System.Linq.Expressions.Expression Expression => throw new NotImplementedException();

		public Type ElementType => throw new NotImplementedException();

		public IQueryProvider Provider => throw new NotImplementedException();

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
				else if (titleBarBorder != null)
				{
					System.Reflection.PropertyInfo propertyInfo = this.titleBarBorder.GetType().GetProperty(ColorPropertyName);
					propertyInfo.SetValue(this.titleBarBorder, backgroundColor, null);
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

		// visual-studio 2017
		protected DependencyObject titleBarContainer = null;

		// background-color
		protected Border titleBarBorder = null;
		// textbox
		protected TextBlock titleBarTextBox = null;

		protected Brush defaultBackgroundValue;
		protected Brush defaultTextForeground;

		protected const string ColorPropertyName = "Background";
	}

	internal class TitleBarModel2017 : TitleBarModel
	{
		public TitleBarModel2017(Window window) : base(window)
		{
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
	}

	internal class TitleBarModel2019 : TitleBarModel
	{
		public TitleBarModel2019(Window window) : base(window)
		{
			try
			{
				// set title bar of main window
				if (window == Application.Current.MainWindow)
				{
					var windowContentPresenter = VisualTreeHelper.GetChild(window, 0);


					var titleBar = window.GetElement<Border>("MainWindowTitleBar");
					var rootGrid = titleBar.GetElement<Grid>(max_depth: 1);
					var infoElement = rootGrid?.GetElement<ContentControl>("PART_SolutionInfoControlHost");
					var iegrid = rootGrid?.GetElement<Grid>();
					this.titleBarBorder = infoElement?.GetElement<Border>("TextBorder");
					this.titleBarTextBox = this.titleBarBorder?.GetElement<TextBlock>();
					//iegrid.Children.Add(this.titleBarBorder);
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
	}
}
