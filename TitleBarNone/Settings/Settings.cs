using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace Atma.TitleBarNone.Settings
{

	public enum PatternDependency
	{
		None,
		Git,
		Vsr
	}

	public class TitleBarFormat
	{
		public TitleBarFormat(string pattern)
		{
			Pattern = pattern;
		}

		public TitleBarFormat(string pattern, System.Drawing.Color color)
		{
			Pattern = pattern;
			Color = color;
		}

		public string Pattern = null;
		public System.Drawing.Color Color = System.Drawing.Color.Transparent;
	}

	public class TitleBarFormatConverter : TypeConverter
	{
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (destinationType == typeof(string))
			{
				return ((TitleBarFormat)value).Pattern;
			}

			return base.ConvertTo(context, culture, value, destinationType);
		}

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value is string)
			{
				return new TitleBarFormat(value as string);
			}

			return base.ConvertFrom(context, culture, value);
		}

	}


	public class SettingsTriplet
	{
		public PatternDependency Dependency = PatternDependency.None;

		public string SolutionFilter = "";

		public TitleBarFormat FormatIfNothingOpened;
		public TitleBarFormat FormatIfDocumentOpened;
		public TitleBarFormat FormatIfSolutionOpened;
	}

#if false
	public class Settings
	{
		public string SolutionFilePath;
		public string SolutionFileName;

		// Apply overrides for Paths, Paths is null for solution override
		public List<string> Paths;

		// solution name (file name part or override value)
		public string SolutionName;

		public string SolutionPattern;

		public void Merge(Settings s)
		{
			Merge_(ref SolutionName, s.SolutionName);
			Merge_(ref SolutionPattern, s.SolutionPattern);
		}

		private void Merge_<T>(ref T d, T s)
		{
			if (s != null)
				d = s;
		}
	}
#endif
}
