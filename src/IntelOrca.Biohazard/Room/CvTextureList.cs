using System;
using System.IO;
using System.Runtime.InteropServices;
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

        public ReadOnlySpan<Tim2> Tims
        {
            get
            {
                var result = new Tim2[Count];
                for (var i = 0; i < result.Length; i++)
                {
                    result[i] = GetTim(i);
                }
                return result;
            }
        }

        private Tim2 GetTim(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var offsets = Offsets;
            var offset = offsets[index] - BaseOffset;
            var endOffset = index < Count - 1 ? offsets[index + 1] - BaseOffset : Data.Length;
            var timData = Data[offset..endOffset];
            var actualTimLength = MemoryMarshal.Cast<byte, int>(timData.Span.Slice(4, 4))[0];
            var actualTimData = timData.Slice(32, actualTimLength);
            return new Tim2(actualTimData);
        }

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
