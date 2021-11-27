using Languid.Compiler.CodeAnalysis;

namespace Languid.Compiler
{
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
                '(' => SyntaxKind.GroupOpenToken,
                ')' => SyntaxKind.GroupCloseToken,
                _   => SyntaxKind.BadToken,
            };

            if (singleCharacterToken == SyntaxKind.BadToken)
            {
                _diagnostics.Add($"ERROR: bad character input: {Current}");
            }

            return getSingleCharacterToken(singleCharacterToken);
        }
    }
}
