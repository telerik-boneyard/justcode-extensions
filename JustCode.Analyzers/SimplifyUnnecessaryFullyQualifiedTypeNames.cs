namespace JustCode.Analyzers
{
	using System.Collections.Generic;
	using System.ComponentModel.Composition;
	using System.Linq;
	using Telerik.JustCode.CommonLanguageModel;

	[Export(typeof(IEngineModule))]
	[Export(typeof(ICodeMarkerGroupDefinition))]
	public class SimplifyUnnecessaryFullyQualifiedTypeNames : CodeMarkerProviderModuleBase
	{
		private const string Description = "Convert unnecessary fully qualified type names to simple type names";
		private const string FixText = "Use {0}";
		private const string MarkerText = "There is already a using for the namespace";
		private const string RemoveFullyQuallifiedTypesMarkerID = "RemoveFullyQualifiedTypesMarker";

		public override IEnumerable<CodeMarkerGroup> CodeMarkerGroups
		{
			get
			{
				foreach (var language in new[] { LanguageNames.CSharp, LanguageNames.VisualBasic })
				{
					yield return CodeMarkerGroup.Define(
						language,
						RemoveFullyQuallifiedTypesMarkerID,
						CodeMarkerAppearance.DeadCodeWarning,
						Description,
						true,
						MarkerText,
						FixText);
				}
			}
		}

		protected override void AddCodeMarkers(FileModel fileModel)
		{
			fileModel.All<IQualifiedTypeName>()
					 .Where(q => q.IsNamespaceInScope(q.NamespaceName.Namespace))
					 .ForEach(q => q.NamespaceName.AddCodeMarker(RemoveFullyQuallifiedTypesMarkerID, this, ConvertToSimpleTypeName, q, q.Type.Name));
		}

		private void ConvertToSimpleTypeName(IQualifiedTypeName qualifiedTypeName)
		{
			qualifiedTypeName.ReplaceWith(qualifiedTypeName.Language.TypeName(qualifiedTypeName.Type));
		}
	}
}