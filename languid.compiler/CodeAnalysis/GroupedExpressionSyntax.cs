namespace Languid.Compiler.CodeAnalysis
{
    sealed class GroupedExpressionSyntax : ExpressionSyntax
    {
        public GroupedExpressionSyntax(SyntaxToken openToken, ExpressionSyntax expression, SyntaxToken closeToken)
        {
            GroupOpenToken = openToken;
            Expression = expression;
            GroupCloseToken = closeToken;
        }

        public override SyntaxKind Kind => SyntaxKind.GroupedExpression;
        public SyntaxToken GroupOpenToken { get; }
        public ExpressionSyntax Expression { get; }
        public SyntaxToken GroupCloseToken { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return GroupOpenToken;
            yield return Expression;
            yield return GroupCloseToken;
        }
    }
}
