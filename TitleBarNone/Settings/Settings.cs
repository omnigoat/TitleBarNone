using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace Atma.TitleBarNone.Settings
{
	[Flags]
	public enum Dependencies
	{
		None = 0,
		SolutionGlob = 1,
		Git = 2,
		Versionr = 4,
		SVN = 8
	}

	public class TitleBarFormat
	{
		public TitleBarFormat(string pattern)
		{
			Pattern = pattern;
		}

		public TitleBarFormat(string pattern, System.Drawing.Color? color)
		{
			Pattern = pattern;
			Color = color;
		}

		public string Pattern;
		public System.Drawing.Color? Color;
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
		public List<Tuple<string, string>> PatternDependencies = new List<Tuple<string, string>>();

		public TitleBarFormat FormatIfNothingOpened;
		public TitleBarFormat FormatIfDocumentOpened;
		public TitleBarFormat FormatIfSolutionOpened;
	}

}
