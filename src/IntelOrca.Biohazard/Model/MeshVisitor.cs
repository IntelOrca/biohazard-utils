using System.Runtime.InteropServices;

namespace IntelOrca.Biohazard.Model
{
    public abstract class MeshVisitor
    {
        private IModelMesh? _mesh;

        public bool Trianglulate { get; set; }

        public void Accept(IModelMesh mesh)
        {
            _mesh = mesh;
            if (_mesh is Tmd tmd)
            {
                for (var i = 0; i < tmd.NumParts; i++)
                {
                    if (VisitPart(i))
                    {
                        var obj = tmd.Objects[i];
                        var positions = tmd.GetPositionData(in obj);
                        var normals = tmd.GetNormalData(in obj);
                        var triangles = tmd.GetTriangles(in obj);

                        foreach (var position in positions)
                        {
                            VisitPosition(new Vector(position.x, position.y, position.z));
                        }
                        foreach (var normal in normals)
                        {
                            VisitNormal(new Vector(normal.x, normal.y, normal.z));
                        }
                        foreach (var triangle in triangles)
                        {
                            VisitPrimitive(3, (byte)(triangle.page & 0x0F));
                            VisitPrimitivePoint(triangle.v0, triangle.n0, triangle.tu0, triangle.tv0);
                            VisitPrimitivePoint(triangle.v1, triangle.n1, triangle.tu1, triangle.tv1);
                            VisitPrimitivePoint(triangle.v2, triangle.n2, triangle.tu2, triangle.tv2);
                            LeavePrimitive();
                        }
                        LeavePart(i);
                    }
                }
            }
            else if (_mesh is Md1 md1)
            {
                for (var i = 0; i < md1.NumParts; i++)
                {
                    if (VisitPart(i))
                    {
                        var objTriangles = md1.Objects[i * 2];
                        var objQuads = md1.Objects[i * 2 + 1];
                        var positions = md1.GetPositionData(in objTriangles);
                        var normals = md1.GetNormalData(in objTriangles);
                        var triangles = md1.GetTriangles(in objTriangles);
                        var quads = md1.GetQuads(in objQuads);
                        var triangleTextures = md1.GetTriangleTextures(in objTriangles);
                        var quadTextures = md1.GetQuadTextures(in objQuads);

                        foreach (var position in positions)
                        {
                            VisitPosition(new Vector(position.x, position.y, position.z));
                        }
                        foreach (var normal in normals)
                        {
                            VisitNormal(new Vector(normal.x, normal.y, normal.z));
                        }
                        for (var j = 0; j < triangles.Length; j++)
                        {
                            var triangle = triangles[j];
                            var triangleTexture = triangleTextures[j];
                            VisitPrimitive(3, (byte)(triangleTexture.page & 0x0F));
                            VisitPrimitivePoint(triangle.v0, triangle.n0, triangleTexture.u0, triangleTexture.v0);
                            VisitPrimitivePoint(triangle.v1, triangle.n1, triangleTexture.u1, triangleTexture.v1);
                            VisitPrimitivePoint(triangle.v2, triangle.n2, triangleTexture.u2, triangleTexture.v2);
                            LeavePrimitive();
                        }
                        if (Trianglulate)
                        {
                            for (var j = 0; j < quads.Length; j++)
                            {
                                var quad = quads[j];
                                var quadTexture = quadTextures[j];
                                VisitPrimitive(3, (byte)(quadTexture.page & 0x0F));
                                VisitPrimitivePoint(quad.v0, quad.n0, quadTexture.u0, quadTexture.v0);
                                VisitPrimitivePoint(quad.v1, quad.n1, quadTexture.u1, quadTexture.v1);
                                VisitPrimitivePoint(quad.v2, quad.n2, quadTexture.u2, quadTexture.v2);
                                LeavePrimitive();
                                VisitPrimitive(3, (byte)(quadTexture.page & 0x0F));
                                VisitPrimitivePoint(quad.v3, quad.n3, quadTexture.u3, quadTexture.v3);
                                VisitPrimitivePoint(quad.v2, quad.n2, quadTexture.u2, quadTexture.v2);
                                VisitPrimitivePoint(quad.v1, quad.n1, quadTexture.u1, quadTexture.v1);
                                LeavePrimitive();
                            }
                        }
                        else
                        {
                            for (var j = 0; j < quads.Length; j++)
                            {
                                var quad = quads[j];
                                var quadTexture = quadTextures[j];
                                VisitPrimitive(4, (byte)(quadTexture.page & 0x0F));
                                VisitPrimitivePoint(quad.v0, quad.n0, quadTexture.u0, quadTexture.v0);
                                VisitPrimitivePoint(quad.v1, quad.n1, quadTexture.u1, quadTexture.v1);
                                VisitPrimitivePoint(quad.v2, quad.n2, quadTexture.u2, quadTexture.v2);
                                VisitPrimitivePoint(quad.v3, quad.n3, quadTexture.u3, quadTexture.v3);
                                LeavePrimitive();
                            }
                        }
                        LeavePart(i);
                    }
                }
            }
            else if (_mesh is Md2 md2)
            {
                for (var i = 0; i < md2.NumParts; i++)
                {
                    if (VisitPart(i))
                    {
                        var obj = md2.Objects[i];
                        var positions = md2.GetPositionData(in obj);
                        var normals = md2.GetNormalData(in obj);
                        var triangles = md2.GetTriangles(in obj);
                        var quads = md2.GetQuads(in obj);

                        foreach (var position in positions)
                        {
                            VisitPosition(new Vector(position.x, position.y, position.z));
                        }
                        foreach (var normal in normals)
                        {
                            VisitNormal(new Vector(normal.x, normal.y, normal.z));
                        }
                        foreach (var triangle in triangles)
                        {
                            VisitPrimitive(3, (byte)(triangle.page & 0x0F));
                            VisitPrimitivePoint(triangle.v0, triangle.v0, triangle.tu0, triangle.tv0);
                            VisitPrimitivePoint(triangle.v1, triangle.v1, triangle.tu1, triangle.tv1);
                            VisitPrimitivePoint(triangle.v2, triangle.v2, triangle.tu2, triangle.tv2);
                            LeavePrimitive();
                        }
                        if (Trianglulate)
                        {
                            foreach (var quad in quads)
                            {
                                VisitPrimitive(3, (byte)(quad.page & 0x0F));
                                VisitPrimitivePoint(quad.v0, quad.v0, quad.tu0, quad.tv0);
                                VisitPrimitivePoint(quad.v1, quad.v1, quad.tu1, quad.tv1);
                                VisitPrimitivePoint(quad.v2, quad.v2, quad.tu2, quad.tv2);
                                LeavePrimitive();
                                VisitPrimitive(3, (byte)(quad.page & 0x0F));
                                VisitPrimitivePoint(quad.v3, quad.v3, quad.tu3, quad.tv3);
                                VisitPrimitivePoint(quad.v2, quad.v2, quad.tu2, quad.tv2);
                                VisitPrimitivePoint(quad.v1, quad.v1, quad.tu1, quad.tv1);
                                LeavePrimitive();
                            }
                        }
                        else
                        {
                            foreach (var quad in quads)
                            {
                                VisitPrimitive(4, (byte)(quad.page & 0x0F));
                                VisitPrimitivePoint(quad.v0, quad.v0, quad.tu0, quad.tv0);
                                VisitPrimitivePoint(quad.v1, quad.v1, quad.tu1, quad.tv1);
                                VisitPrimitivePoint(quad.v2, quad.v2, quad.tu2, quad.tv2);
                                VisitPrimitivePoint(quad.v3, quad.v3, quad.tu3, quad.tv3);
                                LeavePrimitive();
                            }
                        }
                        LeavePart(i);
                    }
                }
            }
            else if (_mesh is Obj wfobj)
            {
                for (var i = 0; i < wfobj.NumParts; i++)
                {
                    if (VisitPart(i))
                    {
                        var group = wfobj.Groups[i];
                        foreach (var position in group.Positions)
                        {
                            VisitPosition(new Vector(
                                (short)(position.x * Obj.PositionMultiplier),
                                (short)(position.y * Obj.PositionMultiplier),
                                (short)(position.z * Obj.PositionMultiplier)));
                        }
                        foreach (var normal in group.Normals)
                        {
                            VisitNormal(new Vector(
                                (short)(normal.x * Obj.NormalMultiplier),
                                (short)(normal.y * Obj.NormalMultiplier),
                                (short)(normal.z * Obj.NormalMultiplier)));
                        }
                        foreach (var primitive in group.Primitives)
                        {
                            // Determine which texture page this is on
                            var firstTex = primitive.Vertices[0].Texture;
                            var firstU = group.TextureCoordinates[firstTex].u;
                            var firstPage = ((int)(firstU * 4)) & 3;

                            var subPrimitives = new Obj.Primitive[0];
                            if (primitive.NumPoints == 4 && Trianglulate)
                            {
                                subPrimitives = new[] {
                                    primitive,
                                    new Obj.Primitive(new[]
                                    {
                                        primitive.Vertices[3],
                                        primitive.Vertices[2],
                                        primitive.Vertices[1]
                                    })
                                };
                            }
                            else if (primitive.NumPoints == 3)
                            {
                                subPrimitives = new[] { primitive };
                            }
                            foreach (var sp in subPrimitives)
                            {
                                VisitPrimitive(sp.NumPoints, (byte)(firstPage & 0x0F));
                                foreach (var p in primitive.Vertices)
                                {
                                    var texIndex = p.Texture;
                                    var tex = group.TextureCoordinates[texIndex];
                                    var u = (byte)((int)(tex.u * 256) & 0xFF);
                                    var v = (byte)((int)(tex.v * 256) & 0xFF);
                                    VisitPrimitivePoint((ushort)p.Position, (ushort)p.Normal, u, v);
                                }
                                LeavePrimitive();
                            }
                        }
                        LeavePart(i);
                    }
                }
            }
        }

        public virtual bool VisitPart(int index)
        {
            return true;
        }

        public virtual void LeavePart(int index)
        {
        }

        public virtual void VisitPosition(Vector value)
        {
        }

        public virtual void VisitNormal(Vector value)
        {
        }

        public virtual void VisitPrimitive(int numPoints, byte page)
        {
        }

        public virtual void VisitPrimitivePoint(ushort v, ushort n, byte tu, byte tv)
        {
        }

        public virtual void LeavePrimitive()
        {
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Vector
        {
            public short x;
            public short y;
            public short z;
            public short zero;

            public Vector(short x, short y, short z)
                : this()
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }
        }
    }
}
