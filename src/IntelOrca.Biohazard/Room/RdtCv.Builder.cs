using System;
using System.Collections.Generic;
using System.IO;

namespace IntelOrca.Biohazard.Room
{
    public partial class RdtCv
    {
        public class Builder : IRdtBuilder
        {
            private readonly MemoryStream _ms;

            public List<Item> Items { get; } = new List<Item>();
            public List<Aot> Aots { get; } = new List<Aot>();
            public CvScript Script { get; set; } = new CvScript();

            public Builder(byte[] data)
            {
                _ms = new MemoryStream(data);
            }

            public RdtCv ToRdt()
            {
                var br = new BinaryReader(_ms);
                var bw = new BinaryWriter(_ms);
                _ms.Position = 16;
                var tableOffset = br.ReadInt32();
                var modelOffset = br.ReadInt32();
                var motionOffset = br.ReadInt32();
                var scriptOffset = br.ReadInt32();
                var textureOffset = br.ReadInt32();

                _ms.Position = tableOffset;
                br.ReadInt32();
                br.ReadInt32();
                br.ReadInt32();
                br.ReadInt32();
                var items = br.ReadInt32();
                br.ReadInt32();
                br.ReadInt32();
                var aots = br.ReadInt32();

                _ms.Position = 256;
                br.ReadInt32();
                br.ReadInt32();
                br.ReadInt32();
                br.ReadInt32();
                var itemCount = br.ReadInt32();
                br.ReadInt32();
                br.ReadInt32();
                var aotCount = br.ReadInt32();

                if (itemCount != Items.Count)
                    throw new Exception("Changing number of items is not supported");

                if (aotCount != Aots.Count)
                    throw new Exception("Changing number of doors is not supported");

                if (textureOffset - scriptOffset != Script.Data.Length)
                    throw new Exception("Changing script length is not supported");

                _ms.Position = items;
                foreach (var item in Items)
                {
                    bw.Write(item);
                }

                _ms.Position = aots;
                foreach (var aot in Aots)
                {
                    bw.Write(aot);
                }

                _ms.Position = scriptOffset;
                bw.Write(Script.Data);

                return new RdtCv(_ms.ToArray());
            }

            IRdt IRdtBuilder.ToRdt() => ToRdt();
        }
    }
}
