namespace JustCode.Analyzers
{
	using System.Collections.Generic;
	using System.ComponentModel.Composition;
	using Telerik.JustCode.CommonLanguageModel;

	[Export(typeof(IEngineModule))]
	[Export(typeof(ICodeMarkerGroupDefinition))]
	public class UseStringEmpty : CodeMarkerProviderModuleBase
	{
		private const string Description = "Replace \"\" with string.Empty";
		private const string FixText = "Replace with string.Empty";
		private const string MarkerText = "Use string.Empty";
		private const string UseStringEmptyMarkerID = "UseStringEmptyMarker";

		public override IEnumerable<CodeMarkerGroup> CodeMarkerGroups
		{
			get
			{
				foreach (var language in new[] { LanguageNames.CSharp, LanguageNames.VisualBasic })
				{
					yield return CodeMarkerGroup.Define(
						language,
						UseStringEmptyMarkerID,
						CodeMarkerAppearance.Warning,
						Description,
						true,
						MarkerText,
						FixText);
				}
			}
		}

		protected override void AddCodeMarkers(FileModel fileModel)
		{
			fileModel.All<IStringLiteral>()
					 .Where(x => x.Content.Length == 0 && CanBeConvertToStringEmpty(x))
					 .ForEach(literal => literal.AddCodeMarker(UseStringEmptyMarkerID, this, ReplaceWithStringEmpty, literal));
		}

		private bool CanBeConvertToStringEmpty(IStringLiteral literal)
		{
			IFieldDeclaration field = literal.Enclosing<IFieldDeclaration>();
			if (field.Exists && field.Modifiers.Modifiers.IsConst())
			{
				return false;
			}

			IVariableDeclaration variable = literal.Enclosing<IVariableDeclaration>();
			if (variable.Exists && variable.IsConst)
			{
				return false;
			}

			IAttribute attributes = literal.Enclosing<IAttribute>();
			if (attributes.Exists)
			{
				return false;
			}

			return true;
		}

		private void ReplaceWithStringEmpty(IStringLiteral literal)
		{
			IMemberAccess isEmpty = literal.Language.MemberAccess(literal.Language.TypeName(literal.Type),
				literal.Language.None<IExpression>(),
				literal.Language.Identifier("Empty"));

			literal.ReplaceWith(isEmpty);
		}
	}
}