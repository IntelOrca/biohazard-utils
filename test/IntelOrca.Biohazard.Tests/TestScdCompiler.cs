using System.Linq;
using IntelOrca.Biohazard.Script;
using IntelOrca.Biohazard.Script.Compilation;
using Xunit;

namespace IntelOrca.Biohazard.Tests
{
    public class TestScdCompiler
    {
        [Fact]
        public void TestEmpty()
        {
            var expected = "0200090A3200010002000100";
            AssertScd(expected, @"
#version 2

proc init
{
    sleep(10, 50);
}
");
        }

        [Fact]
        public void TestIfStatement()
        {
            var expected = "020006000A0021010400090A32000800010002000100";
            AssertScd(expected, @"
#version 2

proc init
{
    if (ck(1, 4, 0)) {
        sleep(10, 50);
    }
}
");
        }

        [Fact]
        public void TestIfElseStatement()
        {
            var expected = "020006000C00210104002201040107000800090A3200010002000100";
            AssertScd(expected, @"
#version 2

proc init
{
    if (ck(1, 4, 0)) {
        set(1, 4, 1);
    } else {
        sleep(10, 50);
    }
}
");
        }

        private void AssertCompile(string script)
        {
            var scdCompiler = new ScdCompiler();
            var result = scdCompiler.Compile("temp.bio", script);
            Assert.Equal(0, result);
        }

        private void AssertScd(string expected, string script)
        {
            var scdCompiler = new ScdCompiler();
            var result = scdCompiler.Compile("temp.bio", script);
            Assert.Equal(0, result);

            var scdInit = scdCompiler.OutputInit;
            var scdMain = scdCompiler.OutputMain;
            var sInit = Disassemble(scdInit);
            var sMain = Disassemble(scdMain);

            var actual = string.Concat(scdInit.Concat(scdMain).Select(x => x.ToString("X2")).ToArray());
            Assert.Equal(expected, actual);
        }

        private string Disassemble(byte[] scd)
        {
            var scdReader = new ScdReader();
            return scdReader.Diassemble(scd, BioVersion.Biohazard2, BioScriptKind.Init, true);
        }
    }
}
