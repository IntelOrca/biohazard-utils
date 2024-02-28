using System;
using System.IO;
using IntelOrca.Biohazard.Extensions;

namespace IntelOrca.Biohazard.Room
{
    public readonly struct CvModelList
    {
        public int BaseOffset { get; }
        public ReadOnlyMemory<byte> Data { get; }

        public CvModelList(int baseOffset, ReadOnlyMemory<byte> data)
        {
            BaseOffset = baseOffset;
            Data = data;
        }

        public int Count => (Data.GetSafeSpan<int>(0, 1)[0] - BaseOffset) / 4;
        public ReadOnlySpan<int> Offsets => Data.GetSafeSpan<int>(0, Count);

        public CvModelList WithNewBaseOffset(int baseOffset)
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            var originalOffsets = Offsets;
            for (var i = 0; i < Count; i++)
            {
                bw.Write(baseOffset + (originalOffsets[i] - BaseOffset));
            }

            bw.Write(Data.Slice((int)ms.Length));
            return new CvModelList(baseOffset, ms.ToArray());
        }
    }
}
