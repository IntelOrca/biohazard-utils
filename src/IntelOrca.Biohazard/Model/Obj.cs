using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace IntelOrca.Biohazard.Model
{
    internal sealed class Obj : IModelMesh
    {
        public const double PositionMultiplier = 1000;
        public const double NormalMultiplier = 5000;

        private readonly Group[] _groups;

        public BioVersion Version => BioVersion.Unknown;
        public int NumParts { get; }
        public ReadOnlyMemory<byte> Data => throw new NotImplementedException();
        public ReadOnlySpan<Group> Groups => _groups;

        public IModelMeshBuilder ToBuilder()
        {
            throw new NotImplementedException();
        }

        public Obj(string path)
        {
            var rawForm = RawForm.FromFile(path);
            var groups = new List<Group>();
            foreach (var o in rawForm.Objects)
            {
                var allVertices = o.Primitives.SelectMany(x => x.Vertices);
                var minPositionIndex = allVertices.Select(x => x.Position).Min();
                var maxPositionIndex = allVertices.Select(x => x.Position).Max();
                var minNormalIndex = allVertices.Select(x => x.Normal).Min();
                var maxNormalIndex = allVertices.Select(x => x.Normal).Max();
                var minTextureIndex = allVertices.Select(x => x.Texture).Min();
                var maxTextureIndex = allVertices.Select(x => x.Texture).Max();

                var positions = rawForm.Positions.Skip(minPositionIndex).Take(maxPositionIndex - minPositionIndex).ToArray();
                var normals = rawForm.Normals.Skip(minPositionIndex).Take(maxPositionIndex - minPositionIndex).ToArray();
                var textureCoordinates = rawForm.TextureCoordinates.Skip(minPositionIndex).Take(maxPositionIndex - minPositionIndex).ToArray();
                groups.Add(new Group(o.Name, positions, normals, textureCoordinates, o.Primitives.ToArray()));
            }
            _groups = groups.OrderBy(x => x.Name).ToArray();
        }

        private class RawForm
        {
            public List<Vertex> Positions { get; } = new List<Vertex>();
            public List<Vertex> Normals { get; } = new List<Vertex>();
            public List<TextureCoordinate> TextureCoordinates { get; } = new List<TextureCoordinate>();
            public List<RawFormObject> Objects { get; } = new List<RawFormObject>();

            public static RawForm FromFile(string path)
            {
                var result = new RawForm();
                RawFormObject? currentGroup = null;
                var text = File.ReadAllText(path);
                var tr = new StringReader(text);
                string line;
                while ((line = tr.ReadLine()) != null)
                {
                    line = line.Trim();
                    var commentIndex = line.IndexOf('#');
                    if (commentIndex != -1)
                    {
                        line = line.Substring(0, commentIndex);
                    }

                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0)
                        continue;

                    switch (parts[0])
                    {
                        case "o":
                            if (currentGroup != null)
                            {
                                result.Objects.Add(currentGroup);
                            }
                            currentGroup = new RawFormObject(parts[1]);
                            break;
                        case "v":
                            result.Positions.Add(new Vertex(double.Parse(parts[1]), double.Parse(parts[2]), double.Parse(parts[3])));
                            break;
                        case "vn":
                            result.Normals.Add(new Vertex(double.Parse(parts[1]), double.Parse(parts[2]), double.Parse(parts[3])));
                            break;
                        case "vt":
                            result.TextureCoordinates.Add(new TextureCoordinate(double.Parse(parts[1]), double.Parse(parts[2])));
                            break;
                        case "f":
                            currentGroup!.Primitives.Add(new Primitive(
                                parts
                                    .Skip(1)
                                    .Select(ParseFaceVertex)
                                    .ToArray()));
                            break;
                    }
                }
                if (currentGroup != null)
                {
                    result.Objects.Add(currentGroup);
                }
                return result;
            }

            private static FaceVertex ParseFaceVertex(string component)
            {
                var parts = component.Split('/');
                return new FaceVertex()
                {
                    Position = int.Parse(parts[0]) - 1,
                    Texture = int.Parse(parts[1]) - 1,
                    Normal = int.Parse(parts[2]) - 1
                };
            }

            public class RawFormObject
            {
                public string Name { get; }
                public List<Primitive> Primitives { get; } = new List<Primitive>();

                public RawFormObject(string name)
                {
                    Name = name;
                }
            }
        }

        [DebuggerDisplay("v {x} {y} {z}")]
        public struct Vertex
        {
            public double x, y, z;

            public Vertex(double x, double y, double z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }
        }

        [DebuggerDisplay("vt {u} {v}")]
        public struct TextureCoordinate
        {
            public double u, v;

            public TextureCoordinate(double u, double v)
            {
                this.u = u;
                this.v = v;
            }
        }

        [DebuggerDisplay("{Position}/{Texture}/{Normal}")]
        public struct FaceVertex
        {
            public int Position;
            public int Texture;
            public int Normal;

            public FaceVertex(int position, int texture, int normal)
            {
                Position = position;
                Texture = texture;
                Normal = normal;
            }
        }

        [DebuggerDisplay("Name = {Name} Primitives = {Primitive.Count}")]
        public class Group
        {
            public string Name { get; }
            public Vertex[] Positions { get; }
            public Vertex[] Normals { get; }
            public TextureCoordinate[] TextureCoordinates { get; }
            public Primitive[] Primitives { get; }

            public Group(
                string name,
                Vertex[] positions,
                Vertex[] normals,
                TextureCoordinate[] textureCoordinates,
                Primitive[] primitives)
            {
                Name = name;
                Positions = positions;
                Normals = normals;
                TextureCoordinates = textureCoordinates;
                Primitives = primitives;
            }
        }

        public readonly struct Primitive
        {
            public FaceVertex[] Vertices { get; }
            public int NumPoints => Vertices.Length;

            public Primitive(FaceVertex[] vertices)
            {
                Vertices = vertices;
            }

            public override readonly string ToString()
            {
                var sb = new StringBuilder();
                sb.Append('f');
                sb.Append(' ');
                foreach (var v in Vertices)
                {
                    sb.Append(v.ToString());
                    sb.Append(' ');
                }
                if (sb[sb.Length - 1] == ' ')
                {
                    sb.Remove(sb.Length - 1, 1);
                }
                return sb.ToString();
            }
        }
    }
}
