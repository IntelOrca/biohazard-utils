using System;
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
using IntelOrca.Biohazard.Extensions;
using IntelOrca.Biohazard.Model;

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

        public void Refresh()
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
            var treeViewItem = Find<TreeViewItem>(treeView);
            if (treeViewItem != null)
            {
                treeViewItem.IsExpanded = true;
            }
        }

        private static T Find<T>(DependencyObject parent)
        {
            if (parent is T value)
                return value;

            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = Find<T>(child);
                if (result != null)
                    return result;
            }

            return default(T);
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
                    case MorphData morph:
                        tvi = new MorphTreeViewItem(projectFile, i, morph);
                        break;
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
            var emrChunkIndex = GetEmrChunkIndex();
            var emr = Model.GetChunk<Emr>(emrChunkIndex);
            if (ProjectFile.Content is PldFile pldFile)
            {
                mainWindow.LoadMesh(pldFile.GetMesh(0));
                mainWindow.LoadAnimation(emr, Edd, Index);
            }
            else if (ProjectFile.Content is PlwFile plwFile)
            {
                var texture = project.MainTexture;
                if (plwFile.Version != BioVersion.Biohazard1)
                    texture = texture.WithWeaponTexture(plwFile.Tim);
                else
                    emr = project.MainModel.GetEmr(0);
                var mesh = project.MainModel.GetMesh(0);
                switch (Model.Version)
                {
                    case BioVersion.Biohazard1:
                    case BioVersion.Biohazard2:
                        mesh = mesh.ReplacePart(11, plwFile.GetMesh(0));
                        break;
                }

                mainWindow.LoadMesh(mesh, texture);
                mainWindow.LoadAnimation(emr, Edd, Index);
            }
            else if (ProjectFile.Content is EmdFile emdFile)
            {
                mainWindow.LoadMesh(emdFile.GetMesh(0));
                mainWindow.LoadAnimation(emr, Edd, Index);
            }
        }

        private int GetEmrChunkIndex()
        {
            if (Model.Version == BioVersion.Biohazard1)
                return ChunkIndex - 1;
            else
                return ChunkIndex + 1;
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
                    Model.SetEmr(0, new Emr(Model.Version, File.ReadAllBytes(path)));
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

        public override string Header => PartName.GetPartName(Model.Version, PartIndex);
    }

    public class MeshTreeViewItem : ChunkTreeViewItem
    {
        public override ImageSource Image => (ImageSource)Application.Current.Resources["IconMD1"];
        public override string Header
        {
            get
            {
                switch (Mesh.Version)
                {
                    case BioVersion.Biohazard1:
                        return "TMD";
                    case BioVersion.Biohazard2:
                        return "MD1";
                    case BioVersion.Biohazard3:
                        return "MD2";
                    default:
                        throw new NotSupportedException();
                };
            }
        }

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
            AddMenuItem("Import to page 3/4...", Import34);
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

        private void CreateChildren()
        {
            Items.Clear();
            for (var i = 0; i < Mesh.NumParts; i++)
            {
                Items.Add(new MeshPartTreeViewItem(this, i));
            }
        }

        public void RefreshAndCreateChildren()
        {
            CreateChildren();
            MainWindow.Instance.LoadMesh(Mesh);
        }

        public override void OnSelect()
        {
            MainWindow.Instance.SelectPart(-1);
        }

        public override void OnDefaultAction()
        {
            var mainWindow = MainWindow.Instance;
            var texture = mainWindow.Project.MainTexture;
            if (Model is PlwFile plwFile && plwFile.Version != BioVersion.Biohazard1)
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
                .AddExtension(Mesh.GetExtensionPattern())
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
                    RefreshAndCreateChildren();
                });
        }

        private void Import34()
        {
            CommonFileDialog
                .Open()
                .AddExtension(Mesh.GetExtensionPattern())
                .AddExtension("*.emd")
                .AddExtension("*.pld")
                .Show(path =>
                {
                    if (path.EndsWith(".emd", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".pld", StringComparison.OrdinalIgnoreCase))
                    {
                        ImportFromModel34(path);
                    }
                    RefreshAndCreateChildren();
                });
        }

        private void ImportFromObj(string path)
        {
            var project = MainWindow.Instance.Project;
            var tim = project.MainTexture;
            var numPages = tim.Width / 128;
            var objImporter = new ObjImporter();
            if (Model is PlwFile)
                Mesh = objImporter.Import(Mesh.Version, path, numPages);
            else
                Mesh = objImporter.Import(Mesh.Version, path, numPages, Model.GetEmr(0).GetFinalPosition);
        }

        private void ImportFromModel(string path)
        {
            var project = MainWindow.Instance.Project;
            var modelFile = ModelFile.FromFile(path);

            // Mesh
            var converter = new MeshConverter();
            Mesh = converter.ConvertMesh(modelFile.GetMesh(0), Mesh.Version);

            // Bone positions
            Model.SetEmr(0, converter.ConvertEmr(Model.GetEmr(0), modelFile.GetEmr(0)));

            // Texture
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

        private void ImportFromModel34(string path)
        {
            var project = MainWindow.Instance.Project;
            var modelFile = ModelFile.FromFile(path);

            // Mesh
            var converter = new MeshConverter();
            Mesh = converter
                .ConvertMesh(modelFile.GetMesh(0), Mesh.Version)
                .EditMeshTextures(pt =>
                {
                    pt.Page = (pt.Page + 2) % 4;
                });

            // Bone positions
            Model.SetEmr(0, converter.ConvertEmr(Model.GetEmr(0), modelFile.GetEmr(0)));

            // Texture
            TimFile srcTexture = null;
            if (modelFile is PldFile pldFile)
            {
                srcTexture = pldFile.Tim;
            }
            else if (modelFile is EmdFile emdFile && emdFile.Version == BioVersion.Biohazard1)
            {
                srcTexture = emdFile.GetTim(0);
            }
            else
            {
                var timPath = Path.ChangeExtension(path, ".tim");
                if (File.Exists(timPath))
                {
                    srcTexture = new TimFile(timPath);
                }
            }

            var texture = project.MainTexture;
            texture.ImportPage(2, srcTexture.ExportPage(0));
            texture.ImportPage(3, srcTexture.ExportPage(1));
            project.MainTexture = texture;
        }

        private void Export()
        {
            var dialog = CommonFileDialog.Save();
            if (ProjectFile != null)
            {
                dialog.WithDefaultFileName(Path.ChangeExtension(ProjectFile.Filename, Mesh.GetDefaultExtension()));
            }
            dialog
                .AddExtension(Mesh.GetExtensionPattern())
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

            var numPages = tim.Width / 128;
            var objExporter = new ObjExporter();
            if (Model is PlwFile)
                objExporter.Export(Mesh, path, numPages);
            else
                objExporter.Export(Mesh, path, numPages, Model.GetEmr(0).GetFinalPosition);

            var texturePath = Path.ChangeExtension(path, ".png");
            tim.ToBitmap().Save(texturePath);
        }

        private TimFile GetTimFile()
        {
            var project = MainWindow.Instance.Project;
            var mainTexture = project.MainTexture;
            if (Model is PlwFile plw)
            {
                if (plw.Version == BioVersion.Biohazard1)
                    return mainTexture;
                else
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
            Mesh = Mesh.AddPart(Mesh.CreateEmptyPart());
            CreateChildren();
        }

        private void AutoHandWithGun()
        {
            var builder = Model.Md2.ToBuilder();
            if (builder.Parts.Count < 16)
            {
                var part = new Md2.Builder.Part();
                part.Positions.Add(new Md2.Vector());
                part.Normals.Add(new Md2.Vector());
                part.Triangles.Add(new Md2.Triangle());
                builder.Parts.Add(part);
            }
            builder.Parts[15] = builder.Parts[4];
            Mesh = builder.ToMesh();
            CreateChildren();
        }
    }

    public class MeshPartTreeViewItem : ChunkTreeViewItem
    {
        public override ImageSource Image => (ImageSource)Application.Current.Resources["IconPart"];
        public override string Header => $"Part {PartIndex}";
        public MeshTreeViewItem Parent { get; }
        public int PartIndex { get; }

        public IModelMesh Mesh
        {
            get => Model.GetChunk<IModelMesh>(ChunkIndex);
            set => Model.SetChunk<IModelMesh>(ChunkIndex, value);
        }

        public MeshPartTreeViewItem(MeshTreeViewItem parent, int partIndex)
            : base(parent.ProjectFile, parent.ChunkIndex)
        {
            Parent = parent;
            PartIndex = partIndex;

            AddMenuItem("Import...", Import);
            AddMenuItem("Export...", Export);
            AddSeperator();
            AddMenuItem("Open in Blender...", OpenInBlender);
            AddSeperator();
            AddMenuItem("Clear", Clear);
            AddMenuItem("Delete", Delete);
            AddSeperator();
            AddMenuItem("Move all UV to page 0", () => MoveUVToPage(0));
            AddMenuItem("Move all UV to page 1", () => MoveUVToPage(1));
        }

        public override void OnSelect()
        {
            if (Model is PlwFile)
            {
                if (PartIndex == 0)
                    MainWindow.Instance.SelectPart(11);
                else
                    MainWindow.Instance.SelectPart(-1);
            }
            else
            {
                MainWindow.Instance.SelectPart(PartIndex);
            }
        }

        public override void OnDefaultAction()
        {
            var mainWindow = MainWindow.Instance;
            var project = mainWindow.Project;
            var projectFile = ProjectFile;
            TimFile texture = null;
            if (projectFile.Content is PlwFile plwFile &&
                project.MainModel is PldFile parentPldFile)
            {
                texture = project.MainTexture.WithWeaponTexture(plwFile.Tim);
            }
            var partMesh = Mesh.ExtractPart(PartIndex);
            mainWindow.LoadMeshPart(partMesh, PartIndex, texture);
        }

        private void Import()
        {
            CommonFileDialog
                .Open()
                .AddExtension(Mesh.GetExtensionPattern())
                .AddExtension("*.obj")
                .Show(path =>
                {
                    if (path.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
                    {
                        ImportFromObj(path);
                    }
                    else if (path.EndsWith(".tmd", StringComparison.OrdinalIgnoreCase))
                    {
                        ImportMesh(new Tmd(File.ReadAllBytes(path)));
                    }
                    else if (path.EndsWith(".md1", StringComparison.OrdinalIgnoreCase))
                    {
                        ImportMesh(new Tmd(File.ReadAllBytes(path)));
                    }
                    else if (path.EndsWith(".md2", StringComparison.OrdinalIgnoreCase))
                    {
                        ImportMesh(new Tmd(File.ReadAllBytes(path)));
                    }
                });
        }

        private void Export()
        {
            var dialog = CommonFileDialog.Save();
            if (ProjectFile != null)
            {
                dialog.WithDefaultFileName(Path.ChangeExtension(ProjectFile.Filename, $".{PartIndex:00}{Mesh.GetDefaultExtension()}"));
            }
            dialog
                .AddExtension(Mesh.GetExtensionPattern())
                .AddExtension("*.obj")
                .Show(path =>
                {
                    if (path.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
                    {
                        ExportToObj(path);
                    }
                    else
                    {
                        File.WriteAllBytes(path, Mesh.ExtractPart(PartIndex).Data.ToArray());
                    }
                });
        }

        private void ImportMesh(IModelMesh mesh)
        {
            Mesh = Mesh.ReplacePart(PartIndex, mesh);
            RefreshMesh();
        }

        private void RefreshMesh()
        {
            var mainWindow = MainWindow.Instance;
            var texture = mainWindow.Project.MainTexture;
            if (Model is PlwFile plwFile)
            {
                if (plwFile.Version == BioVersion.Biohazard1)
                {
                    mainWindow.LoadMeshWithoutArmature(Mesh);
                }
                else
                {
                    texture = texture.WithWeaponTexture(plwFile.Tim);
                    mainWindow.LoadMeshWithoutArmature(Mesh, texture);
                }
            }
            else
            {
                mainWindow.LoadMesh(Mesh);
            }
        }

        private void ImportFromObj(string path)
        {
            var project = MainWindow.Instance.Project;
            var tim = project.MainTexture;
            var numPages = tim.Width / 128;
            var objImporter = new ObjImporter();
            ImportMesh(objImporter.Import(Mesh.Version, path, numPages));
        }

        private void ExportToObj(string path)
        {
            var project = MainWindow.Instance.Project;
            var tim = GetTimFile();
            var mesh = Mesh.ExtractPart(PartIndex);

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
            if (Model is PlwFile plw && plw.Version != BioVersion.Biohazard1)
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
            ImportMesh(Mesh.CreateEmptyPart());
        }

        private void Delete()
        {
            Mesh = Mesh.RemovePart(PartIndex);
            Parent.RefreshAndCreateChildren();
        }

        private void MoveUVToPage(int page)
        {
            Mesh = Mesh.MoveUVToPage(PartIndex, page);
            RefreshMesh();
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

    public class MorphTreeViewItem : ChunkTreeViewItem
    {
        public override ImageSource Image => (ImageSource)Application.Current.Resources["IconPLD"];
        public override string Header => "DAT";
        public MorphData MorphData { get; }

        public MorphTreeViewItem(ProjectFile projectFile, int chunkIndex, MorphData morphData)
            : base(projectFile, chunkIndex)
        {
            MorphData = morphData;
            CreateChildren();
        }

        private void CreateChildren()
        {
            Items.Clear();
            for (var i = 0; i < MorphData.NumParts; i++)
            {
                Items.Add(new MorphGroupTreeViewItem(this, i));
            }
        }
    }

    public class MorphGroupTreeViewItem : ChunkTreeViewItem
    {
        public override ImageSource Image => (ImageSource)Application.Current.Resources["IconPLD"];
        public override string Header => $"Morph {GroupIndex}";
        public MorphTreeViewItem Parent { get; }
        public MorphData MorphData => Parent.MorphData;
        public int GroupIndex { get; }

        public MorphGroupTreeViewItem(MorphTreeViewItem parent, int groupIndex)
            : base(parent.ProjectFile, parent.ChunkIndex)
        {
            Parent = parent;
            GroupIndex = groupIndex;
        }
    }
}
