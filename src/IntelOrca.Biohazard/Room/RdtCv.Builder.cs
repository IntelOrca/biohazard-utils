using System;
using System.Collections.Generic;
using System.IO;

namespace IntelOrca.Biohazard.Room
{
    public partial class RdtCv
    {
        public class Builder : IRdtBuilder
        {
            public FileHeader Header { get; set; }
            public byte[] UnknownDataAfterHeader { get; set; } = new byte[0];
            public CvCameraList Cameras { get; set; }
            public byte[] LightingData { get; set; } = new byte[0];
            public byte[] EnemyData { get; set; } = new byte[0];
            public byte[] ObjectData { get; set; } = new byte[0];
            public List<Item> Items { get; } = new List<Item>();
            public ReadOnlyMemory<byte> EffectData { get; set; }
            public ReadOnlyMemory<byte> BoundaryData { get; set; }
            public List<Aot> Aots { get; } = new List<Aot>();
            public byte[] TriggerData { get; set; } = new byte[0];
            public byte[] PlayerData { get; set; } = new byte[0];
            public byte[] EventData { get; set; } = new byte[0];
            public byte[] Unknown1Data { get; set; } = new byte[0];
            public int Unknown1Count { get; set; }
            public int Unknown2 { get; set; }
            public int Unknown2Count { get; set; }
            public int ReactionCount { get; set; }
            public List<Reaction> Reactions { get; } = new List<Reaction>();
            public byte[] TextData { get; set; } = new byte[0];
            public byte[] SysmesData { get; set; } = new byte[0];
            public byte[] ModelData { get; set; } = new byte[0];
            public byte[] MotionData { get; set; } = new byte[0];
            public ScdProcedureList Script { get; set; }
            public byte[] TextureData { get; set; } = new byte[0];

            public RdtCv ToRdt()
            {
                var header = Header;

                var ms = new MemoryStream();
                var bw = new BinaryWriter(ms);
                bw.Write(header);

                ms.Position = 0x100;
                bw.Write(Cameras.Count);
                bw.Write(LightingData.Length / 224);
                bw.Write(EnemyData.Length / 36);
                bw.Write(ObjectData.Length / 36);
                bw.Write(Items.Count);
                bw.Write(EffectData.Length / 68);
                bw.Write(BoundaryData.Length / 36);
                bw.Write(Aots.Count);
                bw.Write(TriggerData.Length / 36);
                bw.Write(PlayerData.Length / 16);
                bw.Write(EventData.Length / 36);
                bw.Write(Unknown1Count);
                bw.Write(Unknown2Count);
                bw.Write(ReactionCount);

                ms.Position = 0x180;
                bw.Write(UnknownDataAfterHeader);

                ms.Position = 0x46C;

                var tableOffsets = new int[16];
                if (Cameras.Data.Length != 0)
                {
                    tableOffsets[0] = (int)ms.Position;
                    bw.Write(Cameras.Data);
                }

                if (LightingData.Length != 0)
                {
                    tableOffsets[1] = (int)ms.Position;
                    bw.Write(LightingData);
                }

                if (EnemyData.Length != 0)
                {
                    tableOffsets[2] = (int)ms.Position;
                    bw.Write(EnemyData);
                }

                if (ObjectData.Length != 0)
                {
                    tableOffsets[3] = (int)ms.Position;
                    bw.Write(ObjectData);
                }

                if (Items.Count != 0)
                {
                    tableOffsets[4] = (int)ms.Position;
                    foreach (var item in Items)
                    {
                        bw.Write(item);
                    }
                }

                if (EffectData.Length != 0)
                {
                    tableOffsets[5] = (int)ms.Position;
                    bw.Write(EffectData);
                }

                if (BoundaryData.Length != 0)
                {
                    tableOffsets[6] = (int)ms.Position;
                    bw.Write(BoundaryData);
                }

                if (Aots.Count != 0)
                {
                    tableOffsets[7] = (int)ms.Position;
                    foreach (var aot in Aots)
                    {
                        bw.Write(aot);
                    }
                }

                if (TriggerData.Length != 0)
                {
                    tableOffsets[8] = (int)ms.Position;
                    bw.Write(TriggerData);
                }

                if (PlayerData.Length != 0)
                {
                    tableOffsets[9] = (int)ms.Position;
                    bw.Write(PlayerData);
                }

                if (EventData.Length != 0)
                {
                    tableOffsets[10] = (int)ms.Position;
                    bw.Write(EventData);
                }

                if (Unknown1Data.Length != 0)
                {
                    tableOffsets[11] = (int)ms.Position;
                    bw.Write(Unknown1Data);
                }

                tableOffsets[12] = Unknown2;

                if (Reactions.Count != 0)
                {
                    tableOffsets[13] = (int)ms.Position;
                    foreach (var reaction in Reactions)
                    {
                        bw.Write(reaction);
                    }
                }

                if (TextData.Length != 0)
                {
                    tableOffsets[14] = (int)ms.Position;
                    bw.Write(TextData);
                }

                if (SysmesData.Length != 0)
                {
                    tableOffsets[15] = (int)ms.Position;
                    bw.Write(SysmesData);
                }

                if (ModelData.Length != 0)
                {
                    header.ModelOffset = (int)ms.Position;
                    bw.Write(ModelData);
                }

                if (MotionData.Length != 0)
                {
                    header.MotionOffset = (int)ms.Position;
                    bw.Write(MotionData);
                }

                if (Script.Data.Length != 0)
                {
                    header.ScriptOffset = (int)ms.Position;
                    bw.Write(Script.Data);
                }

                if (TextureData.Length != 0)
                {
                    header.TextureOffset = (int)ms.Position;
                    bw.Write(TextureData);
                }

                ms.Position = 0x80;
                header.TableOffset = (int)ms.Position;
                foreach (var offset in tableOffsets)
                {
                    bw.Write(offset);
                }

                ms.Position = 0;
                bw.Write(header);

                Header = header;
                return new RdtCv(ms.ToArray());
            }

            IRdt IRdtBuilder.ToRdt() => ToRdt();
        }
    }
}
