using System.IO;
using Xunit;

namespace IntelOrca.Biohazard.Tests
{
    public class TestWaveformBuilder
    {
        [Fact]
        public void TestMixedChannelResample()
        {
            var installPath1 = TestInfo.GetInstallPath(0);
            var soundBite1 = Path.Combine(installPath1, "JPN", "sound", "BGM_0B.WAV");
            var soundBite2 = Path.Combine(installPath1, "JPN", "sound", "BGM_00.WAV");

            var waveformBuilder = new WaveformBuilder();
            waveformBuilder.Append(soundBite1);
            waveformBuilder.Append(soundBite2);

            waveformBuilder = new WaveformBuilder();
            waveformBuilder.Append(soundBite2);
            waveformBuilder.Append(soundBite1);
        }
    }
}
