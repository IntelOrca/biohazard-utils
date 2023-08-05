﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using IntelOrca.Biohazard.Model;
using IntelOrca.Biohazard.Script;

namespace IntelOrca.Biohazard
{
    public class RdtFile
    {
        private readonly int[] _offsets;
        private readonly int[] _lengths;
        private readonly List<Range> _eventScripts = new List<Range>();
        private readonly List<Range> _emrs = new List<Range>();
        private readonly List<Range> _edds = new List<Range>();

        public BioVersion Version { get; }
        public byte[] Data { get; private set; }
        public ulong Checksum { get; }

        public RdtFile(string path)
            : this(null, path)
        {
        }

        public RdtFile(string path, BioVersion version)
            : this(version, path)
        {
        }

        public RdtFile(byte[] data, BioVersion version)
            : this(version, data)
        {
        }

        private RdtFile(BioVersion? version, string path)
            : this(version, File.ReadAllBytes(path))
        {
        }

        private RdtFile(BioVersion? version, byte[] data)
        {
            Data = data;
            Version = version ?? DetectVersion(data);
            _offsets = ReadHeader();
            _lengths = GetChunkLengths();
            if (Version == BioVersion.Biohazard2)
            {
                // We need to do AST analysis on SCD to find where the end is
                _lengths[GetScdChunkIndex(BioScriptKind.Init)] = MeasureScript(BioScriptKind.Init);
                _lengths[GetScdChunkIndex(BioScriptKind.Main)] = MeasureScript(BioScriptKind.Main);
                _lengths[13] = MeasureTexts(0);
                _lengths[14] = MeasureTexts(1);
            }
            else if (Version == BioVersion.Biohazard3)
            {
                // We need to do AST analysis on SCD to find where the end is
                _lengths[GetScdChunkIndex(BioScriptKind.Init)] = MeasureScript(BioScriptKind.Init);
            }
            GetNumEventScripts();
            Checksum = Data.CalculateFnv1a();
            if (version == BioVersion.Biohazard2)
            {
                ReadEMRs();
            }
        }

        private static BioVersion DetectVersion(byte[] data)
        {
            return data[0x12] == 0xFF && data[0x13] == 0xFF ?
                BioVersion.Biohazard1 :
                BioVersion.Biohazard2;
        }

        public void Save(string path)
        {
            File.WriteAllBytes(path, Data);
        }

        public MemoryStream GetStream()
        {
            return new MemoryStream(Data);
        }

        public int EventScriptCount => _eventScripts.Count;

        private int MeasureTexts(int language)
        {
            var chunkIndex = language == 0 ? 13 : 14;
            var offset = _offsets[chunkIndex];
            if (offset == 0)
                return 0;

            var ms = new MemoryStream(Data);
            var br = new BinaryReader(ms);
            ms.Position = offset;
            var firstOffset = br.ReadUInt16();
            var numTexts = firstOffset / 2;
            ms.Position = offset + ((numTexts - 1) * 2);
            var lastTextOffset = br.ReadUInt16();
            ms.Position = offset + lastTextOffset;
            while (true)
            {
                var b = br.ReadByte();
                if (b == 0xFE)
                {
                    b = br.ReadByte();
                    break;
                }
            }
            var len = (int)(ms.Position - offset);
            return ((len + 3) / 4) * 4;
        }

        public BioString[] GetTexts(int language)
        {
            var chunkIndex = language == 0 ? 13 : 14;
            var offset = _offsets[chunkIndex];
            if (offset == 0)
            {
                return new BioString[0];
            }

            var length = _lengths[chunkIndex];
            var ms = new MemoryStream(Data);
            ms.Position = offset;
            var br = new BinaryReader(ms);
            var firstOffset = br.ReadUInt16();
            var numTexts = firstOffset / 2;
            var textOffsets = new ushort[numTexts];
            var textLengths = new ushort[numTexts];
            textOffsets[0] = firstOffset;
            for (int i = 1; i < numTexts; i++)
            {
                textOffsets[i] = br.ReadUInt16();
                textLengths[i - 1] = (ushort)(textOffsets[i] - textOffsets[i - 1]);
            }
            textLengths[numTexts - 1] = (ushort)(length - textOffsets[numTexts - 1]);

            var result = new List<BioString>();
            for (int i = 0; i < numTexts; i++)
            {
                ms.Position = offset + textOffsets[i];
                var text = br.ReadBytes(textLengths[i]);
                result.Add(new BioString(text));
            }
            return result.ToArray();
        }

        public void SetTexts(int language, BioString[] texts)
        {
            var chunkIndex = language == 0 ? 13 : 14;

            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            for (var i = 0; i < texts.Length; i++)
            {
                bw.Write((ushort)0);
            }
            var offsets = new List<int>();
            for (var i = 0; i < texts.Length; i++)
            {
                offsets.Add((int)ms.Position);
                bw.Write(texts[i].Data.ToArray());
            }
            ms.Position = 0;
            foreach (var offset in offsets)
            {
                bw.Write((ushort)offset);
            }

            ms.Position = ms.Length;
            while (ms.Position % 4 != 0)
            {
                bw.Write((byte)0);
            }

            UpdateChunk(chunkIndex, ms.ToArray());
        }

        public byte[] GetScd(BioScriptKind kind, int scriptIndex = 0)
        {
            var index = GetScdChunkIndex(kind);
            var start = _offsets[index];
            var length = _lengths[index];
            if (kind == BioScriptKind.Event)
            {
                var range = _eventScripts[scriptIndex];
                var data = new byte[range.Length];
                Array.Copy(Data, start + range.Start, data, 0, range.Length);
                return data;
            }
            else
            {
                var data = new byte[length];
                Array.Copy(Data, start, data, 0, length);
                return data;
            }
        }

        public void SetScd(BioScriptKind kind, byte[] data)
        {
            var index = GetScdChunkIndex(kind);
            UpdateChunk(index, data);
        }

        private void RewriteOffset(Stream stream, int min, int delta)
        {
            var br = new BinaryReader(stream);
            var bw = new BinaryWriter(stream);
            var offset = br.ReadInt32();
            if (offset != 0 && offset != -1 && offset >= min)
            {
                stream.Position -= 4;
                bw.Write(offset + delta);
            }
        }

        private int GetScdChunkIndex(BioScriptKind kind)
        {
            switch (Version)
            {
                case BioVersion.Biohazard1:
                    switch (kind)
                    {
                        case BioScriptKind.Init:
                            return 6;
                        case BioScriptKind.Main:
                            return 7;
                        case BioScriptKind.Event:
                            return 8;
                        default:
                            throw new ArgumentException("Invalid kind", nameof(kind));
                    }
                case BioVersion.Biohazard2:
                    switch (kind)
                    {
                        case BioScriptKind.Init:
                            return 16;
                        case BioScriptKind.Main:
                            return 17;
                        default:
                            throw new ArgumentException("Invalid kind", nameof(kind));
                    }
                case BioVersion.Biohazard3:
                    switch (kind)
                    {
                        case BioScriptKind.Init:
                            return 16;
                        default:
                            throw new ArgumentException("Invalid kind", nameof(kind));
                    }
                default:
                    throw new NotSupportedException();
            }
        }

        private int[] ReadHeader()
        {
            if (Data.Length <= 8)
                return new int[0];

            var br = new BinaryReader(new MemoryStream(Data));
            if (Version == BioVersion.Biohazard1)
            {
                br.ReadBytes(12);
                br.ReadBytes(20 * 3);

                var offsets = new int[19];
                for (int i = 0; i < offsets.Length; i++)
                {
                    offsets[i] = br.ReadInt32();
                }
                return offsets;
            }
            else
            {
                br.ReadBytes(8);

                var offsets = new int[23];
                for (int i = 0; i < offsets.Length; i++)
                {
                    offsets[i] = br.ReadInt32();
                }
                return offsets;
            }
        }

        private void WriteOffsets()
        {
            var bw = new BinaryWriter(new MemoryStream(Data));
            if (Version == BioVersion.Biohazard1)
            {
                bw.BaseStream.Position = 12 + (20 * 3);
            }
            else
            {
                bw.BaseStream.Position = 8;
            }
            for (int i = 0; i < _offsets.Length; i++)
            {
                bw.Write(_offsets[i]);
            }
        }

        private int[] GetChunkLengths()
        {
            var lengths = new int[_offsets.Length];
            for (int i = 0; i < _offsets.Length; i++)
            {
                var start = _offsets[i];
                var end = Data.Length;
                for (int j = 0; j < _offsets.Length; j++)
                {
                    var o = _offsets[j];
                    if (o > start && o < end)
                    {
                        end = o;
                    }
                }
                lengths[i] = end - start;
            }
            return lengths;
        }

        private int GetNumEventScripts()
        {
            if (Version != BioVersion.Biohazard1)
                return 0;

            var chunkIndex = GetScdChunkIndex(BioScriptKind.Event);
            var chunkOffset = _offsets[chunkIndex];
            var endOffset = chunkOffset + _lengths[chunkIndex];
            var ms = new MemoryStream(Data);
            ms.Position = chunkOffset;

            var br = new BinaryReader(ms);
            var offset = br.ReadInt32();
            var numScripts = offset / 4;
            _eventScripts.Clear();
            for (int i = 0; i < numScripts; i++)
            {
                var nextOffset = i == numScripts - 1 ? endOffset : br.ReadInt32();
                if (nextOffset == 0)
                {
                    var length = endOffset - chunkOffset - offset;
                    _eventScripts.Add(new Range(offset, length));
                    break;
                }
                else
                {
                    var length = nextOffset - offset;
                    _eventScripts.Add(new Range(offset, length));
                }
                offset = nextOffset;
            }
            return numScripts;
        }

        public void ReadScript(BioScriptVisitor visitor)
        {
            visitor.VisitVersion(Version);
            ReadScript(BioScriptKind.Init, visitor);
            if (Version != BioVersion.Biohazard3)
                ReadScript(BioScriptKind.Main, visitor);
            if (Version == BioVersion.Biohazard1)
                ReadScript(BioScriptKind.Event, visitor);
        }

        private void ReadScript(BioScriptKind kind, BioScriptVisitor visitor)
        {
            var chunkIndex = GetScdChunkIndex(kind);
            var scriptOffset = _offsets[chunkIndex];
            if (scriptOffset == 0)
                return;

            if (kind == BioScriptKind.Event)
            {
                for (int i = 0; i < _eventScripts.Count; i++)
                {
                    Range eventScript = _eventScripts[i];
                    var eventScriptOffset = scriptOffset + eventScript.Start;
                    var eventScriptLength = eventScript.Length;
                    var scdReader = new ScdReader();
                    scdReader.BaseOffset = scriptOffset + eventScript.Start;
                    scdReader.ReadEventScript(new ReadOnlyMemory<byte>(Data, eventScriptOffset, eventScriptLength), visitor, i);
                }
            }
            else
            {
                var scriptLength = _lengths[chunkIndex];
                var scdReader = new ScdReader();
                scdReader.BaseOffset = scriptOffset;
                scdReader.ReadScript(new ReadOnlyMemory<byte>(Data, scriptOffset, scriptLength), Version, kind, visitor);
            }
        }

        private int MeasureScript(BioScriptKind kind)
        {
            var chunkIndex = GetScdChunkIndex(kind);
            var scriptOffset = _offsets[chunkIndex];
            var scriptLength = _lengths[chunkIndex];
            var span = new ReadOnlyMemory<byte>(Data, scriptOffset, scriptLength);
            var scdReader = new ScdReader();
            return scdReader.MeasureScript(span, Version, kind);
        }

        private void ReadEMRs()
        {
            var rbj = _offsets[22];
            if (rbj == 0)
                return;

            var ms = new MemoryStream(Data);
            var br = new BinaryReader(ms);

            ms.Position = rbj;
            var chunkLen = br.ReadInt32();
            var emrCount = br.ReadInt32();

            ms.Position = rbj + chunkLen;

            var offsets = new List<int>();
            for (int i = 0; i < emrCount * 2; i++)
            {
                offsets.Add(rbj + br.ReadInt32());
            }
            offsets.Add(rbj + chunkLen);

            for (int i = 0; i < emrCount; i++)
            {
                _emrs.Add(new Range(offsets[i * 2 + 0], offsets[i * 2 + 1] - offsets[i * 2 + 0]));
                _edds.Add(new Range(offsets[i * 2 + 1], offsets[i * 2 + 2] - offsets[i * 2 + 1]));
            }
        }

        internal int EmrCount => _emrs.Count;

        internal EmrFlags GetEmrFlags(int index)
        {
            var ms = GetStream();
            var br = new BinaryReader(ms);
            ms.Position = _emrs[index].Start;
            return (EmrFlags)br.ReadInt32();
        }

        internal void SetEmrFlags(int index, EmrFlags flags)
        {
            var ms = GetStream();
            var bw = new BinaryWriter(ms);
            ms.Position = _emrs[index].Start;
            bw.Write((int)flags);
        }

        internal void ScaleEmrYs(int index, double yRatio)
        {
            var ms = GetStream();
            var br = new BinaryReader(ms);
            var bw = new BinaryWriter(ms);

            var emr = _emrs[index];
            ms.Position = emr.Start;

            var flags = (EmrFlags)br.ReadUInt32();
            var pArmature = br.ReadUInt16();
            var pFrames = br.ReadUInt16();
            var nArmature = br.ReadUInt16();
            var frameLen = br.ReadUInt16();
            var totalFrames = (emr.Length - 12) / 80;
            for (int i = 0; i < totalFrames; i++)
            {
                var xOffset = br.ReadInt16();
                var yOffset = br.ReadInt16();
                if (yOffset != 0)
                {
                    ms.Position -= 2;
                    bw.Write((short)(yOffset * yRatio));
                }
                var zOffset = br.ReadInt16();
                var xSpeed = br.ReadInt16();
                var ySpeed = br.ReadInt16();
                var zSpeed = br.ReadInt16();

                // Rotations
                ms.Position += 68;
            }
        }

        internal int DuplicateEmr(int index)
        {
            var emr = _emrs[index];
            var edd = _edds[index];
            var emrData = Data.Skip(emr.Start).Take(emr.Length).ToArray();
            var eddData = Data.Skip(edd.Start).Take(edd.Length).ToArray();

            var rbjOffset = _offsets[22];
            var oldChunkLength = BitConverter.ToInt32(Data, rbjOffset);
            var chunkLength = oldChunkLength;
            _emrs.Add(new Range(rbjOffset + chunkLength, emrData.Length));
            chunkLength += emrData.Length;
            _edds.Add(new Range(rbjOffset + chunkLength, eddData.Length));
            chunkLength += eddData.Length;

            var rbjData = new List<byte>();
            rbjData.AddRange(BitConverter.GetBytes(chunkLength));
            rbjData.AddRange(BitConverter.GetBytes(_emrs.Count));
            rbjData.AddRange(Data.Skip(rbjOffset + 8).Take(oldChunkLength - 8));
            rbjData.AddRange(emrData);
            rbjData.AddRange(eddData);
            for (int i = 0; i < _emrs.Count; i++)
            {
                rbjData.AddRange(BitConverter.GetBytes(_emrs[i].Start - rbjOffset));
                rbjData.AddRange(BitConverter.GetBytes(_edds[i].Start - rbjOffset));
            }
            UpdateChunk(22, rbjData.ToArray());
            return EmrCount - 1;
        }

        internal void UpdateEmrFlags(int index, EmrFlags flags)
        {
            var emr = _emrs[index];
            var stream = GetStream();
            stream.Position = emr.Start;
            var bw = new BinaryWriter(stream);
            bw.Write((int)flags);
        }

        private void UpdateChunk(int index, byte[] data)
        {
            if (_lengths[index] == data.Length)
            {
                Array.Copy(data, 0, Data, _offsets[index], data.Length);
            }
            else
            {
                var start = _offsets[index];
                if (start == 0)
                {
                    if (index == 13 || index == 14)
                    {
                        if (EmbeddedModels.Length == 0)
                        {
                            start = _offsets[16];
                        }
                        else
                        {
                            start = EmbeddedModels[0].MD1;
                        }
                    }
                    else
                    {
                        throw new NotSupportedException("No existing chunk to replace");
                    }
                }

                var length = _lengths[index];
                var end = start + length;
                var lengthDelta = data.Length - length;
                var sliceA = Data.Take(start).ToArray();
                var sliceB = Data.Skip(end).ToArray();
                for (int i = 0; i < _offsets.Length; i++)
                {
                    if (_offsets[i] != 0 && _offsets[i] >= start)
                    {
                        _offsets[i] += lengthDelta;
                    }
                }
                _offsets[index] = start;
                _lengths[index] = data.Length;
                Data = sliceA.Concat(data).Concat(sliceB).ToArray();

                if (Version == BioVersion.Biohazard1)
                {
                    // Re-write ESP offsets
                    var ms = new MemoryStream(Data);
                    var br = new BinaryReader(ms);
                    var bw = new BinaryWriter(ms);
                    ms.Position = _offsets[14];
                    for (int i = 0; i < 8; i++)
                    {
                        var x = br.ReadInt32();
                        bw.BaseStream.Position -= 4;
                        if (x != -1)
                        {
                            var y = x + lengthDelta;
                            bw.Write(y);
                            bw.BaseStream.Position -= 4;
                        }
                        bw.BaseStream.Position -= 4;
                    }

                    // Re-write ESP offsets
                    ms.Position = Data.Length - 4;
                    for (int i = 0; i < 8; i++)
                    {
                        var x = br.ReadInt32();
                        bw.BaseStream.Position -= 4;
                        if (x != -1)
                        {
                            var y = x + lengthDelta;
                            bw.Write(y);
                            bw.BaseStream.Position -= 4;
                        }
                        bw.BaseStream.Position -= 4;
                    }
                }
                else
                {
                    // Re-write TIM offsets
                    var numEmbeddedTIMs = Data[2];
                    if (numEmbeddedTIMs != 0)
                    {
                        var ms = new MemoryStream(Data);
                        ms.Position = _offsets[10];
                        for (int i = 0; i < numEmbeddedTIMs; i++)
                        {
                            RewriteOffset(ms, start, lengthDelta);
                            RewriteOffset(ms, start, lengthDelta);
                        }
                    }
                }
                WriteOffsets();
            }
        }

        public string DisassembleScd(bool listing = false)
        {
            var scriptDecompiler = new ScriptDecompiler(true, listing);
            ReadScript(scriptDecompiler);
            return scriptDecompiler.GetScript();
        }

        public string DecompileScd()
        {
            var scriptDecompiler = new ScriptDecompiler(false, false);
            ReadScript(scriptDecompiler);
            return scriptDecompiler.GetScript();
        }

        public EmbeddedModel[] EmbeddedModels
        {
            get
            {
                var count = Data[2];
                var result = new EmbeddedModel[count];
                var br = new BinaryReader(new MemoryStream(Data));
                br.BaseStream.Position = _offsets[10];
                for (var i = 0; i < count; i++)
                {
                    var tim = br.ReadInt32();
                    var md1 = br.ReadInt32();
                    result[i] = new EmbeddedModel(tim, md1);
                }
                return result;
            }
        }

        public RdtAnimation[] Animations
        {
            get
            {
                var rbj = _offsets[22];
                if (rbj == 0)
                    return new RdtAnimation[0];

                var ms = new MemoryStream(Data);
                var br = new BinaryReader(ms);

                ms.Position = rbj;
                var chunkLen = br.ReadInt32();
                var emrCount = br.ReadInt32();

                ms.Position = rbj + chunkLen;

                var offsets = new List<int>();
                for (int i = 0; i < emrCount * 2; i++)
                {
                    offsets.Add(rbj + br.ReadInt32());
                }
                offsets.Add(rbj + chunkLen);

                var result = new List<RdtAnimation>();
                for (int i = 0; i < emrCount; i++)
                {
                    var flags = (EmrFlags)BitConverter.ToInt32(Data, offsets[i * 2 + 0]);
                    var emrRange = new Memory<byte>(Data, offsets[i * 2 + 0] + 4, offsets[i * 2 + 1] - offsets[i * 2 + 0] - 4);
                    var eddRange = new Memory<byte>(Data, offsets[i * 2 + 1], offsets[i * 2 + 2] - offsets[i * 2 + 1]);
                    var emr = new Emr(Version, emrRange);
                    var edd = new Edd(eddRange);
                    result.Add(new RdtAnimation(flags, edd, emr));
                }
                return result.ToArray();
            }
            set
            {
                var ms = new MemoryStream();
                var bw = new BinaryWriter(ms);
                bw.Write(0);
                bw.Write(value.Length);

                var offsets = new List<int>();
                foreach (var ani in value)
                {
                    offsets.Add((int)ms.Position);
                    bw.Write((int)ani.Flags);
                    bw.Write(ani.Emr.Data.ToArray());
                    offsets.Add((int)ms.Position);
                    bw.Write(ani.Edd.Data.ToArray());
                }

                var chunkLen = (int)ms.Length;
                foreach (var offset in offsets)
                {
                    bw.Write(offset);
                }
                ms.Position = 0;
                bw.Write(chunkLen);
                UpdateChunk(22, ms.ToArray());
            }
        }

        public readonly struct EmbeddedModel
        {
            public int TIM { get; }
            public int MD1 { get; }

            public EmbeddedModel(int tim, int md1)
            {
                TIM = tim;
                MD1 = md1;
            }
        }
    }

    [DebuggerDisplay("Start = {Start} Length = {Length}")]
    internal struct Range : IEquatable<Range>
    {
        public int Start { get; }
        public int Length { get; }
        public int End => Start + Length;

        public Range(int start, int length)
        {
            Start = start;
            Length = length;
        }

        public override bool Equals(object? obj)
        {
            return obj is Range range && Equals(range);
        }

        public bool Equals(Range other)
        {
            return Start == other.Start &&
                   Length == other.Length &&
                   End == other.End;
        }

        public override int GetHashCode()
        {
            int hashCode = -1042531914;
            hashCode = hashCode * -1521134295 + Start.GetHashCode();
            hashCode = hashCode * -1521134295 + Length.GetHashCode();
            hashCode = hashCode * -1521134295 + End.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(Range left, Range right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Range left, Range right)
        {
            return !(left == right);
        }
    }
}
