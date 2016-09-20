using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Tick
{
    #region RawData
    public DemoCommand command;
    public int tickNumber;
    public byte playerSlot;
    public int size;
    public int messageType;
    #endregion

    public DemoParser demo { get; private set; }

    public Tick(DemoParser demo)
    {
        this.demo = demo;
    }

    public void ParseTickData(byte[] data)
    {
        if (command == DemoCommand.DataTables)
        {
            ParseDataTables(data);
        }
        else if (command == DemoCommand.Signon || command == DemoCommand.Packet)
        {
            int currentIndex = 0, bytesRead = 0;
            while (currentIndex < data.Length)
            {
                messageType = 0;
                int length = 0;
                if (data != null)
                {
                    messageType = DataParser.ReadProtoInt(data, currentIndex, out bytesRead);
                    currentIndex += bytesRead; bytesRead = 0;
                    length = DataParser.ReadProtoInt(data, currentIndex, out bytesRead);
                    currentIndex += bytesRead; bytesRead = 0;
                }
                //Debug.Log("Number: " + tickNumber + " Command: " + command + "\nMessageType: " + (messageType >= 8 ? ((SVC_Messages)messageType).ToString() : ((NET_Messages)messageType).ToString()) + " MessageLength: " + length);

                byte[] cmdBytes = DataParser.ReadBytes(data, currentIndex, length);
                if ((SVC_Messages)messageType == SVC_Messages.svc_PacketEntities)
                {
                    ParsePacketEntities(cmdBytes);
                }
                else if ((SVC_Messages)messageType == SVC_Messages.svc_GameEventList)
                {
                    ParseGameEventList(cmdBytes);
                }
                else if ((SVC_Messages)messageType == SVC_Messages.svc_GameEvent)
                {
                    ParseGameEvent(cmdBytes);
                }
                else if ((SVC_Messages)messageType == SVC_Messages.svc_CreateStringTable)
                {
                    ParseCreateStringTable(cmdBytes);
                }
                else if ((SVC_Messages)messageType == SVC_Messages.svc_UpdateStringTable)
                {
                    ParseUpdateStringTable(cmdBytes);
                }
                else if ((NET_Messages)messageType == NET_Messages.net_Tick)
                {
                    ParseNetTick(cmdBytes);
                }
                currentIndex += length;
            }
        }
    }

    #region Message Parsers
    private void ParseDataTables(byte[] data)
    {
        DataTables dataTables = DataTables.Parse(data);

        MapEquipment(dataTables);

        BindEntities(dataTables);
    }
    private void MapEquipment(DataTables dataTables)
    {
        for (int i = 0; i < dataTables.serverClasses.Length; i++)
        {
            ServerClass sc = dataTables.serverClasses[i];

            if (sc.baseClasses.Length > 6 && sc.baseClasses[6].name == "CWeaponCSBase")
            {
                try { demo.equipmentMapping.Add(sc, (EquipmentElement)Enum.Parse(typeof(EquipmentElement), sc.dataTableName.Substring(3))); }
                catch (Exception e) { Debug.Log(e.ToString()); }
            }
        }
    }
    private void BindEntities(DataTables dataTables)
    {

    }
    private void ParsePacketEntities(byte[] data)
    {

    }
    private void ParseGameEventList(byte[] data)
    {
        demo.gameEventDescriptors = new Dictionary<int, EventDescriptor>();

        int currentIndex = 0;
        int bytesRead = 0;

        while (currentIndex < data.Length)
        {
            int desc = DataParser.ReadProtoInt(data, currentIndex, out bytesRead);
            currentIndex += bytesRead; bytesRead = 0;
            int wireType = desc & 7;
            int fieldNum = desc >> 3;

            if (wireType != 2 || fieldNum != 1) throw new Exception("GameEventList: WireType must equal 2 and FieldNum must equal 1");

            int descriptorLength = DataParser.ReadProtoInt(data, currentIndex, out bytesRead);
            currentIndex += bytesRead; bytesRead = 0;
            EventDescriptor descriptor = EventDescriptor.Parse(DataParser.ReadBytes(data, currentIndex, descriptorLength));
            demo.gameEventDescriptors.Add(descriptor.eventID, descriptor);
            currentIndex += descriptorLength;
        }

        /*for(int i = 0; i < demo.gameEventDescriptors.Count; i++)
        {
            string eventDebug = demo.gameEventDescriptors.Keys.ElementAt(i) + ": " + demo.gameEventDescriptors.Values.ElementAt(i).name + "\n";
            for(int j = 0; j < demo.gameEventDescriptors.Values.ElementAt(i).keys.Length; j++)
            {
                eventDebug += "\n" + demo.gameEventDescriptors.Values.ElementAt(i).keys[j].name;
            }
            Debug.Log(eventDebug);
        }*/
    }
    private void ParseGameEvent(byte[] data)
    {
        GameEvent gameEvent = GameEvent.Parse(data);

        if (demo.gameEventDescriptors == null) return;
        EventDescriptor eventDescriptor = demo.gameEventDescriptors[gameEvent.eventID];
        //if (Players.Count <= 0 && eventDescriptor.name != "player_connect") return;

        Dictionary<string, object> eventData = GameEvent.MapData(eventDescriptor, gameEvent);

        //if (!demo.uniqueEvents.ContainsKey(eventDescriptor)) demo.uniqueEvents.Add(eventDescriptor, eventData); //Debug Unique Events

        #region Event Handling
        //Used Events in BlazingBlace: 163 21 9 27 229 125 11 151 153 24 23 164 129 234 154 128 152 165 181 148 107 36 149 106 50 13 14 168 40 139 190 178 42 66 44 157 156 101 15 103 124 126 158 159 112 105 7 216 166 39 110 37 179 92
        #region Events used in recent Replay
        /*
        player_footstep (int userid = 6)
        weapon_fire (int userid = 3, string weapon = weapon_m4a1, bool silenced = False)
        player_hurt (int userid = 12, int attacker = 3, int health = 68, int armor = 0, string weapon = m4a1, int dmg_health = 32, int dmg_armor = 0, int hitgroup = 2)
        player_death (int userid = 12, int attacker = 3, int assister = 0, string weapon = m4a1, int weapon_itemid = 0, int weapon_fauxitemid = 18446744069414584336, int weapon_originalowner_xuid = 76561198122821247, bool headshot = True, int dominated = 0, int revenge = 0, int penetrated = 0, bool noreplay = False)
        weapon_zoom (int userid = 10)
        player_jump (int userid = 3)
        hltv_status (int clients = 0, int slots = 50, int proxies = 0, ? master = , int externaltotal = 0, int externallinked = 0)
        player_spawn (int userid = 12, int teamnum = 3)
        hltv_chase (int target1 = 2, int target2 = 11, int distance = 96, int theta = -30, int phi = -20, int inertia = 0, int ineye = 1)
        weapon_reload (int userid = 12)
        weapon_fire_on_empty (int userid = 12, string weapon = weapon_m4a1)
        player_team (int userid = 8, int team = 3, int oldteam = 0, bool disconnect = False, bool autoteam = False, bool silent = False, bool isbot = False)
        player_disconnect (int userid = 20, string reason = Kicked by Console, string name Yogi, string networkid BOT)
        decoy_started (int userid = 12, int entityid = 480, float x = 775.8956, float y = 2075.691, float z = 193.1547)
        decoy_detonate (int userid = 12, int entityid = 480, float x = 775.8956, float y = 2075.691, float z = 193.1547)
        cs_pre_restart ()
        round_prestart ()
        bomb_pickup (int userid = 7)
        round_start (int timelimit = 115, int fraglimit = 0, string objective = BOMB TARGET)
        round_poststart ()
        begin_new_match ()
        cs_round_start_beep ()
        cs_round_final_beep ()
        round_freeze_end ()
        round_announce_match_start ()
        other_death (int otherid = 97, string othertype = chicken, int attacker = 5, string weapon = knife_t, int weapon_itemid = 0, int weapon_fauxitemid = 18446744069414584379, int weapon_originalowner_xuid = 76561198138395904, bool headshot = False, int penetrated = 0)
        flashbang_detonate (int userid = 10, int entityid = 112, float x = 116.3785, float y = 1348.125, float z = 125.1487)
        player_blind (int userid = 5)
        buytime_ended ()
        bomb_dropped (int userid = 7, int entindex = 610)
        round_mvp (int userid = 4, int reason = 1, int musickkitmvps = 0)
        cs_win_panel_round (bool show_timer_defend = False, bool show_timer_attack = True, int timer_time = 41, int final_event = 8, string funfact_token = #funfact_kills_headshots, int funfact_player = 3, int funfact_data1 = 4, int funfact_data2 = 0, int funfact_data3 = 0)
        round_end (int winner = 3, int reason = 8, string message = #SFUI_Notice_CTs_Win, int legacy = 0, int player_count = 0)
        round_officially_ended ()
        hegrenade_detonate (int userid = 4, int entityid = 140, float x = 82.71462, float y = 1288.01, float z = 108.7554)
        smokegrenade_detonate (int userid = 10, int entityid = 121, float x = 124.7404, float y = 1538.201, float z = 104.3713)
        smokegrenade_expired (int userid = 10, int entityid = 121, float x = 124.7404, float y = 1538.201, float z = 104.3713)
        player_falldamage (int userid = 3, float damage = 5.775379)
        bomb_beginplant (int userid = 5, int site = 380)
        bomb_planted (int userid = 5, int site = 380)
        bomb_beep (int entindex = 521)
        inferno_startburn (int entityid = 109, float x = 716.717, float y = 1924.841, float z = 194.4628)
        inferno_expire (int entityid = 109, float x = 716.717, float y = 1924.841, float z = 194.4628)
        round_time_warning ()
        player_connect (string name = Lester, int index = 9, int userid = 22, string networkid = BOT, ? address = )
        round_announce_last_round_half ()
        bot_takeover (int userid = 7, int botid = 22, int index = 5)
        announce_phase_end ()
        bomb_exploded (int userid = 3, int site = 414)
        bomb_begindefuse (int userid = 3, bool haskit = False)
        bomb_defused (int userid = 3, int site = 380)
        round_announce_final ()
        cs_win_panel_match ()
        endmatch_cmm_start_reveal_items ()
        */
        #endregion
        if (eventDescriptor.name == "achievement_earned") ; //66 player achievement
        else if (eventDescriptor.name == "achievement_earned_local") ; //186 achievement splitscreenplayer
        else if (eventDescriptor.name == "achievement_event") ; //64 achievement_name cur_val max_val
        else if (eventDescriptor.name == "achievement_increment") ; //65 achievement_id cur_val max_val
        else if (eventDescriptor.name == "achievement_info_loaded") ; //173
        else if (eventDescriptor.name == "achievement_write_failed") ; //67
        else if (eventDescriptor.name == "add_bullet_hit_marker") ; //n101 userid bone pos_x pos_y pos_z ang_x ang_y ang_z start_x start_y start_z hit
        else if (eventDescriptor.name == "add_player_sonar_icon") ; //n100 userid pos_x pos_y pos_z
        else if (eventDescriptor.name == "ammo_pickup") ; //135 userid item index
        else if (eventDescriptor.name == "announce_phase_end") ; //110
        else if (eventDescriptor.name == "assassination_target_killed") ; //194 target killer
        else if (eventDescriptor.name == "begin_new_match") ; //50
        else if (eventDescriptor.name == "bomb_abortdefuse") ; //113 userid
        else if (eventDescriptor.name == "bomb_abortplant") ; //102 userid site
        else if (eventDescriptor.name == "bomb_beep") ; //124 entindex
        else if (eventDescriptor.name == "bomb_begindefuse") ; //112 userid haskit
        else if (eventDescriptor.name == "bomb_beginplant") ; //101 userid site
        else if (eventDescriptor.name == "bomb_defused") ; //104 userid site
        else if (eventDescriptor.name == "bomb_dropped") ; //106 userid entindex
        else if (eventDescriptor.name == "bomb_exploded") ; //105 userid site
        else if (eventDescriptor.name == "bomb_pickup") ; //107 userid
        else if (eventDescriptor.name == "bomb_planted") ; //103 userid site
        else if (eventDescriptor.name == "bonus_updated") ; //62 numadvanced numbronze numsilver numgold
        else if (eventDescriptor.name == "bot_takeover") ; //216 userid botid index
        else if (eventDescriptor.name == "break_breakable") ; //58 entindex userid material
        else if (eventDescriptor.name == "break_prop") ; //59 entindex userid
        else if (eventDescriptor.name == "bullet_impact") ; //162 userid x y z
        else if (eventDescriptor.name == "buymenu_close") ; //147 userid
        else if (eventDescriptor.name == "buymenu_open") ; //146 userid
        else if (eventDescriptor.name == "buytime_ended") ; //139
        else if (eventDescriptor.name == "cart_updated") ; //94
        else if (eventDescriptor.name == "client_disconnect") ; //191
        else if (eventDescriptor.name == "client_loadout_changed") ; //98
        else if (eventDescriptor.name == "cs_game_disconnected") ; //177
        else if (eventDescriptor.name == "cs_intermission") ; //111
        else if (eventDescriptor.name == "cs_match_end_restart") ; //180
        else if (eventDescriptor.name == "cs_pre_restart") ; //181
        else if (eventDescriptor.name == "cs_prev_next_spectator") ; //223 next
        else if (eventDescriptor.name == "cs_round_start_beep") ; //13
        else if (eventDescriptor.name == "cs_round_final_beep") ; //14
        else if (eventDescriptor.name == "cs_win_panel_match") ; //179 show_time_defend show_timer_attack timer_time final_event funfact_token funfact_player funfact_data1 funfact_data2 funfact_data3
        else if (eventDescriptor.name == "cs_win_panel_round") ; //178 show_timer_defend show_timer_attack timer_time final_event funfact_token funfact_player funfact_data1 funfact_data2 funfact_data3
        else if (eventDescriptor.name == "decoy_detonate") ; //156 userid entityid x y z
        else if (eventDescriptor.name == "decoy_firing") ; //161 userid entityid x y z
        else if (eventDescriptor.name == "decoy_started") ; //157 userid entityid x y z
        else if (eventDescriptor.name == "defuser_dropped") ; //108 entityid
        else if (eventDescriptor.name == "defuser_pickup") ; //109 entityid userid
        else if (eventDescriptor.name == "difficulty_changed") ; //54 newDifficulty oldDifficulty strDifficulty
        else if (eventDescriptor.name == "dm_bonus_weapon_start") ; //57 time wepID Pos
        else if (eventDescriptor.name == "door_moving") ; //167 entindex userid
        else if (eventDescriptor.name == "enable_restart_voting") ; //207 enable
        else if (eventDescriptor.name == "endmatch_cmm_start_reveal_items") ; //92
        else if (eventDescriptor.name == "endmatch_mapvote_selecting_map") ; //91 count slot1 slot2 slot3 slot4 slot5 slot6 slot7 slot8 slot9 slot10
        else if (eventDescriptor.name == "enter_bombzone") ; //140 userid hasbomb isplanted
        else if (eventDescriptor.name == "enter_buyzone") ; //137 userid canbuy
        else if (eventDescriptor.name == "enter_rescue_zone") ; //142 userid
        else if (eventDescriptor.name == "entity_killed") ; //61 entindex_killed entindex_attacker entindex_inflictor damagebits
        else if (eventDescriptor.name == "entity_visible") ; //76 userid subject classname entityname
        else if (eventDescriptor.name == "exit_bombzone") ; //141 userid hasbomb isplanted
        else if (eventDescriptor.name == "exit_buyzone") ; //138 userid canbuy
        else if (eventDescriptor.name == "exit_rescue_zone") ; //143 userid
        else if (eventDescriptor.name == "finale_start") ; //55 rushes
        else if (eventDescriptor.name == "flare_ignite_npc") ; //69 entindex
        else if (eventDescriptor.name == "flashbang_detonate") ; //152 userid entityid x y z
        else if (eventDescriptor.name == "freezecam_started") ; //184
        else if (eventDescriptor.name == "game_end") ; //35 winner
        else if (eventDescriptor.name == "game_init") ; //32
        else if (eventDescriptor.name == "game_messages") ; //56 target text
        else if (eventDescriptor.name == "game_newmap") ; //33 mapname
        else if (eventDescriptor.name == "game_start") ; //34 roundslimit timelimit fraglimit objective
        else if (eventDescriptor.name == "gameinstructor_draw") ; //73
        else if (eventDescriptor.name == "gameinstructor_nodraw") ; //74
        else if (eventDescriptor.name == "gameui_hidden") ; //19
        else if (eventDescriptor.name == "gc_connected") ; //96
        else if (eventDescriptor.name == "gg_bonus_grenade_achieved") ; //198 userid
        else if (eventDescriptor.name == "gg_final_weapon_achieved") ; //197 playerid
        else if (eventDescriptor.name == "gg_killed_enemy") ; //196 victimid attackerid dominated revenge bonus
        else if (eventDescriptor.name == "gg_leader") ; //200 playerid
        else if (eventDescriptor.name == "gg_player_impending_upgrade") ; //202 userid
        else if (eventDescriptor.name == "gg_player_levelup") ; //192 userid weaponrank weaponname
        else if (eventDescriptor.name == "gg_reset_round_start_sounds") ; //211 userid
        else if (eventDescriptor.name == "gg_team_leader") ; //201 playerid
        else if (eventDescriptor.name == "ggprogressive_player_levelup") ; //195 userid weaponrank weaponname
        else if (eventDescriptor.name == "ggtr_player_levelup") ; //193 userid weaponrank weaponname
        else if (eventDescriptor.name == "grenade_bounce") ; //150 userid
        else if (eventDescriptor.name == "hegrenade_detonate") ; //151 userid entityid x y z
        else if (eventDescriptor.name == "helicopter_grenade_punt_miss") ; //70
        else if (eventDescriptor.name == "hide_freezepanel") ; //183
        else if (eventDescriptor.name == "hltv_cameraman") ; //230 index
        else if (eventDescriptor.name == "hltv_changed_mode") ; //176 oldmode newmode obs_target
        else if (eventDescriptor.name == "hltv_changed_target") ; //238 mode old_target obs_target
        else if (eventDescriptor.name == "hltv_chase") ; //234 target1 target2 distance theta phi inertia ineye
        else if (eventDescriptor.name == "hltv_chat") ; //237 text
        else if (eventDescriptor.name == "hltv_fixed") ; //233 posx posy posz theta phi offset fov target
        else if (eventDescriptor.name == "hltv_message") ; //235 text
        else if (eventDescriptor.name == "hltv_rank_camera") ; //231 index rank target
        else if (eventDescriptor.name == "hltv_rank_entity") ; //232 index rank target
        else if (eventDescriptor.name == "hltv_status") ; //229 clients slots proxies master externaltotal externallinked
        else if (eventDescriptor.name == "hltv_title") ; //236 text
        else if (eventDescriptor.name == "hostage_call_for_help") ; //120 hostage
        else if (eventDescriptor.name == "hostage_follows") ; //114 userid hostage
        else if (eventDescriptor.name == "hostage_hurt") ; //115 userid hostage
        else if (eventDescriptor.name == "hostage_killed") ; //116 userid hostage
        else if (eventDescriptor.name == "hostage_rescued") ; //117 userid hostage site
        else if (eventDescriptor.name == "hostage_rescued_all") ; //119
        else if (eventDescriptor.name == "hostage_stops_following") ; //118 userid hostage
        else if (eventDescriptor.name == "hostname_changed") ; //53 hostname
        else if (eventDescriptor.name == "inferno_expire") ; //159 entityid x y z
        else if (eventDescriptor.name == "inferno_extinguish") ; //160 entityid x y z
        else if (eventDescriptor.name == "inferno_startburn") ; //158 entityid x y z
        else if (eventDescriptor.name == "inspect_weapon") ; //131 userid
        else if (eventDescriptor.name == "instructor_server_hint_create") ; //78 hint_name hint_replace_key hint_target hint_activator_userid hint_timeout hint_icon_onscreen hint_icon_offscreen hint_caption hint_activator_caption hint_color hint_icon_offset hint_range hint_flags hint_binding hint_gamepad_binding hint_allow_nodraw hint_nooffscreen hint_forcecaption hint_local_player_only
        else if (eventDescriptor.name == "instructor_server_hint_stop") ; //79 hint_name
        else if (eventDescriptor.name == "inventory_updated") ; //93
        else if (eventDescriptor.name == "item_equip") ; //136 userid item canzoom hassilencer issilenced hastracers weptype ispainted
        else if (eventDescriptor.name == "item_found") ; //187 player quality method itemdef itemid
        else if (eventDescriptor.name == "item_pickup") ; //134 userid item silent
        else if (eventDescriptor.name == "item_purchase") ; //100 userid team weapon
        else if (eventDescriptor.name == "item_schema_initialized") ; //97
        else if (eventDescriptor.name == "items_gifted") ; //20
        else if (eventDescriptor.name == "jointeam_failed") ; //220 userid reason
        else if (eventDescriptor.name == "map_transition") ; //75
        else if (eventDescriptor.name == "match_end_conditions") ; //189 frags max_rounds win_rounds time
        else if (eventDescriptor.name == "material_default_complete") ; //222
        else if (eventDescriptor.name == "mb_input_lock_cancel") ; //170
        else if (eventDescriptor.name == "mb_input_lock_success") ; //169
        else if (eventDescriptor.name == "molotov_detonate") ; //155 userid x y z
        else if (eventDescriptor.name == "nav_blocked") ; //171 area blocked
        else if (eventDescriptor.name == "nav_generate") ; //172
        else if (eventDescriptor.name == "nextlevel_changed") ; //225 nextlevel
        else if (eventDescriptor.name == "other_death") ; //99 otherid othertype attacker weapon weapon_itemid weapon_fauxitemid weapon_originalowner_xuid headshot penetrated
        else if (eventDescriptor.name == "physgun_pickup") ; //68 entindex
        else if (eventDescriptor.name == "player_activate") ; //10 userid
        else if (eventDescriptor.name == "player_avenged_teammate") ; //185 avenger_id avenged_player_id
        else if (eventDescriptor.name == "player_blind") ; //165 userid
        else if (eventDescriptor.name == "player_changename") ; //30 userid oldname newname
        else if (eventDescriptor.name == "player_chat") ; //25 teamonly userid text
        else if (eventDescriptor.name == "player_class") ; //22 userid class
        else if (eventDescriptor.name == "player_connect") Debug.Log("player_connect\nName: " + eventData["name"] + " NetworkID: " + eventData["networkid"]); //7 name index userid networkid address
        else if (eventDescriptor.name == "player_connect_full") ; //11 userid index
        else if (eventDescriptor.name == "player_death") ; //23 userid attacker assister weapon weapon_itemid weapon_fauxitemid weapon_originalowner_xuid headshot dominated revenge penetrated
        else if (eventDescriptor.name == "player_decal") ; //60 userid
        else if (eventDescriptor.name == "player_disconnect") ; //9 userid reason name networkid
        else if (eventDescriptor.name == "player_falldamage") ; //166 userid damage
        else if (eventDescriptor.name == "player_footstep") ; //163 userid
        else if (eventDescriptor.name == "player_given_c4") ; //210 userid
        else if (eventDescriptor.name == "player_hintmessage") ; //31 hintmessage
        else if (eventDescriptor.name == "player_hurt") ; //24 userid attacker health armor weapon dmg_health dmg_armor hitgroup
        else if (eventDescriptor.name == "player_info") ; //8 name index userid networkid bot
        else if (eventDescriptor.name == "player_jump") ; //164 userid
        else if (eventDescriptor.name == "player_radio") ; //123 userid slot
        else if (eventDescriptor.name == "player_reset_vote") ; //206 userid vote
        else if (eventDescriptor.name == "player_say") ; //12 userid text
        else if (eventDescriptor.name == "player_score") ; //26 userid kills deaths score
        else if (eventDescriptor.name == "player_shoot") ; //28 userid weapon mode
        else if (eventDescriptor.name == "player_spawn") ; //27 userid teamnum
        else if (eventDescriptor.name == "player_spawned") ; //133 userid inrestart
        else if (eventDescriptor.name == "player_stats_updated") ; //63 forceupload
        else if (eventDescriptor.name == "player_team") Debug.Log("player_team\nUserID: " + eventData["userid"] + " Team: " + eventData["team"] + " OldTeam: " + eventData["oldteam"] + " Disconnect: " + eventData["disconnect"] + " Autoteam: " + eventData["autoteam"] + " IsBot: " + eventData["isbot"]); //21 userid team oldteam disconnect autoteam silent isbot
        else if (eventDescriptor.name == "player_use") ; //29 userid entity
        else if (eventDescriptor.name == "ragdoll_dissolved") ; //72 entindex
        else if (eventDescriptor.name == "read_game_titledata") ; //80 controllerId
        else if (eventDescriptor.name == "repost_xbox_achievements") ; //188 splitscreenplayer
        else if (eventDescriptor.name == "reset_game_titledata") ; //82 controllerId
        else if (eventDescriptor.name == "reset_player_controls") ; //219
        else if (eventDescriptor.name == "round_announce_final") ; //38
        else if (eventDescriptor.name == "round_announce_last_round_half") ; //39
        else if (eventDescriptor.name == "round_announce_match_point") ; //37
        else if (eventDescriptor.name == "round_announce_match_start") ; //40
        else if (eventDescriptor.name == "round_announce_warmup") ; //41
        else if (eventDescriptor.name == "round_end") ; //42 winner reason message
        else if (eventDescriptor.name == "round_end_upload_stats") ; //43
        else if (eventDescriptor.name == "round_freeze_end") ; //168
        else if (eventDescriptor.name == "round_officially_ended") ; //44
        else if (eventDescriptor.name == "round_mvp") ; //190 userid reason
        else if (eventDescriptor.name == "round_poststart") ; //149
        else if (eventDescriptor.name == "round_prestart") ; //148
        else if (eventDescriptor.name == "round_start") ; //36 timelimit fraglimit objective
        else if (eventDescriptor.name == "round_start_pre_entity") ; //51
        else if (eventDescriptor.name == "round_time_warning") ; //15
        else if (eventDescriptor.name == "seasoncoin_levelup") ; //226 player category rank
        else if (eventDescriptor.name == "server_addban") ; //5 name userid networkid ip duration by kicked
        else if (eventDescriptor.name == "server_cvar") ; //3 cvarname cvarvalue
        else if (eventDescriptor.name == "server_message") ; //4 text
        else if (eventDescriptor.name == "server_pre_shutdown") ; //1 reason
        else if (eventDescriptor.name == "server_removeban") ; //6 networkid ip by
        else if (eventDescriptor.name == "server_shutdown") ; //2 reason
        else if (eventDescriptor.name == "server_spawn") ; //0 hostname address port game mapname maxplayers os dedicated official password
        else if (eventDescriptor.name == "set_instructor_group_enabled") ; //77 group enabled
        else if (eventDescriptor.name == "sfuievent") ; //208 action data slot
        else if (eventDescriptor.name == "show_freezepanel") ; //182 victim killer hits_taken damage_taken hits_given damage_given
        else if (eventDescriptor.name == "silencer_detach") ; //130 userid
        else if (eventDescriptor.name == "silencer_off") ; //144 userid
        else if (eventDescriptor.name == "silencer_on") ; //145 userid
        else if (eventDescriptor.name == "smokegrenade_detonate") ; //153 userid entityid x y z
        else if (eventDescriptor.name == "smokegrenade_expired") ; //154 userid entityid x y z
        else if (eventDescriptor.name == "spec_mode_updated") ; //175 userid
        else if (eventDescriptor.name == "spec_target_updated") ; //174 userid
        else if (eventDescriptor.name == "start_halftime") ; //228
        else if (eventDescriptor.name == "start_vote") ; //209 userid type vote_parameter
        else if (eventDescriptor.name == "store_pricesheet_updated") ; //95
        else if (eventDescriptor.name == "survival_announce_phase") ; //n58 phase
        else if (eventDescriptor.name == "switch_team") ; //199 numPlayers numSpectators avg_rank numTSlotsFree numCTSlotsFree
        else if (eventDescriptor.name == "tagrenade_detonate") ; //n162 userid entityid x y z
        else if (eventDescriptor.name == "teamplay_broadcast_audio") ; //18 team sound
        else if (eventDescriptor.name == "teamplay_round_start") ; //52 full_reset
        else if (eventDescriptor.name == "team_info") ; //16 teamid teamname
        else if (eventDescriptor.name == "team_score") ; //17 teamid score
        else if (eventDescriptor.name == "teamchange_pending") ; //221 userid toteam
        else if (eventDescriptor.name == "tournament_reward") ; //227 defindex totalrewards accountid
        else if (eventDescriptor.name == "tr_exit_hint_trigger") ; //215
        else if (eventDescriptor.name == "tr_mark_best_time") ; //214 time
        else if (eventDescriptor.name == "tr_mark_complete") ; //213 complete
        else if (eventDescriptor.name == "tr_player_flashbanged") ; //212 userid
        else if (eventDescriptor.name == "tr_show_exit_msgbox") ; //218 userid
        else if (eventDescriptor.name == "tr_show_finish_msgbox") ; //217 userid
        else if (eventDescriptor.name == "trial_time_expired") ; //204 slot
        else if (eventDescriptor.name == "ugc_file_download_start") ; //49 hcontent published_file_id
        else if (eventDescriptor.name == "ugc_map_download_error") ; //47 published_file_id error_code
        else if (eventDescriptor.name == "ugc_map_download_finished") ; //48 hcontent
        else if (eventDescriptor.name == "ugc_map_info_received") ; //45 published_file_id
        else if (eventDescriptor.name == "ugc_map_unsubscribed") ; //46 published_file_id
        else if (eventDescriptor.name == "update_matchmaking_stats") ; //205
        else if (eventDescriptor.name == "user_data_downloaded") ; //71
        else if (eventDescriptor.name == "verify_client_hit") ; //n102 userid pos_x pos_y pos_z timestamp
        else if (eventDescriptor.name == "vip_escaped") ; //121 userid
        else if (eventDescriptor.name == "vip_killed") ; //122 userid attacker
        else if (eventDescriptor.name == "vote_cast") ; //89 vote_option team entityid
        else if (eventDescriptor.name == "vote_changed") ; //86 vote_option1 vote_option2 vote_option3 vote_option4 vote_option5 potentialVotes
        else if (eventDescriptor.name == "vote_ended") ; //84
        else if (eventDescriptor.name == "vote_failed") ; //88 team
        else if (eventDescriptor.name == "vote_options") ; //90 count option1 option2 option3 option4 option5
        else if (eventDescriptor.name == "vote_passed") ; //87 details param1 team
        else if (eventDescriptor.name == "vote_started") ; //85 issue param1 team initiator
        else if (eventDescriptor.name == "weapon_fire") ; //125 userid weapon silenced
        else if (eventDescriptor.name == "weapon_fire_on_empty") ; //126 userid weapon
        else if (eventDescriptor.name == "weapon_outofammo") ; //127 userid
        else if (eventDescriptor.name == "weapon_reload") ; //128 userid
        else if (eventDescriptor.name == "weapon_reload_database") ; //83
        else if (eventDescriptor.name == "weapon_zoom") ; //129 userid
        else if (eventDescriptor.name == "weapon_zoom_rifle") ; //132 userid
        else if (eventDescriptor.name == "write_game_titledata") ; //81 controllerId
        else if (eventDescriptor.name == "write_profile_data") ; //203
        #endregion
    }
    private void ParseCreateStringTable(byte[] data)
    {

    }
    private void ParseUpdateStringTable(byte[] data)
    {

    }
    private void ParseNetTick(byte[] data)
    {

    }
    #endregion
}

public class DataTables
{
    public SendTable[] sendTables;
    public ServerClass[] serverClasses;
    List<ExcludeEntry> currentExcludes = new List<ExcludeEntry>();
    List<ServerClass> currentBaseClasses = new List<ServerClass>();

    public static DataTables Parse(byte[] data)
    {
        DataTables dataTables = new DataTables();

        int currentIndex = 0;
        int bytesRead = 0;

        List<SendTable> sendTables = new List<SendTable>();
        List<ServerClass> serverClasses = new List<ServerClass>();

        bool endSendTable = false;
        while (!endSendTable)
        {
            SVC_Messages type = (SVC_Messages)(DataParser.ReadProtoInt(data, currentIndex, out bytesRead));
            currentIndex += bytesRead; bytesRead = 0;
            if (type != SVC_Messages.svc_SendTable) throw new Exception("DataTables: Incorrect SVC Message type " + type + " should be SendTable");

            int tableLength = DataParser.ReadProtoInt(data, currentIndex, out bytesRead);
            currentIndex += bytesRead; bytesRead = 0;
            SendTable sendTable = SendTable.Parse(DataParser.ReadBytes(data, currentIndex, tableLength));
            currentIndex += tableLength;

            //Debug.Log("SendTable: " + sendTable.name);
            endSendTable = sendTable.isEnd;
            if (!endSendTable) sendTables.Add(sendTable);
        }

        int serverClassCount = BitConverter.ToUInt16(data, currentIndex);
        currentIndex += 2;

        //Debug.Log("SendTables: " + sendTables.Count + " ServerClasses: " + serverClassCount);
        for(int i = 0; i < serverClassCount; i++)
        {
            ServerClass serverClass = ServerClass.Parse(data, currentIndex, out bytesRead);
            currentIndex += bytesRead; bytesRead = 0;

            //Debug.Log("ServerClass" + i + ": " + serverClass.name + ", " + serverClass.dataTableName + ", " + serverClass.classID + " <= " + serverClassCount);
            if (serverClass.classID >= serverClassCount) throw new Exception("Class index out of bounds");

            serverClass.dataTableID = sendTables.FindIndex(item => item.name == serverClass.dataTableName);

            serverClasses.Add(serverClass);
        }

        dataTables.sendTables = sendTables.ToArray();
        dataTables.serverClasses = serverClasses.ToArray();

        for (int i = 0; i < dataTables.serverClasses.Length; i++)
            dataTables.FlattenDataTable(i);

        return dataTables;
    }

    public void FlattenDataTable(int serverClassIndex)
    {
        SendTable table = sendTables[serverClasses[serverClassIndex].dataTableID];

        currentExcludes.Clear();
        currentBaseClasses = new List<ServerClass>();

        GatherExcludesAndBaseClasses(table, true);

        serverClasses[serverClassIndex].baseClasses = currentBaseClasses.ToArray();

        GatherProperties(table, serverClassIndex, "");

        List<FlattenedPropertyEntry> flattenedProperties = serverClasses[serverClassIndex].flattenedProperties;

        List<int> priorities = new List<int>();
        priorities.Add(64);
        priorities.AddRange(flattenedProperties.Select(item => item.property.priority).Distinct());
        priorities.Sort();

        int start = 0;
        for(int priorityIndex = 0; priorityIndex < priorities.Count; priorityIndex++)
        {
            int priority = priorities[priorityIndex];
            
            while (true)
            {
                int currentProperty = start;

                while(currentProperty < flattenedProperties.Count)
                {
                    SendTableProperty property = flattenedProperties[currentProperty].property;

                    if (property.priority == priority || (priority == 64 && (property.flags & SendTableProperty.SendPropertyFlag.ChangesOften) != 0))
                    {
                        if(start != currentProperty)
                        {
                            FlattenedPropertyEntry temp = flattenedProperties[start];
                            flattenedProperties[start] = flattenedProperties[currentProperty];
                            flattenedProperties[currentProperty] = temp;
                        }

                        start++;
                        break;
                    }
                    currentProperty++;
                }

                if (currentProperty >= flattenedProperties.Count)
                    break;
            }
        }
    }

    public void GatherExcludesAndBaseClasses(SendTable table, bool collectBaseClasses)
    {
        currentExcludes.AddRange(table.properties.Where(item => (item.flags & SendTableProperty.SendPropertyFlag.Exclude) != 0).Select(item => new ExcludeEntry(item.name, item.dataTableName, table.name)));

        foreach(SendTableProperty property in table.properties.Where(item => item.type == SendTableProperty.SendPropertyType.DataTable))
        {
            if(collectBaseClasses && property.name == "baseclass")
            {
                GatherExcludesAndBaseClasses(sendTables.FirstOrDefault(item => item.name == property.dataTableName), true);
                currentBaseClasses.Add(serverClasses.Single(item => item.dataTableName == property.dataTableName));
            }
            else
            {
                GatherExcludesAndBaseClasses(sendTables.FirstOrDefault(item => item.name == property.dataTableName), false);
            }
        }
    }

    public void GatherProperties(SendTable table, int serverClassIndex, string prefix)
    {
        serverClasses[serverClassIndex].flattenedProperties.AddRange(IteratePropertiesInGather(table, serverClassIndex, new List<FlattenedPropertyEntry>(), prefix));
    }
    public List<FlattenedPropertyEntry> IteratePropertiesInGather(SendTable table, int serverClassIndex, List<FlattenedPropertyEntry> flattenedProperties, string prefix)
    {
        for(int i = 0; i < table.properties.Length; i++)
        {
            if ((table.properties[i].flags & SendTableProperty.SendPropertyFlag.InsideArray) != 0 || (table.properties[i].flags & SendTableProperty.SendPropertyFlag.Exclude) != 0 || currentExcludes.Exists(item => table.name == item.dtName && table.properties[i].name == item.varName))
                continue;

            if(table.properties[i].type == SendTableProperty.SendPropertyType.DataTable)
            {
                if ((table.properties[i].flags & SendTableProperty.SendPropertyFlag.Collapsible) != 0) IteratePropertiesInGather(sendTables.FirstOrDefault(item => item.name == table.properties[i].dataTableName), serverClassIndex, flattenedProperties, prefix);
                else
                {
                    string nfix = prefix + ((table.properties[i].name.Length > 0) ? table.properties[i].name + "." : "");
                    GatherProperties(sendTables.FirstOrDefault(item => item.name == table.properties[i].dataTableName), serverClassIndex, nfix);
                }
            }
            else
            {
                if(table.properties[i].type == SendTableProperty.SendPropertyType.Array)
                {
                    flattenedProperties.Add(new FlattenedPropertyEntry(prefix + table.properties[i].name, table.properties[i], table.properties[i - 1]));
                }
                else
                {
                    flattenedProperties.Add(new FlattenedPropertyEntry(prefix + table.properties[i].name, table.properties[i], null));
                }
            }
        }

        return flattenedProperties;
    }
}
public class SendTable
{
    public SendTableProperty[] properties;
    public string name;
    public bool isEnd;
    public bool needsDecoder;

    public static SendTable Parse(byte[] data)
    {
        SendTable table = new SendTable();
        List<SendTableProperty> properties = new List<SendTableProperty>();

        int currentIndex = 0;
        int bytesRead = 0;
        while(currentIndex < data.Length)
        {
            int desc = DataParser.ReadProtoInt(data, currentIndex, out bytesRead);
            currentIndex += bytesRead; bytesRead = 0;
            int wireType = desc & 7;
            int fieldNum = desc >> 3;

            if (wireType == 2)
            {
                if (fieldNum == 2)
                {
                    table.name = DataParser.ReadProtoString(data, currentIndex, out bytesRead);
                    currentIndex += bytesRead; bytesRead = 0;
                }
                else if (fieldNum == 4)
                {
                    int propertyLength = DataParser.ReadProtoInt(data, currentIndex, out bytesRead);
                    currentIndex += bytesRead; bytesRead = 0;
                    properties.Add(SendTableProperty.Parse(DataParser.ReadBytes(data, currentIndex, propertyLength)));
                    currentIndex += propertyLength;
                }
                else throw new Exception("DataTable: Unknown FieldNum and WireType combination");
            }
            else if (wireType == 0)
            {
                int value = DataParser.ReadProtoInt(data, currentIndex, out bytesRead);
                currentIndex += bytesRead; bytesRead = 0;

                if (fieldNum == 1)
                {
                    table.isEnd = value != 0;
                }
                else if (fieldNum == 3)
                {
                    table.needsDecoder = value != 0;
                }
            }
            else throw new Exception("DataTable: Unexpected WireType");
        }
        table.properties = properties.ToArray();

        string sendTableDebug = "SendTable: " + table.name + "\n";
        for (int i = 0; i < table.properties.Length; i++)
        {
            sendTableDebug += "\nType: " + table.properties[i].type + " Name: " + table.properties[i].name + " DataTableName: " + table.properties[i].dataTableName;
        }
        Debug.Log(sendTableDebug);
        return table;
    }
}
public class SendTableProperty
{
    public SendPropertyFlag flags;
    public string name;
    public string dataTableName;
    public float lowValue;
    public float highValue;
    public int numberOfBits;
    public int numberOfElements;
    public int priority;
    public SendPropertyType type;

    public static SendTableProperty Parse(byte[] data)
    {
        SendTableProperty property = new SendTableProperty();

        int currentIndex = 0;
        int bytesRead = 0;
        while(currentIndex < data.Length)
        {
            int desc = DataParser.ReadProtoInt(data, currentIndex, out bytesRead);
            currentIndex += bytesRead; bytesRead = 0;
            int wireType = desc & 7;
            int fieldNum = desc >> 3;

            if (wireType == 2)
            {
                if (fieldNum == 2)
                {
                    property.name = DataParser.ReadProtoString(data, currentIndex, out bytesRead);
                    currentIndex += bytesRead; bytesRead = 0;
                }
                else if (fieldNum == 5)
                {
                    property.dataTableName = DataParser.ReadProtoString(data, currentIndex, out bytesRead);
                    currentIndex += bytesRead; bytesRead = 0;
                }
                else throw new Exception("DataTableProperty: Unknown WireType and FieldNum combination");
            }
            else if (wireType == 0)
            {
                int value = DataParser.ReadProtoInt(data, currentIndex, out bytesRead);
                currentIndex += bytesRead; bytesRead = 0;

                if (fieldNum == 1)
                {
                    property.type = (SendPropertyType)value;
                }
                else if (fieldNum == 3)
                {
                    property.flags = (SendPropertyFlag)value;
                }
                else if (fieldNum == 4)
                {
                    property.priority = value;
                }
                else if (fieldNum == 6)
                {
                    property.numberOfElements = value;
                }
                else if (fieldNum == 9)
                {
                    property.numberOfBits = value;
                }
            }
            else if (wireType == 5)
            {
                float value = BitConverter.ToSingle(data, currentIndex);
                currentIndex += 4;

                if (fieldNum == 7)
                {
                    property.lowValue = value;
                }
                if (fieldNum == 8)
                {
                    property.highValue = value;
                }
            }
            else throw new Exception("DataTableProperty: Unknown WireType");
        }

        return property;
    }

    public enum SendPropertyType
    {
        Int = 0,
        Float = 1,
        Vector = 2,
        VectorXY = 3,
        String = 4,
        Array = 5,
        DataTable = 6,
        Int64 = 7,
    }
    [Flags]
    public enum SendPropertyFlag
    {
        /// <summary>
        /// Unsigned integer data.
        /// </summary>
        Unsigned = (1 << 0),
        /// <summary>
        /// If this is set, the float/vector is treated like a world coordinate. 
        /// Note that the bit count is ignored in this case.
        /// </summary>
        Coord = (1 << 1),
        /// <summary>
        /// For floating point, don't scale into range, just take value as is
        /// </summary>
        NoScale = (1 << 2),
        /// <summary>
        /// For floating point, limit high value to range minus one bit unit
        /// </summary>
        RoundDown = (1 << 3),
        ///<summary>
        /// For floating point, limit low value to range minus one bit unit
        ///</summary>
        RoundUp = (1 << 4),
        ///<summary>
        /// If this is set, the vector is treated like a normal (only valid for vectors)
        ///</summary>
        Normal = (1 << 5),
        ///<summary>
        /// This is an exclude prop (not excludED, but it points at another prop to be excluded).
        ///</summary>
        Exclude = (1 << 6),
        ///<summary>
        /// Use XYZ/Exponent encoding for vectors.
        ///</summary>
        XYZE = (1 << 7),
        ///<summary>
        /// This tells us that the property is inside an array, so it shouldn't be put into the
        /// flattened property list. Its array will point at it when it needs to.
        ///</summary>
        InsideArray = (1 << 8),
        ///<summary>
        /// Set for datatable props using one of the default datatable proxies like
        /// SendProxy_DataTableToDataTable that always send the data to all clients.
        ///</summary>
        ProxyAlwaysYes = (1 << 9),
        ///<summary>
        /// Set automatically if SPROP_VECTORELEM is used.
        ///</summary>
        IsVectorElement = (1 << 10),
        ///<summary>
        /// Set automatically if it's a datatable with an offset of 0 that doesn't change the pointer
        /// (ie: for all automatically-chained base classes).
        /// In this case, it can get rid of this SendPropDataTable altogether and spare the
        /// trouble of walking the hierarchy more than necessary.
        ///</summary>
        Collapsible = (1 << 11),
        ///<summary>
        /// Like SPROP_COORD, but special handling for multiplayer games
        ///</summary>
        CoordMp = (1 << 12),
        /// <summary>
        /// Like SPROP_COORD, but special handling for multiplayer games where the fractional component only gets a 3 bits instead of 5
        /// </summary>
        CoordMpLowPrecision = (1 << 13),
        /// <summary>
        /// SPROP_COORD_MP, but coordinates are rounded to integral boundaries
        /// </summary>
        CoordMpIntegral = (1 << 14),
        /// <summary>
        /// Like SPROP_COORD, but special encoding for cell coordinates that can't be negative, bit count indicate maximum value
        /// </summary>
        CellCoord = (1 << 15),
        /// <summary>
        /// Like SPROP_CELL_COORD, but special handling where the fractional component only gets a 3 bits instead of 5
        /// </summary>
        CellCoordLowPrecision = (1 << 16),
        /// <summary>
        /// SPROP_CELL_COORD, but coordinates are rounded to integral boundaries
        /// </summary>
        CellCoordIntegral = (1 << 17),
        ///<summary>
        /// this is an often changed field, moved to head of sendtable so it gets a small index
        ///</summary>
        ChangesOften = (1 << 18),
        /// <summary>
        /// use var int encoded (google protobuf style), note you want to include SPROP_UNSIGNED if needed, its more efficient
        /// </summary>
        VarInt = (1 << 19)
    }
}
public class ServerClass
{
    public int classID;
    public int dataTableID;
    public string name;
    public string dataTableName;

    public List<FlattenedPropertyEntry> flattenedProperties = new List<FlattenedPropertyEntry>();
    public ServerClass[] baseClasses;

    public static ServerClass Parse(byte[] data, int startIndex, out int bytesRead)
    {
        ServerClass serverClass = new ServerClass();

        int stringBytes;

        bytesRead = 0;
        serverClass.classID = BitConverter.ToUInt16(data, startIndex);
        bytesRead += 2;
        serverClass.name = DataParser.ReadDataTableString(data, startIndex + bytesRead, out stringBytes);
        bytesRead += stringBytes; stringBytes = 0;
        serverClass.dataTableName = DataParser.ReadDataTableString(data, startIndex + bytesRead, out stringBytes);
        bytesRead += stringBytes; stringBytes = 0;

        Debug.Log("ServerClass\n\nName: " + serverClass.name + " DataTableName: " + serverClass.dataTableName);
        return serverClass;
    }
}
public class ExcludeEntry
{
    public string varName;
    public string dtName;
    public string excludingDT;

    public ExcludeEntry(string varName, string dtName, string excludingDT)
    {
        this.varName = varName;
        this.dtName = dtName;
        this.excludingDT = excludingDT;
    }
}
public class FlattenedPropertyEntry
{
    public string propertyName;
    public SendTableProperty property;
    public SendTableProperty arrayElementProperty;

    public FlattenedPropertyEntry(string propertyName, SendTableProperty property, SendTableProperty arrayElementProperty)
    {
        this.propertyName = propertyName;
        this.property = property;
        this.arrayElementProperty = arrayElementProperty;
    }
}

public class PacketEntities
{
    public int maxEntries;
    public int updatedEntries;
    public bool isDelta;
    public bool updateBaseLine;
    //private int _IsDelta;
    //public bool IsDelta { get { return _IsDelta != 0; } }
    //private int _UpdateBaseline;
    //public bool UpdateBaseline { get { return _UpdateBaseline != 0; } }
    public int baseline;
    public int deltaFrom;

    Tick tick;

    private PacketEntities(Tick tick)
    {
        this.tick = tick;
    }

    public static PacketEntities Parse(byte[] data, Tick tick)
    {
        PacketEntities packetEntities = new PacketEntities(tick);

        int currentIndex = 0;
        int bytesRead = 0;

        while(currentIndex < data.Length)
        {
            int desc = DataParser.ReadProtoInt(data, currentIndex, out bytesRead);
            currentIndex += bytesRead; bytesRead = 0;
            int wireType = desc & 7;
            int fieldNum = desc >> 3;

            if (fieldNum == 7 && wireType == 2)
            {
                int dataLength = DataParser.ReadProtoInt(data, currentIndex, out bytesRead);
                currentIndex += bytesRead; bytesRead = 0;
                packetEntities.RetrieveEntityData(DataParser.ReadBytes(data, currentIndex, dataLength));
                currentIndex += dataLength;
            }

            if (wireType != 0)
            {
                int value = DataParser.ReadProtoInt(data, currentIndex, out bytesRead);
                currentIndex += bytesRead; bytesRead = 0;

                if (fieldNum == 1) packetEntities.maxEntries = value;
                else if (fieldNum == 2) packetEntities.updatedEntries = value;
                else if (fieldNum == 3) packetEntities.isDelta = value != 0;
                else if (fieldNum == 4) packetEntities.updateBaseLine = value != 0;
                else if (fieldNum == 5) packetEntities.baseline = value;
                else if (fieldNum == 6) packetEntities.deltaFrom = value;
            }
        }

        return packetEntities;
    }

    private void RetrieveEntityData(byte[] data)
    {
        int currentBitIndex = 0;
        int bitsRead = 0;
        int currentEntity = -1;

        for(int i = 0; i < updatedEntries; i++)
        {
            currentEntity += 1 + (int)DataParser.ReadUBitInt(data, currentBitIndex, out bitsRead);
            currentBitIndex += bitsRead; bitsRead = 0;

            bool currentFlag = DataParser.ReadBit(data, currentBitIndex);
            currentBitIndex += 1;
            if(!currentFlag)
            {
                currentFlag = DataParser.ReadBit(data, currentBitIndex);
                currentBitIndex += 1;
                if(currentFlag)
                {
                    //Create Entity
                }
                else
                {
                    //Update Entity
                }
            }
            else
            {
                //Destroy Entity
            }
        }
    }
}

public class EventDescriptor
{
    public int eventID;
    public string name;
    public EventKey[] keys;

    public static EventDescriptor Parse(byte[] data)
    {
        EventDescriptor descriptor = new EventDescriptor();
        List<EventKey> keys = new List<EventKey>();

        int currentIndex = 0;
        int bytesRead = 0;

        while (currentIndex < data.Length)
        {
            int desc = DataParser.ReadProtoInt(data, currentIndex, out bytesRead);
            currentIndex += bytesRead; bytesRead = 0;
            int wireType = desc & 7;
            int fieldNum = desc >> 3;

            if (wireType == 0 && fieldNum == 1)
            {
                descriptor.eventID = DataParser.ReadProtoInt(data, currentIndex, out bytesRead);
                currentIndex += bytesRead; bytesRead = 0;
            }
            else if (wireType == 2 && fieldNum == 2)
            {
                descriptor.name = DataParser.ReadProtoString(data, currentIndex, out bytesRead);
                currentIndex += bytesRead; bytesRead = 0;
            }
            else if (wireType == 2 && fieldNum == 3)
            {
                int keyLength = DataParser.ReadProtoInt(data, currentIndex, out bytesRead);
                currentIndex += bytesRead; bytesRead = 0;
                keys.Add(EventKey.Parse(DataParser.ReadBytes(data, currentIndex, keyLength)));
                currentIndex += keyLength;
            }
            else throw new Exception("GameEventListDescriptor: Unknown WireType and FieldNum combination");
        }
        descriptor.keys = keys.ToArray();

        return descriptor;
    }
}
public class EventKey
{
    public int type;
    public string name;

    public static EventKey Parse(byte[] data)
    {
        EventKey key = new EventKey();

        int currentIndex = 0;
        int bytesRead = 0;

        while(currentIndex < data.Length)
        {
            int desc = DataParser.ReadProtoInt(data, currentIndex, out bytesRead);
            currentIndex += bytesRead; bytesRead = 0;
            int wireType = desc & 7;
            int fieldNum = desc >> 3;

            if (wireType == 0 && fieldNum == 1)
            {
                key.type = DataParser.ReadProtoInt(data, currentIndex, out bytesRead);
                currentIndex += bytesRead; bytesRead = 0;
            }
            else if (wireType == 2 && fieldNum == 2)
            {
                key.name = DataParser.ReadProtoString(data, currentIndex, out bytesRead);
                currentIndex += bytesRead; bytesRead = 0;
            }
            else throw new Exception("GameEventListDescriptorKey: Unknown WireType and FieldNum combination");
        }
        
        return key;
    }

    /*public override bool Equals(object obj)
    {
        return (obj is EventKey) ? (((EventKey)obj).type == type && ((EventKey)obj).name.Equals(name, StringComparison.InvariantCultureIgnoreCase)) : false;
    }*/
}

public class GameEvent
{
    public string eventName;
    public int eventID;
    public object[] keys;

    public static GameEvent Parse(byte[] data)
    {
        GameEvent gameEvent = new GameEvent();
        List<object> keys = new List<object>();

        int currentIndex = 0;
        int bytesRead = 0;

        while (currentIndex < data.Length)
        {
            int desc = DataParser.ReadProtoInt(data, currentIndex, out bytesRead);
            currentIndex += bytesRead; bytesRead = 0;
            int wireType = desc & 7;
            int fieldNum = desc >> 3;
            if ((wireType == 2) && (fieldNum == 1))
            {
                gameEvent.eventName = DataParser.ReadProtoString(data, currentIndex, out bytesRead);
                currentIndex += bytesRead; bytesRead = 0;
            }
            else if ((wireType == 0) && (fieldNum == 2))
            {
                gameEvent.eventID = DataParser.ReadProtoInt(data, currentIndex, out bytesRead);
                currentIndex += bytesRead; bytesRead = 0;
            }
            else if ((wireType == 2) && (fieldNum == 3))
            {
                int keySize = DataParser.ReadProtoInt(data, currentIndex, out bytesRead);
                currentIndex += bytesRead; bytesRead = 0;
                int keyStartIndex = currentIndex;

                desc = DataParser.ReadProtoInt(data, currentIndex, out bytesRead);
                currentIndex += bytesRead; bytesRead = 0;
                wireType = (byte)(desc & 7);
                fieldNum = desc >> 3;

                if ((wireType != 0) || (fieldNum != 1)) throw new Exception("GameEvent: WireType does not equal zero or FieldNum does not equal one");

                int typeMember = DataParser.ReadProtoInt(data, currentIndex, out bytesRead);
                currentIndex += bytesRead; bytesRead = 0;

                desc = DataParser.ReadProtoInt(data, currentIndex, out bytesRead);
                currentIndex += bytesRead; bytesRead = 0;
                wireType = (byte)(desc & 7);
                fieldNum = desc >> 3;

                if (fieldNum != (typeMember + 1)) throw new Exception("GameEvent: FieldNum does not equal (TypeMember + 1)");

                if (typeMember == 1) //string
                {
                    if (wireType != 2) throw new Exception("GameEvent: TypeMember 1, WireType should be 2");
                    keys.Add(DataParser.ReadProtoString(data, currentIndex, out bytesRead));
                    currentIndex += bytesRead; bytesRead = 0;
                }
                else if (typeMember == 2) //float
                {
                    if (wireType != 5) throw new Exception("GameEvent: TypeMember 2, WireType should be 5");
                    keys.Add(BitConverter.ToSingle(data, currentIndex));
                    currentIndex += 4;
                }
                else if (typeMember == 3 || typeMember == 4 || typeMember == 5) //long/short/byte
                {
                    if (wireType != 0) throw new Exception("GameEvent: TypeMember 3 4 or 5, WireType should be 0");
                    keys.Add(DataParser.ReadProtoInt(data, currentIndex, out bytesRead));
                    currentIndex += bytesRead; bytesRead = 0;
                }
                else if (typeMember == 6) //bool
                {
                    if (wireType != 0) throw new Exception("GameEvent: TypeMember 6, WireType should be 0");
                    keys.Add(DataParser.ReadProtoInt(data, currentIndex, out bytesRead) != 0);
                    currentIndex += bytesRead; bytesRead = 0;
                }
                else throw new Exception("GameEvent: Unknown TypeMember");

                if (currentIndex - keyStartIndex < keySize) throw new Exception("GameEvent: Key Data larger than expected");
            }
        }
        gameEvent.keys = keys.ToArray();

        //Debug.Log("GameEvent ID: " + gameEvent.eventID + " Name: " + gameEvent.eventName + " Keys: " + gameEvent.keys.Length);
        return gameEvent;
    }

    public static Dictionary<string, object> MapData(EventDescriptor eventDescriptor, GameEvent currentEvent)
    {
        Dictionary<string, object> data = new Dictionary<string, object>();

        for (int i = 0; i < eventDescriptor.keys.Length; i++)
            data.Add(eventDescriptor.keys[i].name, currentEvent.keys[i]);

        return data;
    }
}

public enum EquipmentElement
{
    Unknown = 0,

    //Pistols
    WeaponHKP2000 = 1,
    WeaponGlock = 2,
    WeaponP250 = 3,
    WeaponDEagle = 4,
    WeaponFiveSeven = 5,
    WeaponElite = 6,
    WeaponTec9 = 7,
    WeaponCZ75a = 8,
    WeaponUSP = 9,
    WeaponRevolver = 10,

    //SMGs
    WeaponMP7 = 101,
    WeaponMP9 = 102,
    WeaponBizon = 103,
    WeaponMAC10 = 104,
    WeaponUMP45 = 105,
    WeaponP90 = 106,

    //Heavy
    WeaponSawedoff = 201,
    WeaponNOVA = 202,
    WeaponMag7 = 203,
    WeaponXM1014 = 204,
    WeaponM249 = 205,
    WeaponNegev = 206,

    //Rifle
    WeaponGalil = 301,
    WeaponFamas = 302,
    WeaponAK47 = 303,
    WeaponM4A4 = 304,
    WeaponM4A1 = 305,
    WeaponSSG08 = 306,
    WeaponSG556 = 307,
    WeaponAug = 308,
    WeaponAWP = 309,
    WeaponSCAR20 = 310,
    WeaponG3SG1 = 311,

    //Equipment
    WeaponTaser = 401,
    Kevlar = 402,
    Helmet = 403,
    WeaponC4 = 404,
    WeaponKnife = 405,
    DefuseKit = 406,
    World = 407,

    //Grenades
    DecoyGrenade = 501,
    MolotovGrenade = 502,
    IncendiaryGrenade = 503,
    Flashbang = 504,
    SmokeGrenade = 505,
    HEGrenade = 506
}
public enum EquipmentClass
{
    Unknown = 0,
    Pistol = 1,
    SMG = 2,
    Heavy = 3,
    Rifle = 4,
    Equipment = 5,
    Grenade = 6,
}
public enum DemoCommand
{
    Signon = 1, // it's a startup message, process as fast as possible
    Packet, // it's a normal network packet that we stored off
    Synctick, // sync client clock to demo tick
    ConsoleCommand, // Console Command
    UserCommand, // user input command
    DataTables, //  network data tables
    Stop, // end of time.
    CustomData, // a blob of binary data understood by a callback function

    StringTables,
    LastCommand = StringTables, // Last Command
    FirstCommand = Signon, // First Command
};