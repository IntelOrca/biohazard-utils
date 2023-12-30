using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace IntelOrca.Biohazard.Room
{
    public readonly struct CvScript
    {
        public ReadOnlyMemory<byte> Data { get; }

        public CvScript(ReadOnlyMemory<byte> data)
        {
            Data = data;
        }

        public int Count
        {
            get
            {
                if (Data.Length < 4)
                    return 0;

                return GetOffset(0) / 4;
            }
        }

        public CvScriptRoutine this[int index]
        {
            get
            {
                var count = Count;
                if (index < 0 || index >= count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                var offset = GetOffset(index);
                var length = index < count - 1 ? GetOffset(index + 1) - offset : Data.Length - offset;
                return new CvScriptRoutine(Data.Slice(offset, length));
            }
        }

        public int GetOffset(int index)
        {
            var offsets = MemoryMarshal.Cast<byte, int>(Data.Span);
            return offsets[index];
        }

        public Builder ToBuilder()
        {
            var builder = new Builder();
            for (var i = 0; i < Count; i++)
                builder.Routines.Add(this[i]);
            return builder;
        }

        public class Builder
        {
            public List<CvScriptRoutine> Routines { get; } = new List<CvScriptRoutine>();

            public CvScript ToScript()
            {
                var ms = new MemoryStream();
                var bw = new BinaryWriter(ms);

                var offset = Routines.Count * 4;
                for (var i = 0; i < Routines.Count; i++)
                {
                    bw.Write(offset);
                    offset += Routines[i].Data.Length;
                }

                foreach (var routine in Routines)
                {
                    bw.Write(routine.Data);
                }

                var bytes = ms.ToArray();
                return new CvScript(bytes);
            }
        }
    }

    public readonly struct CvScriptRoutine
    {
        public ReadOnlyMemory<byte> Data { get; }

        public CvScriptRoutine(ReadOnlyMemory<byte> data)
        {
            Data = data;
        }
    }
}
