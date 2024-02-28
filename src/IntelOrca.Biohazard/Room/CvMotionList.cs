using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using IntelOrca.Biohazard.Extensions;

namespace IntelOrca.Biohazard.Room
{
    public readonly struct CvMotionList
    {
        public int BaseOffset { get; }
        public ReadOnlyMemory<byte> Data { get; }

        public CvMotionList(int baseOffset, ReadOnlyMemory<byte> data)
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

        public ReadOnlySpan<CvMotionListPage> Pages
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

                var pages = new CvMotionListPage[PageCount];
                for (var i = 0; i < pages.Length; i++)
                {
                    var offset = Offsets[i];
                    if (offset != 0)
                    {
                        var endOffset = realOffsets.First(x => x > offset);
                        var length = endOffset - offset;
                        var data = Data.Slice(offset - BaseOffset, length);
                        pages[i] = new CvMotionListPage(data);
                    }
                }
                return pages;
            }
        }

        public CvMotionList WithNewBaseOffset(int baseOffset)
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            var originalOffsets = Offsets;
            for (var i = 0; i < PageCount; i++)
            {
                bw.Write(baseOffset + (originalOffsets[i] - BaseOffset));
            }

            bw.Write(Data.Slice((int)ms.Length));
            return new CvMotionList(baseOffset, ms.ToArray());
        }
    }

    public readonly struct CvMotionListPage
    {
        public ReadOnlyMemory<byte> Data { get; }

        public CvMotionListPage(ReadOnlyMemory<byte> data)
        {
            Data = data;
        }
    }
}
