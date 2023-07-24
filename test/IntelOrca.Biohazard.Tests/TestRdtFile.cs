using System.IO;
using IntelOrca.Biohazard.Script;
using Xunit;

namespace IntelOrca.Biohazard.Tests
{
    public class TestRdtFile
    {
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
            var rdtPath = Path.Combine(installPath, "data", "pl1", "rdt", "ROOM2001.RDT");
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
    }
}
