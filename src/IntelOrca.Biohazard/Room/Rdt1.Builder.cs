using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using IntelOrca.Biohazard.Model;

namespace IntelOrca.Biohazard.Room
{
    public unsafe partial class Rdt1
    {
        public class Builder
        {
            public Rdt1Header Header { get; set; }
            public byte[] LIT { get; set; } = new byte[0];
            public byte[] RID { get; set; } = new byte[0];
            public List<ModelTextureIndex> EmbeddedObjectModelTable { get; set; } = new List<ModelTextureIndex>();
            public List<ModelTextureIndex> EmbeddedItemModelTable { get; set; } = new List<ModelTextureIndex>();
            public byte[] RVD { get; set; } = new byte[0];
            public byte[] PRI { get; set; } = new byte[0];
            public byte[] SCA { get; set; } = new byte[0];
            public byte[] BLK { get; set; } = new byte[0];
            public byte[] FLR { get; set; } = new byte[0];
            public ScdProcedure InitSCD { get; set; }
            public ScdProcedure MainSCD { get; set; }
            public EventScd EventSCD { get; set; }
            public byte[] EMR { get; set; } = new byte[0];
            public byte[] EDD { get; set; } = new byte[0];
            public byte[] MSG { get; set; } = new byte[0];
            public byte[] ESPIDs { get; set; } = new byte[0];
            public byte[] EDT { get; set; } = new byte[0];
            public byte[] VH { get; set; } = new byte[0];
            public byte[] VB { get; set; } = new byte[0];

            public List<TimFile> CameraTextures { get; set; } = new List<TimFile>();
            public List<Tmd> EmbeddedObjectTmd { get; set; } = new List<Tmd>();
            public List<TimFile> EmbeddedObjectTim { get; set; } = new List<TimFile>();
            public List<Tmd> EmbeddedItemTmd { get; set; } = new List<Tmd>();
            public List<TimFile> EmbeddedItemTim { get; set; } = new List<TimFile>();
            public EmbeddedItemIcons EmbeddedItemIcons { get; set; }
            public List<Esp> Esps { get; set; } = new List<Esp>();
            public List<TimFile> EspTextures { get; set; } = new List<TimFile>();

            public Rdt1 ToRdt()
            {
                // Validate
                if (LIT.Length != 0x42)
                    throw new InvalidDataException("LIT must be 0x42 bytes long.");

                var offsetTable = new int[19];

                var ms = new MemoryStream();
                var bw = new BinaryWriter(ms);

                bw.Write(Header);
                bw.Write(LIT);

                // Offsets placeholder
                var offsetTableOffset = ms.Position;
                for (var i = 0; i < 19; i++)
                {
                    bw.Write(0);
                }

                Debug.Assert(ms.Position == 0x94);
                bw.Write(RID);

                offsetTable[3] = (int)ms.Position;
                for (var i = 0; i < EmbeddedItemModelTable.Count; i++)
                {
                    bw.Write(0);
                    bw.Write(0);
                }

                offsetTable[2] = (int)ms.Position;
                for (var i = 0; i < EmbeddedObjectModelTable.Count; i++)
                {
                    bw.Write(0);
                    bw.Write(0);
                }

                offsetTable[0] = (int)ms.Position;
                bw.Write(RVD);
                bw.Write(-1);

                bw.Write(PRI);

                foreach (var tim in CameraTextures)
                    bw.Write(tim.GetBytes());

                offsetTable[1] = (int)ms.Position;
                bw.Write(SCA);
                bw.Write(SCA.Length);
                offsetTable[4] = (int)ms.Position;
                bw.Write(BLK);
                bw.Write((ushort)0);
                offsetTable[5] = (int)ms.Position;
                bw.Write(FLR);

                offsetTable[6] = (int)ms.Position;
                bw.Write(InitSCD.Data.ToArray());
                offsetTable[7] = (int)ms.Position;
                bw.Write(MainSCD.Data.ToArray());
                offsetTable[8] = (int)ms.Position;
                bw.Write(EventSCD.Data.ToArray());
                offsetTable[9] = (int)ms.Position;
                bw.Write(EMR);
                offsetTable[10] = (int)ms.Position;
                bw.Write(EDD);
                offsetTable[11] = (int)ms.Position;
                bw.Write(MSG);

                offsetTable[12] = (int)ms.Position;
                bw.Write(EmbeddedItemIcons.Data.ToArray());

                offsetTable[13] = (int)ms.Position;
                bw.Write(ESPIDs);

                var espTable = new int[Esps.Count];
                for (var i = 0; i < Esps.Count; i++)
                {
                    espTable[i] = (int)ms.Position;
                    bw.Write(Esps[i].Data.ToArray());
                }

                offsetTable[14] = (int)ms.Position;
                foreach (var o in espTable)
                {
                    bw.Write(o);
                }

                var itemTmdTable = new int[EmbeddedItemTmd.Count];
                for (var i = 0; i < EmbeddedItemTmd.Count; i++)
                {
                    itemTmdTable[i] = (int)ms.Position;
                    bw.Write(EmbeddedItemTmd[i].Data.ToArray());
                }

                var objectTmdTable = new int[EmbeddedObjectTmd.Count];
                for (var i = 0; i < EmbeddedObjectTmd.Count; i++)
                {
                    objectTmdTable[i] = (int)ms.Position;
                    bw.Write(EmbeddedObjectTmd[i].Data.ToArray());
                }

                offsetTable[16] = (int)ms.Position;
                bw.Write(EDT);
                offsetTable[17] = (int)ms.Position;
                bw.Write(VH);
                offsetTable[18] = (int)ms.Position;
                bw.Write(VB);

                var itemTimTable = new int[EmbeddedItemTim.Count];
                for (var i = 0; i < EmbeddedItemTim.Count; i++)
                {
                    itemTimTable[i] = (int)ms.Position;
                    bw.Write(EmbeddedItemTim[i].GetBytes());
                }

                var objectTimTable = new int[EmbeddedObjectTim.Count];
                for (var i = 0; i < EmbeddedObjectTim.Count; i++)
                {
                    objectTimTable[i] = (int)ms.Position;
                    bw.Write(EmbeddedObjectTim[i].GetBytes());
                }

                var espTimTable = new int[EspTextures.Count];
                for (var i = 0; i < EspTextures.Count; i++)
                {
                    espTimTable[i] = (int)ms.Position;
                    bw.Write(EspTextures[i].GetBytes());
                }

                var undefinedEspTims = 8 - espTimTable.Length;
                for (var i = 0; i < undefinedEspTims; i++)
                    bw.Write(-1);

                offsetTable[15] = (int)ms.Position;
                foreach (var o in espTimTable)
                    bw.Write(o);

                // Write item TIM table
                ms.Position = offsetTable[3];
                for (var i = 0; i < EmbeddedItemModelTable.Count; i++)
                {
                    bw.Write(itemTmdTable[EmbeddedItemModelTable[i].Model]);
                    bw.Write(itemTimTable[EmbeddedItemModelTable[i].Texture]);
                }

                // Write object TIM table
                ms.Position = offsetTable[2];
                for (var i = 0; i < EmbeddedObjectModelTable.Count; i++)
                {

                    bw.Write(objectTmdTable[EmbeddedObjectModelTable[i].Model]);
                    bw.Write(objectTimTable[EmbeddedObjectModelTable[i].Texture]);
                }

                // Write offsets
                ms.Position = offsetTableOffset;
                for (var i = 0; i < 19; i++)
                {
                    bw.Write(offsetTable[i]);
                }

                var bytes = ms.ToArray();
                return new Rdt1(bytes);
            }
        }
    }
}
