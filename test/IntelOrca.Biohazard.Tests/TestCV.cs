using System.IO;
using IntelOrca.Biohazard.Room;
using Xunit;
using Xunit.Abstractions;
using static IntelOrca.Biohazard.Tests.MemoryAssert;

namespace IntelOrca.Biohazard.Tests
{
    public class TestCv
    {
        private readonly ITestOutputHelper _output;

        public TestCv(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void RDT_000()
        {
            var rdt = GetRdt(0);
            Assert.Equal(8, rdt.Items.Length);
            Assert.Equal(0x08, rdt.Items[0].Type);

            Assert.Equal(13, rdt.Aots.Length);
            Assert.Equal(0, rdt.Aots[0].Stage);
            Assert.Equal(1, rdt.Aots[0].Room);
            Assert.Equal(0, rdt.Aots[0].ExitId);
            Assert.Equal(0, rdt.Aots[0].Transition);
        }

        [Fact]
        public void RDT_001()
        {
            var rdt = GetRdt(1);
            Assert.Equal(2, rdt.Items.Length);
            Assert.Equal(0x1F, rdt.Items[0].Type);
            Assert.Equal(0x0C, rdt.Items[1].Type);

            Assert.Equal(5, rdt.Aots.Length);
            Assert.Equal(0, rdt.Aots[0].Stage);
            Assert.Equal(2, rdt.Aots[0].Room);
            Assert.Equal(0, rdt.Aots[0].ExitId);
            Assert.Equal(1, rdt.Aots[0].Transition);
            Assert.Equal(0, rdt.Aots[4].Stage);
            Assert.Equal(0, rdt.Aots[4].Room);
            Assert.Equal(1, rdt.Aots[4].ExitId);
            Assert.Equal(0, rdt.Aots[4].Transition);
        }

        [Fact]
        public void RDT_000_Rebuild() => AssertRebuild(0);

        [Fact]
        public void RDT_001_Rebuild() => AssertRebuild(1);

        [Fact]
        public void RDT_001_Rebuild_WithChange()
        {
            var rdtBuilder = GetRdt(1).ToBuilder();
            var item0 = rdtBuilder.Items[0];
            var item1 = rdtBuilder.Items[1];
            var door0 = rdtBuilder.Aots[0];

            item0.Type = 0x14;
            item1.Type = 0x15;
            door0.Room = 9;

            rdtBuilder.Items[0] = item0;
            rdtBuilder.Items[1] = item1;
            rdtBuilder.Aots[0] = door0;
            var newRdt = rdtBuilder.ToRdt();

            Assert.Equal(2, newRdt.Items.Length);
            Assert.Equal(0x14, newRdt.Items[0].Type);
            Assert.Equal(0x15, newRdt.Items[1].Type);

            Assert.Equal(5, newRdt.Aots.Length);
            Assert.Equal(0, newRdt.Aots[0].Stage);
            Assert.Equal(9, newRdt.Aots[0].Room);
            Assert.Equal(0, newRdt.Aots[0].ExitId);
            Assert.Equal(1, newRdt.Aots[0].Transition);
            Assert.Equal(0, newRdt.Aots[4].Stage);
            Assert.Equal(0, newRdt.Aots[4].Room);
            Assert.Equal(1, newRdt.Aots[4].ExitId);
            Assert.Equal(0, newRdt.Aots[4].Transition);
        }

        [Fact]
        public void RDT_010_Rebuild_Script()
        {
            var rdt = GetRdt(1);
            var s1 = rdt.Script;
            var sb = s1.ToBuilder();
            var s2 = sb.ToProcedureList();
            AssertMemory(s1.Data, s2.Data);
        }

        [Fact]
        public void RDT_013_Rebuild() => AssertRebuild(13);

        [Fact(Skip = "Takes too long")]
        public void RDT_All_Rebuild()
        {
            var fail = false;
            for (var i = 0; i < 205; i++)
            {
                try
                {
                    AssertRebuild(i);
                }
                catch
                {
                    fail = true;
                    _output.WriteLine($"{i}: FAIL");
                }
            }
            Assert.False(fail);
        }

        private void AssertRebuild(int index)
        {
            var rdt = GetRdt(index);
            var newRdt = rdt.ToBuilder().ToRdt();
            AssertAndCompareMemory(rdt.Data, newRdt.Data);
        }

        private RdtCv GetRdt(int index)
        {
            var installPath = TestInfo.GetInstallPath(3);
            var rdxAfs = Path.Combine(installPath, "data", "RDX_LNK.AFS");
            var afsFile = new AfsFile(File.ReadAllBytes(rdxAfs));
            var prsData = new PrsFile(afsFile.GetFileData(index));
            var rdt = new RdtCv(prsData.Uncompressed);
            return rdt;
        }
    }
}
