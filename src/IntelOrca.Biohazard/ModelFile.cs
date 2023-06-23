using System;
using System.IO;
using System.Linq;

namespace IntelOrca.Biohazard
{
    public abstract class ModelFile
    {
        private readonly byte[][] _chunks;

        public BioVersion Version { get; }
        public int NumChunks => _chunks.Length;

        public ModelFile(BioVersion version, string path)
        {
            Version = version;
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var br = new BinaryReader(fs);

                // Read header
                var directoryOffset = br.ReadInt32();
                var numOffsets = br.ReadInt32();

                // Read directory
                fs.Position = directoryOffset;
                var offsets = new int[numOffsets + 1];
                for (int i = 0; i < numOffsets; i++)
                {
                    offsets[i] = br.ReadInt32();
                }
                offsets[numOffsets] = directoryOffset;

                // Check all offsets are in order
                var lastOffset = 0;
                foreach (var offset in offsets)
                {
                    if (offset < lastOffset)
                        throw new NotSupportedException("Offsets not in order");
                    lastOffset = offset;
                }

                // Read chunks
                _chunks = new byte[numOffsets][];
                for (int i = 0; i < numOffsets; i++)
                {
                    var len = offsets[i + 1] - offsets[i];
                    fs.Position = offsets[i];
                    _chunks[i] = br.ReadBytes(len);
                }
            }
        }

        public void Save(string path)
        {
            var chunkSum = _chunks.Sum(x => x.Length);
            using (var fs = new FileStream(path, FileMode.Create))
            {
                var bw = new BinaryWriter(fs);
                bw.Write(8 + chunkSum);
                bw.Write(_chunks.Length);
                for (int i = 0; i < _chunks.Length; i++)
                {
                    bw.Write(_chunks[i]);
                }

                var offset = 8;
                for (int i = 0; i < _chunks.Length; i++)
                {
                    bw.Write(offset);
                    offset += _chunks[i].Length;
                }
            }
        }

        protected virtual ReadOnlySpan<ChunkKind> ChunkKinds => new ReadOnlySpan<ChunkKind>();

        public ChunkKind GetChunkKind(int index)
        {
            var kinds = ChunkKinds;
            if (index < 0 || index >= kinds.Length)
                return ChunkKind.Unknown;
            return kinds[index];
        }

        public object GetChunk(int index)
        {
            var kind = GetChunkKind(index);
            switch (kind)
            {
                case ChunkKind.Animation:
                    return new Edd(GetChunkData(index));
                case ChunkKind.Armature:
                    return new Emr(GetChunkData(index));
                case ChunkKind.Mesh:
                    if (Version == BioVersion.Biohazard2)
                        return new Md1(GetChunkData(index));
                    else
                        return new Md2(GetChunkData(index));
                case ChunkKind.Texture:
                    return new TimFile(GetChunkData(index));
                default:
                    return null;
            }
        }

        protected virtual ReadOnlyMemory<byte> GetChunkData(int index) => _chunks[index];
        protected void SetChunkData(int index, ReadOnlySpan<byte> data) => _chunks[index] = data.ToArray();

        private int GetChunkIndex(ChunkKind kind, int number)
        {
            for (var i = 0; i < _chunks.Length; i++)
            {
                var chunkKind = GetChunkKind(i);
                if (chunkKind == kind)
                {
                    if (number == 0)
                        return i;
                    number--;
                }
            }
            throw new ArgumentException("Chunk does not exist");
        }

        public ReadOnlyMemory<byte> GetChunk(ChunkKind kind, int number)
        {
            var chunkIndex = GetChunkIndex(kind, number);
            return GetChunkData(chunkIndex);
        }

        public void SetChunk(ChunkKind kind, int number, ReadOnlyMemory<byte> data) => SetChunk(kind, number, data.Span);

        public void SetChunk(ChunkKind kind, int number, ReadOnlySpan<byte> data)
        {
            var chunkIndex = GetChunkIndex(kind, number);
            SetChunkData(chunkIndex, data);
        }

        public Edd GetEdd(int number) => new Edd(GetChunk(ChunkKind.Animation, number));
        public void SetEdd(int number, Edd value) => SetChunk(ChunkKind.Animation, number, value.Data);

        public Emr GetEmr(int number) => new Emr(GetChunk(ChunkKind.Armature, number));
        public void SetEmr(int number, Emr value) => SetChunk(ChunkKind.Armature, number, value.Data);

        public IModelMesh GetMesh(int number) =>
            Version == BioVersion.Biohazard2 ?
                (IModelMesh)new Md1(GetChunk(ChunkKind.Mesh, number)) :
                (IModelMesh)new Md2(GetChunk(ChunkKind.Mesh, number));
        public void SetMesh(int number, IModelMesh value) => SetChunk(ChunkKind.Mesh, number, value.Data);

        public TimFile GetTim(int number) => new TimFile(GetChunk(ChunkKind.Texture, number));
        public void SetTim(int number, TimFile value) => SetChunk(ChunkKind.Texture, number, new ReadOnlySpan<byte>(value.GetBytes()));

        public Md1 Md1
        {
            get
            {
                if (Version != BioVersion.Biohazard2)
                    throw new InvalidOperationException();
                return new Md1(GetChunk(ChunkKind.Mesh, 0));
            }
            set
            {
                if (Version != BioVersion.Biohazard2)
                    throw new InvalidOperationException();
                SetChunk(ChunkKind.Mesh, 0, value.Data);
            }
        }

        public Md2 Md2
        {
            get
            {
                if (Version != BioVersion.Biohazard3)
                    throw new InvalidOperationException();
                return new Md2(GetChunk(ChunkKind.Mesh, 0));
            }
            set
            {
                if (Version != BioVersion.Biohazard3)
                    throw new InvalidOperationException();
                SetChunk(ChunkKind.Mesh, 0, value.Data);
            }
        }

        public static ModelFile? FromFile(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var br = new BinaryReader(fs);
                var directoryOffset = br.ReadInt32();
                var numOffsets = br.ReadInt32();
                fs.Close();
                switch (numOffsets)
                {
                    case 4:
                        if (path.EndsWith(".plw", StringComparison.OrdinalIgnoreCase))
                            return new PlwFile(BioVersion.Biohazard2, path);
                        return new PldFile(BioVersion.Biohazard2, path);
                    case 5:
                        return new PldFile(BioVersion.Biohazard3, path);
                    case 8:
                        return new EmdFile(BioVersion.Biohazard2, path);
                    case 9:
                        return new PlwFile(BioVersion.Biohazard3, path);
                    case 15:
                        return new EmdFile(BioVersion.Biohazard3, path);
                    default:
                        throw new InvalidDataException("Unsupported file type");
                }
            }
        }

        public enum ChunkKind
        {
            Unknown,
            Animation,
            Armature,
            Dat,
            Mesh,
            Texture,
        }
    }
}
