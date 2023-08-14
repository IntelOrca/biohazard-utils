using System;
using System.Collections.Generic;
using System.IO;
using IntelOrca.Biohazard.Extensions;

namespace IntelOrca.Biohazard.Room
{
    public readonly struct ScdProcedureList
    {
        public BioVersion Version { get; }
        public ReadOnlyMemory<byte> Data { get; }

        public ScdProcedureList(BioVersion version, ReadOnlyMemory<byte> data)
        {
            Version = version;
            Data = data;
        }

        public int Count
        {
            get
            {
                var firstOffset = Data.GetSafeSpan<ushort>(0, 1)[0];
                var numOffsets = firstOffset / 2;
                return numOffsets;
            }
        }

        public ScdProcedure this[int index]
        {
            get
            {
                var count = Count;
                var offset = Data.GetSafeSpan<ushort>(0, count)[index];
                var nextOffset = index == count - 1 ?
                    Data.Length :
                    Data.GetSafeSpan<ushort>(0, count)[index + 1];
                return new ScdProcedure(Version, Data.Slice(offset, nextOffset - offset));
            }
        }

        public Builder ToBuilder()
        {
            var builder = new Builder(Version);
            for (var i = 0; i < Count; i++)
            {
                builder.Procedures.Add(this[i]);
            }
            return builder;
        }

        public class Builder
        {
            public BioVersion Version { get; }
            public List<ScdProcedure> Procedures { get; } = new List<ScdProcedure>();

            public Builder(BioVersion version)
            {
                Version = version;
            }

            public ScdProcedureList ToProcedureList()
            {
                var ms = new MemoryStream();
                var bw = new BinaryWriter(ms);

                var baseOffset = Procedures.Count * 2;
                foreach (var procedure in Procedures)
                {
                    bw.Write((ushort)baseOffset);
                    baseOffset += procedure.Data.Length;
                }
                foreach (var procedure in Procedures)
                {
                    bw.Write(procedure.Data);
                }

                var bytes = ms.ToArray();
                return new ScdProcedureList(Version, bytes);
            }
        }
    }
}
