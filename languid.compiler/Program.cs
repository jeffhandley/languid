while (true)
{
    Console.Write("> ");

    var line = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(line))
        return;

    var lexer = new Lexer(line);
    SyntaxToken token;

    do
    {
        token = lexer.NextToken();
        Console.WriteLine($"{token.Kind}: '{token.Text}'");
    }
    while (token.Kind != SyntaxKind.EndOfFileToken);
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
    EndOfFileToken = Int32.MaxValue
}

record SyntaxToken(SyntaxKind Kind, int Position, string Text, object Value);

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
