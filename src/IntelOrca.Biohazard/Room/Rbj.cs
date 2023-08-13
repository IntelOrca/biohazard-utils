using System;

namespace IntelOrca.Biohazard.Room
{
    public readonly struct Rbj
    {
        public BioVersion Version { get; }
        public ReadOnlyMemory<byte> Data { get; }

        public Rbj(BioVersion version, ReadOnlyMemory<byte> data)
        {
            Version = version;
            Data = data;
        }
    }
}
