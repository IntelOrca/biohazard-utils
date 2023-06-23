using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace IntelOrca.Biohazard
{
    public class Edd
    {
        public ReadOnlyMemory<byte> Data { get; }

        public Edd(ReadOnlyMemory<byte> data)
        {
            Data = data;
        }

        public int AnimationCount => Animations.Length;
        public ReadOnlySpan<Animation> Animations
        {
            get
            {
                var firstOffset = GetSpan<ushort>(2, 1)[0];
                var count = firstOffset / 4;
                return GetSpan<Animation>(0, count);
            }
        }
        public ReadOnlySpan<Frame> GetFrames(int animationIndex)
        {
            var animation = Animations[animationIndex];
            var offset = animation.Offset;
            return GetSpan<Frame>(offset, animation.Count);
        }

        private ReadOnlySpan<T> GetSpan<T>(int offset, int count) where T : struct
        {
            var data = Data.Span.Slice(offset);
            return MemoryMarshal.Cast<byte, T>(data).Slice(0, count);
        }

        [DebuggerDisplay("Count = {Count} Offset = {Offset}")]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Animation
        {
            public ushort Count { get; set; }
            public ushort Offset { get; set; }
        }

        [DebuggerDisplay("{Index} Flags = {Flags}")]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Frame
        {
            private uint _value;

            public ushort Index
            {
                get => (ushort)(_value & 0xFFF);
                set => _value = (uint)((_value & ~0xFFF) | ((uint)value & 0xFFF));
            }

            public byte Flags
            {
                get => (byte)(_value >> 12);
                set => _value = ((uint)value << 12) | (_value & 0xFFF);
            }
        }
    }
}
