using EnvDTE;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Atma.TitleBarNone.Resolvers
{
	public struct VsState
	{
		public dbgDebugMode Mode;
		public Solution Solution;
	}

	public abstract class Resolver
	{
		protected Resolver(IEnumerable<string> tags)
		{
			m_Tags = tags.ToList();
		}

		public abstract bool Available { get; }

		public delegate void ChangedDelegate(Resolver resolver);
		public abstract ChangedDelegate Changed { get; set; }

		public bool Applicable(string tag)
		{
			return m_Tags.Contains(new string(tag.TakeWhile(x => char.IsLetter(x) || x == '-').ToArray()));
		}

		public abstract bool ResolveBoolean(VsState state, string tag);
		public abstract string Resolve(VsState state, string tag);

		public virtual int SatisfiesDependency(Settings.SettingsTriplet triplet)
		{
			return 0;
		}

		protected void RaiseChange()
		{
			Changed?.Invoke(this);
		}

		private readonly List<string> m_Tags;
	}
}
