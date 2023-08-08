using System;
using System.Runtime.InteropServices;

namespace IntelOrca.Biohazard.Room
{
    public readonly struct EmbeddedModelTable
    {
        public ReadOnlyMemory<byte> Data { get; }

        public EmbeddedModelTable(ReadOnlyMemory<byte> data)
        {
            Data = data;
        }

        public int Count => Data.Length / 8;
        public ReadOnlySpan<ModelTextureOffset> Offsets => MemoryMarshal.Cast<byte, ModelTextureOffset>(Data.Span);
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct ModelTextureOffset
    {
        private readonly int _model;
        private readonly int _texture;

        public int Model => _model;
        public int Texture => _texture;

        public ModelTextureOffset(int model, int texture)
        {
            _model = model;
            _texture = texture;
        }
    }

    /// <summary>
    /// Used for RDT builders so that they can refer to models and textures by index.
    /// </summary>
    public readonly struct ModelTextureIndex
    {
        public int Model { get; }
        public int Texture { get; }

        public ModelTextureIndex(int model, int texture)
        {
            Model = model;
            Texture = texture;
        }
    }
}
