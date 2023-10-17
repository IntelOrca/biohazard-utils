using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using emdui.Extensions;
using IntelOrca.Biohazard;
using IntelOrca.Biohazard.Extensions;
using IntelOrca.Biohazard.Model;
using Microsoft.Win32;

namespace emdui
{
    /// <summary>
    /// Interaction logic for TimView.xaml
    /// </summary>
    public partial class TimView : UserControl
    {
        public event EventHandler TimUpdated;

        private TimFile _timFile;
        private int _selectedPage;
        private UVPrimitive[] _primitives;
        private bool _readOnly;

        public TimFile Tim
        {
            get => _timFile;
            set
            {
                if (_timFile != value)
                {
                    _timFile = value;
                    RefreshImage();
                }
            }
        }

        public int SelectedPage
        {
            get => _selectedPage;
            set
            {
                if (_selectedPage != value)
                {
                    _selectedPage = value;
                    RefreshPage();
                }
            }
        }

        public UVPrimitive[] Primitives
        {
            get => _primitives;
            set
            {
                if (_primitives != value)
                {
                    _primitives = value;
                    RefreshPrimitives();
                }
            }
        }

        public bool ReadOnly
        {
            get => _readOnly;
            set
            {
                if (_readOnly != value)
                {
                    _readOnly = value;
                    _selectedPage = _readOnly ? -1 : 0;
                    RefreshPage();
                }
            }
        }

        public void SetPrimitivesFromMesh(IModelMesh mesh, int partIndex = -1)
        {
            var visitor = new UVVisitor(partIndex);
            visitor.Accept(mesh);
            Primitives = visitor.Primitives;
        }

        public TimView()
        {
            InitializeComponent();
            RefreshPage();
        }

        private void RefreshPage()
        {
            if (_readOnly)
                _selectedPage = -1;

            var borders = selectionContainer.Children.OfType<Border>().ToArray();
            for (var i = 0; i < borders.Length; i++)
            {
                var border = borders[i];
                border.Visibility = i == _selectedPage ? Visibility.Visible : Visibility.Hidden;
            }
        }

        private void RefreshPrimitives()
        {
            primitiveContainer.Children.Clear();

            if (_primitives == null)
                return;

            foreach (var p in _primitives)
            {
                var offset = p.Page * 128;
                var polygon = new Polygon();
                polygon.SnapsToDevicePixels = true;
                polygon.Stroke = Brushes.Lime;
                polygon.StrokeThickness = 0.5;
                polygon.StrokeMiterLimit = 1;
                polygon.Points.Add(new Point(p.U0 + offset, p.V0));
                polygon.Points.Add(new Point(p.U1 + offset, p.V1));
                if (p.IsQuad)
                {
                    polygon.Points.Add(new Point(p.U3 + offset, p.V3));
                }
                polygon.Points.Add(new Point(p.U2 + offset, p.V2));
                primitiveContainer.Children.Add(polygon);
            }
        }

        private void RefreshImage()
        {
            if (_timFile == null)
                return;

            image.Width = _timFile.Width;
            image.Height = _timFile.Height;
            image.Source = _timFile.ToBitmap();
        }

        private void TimView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TimView_MouseMove(sender, e);
        }

        private void TimView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed ||
                e.RightButton == MouseButtonState.Pressed)
            {
                var position = e.GetPosition(this);
                var pageWidth = ActualWidth / 4;
                var page = (int)(position.X / pageWidth);
                SelectedPage = page;
            }
        }

        private void RefreshAndRaiseTimEvent()
        {
            RefreshImage();
            TimUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void EnsureSelectedPageExists()
        {
            var minWidth = (_selectedPage + 1) * 128;
            if (_timFile.Width < minWidth)
            {
                _timFile.ResizeImage(minWidth, _timFile.Height);
            }
        }

        private void ImportPage(int page, TimFile source)
        {
            EnsureSelectedPageExists();
            _timFile.ImportPage(page, source);
            RefreshAndRaiseTimEvent();
        }

        private BitmapSource ImportBitmap(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var bitmapDecoder = BitmapDecoder.Create(fs, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                var frame = bitmapDecoder.Frames[0];
                return frame;
            }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "All Supported Files (*.png;*.tim)|*.png;*tim";
                openFileDialog.Filter += "|PNG (*.png)|*.png";
                openFileDialog.Filter += "|TIM (*.tim)|*.tim";
                if (openFileDialog.ShowDialog() == true)
                {
                    var path = openFileDialog.FileName;
                    if (path.EndsWith(".tim", StringComparison.OrdinalIgnoreCase))
                    {
                        _timFile = new TimFile(path);
                    }
                    else
                    {
                        _timFile = ImportBitmap(path).ToTimFile();
                    }
                }
                RefreshAndRaiseTimEvent();
            }
            catch (Exception ex)
            {
                ex.ShowMessageBox(this);
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "PNG (*.png)|*.png";
                saveFileDialog.Filter += "|TIM (*.tim)|*.tim";
                if (saveFileDialog.ShowDialog() == true)
                {
                    var path = saveFileDialog.FileName;
                    if (path.EndsWith(".tim", StringComparison.OrdinalIgnoreCase))
                    {
                        _timFile.Save(path);
                    }
                    else
                    {
                        _timFile.ToBitmap().Save(path);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ShowMessageBox(this);
            }
        }

        private void ImportPage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "All Supported Files (*.png;*.tim)|*.png;*tim";
                openFileDialog.Filter += "|PNG (*.png)|*.png";
                openFileDialog.Filter += "|TIM (*.tim)|*.tim";
                if (openFileDialog.ShowDialog() == true)
                {
                    var path = openFileDialog.FileName;
                    if (path.EndsWith(".tim", StringComparison.OrdinalIgnoreCase))
                    {
                        var timFile = new TimFile(openFileDialog.FileName);
                        ImportPage(_selectedPage, timFile);
                    }
                    else
                    {
                        ImportPage(_selectedPage, ImportBitmap(path).ToTimFile());
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ShowMessageBox(this);
            }
        }

        private void ExportPage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var numPages = _timFile.Width / 128;
                if (numPages <= _selectedPage)
                    return;

                var saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "TIM (*.tim)|*.tim";
                saveFileDialog.Filter += "|PNG (*.png)|*.png";
                if (saveFileDialog.ShowDialog() == true)
                {
                    var path = saveFileDialog.FileName;
                    var timFile = _timFile.ExportPage(_selectedPage);
                    if (path.EndsWith(".tim", StringComparison.OrdinalIgnoreCase))
                    {
                        timFile.Save(path);
                    }
                    else
                    {
                        timFile.ToBitmap().Save(path);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ShowMessageBox(this);
            }
        }

        private void DeletePage_Click(object sender, RoutedEventArgs e)
        {
            var numPages = (_timFile.Width + 127) / 128;
            if (_selectedPage >= numPages)
            {
                return;
            }
            if (numPages > 1 && _selectedPage == numPages - 1)
            {
                _timFile.ResizeImage((numPages - 1) * 128, _timFile.Height);
                _timFile.ResizeCluts(numPages - 1);
            }
            else
            {
                var xStart = _selectedPage * 128;
                for (var y = 0; y < _timFile.Height; y++)
                {
                    for (var x = 0; x < 128; x++)
                    {
                        _timFile.SetRawPixel(xStart + x, y, 0);
                    }
                }
            }
            RefreshAndRaiseTimEvent();
        }

        private void CopyPage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var numPages = _timFile.Width / 128;
                if (numPages <= _selectedPage)
                    return;

                var palette = _timFile.GetPalette(_selectedPage);
                var pixels = new ushort[128 * 256];
                var xStart = _selectedPage * 128;
                for (var y = 0; y < _timFile.Height; y++)
                {
                    for (var x = 0; x < 128; x++)
                    {
                        pixels[(y * 128) + x] = _timFile.GetRawPixel(xStart + x, y);
                    }
                }
                Clipboard.SetData(PageClipboardObject.Format, new PageClipboardObject(palette, pixels));
            }
            catch (Exception ex)
            {
                ex.ShowMessageBox(this);
            }
        }

        private void PastePage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var clipboardObject = Clipboard.GetData(PageClipboardObject.Format) as PageClipboardObject;
                if (clipboardObject == null)
                    return;

                EnsureSelectedPageExists();

                _timFile.SetPalette(_selectedPage, clipboardObject.Palette);
                var pixels = clipboardObject.Pixels;
                var xStart = _selectedPage * 128;
                for (var y = 0; y < _timFile.Height; y++)
                {
                    for (var x = 0; x < 128; x++)
                    {
                        _timFile.SetRawPixel(xStart + x, y, pixels[(y * 128) + x]);
                    }
                }
                RefreshAndRaiseTimEvent();
            }
            catch (Exception ex)
            {
                ex.ShowMessageBox(this);
            }
        }

        private void FixColours_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var pageToPaletteTrim = 1;
                var numPages = _timFile.Width / 128;

                // Must only have 3 cluts, otherwise inventory colours go weird
                _timFile.ResizeCluts(numPages);

                if (numPages <= pageToPaletteTrim)
                    return;

                var palette = _timFile.GetPalette(pageToPaletteTrim);
                var targetPalette = new byte[palette.Length];
                for (var i = 0; i < palette.Length; i++)
                {
                    if (i >= 240)
                    {
                        var oldValue = TimFile.Convert16to32(palette[i]);
                        targetPalette[i] = _timFile.FindBestPaletteEntry(pageToPaletteTrim, 0, 240, oldValue);
                    }
                    else
                    {
                        targetPalette[i] = (byte)i;
                    }
                }

                var xStart = pageToPaletteTrim * 128;
                for (var y = 0; y < _timFile.Height; y++)
                {
                    for (var x = 0; x < 128; x++)
                    {
                        var p = _timFile.GetRawPixel(xStart + x, y);
                        if (p > 239)
                        {
                            var newP = targetPalette[p];
                            _timFile.SetRawPixel(xStart + x, y, newP);
                        }
                    }
                }
                RefreshAndRaiseTimEvent();
            }
            catch (Exception ex)
            {
                ex.ShowMessageBox(this);
            }
        }

        [Serializable]
        private sealed class PageClipboardObject
        {
            public const string Format = "emdui_TIM_PAGE";

            public ushort[] Palette { get; }
            public ushort[] Pixels { get; }

            public PageClipboardObject(ushort[] palette, ushort[] pixels)
            {
                Palette = palette;
                Pixels = pixels;
            }
        }

        public struct UVPrimitive
        {
            public bool IsQuad { get; set; }
            public byte Page { get; set; }
            public byte U0 { get; set; }
            public byte V0 { get; set; }
            public byte U1 { get; set; }
            public byte V1 { get; set; }
            public byte U2 { get; set; }
            public byte V2 { get; set; }
            public byte U3 { get; set; }
            public byte V3 { get; set; }
        }

        private class UVVisitor : MeshVisitor
        {
            private readonly List<UVPrimitive> _primitives = new List<UVPrimitive>();
            private readonly int _partIndex;
            private UVPrimitive _primitive;
            private int _pointIndex;

            public UVPrimitive[] Primitives => _primitives.ToArray();

            public UVVisitor(int partIndex)
            {
                _partIndex = partIndex;
            }

            public override bool VisitPart(int index)
            {
                return _partIndex == index;
            }

            public override void VisitPrimitive(int numPoints, byte page)
            {
                _primitive = new UVPrimitive();
                if (numPoints == 4)
                    _primitive.IsQuad = true;
                _primitive.Page = page;
                _pointIndex = 0;
            }

            public override void VisitPrimitivePoint(ushort v, ushort n, byte tu, byte tv)
            {
                switch (_pointIndex)
                {
                    case 0:
                        _primitive.U0 = tu;
                        _primitive.V0 = tv;
                        break;
                    case 1:
                        _primitive.U1 = tu;
                        _primitive.V1 = tv;
                        break;
                    case 2:
                        _primitive.U2 = tu;
                        _primitive.V2 = tv;
                        break;
                    case 3:
                        _primitive.U3 = tu;
                        _primitive.V3 = tv;
                        break;
                }
                _pointIndex++;
            }

            public override void LeavePrimitive()
            {
                _primitives.Add(_primitive);
            }
        }

        private void Reorganise_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = MainWindow.Instance;
            var project = mainWindow.Project;
            var texturePackerWindow = new TexturePackerWindow();
            texturePackerWindow.Meshes = project.Files
                .Where(x => x.Content is ModelFile)
                .Select(x => ((ModelFile)x.Content).GetMesh(0))
                .ToArray();
            texturePackerWindow.Texture = project.MainTexture;
            texturePackerWindow.Refresh();

            texturePackerWindow.Owner = mainWindow;
            texturePackerWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (texturePackerWindow.ShowDialog() == true)
            {
                var updatedMeshes = texturePackerWindow.UpdatedMeshes;
                var index = 0;
                project.MainTexture = texturePackerWindow.UpdatedTexture;
                foreach (var file in project.Files)
                {
                    if (file.Content is ModelFile modelFile)
                    {
                        modelFile.SetMesh(0, updatedMeshes[index]);
                        index++;
                    }
                }
                mainWindow.LoadMesh(project.MainModel.GetMesh(0));
            }
        }

        private static byte ClampByte(int x) => (byte)Math.Max(0, Math.Min(255, x));
        private static byte ClampPage(int page, int x)
        {
            var pageLeft = page * 128;
            var pageRight = ((page + 1) * 128) - 1;
            return (byte)(Math.Max(pageLeft, Math.Min(pageRight, x)) % 128);
        }

        private void mainGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_readOnly)
            {
                e.Handled = true;
            }
        }

        private void SwapPage01_Click(object sender, RoutedEventArgs e)
        {
            var page0 = _timFile.ExportPage(0);
            var page1 = _timFile.ExportPage(1);
            _timFile.ImportPage(0, page1);
            _timFile.ImportPage(1, page0);

            var mainWindow = MainWindow.Instance;
            var project = mainWindow.Project;
            foreach (var file in project.Files)
            {
                if (file.Content is ModelFile modelFile)
                {
                    var mesh = modelFile.GetMesh(0);
                    mesh = mesh.SwapPages(0, 1, modelFile is PldFile || (modelFile is PlwFile plw && plw.Version != BioVersion.Biohazard1));
                    modelFile.SetMesh(0, mesh);
                }
            }

            RefreshAndRaiseTimEvent();
            mainWindow.LoadMesh(project.MainModel.GetMesh(0));
        }

        private void FixHD_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                for (var y = 0; y < 4; y++)
                {
                    for (var x = 0; x < 4; x++)
                    {
                        // _timFile.SetPalette(0, x, 0);
                        _timFile.SetRawPixel(3, 2, 0);
                    }
                }
                RefreshAndRaiseTimEvent();
            }
            catch (Exception ex)
            {
                ex.ShowMessageBox(this);
            }
        }
    }
}
