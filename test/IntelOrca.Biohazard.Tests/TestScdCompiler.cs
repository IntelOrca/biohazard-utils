using IntelOrca.Biohazard.Script.Compilation;
using Xunit;

namespace IntelOrca.Biohazard.Tests
{
    public class TestScdCompiler
    {
        [Fact]
        public void TestEmpty()
        {
            var script = @"
#version 2

proc init
{
    sleep(10, 50);
}
";

            var scdCompiler = new ScdCompiler();
            scdCompiler.Compile("temp.bio", script);
        }
    }
}
