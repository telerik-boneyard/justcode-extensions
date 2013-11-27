namespace JustCode.Navigation
{
	using System.Collections.Generic;
	using System.ComponentModel.Composition;
	using Telerik.JustCode.CommonLanguageModel;
	using Telerik.JustCode.CommonLanguageModel.KeyBindings;

	[Export(typeof(IEngineModule))]
	[Export(typeof(ICommandDefinition))]
	public class GotoPreviousMember : CommandModuleBase
	{
		public override string CommandIdentifier
		{
			get
			{
				return "GotoPreviousMember";
			}
		}

		public override IEnumerable<KeyBinding> GetKeyBindings(KeyBindingProfile profile)
		{
			yield return new KeyBinding(KeyBindingScope.TextEditor, new KeyCombination(KeyboardModifierKeys.Alt, KeyboardKey.UpArrow));
		}
 		
		public override IEnumerable<CommandMenuLocation> MenuLocations
		{
			get
			{
				return PlaceInNavigateMenus(100, 1);
			}
		}

		public override string Text
		{
			get
			{
				return "Extension: Go to previous member declaration";
			}
		}

		public override bool CanExecute(SolutionModel solutionModel, SelectionContext context)
		{
			FileModel fileModel;
			CodeSpan selection;

			if (!solutionModel.IsEditorSelection(context, out fileModel, out selection))
			{
				return false;
			}

			IMemberDeclaration member = fileModel.InnerMost<IMemberDeclaration>(selection);
			return member.ExistsTextuallyInFile && member.Identifier.CodeSpan.Intersects(selection);
		}

		public override void Execute(SolutionModel solutionModel, SelectionContext context)
		{
			FileModel fileModel;
			CodeSpan selection;

			if (!solutionModel.IsEditorSelection(context, out fileModel, out selection))
			{
				return;
			}

			IMemberDeclaration memberDeclaration = fileModel.InnerMost<IMemberDeclaration>(selection);
			if (memberDeclaration.ExistsTextuallyInFile)
			{
				IMemberDeclaration previousMember = memberDeclaration.PreviousMember();
				if (previousMember.ExistsTextuallyInFile)
				{
					previousMember.Identifier.NavigateTo();
				}
			}
		}
	}
}