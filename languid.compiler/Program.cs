namespace Languid.Compiler
{
    class Program
    {
        private static void PrettyPrint(SyntaxNode node, string indent = "")
        {
            Console.Write(indent + node.Kind);

            if (node is SyntaxToken t && t.Value is not null)
            {
                Console.Write($" {t.Value}");
            }

            Console.WriteLine();

            indent += "    ";

            foreach (var child in node.GetChildren())
                PrettyPrint(child, indent);
        }

        public static void Main(string[] _)
        {
            while (true)
            {
                Console.Write("> ");

                var line = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(line))
                    return;

                var parser = new Parser(line);
                var expression = parser.Parse();

                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkGray;
                PrettyPrint(expression, "");
                Console.ForegroundColor = color;

                var lexer = new Lexer(line);
                SyntaxToken token;

                do
                {
                    token = lexer.NextToken();
                    Console.WriteLine($"{token.Kind}: '{token.Text}'");
                }
                while (token.Kind != SyntaxKind.EndOfFileToken);
            }
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

        public Lexer(string text)
        {
            _text = text;
        }

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

            return Current switch
            {
                '+' => getSingleCharacterToken(SyntaxKind.PlusToken),
                '-' => getSingleCharacterToken(SyntaxKind.MinusToken),
                '*' => getSingleCharacterToken(SyntaxKind.StarToken),
                '/' => getSingleCharacterToken(SyntaxKind.SlashToken),
                '(' => getSingleCharacterToken(SyntaxKind.OpenParenToken),
                ')' => getSingleCharacterToken(SyntaxKind.CloseParenToken),
                _   => getSingleCharacterToken(SyntaxKind.BadToken),
            };
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

    class Parser
    {
        private SyntaxToken[] _tokens;
        private int _position;

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
                    case SyntaxKind.EndOfFileToken:
                        break;

                    default:
                        tokens.Add(token);
                        break;
                }
            }
            while (token.Kind != SyntaxKind.EndOfFileToken);

            _tokens = tokens.ToArray();
        }

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

            return new SyntaxToken(kind, Current.Position, null, null);
        }

        private ExpressionSyntax ParsePrimaryExpression()
        {
            var numberToken = Match(SyntaxKind.NumberToken);
            return new NumberExpressionSyntax(numberToken);
        }

        public ExpressionSyntax Parse()
        {
            var left = ParsePrimaryExpression();

            while (Current.Kind == SyntaxKind.PlusToken || Current.Kind == SyntaxKind.MinusToken)
            {
                var operatorToken = NextToken();
                var right = ParsePrimaryExpression();

                left = new BinaryExpressionSyntax(left, operatorToken, right);
            }

            return left;
        }
    }
}
