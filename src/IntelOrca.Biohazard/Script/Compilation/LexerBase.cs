using System.Collections.Generic;

namespace IntelOrca.Biohazard.Script.Compilation
{
    internal abstract class LexerBase
    {
        private string _path = "";
        private string _s = "";
        private int _sIndex;

        private int _offset;
        private int _line;
        private int _column;

        protected int CurrentTokenLength => _sIndex - _offset;

        public ErrorList Errors { get; }

        public LexerBase(ErrorList errors)
        {
            Errors = errors;
        }

        public Token[] ParseAllTokens(string path, string script)
        {
            _path = path;
            _s = script;

            var tokens = new List<Token>();
            Token token;
            do
            {
                var c = PeekChar();
                token = c == char.MinValue ?
                    new Token(TokenKind.EOF, _path, _line, _column) :
                    ParseToken();
                ValidateToken(in token);
                tokens.Add(token);
            } while (token.Kind != TokenKind.EOF);
            return tokens.ToArray();
        }

        protected virtual void Begin() { }
        protected abstract Token ParseToken();
        protected abstract bool ValidateToken(in Token token);

        protected void EmitError(in Token token, int code, params object[] args)
        {
            Errors.AddError(token.Path, token.Line, token.Column, code, string.Format(ErrorCodes.GetMessage(code), args));
        }

        protected char GetLastReadChar()
        {
            return _s[_sIndex - 1];
        }

        protected Token CreateToken(TokenKind kind)
        {
            var length = _sIndex - _offset;
            var token = new Token(_s, _offset, kind, _path, _line, _column, length);
            if (kind == TokenKind.NewLine)
            {
                _line++;
                _column = 0;
            }
            else
            {
                _column += length;
            }
            _offset = _sIndex;
            return token;
        }

        protected bool ParseNewLine()
        {
            var c = PeekChar();
            if (c != '\n' && c != '\r')
                return false;

            ReadChar();
            c = PeekChar();
            if (c == '\n')
                ReadChar();
            return true;
        }

        protected bool ParseWhitespace()
        {
            var c = PeekChar();
            if (!char.IsWhiteSpace(c))
                return false;
            do
            {
                ReadChar();
                c = PeekChar();
            } while (c != char.MinValue && char.IsWhiteSpace(c));
            return true;
        }

        protected bool ParseNumber()
        {
            var c = PeekChar();
            if ((c < '0' || c > '9') && c != '-')
                return false;

            if (c == '-')
            {
                c = PeekChar(skip: 1);
                if (c < '0' || c > '9')
                    return false;
                c = ReadChar();
            }

            while (true)
            {
                c = PeekChar();
                if (!char.IsLetterOrDigit(c))
                {
                    break;
                }
                ReadChar();
            }
            return true;
        }

        protected bool Parse(char c)
        {
            if (PeekChar() != c)
                return false;

            ReadChar();
            return true;
        }

        protected bool Parse(string s)
        {
            var remaining = _s.Length - _sIndex;
            if (remaining < s.Length)
                return false;

            if (string.Compare(_s, _sIndex, s, 0, s.Length) != 0)
                return false;

            _sIndex += s.Length;
            return true;
        }

        protected char PeekChar(int skip = 0)
        {
            var offset = _sIndex + skip;
            if (offset >= _s.Length)
                return char.MinValue;
            return _s[offset];
        }

        protected char ReadChar()
        {
            var result = PeekChar();
            _sIndex++;
            return result;
        }

        protected char GetLastChar()
        {
            if (_sIndex == 0)
                return char.MinValue;
            return _s[_sIndex - 1];
        }
    }
}
