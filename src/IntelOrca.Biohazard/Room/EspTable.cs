using System;
using System.Runtime.InteropServices;
using IntelOrca.Biohazard.Extensions;

namespace IntelOrca.Biohazard.Room
{
    public readonly struct EspTable
    {
        public ReadOnlyMemory<byte> Data { get; }

        public EspTable(ReadOnlyMemory<byte> data)
        {
            Data = data;
        }

        public ReadOnlySpan<byte> Ids => Data.Span.Slice(0, 8);
        public ReadOnlySpan<int> Offsets => MemoryMarshal.Cast<byte, int>(Data.Span.TruncateStartBy(-8 * 4));

        public int Count
        {
            get
            {
                var count = 0;
                var span = Ids;
                for (var i = 0; i < span.Length; i++)
                {
                    if (span[i] != 0xFF)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public Eff GetEff(int index)
        {
            var offsetIndex = 8 - Count + index;
            var offset = Offsets[offsetIndex];
            var nextOffset = index == Count - 1
                ? Data.Length - (8 * 4)
                : Offsets[offsetIndex + 1];
            var data = Data.Slice(offset, nextOffset - offset);
            return new Eff(data);
        }
    }
}
