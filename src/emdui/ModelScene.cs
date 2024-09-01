using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using emdui.Extensions;
using IntelOrca.Biohazard;
using IntelOrca.Biohazard.Model;

namespace emdui
{
    public class ModelScene
    {
        private BitmapSource _texture;
        private Model3DGroup _root;
        private Model3DGroup[] _armature;
        private Model3DGroup[] _rig;
        private GeometryModel3D[] _model3d;
        private GeometryModel3D _highlightedModel;
        private int _highlightedPart = -1;

        private IModelMesh _mesh;
        private Emr _emr;
        private int _numParts;

        public void SetKeyframe(int keyframeIndex)
        {
            for (var i = 0; i < _armature.Length; i++)
            {
                if (_armature[i] is Model3DGroup armature)
                {
                    armature.Transform = GetTransformation(i, keyframeIndex);
                }
                if (_rig[i] is Model3DGroup rig)
                {
                    rig.Transform = GetTransformation(i, keyframeIndex);
                }
            }
        }

        public ModelVisual3D CreateVisual3d()
        {
            return new ModelVisual3D()
            {
                Content = _root
            };
        }

        public GeometryModel3D GetModel3d(int partIndex)
        {
            return partIndex < 0 || partIndex >= _model3d.Length ? null : _model3d[partIndex];
        }

        public void HighlightPart(int partIndex)
        {
            _highlightedPart = partIndex;

            if (_highlightedModel != null)
            {
                _highlightedModel.Material = CreateMaterial(false);
                _highlightedModel.BackMaterial = _highlightedModel.Material;
                _highlightedModel = null;
            }

            var model3d = GetModel3d(partIndex);
            if (model3d == null)
                return;

            model3d.Material = CreateMaterial(true);
            model3d.BackMaterial = model3d.Material;
            _highlightedModel = model3d;
        }

        public void GenerateFrom(IModelMesh mesh, Emr emr, TimFile timFile)
        {
            _emr = emr;
            _texture = timFile.ToBitmap();
            _mesh = mesh;
            _numParts = _mesh.NumParts;
            _model3d = new GeometryModel3D[_numParts];
            _armature = new Model3DGroup[_numParts];
            _rig = new Model3DGroup[_numParts];
            _root = CreateModel();
        }

        private Model3DGroup CreateModel()
        {
            var rootGroup = new Model3DGroup();

            var armatureParts = new int[0];

            if (_emr != null && _emr.NumParts != 0 && Settings.Default.ShowFloor)
            {
                rootGroup.Children.Add(CreateFloor());
            }
            if (_emr != null && _emr.NumParts != 0)
            {
                if (Settings.Default.ShowBones)
                {
                    var rig = CreateRigFromArmature(0);
                    if (rig != null)
                    {
                        rootGroup.Children.Add(rig);
                    }
                }
                else
                {
                    var main = CreateModelFromArmature(0);
                    if (main != null)
                    {
                        rootGroup.Children.Add(main);
                        armatureParts = GetAllArmatureParts(0);
                    }
                }
            }
            if (!Settings.Default.ShowBones)
            {
                for (var i = 0; i < _numParts; i++)
                {
                    if (!armatureParts.Contains(i))
                    {
                        var model = CreateModelFromPart(i);
                        rootGroup.Children.Add(model);
                    }
                }
            }

            return rootGroup;
        }

        private GeometryModel3D CreateFloor()
        {
            var size = 5000;

            var mesh = CreateCubeMesh(size, 256, size);
            var material = new DiffuseMaterial(Brushes.Gray);
            var floor = new GeometryModel3D()
            {
                Geometry = mesh,
                Material = material,
                Transform = new TranslateTransform3D(0, 128, 0)
            };
            return floor;
        }

        internal static MeshGeometry3D CreateCubeMesh(int width, int height, int depth)
        {
            var mesh = new MeshGeometry3D();
            mesh.Positions = new Point3DCollection();
            mesh.Normals = new Vector3DCollection();
            mesh.TriangleIndices = new Int32Collection();

            var p = new Point3D[]
            {
                new Point3D(-width / 2, -height / 2, -depth / 2), // 0
                new Point3D(-width / 2, +height / 2, -depth / 2), // 1
                new Point3D(+width / 2, +height / 2, -depth / 2), // 2
                new Point3D(+width / 2, -height / 2, -depth / 2), // 3
                new Point3D(-width / 2, -height / 2, +depth / 2), // 4
                new Point3D(-width / 2, +height / 2, +depth / 2), // 5
                new Point3D(+width / 2, +height / 2, +depth / 2), // 6
                new Point3D(+width / 2, -height / 2, +depth / 2)  // 7
            };

            AddQuad(mesh, p[0], p[1], p[2], p[3], new Vector3D(0, 0, -1));
            AddQuad(mesh, p[5], p[4], p[7], p[6], new Vector3D(0, 0, 1));
            AddQuad(mesh, p[1], p[5], p[6], p[2], new Vector3D(0, 1, 0));
            AddQuad(mesh, p[4], p[0], p[3], p[7], new Vector3D(0, -1, 0));
            AddQuad(mesh, p[4], p[5], p[1], p[0], new Vector3D(-1, 0, 0));
            AddQuad(mesh, p[3], p[2], p[6], p[7], new Vector3D(1, 0, 0));
            return mesh;
        }

        private static void AddQuad(MeshGeometry3D mesh, Point3D a, Point3D b, Point3D c, Point3D d, Vector3D normal)
        {
            mesh.Positions.Add(a);
            mesh.Positions.Add(b);
            mesh.Positions.Add(c);
            mesh.Positions.Add(a);
            mesh.Positions.Add(c);
            mesh.Positions.Add(d);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
        }

        private static MeshGeometry3D CreateBoneMesh(double length, double radius)
        {
            // Create the cylinder mesh
            MeshGeometry3D boneMesh = new MeshGeometry3D();

            // Calculate the dimensions of the cylinder
            double halfLength = length / 2;
            int numSegments = 32;

            // Generate the vertices and normals
            for (int i = 0; i <= numSegments; i++)
            {
                double theta = 2.0 * Math.PI * (double)i / numSegments;
                double x = radius * Math.Cos(theta);
                double y = radius * Math.Sin(theta);

                // Top cap vertices
                boneMesh.Positions.Add(new Point3D(x, y, halfLength));

                // Bottom cap vertices
                boneMesh.Positions.Add(new Point3D(x, y, -halfLength));

                // Top cap normals
                boneMesh.Normals.Add(new Vector3D(x, y, 0));

                // Bottom cap normals
                boneMesh.Normals.Add(new Vector3D(x, y, 0));
            }

            // Generate the triangle indices
            for (int i = 0; i < numSegments; i++)
            {
                int baseIndex = i * 2;

                // Top cap triangles
                boneMesh.TriangleIndices.Add(baseIndex);
                boneMesh.TriangleIndices.Add(baseIndex + 1);
                boneMesh.TriangleIndices.Add(baseIndex + 3);

                // Bottom cap triangles
                boneMesh.TriangleIndices.Add(baseIndex);
                boneMesh.TriangleIndices.Add(baseIndex + 3);
                boneMesh.TriangleIndices.Add(baseIndex + 2);
            }

            return boneMesh;
        }


        private int[] GetAllArmatureParts(int rootPartIndex)
        {
            var emr = _emr;
            var parts = new List<int>();
            var stack = new Stack<byte>();
            stack.Push((byte)rootPartIndex);
            while (stack.Count != 0)
            {
                var partIndex = stack.Pop();
                parts.Add(partIndex);

                var children = emr.GetArmatureParts(partIndex);
                foreach (var child in children)
                {
                    stack.Push(child);
                }
            }
            return parts.ToArray();
        }

        private Model3DGroup CreateModelFromArmature(int partIndex)
        {
            if (_armature.Length <= partIndex)
                return null;

            var armature = new Model3DGroup();
            var armatureMesh = CreateModelFromPart(partIndex);
            if (armatureMesh != null)
                armature.Children.Add(armatureMesh);

            // Children
            var subParts = _emr.GetArmatureParts(partIndex);
            foreach (var subPart in subParts)
            {
                var subPartMesh = CreateModelFromArmature(subPart);
                if (subPartMesh != null)
                {
                    armature.Children.Add(subPartMesh);
                }
            }

            armature.Transform = GetTransformation(partIndex, -1);
            _armature[partIndex] = armature;
            return armature;
        }

        private Model3DGroup CreateRigFromArmature(int partIndex, int parent = -1)
        {
            if (_rig.Length <= partIndex)
                return null;

            var parentModel = parent == -1 ? null : _rig[parent];

            var position = _emr.GetRelativePosition(partIndex);
            var length = Math.Sqrt((position.x * position.x) + (position.y * position.y) + (position.z * position.z));
            var midPoint = new Emr.Vector(
                (short)(position.x / 2.0),
                (short)(position.y / 2.0),
                (short)(position.z / 2.0)
            );

            var direction = new Vector3D(position.x, position.y, position.z);
            direction.Normalize();

            var rx = 0;
            var ry = -Math.Atan2(direction.X, direction.Y) * (180 / Math.PI);
            if (direction.Y < 0)
                ry = -ry;
            var rz = 90 + Math.Atan2(direction.Z, direction.Y) * (180 / Math.PI);

            var boneGroup = new Model3DGroup();
            _rig[partIndex] = boneGroup;
            var boneModel = new GeometryModel3D();
            boneModel.Geometry = CreateBoneMesh(length, 16);
            boneModel.Material = CreateBoneMaterial(false);
            boneModel.BackMaterial = boneModel.Material;

            var transformGroup = new Transform3DGroup();
            transformGroup.Children.Add(new RotateTransform3D(
                new AxisAngleRotation3D(new Vector3D(0, 0, 1), rx)));
            transformGroup.Children.Add(new RotateTransform3D(
                new AxisAngleRotation3D(new Vector3D(0, 1, 0), ry)));
            transformGroup.Children.Add(new RotateTransform3D(
                new AxisAngleRotation3D(new Vector3D(1, 0, 0), rz)));
            transformGroup.Children.Add(new TranslateTransform3D(midPoint.x, midPoint.y, midPoint.z));
            boneModel.Transform = transformGroup;
            if (parentModel != null)
                parentModel.Children.Add(boneModel);

            var cubeModel = new GeometryModel3D();
            cubeModel.Geometry = CreateCubeMesh(96, 94, 94);
            cubeModel.Material = CreateBoneMaterial(partIndex == 0);
            cubeModel.BackMaterial = boneModel.Material;
            boneGroup.Children.Add(cubeModel);

            // Children
            var subParts = _emr.GetArmatureParts(partIndex);
            foreach (var subPart in subParts)
            {
                var subPartMesh = CreateRigFromArmature(subPart, partIndex);
                if (subPartMesh != null)
                {
                    boneGroup.Children.Add(subPartMesh);
                }
            }

            boneGroup.Transform = GetTransformation(partIndex, -1);
            return boneGroup;
        }

        private Transform3D GetTransformation(int partIndex, int keyFrameIndex)
        {
            var emr = _emr;
            var relativePosition = emr.GetRelativePosition(partIndex);

            var transformGroup = new Transform3DGroup();
            if (keyFrameIndex >= 0 && keyFrameIndex < emr.KeyFrames.Length)
            {
                var keyFrame = emr.KeyFrames[keyFrameIndex];
                if (partIndex == 0)
                {
                    relativePosition = keyFrame.Offset;
                }

                var angle = keyFrame.GetAngle(partIndex);
                var rx = (angle.x / 4096.0) * 360;
                var ry = (angle.y / 4096.0) * 360;
                var rz = (angle.z / 4096.0) * 360;

                transformGroup.Children.Add(new RotateTransform3D(
                    new AxisAngleRotation3D(new Vector3D(0, 0, 1), rz)));
                transformGroup.Children.Add(new RotateTransform3D(
                    new AxisAngleRotation3D(new Vector3D(0, 1, 0), ry)));
                transformGroup.Children.Add(new RotateTransform3D(
                    new AxisAngleRotation3D(new Vector3D(1, 0, 0), rx)));
            }

            transformGroup.Children.Add(new TranslateTransform3D(relativePosition.x, relativePosition.y, relativePosition.z));
            return transformGroup;
        }

        private GeometryModel3D CreateModelFromPart(int partIndex)
        {
            var textureSize = new Size(_texture.PixelWidth, _texture.PixelHeight);
            var model = new GeometryModel3D();
            if (_mesh.NumParts <= partIndex)
                return null;

            model.Geometry = CreateMesh(_mesh, partIndex, textureSize);
            model.Material = CreateMaterial(false);
            model.BackMaterial = model.Material;

            _model3d[partIndex] = model;
            return model;
        }

        private Material CreateMaterial(bool highlighted)
        {
            var material = new DiffuseMaterial();
            material.Brush = new ImageBrush(_texture)
            {
                TileMode = TileMode.Tile,
                ViewportUnits = BrushMappingMode.Absolute
            };
            if (highlighted)
                material.AmbientColor = Colors.Blue;
            return material;
        }

        private Material CreateBoneMaterial(bool highlighted)
        {
            var material = new DiffuseMaterial();
            material.Brush = new SolidColorBrush(Colors.White);
            if (highlighted)
                material.AmbientColor = Colors.Blue;
            else
                material.AmbientColor = Colors.LightGray;
            return material;
        }

        private static MeshGeometry3D CreateMesh(IModelMesh mesh, int partIndex, Size textureSize)
        {
            var visitor = new MeshGeometry3DMeshVisitor(partIndex, textureSize);
            visitor.Accept(mesh);
            return visitor.Mesh;
        }

        private class MeshGeometry3DMeshVisitor : MeshVisitor
        {
            private readonly int _partIndex;
            private readonly Size _textureSize;
            private readonly List<Vector> _positions = new List<Vector>();
            private readonly List<Vector> _normals = new List<Vector>();
            private byte _page;

            public MeshGeometry3D Mesh { get; } = new MeshGeometry3D();

            public MeshGeometry3DMeshVisitor(int partIndex, Size textureSize)
            {
                _partIndex = partIndex;
                _textureSize = textureSize;
                Trianglulate = true;
            }

            public override bool VisitPart(int index)
            {
                _positions.Clear();
                _normals.Clear();
                return index == _partIndex;
            }

            public override void VisitPrimitive(int numPoints, byte page)
            {
                _page = page;
            }

            public override void VisitPosition(Vector value)
            {
                _positions.Add(value);
            }

            public override void VisitNormal(Vector value)
            {
                _normals.Add(value);
            }

            public override void VisitPrimitivePoint(ushort v, ushort n, byte tu, byte tv)
            {
                Mesh.Positions.Add(_positions[v].ToPoint3D());
                Mesh.Normals.Add(_normals[n].ToVector3D());

                var offsetTu = _page * 128;
                Mesh.TextureCoordinates.Add(new Point((offsetTu + tu) / _textureSize.Width, tv / _textureSize.Height));
            }
        }
    }
}
