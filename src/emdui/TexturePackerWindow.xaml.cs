﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        private CancellationTokenSource _cts;
        private UVPrimitive[] _oldBoundaries;
        private UVPrimitive[] _newBoundaries;

        public IModelMesh[] Meshes { get; set; }
        public TimFile Texture { get; set; }

        public IModelMesh[] UpdatedMeshes { get; set; }
        public TimFile UpdatedTexture { get; set; }

        public TexturePackerWindow()
        {
            InitializeComponent();
        }

        public async void Refresh() => await RefreshAsync();

        public async Task RefreshAsync()
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
            await UpdateAsync();
        }

        private async Task UpdateAsync()
        {
            // Cancel any previous reorg
            _cts?.Cancel();

            // Start new reorg
            var partConstraints = constraintListView.ItemsSource as TexturePackerConstraint[];
            var constaints = partConstraints.Select(x => x.PartConstraint).ToArray();
            _cts = new CancellationTokenSource();
            await UpdateAsync(constaints, _cts.Token);
        }

        private async Task UpdateAsync(PartConstraint[] constraints, CancellationToken ct)
        {
            var mainMesh = Meshes[0];
            var extra = Meshes.Skip(1).ToArray();
            var isPld = MainWindow.Instance.Project.MainModel is PldFile;
            var constraint = new TextureReorganiserConstraint(isPld, constraints);
            var reorg = new TextureReorganiser(mainMesh, extra, Texture);
            reorg.Detect(constraint);
            var oldRects = reorg.Rects.ToArray();

            await Task.Run(() => reorg.ReorganiseWithConstraints(constraint));
            if (ct.IsCancellationRequested)
                return;

            UpdatedMeshes = new[] { reorg.Mesh }.Concat(reorg.ExtraMeshes).ToArray();
            UpdatedTexture = reorg.TimFile;
            timViewOld.Tim = Texture;
            timView.Tim = UpdatedTexture;

            var partConstraints = constraintListView.ItemsSource as TexturePackerConstraint[];
            var status = true;
            foreach (var pc in partConstraints)
            {
                pc.Status = true;
            }
            foreach (var r in reorg.UnplacedRects)
            {
                foreach (var pi in r.PartIndicies)
                {
                    var pc = partConstraints.FirstOrDefault(x => x.PartIndex == pi);
                    if (pc != null)
                    {
                        pc.Status = false;
                        status = false;
                    }
                }
            }
            warningTextBlock.Visibility = status ? Visibility.Hidden : Visibility.Visible;

            reorg.Detect(constraint);
            _oldBoundaries = SetPrimitives(timViewOld, oldRects);
            _newBoundaries = SetPrimitives(timView, reorg.Rects);
        }

        private UVPrimitive[] SetPrimitives(TimView timView, TextureReorganiser.Rect[] rects)
        {
            timView.Primitives = rects
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
            return timView.Primitives;
        }

        private async void Constraint_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                await UpdateAsync();
            }
            catch
            {
            }
        }

        private static byte ClampByte(int x) => (byte)Math.Max(0, Math.Min(255, x));
        private static byte ClampPage(int page, int x)
        {
            var pageLeft = page * 128;
            var pageRight = ((page + 1) * 128) - 1;
            return (byte)(Math.Max(pageLeft, Math.Min(pageRight, x)) % 128);
        }

        private void constraintListView_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var listView = (ListView)sender;
            var hitTestResult = VisualTreeHelper.HitTest(listView, e.GetPosition(listView));
            var listViewItem = FindAncestor<ListViewItem>(hitTestResult.VisualHit);
            if (listViewItem != null)
            {
                if (listView.ItemContainerGenerator.ItemFromContainer(listViewItem) is TexturePackerConstraint c)
                {
                    timViewOld.SetPrimitivesFromMesh(Meshes[0], c.PartIndex);
                    if (UpdatedMeshes != null)
                        timView.SetPrimitivesFromMesh(UpdatedMeshes[0], c.PartIndex);
                }
            }
        }

        private void constraintListView_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            timViewOld.Primitives = _oldBoundaries;
            timView.Primitives = _newBoundaries;
        }

        // Helper method to find the ancestor of a specific type in the visual tree
        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T ancestor)
                {
                    return ancestor;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }

    internal class TextureReorganiserConstraint : ITextureReorganiserConstraint
    {
        public bool IsPld { get; }
        public PartConstraint[] Constraints { get; }

        public TextureReorganiserConstraint(bool isPld, PartConstraint[] constraints)
        {
            IsPld = isPld;
            Constraints = constraints;
        }

        private int? GetPage(ushort partIndex)
        {
            // PLWs always go on page 1
            if (IsPld && (partIndex & ~0xFF) != 0)
                return 1;

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
            var lockedA = IsLocked(a);
            var lockedB = IsLocked(b);
            if (lockedA || lockedB)
                return lockedA && lockedB;

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
            var c = Constraints.FirstOrDefault(x => rr.ContainsPart(x.PartIndex));
            if (c.Scale == 0 || c.Scale > 1)
                c.Scale = 1;
            return c.Scale;
        }

        public bool IsLocked(in TextureReorganiser.Rect r)
        {
            if (!IsPld)
                return false;

            var otherRect = new TextureReorganiser.Rect();
            otherRect.X = 128 + 72;
            otherRect.Y = 224;
            otherRect.Width = 56;
            otherRect.Height = 32;
            // return r.IntersectsWith(otherRect);
            return r.X >= otherRect.X && r.Y >= otherRect.Y &&
                r.Right <= otherRect.Right && r.Bottom <= otherRect.Bottom;
        }
    }

    public class TexturePackerConstraint : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _status;
        private string _scale = "1.0";

        public int PartIndex { get; set; }
        public int Page { get; set; } = -1;
        public double Scale { get; set; } = 1;
        public bool Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayBackground)));
                }
            }
        }

        public TexturePackerConstraint()
        {
        }

        public TexturePackerConstraint(int partIndex)
        {
            PartIndex = partIndex;
        }

        public PartConstraint PartConstraint => new PartConstraint(PartIndex, Page, Scale);

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
            get => _scale;
            set
            {
                _scale = value;
                if (double.TryParse(value, out var result))
                    Scale = result;
                else
                    Scale = 1;
            }
        }

        public string DisplayPartName => PartName.GetPartName(MainWindow.Instance.Project.Version, PartIndex);

        public Brush DisplayBackground => Status ? Brushes.LightGreen : Brushes.IndianRed;
    }

    public struct PartConstraint
    {
        public int PartIndex { get; set; }
        public int Page { get; set; }
        public double Scale { get; set; }

        public PartConstraint(int partIndex, int page, double scale)
        {
            PartIndex = partIndex;
            Page = page;
            Scale = scale;
        }
    }
}
