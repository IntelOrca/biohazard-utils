using System;
using System.Linq;
using System.Windows;
using IntelOrca.Biohazard;
using IntelOrca.Biohazard.Model;
using static emdui.TimView;

namespace emdui
{
    /// <summary>
    /// Interaction logic for TexturePackerWindow.xaml
    /// </summary>
    public partial class TexturePackerWindow : Window
    {
        public IModelMesh[] Meshes { get; set; }
        public TimFile Texture { get; set; }

        public IModelMesh[] UpdatedMeshes { get; set; }
        public TimFile UpdatedTexture { get; set; }

        public TexturePackerWindow()
        {
            InitializeComponent();
        }

        public void Refresh()
        {
            var numParts = Meshes[0].NumParts;
            var constraints = Enumerable
                .Range(0, numParts)
                .Select(x => new TexturePackerConstraint(x))
                .ToArray();
            constraints.First(x => x.PartIndex == 0).Page = 0;
            constraints.First(x => x.PartIndex == 8).Page = 1;
            constraints.First(x => x.PartIndex == 9).Page = 0;
            constraints.First(x => x.PartIndex == 11).Page = 1;
            constraints.First(x => x.PartIndex == 12).Page = 0;
            constraints.First(x => x.PartIndex == 14).Page = 1;
            constraintListView.ItemsSource = constraints;
            Update();
        }

        private void Update()
        {
            var mainMesh = Meshes[0];
            var extra = Meshes.Skip(1).ToArray();
            var reorg = new TextureReorganiser(mainMesh, extra, Texture);
            var constraint = new TextureReorganiserConstraint()
            {
                Constraints = constraintListView.ItemsSource as TexturePackerConstraint[]
            };
            reorg.ReorganiseWithConstraints(constraint);
            UpdatedMeshes = new[] { reorg.Mesh }.Concat(reorg.ExtraMeshes).ToArray();
            UpdatedTexture = reorg.TimFile;
            timView.Tim = UpdatedTexture;

            reorg.Detect(constraint);
            timView.Primitives = reorg.Rects
                .Select(x => new UVPrimitive()
                {
                    IsQuad = true,
                    Page = (byte)x.Page,
                    U0 = ClampPage(x.Page, x.Left),
                    V0 = ClampByte(x.Top),
                    U1 = ClampPage(x.Page, x.Right),
                    V1 = ClampByte(x.Top),
                    U3 = ClampPage(x.Page, x.Right),
                    V3 = ClampByte(x.Bottom),
                    U2 = ClampPage(x.Page, x.Left),
                    V2 = ClampByte(x.Bottom)
                })
                .ToArray();
        }

        private void Constraint_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Update();
        }

        private static byte ClampByte(int x) => (byte)Math.Max(0, Math.Min(255, x));
        private static byte ClampPage(int page, int x)
        {
            var pageLeft = page * 128;
            var pageRight = ((page + 1) * 128) - 1;
            return (byte)(Math.Max(pageLeft, Math.Min(pageRight, x)) % 128);
        }
    }

    internal class TextureReorganiserConstraint : ITextureReorganiserConstraint
    {
        public TexturePackerConstraint[] Constraints { get; set; }

        private int? GetPage(byte partIndex)
        {
            foreach (var constraint in Constraints)
            {
                if (constraint.PartIndex == partIndex)
                {
                    var p = constraint.Page;
                    return p == -1 ? null : (int?)p;
                }
            }
            return null;
        }

        public bool CanMerge(in TextureReorganiser.Rect a, in TextureReorganiser.Rect b)
        {
            var pageA = a.PartIndicies.Select(GetPage).Where(x => x != null).FirstOrDefault();
            var pageB = b.PartIndicies.Select(GetPage).Where(x => x != null).FirstOrDefault();
            var result = pageA == null || pageB == null || pageA == pageB;
            if (result)
            {
                var sA = GetScale(a);
                var sB = GetScale(b);
                if (sA != sB)
                    return false;
            }
            return result;
        }

        public int? GetPage(in TextureReorganiser.Rect r)
        {
            var page = r.PartIndicies.Select(GetPage).Where(x => x != null).FirstOrDefault();
            return page;
        }

        public double GetScale(in TextureReorganiser.Rect r)
        {
            var rr = r;
            var c = Constraints.FirstOrDefault(x => Array.IndexOf(rr.PartIndicies, (byte)x.PartIndex) != -1);
            if (c == null)
            {
                return 1;
            }
            if (c.Scale == 0 || c.Scale > 1)
                c.Scale = 1;
            return c.Scale;
        }
    }

    public class TexturePackerConstraint
    {
        public int PartIndex { get; set; }
        public int Page { get; set; } = -1;
        public double Scale { get; set; } = 1;

        public TexturePackerConstraint()
        {
        }

        public TexturePackerConstraint(int partIndex)
        {
            PartIndex = partIndex;
        }

        public string DisplayPartIndex => $"Part {PartIndex}";

        public string DisplayPage
        {
            get => Page == -1 ? "" : Page.ToString();
            set
            {
                if (int.TryParse(value, out var result))
                    Page = result;
                else
                    Page = -1;
            }
        }

        public string DisplayScale
        {
            get => Scale.ToString("0.00");
            set
            {
                if (double.TryParse(value, out var result))
                    Scale = result;
                else
                    Scale = 1;
            }
        }
    }
}
