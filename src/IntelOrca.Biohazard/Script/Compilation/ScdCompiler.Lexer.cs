using System;

namespace IntelOrca.Biohazard.Script.Compilation
{
    public partial class ScdCompiler
    {
        private class Lexer : LexerBase
        {
            public Lexer(ErrorList errors)
                : base(errors)
            {
            }

            protected override void Begin()
            {
            }

            protected override Token ParseToken()
            {
                return true switch
                {
                    _ when ParseNewLine() => CreateToken(TokenKind.NewLine),
                    _ when ParseWhitespace() => CreateToken(TokenKind.Whitespace),
                    _ when ParseComment() => CreateToken(TokenKind.Comment),
                    _ when ParseDirective() => CreateToken(TokenKind.Directive),
                    _ when ParseNumber() => CreateToken(TokenKind.Number),
                    _ when Parse("proc") => CreateToken(TokenKind.Proc),
                    _ when Parse('{') => CreateToken(TokenKind.OpenBlock),
                    _ when Parse('}') => CreateToken(TokenKind.CloseBlock),
                    _ when Parse('(') => CreateToken(TokenKind.OpenParen),
                    _ when Parse(')') => CreateToken(TokenKind.CloseParen),
                    _ when Parse(',') => CreateToken(TokenKind.Comma),
                    _ when Parse(';') => CreateToken(TokenKind.Semicolon),
                    _ when ParseSymbol() => CreateToken(TokenKind.Symbol),
                    _ => throw new Exception()
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

            private bool ParseComment() => Parse("//");

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
                    if (!char.IsLetterOrDigit(c))
                    {
                        break;
                    }
                    ReadChar();
                }
            }
        }
    }
}
