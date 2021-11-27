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

                Console.WriteLine("Parsing: " + line);

                var parser = new Parser(line);
                var syntaxTree = parser.Parse();

                Console.WriteLine("Parsed");

                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkGray;
                PrettyPrint(syntaxTree.Root, "");

                if (syntaxTree.Diagnostics.Any())
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

                int.TryParse(text, out var value);

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
            Console.WriteLine("Parse()");
            var left = ParsePrimaryExpression();

            while (Current.Kind == SyntaxKind.PlusToken || Current.Kind == SyntaxKind.MinusToken)
            {
                var operatorToken = NextToken();
                var right = ParsePrimaryExpression();

                left = new BinaryExpressionSyntax(left, operatorToken, right);
            }

            Console.WriteLine("Parse() completed");
            return left;
        }
    }
}
