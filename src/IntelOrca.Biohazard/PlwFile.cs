using System;

namespace IntelOrca.Biohazard
{
    public class PlwFile : ModelFile
    {
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
            Version == BioVersion.Biohazard2 ? g_chunkKinds2 : g_chunkKinds3;

        public TimFile Tim
        {
            get => GetTim(0);
            set => SetTim(0, value);
        }
    }
}
