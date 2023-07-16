using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace IntelOrca.Biohazard.Script.Compilation
{
    public partial class ScdCompiler
    {
        private class Generator
        {
            private readonly ErrorList _errors;
            private BioVersion? _version;
            private IConstantTable _constantTable = new Bio1ConstantTable();
            private List<string> _procedureNames = new List<string>();
            private List<ProcedureBuilder> _procedures = new List<ProcedureBuilder>();
            private ProcedureBuilder _currentProcedure = new ProcedureBuilder("");

            public byte[] OutputInit { get; private set; } = new byte[0];
            public byte[] OutputMain { get; private set; } = new byte[0];

            public Generator(ErrorList errors)
            {
                _errors = errors;
            }

            public int Generate(SyntaxTree tree)
            {
                Visit(tree.Root);
                CheckProcedureReferences();
                if (_errors.Count != 0)
                {
                    return 1;
                }

                OutputInit = GenerateScd("init");
                OutputMain = GenerateScd("main", "aot");
                return 0;
            }

            private void CheckProcedureReferences()
            {
                var procedureNames = new HashSet<string>(_procedures.Select(x => x.Name));
                foreach (var proc in _procedures)
                {
                    var procRefs = proc.ProcedureReferences;
                    foreach (var procRef in procRefs)
                    {
                        if (!procedureNames.Contains(procRef.Name))
                        {
                            EmitError(procRef.Token, ErrorCodes.ProcedureNameAlreadyDefined, procRef.Name);
                        }
                    }
                }
            }

            private byte[] GenerateScd(params string[] startProcs)
            {
                // Make sure order is correct and create empty procedures for missing required ones
                var procedures = _procedures.ToList();
                foreach (var procName in startProcs.Reverse())
                {
                    var index = procedures.FindIndex(x => x.Name == procName);
                    if (index == -1)
                    {
                        var proc = new ProcedureBuilder(procName);
                        proc.Write((byte)OpcodeV2.EvtEnd);
                        proc.Write((byte)0x00);
                        procedures.Insert(0, proc);
                    }
                    else
                    {
                        var proc = procedures[index];
                        procedures.RemoveAt(index);
                        procedures.Insert(0, proc);
                    }
                }

                // Removed unreferenced procedures
                var referencedProcedures = new HashSet<ProcedureBuilder>();
                var searchStack = new Stack<ProcedureBuilder>();
                for (var i = 0; i < startProcs.Length; i++)
                {
                    referencedProcedures.Add(procedures[i]);
                    searchStack.Push(procedures[i]);
                }
                while (searchStack.Count != 0)
                {
                    var proc = searchStack.Pop();
                    foreach (var procRef in proc.ProcedureReferences)
                    {
                        var targetProc = _procedures.First(x => x.Name == procRef.Name);
                        if (referencedProcedures.Add(targetProc))
                        {
                            searchStack.Push(targetProc);
                        }
                    }
                }
                procedures.RemoveAll(x => !referencedProcedures.Contains(x));

                foreach (var proc in procedures)
                {
                    proc.FixProceduresReferences(procedures.Select(x => x.Name).ToArray());
                }

                var ms = new MemoryStream();
                var bw = new BinaryWriter(ms);
                ms.Position += procedures.Count * 2;
                for (var i = 0; i < procedures.Count; i++)
                {
                    var backupPosition = ms.Position;
                    ms.Position = i * 2;
                    bw.Write((ushort)backupPosition);
                    ms.Position = backupPosition;
                    bw.Write(procedures[i].ToArray());
                }
                return ms.ToArray();
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
                    case VersionSyntaxNode versionNode:
                        VisitVersionNode(versionNode);
                        break;
                    case ProcedureSyntaxNode procedureNode:
                        VisitProcedureNode(procedureNode);
                        break;
                    case ForkSyntaxNode forkNode:
                        VisitForkNode(forkNode);
                        break;
                    case IfSyntaxNode ifNode:
                        VisitIfNode(ifNode);
                        break;
                    case WhileSyntaxNode whileNode:
                        VisitWhileNode(whileNode);
                        break;
                    case OpcodeSyntaxNode opcodeNode:
                        VisitOpcodeNode(opcodeNode);
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
            }

            private void VisitProcedureNode(ProcedureSyntaxNode procedureNode)
            {
                var name = procedureNode.NameToken.Text;
                if (_procedureNames.Contains(name))
                {
                    EmitError(procedureNode.NameToken, ErrorCodes.ProcedureNameAlreadyDefined, name);
                }

                _currentProcedure = new ProcedureBuilder(name);
                VisitChildren(procedureNode);
                _currentProcedure.Align();
                _currentProcedure.Write((byte)OpcodeV2.EvtEnd);
                _currentProcedure.Write((byte)0);
                _currentProcedure.FixLabels();
                _procedures.Add(_currentProcedure);
            }

            private void VisitForkNode(ForkSyntaxNode forkNode)
            {
                _currentProcedure.Align();
                _currentProcedure.Write((byte)OpcodeV2.EvtExec);
                _currentProcedure.Write((byte)0xFF);
                _currentProcedure.Write((byte)0x18);
                _currentProcedure.WriteProcedureRef(forkNode.ProcedureToken);
            }

            private void VisitIfNode(IfSyntaxNode ifNode)
            {
                _currentProcedure.Align();
                _currentProcedure.Write((byte)OpcodeV2.IfelCk);
                _currentProcedure.Write((byte)0);
                var elseLabel = _currentProcedure.WriteLabelRef(2, 2);
                var endIfLabel = elseLabel;

                Visit(ifNode.Condition);

                if (ifNode.IfBlock != null)
                {
                    Visit(ifNode.IfBlock);
                }
                if (ifNode.ElseBlock != null)
                {
                    _currentProcedure.Align();
                    _currentProcedure.Write((byte)OpcodeV2.ElseCk);
                    _currentProcedure.Write((byte)0);
                    endIfLabel = _currentProcedure.WriteLabelRef(2, -2);
                    _currentProcedure.WriteLabel(elseLabel);
                    Visit(ifNode.ElseBlock);
                }
                else
                {
                    _currentProcedure.Align();
                    _currentProcedure.Write((byte)OpcodeV2.EndIf);
                    _currentProcedure.Write((byte)0);
                }
                _currentProcedure.Align();
                _currentProcedure.WriteLabel(endIfLabel);
            }

            private void VisitWhileNode(WhileSyntaxNode whileNode)
            {
                _currentProcedure.Align();
                _currentProcedure.Write((byte)OpcodeV2.While);
                var bodyLabel = _currentProcedure.WriteLabelRef(1, 3);
                var endLabel = _currentProcedure.WriteLabelRef(2, 2);

                Visit(whileNode.Condition);
                _currentProcedure.WriteLabel(bodyLabel);
                if (whileNode.Block != null)
                {
                    Visit(whileNode.Block);
                }

                _currentProcedure.Align();
                _currentProcedure.Write((byte)OpcodeV2.Ewhile);
                _currentProcedure.Write((byte)0);
                _currentProcedure.WriteLabel(endLabel);
            }

            private void VisitOpcodeNode(OpcodeSyntaxNode opcodeNode)
            {
                var opcodeRaw = _constantTable.FindOpcode(opcodeNode.OpcodeToken.Text);
                if (opcodeRaw == null)
                {
                    if (opcodeNode.Operands.Length == 0)
                    {
                        _currentProcedure.Write((byte)OpcodeV2.Gosub);
                        _currentProcedure.WriteProcedureRef(opcodeNode.OpcodeToken);
                    }
                    else
                    {
                        EmitError(opcodeNode.OpcodeToken, ErrorCodes.UnknownOpcode, opcodeNode.OpcodeToken.Text);
                    }
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

                if ((opcodeLength & 1) == 0)
                {
                    _currentProcedure.Align();
                }
                _currentProcedure.Write(opcodeRaw.Value);

                var numOperands = Math.Min(operands.Length, numArguments);
                for (var i = 0; i < numOperands; i++)
                {
                    var arg = opcodeSignature[i];
                    var value = ProcessOperand(operands[i]);
                    if (char.IsUpper(arg) || arg == '@' || arg == '~')
                    {
                        _currentProcedure.Write((short)value);
                    }
                    else
                    {
                        _currentProcedure.Write((byte)value);
                    }
                }
            }

            private int ProcessOperand(SyntaxNode node)
            {
                return ProcessExpression((ExpressionSyntaxNode)node);
            }

            private int ProcessExpression(ExpressionSyntaxNode node)
            {
                if (node is BinaryExpressionSyntaxNode binaryNode)
                {
                    var lhs = ProcessExpression(binaryNode.Left);
                    var rhs = ProcessExpression(binaryNode.Right);
                    return node.Kind switch
                    {
                        ExpressionKind.BitwiseOr => lhs | rhs,
                        _ => throw new NotSupportedException()
                    };
                }
                else if (node is LiteralSyntaxNode literalNode)
                {
                    var token = literalNode.LiteralToken;
                    if (token.Kind == TokenKind.Number)
                    {
                        return ParseNumber(in token);
                    }
                    else if (token.Kind == TokenKind.Symbol)
                    {
                        var value = _constantTable.GetConstantValue(token.Text);
                        if (value == null)
                        {
                            value = 0;
                            EmitError(in token, ErrorCodes.UnknownSymbol, token.Text);
                        }
                        return value.Value;
                    }
                    else
                    {
                        EmitError(in token, ErrorCodes.InvalidOperand);
                    }
                }
                return 0;
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

        [DebuggerDisplay("Name = {Name}")]
        private class ProcedureBuilder
        {
            private readonly MemoryStream _ms = new MemoryStream();
            private BinaryWriter _bw;
            private readonly List<Label> _labels = new List<Label>();
            private readonly List<LabelReference> _labelReferences = new List<LabelReference>();
            private readonly List<int> _labelOffsets = new List<int>();
            private readonly List<ProcedureReference> _procedureReferences = new List<ProcedureReference>();

            public string Name { get; }
            public int Offset => (int)_ms.Position;
            public List<ProcedureReference> ProcedureReferences => _procedureReferences;

            public ProcedureBuilder(string name)
            {
                Name = name;
                _bw = new BinaryWriter(_ms);
            }

            public void Align()
            {
                if ((Offset & 1) == 1)
                {
                    Write((byte)OpcodeV2.Nop);
                }
            }

            public void WriteLabel(Label label)
            {
                while (_labelOffsets.Count <= label.Id)
                    _labelOffsets.Add(0);
                _labelOffsets[label.Id] = Offset;
            }

            public Label WriteLabelRef(byte size, int relativeBaseAddress)
            {
                var label = new Label(_labels.Count);
                _labels.Add(label);
                _labelReferences.Add(new LabelReference(label, Offset, size, Offset + relativeBaseAddress));
                if (size == 1)
                    Write((byte)0);
                else if (size == 2)
                    Write((ushort)0);
                else
                    throw new NotSupportedException();
                return label;
            }

            public void WriteProcedureRef(in Token token)
            {
                _procedureReferences.Add(new ProcedureReference(token, Offset));
                Write((byte)0);
            }

            public void FixLabels()
            {
                foreach (var reference in _labelReferences)
                {
                    var targetAddress = _labelOffsets[reference.Label.Id];
                    var value = targetAddress - reference.BaseAddress;
                    _ms.Position = reference.WriteOffset;
                    if (reference.WriteLength == 1)
                        Write((byte)value);
                    else if (reference.WriteLength == 2)
                        Write((short)value);
                    else
                        throw new NotSupportedException();
                }
            }

            public void FixProceduresReferences(string[] procedureNames)
            {
                foreach (var reference in _procedureReferences)
                {
                    _ms.Position = reference.WriteOffset;
                    var index = Array.IndexOf(procedureNames, reference.Name);
                    if (index == -1)
                        throw new InvalidOperationException();
                    Write((byte)index);
                }
            }

            public byte[] ToArray() => _ms.ToArray();

            public void Write(short value) => _bw.Write(value);
            public void Write(ushort value) => _bw.Write(value);
            public void Write(byte value) => _bw.Write(value);
        }

        [DebuggerDisplay("Label #{Id}")]
        private readonly struct Label
        {
            public int Id { get; }

            public Label(int id)
            {
                Id = id;
            }
        }

        private readonly struct LabelReference
        {
            public Label Label { get; }
            public int WriteOffset { get; }
            public byte WriteLength { get; }
            public int BaseAddress { get; }

            public LabelReference(Label label, int writeOffset, byte writeLength, int baseAddress)
            {
                Label = label;
                WriteOffset = writeOffset;
                WriteLength = writeLength;
                BaseAddress = baseAddress;
            }
        }

        [DebuggerDisplay("Name = {Name} WriteOffset = {WriteOffset}")]
        private readonly struct ProcedureReference
        {
            public string Name => Token.Text;
            public int WriteOffset { get; }
            public Token Token { get; }

            public ProcedureReference(Token token, int writeOffset)
            {
                Token = token;
                WriteOffset = writeOffset;
            }
        }
    }
}
