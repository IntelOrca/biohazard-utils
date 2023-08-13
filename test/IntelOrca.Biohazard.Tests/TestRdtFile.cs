using System.IO;
using IntelOrca.Biohazard.Room;
using IntelOrca.Biohazard.Script;
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
            var installPath = TestInfo.GetInstallPath(0);
            var rdtPath = Path.Combine(installPath, "JPN", "STAGE1", "ROOM1000.RDT");
            var rdt = new Rdt1(rdtPath);

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
        public void RE1_100_Rebuild()
        {
            var installPath = TestInfo.GetInstallPath(0);
            var rdtPath = Path.Combine(installPath, "JPN", "STAGE1", "ROOM1000.RDT");
            var rdt = new Rdt1(rdtPath);

            var builder = rdt.ToBuilder();
            var rebuiltRdt = builder.ToRdt();

            Assert.Equal(rebuiltRdt.Data.ToArray(), rdt.Data.ToArray());
        }

        [Fact]
        public void RE1_106_Rebuild()
        {
            var installPath = TestInfo.GetInstallPath(0);
            var rdtPath = Path.Combine(installPath, "JPN", "STAGE1", "ROOM1060.RDT");
            var rdt = new Rdt1(rdtPath);

            var builder = rdt.ToBuilder();
            var rebuiltRdt = builder.ToRdt();

            Assert.Equal(rebuiltRdt.Data.ToArray(), rdt.Data.ToArray());
        }

        [Fact]
        public void RE1_All_Rebuild()
        {
            var fail = false;
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
                    try
                    {
                        var rdt = new Rdt1(rdtPath);
                        var builder = rdt.ToBuilder();
                        var rebuiltRdt = builder.ToRdt();
                        try
                        {
                            Assert.Equal(rebuiltRdt.Data.ToArray(), rdt.Data.ToArray());
                            // _output.WriteLine($"{fileName}: PASS");
                        }
                        catch
                        {
                            fail = true;
                            _output.WriteLine($"{fileName}: FAIL");
                        }
                    }
                    catch
                    {
                        _output.WriteLine($"{fileName}: CRASH");
                    }
                }
            }
            Assert.False(fail);
        }

        [Fact]
        public void RE2_200()
        {
            var installPath = TestInfo.GetInstallPath(1);
            var rdtPath = Path.Combine(installPath, "data", "pl0", "rdt", "ROOM2000.RDT");
            var rdt = new Rdt2(rdtPath);

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
        public void RE2_200_Rebuild()
        {
            var installPath = TestInfo.GetInstallPath(1);
            var rdtPath = Path.Combine(installPath, "data", "pl0", "rdt", "ROOM2000.RDT");
            var rdt = new Rdt2(rdtPath);

            var builder = rdt.ToBuilder();
            var rebuiltRdt = builder.ToRdt();

            // File.WriteAllBytes(@"M:\temp\rdt\original.rdt", rdt.Data.ToArray());
            // File.WriteAllBytes(@"M:\temp\rdt\rebuilt.rdt", rebuiltRdt.Data.ToArray());

            Assert.Equal(rebuiltRdt.Data.ToArray(), rdt.Data.ToArray());
        }

        [Fact]
        public void RE2_100_Rebuild()
        {
            var installPath = TestInfo.GetInstallPath(1);
            var rdtPath = Path.Combine(installPath, "data", "pl0", "rdt", "ROOM1000.RDT");
            var rdt = new Rdt2(rdtPath);

            File.WriteAllBytes(@"M:\temp\rdt\original.rdt", rdt.Data.ToArray());

            var builder = rdt.ToBuilder();
            var rebuiltRdt = builder.ToRdt();

            // File.WriteAllBytes(@"M:\temp\rdt\rebuilt.rdt", rebuiltRdt.Data.ToArray());

            Assert.Equal(rebuiltRdt.Data.ToArray(), rdt.Data.ToArray());
        }

        [Fact]
        public void RE2_All_Rebuild()
        {
            var fail = false;
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
                    try
                    {
                        var rdt = new Rdt2(rdtPath);
                        var builder = rdt.ToBuilder();
                        var rebuiltRdt = builder.ToRdt();
                        try
                        {
                            Assert.Equal(rebuiltRdt.Data.ToArray(), rdt.Data.ToArray());
                            // _output.WriteLine($"{fileName}: PASS");
                        }
                        catch
                        {
                            fail = true;
                            _output.WriteLine($"{fileName}: FAIL");
                        }
                    }
                    catch
                    {
                        _output.WriteLine($"{fileName}: CRASH");
                    }
                }
            }
            Assert.False(fail);
        }

        [Fact]
        public void RebuildTextChunk_102()
        {
            var installPath = TestInfo.GetInstallPath(1);
            var rdtPath = Path.Combine(installPath, "data", "pl0", "rdt", "ROOM1020.RDT");
            var rdtFile = new RdtFile(rdtPath, BioVersion.Biohazard2);

            var jpn = new BioString[] { new BioString("This is a test.") };
            var eng = new BioString[] { new BioString("This is a test.") };
            rdtFile.SetTexts(0, jpn);
            rdtFile.SetTexts(1, eng);

            var actual = rdtFile.Data.CalculateFnv1a();
            Assert.Equal(1416122343111777090UL, actual);
        }

        [Fact]
        public void RebuildTextChunk_200()
        {
            var installPath = TestInfo.GetInstallPath(1);
            var rdtPath = Path.Combine(installPath, "data", "pl1", "rdt", "ROOM20B1.RDT");
            var rdtFile = new RdtFile(rdtPath, BioVersion.Biohazard2);
            var expected = rdtFile.Data.CalculateFnv1a();

            var jpn = rdtFile.GetTexts(0);
            var eng = rdtFile.GetTexts(1);
            rdtFile.SetTexts(0, jpn);
            rdtFile.SetTexts(1, eng);

            var actual = rdtFile.Data.CalculateFnv1a();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void RebuildAnimations_112()
        {
            var installPath = TestInfo.GetInstallPath(1);
            var rdtPath = Path.Combine(installPath, "data", "pl1", "rdt", "ROOM1121.RDT");
            var rdtFile = new RdtFile(rdtPath, BioVersion.Biohazard2);
            var expected = rdtFile.Data.CalculateFnv1a();

            rdtFile.Animations = rdtFile.Animations;

            var actual = rdtFile.Data.CalculateFnv1a();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void RebuildScd_117()
        {
            var installPath = TestInfo.GetInstallPath(1);
            var rdtPath = Path.Combine(installPath, "data", "pl1", "rdt", "ROOM1121.RDT");
            var rdtFile = new RdtFile(rdtPath, BioVersion.Biohazard2);
            var expectedData = rdtFile.Data;

            var init = rdtFile.GetScd(BioScriptKind.Init);
            var main = rdtFile.GetScd(BioScriptKind.Main);
            rdtFile.SetScd(BioScriptKind.Init, init);
            rdtFile.SetScd(BioScriptKind.Main, main);

            var actualData = rdtFile.Data;
            Assert.Equal(expectedData, actualData);
        }

        [Fact]
        public void ChangeScd_117()
        {
            var installPath = TestInfo.GetInstallPath(1);
            var rdtPath = Path.Combine(installPath, "data", "pl1", "rdt", "ROOM1121.RDT");
            var rdtFile = new RdtFile(rdtPath, BioVersion.Biohazard2);

            var init = new byte[] { 0x02, 0x00, 0x01, 0x00 };
            var main = new byte[] { 0x04, 0x00, 0x06, 0x00, 0x01, 0x00, 0x01, 0x00 };
            rdtFile.SetScd(BioScriptKind.Init, init);
            rdtFile.SetScd(BioScriptKind.Main, main);

            var hash = rdtFile.Data.CalculateFnv1a();
            Assert.Equal(14551199640999392555UL, hash);
        }

        [Fact]
        public void ChangeScd_102()
        {
            var installPath = TestInfo.GetInstallPath(1);
            var rdtPath = Path.Combine(installPath, "data", "pl1", "rdt", "ROOM1021.RDT");
            var rdtFile = new RdtFile(rdtPath, BioVersion.Biohazard2);
            var expectedData = rdtFile.Data;

            var models = rdtFile.Models;
            rdtFile.Models = models;

            var actualData = rdtFile.Data;
            Assert.Equal(expectedData, actualData);
        }
    }
}
