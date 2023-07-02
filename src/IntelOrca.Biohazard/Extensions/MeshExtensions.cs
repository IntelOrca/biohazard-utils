using System;
using IntelOrca.Biohazard.Model;

namespace IntelOrca.Biohazard.Extensions
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

        public static IModelMesh EditMeshTextures(this IModelMesh mesh, Action<PrimitiveTexture> modify)
        {
            var converter = new MeshConverter();
            var md1 = (Md1)converter.ConvertMesh(mesh, BioVersion.Biohazard2, false);
            var builder = md1.ToBuilder();

            var modifyArgs = new PrimitiveTexture();
            for (int partIndex = 0; partIndex < builder.Parts.Count; partIndex++)
            {
                var part = builder.Parts[partIndex];
                modifyArgs.PartIndex = partIndex;

                for (var i = 0; i < part.TriangleTextures.Count; i++)
                {
                    var tt = part.TriangleTextures[i];

                    modifyArgs.NumPoints = 3;
                    modifyArgs.Page = tt.page & 0x0F;
                    modifyArgs.Points[0] = new PrimitiveTexture.UV(tt.page, tt.u0, tt.v0);
                    modifyArgs.Points[1] = new PrimitiveTexture.UV(tt.page, tt.u1, tt.v1);
                    modifyArgs.Points[2] = new PrimitiveTexture.UV(tt.page, tt.u2, tt.v2);

                    modify(modifyArgs);

                    tt.u0 = modifyArgs.Points[0].U;
                    tt.v0 = modifyArgs.Points[0].V;
                    tt.u1 = modifyArgs.Points[1].U;
                    tt.v1 = modifyArgs.Points[1].V;
                    tt.u2 = modifyArgs.Points[2].U;
                    tt.v2 = modifyArgs.Points[2].V;
                    tt.page = (byte)(0x80 | (modifyArgs.Page & 0x0F));

                    part.TriangleTextures[i] = tt;
                }

                for (var i = 0; i < part.QuadTextures.Count; i++)
                {
                    var qt = part.QuadTextures[i];

                    modifyArgs.NumPoints = 4;
                    modifyArgs.Page = qt.page & 0x0F;
                    modifyArgs.Points[0] = new PrimitiveTexture.UV(qt.page, qt.u0, qt.v0);
                    modifyArgs.Points[1] = new PrimitiveTexture.UV(qt.page, qt.u1, qt.v1);
                    modifyArgs.Points[2] = new PrimitiveTexture.UV(qt.page, qt.u2, qt.v2);
                    modifyArgs.Points[3] = new PrimitiveTexture.UV(qt.page, qt.u3, qt.v3);

                    modify(modifyArgs);

                    qt.u0 = modifyArgs.Points[0].U;
                    qt.v0 = modifyArgs.Points[0].V;
                    qt.u1 = modifyArgs.Points[1].U;
                    qt.v1 = modifyArgs.Points[1].V;
                    qt.u2 = modifyArgs.Points[2].U;
                    qt.v2 = modifyArgs.Points[2].V;
                    qt.u3 = modifyArgs.Points[3].U;
                    qt.v3 = modifyArgs.Points[3].V;
                    qt.page = (byte)(0x80 | (modifyArgs.Page & 0x0F));

                    part.QuadTextures[i] = qt;
                }
            }
            return converter.ConvertMesh(builder.ToMesh(), mesh.Version, false);
        }

        public static IModelMesh SwapPages(this IModelMesh mesh, int pageA, int pageB)
        {
            return mesh.EditMeshTextures(modify =>
            {
                if (modify.Page == pageA)
                    modify.Page = pageB;
                else if (modify.Page == pageB)
                    modify.Page = pageA;
            });
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
                        tt.clutId = (ushort)(0x7800 | page * 0x40);
                        tt.page = (byte)(tt.page & 0xF0 | page & 0x0F);
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
                        tt.page = (byte)(tt.page & 0xF0 | page & 0x0F);
                        part.TriangleTextures[i] = tt;
                    }
                    for (var i = 0; i < part.QuadTextures.Count; i++)
                    {
                        var qt = part.QuadTextures[i];
                        qt.page = (byte)(qt.page & 0xF0 | page & 0x0F);
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
                        tt.page = (byte)(tt.page & 0xF0 | page & 0x0F);
                        part.Triangles[i] = tt;
                    }
                    for (var i = 0; i < part.Quads.Count; i++)
                    {
                        var qt = part.Quads[i];
                        qt.page = (byte)(qt.page & 0xF0 | page & 0x0F);
                        part.Quads[i] = qt;
                    }
                    return builder.ToMesh();
                }
                default:
                    throw new NotSupportedException();
            }
        }

        public class PrimitiveTexture
        {
            public int PartIndex { get; set; }
            public int Page { get; set; }
            public int NumPoints { get; set; }
            public UV[] Points { get; } = new UV[4];

            public struct UV
            {
                public byte Page { get; set; }
                public byte U { get; set; }
                public byte V { get; set; }

                public int X
                {
                    get => (Page * 128) + U;
                    set
                    {
                        Page = (byte)(value / 128);
                        U = (byte)(value % 128);
                    }
                }

                public int Y
                {
                    get => V;
                    set => V = (byte)value;
                }

                public UV(int page, byte u, byte v)
                {
                    Page = (byte)(page & 0x0F);
                    U = u;
                    V = v;
                }

                public UV(int page, int u, int v)
                {
                    Page = (byte)(page & 0x0F);
                    var minX = Page * 128;
                    var maxX = minX + 127;
                    U = (byte)(Math.Max(minX, Math.Min(u, maxX)) % 128);
                    V = (byte)Math.Max(0, Math.Min(v, 255));
                }
            }
        }
    }
}
