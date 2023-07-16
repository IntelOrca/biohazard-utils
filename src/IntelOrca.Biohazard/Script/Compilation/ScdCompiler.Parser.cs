using System;
using System.Collections.Generic;
using System.Linq;

namespace IntelOrca.Biohazard.Script.Compilation
{
    public partial class ScdCompiler
    {
        private class Parser
        {
            private readonly ErrorList _errors;
            private Token[] _tokens = new Token[0];
            private int _tokenIndex;

            public Parser(ErrorList errors)
            {
                _errors = errors;
            }

            public SyntaxTree BuildSyntaxTree(Token[] tokens)
            {
                _tokens = tokens
                    .Where(x => !IsTrivialToken(x.Kind))
                    .ToArray();
                var parsers = new Func<SyntaxNode?>[]
                {
                    ParseDirective,
                    ParseProcedure,
                };

                var nodes = new List<SyntaxNode>();
                while (!ParseToken(TokenKind.EOF))
                {
                    foreach (var parser in parsers)
                    {
                        var node = parser();
                        if (node != null)
                        {
                            nodes.Add(node);
                            break;
                        }
                    }
                }
                return new SyntaxTree(new BlockSyntaxNode(nodes.ToArray()));
            }

            private static bool IsTrivialToken(TokenKind kind)
            {
                return kind == TokenKind.NewLine || kind == TokenKind.Whitespace;
            }

            private ref readonly Token PeekToken()
            {
                return ref _tokens[_tokenIndex];
            }

            private ref readonly Token ReadToken()
            {
                ref var token = ref _tokens[_tokenIndex];
                if (token.Kind != TokenKind.EOF)
                    _tokenIndex++;
                return ref token;
            }

            private ref readonly Token LastToken => ref _tokens[_tokenIndex - 1];

            private bool ParseToken(TokenKind kind)
            {
                ref readonly var token = ref PeekToken();
                if (token.Kind != kind)
                    return false;

                ReadToken();
                return true;
            }

            private VersionSyntaxNode? ParseDirective()
            {
                if (!ParseToken(TokenKind.Directive))
                    return null;

                ref readonly var lastToken = ref LastToken;
                if (lastToken.Text != "#version")
                {
                    EmitError(in lastToken, ErrorCodes.UnknownDirective, lastToken.Text);
                    return null;
                }

                if (!ParseToken(TokenKind.Number))
                {
                    EmitError(in lastToken, ErrorCodes.ExpectedScdVersionNumber);
                    return null;
                }

                return new VersionSyntaxNode(LastToken);
            }

            private ProcedureSyntaxNode? ParseProcedure()
            {
                if (!ParseToken(TokenKind.Proc))
                    return null;

                if (!ParseToken(TokenKind.Symbol))
                {
                    EmitError(in LastToken, ErrorCodes.ExpectedProcedureName);
                    return null;
                }

                ref readonly var nameToken = ref LastToken;
                var block = ParseBlock();
                return new ProcedureSyntaxNode(nameToken, block);
            }

            private BlockSyntaxNode? ParseBlock()
            {
                if (!ParseExpected(TokenKind.OpenBlock))
                    return null;

                var nodes = new List<SyntaxNode>();
                while (true)
                {
                    var opcode = ParseStatement();
                    if (opcode != null)
                    {
                        nodes.Add(opcode);
                        continue;
                    }
                    ParseExpected(TokenKind.CloseBlock);
                    break;
                }

                return new BlockSyntaxNode(nodes.ToArray());
            }

            private SyntaxNode? ParseStatement()
            {
                SyntaxNode? node = ParseIfStatement();
                if (node != null)
                    return node;

                node = ParseOpcode();
                if (node != null)
                {
                    ParseExpected(TokenKind.Semicolon);
                    return node;
                }

                return null;
            }

            private IfSyntaxNode? ParseIfStatement()
            {
                if (!ParseToken(TokenKind.If))
                    return null;

                ref readonly var ifToken = ref LastToken;

                if (!ParseExpected(TokenKind.OpenParen))
                    return null;

                var condition = ParseOpcode();
                if (condition == null)
                {
                    EmitError(in ifToken, ErrorCodes.ExpectedCondition);
                    return null;
                }

                if (!ParseExpected(TokenKind.CloseParen))
                    return null;

                var ifBlock = ParseBlock();
                var elseBlock = null as BlockSyntaxNode;
                if (ParseToken(TokenKind.Else))
                {
                    elseBlock = ParseBlock();
                }
                return new IfSyntaxNode(new[] { condition }, ifBlock, elseBlock);
            }

            private OpcodeSyntaxNode? ParseOpcode()
            {
                if (!ParseToken(TokenKind.Symbol))
                    return null;

                ref readonly var opcodeToken = ref LastToken;

                if (!ParseToken(TokenKind.OpenParen))
                {
                    EmitError(in LastToken, ErrorCodes.ExpectedOpenParen);
                    return null;
                }

                var operands = new List<SyntaxNode>();
                while (true)
                {
                    var operand = ParseOperand();
                    if (operand == null)
                    {
                        EmitError(in LastToken, ErrorCodes.ExpectedOperand);
                        SkipOpcode();
                        break;
                    }
                    operands.Add(operand);
                    if (ParseToken(TokenKind.Comma))
                        continue;
                    if (ParseToken(TokenKind.CloseParen))
                        break;
                    EmitError(in LastToken, ErrorCodes.ExpectedComma);
                    SkipOpcode();
                    break;
                }

                return new OpcodeSyntaxNode(opcodeToken, operands.ToArray());
            }

            private SyntaxNode? ParseOperand()
            {
                ref readonly var token = ref PeekToken();
                if (token.Kind != TokenKind.Symbol &&
                    token.Kind != TokenKind.Number)
                {
                    return null;
                }

                token = ref ReadToken();
                return new LiteralSyntaxNode(token);
            }

            private void SkipOpcode()
            {
                while (true)
                {
                    ref readonly var token = ref PeekToken();
                    if (token.Kind == TokenKind.EOF)
                    {
                        return;
                    }
                    else if (token.Kind == TokenKind.Semicolon)
                    {
                        ReadToken();
                        return;
                    }
                    else if (token.Kind == TokenKind.OpenBlock ||
                             token.Kind == TokenKind.CloseBlock)
                    {
                        return;
                    }
                }
            }

            private bool ParseExpected(TokenKind kind)
            {
                if (ParseToken(kind))
                {
                    return true;
                }
                else
                {
                    var errorCode = kind switch
                    {
                        TokenKind.OpenParen => ErrorCodes.ExpectedOpenParen,
                        TokenKind.CloseParen => ErrorCodes.ExpectedCloseParen,
                        TokenKind.OpenBlock => ErrorCodes.ExpectedOpenBlock,
                        TokenKind.CloseBlock => ErrorCodes.ExpectedCloseBlock,
                        TokenKind.Semicolon => ErrorCodes.ExpectedSemicolon,
                        _ => throw new NotImplementedException(),
                    };
                    EmitError(in LastToken, errorCode);
                    return false;
                }
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

        private class SyntaxTree
        {
            public BlockSyntaxNode Root { get; }

            public SyntaxTree() : this(new BlockSyntaxNode(new SyntaxNode[0]))
            {
            }

            public SyntaxTree(BlockSyntaxNode root)
            {
                Root = root;
            }
        }

        private class SyntaxNode
        {
            public virtual IEnumerable<SyntaxNode> Children
            {
                get
                {
                    yield break;
                }
            }
        }

        private class VersionSyntaxNode : SyntaxNode
        {
            public Token VersionToken { get; }

            public VersionSyntaxNode(Token versionToken)
            {
                VersionToken = versionToken;
            }
        }

        private class ProcedureSyntaxNode : SyntaxNode
        {
            public Token NameToken { get; }
            public BlockSyntaxNode? Block { get; }

            public ProcedureSyntaxNode(Token nameToken, BlockSyntaxNode? block)
            {
                NameToken = nameToken;
                Block = block;
            }

            public override IEnumerable<SyntaxNode> Children
            {
                get
                {
                    if (Block != null)
                        yield return Block;
                }
            }
        }

        private class BlockSyntaxNode : SyntaxNode
        {
            public SyntaxNode[] Statements { get; }

            public BlockSyntaxNode(SyntaxNode[] statements)
            {
                Statements = statements;
            }

            public override IEnumerable<SyntaxNode> Children => Statements;
        }

        private class IfSyntaxNode : SyntaxNode
        {
            public SyntaxNode[] Conditions { get; }
            public BlockSyntaxNode? IfBlock { get; }
            public BlockSyntaxNode? ElseBlock { get; }

            public IfSyntaxNode(SyntaxNode[] conditions, BlockSyntaxNode? ifBlock, BlockSyntaxNode? elseBlock)
            {
                Conditions = conditions;
                IfBlock = ifBlock;
                ElseBlock = elseBlock;
            }

            public override IEnumerable<SyntaxNode> Children
            {
                get
                {
                    foreach (var condition in Conditions)
                        yield return condition;
                    if (IfBlock != null)
                        yield return IfBlock;
                    if (ElseBlock != null)
                        yield return ElseBlock;
                }
            }
        }

        private class OpcodeSyntaxNode : SyntaxNode
        {
            public Token OpcodeToken { get; }
            public SyntaxNode[] Operands { get; }

            public OpcodeSyntaxNode(Token opcodeToken, SyntaxNode[] operands)
            {
                OpcodeToken = opcodeToken;
                Operands = operands;
            }

            public override IEnumerable<SyntaxNode> Children => Operands;
        }

        private class LiteralSyntaxNode : SyntaxNode
        {
            public Token LiteralToken { get; }

            public LiteralSyntaxNode(Token literalToken)
            {
                LiteralToken = literalToken;
            }
        }
    }
}
