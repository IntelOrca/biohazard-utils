using System;

namespace IntelOrca.Biohazard.Model
{
    public static class MeshExtensions
    {
        public static IModelMeshBuilder Add(this IModelMeshBuilder builder, IModelMeshBuilderPart part)
        {
            var result = builder.Add();
            builder[builder.Count - 1] = part;
            return result;
        }

        public static string GetDefaultExtension(this IModelMesh mesh)
        {
            return mesh.Version switch
            {
                BioVersion.Biohazard1 => ".TMD",
                BioVersion.Biohazard2 => ".MD1",
                BioVersion.Biohazard3 => ".MD2",
                _ => throw new NotImplementedException(),
            };
        }

        public static string GetExtensionPattern(this IModelMesh mesh)
        {
            return mesh.Version switch
            {
                BioVersion.Biohazard1 => "*.tmd",
                BioVersion.Biohazard2 => "*.md1",
                BioVersion.Biohazard3 => "*.md2",
                _ => throw new NotImplementedException(),
            };
        }

        public static IModelMesh CreateEmptyPart(this IModelMesh mesh)
        {
            var builder = mesh.ToBuilder();
            builder.Clear();
            builder.Add();
            return builder.ToMesh();
        }

        public static IModelMesh ExtractPart(this IModelMesh mesh, int partIndex)
        {
            var builder = mesh.ToBuilder();
            var tmp = builder[partIndex];
            builder.Clear();
            builder.Add(tmp);
            return builder.ToMesh();
        }

        public static IModelMesh AddPart(this IModelMesh mesh, IModelMesh newMesh)
        {
            var srcBuilder = newMesh.ToBuilder();
            var builder = mesh.ToBuilder();
            builder.Add(srcBuilder[0]);
            return builder.ToMesh();
        }

        public static IModelMesh ReplacePart(this IModelMesh mesh, int partIndex, IModelMesh newMesh)
        {
            var srcBuilder = newMesh.ToBuilder();
            var builder = mesh.ToBuilder();
            builder[partIndex] = srcBuilder[0];
            return builder.ToMesh();
        }

        public static IModelMesh RemovePart(this IModelMesh mesh, int partIndex)
        {
            var builder = mesh.ToBuilder();
            builder.RemoveAt(partIndex);
            return builder.ToMesh();
        }

        public static IModelMesh MoveUVToPage(this IModelMesh mesh, int partIndex, int page)
        {
            switch (mesh)
            {
                case Tmd tmd:
                {
                    var builder = tmd.ToBuilder();
                    var part = builder.Parts[partIndex];
                    for (var i = 0; i < part.Triangles.Count; i++)
                    {
                        var tt = part.Triangles[i];
                        tt.clutId = (ushort)(0x7800 | (page * 0x40));
                        tt.page = (byte)((tt.page & 0xF0) | (page & 0x0F));
                        part.Triangles[i] = tt;
                    }
                    return builder.ToMesh();
                }
                case Md1 md1:
                {
                    var builder = md1.ToBuilder();
                    var part = builder.Parts[partIndex];
                    for (var i = 0; i < part.TriangleTextures.Count; i++)
                    {
                        var tt = part.TriangleTextures[i];
                        tt.page = (byte)((tt.page & 0xF0) | (page & 0x0F));
                        part.TriangleTextures[i] = tt;
                    }
                    for (var i = 0; i < part.QuadTextures.Count; i++)
                    {
                        var qt = part.QuadTextures[i];
                        qt.page = (byte)((qt.page & 0xF0) | (page & 0x0F));
                        part.QuadTextures[i] = qt;
                    }
                    return builder.ToMesh();
                }
                case Md2 md2:
                {
                    var builder = md2.ToBuilder();
                    var part = builder.Parts[partIndex];
                    for (var i = 0; i < part.Triangles.Count; i++)
                    {
                        var tt = part.Triangles[i];
                        tt.page = (byte)((tt.page & 0xF0) | (page & 0x0F));
                        part.Triangles[i] = tt;
                    }
                    for (var i = 0; i < part.Quads.Count; i++)
                    {
                        var qt = part.Quads[i];
                        qt.page = (byte)((qt.page & 0xF0) | (page & 0x0F));
                        part.Quads[i] = qt;
                    }
                    return builder.ToMesh();
                }
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
