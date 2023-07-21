using System.IO;
using Xunit;

namespace IntelOrca.Biohazard.Tests
{
    public class TestMessage
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
            var actualData = rdtFile.Data;
            Assert.Equal(184444, actualData.Length);
        }

        [Fact]
        public void RebuildTextChunk_200()
        {
            var installPath = TestInfo.GetInstallPath(1);
            var rdtPath = Path.Combine(installPath, "data", "pl1", "rdt", "ROOM2001.RDT");
            var rdtFile = new RdtFile(rdtPath, BioVersion.Biohazard2);
            var expectedData = rdtFile.Data;

            var jpn = rdtFile.GetTexts(0);
            var eng = rdtFile.GetTexts(1);
            rdtFile.SetTexts(0, jpn);
            rdtFile.SetTexts(1, eng);

            var actualData = rdtFile.Data;
            Assert.Equal(expectedData, actualData);
        }
    }
}
