using System;
using IntelOrca.Biohazard.Extensions;

namespace IntelOrca.Biohazard.Room
{
    public readonly struct ScdProcedureList
    {
        public BioVersion Version { get; }
        public ReadOnlyMemory<byte> Data { get; }

        public ScdProcedureList(BioVersion version, ReadOnlyMemory<byte> data)
        {
            Version = version;
            Data = data;
        }

        public int Count
        {
            get
            {
                var firstOffset = Data.GetSafeSpan<ushort>(0, 1)[0];
                var numOffsets = firstOffset / 2;
                return numOffsets;
            }
        }

        public ScdProcedure this[int index]
        {
            get
            {
                var count = Count;
                var offset = Data.GetSafeSpan<ushort>(0, count)[index];
                var nextOffset = index == count - 1 ?
                    Data.Length :
                    Data.GetSafeSpan<ushort>(0, count)[index + 1];
                return new ScdProcedure(Version, Data.Slice(offset, nextOffset - offset));
            }
        }
    }
}
