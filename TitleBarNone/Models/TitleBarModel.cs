using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Windows.Markup;
using System.IO;
using System.Xml;

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

		public static List<T> GetChildren<T>(this UIElement r)
		{
			List<T> children = new List<T>();
			var c = VisualTreeHelper.GetChildrenCount(r);
			for (int i = 0; i < c; ++i)
			{
				var dp = VisualTreeHelper.GetChild(r, i);
				if (dp is T dpt)
					children.Add(dpt);
			}

			return children;
		}
	}

	internal abstract class TitleBarModel
	{
		public TitleBarModel(Window window)
		{
			this.Window = window;
		}

		public static TitleBarModel Make(string vsVersion, Window x)
		{
			try
			{
				if (IsMsvc2017(vsVersion))
					return new TitleBarModel2017(x);
				else if (IsMsvc2019(vsVersion))
					return new TitleBarModel2019(x);
			}
			catch { }

			return null;
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
					//titleBarTextBox.Foreground = textColor;
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

		// visual-studio 2017 & 2019
		protected DependencyObject titleBarContainer = null;

		// background-color
		protected Border titleBarBorder = null;
		// textbox
		protected TextBlock titleBarTextBox = null;

		protected Brush defaultBackgroundValue;
		protected Brush defaultTextForeground;

		protected const string ColorPropertyName = "Background";

		private static bool IsMsvc2017(string str) => str.StartsWith("15");
		private static bool IsMsvc2019(string str) => str.StartsWith("16");
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

					titleBarContainer = VisualTreeHelper.GetChild(rootGrid, 0);

					var dockPanel = VisualTreeHelper.GetChild(titleBarContainer, 0);
					titleBarTextBox = VisualTreeHelper.GetChild(dockPanel, 3) as TextBlock;
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
		protected class TitleLabel
		{
			public TitleLabel(Border border)
			{
				titleBarBorder = border;
				titleBarTextBox = titleBarBorder?.GetElement<TextBlock>();
			}

			// background-color
			protected Border titleBarBorder = null;
			// textbox
			protected TextBlock titleBarTextBox = null;
		}

		public TitleBarModel2019(Window window) : base(window)
		{
			// set title bar of main window
			if (window == Application.Current.MainWindow)
			{
				DeconstructMainWindow(window);
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




		protected void DeconstructMainWindow(Window window)
		{
			var windowContentPresenter = VisualTreeHelper.GetChild(window, 0);

			// get containing title-bar
			titleBarContainer = window.GetElement<Border>("MainWindowTitleBar");

			// see if we have info - if we don't we probably loaded an empty Visual Studio
			var infoElement = (titleBarContainer as UIElement).GetElement<ContentControl>("PART_SolutionInfoControlHost");
			if (!infoElement.HasContent)
				throw new InvalidDataException("main-window hasn't loaded title-bar yet");

			// get 
			var iegrid = infoElement?.GetElement<Grid>();

			var labels = iegrid
				.GetChildren<Border>()
				.Where(x => x.GetElement<TextBlock>() != null)
				.Select(x => new TitleLabel(x));

			this.titleBarBorder = infoElement?.GetElement<Border>("TextBorder");
			this.titleBarTextBox = this.titleBarBorder?.GetElement<TextBlock>();

			var g = new Border
			{
				Background = titleBarBorder.Background,
				BorderBrush = titleBarBorder.BorderBrush,
				BorderThickness = titleBarBorder.BorderThickness,
				Padding = new Thickness(titleBarBorder.Padding.Left, titleBarBorder.Padding.Top, titleBarBorder.Padding.Right, titleBarBorder.Padding.Bottom),
				Child = new Border
				{
					Margin = new Thickness(0, 4.5, 0, 4.5),
					Child = new TextBlock
					{
						Text = "atma",
						
						FontFamily = titleBarTextBox.FontFamily,
						FontWeight = titleBarTextBox.FontWeight,
						FontSize = titleBarTextBox.FontSize,
						FontStyle = titleBarTextBox.FontStyle,
						FontStretch = titleBarTextBox.FontStretch,
						Foreground = titleBarTextBox.Foreground,
						TextAlignment = titleBarTextBox.TextAlignment,
						TextEffects = titleBarTextBox.TextEffects,
						Padding = titleBarTextBox.Padding,
						BaselineOffset = titleBarTextBox.BaselineOffset,
						Width = Math.Floor(MeasureString(titleBarTextBox, "atma").Width)
					}
				}
			};

			iegrid.ColumnDefinitions[1].Width = GridLength.Auto;
			iegrid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});

			g.SetValue(Grid.ColumnProperty, 2);
			//g.HorizontalAlignment = HorizontalAlignment.;
			//iegrid.SetValue(Grid.WidthProperty, Size)
			iegrid.Children.Add(g);
		}

		private Size MeasureString(TextBlock textBlock, string candidate)
		{
			var formattedText = new FormattedText(
				candidate,
				System.Globalization.CultureInfo.CurrentCulture,
				FlowDirection.LeftToRight,
				new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
				textBlock.FontSize,
				Brushes.Black,
				new NumberSubstitution(),
				1);

			return new Size(formattedText.Width, formattedText.Height);
		}


		public static Size MeasureTextSize(string text, FontFamily fontFamily, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch, double fontSize)
		{
			FormattedText ft = new FormattedText(text,
												 System.Globalization.CultureInfo.CurrentCulture,
												 FlowDirection.LeftToRight,
												 new Typeface(fontFamily, fontStyle, fontWeight, fontStretch),
												 fontSize,
												 Brushes.Black,
												 1.0);
			return new Size(ft.Width, ft.Height);
		}

		/// <summary>
		/// Get the required height and width of the specified text. Uses Glyph's
		/// </summary>
		public static Size MeasureText(string text, FontFamily fontFamily, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch, double fontSize)
		{
			Typeface typeface = new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
			GlyphTypeface glyphTypeface;

			if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
			{
				return MeasureTextSize(text, fontFamily, fontStyle, fontWeight, fontStretch, fontSize);
			}

			int totalWidth = 0;
			double height = 0;

			for (int n = 0; n < text.Length; n++)
			{
				ushort glyphIndex = glyphTypeface.CharacterToGlyphMap[text[n]];

				int width = (int)(glyphTypeface.AdvanceWidths[glyphIndex] * fontSize);

				double glyphHeight = glyphTypeface.AdvanceHeights[glyphIndex] * fontSize;

				if (glyphHeight > height)
				{
					height = glyphHeight;
				}

				totalWidth += width;
			}

			return new Size(totalWidth, height);
		}
	}
}
