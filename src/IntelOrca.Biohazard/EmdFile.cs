using System;

namespace IntelOrca.Biohazard
{
    public class EmdFile : ModelFile
    {
        private readonly static ChunkKind[] g_chunkKinds2 = new[]
        {
            ChunkKind.Unknown,
            ChunkKind.Animation,
            ChunkKind.Armature,
            ChunkKind.Animation,
            ChunkKind.Armature,
            ChunkKind.Animation,
            ChunkKind.Armature,
            ChunkKind.Mesh,
        };

        private readonly static ChunkKind[] g_chunkKinds3 = new[]
        {
            ChunkKind.Unknown,
            ChunkKind.Unknown,
            ChunkKind.Animation,
            ChunkKind.Armature,
            ChunkKind.Animation,
            ChunkKind.Armature,
            ChunkKind.Animation,
            ChunkKind.Armature,
            ChunkKind.Unknown,
            ChunkKind.Unknown,
            ChunkKind.Unknown,
            ChunkKind.Unknown,
            ChunkKind.Unknown,
            ChunkKind.Mesh,
            ChunkKind.Mesh,
        };

        public EmdFile(BioVersion version, string path)
            : base(version, path)
        {
        }

        protected override ReadOnlySpan<ChunkKind> ChunkKinds =>
            Version == BioVersion.Biohazard2 ? g_chunkKinds2 : g_chunkKinds3;
    }
}
