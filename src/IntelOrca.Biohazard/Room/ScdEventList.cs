using System;
using System.IO;
using System.Runtime.InteropServices;
using IntelOrca.Biohazard.Script;

namespace IntelOrca.Biohazard.Room
{
    public readonly struct ScdEventList
    {
        public ReadOnlyMemory<byte> Data { get; }

        public ScdEventList(ReadOnlyMemory<byte> data)
        {
            Data = data;
        }

        public int Count
        {
            get
            {
                var offsets = MemoryMarshal.Cast<byte, int>(Data.Span);
                var count = 0;
                for (var i = 0; i < offsets.Length; i++)
                {
                    if (offsets[i] == 0)
                    {
                        break;
                    }
                    count++;
                }
                return count;
            }
        }

        public ScdEvent this[int index]
        {
            get
            {
                var count = Count;
                if (index < 0 || index >= count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                var offset = GetOffset(index);
                var length = index < count - 1 ? GetOffset(index + 1) - offset : Data.Length - offset;
                return new ScdEvent(Data.Slice(offset, length));
            }
        }

        public int GetOffset(int index)
        {
            var offsets = MemoryMarshal.Cast<byte, int>(Data.Span);
            return offsets[index];
        }
    }

    public readonly struct ScdEvent
    {
        public ReadOnlyMemory<byte> Data { get; }

        public ScdEvent(ReadOnlyMemory<byte> data)
        {
            Data = data;
        }

        public string AnalyseEvent()
        {
            var bio1 = new Bio1ConstantTable();

            var msproc = new MemoryStream();
            var bw = new BinaryWriter(msproc);

            var ms = new MemoryStream(Data.ToArray());
            var br = new BinaryReader(ms);

            while (ms.Position < Data.Length)
            {
                var a = br.ReadByte();
                if (a == 0)
                {
                }
                else if (a == 1)
                {
                }
                else if (a == 4)
                {
                    br.ReadByte();
                    br.ReadByte();
                }
                else if (a == 5)
                {
                    var evtId = br.ReadByte();
                    br.ReadByte();
                    bw.Write((byte)20);
                    bw.Write((byte)0);
                    bw.Write((byte)0);
                    bw.Write(evtId);
                }
                else if (a == 6)
                {
                    var len = br.ReadByte();
                    while (true)
                    {
                        var next = br.ReadUInt16();
                        if (next == 0)
                            break;
                        var opcodes = br.ReadBytes(next - 2);
                        bw.Write(opcodes);
                    }
                }
                else if (a == 7)
                {
                    br.ReadByte();
                    var opcode = br.ReadByte();
                    var opcodeLen = bio1.GetInstructionSize(opcode, br);
                    ms.Position--;
                    var opcodeBytes = br.ReadBytes(opcodeLen);
                    bw.Write(opcodeBytes);
                }
                else if (a == 8)
                {
                    br.ReadByte();
                }
                else if (a == 0x86) // unsure about this
                {
                    br.ReadByte();
                }
                else if (a == 0xF6)
                {
                    br.ReadByte();
                }
                else if (a == 0xF8)
                {
                    br.ReadByte();
                    br.ReadByte();
                    br.ReadByte();
                }
                else if (a == 0xFC)
                {
                    var len = br.ReadByte();
                    br.ReadBytes(len);
                }
                else if (a == 0xFF)
                {
                }
                else
                {
                    a = a;
                }
            }

            var dummyproc = new ScdProcedure(BioVersion.Biohazard1, msproc.ToArray());
            var scdReader = new ScdReader();
            return scdReader.Diassemble(dummyproc, BioScriptKind.Event, true);
        }
    }
}
