using System;
using System.IO;
using IntelOrca.Biohazard.Extensions;
using IntelOrca.Biohazard.Room;
using IntelOrca.Biohazard.Script.Compilation;
using Xunit;
using Xunit.Abstractions;
using static IntelOrca.Biohazard.Tests.TestInfo;

namespace IntelOrca.Biohazard.Tests
{
    public class TestRecompile
    {
        private readonly ITestOutputHelper _output;

        public TestRecompile(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void RE2_Leon() => CheckRDTs(BioVersion.Biohazard2, x => x[x.Length - 5] == '0');

        [Fact]
        public void RE2_Claire() => CheckRDTs(BioVersion.Biohazard2, x => x[x.Length - 5] == '1');

        private void CheckRDTs(BioVersion version, Predicate<string> predicate)
        {
            var rdtFileNames = GetAllRdtFileNames(version);
            var fail = false;
            foreach (var rdtFileName in rdtFileNames)
            {
                var rdtId = RdtId.Parse(rdtFileName.Substring(rdtFileName.Length - 8, 3));
                if (rdtId == new RdtId(4, 0x05))
                    continue;
                if (rdtId == new RdtId(6, 0x05))
                    continue;
                if (rdtId.Stage > 6)
                    continue;
                if (!predicate(rdtFileName))
                    continue;

                var rdtFile = GetRdt(version, rdtFileName);
                var sPath = Path.ChangeExtension(rdtFileName, ".bio");
                fail |= AssertRecompileRdt(rdtFile, sPath);
            }
            Assert.False(fail);
        }

        private bool AssertRecompileRdt(IRdt rdtFile, string sPath)
        {
            var disassembly = rdtFile.DecompileScd();
            var scdCompiler = new ScdCompiler();
            var fail = false;
            int err;
            try
            {
                err = scdCompiler.Generate(new StringFileIncluder(sPath, disassembly), sPath);
            }
            catch
            {
                _output.WriteLine("Exception occured in '{0}'", sPath);
                return true;
            }
            if (err != 0)
            {
                foreach (var error in scdCompiler.Errors.Errors)
                {
                    _output.WriteLine(error.ToString());
                }
                fail = true;
            }
            return fail;
        }
    }
}
