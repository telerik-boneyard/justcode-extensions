using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Telerik.JustCode.CommonLanguageModel;
using Telerik.JustCode.CommonLanguageModel.Extensions;

namespace JustCode.Refactorings
{
	[Export(typeof(IEngineModule))]
	[Export(typeof(ICommandDefinition))]
	public class CreatePropertyCallingOnPropertyChangedCommand : CommandModuleBase
	{
		private const string PropertyChangedQualifiedTypeName = "System.ComponentModel.INotifyPropertyChanged";

		public override string CommandIdentifier
		{
			get
			{
				return "CreatePropertyCallingOnPropertyChangedRefactoring";
			}
		}

		public override string Text
		{
			get
			{
				return "Create property calling OnPropertyChanged";
			}
		}

		public override IEnumerable<CommandMenuLocation> MenuLocations
		{
			get
			{
				return PlaceInRefactorMenus(100, 200);
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

			IConstructEnumerable<IFieldDeclaration> fields = FindFields(fileModel, selection);
			return fields.Exist() && ImplementsINotifyPropertyChanged(fields.First().EnclosingClass);
		}

		public override void Execute(SolutionModel solutionModel, SelectionContext context)
		{
			FileModel fileModel;
			CodeSpan selection;

			if (!solutionModel.IsEditorSelection(context, out fileModel, out selection))
			{
				return;
			}

			IConstructEnumerable<IFieldDeclaration> fields = FindFields(fileModel, selection);
			if (fields.Exist())
			{
				IConstructLanguage language = fields.Language;

				foreach (IFieldDeclaration field in fields)
				{
					IPropertyDeclaration property = language.Property(
						language.None<IDocComment>(),
						language.None<IAttributes>(),
						language.Modifiers(Modifiers.Public),
						language.TypeName(field.TypeName.Type),
						language.None<IIdentifier>());

					NamingPolicy propertyNamingPolicy = property.PrimaryNamingPolicy(fileModel.UserSettings);
					string propertyName = propertyNamingPolicy.MakeMemberNameUniqueInScope(field, field.Identifier.Name);

					property.Identifier = language.Identifier(propertyName);

					IAccessor getter = language.FieldGetter(field.Identifier);
					IAccessor setter = CreateSetter(language, propertyName, field);

					property.Accessors = language.Enumerable(new List<IAccessor>() { getter, setter });

					field.EnclosingClass.Insert(property);
				}
			}
		}

		private IAccessor CreateSetter(IConstructLanguage language, string propertyName, IFieldDeclaration field)
		{
			string propertyChangedName = language.Name == LanguageNames.CSharp ? "OnPropertyChanged" : "RaisePropertyChangedEvent";

			IMethodInvocation onPropertyChanged = language.MethodInvocation(language.None<IExpression>(),
				language.Identifier(propertyChangedName),
				language.None<ITypeArguments>(),
				language.Arguments(
					language.Argument(language.StringLiteral(propertyName))));

			IMemberAccess fieldUsage = language.MemberAccess(language.None<IExpression>(), field.Identifier);
			IExpression valueUsage = language.Expression("value");

			IStatement assignment = language.AssignmentStatement(fieldUsage, valueUsage);

			IIfStatement ifStatement =
				language.IfStatement(
					language.BinaryExpression(fieldUsage,
						Operator.NotEqual,
						valueUsage),
					language.Block(assignment, language.ExpressionStatement(onPropertyChanged)));

			IAccessor setter = language.Setter(language.Block(ifStatement));
			return setter;
		}

		private bool ImplementsINotifyPropertyChanged(IClassDeclaration classDeclaration)
		{
			return classDeclaration.Type().AllSuperTypesIncludingThis.Any(t => t.Is(PropertyChangedQualifiedTypeName));
		}

		private IConstructEnumerable<IFieldDeclaration> FindFields(FileModel fileModel, CodeSpan selection)
		{
			return from declaration in fileModel.InnerMost<IClassDeclaration>(selection).ContainedDeclarations
				   where declaration.Is<IFieldDeclaration>() &&
				   declaration.ExistsTextuallyInFile &&
				   !declaration.As<IFieldDeclaration>().IsReadOnly() &&
				   declaration.CodeSpan.Intersects(selection)
				   select declaration.As<IFieldDeclaration>();
		}
	}
}