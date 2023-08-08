namespace IntelOrca.Biohazard.Room
{
    public static class RdtFileChunkKinds
    {
        public const int Unknown = 0;

        public const int RDT1Header = 0x0100 | 0;
        public const int RDT1LIT = 0x0100 | 1;
        public const int RDT1Offsets = 0x0100 | 2;
        public const int RDT1RVD = 0x0100 | 3;
        public const int RDT1SCA = 0x0100 | 4;
        public const int RDT1EmbeddedModelTable = 0x0100 | 5;
        public const int RDT1EmbeddedItemTable = 0x0100 | 6;
        public const int RDT1BLK = 0x0100 | 7;
        public const int RDT1FLR = 0x0100 | 8;
        public const int RDT1InitSCD = 0x0100 | 9;
        public const int RDT1MainSCD = 0x0100 | 10;
        public const int RDT1EventSCD = 0x0100 | 11;
        public const int RDT1EMR = 0x0100 | 12;
        public const int RDT1EDD = 0x0100 | 13;
        public const int RDT1MSG = 0x0100 | 14;
        public const int RDT1EmbeddedItemIcons = 0x0100 | 15;
        public const int RDT1ESPIDs = 0x0100 | 16;
        public const int RDT1ESPEFFTable = 0x0100 | 17;
        public const int RDT1ESPTIMTable = 0x0100 | 18;
        public const int RDT1EDT = 0x0100 | 19;
        public const int RDT1VH = 0x0100 | 20;
        public const int RDT1VB = 0x0100 | 21;
        public const int RDT1RID = 0x0100 | 22;
        public const int RDT1EmbeddedObjectTmd = 0x0100 | 23;
        public const int RDT1EmbeddedObjectTim = 0x0100 | 24;
        public const int RDT1EmbeddedItemTmd = 0x0100 | 25;
        public const int RDT1EmbeddedItemTim = 0x0100 | 26;
        public const int RDT1EmbeddedEspTim = 0x0100 | 27;
        public const int RDT1EmbeddedCamTim = 0x0100 | 28;
        public const int RDT1EmbeddedCamMask = 0x0100 | 29;
        public const int RDT1EmbeddedEspEff = 0x0100 | 30;

        public const int Header = 0x0200 | 1;
        public const int OffsetTable = 0x0200 | 2;

        public const int SoundAttributeTable = 0x0200 | 2;
        public const int EmbeddedVH = 0x0200 | 3;
        public const int EmbeddedVB = 0x0200 | 4;
        public const int EmbeddedTrialVH = 0x0200 | 5;
        public const int EmbeddedTrialVB = 0x0200 | 6;
        public const int Ota = 0x0200 | 7;
        public const int Collisions = 0x0200 | 8;
        public const int CameraPositions = 0x0200 | 9;
        public const int CameraSwitches = 0x0200 | 10;
        public const int Lights = 0x0200 | 11;
        public const int EmbeddedObjectTable = 0x0200 | 12;
        public const int FloorAreas = 0x0200 | 13;
        public const int BlockUnknown = 0x0200 | 14;
        public const int MessagesJpn = 0x0200 | 15;
        public const int MessagesEng = 0x0200 | 16;
        public const int ScrollingTim = 0x0200 | 17;
        public const int ScdInit = 0x0200 | 18;
        public const int ScdMain = 0x0200 | 19;
        public const int Effects = 0x0200 | 20;
        public const int EffectTable = 0x0200 | 21;
        public const int EffectSprites = 0x0200 | 22;
        public const int ObjectTextures = 0x0200 | 23;
        public const int RoomAnimations = 0x0200 | 24;

        public const int EmbeddedObjectTim = 0x0200 | 25;
        public const int EmbeddedObjectMd1 = 0x0200 | 26;

        public const int ScdUnused = 0x0200 | 27;
        public const int ScdEvent = 0x0200 | 28;
    }
}
