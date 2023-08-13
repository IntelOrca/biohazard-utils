using System;

namespace IntelOrca.Biohazard.Room
{
    public interface IRdt
    {
        ReadOnlyMemory<byte> Data { get; }
        IRdtBuilder ToBuilder();
    }

    public interface IRdtBuilder
    {
        IRdt ToRdt();
    }
}
