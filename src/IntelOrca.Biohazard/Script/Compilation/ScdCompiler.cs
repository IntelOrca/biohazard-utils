using System;
using System.Buffers.Binary;
using System.IO;
using IntelOrca.Biohazard.Script.Opcodes;

namespace IntelOrca.Biohazard.Script.Compilation
{
    public partial class ScdCompiler
    {
        public ErrorList Errors { get; } = new ErrorList();
        public byte[] OutputInit { get; private set; } = new byte[0];
        public byte[] OutputMain { get; private set; } = new byte[0];

        public int Compile(string path, string script)
        {
            var lexer = new Lexer(Errors);
            var tokens = lexer.ParseAllTokens(path, script);
            if (Errors.Count != 0)
                return 1;

            var ast = BuildAst();

            var processor = new AstProcessor();
            ast.Visit(processor);

            var serializer = new AstSerializer();
            ast.Visit(serializer);
            var scd = serializer.GetBytes();
            OutputInit = scd;
            return 0;
        }

        private ScriptAst BuildAst()
        {
            var builder = new ScriptAstBuilder();
            builder.VisitVersion(BioVersion.Biohazard2);
            builder.VisitBeginScript(BioScriptKind.Init);
            builder.VisitBeginSubroutine(0);

            builder.VisitOpcode(0, new byte[] { (byte)OpcodeV2.IfelCk, 0, 0, 0 });
            builder.VisitOpcode(0, new byte[] { (byte)OpcodeV2.Ck, 1, 4, 1 });
            builder.VisitOpcode(0, new byte[] { (byte)OpcodeV2.Sleep, 10, 50, 0 });
            builder.VisitOpcode(0, new byte[] { (byte)OpcodeV2.EndIf, 0 });

            builder.VisitEndSubroutine(0);
            builder.VisitEndScript(BioScriptKind.Init);

            return builder.Ast;
        }

        private class AstProcessor : ScriptAstVisitor
        {
            private int _offset;

            public override void VisitIf(IfAstNode node)
            {
            }

            public override void VisitEndIf(IfAstNode node)
            {
                var ifOpcode = node.If?.Opcode;
                if (ifOpcode is UnknownOpcode op)
                {
                    var blockLenSpan = new Span<byte>(op.Data).Slice(1, 2);
                    var blockLen = (ushort)(_offset - ifOpcode.Offset);
                    if (node.EndIf != null)
                        _offset += 2;
                    BinaryPrimitives.WriteUInt16LittleEndian(blockLenSpan, blockLen);
                }
            }

            public override void VisitOpcode(OpcodeAstNode node)
            {
                node.Opcode.Offset = _offset;
                _offset += node.Opcode.Length;
            }
        }

        private class AstSerializer : ScriptAstVisitor
        {
            private readonly MemoryStream _ms = new MemoryStream();
            private readonly BinaryWriter _bw;

            public AstSerializer()
            {
                _bw = new BinaryWriter(_ms);
            }

            public byte[] GetBytes() => _ms.ToArray();

            public override void VisitOpcode(OpcodeAstNode node)
            {
                node.Opcode.Write(_bw);
            }
        }

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
