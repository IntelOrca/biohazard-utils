using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using emdui.Extensions;
using IntelOrca.Biohazard;
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

            LoadMesh(_project.MainModel.GetMesh(0));
            RefreshStatusBar();
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
            _scene = new ModelScene();
            _scene.GenerateFrom(_mesh, _emr, _tim);
            viewport0.Scene = _scene;
            viewport1.Scene = _scene;

            RefreshHighlightedPart();
            RefreshRelativePositionTextBoxes();
        }

        private void RefreshHighlightedPart()
        {
            if (_scene == null)
                return;

            _scene.HighlightPart(_isolatedPartIndex == -1 ? _selectedPartIndex : -1);
            RefreshTimPrimitives();
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
                partXTextBox.Text = pos.x.ToString();
                partYTextBox.Text = pos.y.ToString();
                partZTextBox.Text = pos.z.ToString();
                partXTextBox.Visibility = Visibility.Visible;
                partYTextBox.Visibility = Visibility.Visible;
                partZTextBox.Visibility = Visibility.Visible;
            }
            else
            {
                partXTextBox.Visibility = Visibility.Hidden;
                partYTextBox.Visibility = Visibility.Hidden;
                partZTextBox.Visibility = Visibility.Hidden;
            }
        }

        private void RefreshStatusBar()
        {
            var game = _project.Version == BioVersion.Biohazard2 ? "RE 2" : "RE 3";
            var fileType = _project.MainModel is EmdFile ? ".EMD" : ".PLD";
            fileTypeLabel.Content = $"{game} {fileType} File";
            // numPartsLabel.Content = $"{GetNumParts()} parts";
        }

        private void RefreshPrimitives()
        {
            var md1 = _mesh as Md1;
            var selectedIndex = _selectedPartIndex * 2;
            if (selectedIndex >= 0 && selectedIndex < md1.NumObjects)
            {
                var objTri = md1.Objects[selectedIndex + 0];
                var objQuad = md1.Objects[selectedIndex + 1];
                var triangles = md1.GetTriangles(objTri);
                var quads = md1.GetQuads(objQuad);

                var items = new List<string>();
                for (int i = 0; i < triangles.Length; i++)
                    items.Add($"Triangle {i}");
                for (int i = 0; i < quads.Length; i++)
                    items.Add($"Quad {i}");

                listPrimitives.ItemsSource = items;
            }
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
            // ExportToBioRand(@"C:\Users\Ted\Desktop\ethan");
            // LoadProject(@"M:\temp\re3extracted\ROOM\EMD\EM1A.EMD");
            // LoadProject(@"F:\games\re1\JPN\ENEMY\CHAR10.EMD");
            LoadProject(@"F:\games\re1\mod_test\ENEMY\CHAR10.EMD");
#endif
        }

        private void treeParts_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            RefreshHighlightedPart();
            RefreshRelativePositionTextBoxes();
        }

        private void listPrimitives_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var md1 = _mesh as Md1;
            var objIndex = _selectedPartIndex * 2;
            if (objIndex >= 0 && objIndex < md1.NumObjects)
            {
                var objTri = md1.Objects[objIndex + 0];
                var objQuad = md1.Objects[objIndex + 1];
                var triangles = md1.GetTriangles(objTri);
                var triangleTextures = md1.GetTriangleTextures(objTri);
                var quads = md1.GetQuads(objQuad);
                var quadTextures = md1.GetQuadTextures(objQuad);

                var priIndex = listPrimitives.SelectedIndex;
                if (priIndex >= 0 && priIndex < triangles.Length)
                {
                    var tri = triangles[priIndex];
                    var triTex = triangleTextures[priIndex];
                    var positionData = md1.GetPositionData(objTri);
                    var v0 = positionData[tri.v0];
                    var v1 = positionData[tri.v1];
                    var v2 = positionData[tri.v2];
                    // textPrimitive.Text = string.Join("\n", new[] {
                    //     $"v0 = ({v0.x}, {v0.y}, {v0.z})",
                    //     $"v1 = ({v1.x}, {v1.y}, {v1.z})",
                    //     $"v2 = ({v2.x}, {v2.y}, {v2.z})"
                    // });
                    // textPrimitive.Text = string.Join("\n", new[] {
                    //     "u0 = " + triTex.u0,
                    //     "v0 = " + triTex.v0,
                    //     "clutId = " + triTex.clutId,
                    //     "u1 = " + triTex.u1,
                    //     "v1 = " + triTex.v1,
                    //     "page = " + triTex.page,
                    //     "u2 = " + triTex.u2,
                    //     "v2 = " + triTex.v2,
                    //     "zero = " + triTex.zero
                    // });
                }
                else if (priIndex >= triangles.Length && priIndex < triangles.Length + quads.Length)
                {
                    priIndex -= triangles.Length;
                    var quad = quads[priIndex];
                    // textPrimitive.Text = string.Join("\n", new[] {
                    //     quad.n0,
                    //     quad.v0,
                    //     quad.n1,
                    //     quad.v1,
                    //     quad.n2,
                    //     quad.v2,
                    //     quad.n3,
                    //     quad.v3
                    // });
                }
            }
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
                _mesh = targetBuilder.ToMd1();
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
    }
}
