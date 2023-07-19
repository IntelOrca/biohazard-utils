using System;
using System.IO;

namespace IntelOrca.Biohazard.Extensions
{
    public static class TimFileExtensions
    {
        public static TimFile ExportPage(this TimFile tim, int page)
        {
            var palette = tim.GetPalette(page);
            var timFile = new TimFile(128, 256, 8);
            timFile.SetPalette(0, palette);
            var srcX = page * 128;
            for (var y = 0; y < 256; y++)
            {
                for (var x = 0; x < 128; x++)
                {
                    var p = tim.GetRawPixel(srcX + x, y);
                    timFile.SetRawPixel(x, y, p);
                }
            }
            return timFile;
        }

        public static void ImportPage(this TimFile target, int targetPage, TimFile source)
        {
            var palette = source.GetPalette(0);
            target.SetPalette(targetPage, palette);

            var minWidth = (targetPage + 1) * 128;
            if (target.Width < minWidth)
            {
                target.ResizeImage(minWidth, target.Height);
            }

            var dstX = targetPage * 128;
            for (var y = 0; y < 256; y++)
            {
                for (var x = 0; x < 128; x++)
                {
                    var p = source.GetRawPixel(x, y);
                    target.SetRawPixel(dstX + x, y, p);
                }
            }
        }

        public static void SwapPages(this TimFile target, int pageA, int pageB)
        {
            var pA = ExportPage(target, pageA);
            var pB = ExportPage(target, pageB);
            ImportPage(target, pageA, pB);
            ImportPage(target, pageB, pA);
        }

        public static TimFile WithWeaponTexture(this TimFile texture, TimFile weaponTexture, int page = 1)
        {
            var xOffset = page * 128;
            var tim = texture.Clone();
            for (var y = 0; y < 32; y++)
            {
                for (var x = 0; x < 56; x++)
                {
                    var p16 = weaponTexture.GetPixel(x, y);
                    var p8 = tim.FindBestPaletteEntry(page, p16);
                    tim.SetRawPixel(xOffset + 72 + x, 224 + y, p8);
                }
            }
            return tim;
        }

        public static byte[] ToBitmapBuffer(this TimFile tim, Func<int, int, int>? getClutIndex = null)
        {
            getClutIndex ??= (x, y) => 0;

            var stride = (((tim.Width * 3) + 3) / 4) * 4;
            var padding = stride - (tim.Width * 3);
            var rawDataLength = stride * tim.Height;

            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            // BMP header
            bw.Write((byte)0x42);
            bw.Write((byte)0x4D);
            bw.Write(0);
            bw.Write((ushort)0);
            bw.Write((ushort)0);
            bw.Write((uint)54);

            // DIB header
            bw.Write((uint)40);
            bw.Write((uint)tim.Width);
            bw.Write((uint)tim.Height);
            bw.Write((ushort)1);
            bw.Write((ushort)24);
            bw.Write((uint)0);
            bw.Write((uint)rawDataLength);
            bw.Write((uint)0x0B13); // DPI X
            bw.Write((uint)0x0B13); // DPI Y
            bw.Write((uint)0);
            bw.Write((uint)0);

            for (var y = 0; y < tim.Height; y++)
            {
                for (var x = 0; x < tim.Width; x++)
                {
                    var p = tim.GetPixel(x, y, getClutIndex(x, y));
                    bw.Write((p >> 0) & 0xFF);
                    bw.Write((p >> 8) & 0xFF);
                    bw.Write((p >> 16) & 0xFF);
                }
                for (var i = 0; i < padding; i++)
                {
                    bw.Write(0);
                }
            }

            ms.Position = 2;
            bw.Write((uint)ms.Length);

            return ms.ToArray();
        }
    }
}
