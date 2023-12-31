﻿using System;
using System.IO;
using System.Linq;
using IntelOrca.Biohazard.Model;
using Xunit;

namespace IntelOrca.Biohazard.Tests
{
    public class TestModel
    {
        [Fact]
        public void ReSaveEmd()
        {
            var installPath = TestInfo.GetInstallPath(0);
            var playersPath = Path.Combine(installPath, "JPN", "ENEMY");
            var emdFiles = Directory.GetFiles(playersPath, "*.EMD");
            foreach (var emdFilePath in emdFiles)
            {
                if (emdFilePath.EndsWith("EM1032.EMD", StringComparison.OrdinalIgnoreCase))
                    continue;

                var emdFile = new EmdFile(BioVersion.Biohazard1, emdFilePath);

                var ms = new MemoryStream();
                emdFile.Save(ms);

                var actual = ms.ToArray();
                var expected = File.ReadAllBytes(emdFilePath);
                AssertByteArraysEqual(expected, actual);
            }
        }

        [Fact]
        public void ReSaveEmw()
        {
            var installPath = TestInfo.GetInstallPath(0);
            var playersPath = Path.Combine(installPath, "JPN", "PLAYERS");
            var emwFiles = Directory.GetFiles(playersPath, "*.EMW");
            foreach (var emwFilePath in emwFiles)
            {
                var emwFile = new PlwFile(BioVersion.Biohazard1, emwFilePath);

                var ms = new MemoryStream();
                emwFile.Save(ms);

                var actual = ms.ToArray();
                var expected = File.ReadAllBytes(emwFilePath);
                AssertByteArraysEqual(expected, actual);
            }
        }

        [Fact]
        public void RebuildTMD()
        {
            var installPath = TestInfo.GetInstallPath(0);
            var emdPath = Path.Combine(installPath, "JPN", "ENEMY", "EM1028.EMD");
            var emdFile = new EmdFile(BioVersion.Biohazard1, emdPath);
            var mesh = (Tmd)emdFile.GetMesh(0);
            var builder = mesh.ToBuilder();
            var newMesh = builder.ToMesh();

            var expectedData = mesh.Data.ToArray();
            var actualData = newMesh.Data.ToArray();
            AssertByteArraysEqual(expectedData, actualData);
        }

        [Fact]
        public void RebuildMD1()
        {
            var installPath = TestInfo.GetInstallPath(1);
            var pldPath = Path.Combine(installPath, "data", "pl0", "pld", "pl00.pld");
            var pldFile = new PldFile(BioVersion.Biohazard2, pldPath);
            var mesh = (Md1)pldFile.GetMesh(0);
            var builder = mesh.ToBuilder();
            var newMesh = builder.ToMesh();

            var expectedData = mesh.Data.ToArray();
            var actualData = newMesh.Data.ToArray();
            AssertByteArraysEqual(expectedData, actualData);
        }

        [Fact]
        public void RebuildMD2()
        {
            var installPath = TestInfo.GetInstallPath(2);
            var rofsFiles = Directory.GetFiles(installPath, "rofs*.dat");

            var repo = new FileRepository(installPath);
            foreach (var file in rofsFiles)
                repo.AddRE3Archive(file);

            var pldPath = Path.Combine(installPath, "data", "pld", "pl00.pld");
            var pldStream = repo.GetStream(pldPath);
            var pldFile = new PldFile(BioVersion.Biohazard3, pldStream);
            var mesh = (Md2)pldFile.GetMesh(0);
            var builder = mesh.ToBuilder();
            var newMesh = builder.ToMesh();

            var expectedData = mesh.Data.ToArray();
            var actualData = newMesh.Data.ToArray();
            AssertByteArraysEqual(expectedData, actualData);
        }

        [Fact]
        public void RebuildMD2_Empty()
        {
            var installPath = TestInfo.GetInstallPath(2);
            var rofsFiles = Directory.GetFiles(installPath, "rofs*.dat");

            var repo = new FileRepository(installPath);
            foreach (var file in rofsFiles)
                repo.AddRE3Archive(file);

            var emdPath = Path.Combine(installPath, "room", "emd", "em11.emd");
            var emdStream = repo.GetStream(emdPath);
            var emdFile = new EmdFile(BioVersion.Biohazard3, emdStream);
            var mesh = (Md2)emdFile.GetMesh(0);
            var builder = mesh.ToBuilder();
            var newMesh = builder.ToMesh();

            var expectedData = mesh.Data.ToArray();
            var actualData = newMesh.Data.ToArray();
            AssertByteArraysEqual(expectedData, actualData);
        }

        [Fact]
        public void RebuildMorphData()
        {
            var installPath = TestInfo.GetInstallPath(1);
            var emdPath = Path.Combine(installPath, "data", "pl0", "emd0", "em046.emd");
            var emdFile = new EmdFile(BioVersion.Biohazard2, emdPath);
            var morphData = emdFile.GetChunk<MorphData>(0);
            var builder = morphData.ToBuilder();
            var newMesh = builder.ToMorphData();

            var expectedData = morphData.Data.ToArray();
            var actualData = newMesh.Data.ToArray();
            AssertByteArraysEqual(expectedData, actualData);
        }

        [Fact]
        public void RebuildEdd()
        {
            var installPath = TestInfo.GetInstallPath(1);
            var emdPath = Path.Combine(installPath, "data", "pl0", "emd0", "em010.emd");
            var emdFile = new EmdFile(BioVersion.Biohazard2, emdPath);
            var edd = emdFile.GetEdd(0);
            var builder = edd.ToBuilder();
            var newEdd = builder.ToEdd();

            var expectedData = edd.Data.ToArray();
            var actualData = newEdd.Data.ToArray();
            AssertByteArraysEqual(expectedData, actualData);
        }

        [Fact]
        public void RebuildEmr1()
        {
            var installPath = TestInfo.GetInstallPath(0);
            var emdPath = Path.Combine(installPath, "JPN", "ENEMY", "EM1028.EMD");
            var emdFile = new EmdFile(BioVersion.Biohazard1, emdPath);
            var emr = emdFile.GetEmr(0);
            var builder = emr.ToBuilder();
            var newEmr = builder.ToEmr();

            var expectedData = emr.Data.ToArray();
            var actualData = newEmr.Data.ToArray();
            AssertByteArraysEqual(expectedData, actualData);
        }

        [Fact]
        public void RebuildEmr2()
        {
            var installPath = TestInfo.GetInstallPath(1);
            var emdPath = Path.Combine(installPath, "data", "pl0", "emd0", "em010.emd");
            var emdFile = new EmdFile(BioVersion.Biohazard2, emdPath);

            for (var i = 0; i < 3; i++)
            {
                var emr = emdFile.GetEmr(i);
                var builder = emr.ToBuilder();
                var newEmr = builder.ToEmr();

                var expectedData = emr.Data.ToArray();
                var actualData = newEmr.Data.ToArray();
                AssertByteArraysEqual(expectedData, actualData);
            }
        }

        [Fact]
        public void RebuildEmr3()
        {
            var installPath = TestInfo.GetInstallPath(2);
            var rofsFiles = Directory.GetFiles(installPath, "rofs*.dat");

            var repo = new FileRepository(installPath);
            foreach (var file in rofsFiles)
                repo.AddRE3Archive(file);

            var emdPath = Path.Combine(installPath, "room", "emd", "em1d.emd");
            var emdStream = repo.GetStream(emdPath);
            var emdFile = new EmdFile(BioVersion.Biohazard3, emdStream);
            for (var i = 0; i < 3; i++)
            {
                var emr = emdFile.GetEmr(i);
                var builder = emr.ToBuilder();
                var newEmr = builder.ToEmr();

                var expectedData = emr.Data.ToArray();
                var actualData = newEmr.Data.ToArray();
                AssertByteArraysEqual(expectedData, actualData);
            }
        }

        [Fact]
        public void RebuildEddEmr_PLD()
        {
            var installPath = TestInfo.GetInstallPath(1);
            var pldPath = Path.Combine(installPath, "data", "pl0", "pld", "pl00.pld");
            var pldFile = new PldFile(BioVersion.Biohazard2, pldPath);

            var edd = pldFile.GetEdd(0);
            var emr = pldFile.GetEmr(0);

            var animationBuilder = AnimationBuilder.FromEddEmr(edd, emr);
            var (eddRebuilt, emrRebuilt) = animationBuilder.ToEddEmr();

            AssertByteArraysEqual(edd.Data, eddRebuilt.Data);
            AssertByteArraysEqual(emr.Data, emrRebuilt.Data);
        }

        [Fact]
        public void RebuildEddEmr_PLW()
        {
            var installPath = TestInfo.GetInstallPath(1);
            var pldPath = Path.Combine(installPath, "data", "pl0", "pld", "pl00w02.plw");
            var pldFile = new PldFile(BioVersion.Biohazard2, pldPath);

            var edd = pldFile.GetEdd(0);
            var emr = pldFile.GetEmr(0);

            var animationBuilder = AnimationBuilder.FromEddEmr(edd, emr);
            var (eddRebuilt, emrRebuilt) = animationBuilder.ToEddEmr();

            AssertByteArraysEqual(edd.Data, eddRebuilt.Data);
            AssertByteArraysEqual(emr.Data, emrRebuilt.Data);
        }

        private static void AssertByteArraysEqual(ReadOnlyMemory<byte> expected, ReadOnlyMemory<byte> actual)
        {
            AssertByteArraysEqual(expected.ToArray(), actual.ToArray());
        }

        private static void AssertByteArraysEqual(byte[] expected, byte[] actual)
        {
            Assert.Equal(expected.Length, actual.Length);

            var incorrectIndex = -1;
            var startIndex = -1;
            var endIndex = -1;
            for (var i = 0; i < expected.Length; i++)
            {
                if (expected[i] != actual[i])
                {
                    incorrectIndex = i;
                    startIndex = Math.Max(0, i - 6);
                    endIndex = Math.Min(expected.Length - 1, i + 6);
                    break;
                }
            }

            if (startIndex >= 0 && endIndex >= 0)
            {
                var expectedSlice = ExtractBytes(expected, startIndex, endIndex);
                var actualSlice = ExtractBytes(actual, startIndex, endIndex);
                var assertMessage = $"Arrays differ at offset {incorrectIndex}.\nExpected: [{FormatByteArray(expectedSlice)}]\nActual:   [{FormatByteArray(actualSlice)}]";
                Assert.True(false, assertMessage);
            }
        }

        private static byte[] ExtractBytes(byte[] array, int startIndex, int endIndex)
        {
            var slice = new byte[endIndex - startIndex + 1];
            var sliceIndex = 0;
            for (var i = startIndex; i <= endIndex; i++)
            {
                slice[sliceIndex++] = array[i];
            }
            return slice;
        }

        private static string FormatByteArray(byte[] byteArray)
        {
            return string.Join(", ", byteArray.Select(x => $"0x{x:X2}"));
        }
    }
}
