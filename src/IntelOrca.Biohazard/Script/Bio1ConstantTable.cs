using System.Globalization;
using System.IO;

namespace IntelOrca.Biohazard.Script
{
    public class Bio1ConstantTable : IConstantTable
    {
        private const byte SCE_MESSAGE = 2;
        private const byte SCE_ITEMBOX = 8;
        private const byte SCE_EVENT = 9;
        private const byte SCE_SAVE = 10;

        public string GetEnemyName(byte kind) => g_enemyNames.Namify("ENEMY_", kind);
        public string GetItemName(byte kind) => g_itemNames.Namify("ITEM_", kind);

        public string GetOpcodeSignature(byte opcode, bool isEventOpcode)
        {
            var table = isEventOpcode ? _eventOpcodes : _opcodes;
            if (opcode < table.Length)
                return table[opcode];
            return "";
        }

        public string? GetConstant(byte opcode, int pIndex, BinaryReader reader)
        {
            using (var br = reader.Fork())
            {
                if (opcode == (byte)OpcodeV1.AotSet)
                {
                    if (pIndex == 9)
                    {
                        br.BaseStream.Position += 9;
                        var sce = br.ReadByte();
                        if (sce == SCE_EVENT)
                        {
                            br.BaseStream.Position += 3;
                            return GetConstant('p', br.ReadByte());
                        }
                    }
                }
                else if (opcode == (byte)OpcodeV1.AotReset)
                {
                    if (pIndex == 5)
                    {
                        br.BaseStream.Position += 1;
                        var sce = br.ReadByte();
                        if (sce == SCE_EVENT)
                        {
                            br.BaseStream.Position += 3;
                            return GetConstant('p', br.ReadByte());
                        }
                    }
                }
            }
            return null;
        }

        public string? GetConstant(char kind, int value)
        {
            switch (kind)
            {
                case 'e':
                    return GetEnemyName((byte)value);
                case 'i':
                    if (value == 255)
                        return "LOCKED";
                    else if (value == 254)
                        return "UNLOCK";
                    else if (value == 0)
                        return "UNLOCKED";
                    else
                        return GetItemName((byte)value);
                case 's':
                    return GetConstantName(g_sceNames, value);
                case 'p':
                    return $"event_{value:X2}";
            }
            return null;
        }

        private string? GetConstantName(string?[] table, int value)
        {
            if (value >= 0 && value < table.Length)
                return table[value];
            return null;
        }

        private int? FindConstantValue(string symbol, char kind)
        {
            for (int i = 0; i < 256; i++)
            {
                var name = GetConstant(kind, i);
                if (name == symbol)
                    return i;
            }
            return null;
        }

        public int? GetConstantValue(string symbol)
        {
            switch (symbol)
            {
                case "LOCKED":
                    return 255;
                case "UNLOCK":
                    return 254;
                case "UNLOCKED":
                    return 0;
            }
            if (symbol.StartsWith("ENEMY_"))
                return FindConstantValue(symbol, 'e');
            else if (symbol.StartsWith("ITEM_"))
                return FindConstantValue(symbol, 'i');
            else if (symbol.StartsWith("SCE_"))
                return FindConstantValue(symbol, 'v');
            else if (symbol.StartsWith("RDT_"))
            {
                var number = symbol.Substring(4);
                if (int.TryParse(number, NumberStyles.HexNumber, null, out var rdt))
                {
                    return rdt;
                }
            }
            return null;
        }

        public int GetInstructionSize(byte opcode, BinaryReader? br, bool isEventOpcode = false)
        {
            if (isEventOpcode)
            {
                switch ((Re1EventOpcode)opcode)
                {
                    default:
                        return 1;
                    case Re1EventOpcode.Unk04:
                        return 3;
                    case Re1EventOpcode.Fork:
                        return 4;
                    case Re1EventOpcode.Block:
                        return 2;
                    case Re1EventOpcode.Single:
                        return 2;
                    case Re1EventOpcode.Unk08:
                    case Re1EventOpcode.UnkF6:
                        return 2;
                    case Re1EventOpcode.UnkF8:
                        return 4;
                    case Re1EventOpcode.For:
                        return 4;
                    case Re1EventOpcode.SetInst:
                        using (var br2 = br!.Fork())
                        {
                            var ll = br2.ReadByte();
                            return 2 + ll;
                        }
                }
            }
            else
            {

                if (opcode >= _instructionSizes1.Length)
                    return 0;
                return _instructionSizes1[opcode];
            }
        }

        public byte? FindOpcode(string name)
        {
            for (int i = 0; i < _opcodes.Length; i++)
            {
                var signature = _opcodes[i];
                var colonIndex = signature.IndexOf(':');
                if (colonIndex == -1)
                    continue;

                var opcodeName = signature.Substring(0, colonIndex);
                if (name == opcodeName)
                {
                    return (byte)i;
                }
            }
            return null;
        }

        public bool IsOpcodeCondition(byte opcode)
        {
            switch ((OpcodeV1)opcode)
            {
                case OpcodeV1.Ck:
                case OpcodeV1.Cmp6:
                case OpcodeV1.Cmp7:
                case OpcodeV1.TestItem:
                case OpcodeV1.TestPickup:
                    return true;
            }
            return false;
        }

        public string? GetNamedFlag(int obj, int index)
        {
            return null;
        }

        public string? GetNamedVariable(int index)
        {
            return null;
        }

        private string[] g_enemyNames = new string[]
        {
            "Zombie (Groundskeeper)",
            "Zombie (Naked)",
            "Cerberus",
            "Spider (Brown)",
            "Spider (Black)",
            "Crow",
            "Hunter",
            "Bee",
            "Plant 42",
            "Chimera",
            "Snake",
            "Neptune",
            "Tyrant 1",
            "Yawn 1",
            "Plant42 (roots)",
            "Fountain Plant",
            "Tyrant 2",
            "Zombie (Researcher)",
            "Yawn 2",
            "Cobweb",
            "Computer Hands (left)",
            "Computer Hands (right)",
            "Unknown",
            "Unknown",
            "Unknown",
            "Unknown",
            "Unknown",
            "Unknown",
            "Unknown",
            "Unknown",
            "Unknown",
            "Unknown",
            "Chris (Stars)",
            "Jill (Stars)",
            "Barry (Stars)",
            "Rebecca (Stars)",
            "Wesker (Stars)",
            "Kenneth 1",
            "Forrest",
            "Richard",
            "Enrico",
            "Kenneth 2",
            "Barry 2",
            "Barry 2 (Stars)",
            "Rebecca 2 (Stars)",
            "Barry 3",
            "Wesker 2 (Stars)",
            "Chris (Jacket)",
            "Jill (Black Shirt)",
            "Chris 2 (Jacket)",
            "Jill (Red Shirt)",
        };

        private string[] g_itemNames = new string[]
        {
            "Nothing",
            "Combat Knife",
            "Beretta",
            "Shotgun",
            "DumDum Colt",
            "Colt Python",
            "FlameThrower",
            "Bazooka Acid",
            "Bazooka Explosive",
            "Bazooka Flame",
            "Rocket Launcher",
            "Clip",
            "Shells",
            "DumDum Rounds",
            "Magnum Rounds",
            "FlameThrower Fuel",
            "Explosive Rounds",
            "Acid Rounds",
            "Flame Rounds",
            "Empty Bottle",
            "Water",
            "Umb No. 2",
            "Umb No. 4",
            "Umb No. 7",
            "Umb No. 13",
            "Yellow 6",
            "NP-003",
            "V-Jolt",
            "Broken Shotgun",
            "Square Crank",
            "Hex Crank",
            "Wood Emblem",
            "Gold Emblem",
            "Blue Jewel",
            "Red Jewel",
            "Music Notes",
            "Wolf Medal",
            "Eagle Medal",
            "Chemical",
            "Battery",
            "MO Disk",
            "Wind Crest",
            "Flare",
            "Slides",
            "Moon Crest",
            "Star Crest",
            "Sun Crest",
            "Ink Ribbon",
            "Lighter",
            "Lock Pick",
            "Nameless (Can of Oil)",
            "Sword Key",
            "Armor Key",
            "Sheild Key",
            "Helmet Key",
            "Lab Key (1)",
            "Special Key",
            "Dorm Key (002)",
            "Dorm Key (003)",
            "C. Room Key",
            "Lab Key (2)",
            "Small Key",
            "Red Book",
            "Doom Book (2)",
            "Doom Book (1)",
            "F-Aid Spray",
            "Serum",
            "Red Herb",
            "Green Herb",
            "Blue Herb",
            "Mixed (Red+Green)",
            "Mixed (2 Green)",
            "Mixed (Blue + Green)",
            "Mixed (All)",
            "Mixed (Silver Color)",
            "Mixed (Bright Blue-Green)"
        };

        private string[] _opcodes = new string[]
        {
            "end:u",
            "if:l",
            "else:l",
            "endif:u",
            "ck:ubu",
            "set:ubu",
            "cmpb:uuu",
            "cmpw:uuuI",
            "setb:uuu",
            "cutnext:u",
            "cutcurr:u",
            "",
            "door_aot_set:uIIIIuuuuurIIIIiu",
            "aot_set:uIIIIsuuuuuuu",
            "nop:u",
            "",
            "testitem:i",
            "testpickup:i",
            "aot_reset:usuIII",
            "aot_delete",
            "evt_exec:uup",
            "bgm_play:u",
            "bgm_stop:u",
            "",
            "item_aot_set:uIIIIiuuuuuuuuuuuuuuu",
            "setbyte:uuu",
            "item_ck",
            "enemy:euuuuuuIuuIIIuuuu",
            "",
            "",
            "xa_on",
            "obj:uuuIIIIuuuuuuuuuuuuuuuu",
            "dir_set:uIIIIII",
            "pos_set:uIIIIII",
            "",
            "cut_auto",
            "aot_on",
            "",
            "",
            "",
            "",
            "movie_on:u",
            "effect",
            "",
            "remove_item",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "plc_dest",
            "plc_motion",
            "",
            "plc_ret",
            "",
            "plc_rotate",
            "",
            "",
            "",
            "sleep",
            "",
            "for",
            "next",
            "message_on",
            "exec_inst",
            "process",
            "disable:u"
        };

        private string[] _eventOpcodes = new string[]
        {
            "evt_nop",
            "evt_01",
            "evt_02",
            "evt_03",
            "evt_04",
            "evt_fork:upu",
            "evt_block:u",
            "evt_single:u",
            "evt_08",
            "evt_09",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "evt_plc_dest",
            "evt_plc_motion",
            "",
            "evt_plc_ret",
            "",
            "evt_plc_rotate",
            "",
            "evt_F6",
            "evt_F7",
            "evt_sleep",
            "evt_F9",
            "evt_for:uU",
            "evt_fornext",
            "evt_set_inst",
            "evt_exec_inst",
            "evt_next",
            "evt_disable"
        };

        private static int[] _instructionSizes1 = new int[]
        {
            2, 2, 2, 2, 4, 4, 4, 6, 4, 2, 2, 4, 26, 18, 2, 8,
            2, 2, 10, 4, 4, 2, 2, 10, 26, 4, 2, 22, 6, 2, 4, 28,
            14, 14, 4, 2, 4, 4, 0, 2, 4 + 0, 2, 12, 4, 2, 4, 0, 4,
            12, 4, 4, 4 + 0, 8, 4, 4, 4, 4, 2, 4, 6, 6, 12, 2, 6,
            16, 4, 4, 4, 2, 2, 44 + 0, 14, 2, 2, 2, 2, 4, 2, 4, 2,
            2
        };

        private static readonly string[] g_sceNames = new string[] {
            "SCE_0",
            "SCE_1",
            "SCE_MESSAGE",
            "SCE_3",
            "SCE_4",
            "SCE_5",
            "SCE_6",
            "SCE_7",
            "SCE_ITEMBOX",
            "SCE_EVENT",
            "SCE_SAVE"
        };
    }
}
