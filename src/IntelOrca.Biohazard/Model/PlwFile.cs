using System;

namespace IntelOrca.Biohazard.Model
{
    public class PlwFile : ModelFile
    {
        private readonly static ChunkKind[] g_chunkKinds1 = new[]
        {
            ChunkKind.Armature,
            ChunkKind.Animation,
            ChunkKind.Mesh
        };

        private readonly static ChunkKind[] g_chunkKinds2 = new[]
        {
            ChunkKind.Animation,
            ChunkKind.Armature,
            ChunkKind.Mesh,
            ChunkKind.Texture,
        };

        private readonly static ChunkKind[] g_chunkKinds3 = new[]
        {
            ChunkKind.Animation,
            ChunkKind.Armature,
            ChunkKind.Mesh,
            ChunkKind.Unknown,
            ChunkKind.Unknown,
            ChunkKind.Unknown,
            ChunkKind.Unknown,
            ChunkKind.Unknown,
            ChunkKind.Texture,
        };

        public PlwFile(BioVersion version, string path)
            : base(version, path)
        {
        }
        protected override ReadOnlySpan<ChunkKind> ChunkKinds =>
            Version switch
            {
                BioVersion.Biohazard1 => CalculateKindsRE1(),
                BioVersion.Biohazard2 => g_chunkKinds2,
                BioVersion.Biohazard3 => g_chunkKinds3,
                _ => throw new NotSupportedException()
            };

        private ChunkKind[] CalculateKindsRE1()
        {
            var result = new ChunkKind[NumChunks];
            result[NumChunks - 1] = ChunkKind.Mesh;
            return result;
        }

        public TimFile Tim
        {
            get => GetTim(0);
            set => SetTim(0, value);
        }
    }
}
