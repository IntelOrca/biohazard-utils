using System;

namespace IntelOrca.Biohazard.Room
{
    public readonly struct Esp
    {
        public ReadOnlyMemory<byte> Data { get; }

        public Esp(ReadOnlyMemory<byte> data)
        {
            Data = data;
        }
    }
}
