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

            var parser = new Parser(Errors);
            var syntaxTree = parser.BuildSyntaxTree(tokens);

            var generator = new Generator(Errors);
            var ast = generator.Generate(syntaxTree);

            // var processor = new AstProcessor();
            // ast.Visit(processor);
            // 
            // var serializer = new AstSerializer();
            // ast.Visit(serializer);
            // var scd = serializer.GetBytes();
            // OutputInit = scd;
            return 0;
        }

        private void EmitError(in Token token, int code, params object[] args)
        {
            Errors.AddError(token.Path, token.Line, token.Column, code, string.Format(ErrorCodes.GetMessage(code), args));
        }

        private void EmitWarning(in Token token, int code, params object[] args)
        {
            Errors.AddWarning(token.Path, token.Line, token.Column, code, string.Format(ErrorCodes.GetMessage(code), args));
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
    }
}
