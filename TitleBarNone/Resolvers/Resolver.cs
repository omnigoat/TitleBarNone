using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Atma.TitleBarNone.Resolvers
{
	public struct VsState
	{
		public IEnumerable<Resolver> Resolvers;
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

		public virtual bool SatisfiesDependency(Tuple<string, string> d)
		{
			return false;
		}

		protected static bool GlobMatch(string pattern, string match)
		{
			return string.IsNullOrEmpty(pattern) || new Regex(
				"^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
				RegexOptions.IgnoreCase | RegexOptions.Singleline).IsMatch(match);
		}

		private readonly List<string> m_Tags;
	}
}
