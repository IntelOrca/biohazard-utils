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
    }
}
