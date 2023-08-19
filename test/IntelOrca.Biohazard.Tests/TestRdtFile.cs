using System;
using System.IO;
using IntelOrca.Biohazard.Extensions;
using IntelOrca.Biohazard.Room;
using Xunit;
using Xunit.Abstractions;
using static IntelOrca.Biohazard.Tests.TestInfo;

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
        public void RE1_100_SCD_EVENTS()
        {
            var rdt = (Rdt1)GetRdt(BioVersion.Biohazard1, "ROOM1000.RDT");

            var evtList = rdt.EventSCD;
            var evtListBuilder = evtList.ToBuilder();
            var rebuiltEvtList = evtListBuilder.ToEventList();
            AssertMemory(evtList.Data, rebuiltEvtList.Data);

            var rdtBuilder = rdt.ToBuilder();
            rdtBuilder.EventSCD = rebuiltEvtList;
            var rebuiltRdt = rdtBuilder.ToRdt();
            AssertMemory(rdt.Data, rebuiltRdt.Data);
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
        public void RE3_All_Rebuild() => AssertRebuildAll(BioVersion.Biohazard3);

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
            AssertAndCompareMemory(rdt.Data, rebuiltRdt.Data);
        }

        private static void AssertAndCompareMemory(ReadOnlyMemory<byte> expected, ReadOnlyMemory<byte> actual)
        {
            try
            {
                AssertMemory(expected, actual);
            }
            catch
            {
                var path = @"M:\temp\rdt";
                Directory.CreateDirectory(path);
                expected.WriteToFile(Path.Combine(path, "expected.dat"));
                actual.WriteToFile(Path.Combine(path, "actual.dat"));
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
    }
}
