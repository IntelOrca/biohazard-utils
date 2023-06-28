using System;
using System.Runtime.InteropServices;

namespace IntelOrca.Biohazard.Extensions
{
    internal static class MemoryExtensions
    {
        public unsafe static ReadOnlySpan<T> GetSafeSpan<T>(this ReadOnlyMemory<byte> memory, int offset, int count) where T : struct
        {
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            var requiredLength = count * sizeof(T);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            ReadOnlySpan<byte> span;
            if (memory.Length <= offset)
            {
                span = new byte[requiredLength];
            }
            else if (memory.Length - offset < requiredLength)
            {
                var copyLength = memory.Length - offset;
                var fix = new byte[requiredLength];
                for (var i = 0; i < copyLength; i++)
                {
                    fix[i] = memory.Span[offset + i];
                }
                span = fix;
            }
            else
            {
                span = memory.Span.Slice(offset, requiredLength);
            }
            return MemoryMarshal.Cast<byte, T>(span).Slice(0, count);
        }
    }
}
