using System;
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
                0x07 => 6, //
                0x08 => 4, // sv
                0x09 => 0, //
                0x0A => 4, //
                0x0B => 4, //
                0x0C => 4, //
<<<<<<< Updated upstream
                0x0D => 4, //
                0x0E => 6, // set item flag
=======
                0x0D => 0, //
                0x0E => 6, // item_check
>>>>>>> Stashed changes
                0x0F => 0, //
                0x14 => 4, //
                0x18 => 2, //
                0x23 => 6, //
                0x24 => 4, //
                0x25 => 10, // set_door
                0x2F => 4, //
                0x32 => 12, //
                0x33 => 8, //
                0x34 => 4, //
                0x3B => 6, //
                0x42 => 4, //
                0x43 => 4, //
                0x5B => 2, // play_sound_1
                0x5F => 2, // play_sound_2
                0x63 => 2, //
                0x65 => 4, //
                0x69 => 6, //
                0x72 => 14, //
                0x75 => 4, //
<<<<<<< Updated upstream
                0x86 => 14, //
=======
                0x86 => 16, //
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
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
                0x0D => "enemy_set_flag:uU",
                0x0E => "item_set_flag:0UU",
                0x0F => "",
                0x25 => "set_door",
                0x5B => "play_sound_1",
                0x5F => "play_sound_2",
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
=======
            return g_instructionSignatures[opcode];
>>>>>>> Stashed changes
        }

        public bool IsOpcodeCondition(byte opcode)
        {
            return opcode == 4;
        }

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
            "event_on",
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
