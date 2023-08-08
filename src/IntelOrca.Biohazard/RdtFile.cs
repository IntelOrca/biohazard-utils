using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using IntelOrca.Biohazard.Extensions;
using IntelOrca.Biohazard.Model;
using IntelOrca.Biohazard.Room;
using IntelOrca.Biohazard.Script;

namespace IntelOrca.Biohazard
{
    public class RdtFile
    {
        private readonly RdtFileData _data;

        public BioVersion Version { get; }
        public byte[] Data => _data.Data.ToArray();
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
            _data = new RdtFileData(data);
            Version = version ?? DetectVersion(data);
            ReadHeader();
            ReadAdditionalOffsets();
            CalculateChunkLengths();
            GetEventScripts();
            Checksum = Data.CalculateFnv1a();
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

        public int EventScriptCount => GetEventScripts().Length;

        private int MeasureTexts(int language)
        {
            var chunkKind = language == 0 ? RdtFileChunkKinds.MessagesJpn : RdtFileChunkKinds.MessagesEng;
            var chunk = _data.FindChunkByKind(chunkKind);
            if (chunk == null)
                return 0;

            var br = new BinaryReader(chunk.Value.Stream);
            var firstOffset = br.ReadUInt16();
            var numTexts = firstOffset / 2;
            br.BaseStream.Position = ((numTexts - 1) * 2);
            var lastTextOffset = br.ReadUInt16();
            br.BaseStream.Position = lastTextOffset;
            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                var b = br.ReadByte();
                if (b == 0xFE)
                {
                    b = br.ReadByte();
                    break;
                }
            }
            var len = (int)(br.BaseStream.Position);
            return ((len + 3) / 4) * 4;
        }

        public BioString[] GetTexts(int language)
        {
            var chunkKind = language == 0 ? RdtFileChunkKinds.MessagesJpn : RdtFileChunkKinds.MessagesEng;
            var chunk = _data.FindChunkByKind(chunkKind);
            if (chunk == null)
            {
                return new BioString[0];
            }

            var br = new BinaryReader(chunk.Value.Stream);
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
            textLengths[numTexts - 1] = (ushort)(chunk.Value.Length - textOffsets[numTexts - 1]);

            var result = new List<BioString>();
            for (int i = 0; i < numTexts; i++)
            {
                br.BaseStream.Position = textOffsets[i];
                var text = br.ReadBytes(textLengths[i]);
                result.Add(new BioString(text));
            }
            return result.ToArray();
        }

        public void SetTexts(int language, BioString[] texts)
        {
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

            var chunkKind = language == 0 ? RdtFileChunkKinds.MessagesJpn : RdtFileChunkKinds.MessagesEng;
            var chunk = _data.FindChunkByKind(chunkKind);
            if (chunk == null)
            {
                var beforeChunk = _data.Chunks
                    .FirstOrDefault(x => x.Kind == RdtFileChunkKinds.EmbeddedObjectMd1);
                if (beforeChunk.Parent == null)
                {
                    beforeChunk = _data.FindChunkByKind(RdtFileChunkKinds.ScdInit)!.Value;
                }
                _data.InsertData(chunkKind, beforeChunk.Offset, ms.ToArray());
            }
            else
            {
                _data.SetData(chunkKind, chunk.Value.Offset, ms.ToArray());
            }
            UpdateOffsets();
        }

        public byte[] GetScd(BioScriptKind kind, int scriptIndex = 0)
        {
            var chunk = GetScdChunk(kind)!.Value;
            if (kind == BioScriptKind.Event)
            {
                var eventScripts = GetEventScripts();
                return eventScripts[scriptIndex].Data.ToArray();
            }
            else
            {
                return chunk.Span.ToArray();
            }
        }

        public void SetScd(BioScriptKind kind, byte[] data)
        {
            var chunk = GetScdChunk(kind)!.Value;
            _data.SetData(chunk.Kind, chunk.Offset, data);
            UpdateOffsets();
        }

        private RdtFileData.Chunk? GetScdChunk(BioScriptKind kind)
        {
            var chunkKind = kind switch
            {
                BioScriptKind.Init => RdtFileChunkKinds.ScdInit,
                BioScriptKind.Main => RdtFileChunkKinds.ScdMain,
                BioScriptKind.Event => RdtFileChunkKinds.ScdEvent,
                _ => throw new ArgumentException("Invalid kind", nameof(kind))
            };
            return _data.FindChunkByKind(chunkKind);
        }

        private void ReadHeader()
        {
            if (_data.Length <= 8)
            {
                _data.RegisterOffset(RdtFileChunkKinds.Header, 0);
                return;
            }

            var offsetMap = GetHeaderOffsetMap();
            BinaryReader br;
            if (Version == BioVersion.Biohazard1)
            {
                var chunk = _data.RegisterChunk(RdtFileChunkKinds.Header, 0, 148);
                br = new BinaryReader(chunk.Stream);
                br.ReadBytes(12);
                br.ReadBytes(20 * 3);
            }
            else
            {
                var chunk = _data.RegisterChunk(RdtFileChunkKinds.Header, 0, 100);
                br = new BinaryReader(chunk.Stream);
                br.ReadBytes(8);
            }
            for (int i = 0; i < offsetMap.Length; i++)
            {
                var offset = br.ReadInt32();
                if (offset != 0)
                {
                    var kind = offsetMap[i];
                    var existingChunk = _data.FindChunkByOffset(offset);
                    if (existingChunk == null || existingChunk.Value.Kind == RdtFileChunkKinds.Unknown)
                    {
                        _data.RegisterOffset(kind, offset, overwrite: true);
                    }
                    else
                    {
                        _data.InsertData(kind, offset, new byte[0]);
                    }
                }
            }
        }

        private void ReadAdditionalOffsets()
        {
            if (Version == BioVersion.Biohazard1)
            {

            }
            else
            {
                var chunk = _data.FindChunkByKind(RdtFileChunkKinds.EmbeddedObjectTable);
                if (chunk != null)
                {
                    var count = _data.Data.Span[2];
                    var br = new BinaryReader(chunk.Value.Stream);
                    var timOffsets = new HashSet<int>();
                    var md1Offsets = new HashSet<int>();
                    for (var i = 0; i < count; i++)
                    {
                        var tim = br.ReadInt32();
                        if (tim != 0 && timOffsets.Add(tim))
                        {
                            _data.RegisterOffset(RdtFileChunkKinds.EmbeddedObjectTim, tim);
                        }

                        var md1 = br.ReadInt32();
                        if (md1 != 0 && md1Offsets.Add(md1))
                        {
                            _data.RegisterOffset(RdtFileChunkKinds.EmbeddedObjectMd1, md1);
                        }
                    }
                }
            }
        }

        private void CalculateChunkLengths()
        {
            if (Version != BioVersion.Biohazard1)
            {
                // We need to do AST analysis on SCD to find where the end is
                var initChunk = _data.FindChunkByKind(RdtFileChunkKinds.ScdInit);
                if (initChunk != null)
                {
                    _data.RegisterLength(initChunk.Value.Offset, MeasureScript(BioScriptKind.Init));
                }
            }
            if (Version == BioVersion.Biohazard2)
            {
                var mainChunk = _data.FindChunkByKind(RdtFileChunkKinds.ScdMain);
                if (mainChunk != null)
                {
                    _data.RegisterLength(mainChunk.Value.Offset, MeasureScript(BioScriptKind.Main));
                }

                var jpnChunk = _data.FindChunkByKind(RdtFileChunkKinds.MessagesJpn);
                if (jpnChunk != null)
                {
                    _data.RegisterLength(jpnChunk.Value.Offset, MeasureTexts(0));
                }

                var engChunk = _data.FindChunkByKind(RdtFileChunkKinds.MessagesEng);
                if (engChunk != null)
                {
                    _data.RegisterLength(engChunk.Value.Offset, MeasureTexts(1));
                }
            }
        }

        private void UpdateOffsets()
        {
            if (_data.Length <= 8)
                return;

            UpdateHeader();

            if (Version == BioVersion.Biohazard1)
            {
            }
            else
            {
                var timChunks = _data.Chunks
                    .Where(x => x.Kind == RdtFileChunkKinds.EmbeddedObjectTim)
                    .Select(x => x.Offset)
                    .ToArray();
                var md1Chunks = _data.Chunks
                    .Where(x => x.Kind == RdtFileChunkKinds.EmbeddedObjectMd1)
                    .Select(x => x.Offset)
                    .ToArray();

                var chunk = _data.FindChunkByKind(RdtFileChunkKinds.EmbeddedObjectTable);
                if (chunk != null)
                {
                    var ms = new MemoryStream(chunk.Value.Span.ToArray());
                    var br = new BinaryReader(ms);
                    var bw = new BinaryWriter(ms);
                    var count = NumEmbeddedModels;
                    var offsetsTim = new int[count];
                    var offsetsMd1 = new int[count];
                    for (var i = 0; i < count; i++)
                    {
                        offsetsTim[i] = br.ReadInt32();
                        offsetsMd1[i] = br.ReadInt32();
                    }
                    var indicesTim = offsetsTim.OrderBy(x => x).Where(x => x != 0).Distinct().ToArray();
                    var indicesMd1 = offsetsMd1.OrderBy(x => x).Where(x => x != 0).Distinct().ToArray();
                    var orderTim = offsetsTim.Select(x => x == 0 ? -1 : Array.IndexOf(indicesTim, x)).ToArray();
                    var orderMd1 = offsetsMd1.Select(x => x == 0 ? -1 : Array.IndexOf(indicesMd1, x)).ToArray();
                    var newIndicesTim = _data.Chunks.Where(x => x.Kind == RdtFileChunkKinds.EmbeddedObjectTim).Select(x => x.Offset).ToArray();
                    var newIndicesMd1 = _data.Chunks.Where(x => x.Kind == RdtFileChunkKinds.EmbeddedObjectMd1).Select(x => x.Offset).ToArray();
                    var newOffsetsTim = orderTim.Select(x => x == -1 ? 0 : newIndicesTim[x]).ToArray();
                    var newOffsetsMd1 = orderMd1.Select(x => x == -1 ? 0 : newIndicesMd1[x]).ToArray();

                    ms.Position = 0;
                    for (var i = 0; i < count; i++)
                    {
                        bw.Write(newOffsetsTim[i]);
                        bw.Write(newOffsetsMd1[i]);
                    }
                    _data.SetData(chunk.Value.Kind, chunk.Value.Offset, ms.ToArray());
                }
            }
        }

        private int NumEmbeddedModels
        {
            get => _data.Data.Span[2];
            // set => _data.Data[2] = (byte)value;
            set => throw new NotImplementedException();
        }

        private void UpdateHeader()
        {
            var offsetMap = GetHeaderOffsetMap();
            var bytes = _data[0].Span.ToArray();
            var ms = new MemoryStream(bytes);
            var br = new BinaryReader(ms);
            var bw = new BinaryWriter(ms);
            if (Version == BioVersion.Biohazard1)
            {
                ms.Position += 12;
                ms.Position += 20 * 3;
            }
            else
            {
                ms.Position += 8;
            }
            for (int i = 0; i < offsetMap.Length; i++)
            {
                var original = br.ReadInt32();
                ms.Position -= 4;

                var kind = offsetMap[i];
                var chunk = _data.FindChunkByKind(kind);
                if (chunk == null)
                {
                    bw.Write(0);
                }
                else
                {
                    bw.Write(chunk.Value.Offset);
                }
            }
            _data.SetData(RdtFileChunkKinds.Header, 0, ms.ToArray());
        }

        private EventScript[] GetEventScripts()
        {
            var eventScripts = new List<EventScript>();
            if (Version != BioVersion.Biohazard1)
                return eventScripts.ToArray();

            var chunk = GetScdChunk(BioScriptKind.Event)!.Value;

            var ms = chunk.Stream;
            var br = new BinaryReader(ms);
            var offset = br.ReadInt32();
            var numScripts = offset / 4;
            for (int i = 0; i < numScripts; i++)
            {
                var nextOffset = i == numScripts - 1 ? chunk.Length : br.ReadInt32();
                if (nextOffset == 0)
                {
                    var length = chunk.Length - offset;
                    eventScripts.Add(new EventScript(chunk.Offset + offset, chunk.Memory.Slice(offset, length)));
                    break;
                }
                else
                {
                    var length = nextOffset - offset;
                    eventScripts.Add(new EventScript(chunk.Offset + offset, chunk.Memory.Slice(offset, length)));
                }
                offset = nextOffset;
            }
            return eventScripts.ToArray();
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
            var chunk = GetScdChunk(kind);
            if (chunk == null)
                return;

            if (kind == BioScriptKind.Event)
            {
                var eventScripts = GetEventScripts();
                for (int i = 0; i < eventScripts.Length; i++)
                {
                    var eventScript = eventScripts[i];
                    var scdReader = new ScdReader();
                    scdReader.BaseOffset = eventScript.BaseOffset;
                    scdReader.ReadEventScript(eventScript.Data, visitor, i);
                }
            }
            else
            {
                var scdReader = new ScdReader();
                scdReader.BaseOffset = chunk.Value.Offset;
                scdReader.ReadScript(chunk.Value.Memory, Version, kind, visitor);
            }
        }

        private int MeasureScript(BioScriptKind kind)
        {
            var chunk = GetScdChunk(kind)!.Value;
            var scdReader = new ScdReader();
            return scdReader.MeasureScript(chunk.Memory, Version, kind);
        }

        internal int EmrCount => Animations.Length;

        internal EmrFlags GetEmrFlags(int index) => Animations[index].Flags;

        internal void SetEmrFlags(int index, EmrFlags flags)
        {
            var animations = Animations;
            animations[index] = animations[index].WithFlags(flags);
            Animations = animations;
        }

        internal void ScaleEmrYs(int index, double yRatio)
        {
            var animations = Animations;
            var animation = animations[index];
            var emr = animation.Emr.Scale(yRatio);
            animations[index] = animation.WithEmr(emr);
            Animations = animations;
        }

        internal int DuplicateEmr(int index)
        {
            var animations = Animations.ToList();
            animations.Add(animations[index]);
            Animations = animations.ToArray();
            return animations.Count - 1;
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

        public RdtModel[] Models
        {
            get
            {
                var result = new List<RdtModel>();
                var numEmbeddedModels = NumEmbeddedModels;
                var table = _data.FindChunkByKind(RdtFileChunkKinds.EmbeddedObjectTable);
                if (table != null)
                {
                    var br = new BinaryReader(table.Value.Stream);
                    for (var i = 0; i < numEmbeddedModels; i++)
                    {
                        var timAddress = br.ReadInt32();
                        var md1Address = br.ReadInt32();
                        var timChunk = _data.Chunks
                            .Where(x => x.Kind == RdtFileChunkKinds.EmbeddedObjectTim)
                            .FirstOrDefault(x => x.Offset == timAddress);
                        var md1Chunk = _data.Chunks
                            .Where(x => x.Kind == RdtFileChunkKinds.EmbeddedObjectMd1)
                            .FirstOrDefault(x => x.Offset == md1Address);
                        var tim = new TimFile(timChunk.Stream);
                        var md1 = new Md1(md1Chunk.Memory);
                        result.Add(new RdtModel(md1, tim));
                    }
                }
                return result.ToArray();
            }
            set
            {
                var firstMd1 = _data.Chunks.FirstOrDefault(x => x.Kind == RdtFileChunkKinds.EmbeddedObjectMd1).Offset;
                var firstTim = _data.Chunks.FirstOrDefault(x => x.Kind == RdtFileChunkKinds.EmbeddedObjectTim).Offset;
                while (_data.Chunks.Any(x => x.Kind == RdtFileChunkKinds.EmbeddedObjectMd1))
                {
                    _data.Remove(_data.Chunks.FirstOrDefault(x => x.Kind == RdtFileChunkKinds.EmbeddedObjectMd1));
                }
                while (_data.Chunks.Any(x => x.Kind == RdtFileChunkKinds.EmbeddedObjectTim))
                {
                    _data.Remove(_data.Chunks.FirstOrDefault(x => x.Kind == RdtFileChunkKinds.EmbeddedObjectTim));
                }

                var newMd1Files = value.Select(x => x.Mesh.Data.ToArray()).ToArray();
                var newTimFiles = value.Select(x => x.Texture.GetBytes()).ToArray();
                var hashedMd1Files = newMd1Files.Select(x => x.CalculateFnv1a()).ToArray();
                var hashedTimFiles = newTimFiles.Select(x => x.CalculateFnv1a()).ToArray();
                var uniqMd1Files = hashedMd1Files.Distinct().ToArray();
                var uniqTimFiles = hashedTimFiles.Distinct().ToArray();
                var md1Indices = newMd1Files.Select(x => Array.IndexOf(uniqMd1Files, x.CalculateFnv1a())).ToArray();
                var timIndices = newTimFiles.Select(x => Array.IndexOf(uniqTimFiles, x.CalculateFnv1a())).ToArray();
                newTimFiles = uniqTimFiles.Select(x => newTimFiles.FirstOrDefault(y => x == y.CalculateFnv1a())).ToArray();
                newMd1Files = uniqMd1Files.Select(x => newMd1Files.FirstOrDefault(y => x == y.CalculateFnv1a())).ToArray();

                foreach (var md1 in newMd1Files.Reverse())
                {
                    _data.InsertData(RdtFileChunkKinds.EmbeddedObjectMd1, firstMd1, md1);
                }
                foreach (var tim in newTimFiles.Reverse())
                {
                    _data.InsertData(RdtFileChunkKinds.EmbeddedObjectTim, firstTim, tim);
                }

                NumEmbeddedModels = value.Length;
                var table = _data.FindChunkByKind(RdtFileChunkKinds.EmbeddedObjectTable);
                if (table != null)
                {
                    var bw = new BinaryWriter(new MemoryStream());
                    for (var i = 0; i < value.Length; i++)
                    {
                        var timIndex = timIndices[i];
                        var md1Index = md1Indices[i];
                        var timOffset = _data.Chunks.Where(x => x.Kind == RdtFileChunkKinds.EmbeddedObjectTim).Skip(timIndex).First().Offset;
                        var md1Offset = _data.Chunks.Where(x => x.Kind == RdtFileChunkKinds.EmbeddedObjectMd1).Skip(md1Index).First().Offset;
                        bw.Write(timOffset);
                        bw.Write(md1Offset);
                    }
                }
            }
        }

        public RdtAnimation[] Animations
        {
            get
            {
                var rbj = _data.FindChunkByKind(RdtFileChunkKinds.RoomAnimations);
                if (rbj == null)
                    return new RdtAnimation[0];

                var memory = rbj.Value.Memory;
                var ms = rbj.Value.Stream;
                var br = new BinaryReader(ms);
                var chunkLen = br.ReadInt32();
                var emrCount = br.ReadInt32();

                ms.Position = chunkLen;

                var offsets = new List<int>();
                for (int i = 0; i < emrCount * 2; i++)
                {
                    offsets.Add(br.ReadInt32());
                }
                offsets.Add(chunkLen);

                var result = new List<RdtAnimation>();
                for (int i = 0; i < emrCount; i++)
                {
                    var flagsEmrRange = memory.Slice(offsets[i * 2 + 0], offsets[i * 2 + 1] - offsets[i * 2 + 0]);
                    var emrRange = flagsEmrRange.Slice(4);
                    var eddRange = memory.Slice(offsets[i * 2 + 1], offsets[i * 2 + 2] - offsets[i * 2 + 1]);
                    var flags = MemoryMarshal.Cast<byte, EmrFlags>(flagsEmrRange.Span)[0];
                    var emr = new Emr(Version, emrRange);
                    var edd = new Edd(eddRange);
                    result.Add(new RdtAnimation(flags, edd, emr));
                }
                return result.ToArray();
            }
            set
            {
                var rbj = _data.FindChunkByKind(RdtFileChunkKinds.RoomAnimations);
                if (rbj == null)
                    throw new NotImplementedException();

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
                _data.SetData(rbj.Value.Kind, rbj.Value.Offset, ms.ToArray());
                UpdateOffsets();
            }
        }

        private int[] GetHeaderOffsetMap()
        {
            return Version switch
            {
                BioVersion.Biohazard1 => _re1HeaderOffsetKinds,
                BioVersion.Biohazard2 => _re2HeaderOffsetKinds,
                BioVersion.Biohazard3 => _re3HeaderOffsetKinds,
                _ => throw new NotImplementedException()
            };
        }

        private static readonly int[] _re1HeaderOffsetKinds = new[]
        {
            RdtFileChunkKinds.SoundAttributeTable,
        };

        private static readonly int[] _re2HeaderOffsetKinds = new[]
        {
            RdtFileChunkKinds.SoundAttributeTable,
            RdtFileChunkKinds.EmbeddedVH,
            RdtFileChunkKinds.EmbeddedVB,
            RdtFileChunkKinds.EmbeddedTrialVH,
            RdtFileChunkKinds.EmbeddedTrialVB,
            RdtFileChunkKinds.Ota,
            RdtFileChunkKinds.Collisions,
            RdtFileChunkKinds.CameraPositions,
            RdtFileChunkKinds.CameraSwitches,
            RdtFileChunkKinds.Lights,
            RdtFileChunkKinds.EmbeddedObjectTable,
            RdtFileChunkKinds.FloorAreas,
            RdtFileChunkKinds.BlockUnknown,
            RdtFileChunkKinds.MessagesJpn,
            RdtFileChunkKinds.MessagesEng,
            RdtFileChunkKinds.ScrollingTim,
            RdtFileChunkKinds.ScdInit,
            RdtFileChunkKinds.ScdMain,
            RdtFileChunkKinds.Effects,
            RdtFileChunkKinds.EffectTable,
            RdtFileChunkKinds.EffectSprites,
            RdtFileChunkKinds.ObjectTextures,
            RdtFileChunkKinds.RoomAnimations,
        };

        private static readonly int[] _re3HeaderOffsetKinds = new[]
        {
            RdtFileChunkKinds.SoundAttributeTable,
            RdtFileChunkKinds.EmbeddedVH,
            RdtFileChunkKinds.EmbeddedVB,
            RdtFileChunkKinds.EmbeddedTrialVH,
            RdtFileChunkKinds.EmbeddedTrialVB,
            RdtFileChunkKinds.Ota,
            RdtFileChunkKinds.Collisions,
            RdtFileChunkKinds.CameraPositions,
            RdtFileChunkKinds.CameraSwitches,
            RdtFileChunkKinds.Lights,
            RdtFileChunkKinds.EmbeddedObjectTable,
            RdtFileChunkKinds.FloorAreas,
            RdtFileChunkKinds.BlockUnknown,
            RdtFileChunkKinds.MessagesJpn,
            RdtFileChunkKinds.MessagesEng,
            RdtFileChunkKinds.ScrollingTim,
            RdtFileChunkKinds.ScdInit,
            RdtFileChunkKinds.ScdMain,
            RdtFileChunkKinds.Effects,
            RdtFileChunkKinds.EffectTable,
            RdtFileChunkKinds.EffectSprites,
            RdtFileChunkKinds.ObjectTextures,
            RdtFileChunkKinds.RoomAnimations,
        };

        private readonly struct EventScript
        {
            public int BaseOffset { get; }
            public ReadOnlyMemory<byte> Data { get; }

            public EventScript(int baseOffset, ReadOnlyMemory<byte> data)
            {
                BaseOffset = baseOffset;
                Data = data;
            }
        }
    }
}
