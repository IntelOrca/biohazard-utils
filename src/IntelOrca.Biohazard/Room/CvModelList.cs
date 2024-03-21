using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

        public int PageCount
        {
            get
            {
                var offsets = MemoryMarshal.Cast<byte, int>(Data.Span);
                for (var i = 0; i < offsets.Length; i++)
                {
                    if (offsets[i] != 0)
                    {
                        var firstOffset = offsets[i];
                        return (firstOffset - BaseOffset) / 4;
                    }
                }
                return 0;
            }
        }

        public ReadOnlySpan<int> Offsets => Data.GetSafeSpan<int>(0, PageCount);

        public ReadOnlySpan<CvModelListPage> Pages
        {
            get
            {
                var offsets = Offsets;
                var realOffsets = new List<int>();
                for (var i = 0; i < offsets.Length; i++)
                {
                    if (offsets[i] != 0)
                    {
                        realOffsets.Add(offsets[i]);
                    }
                }
                realOffsets.Add(BaseOffset + Data.Span.Length);

                var pages = new CvModelListPage[PageCount];
                for (var i = 0; i < pages.Length; i++)
                {
                    var offset = Offsets[i];
                    if (offset != 0)
                    {
                        var endOffset = realOffsets.First(x => x > offset);
                        var length = endOffset - offset;
                        var data = Data.Slice(offset - BaseOffset, length);
                        pages[i] = new CvModelListPage(data);
                    }
                }
                return pages;
            }
        }

        public CvModelList WithNewBaseOffset(int baseOffset)
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            var originalOffsets = Offsets;
            for (var i = 0; i < PageCount; i++)
            {
                var offset = originalOffsets[i];
                if (offset != 0)
                    offset = baseOffset + (originalOffsets[i] - BaseOffset);
                bw.Write(offset);
            }

            bw.Write(Data[(PageCount * 4)..]);
            return new CvModelList(baseOffset, ms.ToArray());
        }
    }

    public readonly struct CvModelListPage
    {
        public ReadOnlyMemory<byte> Data { get; }

        public CvModelListPage(ReadOnlyMemory<byte> data)
        {
            Data = data;
        }
    }
}
