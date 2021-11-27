namespace Languid.Compiler.CodeAnalysis
{
    enum SyntaxKind
    {
        BadToken = 0,

        WhiteSpaceToken,
        NumberToken,

        PlusToken, MinusToken, StarToken, SlashToken,
        GroupOpenToken, GroupCloseToken,

        NumberExpression,
        BinaryTermExpression, BinaryFactorExpression,
        GroupedExpression,

        EndOfFileToken = Int32.MaxValue
    }
}
