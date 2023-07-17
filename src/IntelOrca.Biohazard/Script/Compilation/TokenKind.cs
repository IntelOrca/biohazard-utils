namespace IntelOrca.Biohazard.Script.Compilation
{
    internal enum TokenKind : byte
    {
        Unknown,
        Whitespace,
        NewLine,
        Comment,
        Number,
        String,
        Symbol,
        Label,
        Comma,
        Plus,
        Minus,
        Asterisk,
        Pipe,
        Ampersand,
        LShift,
        RShift,
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
        While,

        EOF
    }
}
