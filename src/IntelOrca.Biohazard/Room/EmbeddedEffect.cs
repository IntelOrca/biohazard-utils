using System;

namespace IntelOrca.Biohazard.Room
{
    public readonly struct EmbeddedEffect
    {
        public byte Id { get; }
        public Eff Eff { get; }
        public Tim Tim { get; }

        public EmbeddedEffect(byte id, Eff eff, Tim tim)
        {
            Id = id;
            Eff = eff;
            Tim = tim;
        }
    }

    public readonly struct EmbeddedEffectList
    {
        public ReadOnlyMemory<EmbeddedEffect> Effects { get; }

        public EmbeddedEffectList(ReadOnlyMemory<EmbeddedEffect> effects)
        {
            Effects = effects;
        }

        public int Count => Effects.Length;

        public EmbeddedEffect this[int index] => Effects.Span[index];

        public ReadOnlySpan<byte> Ids
        {
            get
            {
                var effects = Effects.Span;
                var ids = new byte[8];
                for (var i = 0; i < ids.Length; i++)
                {
                    ids[i] = effects[i].Id;
                }
                return ids;
            }
        }
    }
}
