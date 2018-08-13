using System.Collections.Generic;

namespace Atma.TitleBarNone.Settings
{
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
}
