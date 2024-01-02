using System;
using System.IO;

namespace IntelOrca.Biohazard.Script
{
    internal class BioCvConstantTable : IConstantTable
    {
        private readonly string[] g_instructionSignatures = new string[256];

        public BioCvConstantTable()
        {
            for (var i = 0; i < 256; i++)
            {
                var sig = GetOpcodeSignature((byte)i);
                if (sig != "")
                {
                    g_instructionSignatures[i] = sig;
                }
            }
        }

        public byte? FindOpcode(string name, bool isEventOpcode)
        {
            for (int i = 0; i < g_instructionSignatures.Length; i++)
            {
                var signature = g_instructionSignatures[i];
                if (signature == "")
                    continue;

                var opcodeName = signature;
                var colonIndex = signature.IndexOf(':');
                if (colonIndex != -1)
                    opcodeName = signature.Substring(0, colonIndex);

                if (name == opcodeName)
                    return (byte)i;
            }
            return null;
        }

        public string? GetConstant(char kind, int value)
        {
            return kind switch
            {
                '0' => $"ID_ITEM_{value}",
                'T' => GetItemName((byte)value),
                't' => GetItemName((byte)value),
                _ => null
            };
        }

        public string? GetConstant(byte opcode, int pIndex, BinaryReader br)
        {
            return null;
        }

        public int? GetConstantValue(string symbol)
        {
            return null;
        }

        public string GetEnemyName(byte kind)
        {
            throw new NotImplementedException();
        }

        public string GetItemName(byte kind) => g_itemNames.Namify("ITEM_", kind);

        public string? GetNamedFlag(int obj, int index)
        {
            throw new NotImplementedException();
        }

        public string? GetNamedVariable(int index)
        {
            throw new NotImplementedException();
        }

        public int GetInstructionSize(byte opcode, BinaryReader? br, bool isEventOpcode = false)
        {
            var result = opcode switch
            {
                0x00 => 2, // end
                0x01 => 2, // if
                0x02 => 2, // else
                0x03 => 2, // endif
                0x04 => 6, // ck
                0x05 => 6, // set
                0x06 => 4, // cmp
                0x07 => 0, //
                0x08 => 4, // inventory_item
                0x09 => 0, //
                0x0A => 4, //
                0x0B => 4, //
                0x0C => 4, //
                0x0D => 0, //
                0x0E => 6, // set item flag
                0x0F => 0, //
                0x14 => 4, //
                0x18 => 2, //
                0x23 => 6, //
                0x24 => 4, //
                0x25 => 10, //
                0x2F => 4, //
                0x32 => 12, //
                0x33 => 8, //
                0x34 => 4, //
                0x3B => 6, //
                0x42 => 4, //
                0x43 => 4, //
                0x63 => 2, //
                0x65 => 4, //
                0x69 => 6, //
                0x75 => 4, //
                0x8B => 4, //
                0x92 => 2, //
                0x94 => 4, //
                0x95 => 4, //
                0x9C => 2, //
                0xA0 => 2, //
                0xA6 => 10, //
                0xBC => 2, //
                0xCD => 2, //
                0xD1 => 10, //
                0xFF => 4, //
                _ => 0,
            };
            return result == 0 ? 2 : result;
        }

        public string GetOpcodeSignature(byte opcode, bool isEventOpcode = false)
        {
            return opcode switch
            {
                0x00 => "end",
                0x01 => "if:\'",
                0x02 => "else:l",
                0x03 => "endif:u",
                0x04 => "ck:uUuu",
                0x05 => "set:uUuu",
                0x06 => "cmp:uuu",
                0x07 => "",
                0x08 => "inventory_item:uT",
                0x09 => "",
                0x0A => "",
                0x0B => "",
                0x0C => "",
                0x0D => "",
                0x0E => "item_set_flag:0UU",
                0x0F => "",
                0x65 => "disable_input",
                0x95 => "play_bgm",
                0xA0 => "show_map",
                0xA1 => "reduce_health",
                0xA2 => "render_lighter_flame",
                0xA3 => "attach_object",
                0xA4 => "animation",
                0xA6 => "play_bgm_A6",
                0xA7 => "play_bgm_A7",
                0xCB => "countdown",
                0xCD => "main_menu",
                0xCE => "enable_first_person",
                0xFF => "end_of_script",
                _ => "",
            };
        }

        public bool IsOpcodeCondition(byte opcode)
        {
            return opcode == 4;
        }

        private static string[] g_itemNames = new[]
        {
            "None",
            "RocketLauncher",
            "AssaultRifle",
            "SniperRifle",
            "Shotgun",
            "HandgunGlock17",
            "GrenadeLauncher",
            "BowGun",
            "CombatKnife",
            "Handgun",
            "CustomHandgun",
            "LinearLauncher",
            "HandgunBullets",
            "MagnumBullets",
            "ShotgunShells",
            "GrenadeRounds",
            "AcidRounds",
            "FlameRounds",
            "BowGunArrows",
            "M93RPart",
            "FAidSpray",
            "GreenHerb",
            "RedHerb",
            "BlueHerb",
            "MixedHerb2Green",
            "MixedHerbRedGreen",
            "MixedHerbBlueGreen",
            "MixedHerb2GreenBlue",
            "MixedHerb3Green",
            "MixedHerbGreenBlueRed",
            "MagnumBulletsInsideCase",
            "InkRibbon",
            "Magnum",
            "GoldLugers",
            "SubMachineGun",
            "BowGunPowder",
            "GunPowderArrow",
            "BOWGasRounds",
            "MGunBullets",
            "GasMask",
            "RifleBullets",
            "DuraluminCaseUnused",
            "ARifleBullets",
            "AlexandersPierce",
            "AlexandersJewel",
            "AlfredsRing",
            "AlfredsJewel",
            "PrisonersDiary",
            "DirectorsMemo",
            "Instructions",
            "Lockpick",
            "GlassEye",
            "PianoRoll",
            "SteeringWheel",
            "CraneKey",
            "Lighter",
            "EaglePlate",
            "SidePack",
            "MapRoll",
            "HawkEmblem",
            "QueenAntObject",
            "KingAntObject",
            "BiohazardCard",
            "DuraluminCaseM93RParts",
            "Detonator",
            "ControlLever",
            "GoldDragonfly",
            "SilverKey",
            "GoldKey",
            "ArmyProof",
            "NavyProof",
            "AirForceProof",
            "KeyWithTag",
            "IDCard",
            "Map",
            "AirportKey",
            "EmblemCard",
            "SkeletonPicture",
            "MusicBoxPlate",
            "GoldDragonflyNoWings",
            "Album",
            "Halberd",
            "Extinguisher",
            "Briefcase",
            "PadlockKey",
            "TG01",
            "SpAlloyEmblem",
            "ValveHandle",
            "OctaValveHandle",
            "MachineRoomKey",
            "MiningRoomKey",
            "BarCodeSticker",
            "SterileRoomKey",
            "DoorKnob",
            "BatteryPack",
            "HemostaticWire",
            "TurnTableKey",
            "ChemStorageKey",
            "ClementAlpha",
            "ClementSigma",
            "TankObject",
            "SpAlloyEmblemUnused",
            "AlfredsMemo",
            "RustedSword",
            "Hemostatic",
            "SecurityCard",
            "SecurityFile",
            "AlexiasChoker",
            "AlexiasJewel",
            "QueenAntRelief",
            "KingAntRelief",
            "RedJewel",
            "BlueJewel",
            "Socket",
            "SqValveHandle",
            "Serum",
            "EarthenwareVase",
            "PaperWeight",
            "SilverDragonflyNoWings",
            "SilverDragonfly",
            "WingObject",
            "Crystal",
            "GoldDragonfly1Wing",
            "GoldDragonfly2Wings",
            "GoldDragonfly3Wings",
            "File",
            "PlantPot",
            "PictureB",
            "DuraluminCaseBowGunPowder",
            "DuraluminCaseMagnumRounds",
            "BowGunPowderUnused",
            "EnhancedHandgun",
            "Memo",
            "BoardClip",
            "Card",
            "NewspaperClip",
            "LugerReplica",
            "QueenAntReliefComplete",
            "FamilyPicture",
            "FileFolders",
            "RemoteController",
            "QuestionA",
            "M1P",
            "CalicoBullets",
            "ClementMixture",
            "PlayingManual",
            "QuestionB",
            "QuestionC",
            "QuestionD",
            "EmptyExtinguisher",
            "SquareSocket",
            "QuestionE",
            "CrestKeyS",
            "CrestKeyG"
        };
    }
}
