﻿using System.IO;
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
        public void RDT_006_Rebuild() => AssertRebuild(6);

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
        public void RDT_012_Rebuild() => AssertRebuild(12);

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

        [Fact]
        public void RDT_208_Texture_Rebuild()
        {
            var rdts = new[] { 0, 1, 22 };
            foreach (var rdtNum in rdts)
            {
                var rdt = GetRdt(rdtNum);

                // Check texture list
                var oldT = rdt.Textures;
                var newT = oldT.ToBuilder().ToTextureList();
                AssertAndCompareMemory(oldT.Data, newT.Data);

                var oldG = rdt.Textures.Groups[0];
                var newG = oldG.ToBuilder().ToGroup();
                AssertAndCompareMemory(oldG.Data, newG.Data);

                foreach (var oldE in oldG.Entries)
                {
                    if (oldE.Kind != CvTextureEntryKind.TIM2)
                        continue;

                    var newE = new CvTextureEntry(oldE.Tim2);
                    AssertAndCompareMemory(oldE.Tim2.Data, newE.Tim2.Data);
                }
            }
        }

        [Fact]
        public void RDT_10C_Motion_Rebuild()
        {
            var rdt = GetRdt(14);
            var m = rdt.Motions;
            var newBaseOffset = m.BaseOffset + (4 * 6);
            var m2 = m.WithNewBaseOffset(newBaseOffset);

            Assert.Equal(newBaseOffset, m2.BaseOffset);
            Assert.Equal(m.PageCount, m2.PageCount);
            for (var i = 0; i < m2.PageCount; i++)
            {
                Assert.Equal(m.Pages[i].Data.Length, m2.Pages[i].Data.Length);
            }
        }

        [Fact]
        public void RDT_10C_Models_Rebuild()
        {
            var rdt = GetRdt(14);
            var list = rdt.Models;

            // Test WithNewBaseOffset
            {
                var newBaseOffset = list.BaseOffset + (4 * 6);
                var m2 = list.WithNewBaseOffset(newBaseOffset);
                Assert.Equal(newBaseOffset, m2.BaseOffset);
                Assert.Equal(list.PageCount, m2.PageCount);
                for (var i = 0; i < m2.PageCount; i++)
                {
                    Assert.Equal(list.Pages[i].Data.Length, m2.Pages[i].Data.Length);
                }
            }

            // Test page builder
            for (var i = 0; i < list.Pages.Length; i++)
            {
                var page = list.Pages[i];
                var rebuilt = page.ToBuilder().ToCvModelListPage();
                AssertAndCompareMemory(page.Data, rebuilt.Data);
            }

            // Test list builder
            var rebuiltList = list.ToBuilder().ToCvModelList();
            AssertAndCompareMemory(list.Data, rebuiltList.Data);
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
