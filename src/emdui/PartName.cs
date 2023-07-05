using IntelOrca.Biohazard;

namespace emdui
{
    internal static class PartName
    {
        private static string[] g_partNamesRe1 = new string[]
        {
            "chest", "head",
            "waist",
            "thigh (left)", "calf (left)", "foot (left)",
            "thigh (right)", "calf (right)", "foot (right)",
            "upper arm (left)", "forearm (left)", "hand (left)",
            "upper arm (right)", "forearm (right)", "hand (right)"
        };

        private static string[] g_partNamesRe2 = new string[]
        {
            "chest", "waist",
            "thigh (right)", "calf (right)", "foot (right)",
            "thigh (left)", "calf (left)", "foot (left)",
            "head",
            "upper arm (right)", "forearm (right)", "hand (right)",
            "upper arm (left)", "forearm (left)", "hand (left)",
            "ponytail (A)", "ponytail (B)", "ponytail (C)", "ponytail (D)"
        };

        private static string[] g_partNamesRe3 = new string[]
        {
            "chest", "head",
            "upper arm (right)", "forearm (right)", "hand (right)",
            "upper arm (left)", "forearm (left)", "hand (left)",
            "waist",
            "thigh (right)", "calf (right)", "foot (right)",
            "thigh (left)", "calf (left)", "foot (left)",
            "hand with gun"
        };

        public static string GetPartName(BioVersion version, int partIndex)
        {
            string[] partNameArray = null;
            if (version == BioVersion.Biohazard1)
            {
                partNameArray = g_partNamesRe1;
            }
            else if (version == BioVersion.Biohazard2)
            {
                partNameArray = g_partNamesRe2;
            }
            else if (version == BioVersion.Biohazard3)
            {
                partNameArray = g_partNamesRe3;
            }
            if (partNameArray != null && partNameArray.Length > partIndex)
                return partNameArray[partIndex];
            return $"Part {partIndex}";
        }
    }
}
