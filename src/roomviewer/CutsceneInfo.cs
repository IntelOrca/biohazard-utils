using System.Collections.Generic;

namespace IntelOrca.Biohazard.RoomViewer
{
    internal struct REPosition
    {
        public int X { get; }
        public int Y { get; }
        public int Z { get; }
        public int D { get; }

        public int Floor => Y / -1800;

        public REPosition(int x, int y, int z) : this(x, y, z, 0) { }
        public REPosition(int x, int y, int z, int d)
        {
            X = x;
            Y = y;
            Z = z;
            D = d;
        }

        public REPosition WithY(int y) => new REPosition(X, y, Z, D);

        public REPosition Reverse()
        {
            return new REPosition(X, Y, Z, (D + 2000) % 4000);
        }

        public static REPosition OutOfBounds { get; } = new REPosition(-32000, -10000, -32000);

        public override string ToString() => $"({X},{Y},{Z},{D})";
    }

    internal class CutsceneRoomInfo
    {
        public PointOfInterest[] Poi { get; set; }
    }

    internal class PointOfInterest
    {
        public int Id { get; set; }
        public string Kind { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int D { get; set; }
        public int Cut { get; set; }
        public int? CloseCut { get; set; }
        public int[] Cuts { get; set; }
        public int[] Edges { get; set; }

        public REPosition Position => new REPosition(X, Y, Z, D);

        public int[] AllCuts
        {
            get
            {
                var cuts = new List<int> { Cut };
                if (Cuts != null)
                    cuts.AddRange(Cuts);
                if (CloseCut != null)
                    cuts.Add(CloseCut.Value);
                return cuts.ToArray();
            }
        }

        public override string ToString() => $"Id = {Id} Kind = {Kind} Cut = {Cut} Position = {Position}";
    }

    internal static class PoiKind
    {
        public const string Trigger = "trigger";
        public const string Door = "door";
        public const string Stairs = "stairs";
        public const string Waypoint = "waypoint";
        public const string Meet = "meet";
        public const string Npc = "npc";
    }
}
