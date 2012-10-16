using System;
using System.Collections.Generic;
using Telerik.JustCode.CommonLanguageModel;

namespace JustCode.Refactorings
{
	internal class CreateEventInvocatorHelper
	{
		public static IMethodDeclaration CreateEventInvocator(FileModel fileModel, IEventDeclaration eventDeclaration)
		{
			IDelegateDeclaration delegateDeclaration = eventDeclaration.TypeName.Type.As<IDelegateType>().DelegateDeclaration;
			if (delegateDeclaration.Exists)
			{
				var language = eventDeclaration.Language;
				IModifiers modifiers = language.Modifiers(Modifiers.Protected | Modifiers.Virtual);
				IList<IParameter> parameters = new List<IParameter>();
				IList<IArgument> arguments = new List<IArgument>();

				arguments.Add(language.Argument(language.New<IThis>()));
				IParameter lastParameter = delegateDeclaration.Parameters.Parameters.Last();
				GenerateSecondParameterAndArgument(eventDeclaration, lastParameter, language, fileModel, arguments, parameters);

				if (string.Equals(language.Name, LanguageNames.CSharp, StringComparison.Ordinal))
				{
					return CreateCSInvocator(eventDeclaration, modifiers, arguments, parameters, language);
				}
				else if (string.Equals(language.Name, LanguageNames.VisualBasic, StringComparison.Ordinal))
				{
					return CreateVBInvocator(eventDeclaration, modifiers, arguments, parameters, language);
				}
			}

			return eventDeclaration.Language.None<IMethodDeclaration>();
		}
  
		private static void GenerateSecondParameterAndArgument(IEventDeclaration eventDeclaration, IParameter parameter, IConstructLanguage language, FileModel fileModel, IList<IArgument> arguments, IList<IParameter> parameters)
		{
			NamingPolicy parametersNamingPolicy = language.PrimaryNamingPolicyFor<IParameter>(fileModel.UserSettings);
			IType parameterType = eventDeclaration.TypeName.Type.As<IDelegateType>().ReplaceParameterTypesIn(parameter.Type);

			IParameter newParameter = language.Parameter(
				parameter.IsRef,
				parameter.IsOut,
				parameter.IsParams,
				language.TypeName(eventDeclaration.StringTypeAtThisLocation()),
				parameter.DefaultValue);

			string parameterName = parametersNamingPolicy.ChangeNameAccordingToPolicy("propertyName", parameter.SolutionModel);
			newParameter.Identifier = language.Identifier(parameterName);

			IObjectCreation objectCreation = language.ObjectCreation(
				language.TypeName(parameterType), language.Arguments(
					language.Argument(
						language.VariableAccess(newParameter.Identifier))));

			arguments.Add(
				language.Argument(
					objectCreation));

			parameters.Add(newParameter);
		}

		private static IMethodDeclaration CreateVBInvocator(IEventDeclaration eventDeclaration, IModifiers modifiers,
			IList<IArgument> arguments, IList<IParameter> parameters, IConstructLanguage language)
		{
			IEventInvocation eventInvocation = language.New<IEventInvocation>();
			eventInvocation.DelegateInvocation =
				language.DelegateInvocation(
					language.VariableAccess(eventDeclaration.Identifier),
					language.Arguments(arguments));

			IMethodDeclaration method = language.Method(
					language.None<IDocComment>(),
					language.None<IAttributes>(),
					modifiers,
					language.TypeName(eventDeclaration.VoidTypeAtThisLocation()),
					language.None<IMethodTypeParameters>(),
					language.Parameters(parameters),
					language.Block(
						language.ExpressionStatement(eventInvocation)));

			NamingPolicy methodsNamingPolicy = method.PrimaryNamingPolicy(eventDeclaration.FileModel.UserSettings);
			string methodName = methodsNamingPolicy.ChangeNameAccordingToPolicy("Raise" + eventDeclaration.Identifier.Name + "Event",
				eventDeclaration.SolutionModel);

			method.Identifier = language.Identifier(methodName);

			return method;
		}

		private static IMethodDeclaration CreateCSInvocator(IEventDeclaration eventDeclaration, IModifiers modifiers, IList<IArgument> arguments,
			IList<IParameter> parameters, IConstructLanguage language)
		{
			IVariableDeclaration variable = language.Variable(
				eventDeclaration.TypeName,
				language.VariableAccess(eventDeclaration.Identifier));

			NamingPolicy variablesNamingPolicy = variable.PrimaryNamingPolicy(eventDeclaration.FileModel.UserSettings);
			string variableName = variablesNamingPolicy.ChangeNameAccordingToPolicy("on" + eventDeclaration.Identifier.Name,
				eventDeclaration.SolutionModel);

			variable.Identifier = language.Identifier(variableName);

			IIfStatement ifStatement = language.IfStatement(
				language.BinaryExpression(
					language.VariableAccess(variable.Identifier),
					Operator.NotEqual,
					language.New<INull>()),
				language.Block(
					language.ExpressionStatement(
						language.DelegateInvocation(
							language.VariableAccess(variable.Identifier),
							language.Arguments(arguments)))));

			IMethodDeclaration method = language.Method(
				language.None<IDocComment>(),
				language.None<IAttributes>(),
				modifiers,
				language.TypeName(eventDeclaration.VoidTypeAtThisLocation()),
				language.None<IMethodTypeParameters>(),
				language.Parameters(parameters),
				language.Block(
					variable,
					ifStatement));

			NamingPolicy methodsNamingPolicy = method.PrimaryNamingPolicy(eventDeclaration.FileModel.UserSettings);
			string methodName = methodsNamingPolicy.ChangeNameAccordingToPolicy("on" + eventDeclaration.Identifier.ToUpperFirstLetter().Name,
				eventDeclaration.SolutionModel);

			method.Identifier = language.Identifier(methodName);

			return method;
		}
	}
}
