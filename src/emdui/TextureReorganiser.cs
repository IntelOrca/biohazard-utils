using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using IntelOrca.Biohazard;
using IntelOrca.Biohazard.Model;

namespace emdui
{
    internal class TextureReorganiser
    {
        public IModelMesh Mesh { get; private set; }
        public TimFile TimFile { get; private set; }
        public Rect[] Rects { get; private set; } = new Rect[0];

        public TextureReorganiser(IModelMesh mesh, TimFile tim)
        {
            Mesh = mesh;
            TimFile = tim;
        }

        public void Detect()
        {
            var visitor = new UVMeshVisitor();
            visitor.Accept(Mesh);
            Rects = visitor.Primitives;
            while (MergeRects()) { }
        }

        public void Reorganise()
        {
            for (var i = 0; i < 3; i++)
            {
                if (Reorganise(i))
                    break;
            }
            EditTim();
            EditUV();
        }

        private bool Reorganise(int attempt)
        {
            Detect();
            // Scale(0.75, pi => pi == 0 || pi == 1);
            if (attempt == 1)
            {
                Scale(0.5, pi => pi == 2 || pi == 5 || pi == 9 || pi == 12);
            }
            else if (attempt == 2)
            {
                Scale(0.75, pi => true);
            }
            Rects = Reorg(out var numPages);
            return numPages <= 2;
        }

        private bool MergeRects()
        {
            var merged = false;
            var newRects = new List<Rect>();
            foreach (var rect in Rects)
            {
                var skip = false;
                for (var i = 0; i < newRects.Count; i++)
                {
                    var rr = newRects[i];
                    if (rr.UnionIfIntersects(rect))
                    {
                        newRects[i] = rr;
                        skip = true;
                        merged = true;
                        break;
                    }
                }
                if (!skip)
                    newRects.Add(rect);
            }
            Rects = newRects.ToArray();
            return merged;
        }

        private void Scale(double n, Func<int, bool> predicate)
        {
            for (var i = 0; i < Rects.Length; i++)
            {
                ref var r = ref Rects[i];
                if (r.PartIndicies.Any(pi => predicate(pi)))
                {
                    r.Width = (int)(r.Width * n);
                    r.Height = (int)(r.Height * n);
                }
            }
        }

        private Rect[] Reorg(out int numPages)
        {
            var prioritisePart0 = true;
            var rects = Rects
                .OrderByDescending(r => r.Width)
                .ToArray();
            if (prioritisePart0)
            {
                rects = rects
                    .OrderBy(r => r.ContainsPartIndex(0) ? 0 : 1)
                    // .OrderBy(r => r.PartIndex)
                    .ToArray();
            }
            var bins = new List<Bin>();
            foreach (var rect in rects)
            {
                var binFound = false;
                foreach (var bin in bins)
                {
                    if (bin.CanFitRect(rect))
                    {
                        bin.AddRect(rect);
                        binFound = true;
                        break;
                    }
                }
                if (!binFound)
                {
                    var bin = new Bin();
                    bin.AddRect(rect);
                    bins.Add(bin);
                }
            }

            var pages = new List<Page>();
            pages.Add(new Page(0));
            pages.Add(new Page(1));
            pages.Add(new Page(2));
            pages.Add(new Page(3));
            if (prioritisePart0)
            {
                bins = bins
                    .OrderBy(b => b.Rects.Any(x => x.ContainsPartIndex(0)) ? 0 : 1)
                    .ToList();
            }
            foreach (var bin in bins)
            {
                foreach (var page in pages)
                {
                    if (page.CanFitBin(bin))
                    {
                        page.AddBin(bin);
                        break;
                    }
                }
            }
            numPages = pages.Count(x => x.Bins.Count != 0);
            return pages
                .SelectMany(x => x.GetRects())
                .ToArray();
        }

        private void EditTim()
        {
            var imageBlocks = new ImageBlock[Rects.Length];
            for (var i = 0; i < Rects.Length; i++)
            {
                var rect = Rects[i];
                imageBlocks[i] = new ImageBlock(TimFile, rect.OriginalX, rect.OriginalY, rect.OriginalWidth, rect.OriginalHeight);
            }

            TimFile = new TimFile(TimFile.Width, TimFile.Height, 16);
            for (var i = 0; i < Rects.Length; i++)
            {
                var rect = Rects[i];
                var block = imageBlocks[i];
                TimFile.ImportPixels(rect.X, rect.Y, rect.Width, rect.Height, block.GetPixels(rect.Width, rect.Height), 0);
            }
        }

        private void EditUV()
        {
            var converter = new MeshConverter();
            var md1 = (Md1)converter.ConvertMesh(Mesh, BioVersion.Biohazard2);
            var builder = md1.ToBuilder();
            foreach (var part in builder.Parts)
            {
                for (var i = 0; i < part.TriangleTextures.Count; i++)
                {
                    var tt = part.TriangleTextures[i];
                    var points = new[]
                    {
                        new Point(tt.page & 0x0F, tt.u0, tt.v0),
                        new Point(tt.page & 0x0F, tt.u1, tt.v1),
                        new Point(tt.page & 0x0F, tt.u2, tt.v2),
                    };

                    var rect = new Rect();
                    rect.AddPoint(points[0]);
                    rect.AddPoint(points[1]);
                    rect.AddPoint(points[2]);

                    var parentRect = new Rect();
                    foreach (var r in Rects)
                    {
                        if (r.OriginallyIntersectsWith(rect))
                        {
                            parentRect = r;
                            break;
                        }
                    }

                    for (int j = 0; j < points.Length; j++)
                    {
                        points[j].X -= parentRect.OriginalX;
                        points[j].Y -= parentRect.OriginalY;
                        points[j].X = (int)(points[j].X * parentRect.Scale);
                        points[j].Y = (int)(points[j].Y * parentRect.Scale);
                        points[j].X += parentRect.X;
                        points[j].Y += parentRect.Y;
                    }

                    tt.u0 = (byte)points[0].PageX;
                    tt.v0 = (byte)points[0].Y;
                    tt.u1 = (byte)points[1].PageX;
                    tt.v1 = (byte)points[1].Y;
                    tt.u2 = (byte)points[2].PageX;
                    tt.v2 = (byte)points[2].Y;
                    tt.page = (byte)(0x80 | points[0].Page);

                    part.TriangleTextures[i] = tt;
                }
                for (var i = 0; i < part.QuadTextures.Count; i++)
                {
                    var qt = part.QuadTextures[i];

                    var points = new[]
                    {
                        new Point(qt.page & 0x0F, qt.u0, qt.v0),
                        new Point(qt.page & 0x0F, qt.u1, qt.v1),
                        new Point(qt.page & 0x0F, qt.u2, qt.v2),
                        new Point(qt.page & 0x0F, qt.u3, qt.v3),
                    };

                    var rect = new Rect();
                    rect.AddPoint(points[0]);
                    rect.AddPoint(points[1]);
                    rect.AddPoint(points[2]);
                    rect.AddPoint(points[3]);

                    var changeX = 0;
                    var changeY = 0;
                    foreach (var r in Rects)
                    {
                        if (r.OriginallyIntersectsWith(rect))
                        {
                            changeX = r.X - r.OriginalX;
                            changeY = r.Y - r.OriginalY;
                            break;
                        }
                    }

                    for (int j = 0; j < points.Length; j++)
                    {
                        points[j].X += changeX;
                        points[j].Y += changeY;
                    }

                    qt.u0 = (byte)points[0].PageX;
                    qt.v0 = (byte)points[0].Y;
                    qt.u1 = (byte)points[1].PageX;
                    qt.v1 = (byte)points[1].Y;
                    qt.u2 = (byte)points[2].PageX;
                    qt.v2 = (byte)points[2].Y;
                    qt.u3 = (byte)points[3].PageX;
                    qt.v3 = (byte)points[3].Y;
                    qt.page = (byte)(0x80 | points[0].Page);

                    part.QuadTextures[i] = qt;
                }
            }
            Mesh = converter.ConvertMesh(builder.ToMesh(), Mesh.Version);
        }

        [DebuggerDisplay("Width = {Width} Height = {Height}")]
        private class Bin
        {
            public int MaxWidth { get; } = 128;
            public int Width { get; private set; }
            public int Height { get; private set; }
            public List<Rect> Rects { get; } = new List<Rect>();

            public bool CanFitRect(Rect rect)
            {
                var remainingWidth = MaxWidth - Width;
                return rect.Width <= remainingWidth;
            }

            public void AddRect(Rect rect)
            {
                Rects.Add(rect);
                Width += rect.Width;
                Height = Math.Max(Height, rect.Height);
            }

            public Rect[] GetRects(int x, int y)
            {
                var rects = Rects.OrderByDescending(r => r.Height).ToArray();
                for (int i = 0; i < rects.Length; i++)
                {
                    ref var rect = ref rects[i];
                    rect.X = x;
                    rect.Y = y;
                    x += rect.Width;
                }
                return rects;
            }
        }

        private class Page
        {
            public int Index { get; }
            public int MaxHeight { get; } = 256;
            public int Height { get; private set; }
            public List<Bin> Bins { get; } = new List<Bin>();

            public Page(int index)
            {
                Index = index;
            }

            public bool CanFitBin(Bin bin)
            {
                var remainingHeight = MaxHeight - Height;
                return bin.Height <= remainingHeight;
            }

            public void AddBin(Bin bin)
            {
                Bins.Add(bin);
                Height += bin.Height;
            }

            public Rect[] GetRects()
            {
                var rects = new List<Rect>();
                var bins = Bins.OrderByDescending(b => b.Height).ToArray();
                var y = 0;
                foreach (var bin in bins)
                {
                    var x = Index * 128;
                    rects.AddRange(bin.GetRects(x, y));
                    y += bin.Height;
                }
                return rects.ToArray();
            }
        }

        [DebuggerDisplay("X = {X} Y = {Y}")]
        public struct Point
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int PageX => X % 128;
            public int Page => X / 128;

            public Point(int page, int x, int y)
            {
                X = x + (page * 128);
                Y = y;
            }
        }

        [DebuggerDisplay("X = {X} Y = {Y} Width = {Width} Height = {Height}")]
        public struct Rect
        {
            public byte[] PartIndicies { get; set; }
            public bool ContainsPartIndex(int partIndex) => PartIndicies.Contains((byte)partIndex);

            public int OriginalX { get; set; }
            public int OriginalY { get; set; }
            public int OriginalWidth { get; set; }
            public int OriginalHeight { get; set; }

            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }

            public int Left => X;
            public int Top => Y;
            public int Right => X + Width;
            public int Bottom => Y + Height;

            public int Page
            {
                get
                {
                    var avg = Left + (Width / 2);
                    return avg / 128;
                }
            }

            public double Scale => (double)Width / OriginalWidth;

            public void AddPoint(Point p) => AddPoint(p.X, p.Y);

            public void AddPoint(int x, int y)
            {
                if (Width == 0 && Height == 0)
                {
                    X = x;
                    Y = y;
                    Width = 1;
                    Height = 1;
                }
                else
                {
                    var minX = Math.Min(X, x);
                    var minY = Math.Min(Y, y);
                    var maxX = Math.Max(X + Width, x + 1);
                    var maxY = Math.Max(Y + Height, y + 1);

                    X = minX;
                    Y = minY;
                    Width = maxX - minX;
                    Height = maxY - minY;
                }
                OriginalX = X;
                OriginalY = Y;
                OriginalWidth = Width;
                OriginalHeight = Height;
            }

            public bool UnionIfIntersects(Rect other)
            {
                if (IntersectsWith(other))
                {
                    var minX = Math.Min(X, other.X);
                    var minY = Math.Min(Y, other.Y);
                    var maxX = Math.Max(X + Width, other.X + other.Width);
                    var maxY = Math.Max(Y + Height, other.Y + other.Height);

                    X = minX;
                    Y = minY;
                    Width = maxX - minX;
                    Height = maxY - minY;
                    OriginalX = X;
                    OriginalY = Y;
                    OriginalWidth = Width;
                    OriginalHeight = Height;
                    return true;
                }
                return false;
            }

            public bool IntersectsWith(Rect other)
            {
                return X < other.X + other.Width &&
                       X + Width > other.X &&
                       Y < other.Y + other.Height &&
                       Y + Height > other.Y;
            }

            public bool OriginallyIntersectsWith(Rect other)
            {
                return OriginalX < other.X + other.Width &&
                       OriginalX + OriginalWidth > other.X &&
                       OriginalY < other.Y + other.Height &&
                       OriginalY + OriginalHeight > other.Y;
            }
        }

        private readonly struct ImageBlock
        {
            public int Width { get; }
            public int Height { get; }
            public uint[] Pixels { get; }

            public ImageBlock(TimFile tim, int x, int y, int width, int height)
            {
                Pixels = new uint[width * height];
                Width = width;
                Height = height;

                var pixels = tim.GetPixels((xx, yy) => xx / 128);
                for (var yy = 0; yy < height; yy++)
                {
                    for (var xx = 0; xx < width; xx++)
                    {
                        Pixels[yy * width + xx] = tim.GetPixel(x + xx, y + yy, (x + xx) / 128);
                    }
                }
            }

            public uint[] GetPixelsCropped(int cropWidth, int cropHeight)
            {
                var croppedPixels = new uint[cropWidth * cropHeight];
                for (int yy = 0; yy < cropHeight; yy++)
                {
                    Array.Copy(Pixels, yy * Width, croppedPixels, yy * cropWidth, cropWidth);
                }
                return croppedPixels;
            }

            public uint[] GetPixels(int scaledWidth, int scaledHeight)
            {
                var scaledPixels = new uint[scaledWidth * scaledHeight];
                var xScale = (double)Width / scaledWidth;
                var yScale = (double)Height / scaledHeight;
                for (var sy = 0; sy < scaledHeight; sy++)
                {
                    for (var sx = 0; sx < scaledWidth; sx++)
                    {
                        var srcX = (int)(sx * xScale);
                        var srcY = (int)(sy * yScale);
                        var xFraction = (sx * xScale) - srcX;
                        var yFraction = (sy * yScale) - srcY;

                        var srcIndex = srcY * Width + srcX;
                        var srcRightIndex = srcIndex + 1;
                        var srcBottomIndex = srcIndex + Width;
                        var srcBottomRightIndex = srcBottomIndex + 1;

                        var pixelTopLeft = Pixels[srcIndex];
                        var pixelTopRight = (srcRightIndex < Pixels.Length) ? Pixels[srcRightIndex] : pixelTopLeft;
                        var pixelBottomLeft = (srcBottomIndex < Pixels.Length) ? Pixels[srcBottomIndex] : pixelTopLeft;
                        var pixelBottomRight = (srcBottomRightIndex < Pixels.Length) ? Pixels[srcBottomRightIndex] : pixelBottomLeft;

                        var interpolatedPixel = InterpolatePixels(pixelTopLeft, pixelTopRight, pixelBottomLeft, pixelBottomRight, xFraction, yFraction);
                        scaledPixels[sy * scaledWidth + sx] = interpolatedPixel;
                    }
                }
                return scaledPixels;
            }

            private static uint InterpolatePixels(uint topLeft, uint topRight, uint bottomLeft, uint bottomRight, double xFraction, double yFraction)
            {
                var alpha = InterpolateChannel((int)((topLeft >> 24) & 0xFF), (int)((topRight >> 24) & 0xFF), (int)((bottomLeft >> 24) & 0xFF), (int)((bottomRight >> 24) & 0xFF), xFraction, yFraction);
                var red = InterpolateChannel((int)((topLeft >> 16) & 0xFF), (int)((topRight >> 16) & 0xFF), (int)((bottomLeft >> 16) & 0xFF), (int)((bottomRight >> 16) & 0xFF), xFraction, yFraction);
                var green = InterpolateChannel((int)((topLeft >> 8) & 0xFF), (int)((topRight >> 8) & 0xFF), (int)((bottomLeft >> 8) & 0xFF), (int)((bottomRight >> 8) & 0xFF), xFraction, yFraction);
                var blue = InterpolateChannel((int)(topLeft & 0xFF), (int)(topRight & 0xFF), (int)(bottomLeft & 0xFF), (int)(bottomRight & 0xFF), xFraction, yFraction);
                var interpolatedPixel = ((uint)alpha << 24) | ((uint)red << 16) | ((uint)green << 8) | (uint)blue;
                return interpolatedPixel;
            }

            private static double InterpolateChannel(int c00, int c10, int c01, int c11, double xFraction, double yFraction)
            {
                var c0 = c00 + (c10 - c00) * xFraction;
                var c1 = c01 + (c11 - c01) * xFraction;
                var c = c0 + (c1 - c0) * yFraction;
                return c;
            }
        }

        private class UVMeshVisitor : MeshVisitor
        {
            private readonly List<Rect> _primitives = new List<Rect>();
            private Rect _primitive;
            private int _page;
            private int _partIndex;

            public Rect[] Primitives => _primitives.ToArray();

            public override bool VisitPart(int index)
            {
                _partIndex = index;
                return true;
            }

            public override void VisitPrimitive(int numPoints, byte page)
            {
                _primitive = new Rect();
                _primitive.PartIndicies = new[] { (byte)_partIndex };
                _page = page;
            }

            public override void VisitPrimitivePoint(ushort v, ushort n, byte tu, byte tv)
            {
                var uOffset = _page * 128;
                _primitive.AddPoint(uOffset + tu, tv);
            }

            public override void LeavePrimitive()
            {
                _primitives.Add(_primitive);
            }
        }
    }
}
