using System;
using System.Collections.Generic;
using System.IO;
using IntelOrca.Biohazard.Extensions;
using IntelOrca.Biohazard.Room;
using Xunit;
using Xunit.Abstractions;

namespace IntelOrca.Biohazard.Tests
{
    public class TestRdtFile
    {
        private readonly ITestOutputHelper _output;

        public TestRdtFile(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void RE1_100()
        {
            var rdt = (Rdt1)GetRdt(BioVersion.Biohazard1, "ROOM1000.RDT");

            Assert.Equal(2432, rdt.EDD.Data.Length);    // animation.edd
            Assert.Equal(52804, rdt.EMR.Data.Length);   // animation.emr
            Assert.Equal(14, rdt.BLK.Length);           // block.blk
            Assert.Equal(264, rdt.RID.Length);          // camera.rid
            Assert.Equal(192, rdt.SCA.Length);          // collision.sca
            Assert.Equal(12, rdt.FLR.Length);           // floor.flr
            Assert.Equal(66, rdt.LIT.Length);           // light.lit
            Assert.Equal(2076, rdt.PRI.Length);         // sprite.pri
            Assert.Equal(280, rdt.RVD.Length);          // zone.rvd
        }

        [Fact]
        public void RE1_100_Rebuild() => AssertRebuild(BioVersion.Biohazard1, "ROOM1000.RDT");

        [Fact]
        public void RE1_106_Rebuild() => AssertRebuild(BioVersion.Biohazard1, "ROOM1060.RDT");

        [Fact]
        public void RE1_All_Rebuild() => AssertRebuildAll(BioVersion.Biohazard1);

        [Fact]
        public void RE2_200()
        {
            var rdt = (Rdt2)GetRdt(BioVersion.Biohazard2, "ROOM2000.RDT");

            Assert.Equal(840, rdt.RVD.Length);                  // animation/anim.rbj
            Assert.Equal(112, rdt.BLK.Length);                  // block.blk
            Assert.Equal(512, rdt.RID.Length);                  // camera.rid
            Assert.Equal(832, rdt.SCA.Length);                  // collision.sca
            Assert.Equal(50, rdt.FLR.Length);                   // floor.flr
            Assert.Equal(640, rdt.LIT.Length);                  // light.lit
            Assert.Equal(153600, rdt.TIMSCROLL.Data.Length);    // scroll.tim
            Assert.Equal(6340, rdt.PRI.Length);                 // sprite.pri
            Assert.Equal(10744, rdt.RBJ.Data.Length);           // zone.rvd

            // Assert.Equal(8, rdt.ESPID.Length);
            // Assert.Equal(8, rdt.ESPEFF.Length);
            Assert.Equal(4256, rdt.ESPTIM.Data.Length);
        }

        [Fact]
        public void RE2_100_Rebuild() => AssertRebuild(BioVersion.Biohazard2, "ROOM1000.RDT");

        [Fact]
        public void RE2_200_Rebuild() => AssertRebuild(BioVersion.Biohazard2, "ROOM2000.RDT");

        [Fact]
        public void RE2_All_Rebuild() => AssertRebuildAll(BioVersion.Biohazard2);

        [Fact]
        public void RE2_102_MSG()
        {
            var rdt = (Rdt2)GetRdt(BioVersion.Biohazard2, "ROOM1020.RDT");

            Assert.Equal(MsgLanguage.Japanese, rdt.MSGJA.Language);
            Assert.Equal(BioVersion.Biohazard2, rdt.MSGJA.Version);

            Assert.Equal(MsgLanguage.English, rdt.MSGEN.Language);
            Assert.Equal(BioVersion.Biohazard2, rdt.MSGEN.Version);

            var jaBuilder = new MsgList.Builder();
            jaBuilder.Messages.Add(new Msg(BioVersion.Biohazard2, MsgLanguage.Japanese, "This is a test."));

            var enBuilder = new MsgList.Builder();
            enBuilder.Messages.Add(new Msg(BioVersion.Biohazard2, MsgLanguage.English, "This is a test."));

            var rdtBuilder = rdt.ToBuilder();
            rdtBuilder.MSGJA = jaBuilder.ToMsgList();
            rdtBuilder.MSGEN = enBuilder.ToMsgList();
            var rebuiltRdt = rdtBuilder.ToRdt();

            var actual = rebuiltRdt.Data.CalculateFnv1a();
            Assert.Equal(14281979433154599006UL, actual);
        }

        [Fact]
        public void RE2_201_MSG()
        {
            var rdt = (Rdt2)GetRdt(BioVersion.Biohazard2, "ROOM2010.RDT");

            Assert.Equal(MsgLanguage.Japanese, rdt.MSGJA.Language);
            Assert.Equal(BioVersion.Biohazard2, rdt.MSGJA.Version);

            Assert.Equal(MsgLanguage.English, rdt.MSGEN.Language);
            Assert.Equal(BioVersion.Biohazard2, rdt.MSGEN.Version);

            Assert.Equal(8, rdt.MSGJA.Count);
            Assert.Equal(8, rdt.MSGEN.Count);

            var rdtBuilder = rdt.ToBuilder();
            rdtBuilder.MSGJA = rdt.MSGJA.ToBuilder().ToMsgList();
            rdtBuilder.MSGEN = rdt.MSGEN.ToBuilder().ToMsgList();
            var rebuilt = rdtBuilder.ToRdt();

            AssertMemory(rdt.Data, rebuilt.Data);
        }

        [Fact]
        public void RE2_20B_MSG()
        {
            var rdt = (Rdt2)GetRdt(BioVersion.Biohazard2, "ROOM20B1.RDT");

            Assert.Equal(MsgLanguage.Japanese, rdt.MSGJA.Language);
            Assert.Equal(BioVersion.Biohazard2, rdt.MSGJA.Version);

            Assert.Equal(MsgLanguage.English, rdt.MSGEN.Language);
            Assert.Equal(BioVersion.Biohazard2, rdt.MSGEN.Version);

            Assert.Equal(19, rdt.MSGJA.Count);
            Assert.Equal(19, rdt.MSGEN.Count);

            Assert.Equal("A {police station map}.\nWill you take it?@00", rdt.MSGEN[0].ToString());
            Assert.Equal("\nThe door won't open!\n", rdt.MSGEN[18].ToString());

            var rdtBuilder = rdt.ToBuilder();
            rdtBuilder.MSGJA = rdt.MSGJA.ToBuilder().ToMsgList();
            rdtBuilder.MSGEN = rdt.MSGEN.ToBuilder().ToMsgList();

            AssertMemory(rdt.MSGEN.Data, rdtBuilder.MSGEN.Data);
            AssertMemory(rdt.MSGJA.Data, rdtBuilder.MSGJA.Data);

            var rebuilt = rdtBuilder.ToRdt();
            AssertMemory(rdt.Data, rebuilt.Data);
        }

        [Fact]
        public void RE2_112_RBJ()
        {
            var rdt = (Rdt2)GetRdt(BioVersion.Biohazard2, "ROOM1121.RDT");
            var rdtBuilder = rdt.ToBuilder();

            var rbj = rdt.RBJ;
            var rbjBuilder = rbj.ToBuilder();
            rdtBuilder.RBJ = rbjBuilder.ToRbj();
            var rebuiltRdt = rdtBuilder.ToRdt();

            AssertMemory(rdt.RBJ.Data, rdtBuilder.RBJ.Data);
            AssertMemory(rdt.Data, rebuiltRdt.Data);
        }

        [Fact]
        public void RE2_112_SCD()
        {
            var rdt = (Rdt2)GetRdt(BioVersion.Biohazard2, "ROOM1121.RDT");
            var builder = rdt.ToBuilder();

            builder.SCDINIT = new ScdProcedureList(BioVersion.Biohazard2, new byte[] { 0x02, 0x00, 0x01, 0x00 });
            builder.SCDMAIN = new ScdProcedureList(BioVersion.Biohazard2, new byte[] { 0x04, 0x00, 0x06, 0x00, 0x01, 0x00, 0x01, 0x00 });
            var rebuilt = builder.ToRdt();

            var hash = rebuilt.Data.CalculateFnv1a();
            Assert.Equal(6902255125174090017UL, hash);
        }

        [Fact]
        public void RE2_101_SCD()
        {
            var rdt = (Rdt2)GetRdt(BioVersion.Biohazard2, "ROOM1010.RDT");
            var rdtBuilder = rdt.ToBuilder();

            rdtBuilder.SCDINIT = rdtBuilder.SCDINIT.ToBuilder().ToProcedureList();
            rdtBuilder.SCDMAIN = rdtBuilder.SCDMAIN.ToBuilder().ToProcedureList();
            var rebuiltRdt = rdtBuilder.ToRdt();

            AssertMemory(rdt.SCDINIT.Data, rdtBuilder.SCDINIT.Data);
            AssertMemory(rdt.SCDMAIN.Data, rdtBuilder.SCDMAIN.Data);
            AssertMemory(rdt.Data, rebuiltRdt.Data);
        }

        [Fact]
        public void RE3_100()
        {
            AssertRebuild(BioVersion.Biohazard3, "R100.RDT");
        }

        private void AssertRebuildAll(BioVersion version)
        {
            var fail = false;
            var fileNames = GetAllRdtFileNames(version);
            foreach (var fileName in fileNames)
            {
                try
                {
                    AssertRebuild(version, fileName);
                }
                catch
                {
                    fail = true;
                    _output.WriteLine($"{fileName}: FAIL");
                }
            }
            Assert.False(fail);
        }

        private static void AssertRebuild(BioVersion version, string fileName)
        {
            var rdt = GetRdt(version, fileName);
            var rebuiltRdt = rdt.ToBuilder().ToRdt();
            AssertMemory(rdt.Data, rebuiltRdt.Data);
        }

        private static void AssertAndCompareMemory(ReadOnlyMemory<byte> expected, ReadOnlyMemory<byte> actual)
        {
            try
            {
                AssertMemory(expected, actual);
            }
            catch
            {
                expected.WriteToFile(@"M:\temp\rdt\expected.dat");
                actual.WriteToFile(@"M:\temp\rdt\actual.dat");
                throw;
            }
        }

        private static void AssertMemory(ReadOnlyMemory<byte> expected, ReadOnlyMemory<byte> actual)
        {
            var spanExpected = expected.Span;
            var spanActual = actual.Span;

            var length = spanExpected.Length;
            Assert.Equal(length, spanActual.Length);
            for (var i = 0; i < length; i++)
            {
                if (spanExpected[i] != spanActual[i])
                {
                    Assert.False(true, $"Memory did not match at index {i}");
                }
            }
        }

        private static IRdt GetRdt(BioVersion version, string fileName)
        {
            switch (version)
            {
                case BioVersion.Biohazard1:
                {
                    var installPath = TestInfo.GetInstallPath(0);
                    var stage = int.Parse(fileName.Substring(4, 1));
                    var rdtPath = Path.Combine(installPath, "JPN", $"STAGE{stage}", fileName);
                    return new Rdt1(rdtPath);
                }
                case BioVersion.Biohazard2:
                {
                    var installPath = TestInfo.GetInstallPath(1);
                    var player = int.Parse(fileName.Substring(7, 1));
                    var rdtPath = Path.Combine(installPath, "data", $"pl{player}", "rdt", fileName);
                    return new Rdt2(BioVersion.Biohazard2, rdtPath);
                }
                case BioVersion.Biohazard3:
                {
                    var installPath = TestInfo.GetInstallPath(2);
                    var rofsFiles = Directory.GetFiles(installPath, "rofs*.dat");
                    var repo = new FileRepository(installPath);
                    foreach (var file in rofsFiles)
                        repo.AddRE3Archive(file);

                    var rdtPath = Path.Combine(installPath, "data_j", "rdt", fileName);
                    var rdtBytes = repo.GetBytes(rdtPath);
                    return new Rdt2(BioVersion.Biohazard3, rdtBytes);
                }
                default:
                    throw new NotImplementedException();
            }
        }

        private static string[] GetAllRdtFileNames(BioVersion version)
        {
            var results = new List<string>();
            switch (version)
            {
                case BioVersion.Biohazard1:
                {
                    var installPath = TestInfo.GetInstallPath(0);
                    for (var stage = 1; stage <= 7; stage++)
                    {
                        var stageDirectory = Path.Combine(installPath, "JPN", $"STAGE{stage}");
                        var files = Directory.GetFiles(stageDirectory, "*.RDT");
                        foreach (var rdtPath in files)
                        {
                            var length = new FileInfo(rdtPath).Length;
                            if (length <= 5000)
                                continue;

                            var fileName = Path.GetFileName(rdtPath);
                            results.Add(fileName);
                        }
                    }
                    break;
                }
                case BioVersion.Biohazard2:
                {
                    var installPath = TestInfo.GetInstallPath(1);
                    for (var player = 0; player <= 1; player++)
                    {
                        var plDirectory = Path.Combine(installPath, "data", $"pl{player}", "rdt");
                        var files = Directory.GetFiles(plDirectory, "*.RDT");
                        foreach (var rdtPath in files)
                        {
                            var length = new FileInfo(rdtPath).Length;
                            if (length <= 5000)
                                continue;

                            var fileName = Path.GetFileName(rdtPath);
                            results.Add(fileName);
                        }
                    }
                    break;
                }
                case BioVersion.Biohazard3:
                default:
                    throw new NotImplementedException();
            }
            return results.ToArray();
        }
    }
}
