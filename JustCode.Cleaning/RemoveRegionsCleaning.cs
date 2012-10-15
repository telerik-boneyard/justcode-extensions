namespace JustCode.Cleaning
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel.Composition;
	using System.Linq;
	using Telerik.JustCode.CommonLanguageModel;

	[Export(typeof(IEngineModule))]
	[Export(typeof(ICodeCleaningStepDefinition))]
	public class RemoveRegionsCleaning : CodeCleaningStepProviderModuleBase
	{
		private const string MarkerId = "RemoveRegions";
		private const string CleanOptionText = "Remove Regions";
		private const int Order = 100;

		public override IEnumerable<CodeCleaningStep> CodeCleaningSteps
		{
			get
			{
				yield return new CodeCleaningStep(LanguageNames.CSharp, Order, CleanOptionText, MarkerId);
				yield return new CodeCleaningStep(LanguageNames.VisualBasic, Order, CleanOptionText, MarkerId);
			}
		}

		public override void ExecuteCodeCleaningStep(CodeCleaningStep step, FileModel fileModel, CodeSpan span)
		{
			fileModel.All<IPreProcessorDirectiveLine>()
					 .Where(line => line.IsRegion())
					 .ForEach(region => region.Delete());
		}
	}
}