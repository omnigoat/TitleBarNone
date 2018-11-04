using System.IO;

namespace Atma.TitleBarNone.Settings
{
	class SolutionFileChangeProvider
		: FileChangeProvider
	{
		public SolutionFileChangeProvider(string path)
			: base(GetConfigFile(path))
		{
		}

		static string GetConfigFile(string path)
		{
			var file = new FileInfo(path);
			if (!file.Exists)
				return "";
			
			return file.Directory.FullName.ToString();
		}
	}
}
