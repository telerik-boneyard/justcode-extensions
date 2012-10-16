using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Telerik.JustCode.CommonLanguageModel;

namespace JustCode.Refactorings
{
	[Export(typeof(IEngineModule))]
	[Export(typeof(ICommandDefinition))]
	public class CallOnPropertyChangedFromSetterCommand : CommandModuleBase
	{
		private const string NotifyPropertyChangedQualifiedTypeName = "System.ComponentModel.INotifyPropertyChanged";

		public override string CommandIdentifier
		{
			get
			{
				return "CallOnPropertyChangedFromSetterCommand";
			}
		}

		public override string Text
		{
			get
			{
				return "Call OnPropertyChanged from setter";
			}
		}

		public override IEnumerable<CommandMenuLocation> MenuLocations
		{
			get
			{
				return PlaceInRefactorMenus(100, 450);
			}
		}

		public override bool ShouldShowVisualAidTag(SolutionModel solutionModel, SelectionContext context)
		{
			return true;
		}

		public override bool CanExecute(SolutionModel solutionModel, SelectionContext context)
		{
			FileModel fileModel;
			CodeSpan selection;

			if (!solutionModel.IsEditorSelection(context, out fileModel, out selection))
			{
				return false;
			}

			IPropertyDeclaration propertyDeclaration = fileModel.InnerMost<IPropertyDeclaration>(selection);

			if (IsFieldBackedPropertyWithSetterInsideClass(propertyDeclaration))
			{
				return EnclosingClassImplementsINotifyPropertyChanged(propertyDeclaration);
			}

			return false;
		}

		public override void Execute(SolutionModel solutionModel, SelectionContext context)
		{
			FileModel fileModel;
			CodeSpan selection;

			if (!solutionModel.IsEditorSelection(context, out fileModel, out selection))
			{
				return;
			}

			IPropertyDeclaration propertyDeclaration = fileModel.InnerMost<IPropertyDeclaration>(selection);

			if (IsFieldBackedPropertyWithSetterInsideClass(propertyDeclaration) && EnclosingClassImplementsINotifyPropertyChanged(propertyDeclaration))
			{
				IConstructLanguage language = propertyDeclaration.Language;
				string methodInvocationName = language.Name == LanguageNames.CSharp ? "OnPropertyChanged" : "RaisePropertyChangedEvent";
				IMethodInvocation methodInvocation =
					language.MethodInvocation(
						language.None<IExpression>(),
						language.Identifier(methodInvocationName),
						language.None<ITypeArguments>(),
						language.Arguments(
							language.Argument(language.StringLiteral(propertyDeclaration.Identifier.Name))));

				IAccessor setter = propertyDeclaration.Setter();
				List<IStatement> ifBlockStatements = new List<IStatement>(setter.Block.ChildStatements);
				ifBlockStatements.Add(language.ExpressionStatement(methodInvocation));

				IIfStatement ifStatement =
					language.IfStatement(
						language.BinaryExpression(
							language.MemberAccess(language.None<IExpression>(),
							propertyDeclaration.BackingField().Identifier),
						Operator.NotEqual,
						language.Expression("value")),
						language.Block(ifBlockStatements));

				IBlock newBlock = language.Block(ifStatement);
				setter.Block = newBlock;
			}
		}

		private bool EnclosingClassImplementsINotifyPropertyChanged(IPropertyDeclaration propertyDeclaration)
		{
			return propertyDeclaration.EnclosingClass
									  .Type()
									  .AllSuperTypesIncludingThis
									  .Any(t => t.Is(NotifyPropertyChangedQualifiedTypeName));
		}

		private bool IsFieldBackedPropertyWithSetterInsideClass(IPropertyDeclaration propertyDeclaration)
		{
			return propertyDeclaration.ExistsTextuallyInFile && propertyDeclaration.HasSetter() &&
				   propertyDeclaration.IsFieldBacked() && propertyDeclaration.EnclosingClass.IsClass;
		}
	}
}