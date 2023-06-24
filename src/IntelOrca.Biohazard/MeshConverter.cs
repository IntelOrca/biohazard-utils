using System;

namespace IntelOrca.Biohazard
{
    public class MeshConverter
    {
        private static readonly int[] g_partRemap2to1 = new[]
        {
            0, 2, 6, 7, 8, 3, 4, 5, 1, 12, 13, 14, 9, 10, 11
        };

        private static readonly int[] g_partRemap3to2 = new[]
        {
            0, 8, 9, 10, 11, 12, 13, 14, 1, 2, 3, 4, 5, 6, 7
        };

        public int[] GetPartRemap(BioVersion from, BioVersion target)
        {
            if (from == BioVersion.Biohazard2)
            {
                if (target == BioVersion.Biohazard1)
                {
                    return g_partRemap2to1;
                }
            }
            throw new NotSupportedException();
        }

        public IModelMesh ConvertMesh(IModelMesh mesh, BioVersion version)
        {
            switch (version)
            {
                case BioVersion.Biohazard1:
                    var visitor = new TmdConverter(GetPartRemap(mesh.Version, version));
                    visitor.Accept(mesh);
                    return visitor.ToTmd();
                default:
                    throw new NotSupportedException();
            }
        }

        public Emr ConvertEmr(Emr targetEmr, Emr sourceEmr)
        {
            var remap = GetPartRemap(sourceEmr.Version, targetEmr.Version);
            var emrBuilder = targetEmr.ToBuilder();
            for (var i = 0; i < remap.Length; i++)
            {
                var srcPartIndex = i;
                var dstPartIndex = remap[i];
                var src = sourceEmr.GetRelativePosition(srcPartIndex);
                emrBuilder.RelativePositions[dstPartIndex] = src;
            }
            return emrBuilder.ToEmr();
        }

        private class TmdConverter : MeshVisitor
        {
            private TmdBuilder _builder = new TmdBuilder();
            private TmdBuilder.Part? _part;
            private Tmd.Triangle _triangle;
            private int _pointIndex;
            private int[] _partRemap;

            public Tmd ToTmd() => _builder.ToTmd();

            public TmdConverter(int[] partRemap)
            {
                _partRemap = partRemap;
                Trianglulate = true;
            }

            public override bool VisitPart(int index)
            {
                _part = new TmdBuilder.Part();
                return true;
            }

            public override void VisitPosition(Vector value)
            {
                _part!.Positions.Add(new Tmd.Vector(value.x, value.y, value.z));
            }

            public override void VisitNormal(Vector value)
            {
                _part!.Normals.Add(new Tmd.Vector(value.x, value.y, value.z));
            }

            public override void VisitPrimitive(int numPoints, byte page)
            {
                _triangle = new Tmd.Triangle();
                _triangle.unknown = 0x34000609;
                _triangle.clutId = (ushort)(0x7800 | (page * 0x40));
                _triangle.page = (byte)(0x80 | (page & 0x0F));
                _pointIndex = 0;
            }

            public override void VisitPrimitivePoint(ushort v, ushort n, byte tu, byte tv)
            {
                switch (_pointIndex)
                {
                    case 0:
                        _triangle.v0 = v;
                        _triangle.n0 = n;
                        _triangle.tu0 = tu;
                        _triangle.tv0 = tv;
                        break;
                    case 1:
                        _triangle.v1 = v;
                        _triangle.n1 = n;
                        _triangle.tu1 = tu;
                        _triangle.tv1 = tv;
                        break;
                    case 2:
                        _triangle.v2 = v;
                        _triangle.n2 = n;
                        _triangle.tu2 = tu;
                        _triangle.tv2 = tv;
                        break;
                }
                _pointIndex++;
            }

            public override void LeavePrimitive()
            {
                _part!.Triangles.Add(_triangle);
            }

            public override void LeavePart(int index)
            {
                var targetIndex = index < _partRemap.Length ? _partRemap[index] : index;
                while (_builder.Parts.Count <= targetIndex)
                {
                    _builder.Parts.Add(new TmdBuilder.Part());
                }
                _builder.Parts[targetIndex] = _part!;
            }
        }
    }
}
