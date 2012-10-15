namespace JustCode.Analyzers
{
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using System.ComponentModel.Composition;
	using Telerik.JustCode.CommonLanguageModel;

	[Export(typeof(IEngineModule))]
	[Export(typeof(ICodeMarkerGroupDefinition))]
	public class ExcessiveLocalVariables : CodeMarkerProviderModuleBase
	{
		private readonly string[] languages = new string[] { LanguageNames.CSharp, LanguageNames.VisualBasic };
		private const string CodeMarkerId = "ExcessiveLocalVariablesMarker";
		private const string Description = "There are too many local variables in scope. This affects performance.";
		private const string MarkerText = "CA1809: Excessive Local Variables";
		private const string FixText = "Refactor this method until there are 64 or fewer local variables.";

		public override IEnumerable<CodeMarkerGroup> CodeMarkerGroups
		{
			get
			{
				return languages.Select(language => CodeMarkerGroup.Define(
					language,
					CodeMarkerId,
					CodeMarkerAppearance.Warning,
					Description,
					true,
					MarkerText,
					FixText));
			}
		}

		protected override void AddCodeMarkers(FileModel fileModel)
		{
			Dictionary<IMemberDeclaration, List<IVariableDeclaration>> variableDeclarations = new Dictionary<IMemberDeclaration, List<IVariableDeclaration>>();

			foreach (IVariableDeclaration variable in fileModel.All<IVariableDeclaration>().Where(v => v.ExistsTextuallyInFile))
			{
				IMemberDeclaration member = variable.EnclosingMember();
				if (member.ExistsTextuallyInFile)
				{
					if (!variableDeclarations.ContainsKey(member))
					{
						variableDeclarations[member] = new List<IVariableDeclaration>();
					}
					variableDeclarations[member].Add(variable);
				}
			}

			foreach (KeyValuePair<IMemberDeclaration, List<IVariableDeclaration>> pair in variableDeclarations)
			{
				if (pair.Value.Count > 64)
				{
					pair.Key.AddCodeMarker(CodeMarkerId, this);
				}
			}
		}
	}
}