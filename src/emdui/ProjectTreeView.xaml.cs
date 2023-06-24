﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using emdui.Extensions;
using IntelOrca.Biohazard;

namespace emdui
{
    /// <summary>
    /// Interaction logic for ProjectTreeView.xaml
    /// </summary>
    public partial class ProjectTreeView : UserControl
    {
        private Project _project;

        public MainWindow MainWindow { get; set; }

        public ProjectTreeView()
        {
            InitializeComponent();
        }

        private void Refresh()
        {
            treeView.ItemsSource = null;
            if (_project == null)
                return;

            var items = new List<ProjectTreeViewItem>();
            foreach (var projectFile in _project.Files)
            {
                var tvi = Create(projectFile);
                if (tvi == null)
                    throw new Exception("Invalid project item");
                items.Add(tvi);
            }
            treeView.ItemsSource = items;
        }

        private static ProjectTreeViewItem Create(ProjectFile projectFile)
        {
            if (projectFile.Content is PldFile)
            {
                return new PldTreeViewItem(projectFile);
            }
            else if (projectFile.Content is PlwFile)
            {
                return new PlwTreeViewItem(projectFile);
            }
            else if (projectFile.Content is EmdFile)
            {
                return new EmdTreeViewItem(projectFile);
            }
            else if (projectFile.Content is TimFile tim)
            {
                return new TimTreeViewItem(projectFile, tim);
            }
            return null;
        }

        private static TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }

        private void treeView_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);
            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                e.Handled = true;
            }
        }

        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (treeView.SelectedItem is ProjectTreeViewItem tvi)
            {
                tvi.Select();
                treeView.ContextMenu = tvi.GetContextMenu();
            }
        }

        private void treeView_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (treeView.SelectedItem is ProjectTreeViewItem tvi)
            {
                tvi.ExecuteDefaultAction();
                e.Handled = true;
            }
        }

        public Project Project
        {
            get => _project;
            set
            {
                if (_project != value)
                {
                    _project = value;
                    Refresh();
                }
            }
        }
    }

    public abstract class ProjectTreeViewItem : INotifyPropertyChanged
    {
        private readonly List<Tuple<string, Action>> _menuItems = new List<Tuple<string, Action>>();

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual ImageSource Image => (ImageSource)App.Current.Resources["IconPLD"];
        public abstract string Header { get; }
        public virtual ObservableCollection<ProjectTreeViewItem> Items { get; } = new ObservableCollection<ProjectTreeViewItem>();
        public ProjectFile ProjectFile { get; }
        public ModelFile Model => ProjectFile.Content as ModelFile;

        public ProjectTreeViewItem(ProjectFile projectFile)
        {
            ProjectFile = projectFile;
        }

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void AddMenuItem(string header, Action callback)
        {
            _menuItems.Add(new Tuple<string, Action>(header, callback));
        }

        protected void AddSeperator()
        {
            _menuItems.Add(null);
        }

        public void ExecuteDefaultAction()
        {
            OnDefaultAction();
        }

        public void Select()
        {
            OnSelect();
        }

        public virtual void OnDefaultAction()
        {
        }

        public virtual void OnSelect()
        {
        }

        public ContextMenu GetContextMenu()
        {
            if (_menuItems.Count == 0)
                return null;

            var contextMenu = new ContextMenu();
            foreach (var menuItem in _menuItems)
            {
                if (menuItem == null)
                {
                    contextMenu.Items.Add(new Separator());
                }
                else
                {
                    var item = new MenuItem()
                    {
                        Header = menuItem.Item1
                    };
                    item.Click += (s, e) => menuItem.Item2();
                    contextMenu.Items.Add(item);
                }
            }
            return contextMenu;
        }
    }

    public class ModelFileTreeViewItem : ProjectTreeViewItem
    {
        public override string Header => ProjectFile.Filename;

        public ModelFileTreeViewItem(ProjectFile projectFile)
            : base(projectFile)
        {
            var model = Model;
            var isFirstEmr = true;
            for (var i = 0; i < model.NumChunks; i++)
            {
                ProjectTreeViewItem tvi;
                switch (model.GetChunk<object>(i))
                {
                    case Edd edd:
                        tvi = new EddTreeViewItem(projectFile, i, edd);
                        break;
                    case Emr emr:
                        tvi = new EmrTreeViewItem(projectFile, i, emr, isFirstEmr);
                        isFirstEmr = false;
                        break;
                    case IModelMesh mesh:
                        tvi = new MeshTreeViewItem(projectFile, i, mesh);
                        break;
                    case TimFile tim:
                        tvi = new TimTreeViewItem(projectFile, tim);
                        break;
                    default:
                        tvi = new UnknownChunkTreeViewItem(projectFile, i);
                        break;
                }
                Items.Add(tvi);
            }
        }
    }

    public class PldTreeViewItem : ModelFileTreeViewItem
    {
        public override string Header => ProjectFile.Filename;
        public PldFile Pld { get; }

        public PldTreeViewItem(ProjectFile projectFile)
            : base(projectFile)
        {
            Pld = (PldFile)projectFile.Content;
        }
    }

    public class EmdTreeViewItem : ModelFileTreeViewItem
    {
        public override string Header => ProjectFile.Filename;
        public EmdFile Emd { get; }

        public EmdTreeViewItem(ProjectFile projectFile)
            : base(projectFile)
        {
            Emd = (EmdFile)projectFile.Content;
        }
    }

    public class PlwTreeViewItem : ModelFileTreeViewItem
    {
        public override string Header => ProjectFile.Filename;
        public PlwFile Plw { get; }

        public PlwTreeViewItem(ProjectFile projectFile)
            : base(projectFile)
        {
            Plw = (PlwFile)projectFile.Content;
        }
    }

    public abstract class ChunkTreeViewItem : ProjectTreeViewItem
    {
        public int ChunkIndex { get; }

        public ChunkTreeViewItem(ProjectFile projectFile, int chunkIndex)
            : base(projectFile)
        {
            ChunkIndex = chunkIndex;
        }
    }

    public class UnknownChunkTreeViewItem : ChunkTreeViewItem
    {
        public override string Header => "UNKNOWN";

        public UnknownChunkTreeViewItem(ProjectFile projectFile, int chunkIndex)
            : base(projectFile, chunkIndex)
        {
        }
    }

    public class EddTreeViewItem : ChunkTreeViewItem
    {
        public override ImageSource Image => (ImageSource)Application.Current.Resources["IconEDD"];
        public override string Header => "EDD";
        public Edd Edd { get; }
        public int Index { get; }

        public EddTreeViewItem(ProjectFile projectFile, int chunkIndex, Edd edd)
            : base(projectFile, chunkIndex)
        {
            Edd = edd;
            Index = chunkIndex;

            var numAnimations = edd.AnimationCount;
            for (var i = 0; i < numAnimations; i++)
            {
                Items.Add(new AnimationTreeViewItem(ProjectFile, chunkIndex, edd, i));
            }

            AddMenuItem("Import...", Import);
            AddMenuItem("Export...", Export);
        }

        private void Import()
        {
            CommonFileDialog
                .Open()
                .AddExtension("*.edd")
                .Show(path =>
                {
                    Model.SetEdd(0, new Edd(File.ReadAllBytes(path)));
                });
        }

        private void Export()
        {
            var dialog = CommonFileDialog.Save();
            if (ProjectFile != null)
            {
                dialog.WithDefaultFileName(Path.ChangeExtension(ProjectFile.Filename, ".EDD"));
            }
            dialog
                .AddExtension("*.edd")
                .Show(path => File.WriteAllBytes(path, Edd.Data.ToArray()));
        }
    }

    public class AnimationTreeViewItem : ChunkTreeViewItem
    {
        public override ImageSource Image => (ImageSource)Application.Current.Resources["IconAnimation"];
        public override string Header => $"Animation {Index}";
        public Edd Edd { get; }
        public int Index { get; }

        public AnimationTreeViewItem(ProjectFile projectFile, int chunkIndex, Edd edd, int index)
            : base(projectFile, chunkIndex)
        {
            Edd = edd;
            Index = index;
        }

        public override void OnDefaultAction()
        {
            if (Model.Version == BioVersion.Biohazard3)
                return;

            var mainWindow = MainWindow.Instance;
            var project = mainWindow.Project;
            var emr = Model.GetEmr(ChunkIndex);
            if (ProjectFile.Content is PldFile pldFile)
            {
                mainWindow.LoadMesh(pldFile.GetMesh(0));
                mainWindow.LoadAnimation(emr, Edd, Index);
            }
            else if (ProjectFile.Content is PlwFile plwFile)
            {
                if (project.MainModel is PldFile parentPldFile)
                {
                    var texture = project.MainTexture.WithWeaponTexture(plwFile.Tim);
                    var mesh = parentPldFile.GetMesh(0);
                    if (mesh is Md1 md1)
                    {
                        var targetBuilder = md1.ToBuilder();
                        var sourceBuilder = plwFile.Md1.ToBuilder();
                        targetBuilder.Parts[11] = sourceBuilder.Parts[0];
                        mesh = targetBuilder.ToMd1();
                    }
                    else if (mesh is Md2 md2)
                    {
                        // TODO
                    }

                    mainWindow.LoadMesh(mesh, texture);
                    mainWindow.LoadAnimation(emr, Edd, Index);
                }
            }
            else if (ProjectFile.Content is EmdFile emdFile)
            {
                mainWindow.LoadMesh(emdFile.GetMesh(0));
                mainWindow.LoadAnimation(emr, Edd, Index);
            }
        }
    }

    public class EmrTreeViewItem : ChunkTreeViewItem
    {
        public override ImageSource Image => (ImageSource)Application.Current.Resources["IconEMR"];
        public override string Header => "EMR";
        public Emr Emr { get; }

        public EmrTreeViewItem(ProjectFile projectFile, int chunkIndex, Emr emr, bool isFirstEmr)
            : base(projectFile, chunkIndex)
        {
            Emr = emr;
            if (isFirstEmr && emr.NumParts > 0 && !(Model is PlwFile))
            {
                Items.Add(new BoneTreeViewItem(ProjectFile, chunkIndex, emr, 0));
            }

            AddMenuItem("Import...", Import);
            AddMenuItem("Export...", Export);
        }

        public override void OnSelect()
        {
            MainWindow.Instance.SelectPart(-1);
        }

        public override void OnDefaultAction()
        {
            if (ProjectFile.Content is PldFile pldFile)
            {
                MainWindow.Instance.LoadMesh(pldFile.GetMesh(0));
            }
        }

        private void Import()
        {
            CommonFileDialog
                .Open()
                .AddExtension("*.emr")
                .Show(path =>
                {
                    Model.SetEmr(0, new Emr(File.ReadAllBytes(path)));
                });
        }

        private void Export()
        {
            var dialog = CommonFileDialog.Save();
            if (ProjectFile != null)
            {
                dialog.WithDefaultFileName(Path.ChangeExtension(ProjectFile.Filename, ".EMR"));
            }
            dialog
                .AddExtension("*.emr")
                .Show(path => File.WriteAllBytes(path, Emr.Data.ToArray()));
        }
    }

    public class BoneTreeViewItem : ChunkTreeViewItem
    {
        public override ImageSource Image => (ImageSource)Application.Current.Resources["IconArmature"];
        public Emr Emr { get; }
        public int EmrIndex { get; }
        public int PartIndex { get; }

        public BoneTreeViewItem(ProjectFile projectFile, int chunkIndex, Emr emr, int partIndex)
            : base(projectFile, chunkIndex)
        {
            Emr = emr;
            PartIndex = partIndex;

            var children = Emr.GetArmatureParts(partIndex);
            foreach (var child in children)
            {
                Items.Add(new BoneTreeViewItem(ProjectFile, chunkIndex, emr, child));
            }
        }

        public override void OnSelect()
        {
            MainWindow.Instance.SelectPart(PartIndex);
        }

        public override void OnDefaultAction()
        {
            if (ProjectFile.Content is PldFile pldFile)
            {
                MainWindow.Instance.LoadMesh(pldFile.GetMesh(0));
                MainWindow.Instance.SelectPart(PartIndex);
            }
        }

        public override string Header
        {
            get
            {
                var partIndex = PartIndex;
                if (Model.Version == BioVersion.Biohazard2)
                {
                    if (g_partNamesRe2.Length > partIndex)
                        return g_partNamesRe2[partIndex];
                }
                else
                {
                    if (g_partNamesRe3.Length > partIndex)
                        return g_partNamesRe3[partIndex];
                }
                return $"Part {partIndex}";
            }
        }

        private string[] g_partNamesRe2 = new string[]
        {
            "chest", "waist",
            "thigh (right)", "calf (right)", "foot (right)",
            "thigh (left)", "calf (left)", "foot (left)",
            "head",
            "upper arm (right)", "forearm (right)", "hand (right)",
            "upper arm (left)", "forearm (left)", "hand (left)",
            "ponytail (A)", "ponytail (B)", "ponytail (C)", "ponytail (D)"
        };

        private string[] g_partNamesRe3 = new string[]
        {
            "chest", "head",
            "upper arm (right)", "forearm (right)", "hand (right)",
            "upper arm (left)", "forearm (left)", "hand (left)",
            "waist",
            "thigh (right)", "calf (right)", "foot (right)",
            "thigh (left)", "calf (left)", "foot (left)",
            "hand with gun"
        };
    }

    public class MeshTreeViewItem : ChunkTreeViewItem
    {
        public override ImageSource Image => (ImageSource)Application.Current.Resources["IconMD1"];
        public override string Header => Mesh.Version == BioVersion.Biohazard2 ? "MD1" : "MD2";

        public IModelMesh Mesh
        {
            get => Model.GetChunk<IModelMesh>(ChunkIndex);
            set => Model.SetChunk(ChunkIndex, value);
        }

        public MeshTreeViewItem(ProjectFile projectFile, int chunkIndex, IModelMesh mesh)
            : base(projectFile, chunkIndex)
        {
            CreateChildren();
            AddMenuItem("Import...", Import);
            AddMenuItem("Export...", Export);
            AddSeperator();
            AddMenuItem("Open in Blender...", OpenInBlender);
            AddSeperator();
            AddMenuItem("Add part", AddPart);
            if (Model is EmdFile && mesh.Version == BioVersion.Biohazard3)
            {
                AddMenuItem("Copy hand to gun hand", AutoHandWithGun);
            }
        }

        private string DefaultExtension => Mesh.Version == BioVersion.Biohazard2 ? ".MD1" : ".MD2";
        private string ExtensionPattern => Mesh.Version == BioVersion.Biohazard2 ? "*.md1" : "*.md2";

        private void CreateChildren()
        {
            Items.Clear();
            for (var i = 0; i < Mesh.NumParts; i++)
            {
                Items.Add(new MeshPartTreeViewItem(ProjectFile, ChunkIndex, i));
            }
        }

        public override void OnSelect()
        {
            MainWindow.Instance.SelectPart(-1);
        }

        public override void OnDefaultAction()
        {
            var mainWindow = MainWindow.Instance;
            var texture = mainWindow.Project.MainTexture;
            if (Model is PlwFile plwFile)
            {
                texture = texture.WithWeaponTexture(plwFile.Tim);
                mainWindow.LoadMeshWithoutArmature(Mesh, texture);
            }
            else
            {
                mainWindow.LoadMesh(Mesh);
            }
        }

        private void Import()
        {
            CommonFileDialog
                .Open()
                .AddExtension(ExtensionPattern)
                .AddExtension("*.emd")
                .AddExtension("*.pld")
                .AddExtension("*.obj")
                .Show(path =>
                {
                    if (path.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
                    {
                        ImportFromObj(path);
                    }
                    else if (path.EndsWith(".emd", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".pld", StringComparison.OrdinalIgnoreCase))
                    {
                        ImportFromModel(path);
                    }
                    else
                    {
                        var mesh = Model.Version == BioVersion.Biohazard2 ?
                            (IModelMesh)new Md1(File.ReadAllBytes(path)) :
                            (IModelMesh)new Md2(File.ReadAllBytes(path));
                        Mesh = mesh;
                    }
                    CreateChildren();
                    MainWindow.Instance.LoadMesh(Model.GetMesh(0));
                });
        }

        private void ImportFromObj(string path)
        {
            var project = MainWindow.Instance.Project;
            var tim = project.MainTexture;
            var emr = Model.GetEmr(0);
            var numPages = tim.Width / 128;
            var objImporter = new ObjImporter();
            var mesh = project.Version == BioVersion.Biohazard2 ?
                (IModelMesh)objImporter.ImportMd1(path, numPages, emr.GetFinalPosition) :
                (IModelMesh)objImporter.ImportMd2(path, numPages, emr.GetFinalPosition);
            Mesh = mesh;
        }

        private void ImportFromModel(string path)
        {
            var project = MainWindow.Instance.Project;
            var modelFile = ModelFile.FromFile(path);
            if (modelFile.Version == BioVersion.Biohazard2)
            {
                if (Model.Version == BioVersion.Biohazard2)
                {
                    Mesh = modelFile.GetMesh(0);
                    Model.SetEmr(0, modelFile.GetEmr(0));
                }
                else
                {
                    Mesh = ((Md1)modelFile.GetMesh(0)).ToMd2();

                    var map2to3 = new[]
                    {
                        0, 8, 9, 10, 11, 12, 13, 14, 1, 2, 3, 4, 5, 6, 7
                    };
                    var emr = modelFile.GetEmr(0);
                    var emrBuilder = Model.GetEmr(0).ToBuilder();
                    for (var i = 0; i < map2to3.Length; i++)
                    {
                        var srcPartIndex = i;
                        var dstPartIndex = map2to3[i];
                        var src = emr.GetRelativePosition(srcPartIndex);
                        emrBuilder.RelativePositions[dstPartIndex] = src;
                    }
                    Model.SetEmr(0, emrBuilder.ToEmr());
                }
            }
            else
            {
                if (Model.Version == BioVersion.Biohazard2)
                {
                    Mesh = ((Md2)modelFile.GetMesh(0)).ToMd1();
                }
                else
                {
                    Mesh = modelFile.GetMesh(0);

                    var emr = modelFile.GetEmr(0);
                    var emrBuilder = Model.GetEmr(0).ToBuilder();
                    for (var i = 0; i < 15; i++)
                    {
                        emrBuilder.RelativePositions[i] = emr.GetRelativePosition(i);
                    }
                    Model.SetEmr(0, emrBuilder.ToEmr());
                }
            }

            if (modelFile is PldFile pldFile)
            {
                project.MainTexture = pldFile.Tim;
            }
            else
            {
                var timPath = Path.ChangeExtension(path, ".tim");
                if (File.Exists(timPath))
                {
                    project.MainTexture = new TimFile(timPath);
                }
            }
        }

        private void Export()
        {
            var dialog = CommonFileDialog.Save();
            if (ProjectFile != null)
            {
                dialog.WithDefaultFileName(Path.ChangeExtension(ProjectFile.Filename, DefaultExtension));
            }
            dialog
                .AddExtension(ExtensionPattern)
                .AddExtension("*.obj")
                .Show(path =>
                {
                    if (path.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
                    {
                        ExportToObj(path);
                    }
                    else
                    {
                        File.WriteAllBytes(path, Mesh.Data.ToArray());
                    }
                });
        }

        private void ExportToObj(string path)
        {
            var project = MainWindow.Instance.Project;
            var tim = GetTimFile();
            var emr = Model.GetEmr(0);

            var numPages = tim.Width / 128;
            var objExporter = new ObjExporter();
            objExporter.Export(Mesh, path, numPages, emr.GetFinalPosition);

            var texturePath = Path.ChangeExtension(path, ".png");
            tim.ToBitmap().Save(texturePath);
        }

        private TimFile GetTimFile()
        {
            var project = MainWindow.Instance.Project;
            var mainTexture = project.MainTexture;
            if (Model is PlwFile plw)
            {
                return mainTexture.WithWeaponTexture(plw.Tim);
            }
            else
            {
                return mainTexture;
            }
        }

        private void OpenInBlender()
        {
            using (var blenderSupport = new BlenderSupport())
            {
                ExportToObj(blenderSupport.ImportedObjectPath);
                if (blenderSupport.EditInBlender())
                {
                    ImportFromObj(blenderSupport.ExportedObjectPath);
                    CreateChildren();
                    MainWindow.Instance.LoadMesh(Model.GetMesh(0), GetTimFile());
                }
            }
        }

        private void AddPart()
        {
            if (Mesh is Md1 md1)
            {
                var part = new Md1Builder.Part();
                part.Positions.Add(new Md1.Vector());
                part.Normals.Add(new Md1.Vector());
                part.Triangles.Add(new Md1.Triangle());
                part.TriangleTextures.Add(new Md1.TriangleTexture());

                var md1Builder = md1.ToBuilder();
                md1Builder.Parts.Add(part);
                Mesh = md1Builder.ToMd1();
            }
            else if (Mesh is Md2 md2)
            {
                var md2Builder = md2.ToBuilder();
                md2Builder.Parts.Add(new Md2Builder.Part());
                Mesh = md2Builder.ToMd2();
            }
            CreateChildren();
        }

        private void AutoHandWithGun()
        {
            var builder = Model.Md2.ToBuilder();
            if (builder.Parts.Count < 16)
            {
                var part = new Md2Builder.Part();
                part.Positions.Add(new Md2.Vector());
                part.Normals.Add(new Md2.Vector());
                part.Triangles.Add(new Md2.Triangle());
                builder.Parts.Add(part);
            }
            builder.Parts[15] = builder.Parts[4];
            Mesh = builder.ToMd2();
            CreateChildren();
        }
    }

    public class MeshPartTreeViewItem : ChunkTreeViewItem
    {
        public override ImageSource Image => (ImageSource)Application.Current.Resources["IconPart"];
        public override string Header => $"Part {PartIndex}";
        public int PartIndex { get; }

        private string DefaultExtension => Mesh.Version == BioVersion.Biohazard2 ? ".MD1" : ".MD2";
        private string ExtensionPattern => Mesh.Version == BioVersion.Biohazard2 ? "*.md1" : "*.md2";

        public IModelMesh Mesh
        {
            get => Model.GetChunk<IModelMesh>(ChunkIndex);
            set => Model.SetChunk<IModelMesh>(ChunkIndex, value);
        }

        public MeshPartTreeViewItem(ProjectFile projectFile, int chunkIndex, int partIndex)
            : base(projectFile, chunkIndex)
        {
            PartIndex = partIndex;

            AddMenuItem("Import...", Import);
            AddMenuItem("Export...", Export);
            AddSeperator();
            AddMenuItem("Open in Blender...", OpenInBlender);
            AddSeperator();
            AddMenuItem("Clear", Clear);
        }

        public override void OnSelect()
        {
            MainWindow.Instance.SelectPart(PartIndex);
        }

        public override void OnDefaultAction()
        {
            var mainWindow = MainWindow.Instance;
            var project = mainWindow.Project;
            var projectFile = ProjectFile;
            if (projectFile.Content is PldFile pldFile)
            {
                if (project.Version == BioVersion.Biohazard2)
                {
                    var builder = pldFile.Md1.ToBuilder();
                    var partMd1 = builder.Parts[PartIndex];
                    builder.Parts.Clear();
                    builder.Parts.Add(partMd1);
                    var singleMd1 = builder.ToMd1();
                    mainWindow.LoadMeshPart(singleMd1, PartIndex);
                }
                else
                {
                    var builder = pldFile.Md2.ToBuilder();
                    var partMd2 = builder.Parts[PartIndex];
                    builder.Parts.Clear();
                    builder.Parts.Add(partMd2);
                    var singleMd2 = builder.ToMd2();
                    mainWindow.LoadMeshPart(singleMd2, PartIndex);
                }
            }
            else if (projectFile.Content is PlwFile plwFile)
            {
                if (project.MainModel is PldFile parentPldFile)
                {
                    var tim = parentPldFile.Tim;
                    var plwTim = plwFile.Tim;
                    for (var y = 0; y < 32; y++)
                    {
                        for (var x = 0; x < 56; x++)
                        {
                            var p = plwTim.GetPixel(x, y);
                            tim.SetPixel(200 + x, 224 + y, 1, p);
                        }
                    }

                    var builder = plwFile.Md1.ToBuilder();
                    var partMd1 = builder.Parts[PartIndex];
                    builder.Parts.Clear();
                    builder.Parts.Add(partMd1);
                    var singleMd1 = builder.ToMd1();
                    mainWindow.LoadMeshPart(singleMd1, PartIndex, project.MainTexture.WithWeaponTexture(plwFile.Tim));
                }
            }
            else if (projectFile.Content is EmdFile emdFile)
            {
                if (project.Version == BioVersion.Biohazard2)
                {
                    var builder = emdFile.Md1.ToBuilder();
                    var partMd1 = builder.Parts[PartIndex];
                    builder.Parts.Clear();
                    builder.Parts.Add(partMd1);
                    var singleMd1 = builder.ToMd1();
                    mainWindow.LoadMeshPart(singleMd1, PartIndex);
                }
                else
                {
                    var builder = emdFile.Md2.ToBuilder();
                    var partMd2 = builder.Parts[PartIndex];
                    builder.Parts.Clear();
                    builder.Parts.Add(partMd2);
                    var singleMd2 = builder.ToMd2();
                    mainWindow.LoadMeshPart(singleMd2, PartIndex);
                }
            }
        }

        private void Import()
        {
            CommonFileDialog
                .Open()
                .AddExtension(ExtensionPattern)
                .AddExtension("*.obj")
                .Show(path =>
                {
                    if (path.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
                    {
                        ImportFromObj(path);
                    }
                    else
                    {
                        var data = File.ReadAllBytes(path);
                        var mesh = Model.Version == BioVersion.Biohazard2 ?
                            (IModelMesh)new Md1(data) :
                            (IModelMesh)new Md2(data);
                        ImportMesh(mesh);
                    }
                });
        }

        private void Export()
        {
            var dialog = CommonFileDialog.Save();
            if (ProjectFile != null)
            {
                dialog.WithDefaultFileName(Path.ChangeExtension(ProjectFile.Filename, DefaultExtension));
            }
            dialog
                .AddExtension(ExtensionPattern)
                .AddExtension("*.obj")
                .Show(path =>
                {
                    if (path.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
                    {
                        ExportToObj(path);
                    }
                    else
                    {
                        File.WriteAllBytes(path, ExportMesh().Data.ToArray());
                    }
                });
        }

        private void ImportMesh(IModelMesh mesh)
        {
            if (mesh is Md1 md1)
            {
                var srcBuilder = md1.ToBuilder();
                if (srcBuilder.Parts.Count > 0)
                {
                    var builder = ((Md1)Mesh).ToBuilder();
                    builder.Parts[PartIndex] = srcBuilder.Parts[0];
                    Mesh = builder.ToMd1();
                }
            }
            else if (mesh is Md2 md2)
            {
                var srcBuilder = md2.ToBuilder();
                if (srcBuilder.Parts.Count > 0)
                {
                    var builder = ((Md2)Mesh).ToBuilder();
                    builder.Parts[PartIndex] = srcBuilder.Parts[0];
                    Mesh = builder.ToMd2();
                }
            }

            RefreshMesh();
        }

        private void RefreshMesh()
        {
            var mainWindow = MainWindow.Instance;
            var texture = mainWindow.Project.MainTexture;
            if (Model is PlwFile plwFile)
            {
                texture = texture.WithWeaponTexture(plwFile.Tim);
                mainWindow.LoadMeshWithoutArmature(Mesh, texture);
            }
            else
            {
                mainWindow.LoadMesh(Mesh);
            }
        }

        private IModelMesh ExportMesh()
        {
            if (Mesh is Md1 md1)
            {
                var builder = md1.ToBuilder();
                var interestedPart = builder.Parts[PartIndex];
                builder.Parts.Clear();
                builder.Parts.Add(interestedPart);
                return builder.ToMd1();
            }
            else if (Mesh is Md2 md2)
            {
                var builder = md2.ToBuilder();
                var interestedPart = builder.Parts[PartIndex];
                builder.Parts.Clear();
                builder.Parts.Add(interestedPart);
                return builder.ToMd2();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private void ImportFromObj(string path)
        {
            var project = MainWindow.Instance.Project;
            var tim = project.MainTexture;
            var numPages = tim.Width / 128;
            var objImporter = new ObjImporter();

            var mesh = project.Version == BioVersion.Biohazard2 ?
                (IModelMesh)objImporter.ImportMd1(path, numPages) :
                (IModelMesh)objImporter.ImportMd2(path, numPages);
            ImportMesh(mesh);
        }

        private void ExportToObj(string path)
        {
            var project = MainWindow.Instance.Project;
            var tim = GetTimFile();
            var mesh = ExportMesh();

            var numPages = tim.Width / 128;
            var objExporter = new ObjExporter();
            objExporter.Export(mesh, path, numPages);

            var texturePath = Path.ChangeExtension(path, ".png");
            tim.ToBitmap().Save(texturePath);
        }

        private TimFile GetTimFile()
        {
            var project = MainWindow.Instance.Project;
            var mainTexture = project.MainTexture;
            if (Model is PlwFile plw)
            {
                return mainTexture.WithWeaponTexture(plw.Tim);
            }
            else
            {
                return mainTexture;
            }
        }

        private void OpenInBlender()
        {
            using (var blenderSupport = new BlenderSupport())
            {
                ExportToObj(blenderSupport.ImportedObjectPath);
                if (blenderSupport.EditInBlender())
                {
                    ImportFromObj(blenderSupport.ExportedObjectPath);
                }
            }
        }

        private void Clear()
        {
            IModelMesh mesh;
            if (Model.Version == BioVersion.Biohazard2)
            {
                var part = new Md1Builder.Part();
                part.Positions.Add(new Md1.Vector());
                part.Normals.Add(new Md1.Vector());
                part.Triangles.Add(new Md1.Triangle());
                part.TriangleTextures.Add(new Md1.TriangleTexture());

                var md1Builder = new Md1Builder();
                md1Builder.Parts.Add(part);
                mesh = md1Builder.ToMd1();
            }
            else
            {
                var md2Builder = new Md2Builder();
                md2Builder.Parts.Add(new Md2Builder.Part());
                mesh = md2Builder.ToMd2();
            }
            ImportMesh(mesh);
        }
    }

    public class TimTreeViewItem : ProjectTreeViewItem
    {
        public override ImageSource Image => (ImageSource)Application.Current.Resources["IconTIM"];
        public override string Header => ProjectFile.Kind == ProjectFileKind.Tim ? ProjectFile.Filename : "TIM";
        public TimFile Tim { get; }

        public TimTreeViewItem(ProjectFile projectFile, TimFile tim)
            : base(projectFile)
        {
            Tim = tim;

            AddMenuItem("Import...", Import);
            AddMenuItem("Export...", Export);
        }

        private void Import()
        {
            CommonFileDialog
                .Open()
                .AddExtension("*.tim")
                .Show(path =>
                {
                    if (Model is PldFile pld)
                    {
                        pld.Tim = new TimFile(path);
                    }
                    else if (Model is PlwFile plw)
                    {
                        plw.Tim = new TimFile(path);
                    }
                    else if (ProjectFile.Kind == ProjectFileKind.Tim)
                    {
                        MainWindow.Instance.Project.MainTexture = new TimFile(path);
                    }
                    MainWindow.Instance.LoadMesh(Model.GetMesh(0));
                });
        }

        private void Export()
        {
            var dialog = CommonFileDialog.Save();
            if (ProjectFile != null)
            {
                dialog.WithDefaultFileName(Path.ChangeExtension(ProjectFile.Filename, ".TIM"));
            }
            dialog
                .AddExtension("*.png")
                .AddExtension("*.tim")
                .Show(path =>
                {
                    if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        Tim.ToBitmap().Save(path);
                    }
                    else
                    {
                        Tim.Save(path);
                    }
                });
        }
    }
}
