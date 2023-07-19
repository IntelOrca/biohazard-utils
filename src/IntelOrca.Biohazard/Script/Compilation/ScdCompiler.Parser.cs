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
                    var foundParser = false;
                    foreach (var parser in parsers)
                    {
                        var node = parser();
                        if (node != null)
                        {
                            nodes.Add(node);
                            foundParser = true;
                            break;
                        }
                    }
                    if (!foundParser)
                    {
                        if (_errors.Count == 0)
                        {
                            EmitError(in LastToken, ErrorCodes.ParserFailure);
                        }
                        break;
                    }
                }
                return new SyntaxTree(new BlockSyntaxNode(nodes.ToArray()));
            }

            private static bool IsTrivialToken(TokenKind kind)
            {
                return kind == TokenKind.NewLine ||
                    kind == TokenKind.Whitespace ||
                    kind == TokenKind.Comment;
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
                var parsers = new Func<SyntaxNode?>[]
                {
                    ParseIfStatement,
                    ParseWhileStatement,
                    ParseDoWhileStatement,
                    ParseForkStatement,
                    ParseOpcode,
                };
                foreach (var parser in parsers)
                {
                    if (parser() is SyntaxNode node)
                    {
                        if (node is OpcodeSyntaxNode)
                            ParseExpected(TokenKind.Semicolon);
                        return node;
                    }
                }
                return null;
            }

            private ForkSyntaxNode? ParseForkStatement()
            {
                if (!ParseToken(TokenKind.Fork))
                    return null;

                ref readonly var token = ref PeekToken();
                if (token.Kind != TokenKind.Symbol)
                {
                    EmitError(in token, ErrorCodes.ExpectedProcedureName);
                    return null;
                }

                ReadToken();
                return new ForkSyntaxNode(token);
            }

            private IfSyntaxNode? ParseIfStatement()
            {
                if (!ParseToken(TokenKind.If))
                    return null;

                var condition = ParseCondition();
                if (condition == null)
                    return null;

                var ifBlock = ParseBlock();
                var elseBlock = null as BlockSyntaxNode;
                if (ParseToken(TokenKind.Else))
                {
                    elseBlock = ParseBlock();
                }
                return new IfSyntaxNode(condition, ifBlock, elseBlock);
            }

            private WhileSyntaxNode? ParseWhileStatement()
            {
                if (!ParseToken(TokenKind.While))
                    return null;

                var condition = ParseCondition();
                if (condition == null)
                    return null;

                var block = ParseBlock();
                return new WhileSyntaxNode(condition, block);
            }

            private DoWhileSyntaxNode? ParseDoWhileStatement()
            {
                if (!ParseToken(TokenKind.Do))
                    return null;

                var block = ParseBlock();

                if (!ParseExpected(TokenKind.While))
                    return null;

                var condition = ParseCondition();
                if (condition == null)
                    return null;

                if (!ParseExpected(TokenKind.Semicolon))
                    return null;

                return new DoWhileSyntaxNode(block, condition);
            }

            private ConditionalExpressionSyntaxNode? ParseCondition()
            {
                if (!ParseExpected(TokenKind.OpenParen))
                    return null;

                var condition = ParseOpcode();
                if (condition == null)
                {
                    EmitError(in LastToken, ErrorCodes.ExpectedCondition);
                    return null;
                }

                if (!ParseExpected(TokenKind.CloseParen))
                    return null;

                return new ConditionalExpressionSyntaxNode(new[] { condition });
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
                    if (operand != null)
                    {
                        operands.Add(operand);
                        if (ParseToken(TokenKind.Comma))
                            continue;
                    }
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
                    token.Kind != TokenKind.Number &&
                    token.Kind != TokenKind.OpenParen)
                {
                    return null;
                }
                return ParseExpression();
            }

            private ExpressionSyntaxNode? ParseExpression()
            {
                ExpressionSyntaxNode? expression;
                ref readonly var token = ref PeekToken();
                switch (token.Kind)
                {
                    case TokenKind.OpenParen:
                        token = ref ReadToken();
                        expression = ParseExpression();
                        if (expression == null)
                            return null;

                        token = ref PeekToken();
                        if (token.Kind != TokenKind.CloseParen)
                        {
                            EmitError(in token, ErrorCodes.ExpectedCloseParen);
                            return null;
                        }
                        ReadToken();
                        break;
                    case TokenKind.Symbol:
                    case TokenKind.Number:
                        token = ref ReadToken();
                        expression = new LiteralSyntaxNode(token);
                        break;
                    default:
                        EmitError(in token, ErrorCodes.InvalidExpression);
                        return null;
                }

                ref readonly var nextToken = ref PeekToken();
                if (IsBinaryOperator(nextToken.Kind))
                {
                    ReadToken();
                    var rhs = ParseExpression();
                    if (rhs == null)
                        return null;

                    var kind = nextToken.Kind switch
                    {
                        TokenKind.Plus => ExpressionKind.Add,
                        TokenKind.Minus => ExpressionKind.Subtract,
                        TokenKind.Asterisk => ExpressionKind.Multiply,
                        TokenKind.Pipe => ExpressionKind.BitwiseOr,
                        TokenKind.Ampersand => ExpressionKind.BitwiseAnd,
                        TokenKind.LShift => ExpressionKind.LogicalShiftLeft,
                        TokenKind.RShift => ExpressionKind.ArithmeticShiftRight,
                        _ => throw new NotSupportedException()
                    };
                    return new BinaryExpressionSyntaxNode(kind, expression, rhs);
                }
                else
                {
                    return expression;
                }
            }

            private static bool IsBinaryOperator(TokenKind kind) =>
                kind switch
                {
                    TokenKind.Plus => true,
                    TokenKind.Minus => true,
                    TokenKind.Asterisk => true,
                    TokenKind.Pipe => true,
                    TokenKind.Ampersand => true,
                    TokenKind.LShift => true,
                    TokenKind.RShift => true,
                    _ => false
                };

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
                    ReadToken();
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
                        TokenKind.While => ErrorCodes.ExpectedWhile,
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

        private class ForkSyntaxNode : SyntaxNode
        {
            public Token ProcedureToken { get; }

            public ForkSyntaxNode(Token procedureToken)
            {
                ProcedureToken = procedureToken;
            }
        }

        private class IfSyntaxNode : SyntaxNode
        {
            public ConditionalExpressionSyntaxNode Condition { get; }
            public BlockSyntaxNode? IfBlock { get; }
            public BlockSyntaxNode? ElseBlock { get; }

            public IfSyntaxNode(ConditionalExpressionSyntaxNode condition, BlockSyntaxNode? ifBlock, BlockSyntaxNode? elseBlock)
            {
                Condition = condition;
                IfBlock = ifBlock;
                ElseBlock = elseBlock;
            }

            public override IEnumerable<SyntaxNode> Children
            {
                get
                {
                    if (Condition != null)
                        yield return Condition;
                    if (IfBlock != null)
                        yield return IfBlock;
                    if (ElseBlock != null)
                        yield return ElseBlock;
                }
            }
        }

        private class WhileSyntaxNode : SyntaxNode
        {
            public ConditionalExpressionSyntaxNode Condition { get; }
            public BlockSyntaxNode? Block { get; }

            public WhileSyntaxNode(ConditionalExpressionSyntaxNode condition, BlockSyntaxNode? block)
            {
                Condition = condition;
                Block = block;
            }

            public override IEnumerable<SyntaxNode> Children
            {
                get
                {
                    if (Condition != null)
                        yield return Condition;
                    if (Block != null)
                        yield return Block;
                }
            }
        }

        private class DoWhileSyntaxNode : SyntaxNode
        {
            public BlockSyntaxNode? Block { get; }
            public ConditionalExpressionSyntaxNode Condition { get; }

            public DoWhileSyntaxNode(BlockSyntaxNode? block, ConditionalExpressionSyntaxNode condition)
            {
                Block = block;
                Condition = condition;
            }

            public override IEnumerable<SyntaxNode> Children
            {
                get
                {
                    if (Block != null)
                        yield return Block;
                    if (Condition != null)
                        yield return Condition;
                }
            }
        }

        private class ConditionalExpressionSyntaxNode : SyntaxNode
        {
            public SyntaxNode[] Conditions { get; }

            public ConditionalExpressionSyntaxNode(SyntaxNode[] conditions)
            {
                Conditions = conditions;
            }

            public override IEnumerable<SyntaxNode> Children => Conditions;
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

        private abstract class ExpressionSyntaxNode : SyntaxNode
        {
            public abstract ExpressionKind Kind { get; }
        }

        private class BinaryExpressionSyntaxNode : ExpressionSyntaxNode
        {
            public override ExpressionKind Kind { get; }
            public ExpressionSyntaxNode Left { get; }
            public ExpressionSyntaxNode Right { get; }

            public BinaryExpressionSyntaxNode(ExpressionKind kind, ExpressionSyntaxNode left, ExpressionSyntaxNode right)
            {
                Kind = kind;
                Left = left;
                Right = right;
            }
        }

        private class LiteralSyntaxNode : ExpressionSyntaxNode
        {
            public override ExpressionKind Kind => ExpressionKind.Literal;
            public Token LiteralToken { get; }

            public LiteralSyntaxNode(Token literalToken)
            {
                LiteralToken = literalToken;
            }
        }

        public enum ExpressionKind
        {
            Literal,
            Add,
            Subtract,
            Multiply,
            BitwiseOr,
            BitwiseAnd,
            LogicalShiftLeft,
            ArithmeticShiftRight,
        }
    }
}
