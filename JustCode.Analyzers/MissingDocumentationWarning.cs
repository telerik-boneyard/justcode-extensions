using System.Collections.Generic;
using System.ComponentModel.Composition;
using Telerik.JustCode.CommonLanguageModel;
using Telerik.JustCode.CommonLanguageModel.Extensions;

namespace JustCode.Analyzers
{
	[Export(typeof(IEngineModule))]
	[Export(typeof(ICodeMarkerGroupDefinition))]
	public class MissingDocumentationWarning : CodeMarkerProviderModuleBase
	{
		[Import]
		public IDocCommentFactory DocCommentFactory { get; set; }

		private const string WarningID = "MissingDocumentationWarningWarningMarker";
		private const string MarkerText = "This public member does not have a documentation";
		private const string Description = "Public members with no documentation";
		private const string FixText = "Generate documentation";

		protected override void AddCodeMarkers(FileModel fileModel)
		{
			foreach (var memberDeclaration in fileModel.All<IMemberDeclaration>().Where(m => m.ExistsTextuallyInFile && m.IsPublic() && AreAllEnclosingClassesPublic(m)))
			{
				if (!memberDeclaration.DocComment.Exists)
				{
					memberDeclaration.Identifier.AddCodeMarker(WarningID, this, GenerateDocumentation, memberDeclaration);
				}
			}
		}

		public override IEnumerable<CodeMarkerGroup> CodeMarkerGroups
		{
			get
			{
				foreach (var language in new[] { LanguageNames.CSharp, LanguageNames.VisualBasic })
				{
					yield return CodeMarkerGroup.Define(
						language,
						WarningID,
						CodeMarkerAppearance.Warning,
						Description,
						true,
						MarkerText,
						FixText);
				}
			}
		}

		private void GenerateDocumentation(IMemberDeclaration memberDeclaration)
		{
			DocCommentInfo documentation = DocCommentFactory.GenerateDocCommentInfo(memberDeclaration);
			memberDeclaration.DocComment = memberDeclaration.Language.DocComment(documentation.ToString());
		}

		private bool AreAllEnclosingClassesPublic(IMemberDeclaration memberDeclaration)
		{
			IClassDeclaration enclosingTypeDeclaration = memberDeclaration.EnclosingClass;
			bool isEnclosingClassPublic = enclosingTypeDeclaration.IsPublic();
			while (isEnclosingClassPublic && enclosingTypeDeclaration.EnclosingClass.Exists)
			{
				enclosingTypeDeclaration = enclosingTypeDeclaration.EnclosingClass;
				isEnclosingClassPublic = enclosingTypeDeclaration.IsPublic();
			}
			return isEnclosingClassPublic;
		}
	}
}