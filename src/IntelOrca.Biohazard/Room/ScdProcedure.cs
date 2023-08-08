using System;
using System.Runtime.InteropServices;

namespace IntelOrca.Biohazard.Room
{
    public readonly struct ScdProcedure
    {
        public BioVersion Version { get; }
        public ReadOnlyMemory<byte> Data { get; }

        public ScdProcedure(BioVersion version, ReadOnlyMemory<byte> data)
        {
            Version = version;
            Data = data;
        }
    }

    public readonly struct EventScd
    {
        public ReadOnlyMemory<byte> Data { get; }

        public EventScd(ReadOnlyMemory<byte> data)
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

        public ReadOnlyMemory<byte> GetProcedure(int index)
        {
            var count = Count;
            if (index < 0 || index >= count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var offset = GetProcedureOffset(index);
            var length = offset < count - 1 ? GetProcedureOffset(index + 1) - offset : Data.Length - offset;
            return Data.Slice(offset, length);
        }

        private int GetProcedureOffset(int index)
        {
            var offsets = MemoryMarshal.Cast<byte, int>(Data.Span);
            return offsets[index];
        }
    }
}
