namespace JustCode.Navigation
{
	using System.Collections.Generic;
	using System.ComponentModel.Composition;
	using Telerik.JustCode.CommonLanguageModel;
	using Telerik.JustCode.CommonLanguageModel.KeyBindings;

	[Export(typeof(IEngineModule))]
	[Export(typeof(ICommandDefinition))]
	public class GotoContainingDeclaration : CommandModuleBase
	{
		public override string CommandIdentifier
		{
			get
			{
				return "GotoContainingDeclaration";
			}
		}

		public override IEnumerable<KeyBinding> GetKeyBindings(KeyBindingProfile profile)
		{
			yield return new KeyBinding(KeyBindingScope.TextEditor, new KeyCombination(KeyboardModifierKeys.Ctrl, KeyboardKey.OpenBracket));
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
				return "Extension: Go to Containing Declaration";
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

			IMemberDeclaration memberDeclaration = fileModel.InnerMost<IMemberDeclaration>(selection);
			if (memberDeclaration.ExistsTextuallyInFile && !memberDeclaration.Identifier.CodeSpan.Intersects(selection))
			{
				return memberDeclaration.Is<IMethodDeclaration>() || memberDeclaration.Is<IPropertyDeclaration>() ||
					   memberDeclaration.Is<IConstructorDeclaration>() || memberDeclaration.Is<IStaticConstructorDeclaration>();
			}
			else
			{
				return fileModel.InnerMost<ITypeDeclaration>(selection).ExistsTextuallyInFile;
			}
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
			if (memberDeclaration.ExistsTextuallyInFile && !memberDeclaration.Identifier.CodeSpan.Intersects(selection))
			{
				memberDeclaration.Identifier.Select();
			}
			else
			{
				ITypeDeclaration typeDeclaration = fileModel.InnerMost<ITypeDeclaration>(selection);

				if (typeDeclaration.ExistsTextuallyInFile)
				{
					NavigateToTypeDeclaration(typeDeclaration, selection);
				}
			}
		}

		private void NavigateToTypeDeclaration(ITypeDeclaration typeDeclaration, CodeSpan selection)
		{
			if (typeDeclaration.Identifier.CodeSpan.Intersects(selection))
			{
				ITypeDeclaration enclosingType = typeDeclaration.Enclosing<ITypeDeclaration>();
				if (enclosingType.ExistsTextuallyInFile)
				{
					enclosingType.Identifier.Select();
				}
			}
			else
			{
				typeDeclaration.Identifier.Select();
			}
		}
	}
}