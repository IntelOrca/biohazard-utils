using System;

namespace IntelOrca.Biohazard.Room
{
    public static class Rdt
    {
        public static IRdt FromFile(BioVersion version, string path)
        {
            return version switch
            {
                BioVersion.Biohazard1 => new Rdt1(path),
                BioVersion.Biohazard2 => new Rdt2(version, path),
                BioVersion.Biohazard3 => new Rdt2(version, path),
                _ => throw new NotSupportedException()
            };
        }
    }
}
