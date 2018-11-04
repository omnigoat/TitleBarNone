using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Task = System.Threading.Tasks.Task;
using SettingsPageGrid = Atma.TitleBarNone.Settings.SettingsPageGrid;
using System.Collections.Generic;
using Atma.TitleBarNone.Resolvers;
using System.Windows;
using System.IO;

namespace Atma.TitleBarNone
{
	public enum VsEditingMode
	{
		Nothing,
		Document,
		Solution
	}

	[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
	[Guid(PackageGuidString)]
	[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
	[ProvideAutoLoad(UIContextGuids.NoSolution)]
	[ProvideOptionPage(typeof(SettingsPageGrid), "Title Bar None", "Settings", 101, 1000, true)]
	public class TitleBarNonePackage : AsyncPackage
	{
		public const string PackageGuidString = "16599b2d-db6e-49cd-a76e-2b6da7343bcc";

		public const string SolutionSettingsOverrideExtension = ".rn.xml";
		public const string PathTag = "Path";
		public const string SolutionNameTag = "solution-name";
		public const string SolutionPatternTag = "solution-pattern";

		public TitleBarNonePackage()
		{
		}

		public DTE DTE
		{
			get;
			private set;
		}

		public string Pattern
		{
			get
			{
				if (DTE.Solution.IsOpen)
					return PatternIfSolutionOpened;
				else if (DTE.Documents.Count > 0)
					return PatternIfDocumentOpened;
				else
					return PatternIfNothingOpened;
			}
		}

		private IEnumerable<Resolver> Resolvers => m_Resolvers.AsEnumerable().Reverse();

		private IEnumerable<Settings.SettingsTriplet> SettingsTriplets
		{
			get
			{
				return m_UserDirFileChangeProvider.Triplets
					.Concat(m_SolutionsFileChangeProvider.Triplets)
					.Concat(m_DefaultsChangeProvider.Triplets)
					;
			}
		}

		private string PatternIfNothingOpened => SettingsTriplets
			.Where(TripletDependenciesAreSatisfied)
			.Where(x => !string.IsNullOrEmpty(x.FormatIfNothingOpened?.Pattern))
			.FirstOrDefault()?.FormatIfNothingOpened.Pattern ?? "";

		private string PatternIfDocumentOpened => SettingsTriplets
			.Where(TripletDependenciesAreSatisfied)
			.Where(x => !string.IsNullOrEmpty(x.FormatIfDocumentOpened?.Pattern))
			.FirstOrDefault()?.FormatIfDocumentOpened.Pattern ?? "";

		private string PatternIfSolutionOpened => SettingsTriplets
			.Where(TripletDependenciesAreSatisfied)
			.Where(x => !string.IsNullOrEmpty(x.FormatIfSolutionOpened?.Pattern))
			.FirstOrDefault()?.FormatIfSolutionOpened.Pattern ?? "";

		private bool TripletDependenciesAreSatisfied(Settings.SettingsTriplet triplet)
		{
			var result = Resolvers
				.Where(x => x.Available)
				.Aggregate(0, (acc, x) => {
					return acc | x.SatisfiesDependency(triplet);
				});

			return result == 0x3;
		}

		internal SettingsPageGrid UISettings { get; private set; }

		private bool GitIsRequired => SettingsTriplets.Any(x => x.FormatIfSolutionOpened.Pattern.Contains("$git"));

		protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
		{
			// initialize the DTE and bind events
			DTE = await GetServiceAsync(typeof(DTE)) as DTE;

			// create models of IDE/Solution
			ideModel = new Models.IDEModel(DTE);
			solutionModel = new Models.SolutionModel(DTE);

			solutionModel.SolutionOpened += OnSolutionOpened;
			solutionModel.SolutionClosed += OnSolutionClosed;

			// create resolvers
			m_Resolvers = new List<Resolver>
			{
				IDEResolver.Create(ideModel),
				SolutionResolver.Create(solutionModel),
				GitResolver.Create(solutionModel),
				VsrResolver.Create(solutionModel)
			};

			foreach (var resolver in m_Resolvers)
			{
				resolver.Changed += (Resolver r) => UpdateTitleAsync();
			}

			// create settings readers for user-dir
			m_UserDirFileChangeProvider = new Settings.UserDirFileChangeProvider();
			m_UserDirFileChangeProvider.Changed += UpdateTitleAsync;

			// switch to UI thread
			await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			// get UI settings hooks
			UISettings = GetDialogPage(typeof(SettingsPageGrid)) as SettingsPageGrid;
			m_VsOptionsChangeProvider = new Settings.VsOptionsChangeProvider(UISettings);
			m_VsOptionsChangeProvider.Changed += UpdateTitleAsync;
		}

		private void OnSolutionOpened(Solution solution)
		{
			// reset the solution-file settings file
			m_SolutionsFileChangeProvider = new Settings.SolutionFileChangeProvider(solution.FileName);

			//if (VsrResolver.Required(out string vsrpath, Path.GetDirectoryName(DTE.Solution.FileName)))
			//{
			//	m_Resolvers.Add(new VsrResolver(vsrpath));
			//}

			UpdateTitleAsync();
		}

		private void OnSolutionClosed()
		{
			if (m_SolutionsFileChangeProvider != null)
				m_SolutionsFileChangeProvider.Dispose();

			UpdateTitleAsync();
		}

		private void UpdateTitleAsync()
		{
			Application.Current?.Dispatcher?.InvokeAsync(() =>
			{
				UpdateTitle();
			});
		}

		private void UpdateTitle()
		{
			string pattern = Pattern;
			if (string.IsNullOrEmpty(pattern))
				return;

			var state = new VsState()
			{
				Mode = m_Mode,
				Solution = DTE.Solution
			};

			int i = 0;
			if (ParseImpl(out string transformed, state, pattern, ref i, null))
			{
				ChangeWindowTitle(transformed);
			}
		}

		private bool ParseImpl(out string transformed, VsState state, string pattern, ref int i, string singleDollar)
		{
			transformed = "";

			// begin pattern parsing
			while (i < pattern.Length)
			{
				// escape sequences
				if (pattern[i] == '\\')
				{
					++i;
					if (i == pattern.Length)
						break;
					transformed += pattern[i];
					++i;
				}
				// predicates
				else if (pattern[i] == '?' && ParseQuestion(out string r, state, pattern, ref i, singleDollar))
				{
					transformed += r;
				}
				// dollars
				else if (pattern[i] == '$' && ParseDollar(out string r2, state, pattern, ref i, singleDollar))
				{
					transformed += r2;
				}
				else
				{
					transformed += pattern[i];
					++i;
				}
			}

			return true;
		}


		private bool ParseQuestion(out string result, VsState state, string pattern, ref int i, string singleDollar)
		{
			var tag = new string(pattern
				.Substring(i + 1)
				.TakeWhile(x => x >= 'a' && x <= 'z' || x == '-')
				.ToArray());

			i += 1 + tag.Length;

			bool valid = m_Resolvers
				.FirstOrDefault(x => x.Applicable(tag))
				?.ResolveBoolean(state, tag) ?? false;

			if (i == pattern.Length)
			{
				result = null;
				return valid;
			}

			// look for braced group {....}, and skip if question was bad
			if (pattern[i] == '{')
			{
				if (!valid)
				{
					while (i != pattern.Length)
					{
						++i;
						if (pattern[i] == '}')
						{
							++i;
							break;
						}
					}

					result = null;
					return false;
				}
				else
				{
					var transformed_tag = m_Resolvers
						.FirstOrDefault(x => x.Applicable(tag))
						?.Resolve(state, tag);

					var inner = new string(pattern
						.Substring(i + 1)
						.TakeWhile(x => x != '}')
						.ToArray());

					i += 1 + inner.Length + 1;

					int j = 0;
					ParseImpl(out result, state, inner, ref j, transformed_tag);
				}
			}
			else
			{
				result = null;
				return false;
			}

			return true;
		}

		// we support two common methods of string escaping: parens and identifier
		//
		// any pattern that contains a $ will either be immeidately followed with an identifier,
		// or a braced expression, e.g., $git-branch, or ${git-branch}
		//
		// the identifier may be a function-call, like "$path(0, 2)"
		//
		private bool ParseDollar(out string result, VsState state, string pattern, ref int i, string singleDollar)
		{
			++i;

			// peek for brace vs non-brace
			//
			// find EOF or whitespace or number
			if (i == pattern.Length || char.IsWhiteSpace(pattern[i]) || char.IsNumber(pattern[i]))
			{
				++i;
				result = singleDollar ?? "";
				return true;
			}
			// find brace
			else if (pattern[i] == '{')
			{
				var braceExpr = new string(pattern
					.Substring(i + 1)
					.TakeWhile(x => x != '}')
					.ToArray());

				i += 1 + braceExpr.Length;
				if (i != pattern.Length && pattern[i] == '}')
					++i;

				// maybe:
				//  - split by whitespace
				//  - attempt to resolve all
				//  - join together
				result = braceExpr.Split(' ')
					.Select(x =>
					{
						return m_Resolvers
						.FirstOrDefault(r => r.Applicable(x))
						?.Resolve(state, x) ?? x;
					})
					.Aggregate((a, b) => a + " " + b);
					
			}
			// find identifier
			else if (pattern[i] >= 'a' && pattern[i] <= 'z')
			{
				var idenExpr = new string(pattern
					.Substring(i)
					.TakeWhile(x => x >= 'a' && x <= 'z' || x == '-')
					.ToArray());

				i += idenExpr.Length;

				if (i != pattern.Length)
				{
					if (pattern[i] == '(')
					{
						var argExpr = new string(pattern
							.Substring(i)
							.TakeWhile(x => x != ')')
							.ToArray());

						i += argExpr.Length;
						if (i != pattern.Length && pattern[i] == ')')
						{
							++i;
							argExpr += ')';
						}

						idenExpr += argExpr;
					}
				}

				result = m_Resolvers
					.FirstOrDefault(x => x.Applicable(idenExpr))
					?.Resolve(state, idenExpr)
					?? idenExpr;
			}
			else
			{
				result = "";
			}

			return true;
		}

		private void ChangeWindowTitle(string title)
		{
			if (DTE == null || DTE.MainWindow == null)
				return;

			if (System.Windows.Application.Current.MainWindow == null)
			{
				Debug.Print("ChangeWindowTitle - exiting early because System.Windows.Application.Current.MainWindow == null");
				return;
			}

			try
			{
				ThreadHelper.Generic.Invoke(() =>
				{
					System.Windows.Application.Current.MainWindow.Title = DTE.MainWindow.Caption;
					if (System.Windows.Application.Current.MainWindow.Title != title)
						System.Windows.Application.Current.MainWindow.Title = title;
				});
			}
			catch (Exception e)
			{
				Debug.Print(e.Message);
			}
		}

		// models
		private Models.SolutionModel solutionModel;
		private Models.IDEModel ideModel;

		// apparently these could get garbage collected otherwise
		private dbgDebugMode m_Mode = dbgDebugMode.dbgDesignMode;

		private List<Resolver> m_Resolvers;

		private Settings.SolutionFileChangeProvider m_SolutionsFileChangeProvider;
		private Settings.UserDirFileChangeProvider m_UserDirFileChangeProvider;
		private Settings.VsOptionsChangeProvider m_VsOptionsChangeProvider;
		private Settings.DefaultsChangeProvider m_DefaultsChangeProvider = new Settings.DefaultsChangeProvider();
	}
}
