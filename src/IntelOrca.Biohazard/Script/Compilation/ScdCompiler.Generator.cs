using System.Collections.Generic;
using System.Linq;

namespace IntelOrca.Biohazard.Script.Compilation
{
    public partial class ScdCompiler
    {
        private class Generator
        {
            private readonly ErrorList _errors;
            private readonly ScriptAstBuilder _builder = new ScriptAstBuilder();
            private BioVersion? _version;
            private IConstantTable _constantTable = new Bio1ConstantTable();
            private List<string> _procedureNames = new List<string>();

            public Generator(ErrorList errors)
            {
                _errors = errors;
            }

            public ScriptAst Generate(SyntaxTree tree)
            {
                Visit(tree.Root);
                return _builder.Ast;
            }

            private void VisitChildren(SyntaxNode node)
            {
                foreach (var child in node.Children)
                {
                    Visit(child);
                }
            }

            private void Visit(SyntaxNode node)
            {
                switch (node)
                {
                    case VersionSyntaxNode version:
                        VisitVersionNode(version);
                        break;
                    case ProcedureSyntaxNode procedure:
                        VisitProcedureNode(procedure);
                        break;
                    case OpcodeSyntaxNode opcode:
                        VisitOpcodeNode(opcode);
                        break;
                    default:
                        VisitChildren(node);
                        break;
                }
            }

            private void VisitVersionNode(VersionSyntaxNode versionNode)
            {
                var versionToken = versionNode.VersionToken;
                if (_version != null)
                {
                    EmitError(in versionToken, ErrorCodes.ScdTypeAlreadySpecified);
                    return;
                }

                var version = ParseNumber(in versionToken);
                _version = version switch
                {
                    1 => BioVersion.Biohazard1,
                    2 => BioVersion.Biohazard2,
                    3 => BioVersion.Biohazard3,
                    _ => null
                };
                if (_version == null)
                {
                    EmitError(in versionToken, ErrorCodes.InvalidScdVersionNumber);
                    return;
                }

                _constantTable = ConstantTable.FromVersion(_version.Value);
                _builder.VisitVersion(_version.Value);
            }

            private void VisitProcedureNode(ProcedureSyntaxNode procedureNode)
            {
                var name = procedureNode.NameToken.Text;
                if (_procedureNames.Contains(name))
                {
                    EmitError(procedureNode.NameToken, ErrorCodes.ProcedureNameAlreadyDefined, name);
                }

                _builder.VisitBeginSubroutine(0);
                VisitChildren(procedureNode);
                _builder.VisitEndSubroutine(0);
            }

            private void VisitOpcodeNode(OpcodeSyntaxNode opcodeNode)
            {
                var opcodeRaw = _constantTable.FindOpcode(opcodeNode.OpcodeToken.Text);
                if (opcodeRaw == null)
                {
                    EmitError(opcodeNode.OpcodeToken, ErrorCodes.UnknownOpcode, opcodeNode.OpcodeToken.Text);
                    return;
                }

                var opcodeSignature = _constantTable.GetOpcodeSignature(opcodeRaw.Value);
                var opcodeLength = _constantTable.GetInstructionSize(opcodeRaw.Value, null);
                var colonIndex = opcodeSignature.IndexOf(':');
                if (colonIndex == -1)
                {
                    var length = opcodeLength;
                    if (opcodeSignature != "")
                        length--;
                    opcodeSignature = new string('u', length);
                }
                else
                {
                    opcodeSignature = opcodeSignature.Substring(colonIndex + 1);
                }
                var numArguments = opcodeSignature.Length;

                var operands = opcodeNode.Children.ToArray();
                if (operands.Length != numArguments)
                {
                    EmitError(opcodeNode.OpcodeToken, ErrorCodes.IncorrectNumberOfOperands);
                }

                VisitChildren(opcodeNode);
            }

            private int ParseNumber(in Token token)
            {
                return token.Text.StartsWith("0x")
                    ? int.Parse(token.Text.Substring(2), System.Globalization.NumberStyles.HexNumber)
                    : int.Parse(token.Text);
            }

            private void EmitError(in Token token, int code, params object[] args)
            {
                _errors.AddError(token.Path, token.Line, token.Column, code, string.Format(ErrorCodes.GetMessage(code), args));
            }

            private void EmitWarning(in Token token, int code, params object[] args)
            {
                _errors.AddWarning(token.Path, token.Line, token.Column, code, string.Format(ErrorCodes.GetMessage(code), args));
            }
        }
    }
}
