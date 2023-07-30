using System.Linq;

namespace IntelOrca.Biohazard.Script.Compilation
{
    public interface IRdtEditOperation
    {
        void Perform(RdtFile target);
    }

    public class ScdRdtEditOperation : IRdtEditOperation
    {
        public BioScriptKind Kind { get; }
        public byte[] Data { get; }

        public ScdRdtEditOperation(BioScriptKind kind, byte[] data)
        {
            Kind = kind;
            Data = data;
        }

        public void Perform(RdtFile target)
        {
            target.SetScd(Kind, Data);
        }
    }

    public class TextRdtEditOperation : IRdtEditOperation
    {
        public int Language { get; }
        public int Index { get; }
        public BioString Value { get; }

        public TextRdtEditOperation(int language, int index, BioString value)
        {
            Language = language;
            Index = index;
            Value = value;
        }

        public void Perform(RdtFile target)
        {
            var texts = target.GetTexts(Language).ToList();
            texts.SetElement(Index, Value);
            target.SetTexts(Language, texts.ToArray());
        }
    }

    public class AnimationRdtEditOperation : IRdtEditOperation
    {
        public int Index { get; }
        public RdtAnimation Animation { get; }

        public AnimationRdtEditOperation(int index, RdtAnimation animation)
        {
            Index = index;
            Animation = animation;
        }

        public void Perform(RdtFile target)
        {
            var animations = target.Animations.ToList();
            animations.SetElement(Index, Animation);
            target.Animations = animations.ToArray();
        }
    }
}
