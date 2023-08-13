using System;

namespace IntelOrca.Biohazard.Room
{
    public readonly struct Msg
    {
        public BioVersion Version { get; }
        public MsgLanguage Language { get; }
        public ReadOnlyMemory<byte> Data { get; }

        public Msg(BioVersion version, MsgLanguage language, ReadOnlyMemory<byte> data)
        {
            Version = version;
            Language = language;
            Data = data;
        }
    }
}
