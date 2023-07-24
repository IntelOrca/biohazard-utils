using IntelOrca.Biohazard.Model;

namespace IntelOrca.Biohazard
{
    public sealed class RdtAnimation
    {
        public EmrFlags Flags { get; }
        public Edd Edd { get; }
        public Emr Emr { get; }

        public RdtAnimation(EmrFlags flags, Edd edd, Emr emr)
        {
            Flags = flags;
            Edd = edd;
            Emr = emr;
        }

        public RdtAnimation WithFlags(EmrFlags value) => new RdtAnimation(value, Edd, Emr);
        public RdtAnimation WithEdd(Edd value) => new RdtAnimation(Flags, value, Emr);
        public RdtAnimation WithEmr(Emr value) => new RdtAnimation(Flags, Edd, value);
    }
}
