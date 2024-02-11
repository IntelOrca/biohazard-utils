﻿using System;
using System.IO;

namespace IntelOrca.Biohazard.Script
{
    internal class BioCvConstantTable : IConstantTable
    {
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
                'p' => GetProcedureName(value),
                'T' => GetItemName((byte)value),
                't' => GetItemName((byte)value),
                _ => null
            };
        }

        public string? GetConstant(byte opcode, int pIndex, BinaryReader reader)
        {
            using var br = reader.Fork();
            if (opcode == 0x06)
            {
                if (pIndex == 2)
                {
                    var var = br.ReadByte();
                    br.ReadByte();
                    var value = br.ReadByte();
                    if (var == 8)
                        return GetConstant('t', value);
                }
            }
            else if (opcode == 0x08)
            {
                if (pIndex == 1)
                {
                    var var = br.ReadByte();
                    var value = br.ReadByte();
                    if (var == 8)
                        return GetConstant('t', value);
                }
            }
            return null;
        }

        public int? GetConstantValue(string symbol)
        {
            return null;
        }

        private string GetProcedureName(int value)
        {
            return $"main_{value + 2:X2}";
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
            if (opcode == 0x64 || opcode == 0x66 || opcode == 0x67 || opcode == 0x69)
            {
                if (br == null)
                    return 1;

                if (br.BaseStream.Position == br.BaseStream.Length)
                    return 1;

                var byte2 = br.ReadByte();
                br.BaseStream.Position--;
                var opcode2 = (ushort)((opcode << 8) | byte2);
                return opcode2 switch
                {
                    0x6407 => 8,
                    0x640D => 6,
                    0x6434 => 2,
                    0x6480 => 2,
                    0x6481 => 12,
                    0x6482 => 2,
                    0x6483 => 4,
                    0x6489 => 4,
                    0x648B => 2,
                    0x648E => 2,
                    0x6496 => 4,
                    0x6601 => 2,
                    0x6608 => 2,
                    0x660C => 6,
                    0x6614 => 4,
                    0x6780 => 2,
                    0x678F => 6,
                    0x6793 => 8,
                    0x6794 => 4,
                    0x6900 => 5,
                    0x6902 => 3,
                    0x6903 => 3,
                    0x6904 => 3,
                    0x6905 => 6,
                    0x6906 => 6,
                    0x6907 => 8,
                    0x6908 => 8,
                    0x6909 => 6,
                    0x690A => 6,
                    0x690B => 6,
                    0x690C => 6,
                    0x690D => 6,
                    0x690F => 6,
                    0x6910 => 6,
                    0x6911 => 4,
                    0x6912 => 4,
                    0x6913 => 4,
                    0x6917 => 4,
                    0x6918 => 8,
                    0x6919 => 8,
                    0x691A => 10,
                    0x691B => 8,
                    0x691C => 6,
                    0x691D => 3,
                    0x691E => 3,
                    0x691F => 3,
                    0x6920 => 3,
                    0x6921 => 3,
                    0x6922 => 10,
                    0x6924 => 2,
                    0x6926 => 10,
                    0x6928 => 4,
                    0x6929 => 4,
                    0x692A => 4,
                    0x692C => 5,
                    0x692E => 4,
                    0x692F => 2,
                    0x6930 => 4,
                    0x6931 => 4,
                    0x6932 => 4,
                    0x6933 => 6,
                    0x6934 => 4,
                    0x6935 => 4,
                    _ => 1,
                };
            }
            return _opcodeSizes[opcode];
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
                0x06 => "cmpb:uuu",
                0x07 => "cmpw:uuuU",
                0x08 => "setb:uuu",
                0x09 => "setw:uU",
                0x0A => "set_wall_atari:uuu",
                0x0B => "set_etc_atari:uuu",
                0x0C => "set_floor_atari:uuu",
                0x0D => "ck_death:uU",
                0x0E => "ck_item:0Uuu",
                0x0F => "clr_use_item:u",
                0x10 => "ck_use_item:t",
                0x11 => "ck_player_item:t",
                0x12 => "set_cinematic:u",
                0x13 => "set_camera:uuu",
                0x14 => "event_on:uup",
                0x15 => "bgm_on:uuu",
                0x16 => "bgm_off:u",
                0x17 => "se_on:uuuUuu",
                0x18 => "se_off:u",
                0x19 => "voice_on:uUuuuu",
                0x1A => "voice_off:u",
                0x1B => "ck_adx:uuuuu",
                0x1C => "bg_se_on:uUuu",
                0x1D => "bg_se_off:uuu",
                0x1E => "ck_adx_time:uUuu",
                0x1F => "set_message:uuu",
                0x20 => "set_display_object:uuu",
                0x21 => "ck_death_event:uUuu",
                0x22 => "set_ck_enemy:uU",
                0x23 => "set_ck_item:0Uuu",
                0x24 => "set_init_model",
                0x25 => "set_etc_atari2:uUuuuuuu",
                0x26 => "ck_arms_item",
                0x27 => "change_arms_item",
                0x28 => "sub_status",
                0x29 => "set_camera_pause",
                0x2A => "set_camera_2",
                0x2B => "set_motion_pause",
                0x2C => "set_effect:uU",
                0x2D => "init_motion_pause",
                0x2E => "set_player_motion_pause",
                0x2F => "init_set_kage",
                0x30 => "init_motion_pause_ex",
                0x31 => "player_item_lost:t",
                0x32 => "set_object_link:uuuuuUUU",
                0x33 => "set_door_call:uUuuuu",
                0x34 => "set_player_object_link:uuuuuUUU",
                0x35 => "set_light",
                0x36 => "set_fade",
                0x37 => "room_case_no",
                0x38 => "ck_frame:uuuU",
                0x39 => "set_camera_info",
                0x3A => "set_player_muteki",
                0x3B => "set_default_model",
                0x3C => "set_mask",
                0x3D => "set_lip",
                0x3E => "start_mask",
                0x3F => "start_lip",
                0x40 => "set_player_start_look_g:uUUU",
                0x41 => "set_player_stop_look_g",
                0x42 => "set_item_aspd:uuu",
                0x43 => "set_effect_display:uuu",
                0x44 => "set_effect_amb",
                0x45 => "delete_object_se",
                0x46 => "set_next_room_bgm:uuuUuu",
                0x47 => "set_next_room_bg_se:uuuUUuu",
                0x48 => "call_foot_se",
                0x49 => "call_weapon_se",
                0x4A => "set_yakkyou",
                0x4B => "set_light_type",
                0x4C => "set_fog_color",
                0x4D => "ck_player_item_block",
                0x4E => "set_effect_blood:uuuUUUuuuu",
                0x4F => "set_cyouten_henkei",
                0x50 => "set_object_motion",
                0x51 => "set_object_enemy_link:uuuuuUUU",
                0x52 => "set_object_item_link:uuuuuUUU",
                0x53 => "set_enemy_item_link:uuuuuUUU",
                0x54 => "set_enemy_enemy_link:uuuuuUUU",
                0x55 => "start_cyouten_henkei",
                0x56 => "set_effect_blood_pool:uuuU",
                0x57 => "fix_event_camera_player",
                0x58 => "set_effect_blood_pool_2:uUUUuuuuU",
                0x59 => "set_object_object_link:uuuuuUUU",
                0x5A => "set_camera_yure:uU",
                0x5B => "set_init_camera",
                0x5C => "set_message_display_end",
                0x5D => "check_pad",
                0x5E => "start_movie",
                0x5F => "stop_movie",
                0x60 => "check_t_frame:uuuU",
                0x61 => "check_event_timer",
                0x62 => "check_camera",
                0x63 => "set_random",
                0x65 => "load_work",
                0x68 => "load_work_2",
                0x6A => "set_event_skip",
                0x6B => "delete_yakkyou",
                0x6F => "set_object_player_link:uuuuuUUU",
                0x7B => "set_wall_atari_2:uUtuuuuu",
                0x7C => "set_floor_atari_2:uUtuuuuu",
                0x95 => "play_bgm",
                0xA0 => "show_map",
                0xA1 => "reduce_health",
                0xA2 => "render_lighter_flame",
                0xA3 => "attach_object",
                0xA4 => "animation",
                0xA6 => "play_bgm_A6",
                0xA7 => "play_bgm_A7",
                0xB2 => "get_item",
                0xB7 => "item_to_box:t",
                0xB8 => "item_from_box:t",
                0xBF => "player_item_lost_ex",
                0xC2 => "get_item_ex:uuuU",
                0xCB => "countdown",
                0xCD => "main_menu",
                0xCE => "enable_first_person",
                0xCF => "init_game_item_ex",
                0xF3 => "ewhile2",
                0xF8 => "sleep:uU",
                0xF9 => "sleeping:uu",
                0xFA => "for:uU",
                0xFB => "next:u",
                0xFC => "while:u",
                0xFD => "ewhile",
                0xFE => "event_next",
                0xFF => "event_end:u",
                _ => "",
            };
        }

        public bool IsOpcodeCondition(byte opcode)
        {
            return opcode switch
            {
                0x04 => true,
                0x06 => true,
                0x07 => true,
                0x10 => true,
                0x11 => true,
                0x21 => true,
                0x26 => true,
                0x5D => true,
                0x72 => true,
                0x81 => true,
                _ => false,
            };
        }

        private byte[] _opcodeSizes = new byte[]
        {
            2, 2, 2, 2, 6, 6, 4, 6, 4, 4, 4, 4, 4, 4, 6, 2, 2,
            2, 2, 4, 4, 4, 2, 8, 2, 8, 2, 6, 6, 4,
            6, 4, 4, 6, 4, 6, 4, 10, 2, 2, 2, 2, 4,
            2, 4, 2, 2, 4, 4, 2, 12, 8, 12, 4, 6, 2,
            6, 4, 2, 6, 4, 4, 4, 4, 8, 2, 4, 4,
            6, 2, 8, 10, 6, 10, 8, 4, 6, 4, 14, 6, 2, 12,
            12, 12, 12, 2, 6, 2, 14, 12, 4, 2, 2, 6, 4, 2,
            6, 2, 6, 2, 0, 4, 0, 0, 6, 0, 2, 2, 8, 22, 22, 12,
            2, 2, 14, 22, 4, 4, 4, 6, 14, 6, 2, 10, 10,
            4, 4, 6, 4, 2, 4, 2, 8, 4, 16, 2, 10, 2,
            2, 12, 6, 2, 4, 4, 2, 4, 2, 2, 4, 4, 2, 8, 12,
            4, 4, 6, 2, 4, 2, 2, 6, 2, 4, 12, 4, 4,
            4, 4, 10, 2, 10, 6, 6, 4, 2, 4, 22, 6, 4, 8, 4,
            4, 2, 2, 2, 10, 16, 2, 2, 2, 6, 4, 28, 2, 6, 4, 8,
            2, 2, 4, 8, 4, 2, 2, 4, 2, 2, 2, 4, 10, 10,
            2, 4, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 4, 3, 4, 2, 2, 1, 1, 2,
        };

        private static string[] g_instructionSignatures = new string[]
        {
            "end",
            "if:\'",
            "else:l",
            "endif:u",
            "ck:uUuu",
            "set:uUuu",
            "set:uuu",
            "compare_word",
            "sv:uT",
            "sv_word",
            "wall_atari_set",
            "etc_atari_set",
            "floor_atari_set",
            "death_check",
            "item_check:0UU",
            "use_item_clear",
            "use_item_check",
            "player_item_check:t",
            "cinematic_set",
            "camera_set",
            "event_on:uup",
            "bgm_on",
            "bgm_on",
            "se_on",
            "se_off",
            "voice_on",
            "voice_off",
            "adx_check",
            "bg_se_on",
            "bg_se_off",
            "adx_time_check",
            "message_set",
            "set_display_object",
            "death_event_check",
            "enemy_set_check",
            "item_set_check",
            "init_model_set",
            "etc_atari_set_2",
            "use_item_check",
            "arms_item_change",
            "sub_status",
            "camera_set_2",
            "camera_set_2",
            "motion_pause_set",
            "effect_set",
            "init_motion_pause",
            "motion_pause_set_ply",
            "init_set_kage",
            "init_motion_pause_ex",
            "player_item_lost",
            "object_link_set",
            "set_disp_object",
            "object_link_set_ply",
            "light_set",
            "fade_set",
            "room_case_no",
            "frame_check",
            "camera_info_set",
            "muteki_set_pl",
            "def_model_set",
            "mask_set",
            "lip_set",
            "mask_start",
            "lip_start",
            "look_g_set_player_start",
            "look_g_set_player_stop",
            "item_aspd_set",
            "item_aspd_set",
            "effect_amb_set",
            "delete_object_se",
            "set_next_room_bgm",
            "set_next_room_bg_se",
            "foot_se_call",
            "light_set",
            "yakkyou_set",
            "light_type_set",
            "fog_color_set",
            "player_item_block_check",
            "effect_blood_set",
            "cyouten_henkei_set",
            "set_object_motion",
            "object_link_set_object_enemy",
            "object_link_set_object_item",
            "object_link_set_enemy_item",
            "object_link_set_enemy_enemy",
            "cyouten_henkei_start",
            "effect_blood_pool_set",
            "fix_event_camera_ply",
            "effect_blood_pool_set_2",
            "object_link_set_object_object",
            "camera_yure_set",
            "init_camera_set",
            "message_display_end_set",
            "pad_check",
            "movie_start",
            "movie_stop",
            "t_frame_check",
            "event_timer_clear",
            "camera_check",
            "random_set",
            "player_ctr",
            "load_work",
            "object_ctr",
            "sub_ctr",
            "load_work_2",
            "common_ctr",
            "event_skip_set",
            "delete_yakkyou",
            "object_alpha_set",
            "cyodan_set",
            "h_effect_set",
            "object_link_set_object_ply",
            "effect_push",
            "effect_pop",
            "area_search_object",
            "light_parameter_c_set",
            "light_parameter_start",
            "init_midi_slot_set",
            "d_sound_flag_set",
            "sound_volume_set",
            "light_parameter_set",
            "enemy_se_on",
            "enemy_se_off",
            "wal_atari_set_2",
            "flr_atari_set2",
            "motion_pos_set_enemy_ply",
            "kage_sw_set",
            "sound_pan_set",
            "init_pony_set",
            "sub_map_busy_check",
            "set_debug_loop_ex",
            "sound_fade_out",
            "cyouten_henkei_set_ex",
            "cyouten_henkei_start_ex",
            "easy_s_e_set",
            "sound_flag_re_set",
            "effect_uv_set",
            "player_change_set",
            "player_poison_check",
            "add_object_se",
            "rand_test",
            "event_com_set",
            "zombie_up_death_check",
            "face_pause_set",
            "face_re_set",
            "effect_mode_set",
            "bg_se_off_2",
            "bgm_off_2",
            "bg_se_on_2",
            "bgm_on_2",
            "effect_sensya_set",
            "effect_kokuen_set",
            "effect_sand_set",
            "enemy_hp_up",
            "face_rep",
            "movie_check",
            "set_item_motion",
            "object_aspd_set",
            "puru_puru_flag_set",
            "puru_puru_start",
            "map_system_on",
            "map_system_on",
            "event_lighter_fire_set",
            "object_link_set_ply_item",
            "player_kaidan_motion",
            "enemy_render_set",
            "bgm_on_ex",
            "bgm_on_2_ex",
            "fog_parameter_c_set",
            "fog_parameter_start",
            "effect_uv_set_2",
            "bg_color_set",
            "movie_time_check",
            "effect_type_set",
            "player_poison_2_cr",
            "ply_hand_change",
            "h_effect_set_2",
            "object_dpos_check",
            "item_get_get",
            "etc_atari_enemy_pos_set",
            "etc_atari_event_pos_set",
            "load_work_ex",
            "room_sound_case",
            "room_sound_case",
            "item_s_box_to_i_box",
            "grd_pos_set",
            "grd_pos_move_c_set",
            "grd_pos_move_start",
            "event_kill",
            "re_try_point_set",
            "re_try_point_set",
            "ply_dpos_check",
            "cyodan_set_ex",
            "arms_item_set",
            "item_get_get_ex",
            "effect_sand_set_matsumoto",
            "voice_wait",
            "voice_start",
            "game_over_set",
            "player_item_change_m",
            "effect_baku_drm_set",
            "effect_baku_drm_set",
            "effect_clear_event",
            "event_timer_set",
            "enemy_look_flag_set",
            "return_title_event",
            "syukan_mode_set",
            "ex_game_item_init",
            "ex_game_item_init",
            "effect_s_size_set",
            "effect_s_size_set",
            "ranking_call",
            "call_sys_se",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "nothing",
            "e_while_2",
            "com_next",
            "nothing",
            "nothing",
            "nothing",
            "sleep",
            "sleeping",
            "for",
            "for",
            "while",
            "e_while",
            "event_next",
            "event_end",
        };

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
