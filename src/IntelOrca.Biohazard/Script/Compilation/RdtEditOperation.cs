using System;
using IntelOrca.Biohazard.Extensions;
using IntelOrca.Biohazard.Room;

namespace IntelOrca.Biohazard.Script.Compilation
{
    public interface IRdtEditOperation
    {
        void Perform(IRdtBuilder target);
    }

    public class ScdRdtEditOperation : IRdtEditOperation
    {
        public BioScriptKind Kind { get; }
        public ScdProcedureList Data { get; }

        public ScdRdtEditOperation(BioScriptKind kind, ScdProcedureList data)
        {
            Kind = kind;
            Data = data;
        }

        public void Perform(IRdtBuilder target)
        {
            if (target is Rdt2.Builder builder2)
            {
                if (Kind == BioScriptKind.Init)
                {
                    builder2.SCDINIT = Data;
                }
                else if (Kind == BioScriptKind.Main)
                {
                    builder2.SCDMAIN = Data;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }

    public class TextRdtEditOperation : IRdtEditOperation
    {
        public MsgLanguage Language { get; }
        public int Index { get; }
        public Msg Value { get; }

        public TextRdtEditOperation(MsgLanguage language, int index, Msg value)
        {
            Language = language;
            Index = index;
            Value = value;
        }

        public void Perform(IRdtBuilder target)
        {
            if (target is Rdt2.Builder builder2)
            {
                if (Language == MsgLanguage.Japanese)
                {
                    builder2.MSGJA = builder2.MSGJA.WithMessage(Index, Value);
                }
                else if (Language == MsgLanguage.English)
                {
                    builder2.MSGEN = builder2.MSGEN.WithMessage(Index, Value);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }

    public class AnimationRdtEditOperation : IRdtEditOperation
    {
        public int Index { get; }
        public RbjAnimation Animation { get; }

        public AnimationRdtEditOperation(int index, in RbjAnimation animation)
        {
            Index = index;
            Animation = animation;
        }

        public void Perform(IRdtBuilder target)
        {
            if (target is Rdt2.Builder builder2)
            {
                builder2.RBJ = builder2.RBJ.WithAnimation(Index, Animation);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
