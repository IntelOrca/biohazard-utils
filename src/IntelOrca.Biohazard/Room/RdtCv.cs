using System;
using System.IO;
using System.Runtime.InteropServices;
using IntelOrca.Biohazard.Extensions;

namespace IntelOrca.Biohazard.Room
{
    public partial class RdtCv : IRdt
    {
        // 0x000: header
        // 0x080: offsets
        // 0x100: counts
        //        text
        //        sysmes
        //        models
        //        motion
        //        texture

        public BioVersion Version => BioVersion.BiohazardCv;

        public ReadOnlyMemory<byte> Data { get; }

        public RdtCv(string path)
            : this(File.ReadAllBytes(path))
        {
        }

        public RdtCv(ReadOnlyMemory<byte> data)
        {
            Data = data;
        }

        public Builder ToBuilder()
        {
            var builder = new Builder(Data.ToArray());
            builder.Aots.AddRange(Aots.ToArray());
            builder.Items.AddRange(Items.ToArray());
            builder.Script = Script;
            return builder;
        }

        public FileHeader Header => Data.GetSafeSpan<FileHeader>(0, 1)[0];
        private ReadOnlySpan<int> ScriptOffsets => Data.GetSafeSpan<int>(Header.TableOffset, 15);
        private ReadOnlySpan<int> ScriptCounts => Data.GetSafeSpan<int>(256, 14);

        public ReadOnlySpan<Item> Items => GetTable<Item>(4);
        public ReadOnlySpan<Aot> Aots => GetTable<Aot>(7);
        public ReadOnlySpan<Reaction> Reactions
        {
            get
            {
                var reactionOffset = ScriptOffsets[13];
                var textOffset = ScriptOffsets[14];
                var length = textOffset - reactionOffset;
                var count = length / 2056;
                return Data.GetSafeSpan<Reaction>(reactionOffset, count);
            }
        }

        public CvScript Script
        {
            get
            {
                var mem = Data[Header.ScriptOffset..Header.TextureOffset];
                return new CvScript(mem);
            }
        }

        private ReadOnlySpan<T> GetTable<T>(int index) where T : struct
        {
            var offset = ScriptOffsets[index];
            var count = ScriptCounts[index];
            return Data.GetSafeSpan<T>(offset, count);
        }

        IRdtBuilder IRdt.ToBuilder() => ToBuilder();

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct FileHeader
        {
            public int Version;
            public int Unk04;
            public int Unk08;
            public int Unk0C;
            public int TableOffset;
            public int ModelOffset;
            public int MotionOffset;
            public int ScriptOffset;
            public int TextureOffset;
            public int Unk24;
            public int Unk28;
            public int Unk2C;
            public int Unk30;
            public int Unk34;
            public int Unk38;
            public int Unk3C;
            public int Unk40;
            public int Unk44;
            public int Unk48;
            public int Unk4C;
            public int Unk50;
            public int Unk54;
            public int Unk58;
            public int Unk5C;
            public fixed byte Author[32];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Item
        {
            public byte Unk00;
            public byte Unk01;
            public byte Unk02;
            public byte Unk03;
            public int Type;
            public int Unk08;
            public int X;
            public int Y;
            public int Z;
            public short XRot;
            public short YRot;
            public short ZRot;
            public short Unk1E;
            public byte Unk20;
            public byte Unk21;
            public byte Unk22;
            public byte Unk23;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Aot
        {
            public byte Unk00;
            public byte Kind;
            public byte Unk02;
            public byte Unk03;
            public int Flags;
            public int Unk08;
            public int Unk0C;
            public int Unk10;
            public int Unk14;
            public int Unk18;
            public int Unk1C;
            public byte Stage;          // or item index
            public byte Room;           // or message id
            public byte ExitId;
            public byte Transition;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct Reaction
        {
            public uint Unk00;
            public uint Unk01;
            public fixed byte Data[2048];
        }
    }
}
