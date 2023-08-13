using System;
using System.IO;
using System.Linq;
using IntelOrca.Biohazard;
using IntelOrca.Biohazard.Extensions;
using IntelOrca.Biohazard.Room;
using IntelOrca.Biohazard.Script;
using IntelOrca.Biohazard.Script.Compilation;

namespace IntelOrca.Scd
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var paths = args.Where(x => !x.StartsWith("-")).ToArray();
            var rdtPath = paths.FirstOrDefault();
            if (rdtPath == null)
            {
                return PrintUsage();
            }

            var bioVersion = BioVersion.Biohazard2;
            var version = GetOption(args, "-v");
            if (version != null)
            {
                if (!int.TryParse(version, out var parsedVersion) || parsedVersion < 1 || parsedVersion > 3)
                {
                    Console.Error.WriteLine("Invalid version");
                    return 1;
                }
                if (parsedVersion == 1)
                    bioVersion = BioVersion.Biohazard1;
                else if (parsedVersion == 3)
                    bioVersion = BioVersion.Biohazard3;
            }

            if (args.Contains("-x"))
            {
                var rdtFile = Biohazard.Room.Rdt.FromFile(bioVersion, rdtPath);
                if (bioVersion == BioVersion.Biohazard1)
                {
                    var rdt1 = (Rdt1)rdtFile;
                    var eventScd = rdt1.EventSCD;
                    for (int i = 0; i < eventScd.Count; i++)
                    {
                        eventScd[i].WriteToFile($"event_{i:X2}.scd");
                    }
                }
                else
                {
                    var rdt2 = (Rdt2)rdtFile;
                    rdt2.SCDINIT.Data.WriteToFile("init.scd");
                    if (bioVersion != BioVersion.Biohazard3)
                        rdt2.SCDMAIN.Data.WriteToFile("main.scd");
                }
                return 0;
            }
            else if (args.Contains("-d"))
            {
                if (rdtPath.EndsWith(".rdt", StringComparison.OrdinalIgnoreCase))
                {
                    var rdtFile = Biohazard.Room.Rdt.FromFile(bioVersion, rdtPath);
                    foreach (var listing in new[] { false, true })
                    {
                        if (listing && !args.Contains("--list"))
                            continue;

                        var script = rdtFile.DisassembleScd(listing);
                        var extension = listing ? ".lst" : ".s";
                        File.WriteAllText(Path.ChangeExtension(Path.GetFileName(rdtPath), extension), script);
                    }
                }
                else if (rdtPath.EndsWith(".scd", StringComparison.OrdinalIgnoreCase))
                {
                    var kind = args.Contains("--main") ? BioScriptKind.Main : BioScriptKind.Init;
                    var scd = new ScdProcedureList(bioVersion, File.ReadAllBytes(rdtPath));
                    var s = Diassemble(bioVersion, kind, scd);
                    var sPath = Path.ChangeExtension(rdtPath, ".s");
                    File.WriteAllText(sPath, s);
                    var lst = Diassemble(bioVersion, kind, scd, listing: true);
                    var lstPath = Path.ChangeExtension(rdtPath, ".lst");
                    File.WriteAllText(lstPath, lst);
                }
                return 0;
            }
            else if (args.Contains("--decompile"))
            {
                if (rdtPath.EndsWith(".rdt", StringComparison.OrdinalIgnoreCase))
                {
                    var rdtFile = Biohazard.Room.Rdt.FromFile(bioVersion, rdtPath);
                    var script = IntelOrca.Biohazard.Extensions.RdtExtensions.DisassembleScd(rdtFile);
                    File.WriteAllText(Path.ChangeExtension(Path.GetFileName(rdtPath), ".bio"), script);
                    return 0;
                }
                else
                {
                    Console.Error.WriteLine("Only RDT files can be decompiled.");
                    return 1;
                }
            }
            else
            {
                if (CreateGenerator(rdtPath) is IScdGenerator generator)
                {
                    var result = generator.Generate(new SimpleFileIncluder(), rdtPath);
                    if (result == 0)
                    {
                        foreach (var op in generator.Operations)
                        {
                            if (!(op is ScdRdtEditOperation scdEdit))
                                continue;

                            if (scdEdit.Kind == BioScriptKind.Init)
                            {
                                var scdPath = Path.ChangeExtension(rdtPath, "init.scd");
                                File.WriteAllBytes(scdPath, scdEdit.Data.Data.ToArray());
                            }
                            if (scdEdit.Kind == BioScriptKind.Main)
                            {
                                var scdPath = Path.ChangeExtension(rdtPath, "main.scd");
                                File.WriteAllBytes(scdPath, scdEdit.Data.Data.ToArray());
                            }
                        }
                    }
                    else
                    {
                        foreach (var error in generator.Errors.Errors)
                        {
                            Console.WriteLine($"{error.Path}({error.Line + 1},{error.Column + 1}): error {error.ErrorCodeString}: {error.Message}");
                        }
                    }
                }
                else
                {
                    var rdtFile = Biohazard.Room.Rdt.FromFile(bioVersion, rdtPath).ToBuilder();
                    if (paths.Length >= 2)
                    {
                        var inPath = Path.GetFullPath(paths[1]);
                        if (inPath.EndsWith(".scd", StringComparison.OrdinalIgnoreCase))
                        {

                        }
                        else if (CreateGenerator(inPath) is IScdGenerator generator2)
                        {
                            var result = generator2.Generate(new SimpleFileIncluder(), inPath);
                            if (result == 0)
                            {
                                foreach (var op in generator2.Operations)
                                {
                                    op.Perform(rdtFile);
                                }
                            }
                            else
                            {
                                foreach (var error in generator2.Errors.Errors)
                                {
                                    Console.WriteLine(error);
                                }
                            }
                        }
                    }
                    else
                    {

                        var initScdPath = GetOption(args, "--init");
                        var mainScdPath = GetOption(args, "--main");
                        if (initScdPath != null)
                        {
                            var initScd = new ScdProcedureList(bioVersion, File.ReadAllBytes(initScdPath));
                            new ScdRdtEditOperation(BioScriptKind.Init, initScd).Perform(rdtFile);
                        }
                        if (mainScdPath != null)
                        {
                            var mainScd = new ScdProcedureList(bioVersion, File.ReadAllBytes(mainScdPath));
                            new ScdRdtEditOperation(BioScriptKind.Main, mainScd).Perform(rdtFile);
                        }
                    }

                    var outRdt = rdtFile.ToRdt();
                    var outPath = GetOption(args, "-o");
                    if (outPath != null)
                    {
                        outRdt.Data.WriteToFile(outPath);
                    }
                    else
                    {
                        outRdt.Data.WriteToFile(rdtPath + ".patched");
                    }
                }
                return 0;
            }
        }

        private static string Diassemble(BioVersion version, BioScriptKind kind, ScdProcedureList scd, bool listing = false)
        {
            var scdReader = new ScdReader();
            return scdReader.Diassemble(scd, version, kind, listing);
        }

        private static IScdGenerator CreateGenerator(string inputPath)
        {
            if (inputPath.EndsWith(".s", StringComparison.OrdinalIgnoreCase))
            {
                return new ScdAssembler();
            }
            else if (inputPath.EndsWith(".bio", StringComparison.OrdinalIgnoreCase))
            {
                return new ScdCompiler();
            }
            return null;
        }

        private static string GetOption(string[] args, string name)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == name)
                {
                    if (i + 1 >= args.Length)
                        return null;
                    return args[i + 1];
                }
            }
            return null;
        }

        private static int PrintUsage()
        {
            Console.WriteLine("Resident Evil SCD assembler / diassembler");
            Console.WriteLine("usage: scd -x <rdt>");
            Console.WriteLine("       scd -d <rdt | scd>");
            Console.WriteLine("       scd [-o <rdt>] <rdt> [s] | [--init <.scd | .s>] [--main <scd | s>]");
            return 1;
        }
    }
}
