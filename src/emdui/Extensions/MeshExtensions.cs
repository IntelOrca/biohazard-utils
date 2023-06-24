using System;
using IntelOrca.Biohazard;

namespace emdui.Extensions
{
    internal static class MeshExtensions
    {
        public static string GetDefaultExtension(this IModelMesh mesh)
        {
            switch (mesh.Version)
            {
                case BioVersion.Biohazard1:
                    return ".TMD";
                case BioVersion.Biohazard2:
                    return ".MD1";
                case BioVersion.Biohazard3:
                    return ".MD2";
                default:
                    throw new NotImplementedException();
            }
        }

        public static string GetExtensionPattern(this IModelMesh mesh)
        {
            switch (mesh.Version)
            {
                case BioVersion.Biohazard1:
                    return "*.tmd";
                case BioVersion.Biohazard2:
                    return "*.md1";
                case BioVersion.Biohazard3:
                    return "*.md2";
                default:
                    throw new NotImplementedException();
            }
        }

        public static IModelMesh CreateEmptyPart(this IModelMesh mesh)
        {
            switch (mesh.Version)
            {
                case BioVersion.Biohazard1:
                {
                    var builder = new TmdBuilder();
                    builder.Parts.Add(new TmdBuilder.Part());
                    return builder.ToTmd();
                }
                case BioVersion.Biohazard2:
                {
                    var part = new Md1Builder.Part();
                    part.Positions.Add(new Md1.Vector());
                    part.Normals.Add(new Md1.Vector());
                    part.Triangles.Add(new Md1.Triangle());
                    part.TriangleTextures.Add(new Md1.TriangleTexture());

                    var builder = new Md1Builder();
                    builder.Parts.Add(part);
                    return builder.ToMd1();
                }
                case BioVersion.Biohazard3:
                {
                    var builder = new Md2Builder();
                    builder.Parts.Add(new Md2Builder.Part());
                    return builder.ToMd2();
                }
                default:
                    throw new NotImplementedException();
            }
        }

        public static IModelMesh ExtractPart(this IModelMesh mesh, int partIndex)
        {
            switch (mesh)
            {
                case Tmd tmd:
                {
                    var builder = tmd.ToBuilder();
                    var tmp = builder.Parts[partIndex];
                    builder.Parts.Clear();
                    builder.Parts.Add(tmp);
                    return builder.ToTmd();
                }
                case Md1 md1:
                {
                    var builder = md1.ToBuilder();
                    var tmp = builder.Parts[partIndex];
                    builder.Parts.Clear();
                    builder.Parts.Add(tmp);
                    return builder.ToMd1();
                }
                case Md2 md2:
                {
                    var builder = md2.ToBuilder();
                    var tmp = builder.Parts[partIndex];
                    builder.Parts.Clear();
                    builder.Parts.Add(tmp);
                    return builder.ToMd2();
                }
                default:
                    throw new NotSupportedException();
            }
        }

        public static IModelMesh AddPart(this IModelMesh mesh, IModelMesh newMesh)
        {
            switch (mesh)
            {
                case Tmd tmd:
                {
                    var srcBuilder = ((Tmd)newMesh).ToBuilder();
                    var builder = tmd.ToBuilder();
                    builder.Parts.Add(srcBuilder.Parts[0]);
                    return builder.ToTmd();
                }
                case Md1 md1:
                {
                    var srcBuilder = ((Md1)newMesh).ToBuilder();
                    var builder = md1.ToBuilder();
                    builder.Parts.Add(srcBuilder.Parts[0]);
                    return builder.ToMd1();
                }
                case Md2 md2:
                {
                    var srcBuilder = ((Md2)newMesh).ToBuilder();
                    var builder = md2.ToBuilder();
                    builder.Parts.Add(srcBuilder.Parts[0]);
                    return builder.ToMd2();
                }
                default:
                    throw new NotSupportedException();
            }
        }

        public static IModelMesh ReplacePart(this IModelMesh mesh, int partIndex, IModelMesh newMesh)
        {
            switch (mesh)
            {
                case Tmd tmd:
                {
                    var srcBuilder = ((Tmd)newMesh).ToBuilder();
                    var builder = tmd.ToBuilder();
                    builder.Parts[partIndex] = srcBuilder.Parts[0];
                    return builder.ToTmd();
                }
                case Md1 md1:
                {
                    var srcBuilder = ((Md1)newMesh).ToBuilder();
                    var builder = md1.ToBuilder();
                    builder.Parts[partIndex] = srcBuilder.Parts[0];
                    return builder.ToMd1();
                }
                case Md2 md2:
                {
                    var srcBuilder = ((Md2)newMesh).ToBuilder();
                    var builder = md2.ToBuilder();
                    builder.Parts[partIndex] = srcBuilder.Parts[0];
                    return builder.ToMd2();
                }
                default:
                    throw new NotSupportedException();
            }
        }

        public static IModelMesh RemovePart(this IModelMesh mesh, int partIndex)
        {
            switch (mesh)
            {
                case Tmd tmd:
                {
                    var builder = tmd.ToBuilder();
                    builder.Parts.RemoveAt(partIndex);
                    return builder.ToTmd();
                }
                case Md1 md1:
                {
                    var builder = md1.ToBuilder();
                    builder.Parts.RemoveAt(partIndex);
                    return builder.ToMd1();
                }
                case Md2 md2:
                {
                    var builder = md2.ToBuilder();
                    builder.Parts.RemoveAt(partIndex);
                    return builder.ToMd2();
                }
                default:
                    throw new NotSupportedException();
            }
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
                    return builder.ToTmd();
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
                    return builder.ToMd1();
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
                    return builder.ToMd2();
                }
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
