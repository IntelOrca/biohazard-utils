using System;

namespace IntelOrca.Biohazard.Room
{
    public interface IRdt
    {
        BioVersion Version { get; }
        ReadOnlyMemory<byte> Data { get; }
        EmbeddedEffectList EmbeddedEffects { get; }
        IRdtBuilder ToBuilder();
    }

    public interface IRdtBuilder
    {
        EmbeddedEffectList EmbeddedEffects { get; set; }

        IRdt ToRdt();
    }
}
