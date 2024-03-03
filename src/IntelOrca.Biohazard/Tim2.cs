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

        public ReadOnlyMemory<byte> GetPixelData(int index)
        {
            var offset = GetPictureOffset(index);
            var header = GetHeader(index);
            var pixelOffset = offset + header.HeaderSize;
            var pixelLength = header.ImageSize;
            return Data.Slice(pixelOffset, pixelLength);
        }

        public ReadOnlyMemory<byte> GetPaletteData(int index)
        {
            var offset = GetPictureOffset(index);
            var header = GetHeader(index);
            var paletteOffset = offset + header.HeaderSize + header.ImageSize;
            var paletteLength = header.ClutSize;
            return Data.Slice(paletteOffset, paletteLength);
        }

        public Picture Picture0 => new Picture(
            GetHeader(0),
            GetPaletteData(0),
            GetPixelData(0));

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

        public readonly struct Picture
        {
            private readonly PictureHeader _header;

            public PictureHeader Header => _header;
            public ReadOnlyMemory<byte> PaletteData { get; }
            public ReadOnlyMemory<byte> PixelData { get; }

            public Picture(PictureHeader header, ReadOnlyMemory<byte> paletteData, ReadOnlyMemory<byte> pixelData)
            {
                _header = header;
                PaletteData =
                    header.ImageColourType == 5 ?
                        FixPalette(paletteData) :
                        paletteData;
                PixelData = pixelData;
            }

            public int Width => _header.Width;
            public int Height => _header.Height;

            public int GetColour(int paletteIndex)
            {
                var palette = MemoryMarshal.Cast<byte, int>(PaletteData.Span);
                return palette[paletteIndex];
            }

            public byte GetPixelRaw(int x, int y)
            {
                ref readonly var header = ref _header;
                var pixelData = PixelData.Span;
                if (header.ImageColourType == 5)
                {
                    return pixelData[y * header.Width + x];
                }
                else
                {
                    var byteIndex = y * (header.Width / 2) + (x / 2);
                    var bb = pixelData[byteIndex];
                    if ((x & 1) == 0)
                        return (byte)(bb & 0x0F);
                    else
                        return (byte)(bb >> 4);
                }
            }

            public int GetPixel(int x, int y)
            {
                var raw = GetPixelRaw(x, y);
                return Rgba2Argb(GetColour(raw));
            }

            private static ReadOnlyMemory<byte> FixPalette(ReadOnlyMemory<byte> paletteData)
            {
                var colours = MemoryMarshal.Cast<byte, int>(paletteData.Span);
                var resultData = new byte[colours.Length * 4];
                var result = MemoryMarshal.Cast<byte, int>(resultData);
                var parts = colours.Length / 32;
                var stripes = 2;
                var colors = 8;
                var blocks = 2;
                var startIndex = 0;
                var i = 0;
                for (var part = 0; part < parts; part++)
                {
                    for (var block = 0; block < blocks; block++)
                    {
                        for (var stripe = 0; stripe < stripes; stripe++)
                        {
                            for (var color = 0; color < colors; color++)
                            {
                                result[i++] = colours[startIndex + part * colors * stripes * blocks + block * colors + stripe * stripes * colors + color];
                            }
                        }
                    }
                }
                return resultData;
            }

            private static int Rgba2Argb(int value)
            {
                int r = (value & 0xFF);
                int g = (value >> 8) & 0xFF;
                int b = (value >> 16) & 0xFF;
                int a = (value >> 24) & 0xFF;
                return (a << 24) | (r << 16) | (g << 8) | b;
            }
        }
    }
}
