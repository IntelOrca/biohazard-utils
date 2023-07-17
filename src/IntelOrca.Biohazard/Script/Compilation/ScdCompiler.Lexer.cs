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
                foreach (var t in ExpandToken(token))
                {
                    yield return t;
                }
            }

            private IEnumerable<Token> ExpandToken(Token token)
            {
                if (token.Kind != TokenKind.Symbol)
                {
                    yield return token;
                    yield break;
                }

                var macroName = token.Text;
                var macro = _macroTable.TryGetMacro(macroName);
                if (macro != null)
                {
                    foreach (var t in ProcessMacro(token, macro))
                    {
                        foreach (var t2 in ExpandToken(t))
                        {
                            yield return t2;
                        }
                    }
                }
                else
                {
                    yield return token;
                }
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

                var macro = ParseMacro(name, tokens);
                if (macro != null)
                {
                    _macroTable.Add(macro);
                }
            }

            private Macro? ParseMacro(string name, Token[] tokens)
            {
                var parameters = new List<string>();
                var newTokenList = new List<Token>();
                const int stateBegin = 1;
                const int stateParameter = 2;
                const int stateParameterSep = 3;
                const int stateEndParameter = 4;
                const int stateBody = 5;
                var state = stateBegin;
                for (var i = 0; i < tokens.Length; i++)
                {
                    ref readonly var t = ref tokens[i];
                    switch (state)
                    {
                        case stateBegin:
                            if (t.Kind == TokenKind.Whitespace)
                                continue;
                            if (t.Kind == TokenKind.OpenParen)
                            {
                                state = stateParameter;
                            }
                            else
                            {
                                state = stateBody;
                                goto case stateBody;
                            }
                            break;
                        case stateParameter:
                            if (t.Kind == TokenKind.Whitespace)
                                continue;
                            if (t.Kind == TokenKind.CloseParen)
                            {
                                state = stateEndParameter;
                            }
                            else if (t.Kind != TokenKind.Symbol)
                            {
                                EmitError(in t, ErrorCodes.ExpectedOperand);
                                return null;
                            }
                            else
                            {
                                parameters.Add(t.Text);
                                state = stateParameterSep;
                            }
                            break;
                        case stateParameterSep:
                            if (t.Kind == TokenKind.Whitespace)
                                continue;
                            if (t.Kind == TokenKind.CloseParen)
                            {
                                state = stateEndParameter;
                            }
                            else if (t.Kind != TokenKind.Comma)
                            {
                                EmitError(in t, ErrorCodes.ExpectedComma);
                                return null;
                            }
                            else
                            {
                                state = stateParameter;
                            }
                            break;
                        case stateEndParameter:
                            if (t.Kind == TokenKind.Whitespace)
                                continue;
                            state = stateBody;
                            goto case stateBody;
                        case stateBody:
                            newTokenList.Add(t);
                            break;
                    }
                }
                while (newTokenList.Count != 0 && newTokenList[newTokenList.Count - 1].Kind == TokenKind.Whitespace)
                {
                    newTokenList.RemoveAt(newTokenList.Count - 1);
                }
                return new Macro(name, parameters.ToArray(), newTokenList.ToArray());
            }

            private IEnumerable<Token> ProcessMacro(Token token, Macro macro)
            {
                var c = PeekNonWhitespaceChar();
                if (c != '(')
                {
                    if (macro.Parameters.Length == 0)
                    {
                        foreach (var mt in macro.Tokens)
                            yield return mt;
                    }
                    else
                    {
                        EmitError(in token, ErrorCodes.ExpectedOperand);
                    }
                    yield break;
                }

                var t = ScanSingleNonWhitespaceToken();
                if (t.Kind != TokenKind.OpenParen)
                {
                    EmitError(in token, ErrorCodes.ExpectedOperand);
                    yield break;
                }

                var signature = new List<Token[]>();
                var argument = new List<Token>();
                var parenCount = 1;
                while (parenCount != 0)
                {
                    // Scan argument
                    t = ScanSingleToken();
                    if (t.Kind == TokenKind.OpenParen)
                    {
                        parenCount++;
                    }
                    else if (t.Kind == TokenKind.CloseParen)
                    {
                        parenCount--;
                    }
                    else if (t.Kind == TokenKind.Comma && parenCount == 1)
                    {
                        signature.Add(argument.ToArray());
                        argument.Clear();
                    }
                    else if (t.Kind == TokenKind.EOF)
                    {
                        break;
                    }
                    else
                    {
                        foreach (var t2 in ExpandToken(t))
                        {
                            argument.Add(t2);
                        }
                    }
                }
                if (argument.Count != 0)
                {
                    signature.Add(argument.ToArray());
                }
                if (parenCount != 0)
                {
                    EmitError(in token, ErrorCodes.ExpectedOperand);
                    yield break;
                }

                foreach (var mt in macro.GetTokens(signature.ToArray()))
                {
                    foreach (var mt2 in ExpandToken(mt))
                    {
                        yield return mt2;
                    }
                }
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
                    _ when Parse('+') => CreateToken(TokenKind.Plus),
                    _ when Parse('-') => CreateToken(TokenKind.Minus),
                    _ when Parse('*') => CreateToken(TokenKind.Asterisk),
                    _ when Parse('|') => CreateToken(TokenKind.Pipe),
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
            private readonly Dictionary<string, Macro> _macros = new Dictionary<string, Macro>();

            public bool Contains(string name)
            {
                return _macros.ContainsKey(name);
            }

            public void Add(Macro macro)
            {
                _macros.Add(macro.Name, macro);
            }

            public Macro TryGetMacro(string name)
            {
                _macros.TryGetValue(name, out var result);
                return result;
            }

            public Token[]? TryGetMacroTokens(string name)
            {
                if (!_macros.TryGetValue(name, out var result))
                    return null;
                return result.Tokens;
            }
        }

        private class Macro
        {
            public string Name { get; }
            public string[] Parameters { get; }
            public Token[] Tokens { get; }

            public Macro(string name, string[] parameters, Token[] tokens)
            {
                Name = name;
                Parameters = parameters;
                Tokens = tokens;
            }

            public IEnumerable<Token> GetTokens(Token[][] arguments)
            {
                foreach (var token in Tokens)
                {
                    if (token.Kind == TokenKind.Symbol)
                    {
                        var parameterIndex = FindParameter(token.Text);
                        if (parameterIndex != -1)
                        {
                            foreach (var t in arguments[parameterIndex])
                            {
                                yield return t;
                            }
                            continue;
                        }
                    }
                    yield return token;
                }
            }

            private int FindParameter(string name)
            {
                return Array.IndexOf(Parameters, name);
            }
        }
    }
}
