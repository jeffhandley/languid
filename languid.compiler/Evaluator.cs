using Languid.Compiler.CodeAnalysis;

namespace Languid.Compiler
{
    class Evaluator
    {
        private readonly ExpressionSyntax _root;

        public Evaluator(ExpressionSyntax root)
        {
            _root = root;
        }

        public int Evaluate()
        {
            return EvaluateExpressionInt32(_root);
        }

        private int EvaluateExpressionInt32(ExpressionSyntax node)
        {
            if (node is NumberExpressionSyntax n)
            {
                if (n.NumberToken.Value is null)
                {
                    throw new InvalidOperationException($"Unexpected null value for NumericExpressionSyntax: \"{n.NumberToken.Text}\"");
                }

                return (int)n.NumberToken.Value;
            }

            if (node is BinaryExpressionSyntax b)
            {
                var left = EvaluateExpressionInt32(b.Left);
                var right = EvaluateExpressionInt32(b.Right);

                return b.OperatorToken.Kind switch
                {
                    SyntaxKind.PlusToken => left + right,
                    SyntaxKind.MinusToken => left - right,
                    SyntaxKind.StarToken => left * right,
                    SyntaxKind.SlashToken => left / right,
                    _ => throw new InvalidOperationException($"Unexpected binary operator: <{b.OperatorToken.Kind}>"),
                };
            }

            if (node is GroupedExpressionSyntax g)
            {
                return EvaluateExpressionInt32(g.Expression);
            }

            throw new InvalidOperationException($"Unexpected expression: <{node.Kind}>");
        }
    }
}
