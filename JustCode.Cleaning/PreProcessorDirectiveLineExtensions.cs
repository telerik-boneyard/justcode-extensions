namespace JustCode.Cleaning
{
	using System;
	using Telerik.JustCode.CommonLanguageModel;

	public static class PreProcessorDirectiveLineExtensions
	{
		public static bool IsRegion(this IPreProcessorDirectiveLine line)
		{
			return line.ExistsTextuallyInFile &&
				   (line.Text.ToLower().Trim().StartsWith("#region ") ||
					line.Text.ToLower().Trim().StartsWith("#endregion"));
		}

		public static void Delete(this IPreProcessorDirectiveLine line)
		{
			line.FileModel.ReplaceTextually(line.TextualCodeSpan, String.Empty);
		}
	}
}