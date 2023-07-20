using System;
using System.IO;
using System.Text;

namespace IntelOrca.Biohazard
{
    public sealed class BioString
    {
        private const string EnTable = " .___()_____0123456789:_,\"!?_ABCDEFGHIJKLMNOPQRSTUVWXYZ[/]'-_abcdefghijklmnopqrstuvwxyz_________";
        private const byte Green = 0xF9;
        private const byte StartText = 0xFA;
        private const byte YesNoQuestion = 0xFB;
        private const byte NewLine = 0xFC;
        private const byte UnknownFD = 0xFD;
        private const byte EndText = 0xFE;

        private readonly byte[] _data;

        public ReadOnlySpan<byte> Data => _data;

        public BioString() : this("") { }

        public BioString(ReadOnlySpan<byte> data)
        {
            _data = data.ToArray();
        }

        public BioString(string s)
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            bw.Write(StartText);
            bw.Write((byte)2);
            foreach (var c in s)
            {
                if (c == '@')
                {
                    bw.Write(NewLine);
                    bw.Write(YesNoQuestion);
                    bw.Write((byte)0x40);
                }
                else
                {
                    var index = EnTable.IndexOf(c);
                    if (index == -1)
                        bw.Write((byte)0);
                    else
                        bw.Write((byte)index);
                }
            }
            bw.Write(EndText);
            bw.Write((byte)0);
            _data = ms.ToArray();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            var isGreen = false;
            for (var i = 0; i < _data.Length; i++)
            {
                var b = _data[i];
                switch (b)
                {
                    case Green:
                        if (!isGreen)
                            sb.Append('{');
                        else
                            sb.Append('}');
                        isGreen = !isGreen;
                        i++;
                        break;
                    case StartText:
                        i++;
                        break;
                    case YesNoQuestion:
                        i++;
                        sb.Append('@');
                        break;
                    case NewLine:
                        sb.Append('\n');
                        break;
                    case UnknownFD:
                        i++;
                        break;
                    case EndText:
                        i = _data.Length - 1;
                        break;
                    default:
                        if (b < EnTable.Length)
                        {
                            sb.Append(EnTable[b]);
                        }
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
