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
            var result = generator.Generate(syntaxTree);
            if (result != 0)
                return result;

            OutputInit = generator.OutputInit;
            OutputMain = generator.OutputMain;
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
    }
}
