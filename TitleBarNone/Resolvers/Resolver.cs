using EnvDTE;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Atma.TitleBarNone.Resolvers
{
	public struct VsState
	{
		public dbgDebugMode Mode;
		public EnvDTE.Solution Solution;
	}

	public abstract class Resolver
	{
		protected Resolver(IEnumerable<string> tags)
		{
			m_Tags = tags.ToList();
		}

		delegate void ChangedDelegate(Resolver resolver);
		ChangedDelegate Changed;

		public bool Applicable(string tag) { return m_Tags.Contains(tag); }
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
