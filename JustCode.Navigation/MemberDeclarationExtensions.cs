namespace JustCode.Navigation
{
	using Telerik.JustCode.CommonLanguageModel;

	public static class MemberDeclarationExtensions
	{
		public static IMemberDeclaration NextMember(this IMemberDeclaration declaration)
		{
			return declaration.EnclosingClass.ContainedMembers().Where(x => x.IdentifierCodeSpan().StartLocation > declaration.IdentifierCodeSpan().StartLocation).First();
		}

		public static IMemberDeclaration PreviousMember(this IMemberDeclaration declaration)
		{
			return declaration.EnclosingClass.ContainedMembers().Where(x => x.CodeSpan.StartLocation < declaration.CodeSpan.StartLocation).Last();
		}
	}
}