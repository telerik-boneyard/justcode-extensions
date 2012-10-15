namespace JustCode.Analyzers
{
	using System.Collections.Generic;
	using System.ComponentModel.Composition;
	using Telerik.JustCode.CommonLanguageModel;

	[Export(typeof(IEngineModule))]
	[Export(typeof(ICodeMarkerGroupDefinition))]
	public class RemoveNewDelegateCreation : CodeMarkerProviderModuleBase
	{
		private const string Description = "Remove new delegate()";
		private const string FixText = "Remove new delegate()";
		private const string MarkerText = "Unnecessary use of new delegate()";
		private const string RemoveNewDelegateCreationMarkerID = "RemoveNewDelegateCreation";

		public override IEnumerable<CodeMarkerGroup> CodeMarkerGroups
		{
			get
			{
				foreach (var language in new[] { LanguageNames.CSharp, LanguageNames.VisualBasic })
				{
					yield return CodeMarkerGroup.Define(
						language,
						RemoveNewDelegateCreationMarkerID,
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
			foreach (IAddEventHandlerStatement addEventHandler in fileModel.All<IAddEventHandlerStatement>().Where(x => x.HandlerExpression.Is<IObjectCreation>()))
			{
				IObjectCreation objectCreation = addEventHandler.HandlerExpression.As<IObjectCreation>();
				objectCreation.TypeName.AddCodeMarker(RemoveNewDelegateCreationMarkerID, this, RemoveUnnecessaryObjectCreation, objectCreation);
			}
		}

		private void RemoveUnnecessaryObjectCreation(IObjectCreation objectCreation)
		{
			IExpression argument = objectCreation.Arguments.Arguments.First().Expression;
			objectCreation.ReplaceWith(argument);
		}
	}
}