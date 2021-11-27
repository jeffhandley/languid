namespace Languid.Compiler
{
    class Program
    {
        public static void Main(string[] _)
        {
            while (true)
            {
                Console.Write("> ");

                var line = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(line))
                    return;

                var parser = new Parser(line);
                var syntaxTree = parser.Parse();

                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkGray;
                PrettyPrint(syntaxTree.Root, "");

                if (!syntaxTree.Diagnostics.Any())
                {
                    var evaluator = new Evaluator(syntaxTree.Root);
                    var result = evaluator.Evaluate();
                    Console.WriteLine($"Result: {result}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;

                    foreach (var diag in syntaxTree.Diagnostics)
                    {
                        Console.WriteLine(diag);
                    }
                }

                Console.ForegroundColor = color;
            }
        }

        private static void PrettyPrint(SyntaxNode node, string indent = "", bool isLast = true)
        {
            var marker =  isLast ? "└─ " : "├─ ";

            Console.Write(indent);
            Console.Write(marker);
            Console.Write(node.Kind);

            if (node is SyntaxToken t && t.Value is not null)
            {
                Console.Write($" {t.Value}");
            }

            Console.WriteLine();

            indent += isLast ? "   " : "│  ";
            var lastChild = node.GetChildren().LastOrDefault();

            foreach (var child in node.GetChildren())
                PrettyPrint(child, indent, child == lastChild);
        }
    }

    enum SyntaxKind
    {
        BadToken,
        WhiteSpaceToken,
        NumberToken,
        PlusToken,
        MinusToken,
        StarToken,
        SlashToken,
        OpenParenToken,
        CloseParenToken,

        NumberExpression,
        BinaryExpression,

        EndOfFileToken = Int32.MaxValue
    }

    class SyntaxToken : SyntaxNode
    {
        public SyntaxToken(SyntaxKind kind, int position, string text, object value)
        {
            Kind = kind;
            Position = position;
            Text = text;
            Value = value;
        }

        public override SyntaxKind Kind { get; }
        public int Position { get; }
        public string Text { get; }
        public object Value { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            return Enumerable.Empty<SyntaxNode>();
        }
    }

    class Lexer
    {
        private readonly string _text;
        private int _position;
        private List<string> _diagnostics = new();

        public Lexer(string text)
        {
            _text = text;
        }

        public IEnumerable<string> Diagnostics => _diagnostics;

        private char Current
        {
            get
            {
                if (_position >= _text.Length)
                    return '\0';

                return _text[_position];
            }
        }

        private void Next()
        {
            _position++;
        }

        public SyntaxToken NextToken()
        {
            // <numbers>
            // + - * / ( )
            // <whitespace>

            if (_position >= _text.Length)
                return new SyntaxToken(SyntaxKind.EndOfFileToken, _position, "\0", null);

            if (char.IsDigit(Current))
            {
                var start = _position;

                do Next();
                while (char.IsDigit(Current));

                var length = _position - start;
                var text = _text.Substring(start, length);

                if (!int.TryParse(text, out var value))
                {
                    _diagnostics.Add($"The number {_text} cannot be represented by an Int32.");
                }

                return new SyntaxToken(SyntaxKind.NumberToken, start, text, value);
            }

            if (char.IsWhiteSpace(Current))
            {
                var start = _position;

                do Next();
                while (char.IsWhiteSpace(Current));

                var length = _position - start;
                var text = _text.Substring(start, length);

                return new SyntaxToken(SyntaxKind.WhiteSpaceToken, start, text, text);
            }

            Func<SyntaxKind, SyntaxToken> getSingleCharacterToken = (SyntaxKind kind) => {
                var token = new SyntaxToken(kind, _position, _text[_position].ToString(), _text[_position]);
                Next();

                return token;
            };

            var singleCharacterToken = Current switch
            {
                '+' => SyntaxKind.PlusToken,
                '-' => SyntaxKind.MinusToken,
                '*' => SyntaxKind.StarToken,
                '/' => SyntaxKind.SlashToken,
                '(' => SyntaxKind.OpenParenToken,
                ')' => SyntaxKind.CloseParenToken,
                _   => SyntaxKind.BadToken,
            };

            if (singleCharacterToken == SyntaxKind.BadToken)
            {
                _diagnostics.Add($"ERROR: bad character input: {Current}");
            }

            return getSingleCharacterToken(singleCharacterToken);
        }
    }

    abstract class SyntaxNode
    {
        public abstract SyntaxKind Kind { get; }

        public abstract IEnumerable<SyntaxNode> GetChildren();
    }

    abstract class ExpressionSyntax : SyntaxNode
    {

    }

    sealed class NumberExpressionSyntax : ExpressionSyntax
    {
        public NumberExpressionSyntax(SyntaxToken numberToken)
        {
            NumberToken = numberToken;
        }

        public override SyntaxKind Kind => SyntaxKind.NumberExpression;
        public SyntaxToken NumberToken { get; private init; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return NumberToken;
        }
    }

    sealed class BinaryExpressionSyntax : ExpressionSyntax
    {
        public BinaryExpressionSyntax(ExpressionSyntax left, SyntaxToken operatorToken, ExpressionSyntax right)
        {
            Left = left;
            OperatorToken = operatorToken;
            Right = right;
        }

        public override SyntaxKind Kind => SyntaxKind.BinaryExpression;
        public ExpressionSyntax Left { get; private init; }
        public SyntaxToken OperatorToken { get; private init; }
        public ExpressionSyntax Right { get; private init; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Left;
            yield return OperatorToken;
            yield return Right;
        }

        public static bool IsOperatorTokenSupported(SyntaxToken op)
        {
            switch (op.Kind)
            {
                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                case SyntaxKind.StarToken:
                case SyntaxKind.SlashToken:
                    return true;

                default:
                    return false;
            }
        }
    }

    sealed class SyntaxTree
    {
        public SyntaxTree(IEnumerable<string> diagnostics, ExpressionSyntax root, SyntaxToken endOfFileToken)
        {
            Diagnostics = diagnostics.ToArray();
            Root = root;
            EndOfFileToken = endOfFileToken;
        }

        public IReadOnlyList<string> Diagnostics { get; }
        public ExpressionSyntax Root { get; }
        public SyntaxToken EndOfFileToken { get; }
    }

    class Parser
    {
        private SyntaxToken[] _tokens;
        private int _position;
        private List<string> _diagnostics = new();

        public Parser(string text)
        {
            var tokens = new List<SyntaxToken>();

            var lexer = new Lexer(text);
            SyntaxToken token;

            do
            {
                token = lexer.NextToken();

                switch (token.Kind)
                {
                    case SyntaxKind.BadToken:
                    case SyntaxKind.WhiteSpaceToken:
                        break;

                    default:
                        tokens.Add(token);
                        break;
                }
            }
            while (token.Kind != SyntaxKind.EndOfFileToken);

            _tokens = tokens.ToArray();
            _diagnostics.AddRange(lexer.Diagnostics);
        }

        public IEnumerable<string> Diagnostics => _diagnostics;

        private SyntaxToken Peek(int offset = 0) =>
            _tokens[Math.Min(_position + offset, _tokens.Length - 1)];

        private SyntaxToken Current => Peek();

        private SyntaxToken NextToken()
        {
            var current = Current;
            _position++;

            return current;
        }

        private SyntaxToken Match(SyntaxKind kind)
        {
            if (Current.Kind == kind)
                return NextToken();

            _diagnostics.Add($"ERROR: Unexpected token: <{Current.Kind}>, expected: <{kind}>");
            return new SyntaxToken(kind, Current.Position, null, null);
        }

        private ExpressionSyntax ParsePrimaryExpression()
        {
            var numberToken = Match(SyntaxKind.NumberToken);
            return new NumberExpressionSyntax(numberToken);
        }

        public SyntaxTree Parse()
        {
            var expression = ParseExpression();
            var endOfFileToken = Match(SyntaxKind.EndOfFileToken);

            return new SyntaxTree(_diagnostics, expression, endOfFileToken);
        }

        private ExpressionSyntax ParseExpression()
        {
            var left = ParsePrimaryExpression();

            while (BinaryExpressionSyntax.IsOperatorTokenSupported(Current))
            {
                var operatorToken = NextToken();
                var right = ParsePrimaryExpression();

                left = new BinaryExpressionSyntax(left, operatorToken, right);
            }

            return left;
        }
    }

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

            throw new InvalidOperationException($"Unexpected expression: <{node.Kind}>");
        }
    }
}
