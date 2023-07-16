using System;

namespace IntelOrca.Biohazard.Script
{
    internal static class ConstantTable
    {
        public static IConstantTable FromVersion(BioVersion version) =>
            version switch
            {
                BioVersion.Biohazard1 => new Bio1ConstantTable(),
                BioVersion.Biohazard2 => new Bio2ConstantTable(),
                BioVersion.Biohazard3 => new Bio3ConstantTable(),
                _ => throw new NotSupportedException()
            };
    }
}
