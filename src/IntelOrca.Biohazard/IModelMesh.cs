using System;

namespace IntelOrca.Biohazard
{
    public interface IModelMesh
    {
        BioVersion Version { get; }
        int NumParts { get; }
        ReadOnlyMemory<byte> Data { get; }
    }
}
