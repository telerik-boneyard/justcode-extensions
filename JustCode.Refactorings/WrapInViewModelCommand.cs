using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Telerik.JustCode.CommonLanguageModel;
using Telerik.JustCode.CommonLanguageModel.Extensions;

namespace JustCode.Refactorings
{
	[Export(typeof(IEngineModule))]
	[Export(typeof(ICommandDefinition))]
	public class WrapInViewModelCommand : CommandModuleBase
	{
		private const string PropertyChangedSimpleTypeName = "INotifyPropertyChanged";
		private bool propertiesSelectedByUser;

		public override string CommandIdentifier
		{
			get
			{
				return "WrapInViewModelCommand";
			}
		}

		public override string Text
		{
			get
			{
				return "Wrap in ViewModel";
			}
		}

		public override IEnumerable<CommandMenuLocation> MenuLocations
		{
			get
			{
				return PlaceInRefactorMenus(100, 300);
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

			IIdentifier identifier = fileModel.InnerMost<IIdentifier>(selection);
			IClassDeclaration classDeclaration = identifier.ParentConstruct.As<IClassDeclaration>();
			return classDeclaration.ExistsTextuallyInFile && classDeclaration.IsInUserCode() && !classDeclaration.IsPrivate();
		}

		public override void Execute(SolutionModel solutionModel, SelectionContext context)
		{
			FileModel fileModel;
			CodeSpan selection;

			if (!solutionModel.IsEditorSelection(context, out fileModel, out selection))
			{
				return;
			}

			propertiesSelectedByUser = false;

			IIdentifier identifier = fileModel.InnerMost<IIdentifier>(selection);
			IClassDeclaration classDeclaration = identifier.ParentConstruct.As<IClassDeclaration>();
			if (classDeclaration.ExistsTextuallyInFile && classDeclaration.IsInUserCode() && !classDeclaration.IsPrivate())
			{
				IConstructEnumerable<IMemberDeclaration> propertiesForWrapping = GetPropertiesForWrapping(classDeclaration);
				if (!propertiesForWrapping.Any())
				{
					CreateViewModelWithoutUserSelectedProperties(classDeclaration).NavigateTo();
				}
				else
				{
					ConfirmOccurencesDialog confirmOccurencesDialog = fileModel.UIProcess.Get<ConfirmOccurencesDialog>();
					confirmOccurencesDialog.ShowIfOccurencesToConfirmIn("Select properties for wrapping", "Select which properties to be wrapped",
						() => ConfirmPropertyDeclarationsToBeWrappedAndContinueWrapping(classDeclaration, propertiesForWrapping));
				}
			}
		}

		private void ConfirmPropertyDeclarationsToBeWrappedAndContinueWrapping(IClassDeclaration classDeclaration, IConstructEnumerable<IMemberDeclaration> propertiesForWrapping)
		{
			List<string> confirmedPropertiesNamesForWrapping = new List<string>();

			foreach (IMemberDeclaration propertyDeclaration in propertiesForWrapping)
			{
				if (classDeclaration.FileModel.UIProcess.Get<ConfirmOccurencesDialog>().Confirm(propertyDeclaration, true))
				{
					confirmedPropertiesNamesForWrapping.Add(propertyDeclaration.Identifier.Name);
				}
			}

			if (propertiesSelectedByUser)
			{
				string classDeclarationFullName = classDeclaration.FullName;
				IClassDeclaration viewModel = CreateViewModelWithoutUserSelectedProperties(classDeclaration);
				viewModel = RebuildSolutionModel(classDeclaration, viewModel.Identifier.Name);
				IFieldDeclaration wrappedField = viewModel.ContainedDeclarations.First(d => d.Is<IFieldDeclaration>()).As<IFieldDeclaration>();
				IMethodDeclaration onPropertyChangedMethod = viewModel.ContainedDeclarations.First(d => d.Is<IMethodDeclaration>()).As<IMethodDeclaration>();
				classDeclaration = viewModel.FileModel.All<IClassDeclaration>().First(c => string.Equals(c.FullName, classDeclarationFullName, System.StringComparison.Ordinal));

				List<IPropertyDeclaration> confirmedPropertiesForWrapping = GetPropertiesForWrappingByName(classDeclaration, confirmedPropertiesNamesForWrapping);

				InsertWrappedProperties(viewModel, confirmedPropertiesForWrapping, wrappedField, onPropertyChangedMethod.Identifier);
				viewModel.NavigateTo();
			}
			else
			{
				propertiesSelectedByUser = true;
			}
		}

		private List<IPropertyDeclaration> GetPropertiesForWrappingByName(IClassDeclaration classDeclaration, List<string> confirmedPropertiesNamesForWrapping)
		{
			List<IPropertyDeclaration> properties = new List<IPropertyDeclaration>();
			IConstructEnumerable<IDeclaration> containedDeclarations = classDeclaration.ContainedDeclarations;

			foreach (string propertyName in confirmedPropertiesNamesForWrapping)
			{
				IPropertyDeclaration property = containedDeclarations.First(d => string.Equals(propertyName, d.Identifier.Name, StringComparison.Ordinal))
					.As<IPropertyDeclaration>();
				if (property.Exists)
				{
					properties.Add(property);
				}
			}

			return properties;
		}

		private IConstructEnumerable<IMemberDeclaration> GetPropertiesForWrapping(IClassDeclaration classDeclaration)
		{
			return classDeclaration.ContainedMembers().Where(m => m.Is<IPropertyDeclaration>() && IsPropertyVisibleOutsideClass(m.Modifiers));
		}

		private IClassDeclaration CreateViewModelWithoutUserSelectedProperties(IClassDeclaration classDeclaration)
		{
			IClassDeclaration viewModel = CreateViewModel(classDeclaration);
			viewModel = ImplementINotifyPropertyChangedInterface(viewModel, classDeclaration);
			viewModel.Insert(CreateOnPropertyChanged(viewModel));

			return viewModel;
		}

		private IClassDeclaration CreateViewModel(IClassDeclaration classDeclaration)
		{
			ITypeName typeName = classDeclaration.Language.TypeName(classDeclaration.Type());
			IFieldDeclaration fieldDeclaration = CreateFieldToHoldWrappedClass(classDeclaration.Language, typeName, classDeclaration);
			IConstructorDeclaration viewModelConstructor = CreateConstructor(classDeclaration, fieldDeclaration.Identifier, typeName);

			IClassDeclaration viewModel = CreateInitialViewModel(classDeclaration, fieldDeclaration, viewModelConstructor);
			return viewModel;
		}

		private IClassDeclaration CreateInitialViewModel(IClassDeclaration classDeclaration, IFieldDeclaration fieldDeclaration, IConstructorDeclaration viewModelConstructor)
		{
			IConstructLanguage language = classDeclaration.Language;
			IClassDeclaration viewModel = language.Class(
				language.Modifiers(Modifiers.Public),
				language.None<IClassTypeParameters>(),
				language.None<ITypeName>(),
				new List<ITypeName>(),
				new List<IDeclaration>() { fieldDeclaration, viewModelConstructor });

			NamingPolicy classesPolicy = viewModel.PrimaryNamingPolicy(classDeclaration.FileModel.UserSettings);
			string viewModelName = classesPolicy.MakeTypeNameUniqueInNamespace(classDeclaration, classDeclaration.Identifier.Name + "ViewModel");

			viewModel.Identifier = language.Identifier(viewModelName);
			return viewModel;
		}

		private void InsertWrappedProperties(IClassDeclaration viewModel, List<IPropertyDeclaration> confirmedPropertiesForWrapping, IFieldDeclaration wrappedClassField, IIdentifier onPropertyChangedIdentifier)
		{
			IConstructLanguage language = viewModel.Language;

			foreach (IPropertyDeclaration property in confirmedPropertiesForWrapping)
			{
				IMemberAccess propertyMemberAccess = language.MemberAccess(
					language.MemberAccess(language.None<IExpression>(), wrappedClassField.Identifier),
					property.Identifier);

				IAccessor getterOfWrapper = language.None<IAccessor>();
				IAccessor propertyGetter = property.Getter();

				if (propertyGetter.Exists && IsAccessorVisibleOutsideClass(propertyGetter.Modifiers))
				{
					getterOfWrapper = language.Getter(
						language.Modifiers(propertyGetter.Modifiers.Modifiers),
						language.Block(
							language.ReturnStatement(propertyMemberAccess)));
				}

				IAccessor setterOfWrapper = language.None<IAccessor>();
				IAccessor propertySetter = property.Setter();

				if (propertySetter.Exists && IsAccessorVisibleOutsideClass(propertySetter.Modifiers))
				{
					IStatement assignment = language.AssignmentStatement(propertyMemberAccess, language.Expression("value"));

					IMethodInvocation onPropertyChangedInvocation =
						language.MethodInvocation(
							language.None<IExpression>(),
							onPropertyChangedIdentifier,
							language.None<ITypeArguments>(),
							language.Arguments(
								language.Argument(language.StringLiteral(property.Identifier.Name))));

					IIfStatement ifStatement =
						language.IfStatement(
							language.BinaryExpression(propertyMemberAccess,
								Operator.NotEqual,
								language.Expression("value")),
							language.Block(assignment, language.ExpressionStatement(onPropertyChangedInvocation)));

					setterOfWrapper = language.Setter(
						language.Modifiers(propertyGetter.Modifiers.Modifiers),
						language.Block(ifStatement));
				}

				if (getterOfWrapper.Exists || setterOfWrapper.Exists)
				{
					IPropertyDeclaration wrapperProperty = language.Property(
															language.None<IDocComment>(),
															language.None<IAttributes>(),
															property.Modifiers,
															property.TypeName,
															property.Identifier,
															getterOfWrapper,
															setterOfWrapper);
					viewModel.Insert(wrapperProperty);
				}
			}
		}

		private bool IsPropertyVisibleOutsideClass(IModifiers modifiers)
		{
			return modifiers.Modifiers.IsPublic() || modifiers.Modifiers.IsInternal();
		}

		private bool IsAccessorVisibleOutsideClass(IModifiers modifiers)
		{
			return modifiers.Modifiers == Modifiers.None || modifiers.Modifiers.IsInternal();
		}

		private IConstructorDeclaration CreateConstructor(IClassDeclaration classDeclaration, IIdentifier fieldIdentifier, ITypeName typeName)
		{
			IConstructLanguage language = classDeclaration.Language;

			IParameter parameter = language.Parameter(typeName);
			NamingPolicy parameterNamingPolicy = parameter.PrimaryNamingPolicy(classDeclaration.FileModel.UserSettings);
			string parameterName = parameterNamingPolicy.ChangeNameAccordingToPolicy(classDeclaration.Identifier.Name, classDeclaration.Language, classDeclaration.FileModel);
			parameter.Identifier = language.Identifier(parameterName);
			Modifiers constructorModifiers = Modifiers.Public;
			Modifiers visibility = classDeclaration.Modifiers.Modifiers.GetVisibility();
			if (visibility == Modifiers.None || visibility.IsInternal())
			{
				constructorModifiers = Modifiers.Internal;
			}

			IConstructorDeclaration constructor = language.Constructor(language.None<IDocComment>(),
				language.None<IAttributes>(),
				language.Modifiers(constructorModifiers),
				language.Parameters(parameter),
				language.None<IConstructorInitializer>(),
				language.Block(
					language.AssignmentStatement(
						language.MemberAccess(language.This(), fieldIdentifier),
						language.VariableAccess(parameter.Identifier))));

			return constructor;
		}

		private IFieldDeclaration CreateFieldToHoldWrappedClass(IConstructLanguage language, ITypeName typeName, IClassDeclaration classDeclaration)
		{
			IFieldDeclaration fieldDeclaration = language.Field(language.Modifiers(Modifiers.Private | Modifiers.Readonly), typeName);
			NamingPolicy fieldsNamingPolicy = fieldDeclaration.PrimaryNamingPolicy(classDeclaration.FileModel.UserSettings);
			string fieldName = fieldsNamingPolicy.ChangeNameAccordingToPolicy(classDeclaration.Identifier.Name, classDeclaration.Language, classDeclaration.FileModel);
			fieldDeclaration.Identifier = language.Identifier(fieldName);
			return fieldDeclaration;
		}

		private IClassDeclaration ImplementINotifyPropertyChangedInterface(IClassDeclaration viewModel, IClassDeclaration wrappedClass)
		{
			UsingDirectiveHelper.AddUsingDirectiveIfNeeded(wrappedClass, "System", "ComponentModel");

			IConstructLanguage language = viewModel.Language;
			IIdentifier identifier = language.Identifier(PropertyChangedSimpleTypeName);
			ITypeName typeName = language.SimpleTypeName(identifier, language.None<ITypeArguments>());
			viewModel.IntroduceInterface(typeName);

			wrappedClass.Append(viewModel);
			string name = viewModel.Identifier.Name;
			viewModel = RebuildSolutionModel(wrappedClass, name);

			AddStubsHelper.CreateAllUnimplementedMembers(viewModel, new AddStubsOptions());

			return RebuildSolutionModel(wrappedClass, name);
		}

		private IMethodDeclaration CreateOnPropertyChanged(IClassDeclaration viewModel)
		{
			IEventDeclaration eventDeclaration = viewModel.ContainedDeclarations.First(d => d.Is<IEventDeclaration>()).As<IEventDeclaration>();
			IMethodDeclaration onPropertyChanged = CreateEventInvocatorHelper.CreateEventInvocator(viewModel.FileModel, eventDeclaration);
			return onPropertyChanged;
		}

		private IClassDeclaration RebuildSolutionModel(IClassDeclaration wrappedClass, string name)
		{
			wrappedClass.SolutionModel.RebuildWithCurrentModifications();
			IClassDeclaration viewModel = wrappedClass.FileModel.All<IClassDeclaration>().First(c => string.Equals(c.Identifier.Name, name, System.StringComparison.Ordinal));
			return viewModel;
		}
	}
}