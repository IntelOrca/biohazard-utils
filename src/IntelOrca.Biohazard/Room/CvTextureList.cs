using System;
using System.IO;
using IntelOrca.Biohazard.Extensions;

namespace IntelOrca.Biohazard.Room
{
    public readonly struct CvTextureList
    {
        public int BaseOffset { get; }
        public ReadOnlyMemory<byte> Data { get; }

        public CvTextureList(int baseOffset, ReadOnlyMemory<byte> data)
        {
            BaseOffset = baseOffset;
            Data = data;
        }

        public int Count => Data.GetSafeSpan<int>(0, 1)[0];
        public ReadOnlySpan<int> Offsets => Data.GetSafeSpan<int>(4, Count);

        public CvTextureList WithNewBaseOffset(int baseOffset)
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            bw.Write(Count);

            var originalOffsets = Offsets;
            for (var i = 0; i < Count; i++)
            {
                bw.Write(baseOffset + (originalOffsets[i] - BaseOffset));
            }

            bw.Write(Data.Slice((int)ms.Length));
            return new CvTextureList(baseOffset, ms.ToArray());
        }
    }
}
