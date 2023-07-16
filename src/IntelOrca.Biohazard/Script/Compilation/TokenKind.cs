namespace IntelOrca.Biohazard.Script.Compilation
{
    internal enum TokenKind : byte
    {
        Unknown,
        Whitespace,
        NewLine,
        Comment,
        Number,
        Symbol,
        Label,
        Comma,
        Add,
        Subtract,
        BitwiseOr,
        Opcode,
        Directive,

        Semicolon,
        OpenBlock,
        CloseBlock,
        OpenParen,
        CloseParen,
        Proc,
        Fork,
        If,
        Else,

        EOF
    }
}
