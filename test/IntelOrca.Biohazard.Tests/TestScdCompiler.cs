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
            var expected = "0200090A320001000400060001000100";
            AssertScd(expected, @"
#version 2

proc init
{
    sleep(10, 50);
}
");
        }

        [Fact]
        public void TestComment()
        {
            var expected = "020001000400060001000100";
            AssertScd(expected, @"
#version 2

proc init
{
    // = $ sleep(10, 50);
}
");
        }

        [Fact]
        public void TestBuiltInConstant()
        {
            var expected = "02004400002A00800012005A00830000008300000000000001000400060001000100";
            AssertScd(expected, @"
#version 2

proc init
{
    sce_em_set(0, 0, ENEMY_TYRANT_1, 0, 128, 0, 18, 0, 90, -32000, 0, -32000, 0, 0, 0);
}
");
        }

        [Fact]
        public void TestOrOperator()
        {
            var expected = "02002C02043100005CC7B4C9E015080700000000FFFF01000400060001000100";
            AssertScd(expected, @"
#version 2

proc init
{
    aot_set(2, SCE_MESSAGE, SAT_PL | SAT_MANUAL | SAT_FRONT, 0, 0, -14500, -13900, 5600, 1800, 0, 0, 0, 0, 255, 255);
}
");
        }

        [Fact]
        public void TestIfStatement()
        {
            var expected = "020006000A0021010400090A3200080001000400060001000100";
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
            var expected = "020006000C00210104002201040107000800090A320001000400060001000100";
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

        [Fact]
        public void TestWhileStatement()
        {
            var expected = "02000F060A0023001A0504000200100001000400060001000100";
            AssertScd(expected, @"
#version 2

proc init
{
    while (cmp(0, 26, CMP_NE, 4))
    {
        evt_next();
    }
}
");
        }

        [Fact]
        public void TestFork()
        {
            var expected = "0200010006000C000E0004FF180201000100090A1E000100";
            AssertScd(expected, @"
#version 2

proc main
{
    fork wait_for_mr_x
}

proc wait_for_mr_x
{
    sleep(10, 30);
}
");
        }

        [Fact]
        public void TestCall()
        {
            var expected = "0200010006000A000C00180201000100090A1E000100";
            AssertScd(expected, @"
#version 2

proc main
{
    wait_for_mr_x();
}

proc wait_for_mr_x
{
    sleep(10, 30);
}
");
        }

        private void AssertCompile(string script)
        {
            var scdCompiler = new ScdCompiler();
            var result = scdCompiler.Generate("temp.bio", script);
            Assert.Equal(0, result);
        }

        private void AssertScd(string expected, string script)
        {
            var scdCompiler = new ScdCompiler();
            var result = scdCompiler.Generate("temp.bio", script);
            Assert.Equal(0, result);

            var scdInit = scdCompiler.OutputInit;
            var scdMain = scdCompiler.OutputMain;
            var sInit = Disassemble(scdInit, BioScriptKind.Init);
            var sMain = Disassemble(scdMain, BioScriptKind.Main);

            var actual = string.Concat(scdInit.Concat(scdMain).Select(x => x.ToString("X2")).ToArray());
            Assert.Equal(expected, actual);
        }

        private string Disassemble(byte[] scd, BioScriptKind kind)
        {
            var scdReader = new ScdReader();
            return scdReader.Diassemble(scd, BioVersion.Biohazard2, kind, true);
        }
    }
}
