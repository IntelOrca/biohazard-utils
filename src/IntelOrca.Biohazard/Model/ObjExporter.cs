﻿using System;
using System.IO;
using System.Text;

namespace IntelOrca.Biohazard.Model
{
    public class ObjExporter
    {
        private StringBuilder _sb = new StringBuilder();
        private string? _objPath;
        private double _textureWidth;
        private double _textureHeight;

        private void Begin(string objPath, int numPages)
        {
            _objPath = objPath;
            _textureWidth = numPages * 128.0;
            _textureHeight = 256.0;

            var mtlPath = Path.ChangeExtension(objPath, ".mtl");
            var imgPath = Path.ChangeExtension(objPath, ".png");
            AppendLine("newmtl main");
            AppendLine("Ka 1.000 1.000 1.000");
            AppendLine("Kd 1.000 1.000 1.000");
            AppendLine($"map_Kd {Path.GetFileName(imgPath)}");
            File.WriteAllText(mtlPath, _sb.ToString());

            _sb.Clear();
            AppendLine($"mtllib {Path.GetFileName(mtlPath)}");
            AppendLine($"usemtl main");
        }

        private void End()
        {
            File.WriteAllText(_objPath!, _sb.ToString());
        }

        public void Export(IModelMesh mesh, string objPath, int numPages, Func<int, Emr.Vector>? partTranslate = null)
        {
            var meshConverter = new MeshConverter();
            var md2 = (Md2)meshConverter.ConvertMesh(mesh, BioVersion.Biohazard3, remap: false);

            Begin(objPath, numPages);

            var objIndex = 0;
            var vIndex = 1;
            var tvIndex = 1;
            foreach (var obj in md2.Objects)
            {
                var translate = partTranslate == null ? new Emr.Vector() : partTranslate(objIndex);

                AppendLine($"o part_{objIndex:00}");
                foreach (var v in md2.GetPositionData(obj))
                {
                    AppendDataLine("v", (translate.x + v.x) / 1000.0, (translate.y + v.y) / 1000.0, (translate.z + v.z) / 1000.0);
                }
                foreach (var v in md2.GetNormalData(obj))
                {
                    AppendDataLine("vn", v.x / 5000.0, v.y / 5000.0, v.z / 5000.0);
                }
                foreach (var t in md2.GetTriangles(obj))
                {
                    var page = t.page & 0x0F;
                    var offsetU = page * 128;
                    AppendDataLine("vt", (offsetU + t.tu2) / _textureWidth, 1 - t.tv2 / _textureHeight);
                    AppendDataLine("vt", (offsetU + t.tu1) / _textureWidth, 1 - t.tv1 / _textureHeight);
                    AppendDataLine("vt", (offsetU + t.tu0) / _textureWidth, 1 - t.tv0 / _textureHeight);
                }
                foreach (var t in md2.GetQuads(obj))
                {
                    var page = t.page & 0x0F;
                    var offsetU = page * 128;
                    AppendDataLine("vt", (offsetU + t.tu2) / _textureWidth, 1 - t.tv2 / _textureHeight);
                    AppendDataLine("vt", (offsetU + t.tu3) / _textureWidth, 1 - t.tv3 / _textureHeight);
                    AppendDataLine("vt", (offsetU + t.tu1) / _textureWidth, 1 - t.tv1 / _textureHeight);
                    AppendDataLine("vt", (offsetU + t.tu0) / _textureWidth, 1 - t.tv0 / _textureHeight);
                }
                AppendLine($"s 1");
                foreach (var t in md2.GetTriangles(obj))
                {
                    AppendLine($"f {t.v2 + vIndex}/{tvIndex + 0}/{t.v2 + vIndex} {t.v1 + vIndex}/{tvIndex + 1}/{t.v1 + vIndex} {t.v0 + vIndex}/{tvIndex + 2}/{t.v0 + vIndex}");
                    tvIndex += 3;
                }
                AppendLine($"s 1");
                foreach (var t in md2.GetQuads(obj))
                {
                    AppendLine($"f {t.v2 + vIndex}/{tvIndex + 0}/{t.v2 + vIndex} {t.v3 + vIndex}/{tvIndex + 1}/{t.v3 + vIndex} {t.v1 + vIndex}/{tvIndex + 2}/{t.v1 + vIndex} {t.v0 + vIndex}/{tvIndex + 3}/{t.v0 + vIndex}");
                    tvIndex += 4;
                }

                objIndex++;
                vIndex += obj.vtx_count;
            }
            End();
        }

        private void AppendDataLine(string kind, params double[] parameters)
        {
            _sb.Append(kind);
            _sb.Append(' ');
            foreach (var p in parameters)
            {
                _sb.AppendFormat("{0:0.000000}", p);
                _sb.Append(' ');
            }
            _sb.Remove(_sb.Length - 1, 1);
            AppendLine();
        }

        private void AppendLine(string s)
        {
            _sb.Append(s);
            AppendLine();
        }

        private void AppendLine() => _sb.Append('\n');
    }
}