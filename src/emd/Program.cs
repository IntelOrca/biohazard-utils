﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using IntelOrca.Biohazard;
using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.Model;

namespace IntelOrca.Emd
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                var inputPaths = GetArguments(args);
                if (inputPaths.Length == 0)
                    return PrintUsage();

                var version = BioVersion.Biohazard3;
                var versionArg = GetOption(args, "-v");
                if (versionArg == "2")
                    version = BioVersion.Biohazard2;
                else if (versionArg == "3")
                    version = BioVersion.Biohazard3;
                else if (versionArg != null)
                    return PrintUsage();

                var outputPath = GetOption(args, "-o");
                if (outputPath == null)
                {
                    outputPath = Path.ChangeExtension(Path.GetFileName(inputPaths[0]), ".obj");
                }

                var outputEmdPath = Path.ChangeExtension(outputPath, ".emd");
                var outputPldPath = Path.ChangeExtension(outputPath, ".pld");
                var outputObjPath = Path.ChangeExtension(outputPath, ".obj");
                var outputPngPath = Path.ChangeExtension(outputPath, ".png");
                var outputMd1Path = Path.ChangeExtension(outputPath, ".md1");
                var outputMd2Path = Path.ChangeExtension(outputPath, ".md2");
                var outputTimPath = Path.ChangeExtension(outputPath, ".tim");

                var inputEmdPath = inputPaths.FirstOrDefault(x => x.EndsWith(".emd", StringComparison.OrdinalIgnoreCase));
                if (inputEmdPath == null)
                    inputEmdPath = inputPaths.FirstOrDefault(x => x.EndsWith(".pld", StringComparison.OrdinalIgnoreCase));
                if (inputEmdPath == null)
                    return PrintUsage();

                var inputMd1Path = inputPaths.FirstOrDefault(x => x.EndsWith(".md1", StringComparison.OrdinalIgnoreCase));
                var inputMd2Path = inputPaths.FirstOrDefault(x => x.EndsWith(".md2", StringComparison.OrdinalIgnoreCase));
                var inputObjPath = inputPaths.FirstOrDefault(x => x.EndsWith(".obj", StringComparison.OrdinalIgnoreCase));
                var inputPngPath = inputPaths.FirstOrDefault(x => x.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
                var inputTimPath = Path.ChangeExtension(inputEmdPath, ".tim");

                var importing = inputMd2Path != null || inputObjPath != null || inputPngPath != null;

                // Import EMD/PLD
                ModelFile modelFile;
                TimFile timFile = null;
                if (inputEmdPath.EndsWith(".emd", StringComparison.OrdinalIgnoreCase))
                {
                    modelFile = new EmdFile(version, inputEmdPath);
                    if (File.Exists(inputTimPath))
                    {
                        timFile = new TimFile(inputTimPath);
                    }
                }
                else if (inputEmdPath.EndsWith(".pld", StringComparison.OrdinalIgnoreCase))
                {
                    var pldFile = new PldFile(version, inputEmdPath);
                    modelFile = pldFile;
                    timFile = pldFile.Tim;
                }
                else
                {
                    Console.Error.WriteLine("An .emd or .pld file must be imported.");
                    return 1;
                }

                if (importing)
                {
                    if (inputMd2Path != null)
                    {
                        // Import MD1/MD2
                        if (version == BioVersion.Biohazard2)
                            modelFile.Md1 = new Md1(File.ReadAllBytes(inputMd1Path));
                        else
                            modelFile.Md2 = new Md2(File.ReadAllBytes(inputMd2Path));
                    }
                    else if (inputObjPath != null)
                    {
                        // Import OBJ
                        var objImporter = new ObjImporter();
                        objImporter.Import(modelFile.Version, inputObjPath, 3);
                    }
                    if (inputPngPath != null)
                    {
                        var importedTimFile = ImportTimFile(inputPngPath);
                        if (modelFile is PldFile pldFile)
                        {
                            pldFile.Tim = importedTimFile;
                        }
                        else
                        {
                            importedTimFile.Save(outputTimPath);
                        }
                    }
                    {
                        if (modelFile is PldFile pldFile)
                            pldFile.Save(outputPldPath);
                        else if (modelFile is EmdFile emdFile)
                            emdFile.Save(outputEmdPath);
                    }
                }
                else
                {
                    var objExporter = new ObjExporter();
                    if (version == BioVersion.Biohazard2)
                    {
                        var meshConverter = new MeshConverter();
                        var md2 = meshConverter.ConvertMesh(modelFile.Md1, BioVersion.Biohazard3);

                        File.WriteAllBytes(outputMd1Path, modelFile.Md1.Data.ToArray());
                        File.WriteAllBytes(outputMd2Path, md2.Data.ToArray());
                        objExporter.Export(modelFile.Md1, outputObjPath, 3);
                    }
                    else
                    {
                        File.WriteAllBytes(outputMd2Path, modelFile.Md2.Data.ToArray());
                        objExporter.Export(modelFile.Md2, outputObjPath, 3);
                    }
                    timFile?.ToBitmap((x, y) => x / 128).Save(outputPngPath);
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static TimFile ImportTimFile(string path)
        {
            using (var bitmap = (Bitmap)Bitmap.FromFile(path))
            {
                var timFile = new TimFile(bitmap.Width, bitmap.Height, 8);
                var clutIndex = 0;
                for (int x = 0; x < bitmap.Width; x += 128)
                {
                    var srcBounds = new Rectangle(x, 0, Math.Min(bitmap.Width - x, 128), bitmap.Height);
                    var colours = GetColours(bitmap, srcBounds);
                    timFile.SetPalette(clutIndex, colours);
                    timFile.ImportBitmap(bitmap, srcBounds, x, 0, clutIndex);
                    clutIndex++;
                }
                return timFile;
            }
        }

        private static ushort[] GetColours(Bitmap bitmap, Rectangle area)
        {
            var coloursList = new ushort[256];
            var coloursIndex = 1;

            var colours = new HashSet<ushort>();
            for (int y = area.Top; y < area.Bottom; y++)
            {
                for (int x = area.Left; x < area.Right; x++)
                {
                    var c32 = bitmap.GetPixel(x, y);
                    var c16 = TimFile.Convert32to16((uint)c32.ToArgb());
                    if (colours.Add(c16))
                    {
                        coloursList[coloursIndex++] = c16;
                        if (coloursIndex == 256)
                        {
                            return coloursList;
                        }
                    }
                }
            }
            return coloursList;
        }

        private static string[] GetArguments(string[] args)
        {
            var result = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-"))
                {
                    i++;
                }
                else
                {
                    result.Add(args[i]);
                }
            }
            return result.ToArray();
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
            Console.WriteLine("Resident Evil EMD import / export");
            Console.WriteLine("usage: emd [options] PL00.PLD");
            Console.WriteLine("       emd [options] EM52.EMD [-o EM52.obj]");
            Console.WriteLine("       emd [options] EM52.EMD my.obj my.png [-o custom.emd]");
            Console.WriteLine();
            Console.WriteLine("options:");
            Console.WriteLine("    -v <version>     2 for RE2, 3 for RE3.");

            return 1;
        }
    }
}
