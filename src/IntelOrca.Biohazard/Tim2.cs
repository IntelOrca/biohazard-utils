using System;
using System.Runtime.InteropServices;
using IntelOrca.Biohazard.Extensions;

namespace IntelOrca.Biohazard
{
    public readonly struct Tim2
    {
        public ReadOnlyMemory<byte> Data { get; }

        public Tim2(ReadOnlyMemory<byte> data)
        {
            Data = data;
        }

        public int Magic => Data.GetSafeSpan<int>(0, 1)[0];
        public byte Revision => Data.Span[4];
        public byte Format => Data.Span[5];
        public short PictureCount => Data.GetSafeSpan<short>(6, 1)[0];

        private int GetPictureOffset(int index)
        {
            var headerEnd = Format > 0 ? 0x80 : 0x10;
            return headerEnd;
        }

        public PictureHeader GetHeader(int index)
        {
            var offset = GetPictureOffset(index);
            var header = MemoryMarshal.Cast<byte, PictureHeader>(Data.Span[offset..])[0];
            return header;
        }

        public ReadOnlySpan<byte> GetPixelData(int index)
        {
            var offset = GetPictureOffset(index);
            var header = GetHeader(index);
            var pixelOffset = offset + header.HeaderSize;
            var pixelLength = header.ImageSize;
            return Data.Span.Slice(pixelOffset, pixelLength);
        }

        public ReadOnlySpan<byte> GetPaletteData(int index)
        {
            var offset = GetPictureOffset(index);
            var header = GetHeader(index);
            var paletteOffset = offset + header.HeaderSize + header.ImageSize;
            var paletteLength = header.ClutSize;
            return Data.Span.Slice(paletteOffset, paletteLength);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PictureHeader
        {
            public int TotalSize;
            public int ClutSize;
            public int ImageSize;
            public short HeaderSize;
            public short ColoursPerClut;
            public byte Format;
            public byte MipmapCount;
            public byte ClutColourType;
            public byte ImageColourType;
            public short Width;
            public short Height;
            public long GsTex0;
            public long GsTex1;
            public int GsFlags;
            public int GsClut;
        }
    }
}
