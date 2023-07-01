using System;
using System.Windows.Media.Media3D;
using IntelOrca.Biohazard;
using IntelOrca.Biohazard.Model;

namespace emdui.Extensions
{
    internal static class Md1Extensions
    {
        public static Point3D ToPoint3D(this MeshVisitor.Vector v) => new Point3D(v.x, v.y, v.z);
        public static Vector3D ToVector3D(this MeshVisitor.Vector v) => new Vector3D(v.x, v.y, v.z);
        public static Point3D ToPoint3D(this Md1.Vector v) => new Point3D(v.x, v.y, v.z);
        public static Vector3D ToVector3D(this Md1.Vector v) => new Vector3D(v.x, v.y, v.z);
        public static Point3D ToPoint3D(this Md2.Vector v) => new Point3D(v.x, v.y, v.z);
        public static Vector3D ToVector3D(this Md2.Vector v) => new Vector3D(v.x, v.y, v.z);

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
    }
}
