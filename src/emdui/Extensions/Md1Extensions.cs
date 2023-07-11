using System;
using System.Linq;
using System.Windows.Media.Media3D;
using IntelOrca.Biohazard.Model;

namespace emdui.Extensions
{
    internal static class Md1Extensions
    {
        public static Point3D ToPoint3D(this MeshVisitor.Vector v) => new Point3D(v.x, v.y, v.z);
        public static Vector3D ToVector3D(this MeshVisitor.Vector v)
        {
            var result = new Vector3D(-v.x, v.y, v.z);
            result.Normalize();
            return result;
        }

        public static Point3D ToPoint3D(this Md1.Vector v) => new Point3D(v.x, v.y, v.z);
        public static Vector3D ToVector3D(this Md1.Vector v) => new Vector3D(v.x, v.y, v.z);
        public static Point3D ToPoint3D(this Md2.Vector v) => new Point3D(v.x, v.y, v.z);
        public static Vector3D ToVector3D(this Md2.Vector v) => new Vector3D(v.x, v.y, v.z);

        public static int CountPolygons(this IModelMesh mesh)
        {
            var count = 0;
            var builder = mesh.ToBuilder();
            switch (builder)
            {
                case Tmd.Builder tmd:
                    count += tmd.Parts.Sum(x => x.Triangles.Count);
                    break;
                case Md1.Builder md1:
                    count += md1.Parts.Sum(x => x.Triangles.Count);
                    break;
                case Md2.Builder md2:
                    count += md2.Parts.Sum(x => x.Triangles.Count);
                    break;
                default:
                    throw new NotSupportedException();
            }
            return count;
        }
    }
}
