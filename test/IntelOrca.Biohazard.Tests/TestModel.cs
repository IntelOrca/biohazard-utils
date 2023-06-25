using System;
using System.IO;
using System.Linq;
using IntelOrca.Biohazard.Model;
using Xunit;

namespace IntelOrca.Biohazard.Tests
{
    public class TestModel
    {
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
