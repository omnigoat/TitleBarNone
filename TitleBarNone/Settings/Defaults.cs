using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atma.TitleBarNone.Settings
{
	static class Defaults
	{
		public const string ConfgFileName = ".title-bar-none";

		public const string PatternIfNothingOpen = "PatternIfNothingOpen|$ide-name|";
		public const string PatternIfDocumentOpen = "PatternIfDocumentOpen|$document-name - $ide-name|";
		public const string PatternIfSolutionOpen = "PatternIfSolutionOpen|$solution-name?ide-mode{ $} - $ide-name|";

		public const string GitPatternIfOpen = "GitPatternIfOpen|?git{[$git-branch] }$item-name?ide-mode{ $} - $ide-name|";
	}
}
