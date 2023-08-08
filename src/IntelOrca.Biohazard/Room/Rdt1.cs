using System;
using System.IO;
using System.Runtime.InteropServices;
using IntelOrca.Biohazard.Extensions;
using IntelOrca.Biohazard.Model;

namespace IntelOrca.Biohazard.Room
{
    public unsafe partial class Rdt1
    {
        public BioVersion Version => BioVersion.Biohazard1;
        public ReadOnlyMemory<byte> Data { get; }

        public Rdt1(string path)
            : this(File.ReadAllBytes(path))
        {
        }

        public Rdt1(ReadOnlyMemory<byte> data)
        {
            Data = data;
        }

        public Rdt1Header Header => GetSpan<Rdt1Header>(0x00, 1)[0];
        public ReadOnlySpan<byte> LIT => GetSpan<byte>(0x06, 0x42);
        public ReadOnlySpan<int> Offsets => GetSpan<int>(0x48, 19);
        public ReadOnlySpan<byte> RID => GetSpan<byte>(0x94, Header.nCut * sizeof(Rdt1Camera));
        public EmbeddedModelTable EmbeddedObjectModelTable => new EmbeddedModelTable(Data.Slice(Offsets[3], Header.nOmodel * 8));
        public EmbeddedModelTable EmbeddedItemModelTable => new EmbeddedModelTable(Data.Slice(Offsets[4], Header.nItem * 8));
        public ReadOnlySpan<byte> RVD => MemoryMarshal.Cast<Rdt1CameraSwitch, byte>(CameraSwitches);
        public ReadOnlySpan<byte> SCA => GetSpan<byte>(Offsets[1], Offsets[4] - Offsets[1] - 4);
        public ReadOnlySpan<byte> BLK => GetSpan<byte>(Offsets[4], Offsets[5] - Offsets[4] - 2);
        public ReadOnlySpan<byte> FLR => GetSpan<byte>(Offsets[5], Offsets[6] - Offsets[5]);
        public ScdProcedure InitSCD => ReadSCD(Offsets[6]);
        public ScdProcedure MainSCD => ReadSCD(Offsets[7]);
        public EventScd EventSCD => new EventScd(Data.Slice(Offsets[8], Offsets[9] - Offsets[8]));
        public ReadOnlySpan<byte> EDD => GetSpan<byte>(Offsets[10], Offsets[11] - Offsets[10]);
        public ReadOnlySpan<byte> EMR => GetSpan<byte>(Offsets[9], Offsets[10] - Offsets[9]);
        public ReadOnlySpan<byte> MSG => GetSpan<byte>(Offsets[11], Offsets[12] - Offsets[11]);
        public ReadOnlySpan<byte> SND => GetSpan<byte>(Offsets[16], Offsets[17] - Offsets[16]);
        public ReadOnlySpan<byte> VH => GetSpan<byte>(Offsets[17], Offsets[18] - Offsets[17]);
        public ReadOnlySpan<byte> VB => GetSpan<byte>(Offsets[18], 1024);

        public ReadOnlySpan<Rdt1Camera> Cameras
        {
            get
            {
                var numCameras = Header.nCut;
                return GetSpan<Rdt1Camera>(0x94, numCameras);
            }
        }
        public ReadOnlySpan<Rdt1CameraSwitch> CameraSwitches
        {
            get
            {
                var offset = Offsets[0];
                var maxSwitches = (Data.Length - offset) / sizeof(Rdt1Camera);
                var switches = GetSpan<Rdt1CameraSwitch>(offset, maxSwitches);
                var numSwitches = 0;
                for (var i = 0; i < switches.Length; i++)
                {
                    if (switches[i].to == ushort.MaxValue &&
                        switches[i].from == ushort.MaxValue)
                    {
                        numSwitches = i;
                        break;
                    }
                }
                return switches.Slice(0, numSwitches);
            }
        }
        public ReadOnlySpan<Rdt1EmbeddedModel> EmbeddedObjectModels
        {
            get
            {
                var offset = Offsets[2];
                return GetSpan<Rdt1EmbeddedModel>(offset, Header.nOmodel);
            }
        }
        public ReadOnlySpan<Rdt1EmbeddedModel> EmbeddedItemModels
        {
            get
            {
                var offset = Offsets[3];
                return GetSpan<Rdt1EmbeddedModel>(offset, Header.nItem);
            }
        }

        public Builder ToBuilder()
        {
            var builder = new Builder();

            var embeddedTmdOffsets = new OffsetTracker();
            var embeddedTimOffsets = new OffsetTracker();
            foreach (var pair in EmbeddedObjectModelTable.Offsets)
            {
                var tmdIndex = embeddedTmdOffsets.GetOrAdd(pair.Model);
                var timIndex = embeddedTimOffsets.GetOrAdd(pair.Texture);
                builder.EmbeddedObjectModelTable.Add(new ModelTextureIndex(tmdIndex, timIndex));
            }
            foreach (var pair in EmbeddedItemModelTable.Offsets)
            {
                var tmdIndex = embeddedTmdOffsets.GetOrAdd(pair.Model);
                var timIndex = embeddedTimOffsets.GetOrAdd(pair.Texture);
                builder.EmbeddedItemModelTable.Add(new ModelTextureIndex(tmdIndex, timIndex));
            }
            foreach (var offset in embeddedTmdOffsets)
                builder.EmbeddedObjects.Add(ReadEmbeddedTmd(offset));
            foreach (var offset in embeddedTimOffsets)
                builder.EmbeddedTextures.Add(ReadEmbeddedTim(offset));

            builder.Header = Header;
            builder.LIT = LIT.ToArray();
            builder.RID = RID.ToArray();
            builder.RVD = RVD.ToArray();
            builder.SCA = SCA.ToArray();
            builder.BLK = BLK.ToArray();
            builder.FLR = FLR.ToArray();
            builder.InitSCD = InitSCD;
            builder.MainSCD = MainSCD;
            builder.EventSCD = EventSCD;
            builder.EDD = EDD.ToArray();
            builder.EMR = EMR.ToArray();
            builder.MSG = MSG.ToArray();
            builder.SND = SND.ToArray();
            builder.VH = VH.ToArray();
            builder.VB = VB.ToArray();
            return builder;
        }

        private Tmd ReadEmbeddedTmd(int offset)
        {
            var tmdLength = GetSpan<int>(offset, 1)[0];
            return new Tmd(Data.Slice(offset, tmdLength));
        }

        private TimFile ReadEmbeddedTim(int offset)
        {
            var timLength = TimFile.CalculateLength(new SpanStream(Data.Slice(offset)));
            var timData = Data.Slice(offset, timLength);
            return new TimFile(timData);
        }

        private ScdProcedure ReadSCD(int offset)
        {
            var len = GetSpan<ushort>(offset, 1)[0];
            return new ScdProcedure(Version, Data.Slice(offset + 2, len));
        }

        private ReadOnlySpan<T> GetSpan<T>(int offset, int count) where T : struct => Data.GetSafeSpan<T>(offset, count);

        [StructLayout(LayoutKind.Sequential)]
        public struct Rdt1Header
        {
            public byte nSprite;
            public byte nCut;
            public byte nOmodel;
            public byte nItem;
            public byte nDoor;
            public byte nRoom_at;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rdt1Camera
        {
            public int masks_offset;
            public int tim_masks_offset;
            public int camera_from_x;
            public int camera_from_y;
            public int camera_from_z;
            public int camera_to_x;
            public int camera_to_y;
            public int camera_to_z;
            public fixed int unknown1[3];
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rdt1CameraSwitch
        {
            public ushort to;
            public ushort from;
            public short x1, y1;
            public short x2, y2;
            public short x3, y3;
            public short x4, y4;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rdt1EmbeddedModel
        {
            public uint tmd_offset;
            public uint tim_offset;
        }
    }
}
