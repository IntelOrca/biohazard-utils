﻿using System;
using System.IO;
using System.Linq;
using IntelOrca.Biohazard.Room;
using IntelOrca.Biohazard.Script;
using IntelOrca.Biohazard.Script.Compilation;
using Xunit;
using Xunit.Abstractions;

namespace IntelOrca.Biohazard.Tests
{
    public class TestReassemble
    {
        private readonly ITestOutputHelper _output;

        public TestReassemble(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void RE2_Leon()
        {
            CheckRDTs(BioVersion.Biohazard2, Path.Combine(TestInfo.GetInstallPath(1), @"data\pl0\rdt"));
        }

        [Fact]
        public void RE2_Claire()
        {
            CheckRDTs(BioVersion.Biohazard2, Path.Combine(TestInfo.GetInstallPath(1), @"data\pl1\rdt"));
        }

        [Fact]
        public void RE3()
        {
            var installPath = TestInfo.GetInstallPath(2);
            var rofs = new RE3Archive(Path.Combine(installPath, "rofs13.dat"));
            var fail = false;
            foreach (var file in rofs.Files)
            {
                var fileName = Path.GetFileName(file);
                var rdt = rofs.GetFileContents(file);
                var rdtFile = new Rdt2(BioVersion.Biohazard3, rdt);
                var sPath = Path.ChangeExtension(fileName, ".s");
                fail |= AssertReassembleRdt(BioVersion.Biohazard3, rdtFile, sPath);
            }
            Assert.False(fail);
        }

        private void CheckRDTs(BioVersion version, string rdtPath)
        {
            var rdts = Directory.GetFiles(rdtPath, "*.rdt");
            var fail = false;
            foreach (var rdt in rdts)
            {
                var rdtId = RdtId.Parse(rdt.Substring(rdt.Length - 8, 3));
                if (rdtId == new RdtId(4, 0x05))
                    continue;
                if (rdtId == new RdtId(6, 0x05))
                    continue;
                if (rdtId.Stage > 6)
                    continue;

                var rdtFile = new Rdt2(version, rdt);
                var sPath = Path.ChangeExtension(rdt, ".s");
                fail |= AssertReassembleRdt(version, rdtFile, sPath);
            }
            Assert.False(fail);
        }

        private bool AssertReassembleRdt(BioVersion version, Rdt2 rdtFile, string sPath)
        {
            var disassembly = IntelOrca.Biohazard.Extensions.RdtExtensions.DisassembleScd(rdtFile);
            var scdAssembler = new ScdAssembler();
            var err = scdAssembler.Generate(new StringFileIncluder(sPath, disassembly), sPath);
            var fail = false;
            if (err != 0)
            {
                foreach (var error in scdAssembler.Errors.Errors)
                {
                    _output.WriteLine(error.ToString());
                }
                fail = true;
            }
            else
            {
                var scdInit = rdtFile.SCDINIT;
                var scdDataInit = scdAssembler.Operations
                    .OfType<ScdRdtEditOperation>()
                    .FirstOrDefault(x => x.Kind == BioScriptKind.Init)
                    .Data;
                var index = CompareByteArray(scdInit.Data, scdDataInit.Data);
                if (index != -1)
                {
                    _output.WriteLine(".init differs at 0x{0:X2} for '{1}'", index, sPath);
                    fail = true;
                }

                if (rdtFile.Version != BioVersion.Biohazard3)
                {
                    var scdMain = rdtFile.SCDMAIN;
                    var scdDataMain = scdAssembler.Operations
                        .OfType<ScdRdtEditOperation>()
                        .FirstOrDefault(x => x.Kind == BioScriptKind.Main)
                        .Data;
                    index = CompareByteArray(scdMain.Data, scdDataMain.Data);
                    if (index != -1)
                    {
                        _output.WriteLine(".main differs at 0x{0:X2} for '{1}'", index, sPath);
                        fail = true;
                    }
                }
            }
            return fail;
        }

        private static int CompareByteArray(ReadOnlyMemory<byte> a, ReadOnlyMemory<byte> b) => CompareByteArray(a.Span, b.Span);
        private static int CompareByteArray(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
        {
            var minLen = Math.Min(a.Length, b.Length);
            for (int i = 0; i < minLen; i++)
            {
                if (a[i] != b[i])
                    return i;
            }
            if (a.Length != b.Length)
                return minLen;
            return -1;
        }
    }
}
