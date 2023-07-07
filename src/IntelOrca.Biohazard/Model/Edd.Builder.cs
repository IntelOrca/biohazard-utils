using System.Collections.Generic;
using System.IO;

namespace IntelOrca.Biohazard.Model
{
    public sealed partial class Edd
    {
        public class Builder
        {
            public List<Animation> Animations { get; } = new List<Animation>();

            public Edd ToEdd()
            {
                var ms = new MemoryStream();
                var bw = new BinaryWriter(ms);

                var offset = Animations.Count * 4;
                for (var i = 0; i < Animations.Count; i++)
                {
                    var animation = Animations[i];
                    bw.Write((ushort)animation.Frames.Length);
                    bw.Write((ushort)offset);
                    offset += animation.Frames.Length * 4;
                }

                foreach (var animation in Animations)
                {
                    foreach (var frame in animation.Frames)
                    {
                        bw.Write(frame);
                    }
                }

                bw.Write((uint)ms.Length);

                return new Edd(ms.ToArray());
            }

            public class Animation
            {
                public Frame[] Frames { get; set; } = new Frame[0];
            }
        }
    }
}
