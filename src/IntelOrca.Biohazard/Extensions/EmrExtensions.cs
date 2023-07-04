using System;
using System.Runtime.InteropServices;
using IntelOrca.Biohazard.Model;

namespace IntelOrca.Biohazard.Extensions
{
    public static class EmrExtensions
    {
        public static Emr Scale(this Emr emr, double y) => Scale(emr, 1, y, 1);

        public static Emr Scale(this Emr emr, double x, double y, double z)
        {
            if (x == 1 && y == 1 && z == 1)
                return emr;

            var emrBuilder = emr.ToBuilder();
            var numKeyFrames = emrBuilder.KeyFrameData.Length / emrBuilder.KeyFrameSize;
            for (var i = 0; i < numKeyFrames; i++)
            {
                var keyFrameOffset = i * emrBuilder.KeyFrameSize;
                var offset = MemoryMarshal.Cast<byte, Emr.Vector>(new Span<byte>(emrBuilder.KeyFrameData).Slice(keyFrameOffset, emrBuilder.KeyFrameSize));
                offset[0].x = (short)(offset[0].x * x);
                offset[0].y = (short)(offset[0].y * y);
                offset[0].z = (short)(offset[0].z * z);
            }
            return emrBuilder.ToEmr();
        }
    }
}
