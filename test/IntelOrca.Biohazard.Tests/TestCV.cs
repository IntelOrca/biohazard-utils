using System.IO;
using IntelOrca.Biohazard.Room;
using Xunit;

namespace IntelOrca.Biohazard.Tests
{
    public class TestCV
    {
        [Fact]
        public void RDT_000()
        {
            var rdt = GetRdt(0);
            Assert.Equal(8, rdt.Items.Length);
            Assert.Equal(0x08, rdt.Items[0].Type);

            Assert.Equal(13, rdt.Doors.Length);
            Assert.Equal(0, rdt.Doors[0].Stage);
            Assert.Equal(1, rdt.Doors[0].Room);
            Assert.Equal(0, rdt.Doors[0].ExitId);
            Assert.Equal(0, rdt.Doors[0].Transition);
        }

        [Fact]
        public void RDT_001()
        {
            var rdt = GetRdt(1);
            Assert.Equal(2, rdt.Items.Length);
            Assert.Equal(0x1F, rdt.Items[0].Type);
            Assert.Equal(0x0C, rdt.Items[1].Type);

            Assert.Equal(5, rdt.Doors.Length);
            Assert.Equal(0, rdt.Doors[0].Stage);
            Assert.Equal(2, rdt.Doors[0].Room);
            Assert.Equal(0, rdt.Doors[0].ExitId);
            Assert.Equal(1, rdt.Doors[0].Transition);
            Assert.Equal(0, rdt.Doors[4].Stage);
            Assert.Equal(0, rdt.Doors[4].Room);
            Assert.Equal(1, rdt.Doors[4].ExitId);
            Assert.Equal(0, rdt.Doors[4].Transition);
        }

        [Fact]
        public void RDT_001_Rebuild()
        {
            var rdt = GetRdt(1);
            var newRdt = rdt.ToBuilder().ToRdt();
            var a = rdt.Data.CalculateFnv1a();
            var b = newRdt.Data.CalculateFnv1a();
            Assert.Equal(a, b);
        }

        [Fact]
        public void RDT_001_Rebuild_WithChange()
        {
            var rdtBuilder = GetRdt(1).ToBuilder();
            var item0 = rdtBuilder.Items[0];
            var item1 = rdtBuilder.Items[1];
            var door0 = rdtBuilder.Doors[0];

            item0.Type = 0x14;
            item1.Type = 0x15;
            door0.Room = 9;

            rdtBuilder.Items[0] = item0;
            rdtBuilder.Items[1] = item1;
            rdtBuilder.Doors[0] = door0;
            var newRdt = rdtBuilder.ToRdt();

            Assert.Equal(2, newRdt.Items.Length);
            Assert.Equal(0x14, newRdt.Items[0].Type);
            Assert.Equal(0x15, newRdt.Items[1].Type);

            Assert.Equal(5, newRdt.Doors.Length);
            Assert.Equal(0, newRdt.Doors[0].Stage);
            Assert.Equal(9, newRdt.Doors[0].Room);
            Assert.Equal(0, newRdt.Doors[0].ExitId);
            Assert.Equal(1, newRdt.Doors[0].Transition);
            Assert.Equal(0, newRdt.Doors[4].Stage);
            Assert.Equal(0, newRdt.Doors[4].Room);
            Assert.Equal(1, newRdt.Doors[4].ExitId);
            Assert.Equal(0, newRdt.Doors[4].Transition);
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
