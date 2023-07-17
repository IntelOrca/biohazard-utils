using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace IntelOrca.Biohazard.Script.Compilation
{
    public partial class ScdCompiler
    {
        private class Lexer : LexerBase
        {
            private readonly MacroTable _macroTable;

            public Lexer(IFileIncluder includer, ErrorList errors)
                : base(includer, errors)
            {
                _macroTable = new MacroTable();
            }

            private Lexer(Lexer baseLexer)
                : base(baseLexer.Includer, baseLexer.Errors)
            {
                _macroTable = baseLexer._macroTable;
            }

            protected override void Begin()
            {
            }

            protected override IEnumerable<Token> GetNextToken()
            {
                var token = ScanSingleToken();
                if (token.Kind == TokenKind.Directive)
                {
                    if (token.Text == "#include")
                    {
                        var nextToken = ScanSingleNonWhitespaceToken();
                        if (nextToken.Kind == TokenKind.String)
                        {
                            var includeLexer = new Lexer(this);
                            var includePath = nextToken.Text.Substring(1, nextToken.Text.Length - 2);
                            var fullPath = Includer.GetIncludePath(Path, includePath);
                            foreach (var includeToken in includeLexer.GetTokens(fullPath))
                            {
                                if (includeToken.Kind == TokenKind.EOF)
                                    break;
                                yield return includeToken;
                            }
                            yield break;
                        }
                        else
                        {
                            EmitError(in nextToken, ErrorCodes.ExpectedPath);
                        }
                    }
                    else if (token.Text == "#define")
                    {
                        var nameToken = ScanSingleNonWhitespaceToken();
                        if (nameToken.Kind != TokenKind.Symbol)
                        {
                            EmitError(in nameToken, ErrorCodes.ExpectedMacroName);
                            yield break;
                        }

                        var definedTokens = ScanDefineTokens()
                            .SkipWhile(x => x.Kind == TokenKind.Whitespace)
                            .ToList();

                        var endToken = definedTokens.Last();
                        definedTokens.RemoveAt(definedTokens.Count - 1);
                        DefineMacro(nameToken, definedTokens.ToArray());
                        yield return endToken;
                        yield break;
                    }
                }
                else if (token.Kind == TokenKind.Symbol)
                {
                    var macroName = token.Text;
                    var tokens = _macroTable.TryGetMacroTokens(macroName);
                    if (tokens != null)
                    {
                        foreach (var t in tokens)
                        {
                            yield return t;
                        }
                        yield break;
                    }
                }
                yield return token;
            }

            private void DefineMacro(in Token nameToken, Token[] tokens)
            {
                var name = nameToken.Text;
                if (_macroTable.Contains(name))
                {
                    EmitError(in nameToken, ErrorCodes.MacroAlreadyDefined, name);
                    return;
                }

                var nameRegex = new Regex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
                if (!nameRegex.IsMatch(name))
                {
                    EmitError(in nameToken, ErrorCodes.InvalidMacroName, name);
                    return;
                }

                _macroTable.Add(name, tokens);
            }

            private Token ScanSingleNonWhitespaceToken()
            {
                Token token;
                while ((token = ScanSingleToken()).Kind == TokenKind.Whitespace)
                {
                }
                return token;
            }

            private IEnumerable<Token> ScanDefineTokens()
            {
                while (true)
                {
                    var token = ScanSingleToken();
                    yield return token;
                    if (token.Kind == TokenKind.NewLine || token.Kind == TokenKind.EOF)
                    {
                        yield break;
                    }
                }
            }

            private Token ScanSingleToken()
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
                    _ when Parse('|') => CreateToken(TokenKind.BitwiseOr),
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

        private class MacroTable
        {
            private readonly Dictionary<string, Token[]> _macros = new Dictionary<string, Token[]>();

            public bool Contains(string name)
            {
                return _macros.ContainsKey(name);
            }

            public void Add(string name, Token[] tokens)
            {
                _macros.Add(name, tokens);
            }

            public Token[] TryGetMacroTokens(string name)
            {
                _macros.TryGetValue(name, out var result);
                return result;
            }
        }
    }
}
