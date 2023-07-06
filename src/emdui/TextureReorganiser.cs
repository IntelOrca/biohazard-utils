using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using IntelOrca.Biohazard;
using IntelOrca.Biohazard.Extensions;
using IntelOrca.Biohazard.Model;

namespace emdui
{
    internal class TextureReorganiser
    {
        public IModelMesh Mesh { get; private set; }
        public IModelMesh[] ExtraMeshes { get; private set; }
        public TimFile TimFile { get; private set; }
        public Rect[] Rects { get; private set; } = new Rect[0];
        public Rect[] UnplacedRects { get; private set; } = new Rect[0];

        public TextureReorganiser(IModelMesh mesh, IModelMesh[] extra, TimFile tim)
        {
            Mesh = mesh;
            ExtraMeshes = extra;
            TimFile = tim;
        }

        public void Detect(ITextureReorganiserConstraint constraint)
        {
            var allRects = new List<Rect>();
            var meshes = new[] { Mesh }.Concat(ExtraMeshes).ToArray();
            for (var i = 0; i < meshes.Length; i++)
            {
                allRects.AddRange(Detect(meshes[i], i * 0x100));
            }
            Rects = allRects.ToArray();
            while (MergeRects(constraint)) { }
            // InflateRects(32);
            // while (MergeRects()) { }
        }

        private Rect[] Detect(IModelMesh mesh, int basePartIndex)
        {
            var visitor = new UVMeshVisitor(basePartIndex);
            visitor.Accept(mesh);
            return visitor.Primitives;
        }

        public void Reorganise()
        {
            for (var i = 0; i < Rects.Length; i++)
            {
                ref var rect = ref Rects[i];
                rect.ConstrainToPage();
            }

            var results = new[]
            {
                Reorganise(0.75),
                Reorganise(0.50),
                Reorganise(0.25),
            };
            var bestResult = results.OrderBy(x => x.Score).First();
            Rects = bestResult.Rects;

            EditTim();
            EditUV(new NullTextureReorganiserConstraint());
        }

        private ReorganiseResult Reorganise(double scale)
        {
            Detect(new NullTextureReorganiserConstraint());
            var originalRects = Rects;

            var numPages = 4;
            var numToScale = 0;
            while (numPages > 2)
            {
                if (numToScale >= originalRects.Length)
                    break;

                var rects = originalRects.ToArray();
                var average = rects.Average(x => x.Width * x.Height);
                rects = rects.OrderBy(x => Math.Abs((x.Width * x.Height) - average)).ToArray();
                for (var i = 0; i < numToScale; i++)
                {
                    rects[i].Scale = scale;
                }

                Rects = rects;
                Rects = Reorg(new NullTextureReorganiserConstraint(), out numPages);

                numToScale++;
            }
            var result = new ReorganiseResult(numToScale, Rects);
            Rects = originalRects;
            return result;
        }

        public void ReorganiseWithConstraints(ITextureReorganiserConstraint constraint)
        {
            Detect(constraint);
            for (var i = 0; i < Rects.Length; i++)
            {
                ref var r = ref Rects[i];
                r.Scale = constraint.GetScale(in r);
            }
            Rects = Reorg(constraint, out _);
            EditTim();
            EditUV(constraint);
        }

        private struct ReorganiseResult
        {
            public int Score { get; }
            public Rect[] Rects { get; }

            public ReorganiseResult(int score, Rect[] rects)
            {
                Score = score;
                Rects = rects;
            }
        }

        private bool MergeRects(ITextureReorganiserConstraint constraint)
        {
            var merged = false;
            var newRects = new List<Rect>();
            foreach (var rect in Rects)
            {
                var skip = false;
                for (var i = 0; i < newRects.Count; i++)
                {
                    var rr = newRects[i];
                    if (constraint.CanMerge(in rr, in rect) && rr.UnionIfIntersects(rect))
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

        private void InflateRects(int amount)
        {
            for (var i = 0; i < Rects.Length; i++)
            {
                ref var r = ref Rects[i];
                r.Inflate(amount);
            }
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

        private Rect[] Reorg(ITextureReorganiserConstraint constraint, out int numPages)
        {
            var prioritisePart0 = false;
            var lockedRects = Rects
                .Where(x => constraint.IsLocked(x))
                .ToArray();
            var rects = Rects
                .Where(x => !constraint.IsLocked(x))
                .OrderByDescending(r => r.Height)
                .ToArray();
            if (prioritisePart0)
            {
                rects = rects
                    .OrderBy(r => r.ContainsPartIndex(0) ? 0 : 1)
                    .ThenByDescending(r => r.Height)
                    // .OrderBy(r => r.PartIndex)
                    .ToArray();
            }
            var bins = new List<Bin>();
            foreach (var rect in rects)
            {
                var page = constraint.GetPage(in rect);
                var binFound = false;
                foreach (var bin in bins)
                {
                    if (page != null)
                    {
                        var binPage = bin.GetPage(constraint);
                        if (binPage != null && binPage != page)
                        {
                            continue;
                        }
                    }
                    if (bin.AddRect(rect))
                    {
                        binFound = true;
                        break;
                    }
                }
                if (!binFound)
                {
                    bins.Add(new Bin(128, rect.Height, rect));
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
                    .ThenByDescending(b => b.Height)
                    .ToList();
            }
            bins = bins
                .OrderBy(x =>
                {
                    var p = x.GetPage(constraint);
                    return p == null ? int.MaxValue : p;
                })
                .ToList();

            var unplaced = new List<Rect>();
            foreach (var bin in bins)
            {
                var foundPage = false;
                foreach (var page in pages)
                {
                    var binPage = bin.GetPage(constraint);
                    if (binPage != null && binPage != page.Index)
                    {
                        continue;
                    }
                    if (page.CanFitBin(bin))
                    {
                        page.AddBin(bin);
                        foundPage = true;
                        break;
                    }
                }
                if (!foundPage)
                {
                    unplaced.AddRange(bin.Rects);
                }
            }
            UnplacedRects = unplaced.ToArray();
            numPages = pages.Count(x => x.Bins.Count != 0);
            var newRects = pages
                .SelectMany(x => x.GetRects())
                .ToArray();
            return lockedRects.Concat(newRects).ToArray();
        }

        private void EditTim()
        {
            var imageBlocks = new ImageBlock[Rects.Length];
            for (var i = 0; i < Rects.Length; i++)
            {
                var rect = Rects[i];
                imageBlocks[i] = new ImageBlock(TimFile, rect.OriginalX, rect.OriginalY, rect.OriginalWidth, rect.OriginalHeight);
            }

            TimFile = new TimFile(128 * 4, 256, 16);
            for (var i = 0; i < Rects.Length; i++)
            {
                var rect = Rects[i];
                var block = imageBlocks[i];
                TimFile.ImportPixels(rect.X, rect.Y, rect.Width, rect.Height, block.GetPixels(rect.Width, rect.Height), 0);
            }

            TimFile = TimFile.To8bpp((x, y) => x / 128);
            TimFile.DetectSize(out var width, out _);

            var newNumPages = (width + 127) / 128;
            TimFile.ResizeImage(newNumPages * 128, 256);
            TimFile.ResizeCluts(newNumPages);
        }

        private void EditUV(ITextureReorganiserConstraint constraint)
        {
            var meshes = new[] { Mesh }.Concat(ExtraMeshes).ToArray();
            for (var i = 0; i < meshes.Length; i++)
            {
                meshes[i] = EditUV(meshes[i], i * 0x100);
            }

            Mesh = meshes[0];
            ExtraMeshes = meshes
                .Skip(1)
                .ToArray();
        }

        private IModelMesh EditUV(IModelMesh mesh, int basePartIndex)
        {
            return mesh.EditMeshTextures(pt =>
            {
                var rect = new Rect();
                for (var i = 0; i < pt.NumPoints; i++)
                {
                    rect.AddPoint(pt.Page, pt.Points[i].U, pt.Points[i].V);
                }

                var parentRect = new Rect();
                foreach (var r in Rects)
                {
                    if (r.OriginallyContains(rect) && r.ContainsPart(basePartIndex + pt.PartIndex))
                    {
                        parentRect = r;
                        break;
                    }
                }

                if (parentRect.Width == 0 && parentRect.Height == 0)
                {
                    // WARNING no parent found
                }

                pt.Page = parentRect.Page;
                for (int j = 0; j < pt.NumPoints; j++)
                {
                    var x = (double)pt.Points[j].X;
                    var y = (double)pt.Points[j].Y;

                    // if (x == rect.Left)
                    //     x += 1;
                    // if (x == rect.Right - 1)
                    //     x -= 1;
                    // if (y == rect.Top)
                    //     y += 1;
                    // if (y == rect.Bottom - 1)
                    //     y -= 1;

                    x -= parentRect.OriginalX;
                    y -= parentRect.OriginalY;
                    x *= parentRect.Scale;
                    y *= parentRect.Scale;
                    x += parentRect.X;
                    y += parentRect.Y;
                    x = Math.Round(x);
                    y = Math.Round(y);

                    pt.Points[j] = new MeshExtensions.PrimitiveTexture.UV(parentRect.Page, (int)x, (int)y);
                }
            });
        }

        [DebuggerDisplay("Width = {Width} Height = {Height}")]
        private class Bin
        {
            private Rect _rect;

            public int Width { get; }
            public int Height { get; }


            public Bin Top { get; private set; }
            public Bin Bottom { get; private set; }

            public Bin(int width, int height, Rect rect)
            {
                Width = width;
                Height = height;
                _rect = rect;

                if (rect.Width > Width || rect.Height > Height)
                    throw new ArgumentException("rect does not fit in bin", nameof(rect));
            }

            public bool AddRect(Rect rect)
            {
                if (rect.Height > Height)
                    return false;

                var remainingWidth = Width - _rect.Width;
                if (rect.Width > remainingWidth)
                    return false;

                if (Top == null)
                {
                    Top = new Bin(remainingWidth, rect.Height, rect);
                    return true;
                }
                else if (Top.AddRect(rect))
                {
                    return true;
                }
                else if (Bottom == null)
                {
                    var bottomHeight = Height - Top.Height;
                    if (rect.Height > bottomHeight)
                        return false;

                    Bottom = new Bin(remainingWidth, bottomHeight, rect);
                    return true;
                }
                else
                {
                    return Bottom.AddRect(rect);
                }
            }

            public void GetRects(List<Rect> rects, int x, int y)
            {
                _rect.X = x;
                _rect.Y = y;
                rects.Add(_rect);
                Top?.GetRects(rects, x + _rect.Width, y);
                Bottom?.GetRects(rects, x + _rect.Width, y + Top.Height);
            }

            public int? GetPage(ITextureReorganiserConstraint constraint)
            {
                foreach (var rect in Rects)
                {
                    var page = constraint.GetPage(rect);
                    if (page != null)
                        return page;
                }
                return null;
            }

            public IEnumerable<Rect> Rects
            {
                get
                {
                    yield return _rect;
                    if (Top != null)
                    {
                        foreach (var rect in Top.Rects)
                        {
                            yield return rect;
                        }
                    }
                    if (Bottom != null)
                    {
                        foreach (var rect in Bottom.Rects)
                        {
                            yield return rect;
                        }
                    }
                }
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
                int i;
                for (i = 0; i < Bins.Count; i++)
                {
                    if (Bins[i].Height < bin.Height)
                    {
                        break;
                    }
                }
                Bins.Insert(i, bin);
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
                    bin.GetRects(rects, x, y);
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

        [DebuggerDisplay("Parts = [{Parts}] X = {X} Y = {Y} Width = {Width} Height = {Height}")]
        public struct Rect
        {
            public ushort[] PartIndicies { get; set; }
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

            public string Parts => string.Join(", ", PartIndicies);

            public bool ContainsPart(int partIndex) => Array.IndexOf(PartIndicies, (ushort)partIndex) != -1;

            public int Page
            {
                get
                {
                    var avg = Left + (Width / 2);
                    return avg / 128;
                }
            }

            public double Scale
            {
                get => (double)Width / OriginalWidth;
                set
                {
                    Width = (int)(OriginalWidth * value);
                    Height = (int)(OriginalHeight * value);
                }
            }

            public void AddPoint(Point p) => AddPoint(p.X, p.Y);

            public void AddPoint(int page, byte x, byte y) => AddPoint(new Point(page, x, y));

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
                    PartIndicies = PartIndicies.Concat(other.PartIndicies).Distinct().ToArray();
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

            public bool OriginallyContains(Rect other)
            {
                return OriginalX <= other.X &&
                       OriginalX + OriginalWidth >= other.X + other.Width &&
                       OriginalY <= other.Y &&
                       OriginalY + OriginalHeight >= other.Y + other.Height;
            }

            public void Inflate(int amount)
            {
                X -= amount;
                Y -= amount;
                Width += amount * 2;
                Height += amount * 2;
                ConstrainToPage();
            }

            public void ConstrainToPage()
            {
                var page = Page;
                var minX = page * 128;
                var maxX = (page + 1) * 128;
                var minY = 0;
                var maxY = 256;

                var left = Math.Max(minX, X);
                var top = Math.Max(minY, Y);
                var right = Math.Min(maxX, X + Width);
                var bottom = Math.Min(maxY, Y + Height);
                X = left;
                Y = top;
                Width = right - left;
                Height = bottom - top;
                OriginalX = X;
                OriginalY = Y;
                OriginalWidth = Width;
                OriginalHeight = Height;
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
            private int _basePartIndex;
            private int _partIndex;

            public Rect[] Primitives => _primitives.ToArray();

            public UVMeshVisitor(int basePartIndex)
            {
                _basePartIndex = basePartIndex;
            }

            public override bool VisitPart(int index)
            {
                _partIndex = _basePartIndex + index;
                return true;
            }

            public override void VisitPrimitive(int numPoints, byte page)
            {
                _primitive = new Rect();
                _primitive.PartIndicies = new[] { (ushort)_partIndex };
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

    internal class NullTextureReorganiserConstraint : ITextureReorganiserConstraint
    {
        public bool CanMerge(in TextureReorganiser.Rect a, in TextureReorganiser.Rect b) => true;
        public int? GetPage(in TextureReorganiser.Rect r) => null;
        public double GetScale(in TextureReorganiser.Rect r) => 1;
        public bool IsLocked(in TextureReorganiser.Rect r) => false;
    }

    internal interface ITextureReorganiserConstraint
    {
        bool CanMerge(in TextureReorganiser.Rect a, in TextureReorganiser.Rect b);
        int? GetPage(in TextureReorganiser.Rect r);
        double GetScale(in TextureReorganiser.Rect r);
        bool IsLocked(in TextureReorganiser.Rect r);
    }
}
