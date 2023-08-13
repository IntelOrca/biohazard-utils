using IntelOrca.Biohazard.Model;

namespace IntelOrca.Biohazard.Room
{
    public readonly struct RbjAnimation
    {
        public BioVersion Version => Emr.Version;
        public EmrFlags Flags { get; }
        public Edd Edd { get; }
        public Emr Emr { get; }

        public RbjAnimation(EmrFlags flags, Edd edd, Emr emr)
        {
            Flags = flags;
            Edd = edd;
            Emr = emr;
        }

        public RbjAnimation WithFlags(EmrFlags value) => new RbjAnimation(value, Edd, Emr);
        public RbjAnimation WithEdd(Edd value) => new RbjAnimation(Flags, value, Emr);
        public RbjAnimation WithEmr(Emr value) => new RbjAnimation(Flags, Edd, value);
    }
}
