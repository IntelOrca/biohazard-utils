using System;
using System.Collections.Generic;
using System.IO;
using IntelOrca.Biohazard.Model;

namespace IntelOrca.Biohazard.Room
{
    public sealed partial class Rdt2
    {
        public class Builder : IRdtBuilder
        {
            public Rdt2Header Header { get; set; }
            public byte[] RID { get; set; } = new byte[0];
            public byte[] RVD { get; set; } = new byte[0];
            public byte[] LIT { get; set; } = new byte[0];
            public byte[] PRI { get; set; } = new byte[0];
            public byte[] SCA { get; set; } = new byte[0];
            public byte[] BLK { get; set; } = new byte[0];
            public byte[] FLR { get; set; } = new byte[0];
            public ushort? FLRTerminator { get; set; }
            public ScdProcedureList SCDINIT { get; set; }
            public ScdProcedureList SCDMAIN { get; set; }
            public MsgList MSGJA { get; set; }
            public MsgList MSGEN { get; set; }
            public Tim TIMSCROLL { get; set; }
            public EspTable EspTable { get; set; }
            public Rbj RBJ { get; set; }
            public byte[] EDT { get; set; } = new byte[0];
            public byte[] VH { get; set; } = new byte[0];
            public byte[] VB { get; set; } = new byte[0];
            public Tim ESPTIM { get; set; }

            public List<ModelTextureIndex> EmbeddedObjectModelTable { get; set; } = new List<ModelTextureIndex>();
            public List<Md1> EmbeddedObjectMd1 { get; set; } = new List<Md1>();
            public List<Tim> EmbeddedObjectTim { get; set; } = new List<Tim>();

            IRdt IRdtBuilder.ToRdt() => ToRdt();

            public Rdt2 ToRdt()
            {
                // Validate
                if (EmbeddedObjectModelTable.Count != Header.nOmodel)
                    throw new InvalidOperationException("Number of embedded objects does not equal header.");

                var offsetTable = new int[23];

                var ms = new MemoryStream();
                var bw = new BinaryWriter(ms);

                bw.Write(Header);

                // Offsets placeholder
                var offsetTableOffset = ms.Position;
                for (var i = 0; i < offsetTable.Length; i++)
                {
                    bw.Write(0);
                }

                offsetTable[7] = (int)ms.Position;
                bw.Write(RID);

                offsetTable[10] = (int)ms.Position;
                for (var i = 0; i < EmbeddedObjectModelTable.Count; i++)
                {
                    bw.Write(0);
                    bw.Write(0);
                }

                offsetTable[8] = (int)ms.Position;
                bw.Write(RVD);
                bw.Write(-1);

                offsetTable[9] = (int)ms.Position;
                bw.Write(LIT);

                bw.Write(PRI);

                offsetTable[6] = (int)ms.Position;
                bw.Write(SCA);

                if (BLK.Length != 0)
                {
                    offsetTable[12] = (int)ms.Position;
                    bw.Write(BLK);
                }

                offsetTable[11] = (int)ms.Position;
                bw.Write(FLR);
                bw.Write(FLRTerminator ?? 0);

                offsetTable[16] = (int)ms.Position;
                bw.Write(SCDINIT.Data);

                offsetTable[17] = (int)ms.Position;
                bw.Write(SCDMAIN.Data);

                if (!MSGJA.Data.IsEmpty)
                {
                    offsetTable[13] = (int)ms.Position;
                    bw.Write(MSGJA.Data);
                }

                if (!MSGEN.Data.IsEmpty)
                {
                    offsetTable[14] = (int)ms.Position;
                    bw.Write(MSGEN.Data);
                }

                if (!TIMSCROLL.Data.IsEmpty)
                {
                    offsetTable[15] = (int)ms.Position;
                    bw.Write(TIMSCROLL.Data);
                }

                var objectMd1Table = new int[EmbeddedObjectMd1.Count];
                for (var i = 0; i < EmbeddedObjectMd1.Count; i++)
                {
                    objectMd1Table[i] = (int)ms.Position;
                    bw.Write(EmbeddedObjectMd1[i].Data.ToArray());
                }

                if (!EspTable.Data.IsEmpty)
                {
                    offsetTable[18] = (int)ms.Position;
                    bw.Write(EspTable.Data);
                    offsetTable[19] = (int)ms.Position - 4;
                }

                if (!RBJ.Data.IsEmpty)
                {
                    offsetTable[22] = (int)ms.Position;
                    bw.Write(RBJ.Data);
                }

                offsetTable[5] = (int)ms.Position;

                if (EDT.Length != 0)
                {
                    offsetTable[0] = (int)ms.Position;
                    bw.Write(EDT);
                }

                if (VH.Length != 0)
                {
                    offsetTable[1] = (int)ms.Position;
                    bw.Write(VH);
                }

                if (VB.Length != 0)
                {
                    offsetTable[2] = (int)ms.Position;
                    bw.Write(VB);
                }

                if (!ESPTIM.Data.IsEmpty)
                {
                    offsetTable[20] = (int)ms.Position;
                    bw.Write(ESPTIM.Data);
                    offsetTable[21] = (int)ms.Position;
                }

                var objectTimTable = new int[EmbeddedObjectTim.Count];
                for (var i = 0; i < EmbeddedObjectTim.Count; i++)
                {
                    objectTimTable[i] = (int)ms.Position;
                    bw.Write(EmbeddedObjectTim[i].Data);
                }

                // Write object TIM table
                ms.Position = offsetTable[10];
                for (var i = 0; i < EmbeddedObjectModelTable.Count; i++)
                {
                    var obj = EmbeddedObjectModelTable[i];
                    if (obj.Texture != -1)
                        bw.Write(objectTimTable[obj.Texture]);
                    else
                        bw.Write(0);
                    if (obj.Model != -1)
                        bw.Write(objectMd1Table[obj.Model]);
                    else
                        bw.Write(0);
                }

                // Write offsets
                ms.Position = offsetTableOffset;
                for (var i = 0; i < offsetTable.Length; i++)
                {
                    bw.Write(offsetTable[i]);
                }

                var bytes = ms.ToArray();
                // File.WriteAllBytes(@"M:\temp\rdt\rebuilt.rdt", bytes);

                return new Rdt2(bytes);
            }
        }
    }
}
