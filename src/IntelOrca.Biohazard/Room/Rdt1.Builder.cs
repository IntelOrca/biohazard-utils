using System.Collections.Generic;
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
            public byte[] SCA { get; set; } = new byte[0];
            public byte[] BLK { get; set; } = new byte[0];
            public byte[] FLR { get; set; } = new byte[0];
            public ScdProcedure InitSCD { get; set; }
            public ScdProcedure MainSCD { get; set; }
            public EventScd EventSCD { get; set; }
            public byte[] EDD { get; set; } = new byte[0];
            public byte[] EMR { get; set; } = new byte[0];
            public byte[] MSG { get; set; } = new byte[0];
            public byte[] SND { get; set; } = new byte[0];
            public byte[] VH { get; set; } = new byte[0];
            public byte[] VB { get; set; } = new byte[0];
            public List<Tmd> EmbeddedObjects { get; set; } = new List<Tmd>();
            public List<TimFile> EmbeddedTextures { get; set; } = new List<TimFile>();

            public Rdt1 ToRdt()
            {
                // Validate
                if (LIT.Length != 0x42)
                    throw new InvalidDataException("LIT must be 0x42 bytes long.");

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

                bw.Write(RID);
                var embeddedObjectModelTableOffset = (int)ms.Position;
                for (var i = 0; i < embeddedObjectModelTableOffset; i++)
                {
                    bw.Write(0);
                    bw.Write(0);
                }
                var embeddedItemModelTableOffset = (int)ms.Position;
                for (var i = 0; i < embeddedItemModelTableOffset; i++)
                {
                    bw.Write(0);
                    bw.Write(0);
                }

                // Write offsets
                ms.Position = offsetTableOffset;
                bw.Write(0);
                bw.Write(0);
                bw.Write(0);
                bw.Write(embeddedObjectModelTableOffset);
                bw.Write(embeddedItemModelTableOffset);

                var bytes = ms.ToArray();
                return new Rdt1(bytes);
            }
        }
    }
}
