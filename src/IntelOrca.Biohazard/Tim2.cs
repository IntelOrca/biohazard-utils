using System;

namespace IntelOrca.Biohazard
{
    public readonly struct Tim2
    {
        public ReadOnlyMemory<byte> Data { get; }

        public Tim2(ReadOnlyMemory<byte> data)
        {
            Data = data;
        }
    }
}
