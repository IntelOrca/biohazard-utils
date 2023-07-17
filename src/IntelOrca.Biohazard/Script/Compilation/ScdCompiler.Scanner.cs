using System;

namespace IntelOrca.Biohazard.Script.Compilation
{
    public partial class ScdCompiler
    {
        private class Scanner : LexerBase
        {
            public Scanner(IFileIncluder includer, ErrorList errors)
                : base(includer, errors)
            {
            }

            protected override Token GetNextToken()
            {
                return true switch
                {
                    _ when ParseNewLine() => CreateToken(TokenKind.NewLine),
                    _ when ParseWhitespace() => CreateToken(TokenKind.Whitespace),
                    _ when ParseComment() => CreateToken(TokenKind.Comment),
                    _ when ParseDirective() => CreateToken(TokenKind.Directive),
                    _ when ParseString() => CreateToken(TokenKind.String),
                    _ when ParseNumber() => CreateToken(TokenKind.Number),
                    _ when Parse("proc") => CreateToken(TokenKind.Proc),
                    _ when Parse("fork") => CreateToken(TokenKind.Fork),
                    _ when Parse("if") => CreateToken(TokenKind.If),
                    _ when Parse("else") => CreateToken(TokenKind.Else),
                    _ when Parse("while") => CreateToken(TokenKind.While),
                    _ when Parse('{') => CreateToken(TokenKind.OpenBlock),
                    _ when Parse('}') => CreateToken(TokenKind.CloseBlock),
                    _ when Parse('(') => CreateToken(TokenKind.OpenParen),
                    _ when Parse(')') => CreateToken(TokenKind.CloseParen),
                    _ when Parse(',') => CreateToken(TokenKind.Comma),
                    _ when Parse(';') => CreateToken(TokenKind.Semicolon),
                    _ when Parse('+') => CreateToken(TokenKind.Plus),
                    _ when Parse('-') => CreateToken(TokenKind.Minus),
                    _ when Parse('*') => CreateToken(TokenKind.Asterisk),
                    _ when Parse('|') => CreateToken(TokenKind.Pipe),
                    _ when ParseSymbol() => CreateToken(TokenKind.Symbol),
                    _ when ParseAny() => CreateToken(TokenKind.Unknown),
                    _ => throw new InvalidOperationException(),
                };
            }

            protected override bool ValidateToken(in Token token)
            {
                if (token.Kind == TokenKind.Number)
                {
                }
                else if (token.Kind == TokenKind.Comma)
                {
                    if (token.Text != ",")
                    {
                        EmitError(in token, ErrorCodes.InvalidOperator, token.Text);
                        return false;
                    }
                }
                return true;
            }

            private bool ParseComment()
            {
                if (!Parse("//"))
                    return false;

                while (true)
                {
                    var c = PeekChar();
                    if (c == '\n' || c == '\r' || c == '\0')
                    {
                        break;
                    }
                    ReadChar();
                }
                return true;
            }

            private bool ParseDirective()
            {
                if (!Parse('#'))
                    return false;
                ReadChar();
                ParseUntilSeperator();
                return true;
            }

            private bool ParseSymbol()
            {
                var c = PeekChar();
                if (!char.IsLetter(c))
                    return false;
                ParseUntilSeperator();
                return true;
            }

            private void ParseUntilSeperator()
            {
                while (true)
                {
                    var c = PeekChar();
                    if (!char.IsLetterOrDigit(c) && c != '_')
                    {
                        break;
                    }
                    ReadChar();
                }
            }
        }
    }
}
