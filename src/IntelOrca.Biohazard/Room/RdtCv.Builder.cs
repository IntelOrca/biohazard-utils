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
            public List<Door> Doors { get; } = new List<Door>();

            public Builder(byte[] data)
            {
                _ms = new MemoryStream(data);
            }

            public RdtCv ToRdt()
            {
                var br = new BinaryReader(_ms);
                var bw = new BinaryWriter(_ms);
                _ms.Position = 16;
                _ms.Position = br.ReadInt32();
                br.ReadInt32();
                br.ReadInt32();
                br.ReadInt32();
                br.ReadInt32();
                var items = br.ReadInt32();
                br.ReadInt32();
                br.ReadInt32();
                var doors = br.ReadInt32();

                _ms.Position = 256;
                br.ReadInt32();
                br.ReadInt32();
                br.ReadInt32();
                br.ReadInt32();
                var itemCount = br.ReadInt32();
                br.ReadInt32();
                br.ReadInt32();
                var doorCount = br.ReadInt32();

                if (itemCount != Items.Count)
                    throw new Exception("Changing number of items is not supported");

                if (doorCount != Doors.Count)
                    throw new Exception("Changing number of doors is not supported");

                _ms.Position = items;
                foreach (var item in Items)
                {
                    bw.Write(item);
                }

                _ms.Position = doors;
                foreach (var door in Doors)
                {
                    bw.Write(door);
                }

                return new RdtCv(_ms.ToArray());
            }

            IRdt IRdtBuilder.ToRdt() => ToRdt();
        }
    }
}
