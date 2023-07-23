using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IntelOrca.Biohazard.Model;

namespace IntelOrca.Biohazard.Script.Compilation
{
    public partial class ScdCompiler : IScdGenerator
    {
        public ErrorList Errors { get; } = new ErrorList();
        public byte[] OutputInit { get; private set; } = new byte[0];
        public byte[] OutputMain { get; private set; } = new byte[0];
        public string?[] Messages { get; private set; } = new string[0];
        public RdtAnimation?[] Animations { get; private set; } = new RdtAnimation[0];

        public int Generate(IFileIncluder includer, string path)
        {
            var lexer = new Lexer(includer, Errors);
            var tokens = lexer.GetTokens(path).ToArray();
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
            Messages = generator.Messages.ToArray();

            var animations = new List<RdtAnimation?>();
            foreach (var animation in generator.Animations)
            {
                if (animation == null)
                {
                    animations.Add(null);
                    continue;
                }

                var eddPath = Path.Combine(Path.GetDirectoryName(path), animation);
                try
                {
                    var emrPath = Path.ChangeExtension(eddPath, ".emr");
                    var edd = new Edd(File.ReadAllBytes(eddPath));
                    var emr = new Emr(BioVersion.Biohazard2, File.ReadAllBytes(emrPath));
                    animations.Add(new RdtAnimation((EmrFlags)15, edd, emr));
                }
                catch (Exception)
                {
                    Errors.AddError("", 0, 0, ErrorCodes.FileNotFound, path);
                }
            }
            Animations = animations.ToArray();
            return 0;
        }
    }
}
