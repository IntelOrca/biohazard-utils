using System;
using System.IO;
using System.Reflection;
using IntelOrca.Biohazard;

namespace emdui
{
    internal class BioRandExporter
    {
        private Project _project;
        private string _re2emdPath;
        private string _re2pldPath;
        private string _re3emdPath;
        private string _re3pldPath;

        private string GetTemplatePath()
        {
            var exePath = Assembly.GetEntryAssembly().Location;
            var exeDir = Path.GetDirectoryName(exePath);
            var templatePath = Path.Combine(exeDir, "template");
            return templatePath;
        }

        private void CopyTemplate(string baseCharacter, string targetPath, string characterName)
        {
            var templatePath = GetTemplatePath();

            Directory.CreateDirectory(Path.Combine(targetPath, "voice", characterName));
            File.Copy(Path.Combine(templatePath, "voice", "readme.txt"), Path.Combine(targetPath, "voice", characterName, "readme.txt"), true);
            CopyDirectory(Path.Combine(templatePath, "hurt", baseCharacter), Path.Combine(targetPath, "hurt", characterName));
            File.Copy(Path.Combine(templatePath, "hurt", "readme.txt"), Path.Combine(targetPath, "hurt", characterName, "readme.txt"), true);
            CopyDirectory(Path.Combine(templatePath, "re2", "emd", baseCharacter), Path.Combine(targetPath, "re2", "emd", characterName));
            var pld = baseCharacter == "leon" ? "pld0" : "pld1";
            CopyDirectory(Path.Combine(templatePath, "re2", pld, baseCharacter), Path.Combine(targetPath, "re2", pld, characterName));
            CopyDirectory(Path.Combine(templatePath, "re3", "emd", "leon"), Path.Combine(targetPath, "re3", "emd", characterName));
            CopyDirectory(Path.Combine(templatePath, "re3", "pld0", "leon"), Path.Combine(targetPath, "re3", "pld0", characterName));
        }

        public void Import(Project project)
        {
            _project = project;
        }

        public void Export(string baseCharacter, string characterName, string path)
        {
            CopyTemplate(baseCharacter, path, characterName);
            if (baseCharacter == "leon")
            {
                _re2emdPath = Path.Combine(path, "re2", "emd", characterName, "EM050.EMD");
                _re2pldPath = Path.Combine(path, "re2", "pld0", characterName, "PL00.PLD");
            }
            else
            {
                _re2emdPath = Path.Combine(path, "re2", "emd", characterName, "EM051.EMD");
                _re2pldPath = Path.Combine(path, "re2", "pld1", characterName, "PL01.PLD");
            }
            _re3emdPath = Path.Combine(path, "re3", "emd", characterName, "EM50.EMD");
            _re3pldPath = Path.Combine(path, "re3", "pld0", characterName, "PL00.PLD");

            ExportToPld2();
            ExportToEmd2();
            ExportToPld3();
            ExportToEmd3();
        }

        private void ExportToPld2()
        {
            if (_project.MainModel is PldFile pldFile && pldFile.Version == BioVersion.Biohazard2)
            {
                pldFile.Save(_re2pldPath);
            }
            else
            {
                var baseEmr = _project.MainModel.GetEmr(0);
                var baseMd1 = _project.MainModel.Md1;
                var baseTim = _project.MainTexture;
                var pld = new PldFile(BioVersion.Biohazard2, _re2pldPath);
                pld.SetEmr(0, pld.GetEmr(0).WithSkeleton(baseEmr));
                pld.Md1 = OverlayMd1(pld.Md1, baseMd1);
                pld.Tim = baseTim;
                pld.Save(_re2pldPath);
            }
        }

        private void ExportToEmd2()
        {
            var baseEmr = _project.MainModel.GetEmr(0);
            var baseMd1 = _project.MainModel.Md1;
            var baseTim = _project.MainTexture;
            var emd = new EmdFile(BioVersion.Biohazard2, _re2emdPath);
            emd.SetEmr(0, emd.GetEmr(0).WithSkeleton(baseEmr));
            emd.Md1 = OverlayMd1(emd.Md1, baseMd1);
            emd.Save(_re2emdPath);
            baseTim.Save(Path.ChangeExtension(_re2emdPath, ".TIM"));
        }

        private void ExportToPld3()
        {
            var baseEmr = ConvertEmr(_project.MainModel.GetEmr(0), BioVersion.Biohazard2, BioVersion.Biohazard3);
            var baseMd2 = ConvertToMd2(_project.MainModel.Md1);
            var baseTim = _project.MainTexture;
            var pld = new PldFile(BioVersion.Biohazard3, _re3pldPath);
            pld.SetEmr(0, pld.GetEmr(0).WithSkeleton(baseEmr));
            pld.Md2 = OverlayMd2(pld.Md2, baseMd2);

            var targetTim = pld.Tim;
            CopyPage(targetTim, baseTim, 0);
            CopyPage(targetTim, baseTim, 1);
            FixColoursForPage1(targetTim);
            pld.Tim = targetTim;

            pld.Save(_re3pldPath);
        }

        private void ExportToEmd3()
        {
            var baseEmr = ConvertEmr(_project.MainModel.GetEmr(0), BioVersion.Biohazard2, BioVersion.Biohazard3);
            var baseMd2 = ConvertToMd2(_project.MainModel.Md1);
            var baseTim = _project.MainTexture;
            var emd = new EmdFile(BioVersion.Biohazard3, _re3emdPath);
            emd.SetEmr(0, emd.GetEmr(0).WithSkeleton(baseEmr));
            emd.Md2 = OverlayMd2(emd.Md2, baseMd2);
            emd.Save(_re3emdPath);
            baseTim.Save(Path.ChangeExtension(_re3emdPath, ".TIM"));
        }

        private Md1 OverlayMd1(Md1 dst, Md1 src)
        {
            var srcBuilder = src.ToBuilder();
            var dstBuilder = dst.ToBuilder();
            for (var i = 0; i < dstBuilder.Parts.Count; i++)
            {
                if (srcBuilder.Parts.Count > i)
                {
                    dstBuilder.Parts[i] = srcBuilder.Parts[i];
                }
            }
            return dstBuilder.ToMd1();
        }

        private Md2 OverlayMd2(Md2 dst, Md2 src)
        {
            var srcBuilder = src.ToBuilder();
            var dstBuilder = dst.ToBuilder();
            for (var i = 0; i < dstBuilder.Parts.Count; i++)
            {
                if (srcBuilder.Parts.Count > i)
                {
                    dstBuilder.Parts[i] = srcBuilder.Parts[i];
                }
            }
            return dstBuilder.ToMd2();
        }

        private Md2 ConvertToMd2(Md1 md1)
        {
            return md1.ToMd2();
        }

        private Emr ConvertEmr(Emr emr, BioVersion sourceVersion, BioVersion targetVersion)
        {
            if (sourceVersion == targetVersion)
                return emr;

            if (sourceVersion == BioVersion.Biohazard2 && targetVersion == BioVersion.Biohazard3)
            {
                var map2to3 = new[]
                {
                    0, 8, 9, 10, 11, 12, 13, 14, 1, 2, 3, 4, 5, 6, 7
                };
                var emrBuilder = emr.ToBuilder();
                for (var i = 0; i < map2to3.Length; i++)
                {
                    var srcPartIndex = i;
                    var dstPartIndex = map2to3[i];
                    var src = emr.GetRelativePosition(srcPartIndex);
                    emrBuilder.RelativePositions[dstPartIndex] = src;
                }
                return emrBuilder.ToEmr();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static void CopyPage(TimFile dst, TimFile src, int page)
        {
            dst.SetPalette(page, src.GetPalette(page));
            var xOffset = 128 * page;
            for (var y = 0; y < 256; y++)
            {
                for (var x = 0; x < 128; x++)
                {
                    var srcP = src.GetRawPixel(xOffset + x, y);
                    dst.SetRawPixel(xOffset + x, y, srcP);
                }
            }
        }

        private static void FixColoursForPage1(TimFile tim)
        {
            var pageToPaletteTrim = 1;
            var palette = tim.GetPalette(pageToPaletteTrim);
            var targetPalette = new byte[palette.Length];
            for (var i = 0; i < palette.Length; i++)
            {
                if (i >= 240)
                {
                    var oldValue = TimFile.Convert16to32(palette[i]);
                    targetPalette[i] = tim.ImportPixel(pageToPaletteTrim, 0, 240, oldValue);
                }
                else
                {
                    targetPalette[i] = (byte)i;
                }
            }

            var xStart = pageToPaletteTrim * 128;
            for (var y = 0; y < tim.Height; y++)
            {
                for (var x = 0; x < 128; x++)
                {
                    var p = tim.GetRawPixel(xStart + x, y);
                    if (p > 239)
                    {
                        var newP = targetPalette[p];
                        tim.SetRawPixel(xStart + x, y, newP);
                    }
                }
            }
        }

        public static void CopyDirectory(string sourceDirectory, string targetDirectory)
        {
            var diSource = new DirectoryInfo(sourceDirectory);
            var diTarget = new DirectoryInfo(targetDirectory);
            diTarget.Create();
            CopyFilesRecursively(diSource, diTarget);
        }

        private static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
        }
    }
}
