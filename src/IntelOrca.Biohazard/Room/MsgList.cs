using System;
using IntelOrca.Biohazard.Extensions;

namespace IntelOrca.Biohazard.Room
{
    public readonly struct MsgList
    {
        public BioVersion Version { get; }
        public MsgLanguage Language { get; }
        public ReadOnlyMemory<byte> Data { get; }

        public MsgList(BioVersion version, MsgLanguage language, ReadOnlyMemory<byte> data)
        {
            Version = version;
            Language = language;
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

        public Msg this[int index]
        {
            get
            {
                var count = Count;
                var offset = Data.GetSafeSpan<ushort>(0, count)[index];
                var nextOffset = index == count - 1 ?
                    Data.Length :
                    Data.GetSafeSpan<ushort>(0, count)[index + 1];
                return new Msg(Version, Language, Data.Slice(offset, nextOffset - offset));
            }
        }
    }
}
