using System;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using emdui.Extensions;
using IntelOrca.Biohazard;
using IntelOrca.Biohazard.Extensions;
using IntelOrca.Biohazard.Model;
using Microsoft.Win32;

namespace emdui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }

        private Project _project;

        private IModelMesh _mesh;
        private TimFile _tim;
        private Emr _baseEmr;
        private Emr _emr;
        private Edd _edd;

        private ModelScene _scene;
        private DispatcherTimer _timer;

        private int _animationIndex;
        private double _time;
        private int _selectedPartIndex;
        private int _isolatedPartIndex;

        public Project Project => _project;

        public MainWindow()
        {
            InitializeComponent();
            // viewport0.SetCameraOrthographic(new Vector3D(-1, 0, 0));
            viewport1.SetCameraOrthographic(new Vector3D(0, 0, 1));
            _timer = new DispatcherTimer(DispatcherPriority.Render);
            _timer.Interval = TimeSpan.FromMilliseconds(40);
            _timer.Tick += _timer_Tick;
            _timer.IsEnabled = true;

            projectTreeView.MainWindow = this;
            Instance = this;
        }

        private void _timer_Tick(object sender, EventArgs e)
        {
            if (_mesh == null || _animationIndex == -1)
                return;

            var edd = _edd;
            if (edd == null)
                return;

            if (animationDropdown.Items.Count != edd.AnimationCount)
            {
                animationDropdown.Items.Clear();
                for (var i = 0; i < edd.AnimationCount; i++)
                {
                    animationDropdown.Items.Add($"Animation {i}");
                }
            }

            if (_animationIndex != animationDropdown.SelectedIndex)
            {
                animationDropdown.SelectedIndex = _animationIndex;
                // if (animationDropdown.SelectedIndex == -1)
                // {
                //     animationDropdown.SelectedIndex = 0;
                // }
                // _animationIndex = animationDropdown.SelectedIndex;
            }

            var frames = edd.GetFrames(_animationIndex);
            while (_time >= frames.Length)
            {
                _time -= frames.Length;
            }

            var animationKeyframe = (int)_time;
            var emrKeyframeIndex = frames[animationKeyframe].Index;

            timeTextBlock.Text = _time.ToString("0.00");
            _scene.SetKeyframe(emrKeyframeIndex);

            _time++;
        }

        private void SetTimFile(TimFile timFile)
        {
            _tim = timFile;
            _project.MainTexture = timFile;
            RefreshTimImage();
        }

        private void LoadProject(string path)
        {
            _project = Project.FromFile(path);
            projectTreeView.Project = _project;

            var model = _project.MainModel;
            IModelMesh mesh = null;
            for (var i = 0; i < model.NumChunks; i++)
            {
                if (model.GetChunkKind(i) == ModelFile.ChunkKind.Mesh)
                {
                    mesh = model.GetChunk<IModelMesh>(i);
                }
            }
            if (mesh != null)
            {
                LoadMesh(mesh);
            }
        }

        private void SaveModel(string path)
        {
            _project.Save(path);
        }

        private void RefreshTimImage()
        {
            timImage.Tim = null;
            timImage.Tim = _tim;
        }

        private void RefreshModelView()
        {
#if false
            var emrBuilder = _emr.ToBuilder();
            var morph = _project.MainModel.GetChunk<MorphData>(0);
            for (var i = 0; i < morph.NumParts; i++)
            {
                var partPosition = morph.GetPartPositionData(i);
                for (var j = 0; j < partPosition.Length; j++)
                {
                    emrBuilder.RelativePositions[j] = partPosition[j];
                }
                break;
            }
            _emr = emrBuilder.ToEmr();

            var md1Builder = ((Md1)_mesh).ToBuilder();
            for (var n = 0; n < morph.NumParts; n++)
            {
                var morphData = morph.GetMorphData(n, 1);
                var part = md1Builder.Parts[n == 0 ? 0 : 15];
                for (var i = 0; i < morphData.Length; i++)
                {
                    var v = new Md1.Vector(morphData[i].x, morphData[i].y, morphData[i].z);
                    part.Positions[i] = v;
                }
            }
            _mesh = md1Builder.ToMesh();
#endif

            _scene = new ModelScene();
            _scene.GenerateFrom(_mesh, _emr, _tim);
            viewport0.Scene = _scene;
            viewport1.Scene = _scene;

            RefreshHighlightedPart();
            RefreshStatusBar();
        }

        private void RefreshHighlightedPart()
        {
            if (_scene == null)
                return;

            _scene.HighlightPart(_isolatedPartIndex == -1 ? _selectedPartIndex : -1);
            RefreshTimPrimitives();
            RefreshRelativePositionTextBoxes();
        }

        private void RefreshTimPrimitives()
        {
            var partIndex = _isolatedPartIndex;
            if (partIndex == -1)
            {
                partIndex = _selectedPartIndex;
            }
            else
            {
                partIndex = 0;
            }

            if (partIndex == -1)
            {
                timImage.Primitives = null;
                return;
            }

            timImage.SetPrimitivesFromMesh(_mesh, partIndex);
        }

        private void RefreshRelativePositionTextBoxes()
        {
            var emr = _emr;
            var partIndex = _selectedPartIndex;
            if (partIndex != -1 && emr != null && partIndex < emr.NumParts)
            {
                var pos = emr.GetRelativePosition(partIndex);
                partPositionControl.Value = pos;
                partGroupBox.Visibility = Visibility.Visible;
            }
            else
            {
                partGroupBox.Visibility = Visibility.Collapsed;
            }
        }

        private void RefreshStatusBar()
        {
            var game = _project.Version == BioVersion.Biohazard2 ? "RE 2" : "RE 3";
            if (_project.Version == BioVersion.Biohazard1)
                game = "RE 1";

            var fileType = _project.MainModel is EmdFile ? ".EMD" : ".PLD";
            var numParts = _project.MainModel.GetMesh(0).NumParts;
            var basePolyCount = _project.MainModel.GetMesh(0).CountPolygons();
            var maxPolyCount = basePolyCount;
            foreach (var file in _project.Files)
            {
                if (file.Content is ModelFile modelFile)
                {
                    if (modelFile != _project.MainModel)
                    {
                        var total = basePolyCount + modelFile.GetMesh(0).CountPolygons();
                        maxPolyCount = Math.Max(maxPolyCount, total);
                    }
                }
            }

            fileTypeLabel.Content = $"{game} {fileType} File";
            numPartsLabel.Content = $"{numParts} parts";
            numPolygonsLabel.Content = $"{maxPolyCount} polygons";

        }

        private void mainWindow_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            // LoadProject(@"M:\git\rer\IntelOrca.Biohazard.BioRand\data\re2\pld0\barry\pl00.pld");
            // LoadProject(@"M:\git\rer\IntelOrca.Biohazard.BioRand\data\re3\pld0\hunk\PL00.PLD");
            // LoadProject(@"M:\git\rer\IntelOrca.Biohazard.BioRand\data\re3\pld0\leon\PL00.PLD");
            // LoadProject(@"M:\git\rer\IntelOrca.Biohazard.BioRand\data\re2\pld0\chris\PL00.PLD");
            // LoadProject(@"F:\games\re3\mod_biorand\DATA\PLD\PL00.PLD");
            // LoadProject(@"F:\games\re2\data\Pl0\emd0\em041.emd");
            // LoadProject(@"M:\git\rer\IntelOrca.Biohazard.BioRand\data\re2\pld0\chris\pl00.pld");
            // LoadProject(@"F:\games\re2\data\Pl0\emd0\em010.emd");
            // LoadProject(@"M:\git\rer\IntelOrca.Biohazard.BioRand\data\re2\pld0\ark\pl00.pld");
            // LoadProject(@"F:\games\re2\mod_biorand\pl0\emd0\em04a.emd");
            // LoadProject(@"M:\git\rer\IntelOrca.Biohazard.BioRand\data\re2\pld1\ashley\PL01.PLD");
            // LoadProject(@"M:\git\rer\IntelOrca.Biohazard.BioRand\data\re1\pld0\chris\char10.emd");
            LoadProject(@"M:\git\rer\IntelOrca.Biohazard.BioRand\data\re2\pld0\hunk\PL00.PLD");

#if false
            var texturePackerWindow = new TexturePackerWindow();
            texturePackerWindow.Meshes = _project.Files
                .Where(x => x.Content is ModelFile)
                .Select(x => ((ModelFile)x.Content).GetMesh(0))
                .ToArray();
            texturePackerWindow.Texture = _project.MainTexture;
            texturePackerWindow.Refresh();

            texturePackerWindow.Owner = this;
            texturePackerWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (texturePackerWindow.ShowDialog() == true)
            {
                var updatedMeshes = texturePackerWindow.UpdatedMeshes;
                var index = 0;
                _project.MainTexture = texturePackerWindow.UpdatedTexture;
                foreach (var file in _project.Files)
                {
                    if (file.Content is ModelFile modelFile)
                    {
                        modelFile.SetMesh(0, updatedMeshes[index]);
                        index++;
                    }
                }
                LoadMesh(_project.MainModel.GetMesh(0));
            }
#endif
#endif
        }

        private static byte ClampByte(int x) => (byte)Math.Max(0, Math.Min(255, x));
        private static byte ClampPage(int page, int x)
        {
            var pageLeft = page * 128;
            var pageRight = ((page + 1) * 128) - 1;
            return (byte)(Math.Max(pageLeft, Math.Min(pageRight, x)) % 128);
        }

        private void treeParts_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            RefreshHighlightedPart();
            RefreshRelativePositionTextBoxes();
        }

        private void timImage_TimUpdated(object sender, EventArgs e)
        {
            SetTimFile(timImage.Tim);
            RefreshModelView();
        }

        public void LoadMesh(IModelMesh mesh, TimFile texture = null)
        {
            _mesh = mesh;
            _tim = texture ?? _project.MainTexture;
            _baseEmr = _project.MainModel.GetEmr(0);
            _emr = _baseEmr;
            _edd = null;
            _isolatedPartIndex = -1;
            _animationIndex = -1;
            _selectedPartIndex = -1;
            RefreshModelView();
            RefreshTimImage();
        }

        public void LoadMeshWithoutArmature(IModelMesh mesh, TimFile texture = null)
        {
            _mesh = mesh;
            _tim = texture ?? _project.MainTexture;
            _baseEmr = null;
            _emr = null;
            _edd = null;
            _isolatedPartIndex = -1;
            _animationIndex = -1;
            _selectedPartIndex = -1;
            RefreshModelView();
            RefreshTimImage();
        }

        public void LoadMeshPart(IModelMesh mesh, int partIndex, TimFile texture = null)
        {
            _mesh = mesh;
            _tim = texture ?? _project.MainTexture;
            _edd = null;
            _emr = null;
            _isolatedPartIndex = partIndex;
            RefreshModelView();
            RefreshTimImage();
        }

        public void LoadTexture(TimFile texture)
        {
            _tim = texture;
            RefreshModelView();
            RefreshTimImage();
        }

        public void LoadWeaponModel(PlwFile plw)
        {
            _edd = plw.GetEdd(0);
            _baseEmr = _baseEmr.WithKeyframes(plw.GetEmr(0));
            _emr = _baseEmr;
            _isolatedPartIndex = -1;
            _animationIndex = -1;
            _selectedPartIndex = -1;

            if (_mesh is Md1 md1)
            {
                var targetBuilder = md1.ToBuilder();
                var sourceBuilder = plw.Md1.ToBuilder();
                targetBuilder.Parts[11] = sourceBuilder.Parts[0];
                _mesh = targetBuilder.ToMesh();
            }
            else if (_mesh is Md2 md2)
            {
                // TODO
            }

            _tim = _tim.WithWeaponTexture(plw.Tim);
            RefreshModelView();
            RefreshTimImage();
        }

        public void LoadAnimation(Emr emr, Edd edd, int index)
        {
            _emr = _baseEmr.WithKeyframes(emr);
            _edd = edd;
            _animationIndex = index;
            _time = 0;
            _selectedPartIndex = -1;
            RefreshModelView();
        }

        public void SelectPart(int partIndex)
        {
            _selectedPartIndex = partIndex;
            RefreshHighlightedPart();
        }

        private void menuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void OpenCommandBinding_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = _project == null ? null : System.IO.Path.GetDirectoryName(_project.MainPath);
            openFileDialog.Filter = "EMD/PLD Files (*.emd;*.pld)|*.emd;*.pld";
            if (openFileDialog.ShowDialog() == true)
            {
                LoadProject(openFileDialog.FileName);
            }
        }

        private void SaveCommandBinding_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_project == null)
                return;

            SaveModel(_project.MainPath);
        }

        private void SaveAsCommandBinding_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_project == null)
                return;

            CommonFileDialog
                .Save()
                .AddExtension(_project.MainModel is PldFile ? "*.pld" : "*.emd")
                .WithDefaultFileName(_project.MainPath)
                .Show(SaveModel);
        }

        private void ExitCommandBinding_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void CommandBinding_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {

        }

        private void SaveCommandBinding_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _project != null;
            e.Handled = true;
        }

        private void ExportForBioRandCommandBinding_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            var exportToBioRandWindow = new ExportForBioRandWindow();
            exportToBioRandWindow.Owner = this;
            exportToBioRandWindow.Project = _project;
            exportToBioRandWindow.ShowDialog();
        }

        public bool ShowFloor
        {
            get => Settings.Default.ShowFloor;
            set
            {
                Settings.Default.ShowFloor = value;
                Settings.Save();
                RefreshModelView();
            }
        }

        public void RefreshTreeView() => projectTreeView.Refresh();

        private void partPositionControl_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                var partIndex = _selectedPartIndex;
                if (partIndex != -1)
                {
                    var mainModel = _project.MainModel;
                    var oldEmr = mainModel.GetEmr(0);
                    var builder = oldEmr.ToBuilder();
                    builder.RelativePositions[_selectedPartIndex] = partPositionControl.Value;
                    var newEmr = builder.ToEmr();
                    mainModel.SetEmr(0, newEmr);
                    _baseEmr = newEmr;
                    _emr = _baseEmr;
                    RefreshModelView();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
