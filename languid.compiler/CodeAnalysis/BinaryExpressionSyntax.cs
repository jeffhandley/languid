namespace Languid.Compiler.CodeAnalysis
{
    sealed class BinaryExpressionSyntax : ExpressionSyntax
    {
        private readonly bool _isFactor;

        public BinaryExpressionSyntax(ExpressionSyntax left, SyntaxToken operatorToken, ExpressionSyntax right, bool isFactor = false)
        {
            Left = left;
            OperatorToken = operatorToken;
            Right = right;
            _isFactor = isFactor;
        }

        public override SyntaxKind Kind => _isFactor ? SyntaxKind.BinaryFactorExpression : SyntaxKind.BinaryTermExpression;
        public ExpressionSyntax Left { get; private init; }
        public SyntaxToken OperatorToken { get; private init; }
        public ExpressionSyntax Right { get; private init; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Left;
            yield return OperatorToken;
            yield return Right;
        }
    }

}
