using System.IO;
using Xunit;

namespace IntelOrca.Biohazard.Tests
{
    public class TestMessage
    {
        [Fact]
        public void RebuildTextChunk()
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
