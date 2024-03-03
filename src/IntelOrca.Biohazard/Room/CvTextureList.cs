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

        public ReadOnlySpan<CvTextureEntryGroup> Groups
        {
            get
            {
                var result = new CvTextureEntryGroup[Count];
                for (var i = 0; i < result.Length; i++)
                {
                    result[i] = GetGroup(i);
                }
                return result;
            }
        }

        private CvTextureEntryGroup GetGroup(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var offsets = Offsets;
            var offset = offsets[index] - BaseOffset;
            var endOffset = index < Count - 1 ? offsets[index + 1] - BaseOffset : Data.Length;
            var timData = Data[offset..endOffset];
            return new CvTextureEntryGroup(timData);
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

    public readonly struct CvTextureEntryGroup
    {
        public ReadOnlyMemory<byte> Data { get; }

        public CvTextureEntryGroup(ReadOnlyMemory<byte> data)
        {
            Data = data;
        }

        public int Count
        {
            get
            {
                var count = 0;
                var offset = 0;
                while (offset < Data.Length)
                {
                    var entrySize = Data.GetSafeSpan<int>(offset + 4, 1)[0];
                    var chunkSize = 32 + entrySize;
                    offset += chunkSize;
                    count++;
                }
                return count;
            }
        }

        public CvTextureEntry[] Entries
        {
            get
            {
                var result = new CvTextureEntry[Count];
                for (var i = 0; i < Count; i++)
                {
                    result[i] = GetEntry(i);
                }
                return result;
            }
        }

        private CvTextureEntry GetEntry(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var offset = GetOffset(index);
            var endOffset = index == Count - 1 ? Data.Length : GetOffset(index + 1);
            return new CvTextureEntry(Data[offset..endOffset]);
        }

        private int GetOffset(int index)
        {
            var i = 0;
            var offset = 0;
            while (i < index)
            {
                var entrySize = Data.GetSafeSpan<int>(offset + 4, 1)[0];
                var chunkSize = 32 + entrySize;
                offset += chunkSize;
                i++;
            }
            return offset;
        }
    }

    public readonly struct CvTextureEntry
    {
        public ReadOnlyMemory<byte> Data { get; }

        public int Magic => Data.GetSafeSpan<int>(0, 1)[0];
        public int Length => Data.GetSafeSpan<int>(4, 1)[0];

        public CvTextureEntry(ReadOnlyMemory<byte> data)
        {
            Data = data;
        }

        public CvTextureEntryKind Kind
        {
            get
            {
                var magic = Magic;
                if (magic == 0x324D4954)
                    return CvTextureEntryKind.TIM2;
                else if (magic == 0x00494C50)
                    return CvTextureEntryKind.PLI;
                return CvTextureEntryKind.Unknown;
            }
        }

        public Tim2 Tim2
        {
            get
            {
                if (Kind != CvTextureEntryKind.TIM2)
                    throw new InvalidOperationException();
                return new Tim2(Data.Slice(32, Length));
            }
        }
    }

    public enum CvTextureEntryKind
    {
        TIM2,
        PLI,
        Unknown,
    }
}
