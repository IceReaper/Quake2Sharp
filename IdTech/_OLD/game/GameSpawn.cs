/*
Copyright (C) 1997-2001 Id Software, Inc.

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

*/

using IdTech.common;
using Quake2Sharp.game.adapters;
using Quake2Sharp.game.monsters;
using Quake2Sharp.game.types;
using Quake2Sharp.qcommon;
using Quake2Sharp.util;
using System.Text;

namespace Quake2Sharp.game;

public class GameSpawn
{
	private static readonly EntThinkAdapter SP_item_health = new("SP_item_health", ent =>
	{
		GameItems.SP_item_health(ent);

		return true;
	});

	private static readonly EntThinkAdapter SP_item_health_small = new("SP_item_health_small", ent =>
	{
		GameItems.SP_item_health_small(ent);

		return true;
	});

	private static readonly EntThinkAdapter SP_item_health_large = new("SP_item_health_large", ent =>
	{
		GameItems.SP_item_health_large(ent);

		return true;
	});

	private static readonly EntThinkAdapter SP_item_health_mega = new("SP_item_health_mega", ent =>
	{
		GameItems.SP_item_health_mega(ent);

		return true;
	});

	private static readonly EntThinkAdapter SP_info_player_start = new("SP_info_player_start", ent =>
	{
		PlayerClient.SP_info_player_start(ent);

		return true;
	});

	private static readonly EntThinkAdapter SP_info_player_deathmatch = new("SP_info_player_deathmatch", ent =>
	{
		PlayerClient.SP_info_player_deathmatch(ent);

		return true;
	});

	private static readonly EntThinkAdapter SP_info_player_coop = new("SP_info_player_coop", ent =>
	{
		PlayerClient.SP_info_player_coop(ent);

		return true;
	});

	private static readonly EntThinkAdapter SP_info_player_intermission = new("SP_info_player_intermission", ent =>
	{
		PlayerClient.SP_info_player_intermission();

		return true;
	});

	private static readonly EntThinkAdapter SP_func_plat = new("SP_func_plat", ent =>
	{
		GameFunc.SP_func_plat(ent);

		return true;
	});

	private static readonly EntThinkAdapter SP_func_water = new("SP_func_water", ent =>
	{
		GameFunc.SP_func_water(ent);

		return true;
	});

	private static readonly EntThinkAdapter SP_func_train = new("SP_func_train", ent =>
	{
		GameFunc.SP_func_train(ent);

		return true;
	});

	private static readonly EntThinkAdapter SP_func_clock = new("SP_func_clock", ent =>
	{
		GameMisc.SP_func_clock(ent);

		return true;
	});

	/**
     * QUAKED worldspawn (0 0 0) ?
     *
     * Only used for the world. "sky" environment map name "skyaxis" vector axis
     * for rotating sky "skyrotate" speed of rotation in degrees/second "sounds"
     * music cd track number "gravity" 800 is default gravity "message" text to
     * print at user logon
     */
	private static readonly EntThinkAdapter SP_worldspawn = new("SP_worldspawn", ent =>
	{
		ent.movetype = Defines.MOVETYPE_PUSH;
		ent.solid = Defines.SOLID_BSP;
		ent.inuse = true;

		// since the world doesn't use G_Spawn()
		ent.s.modelindex = 1;

		// world model is always index 1
		//---------------
		// reserve some spots for dead player bodies for coop / deathmatch
		PlayerClient.InitBodyQue();

		// set configstrings for items
		GameItems.SetItemNames();

		if (GameBase.st.nextmap != null)
			GameBase.level.nextmap = GameBase.st.nextmap;

		// make some data visible to the server
		if (ent.message != null && ent.message.Length > 0)
		{
			GameBase.gi.configstring(Defines.CS_NAME, ent.message);
			GameBase.level.level_name = ent.message;
		}
		else
			GameBase.level.level_name = GameBase.level.mapname;

		if (GameBase.st.sky != null && GameBase.st.sky.Length > 0)
			GameBase.gi.configstring(Defines.CS_SKY, GameBase.st.sky);
		else
			GameBase.gi.configstring(Defines.CS_SKY, "unit1_");

		GameBase.gi.configstring(Defines.CS_SKYROTATE, "" + GameBase.st.skyrotate);
		GameBase.gi.configstring(Defines.CS_SKYAXIS, Lib.vtos(GameBase.st.skyaxis));
		GameBase.gi.configstring(Defines.CS_CDTRACK, "" + ent.sounds);
		GameBase.gi.configstring(Defines.CS_MAXCLIENTS, "" + (int)GameBase.maxclients.value);

		// status bar program
		if (GameBase.deathmatch.value != 0)
			GameBase.gi.configstring(Defines.CS_STATUSBAR, "" + GameSpawn.dm_statusbar);
		else
			GameBase.gi.configstring(Defines.CS_STATUSBAR, "" + GameSpawn.single_statusbar);

		//---------------
		// help icon for statusbar
		GameBase.gi.imageindex("i_help");
		GameBase.level.pic_health = GameBase.gi.imageindex("i_health");
		GameBase.gi.imageindex("help");
		GameBase.gi.imageindex("field_3");

		if ("".Equals(GameBase.st.gravity))
			GameBase.gi.cvar_set("sv_gravity", "800");
		else
			GameBase.gi.cvar_set("sv_gravity", GameBase.st.gravity);

		GameBase.snd_fry = GameBase.gi.soundindex("player/fry.wav");

		// standing in lava / slime
		GameItems.PrecacheItem(GameItems.FindItem("Blaster"));
		GameBase.gi.soundindex("player/lava1.wav");
		GameBase.gi.soundindex("player/lava2.wav");
		GameBase.gi.soundindex("misc/pc_up.wav");
		GameBase.gi.soundindex("misc/talk1.wav");
		GameBase.gi.soundindex("misc/udeath.wav");

		// gibs
		GameBase.gi.soundindex("items/respawn1.wav");

		// sexed sounds
		GameBase.gi.soundindex("*death1.wav");
		GameBase.gi.soundindex("*death2.wav");
		GameBase.gi.soundindex("*death3.wav");
		GameBase.gi.soundindex("*death4.wav");
		GameBase.gi.soundindex("*fall1.wav");
		GameBase.gi.soundindex("*fall2.wav");
		GameBase.gi.soundindex("*gurp1.wav");

		// drowning damage
		GameBase.gi.soundindex("*gurp2.wav");
		GameBase.gi.soundindex("*jump1.wav");

		// player jump
		GameBase.gi.soundindex("*pain25_1.wav");
		GameBase.gi.soundindex("*pain25_2.wav");
		GameBase.gi.soundindex("*pain50_1.wav");
		GameBase.gi.soundindex("*pain50_2.wav");
		GameBase.gi.soundindex("*pain75_1.wav");
		GameBase.gi.soundindex("*pain75_2.wav");
		GameBase.gi.soundindex("*pain100_1.wav");
		GameBase.gi.soundindex("*pain100_2.wav");

		// sexed models
		// THIS ORDER MUST MATCH THE DEFINES IN g_local.h
		// you can add more, max 15
		GameBase.gi.modelindex("#w_blaster.md2");
		GameBase.gi.modelindex("#w_shotgun.md2");
		GameBase.gi.modelindex("#w_sshotgun.md2");
		GameBase.gi.modelindex("#w_machinegun.md2");
		GameBase.gi.modelindex("#w_chaingun.md2");
		GameBase.gi.modelindex("#a_grenades.md2");
		GameBase.gi.modelindex("#w_glauncher.md2");
		GameBase.gi.modelindex("#w_rlauncher.md2");
		GameBase.gi.modelindex("#w_hyperblaster.md2");
		GameBase.gi.modelindex("#w_railgun.md2");
		GameBase.gi.modelindex("#w_bfg.md2");

		//-------------------
		GameBase.gi.soundindex("player/gasp1.wav");

		// gasping for air
		GameBase.gi.soundindex("player/gasp2.wav");

		// head breaking surface, not gasping
		GameBase.gi.soundindex("player/watr_in.wav");

		// feet hitting water
		GameBase.gi.soundindex("player/watr_out.wav");

		// feet leaving water
		GameBase.gi.soundindex("player/watr_un.wav");

		// head going underwater
		GameBase.gi.soundindex("player/u_breath1.wav");
		GameBase.gi.soundindex("player/u_breath2.wav");
		GameBase.gi.soundindex("items/pkup.wav");

		// bonus item pickup
		GameBase.gi.soundindex("world/land.wav");

		// landing thud
		GameBase.gi.soundindex("misc/h2ohit1.wav");

		// landing splash
		GameBase.gi.soundindex("items/damage.wav");
		GameBase.gi.soundindex("items/protect.wav");
		GameBase.gi.soundindex("items/protect4.wav");
		GameBase.gi.soundindex("weapons/noammo.wav");
		GameBase.gi.soundindex("infantry/inflies1.wav");
		GameBase.sm_meat_index = GameBase.gi.modelindex("models/objects/gibs/sm_meat/tris.md2");
		GameBase.gi.modelindex("models/objects/gibs/arm/tris.md2");
		GameBase.gi.modelindex("models/objects/gibs/bone/tris.md2");
		GameBase.gi.modelindex("models/objects/gibs/bone2/tris.md2");
		GameBase.gi.modelindex("models/objects/gibs/chest/tris.md2");
		GameBase.gi.modelindex("models/objects/gibs/skull/tris.md2");
		GameBase.gi.modelindex("models/objects/gibs/head2/tris.md2");

		//
		// Setup light animation tables. 'a' is total darkness, 'z' is
		// doublebright.
		//
		// 0 normal
		GameBase.gi.configstring(Defines.CS_LIGHTS + 0, "m");

		// 1 FLICKER (first variety)
		GameBase.gi.configstring(Defines.CS_LIGHTS + 1, "mmnmmommommnonmmonqnmmo");

		// 2 SLOW STRONG PULSE
		GameBase.gi.configstring(Defines.CS_LIGHTS + 2, "abcdefghijklmnopqrstuvwxyzyxwvutsrqponmlkjihgfedcba");

		// 3 CANDLE (first variety)
		GameBase.gi.configstring(Defines.CS_LIGHTS + 3, "mmmmmaaaaammmmmaaaaaabcdefgabcdefg");

		// 4 FAST STROBE
		GameBase.gi.configstring(Defines.CS_LIGHTS + 4, "mamamamamama");

		// 5 GENTLE PULSE 1
		GameBase.gi.configstring(Defines.CS_LIGHTS + 5, "jklmnopqrstuvwxyzyxwvutsrqponmlkj");

		// 6 FLICKER (second variety)
		GameBase.gi.configstring(Defines.CS_LIGHTS + 6, "nmonqnmomnmomomno");

		// 7 CANDLE (second variety)
		GameBase.gi.configstring(Defines.CS_LIGHTS + 7, "mmmaaaabcdefgmmmmaaaammmaamm");

		// 8 CANDLE (third variety)
		GameBase.gi.configstring(Defines.CS_LIGHTS + 8, "mmmaaammmaaammmabcdefaaaammmmabcdefmmmaaaa");

		// 9 SLOW STROBE (fourth variety)
		GameBase.gi.configstring(Defines.CS_LIGHTS + 9, "aaaaaaaazzzzzzzz");

		// 10 FLUORESCENT FLICKER
		GameBase.gi.configstring(Defines.CS_LIGHTS + 10, "mmamammmmammamamaaamammma");

		// 11 SLOW PULSE NOT FADE TO BLACK
		GameBase.gi.configstring(Defines.CS_LIGHTS + 11, "abcdefghijklmnopqrrqponmlkjihgfedcba");

		// styles 32-62 are assigned by the light program for switchable
		// lights
		// 63 testing
		GameBase.gi.configstring(Defines.CS_LIGHTS + 63, "a");

		return true;
	});

	/**
     * ED_NewString.
     */
	public static string ED_NewString(string @string)
	{
		var l = @string.Length;
		StringBuilder newb = new(l);

		for (var i = 0; i < l; i++)
		{
			var c = @string[i];

			if (c == '\\' && i < l - 1)
			{
				c = @string[++i];

				if (c == 'n')
					newb.Append('\n');
				else
					newb.Append('\\');
			}
			else
				newb.Append(c);
		}

		return newb.ToString();
	}

	/**
     * ED_ParseField
     *
     * Takes a key/value pair and sets the binary values in an edict.
     */
	private static void ED_ParseField(string key, string value, edict_t ent)
	{
		if (key.Equals("nextmap"))
			Com.Println("nextmap: " + value);

		if (!GameBase.st.set(key, value))
		{
			if (!ent.setField(key, value))
				GameBase.gi.dprintf("??? The key [" + key + "] is not a field\n");
		}
	}

	/**
     * ED_ParseEdict
     *
     * Parses an edict out of the given string, returning the new position ed
     * should be a properly initialized empty edict.
     */
	private static void ED_ParseEdict(Com.ParseHelp ph, edict_t ent)
	{
		bool init;
		string keyname;
		string com_token;
		init = false;

		GameBase.st = new();

		while (true)
		{
			// parse key
			com_token = Com.Parse(ph);

			if (com_token.Equals("}"))
				break;

			if (ph.isEof())
				GameBase.gi.error("ED_ParseEntity: EOF without closing brace");

			keyname = com_token;

			// parse value
			com_token = Com.Parse(ph);

			if (ph.isEof())
				GameBase.gi.error("ED_ParseEntity: EOF without closing brace");

			if (com_token.Equals("}"))
				GameBase.gi.error("ED_ParseEntity: closing brace without data");

			init = true;

			// keynames with a leading underscore are used for utility comments,
			// and are immediately discarded by quake
			if (keyname[0] == '_')
				continue;

			GameSpawn.ED_ParseField(keyname.ToLower(), com_token, ent);
		}

		if (!init)
			GameUtil.G_ClearEdict(ent);

		return;
	}

	/**
     * G_FindTeams
     *
     * Chain together all entities with a matching team field.
     *
     * All but the first will have the FL_TEAMSLAVE flag set. All but the last
     * will have the teamchain field set to the next one.
     */
	private static void G_FindTeams()
	{
		edict_t e, e2, chain;
		int i, j;
		int c, c2;
		c = 0;
		c2 = 0;

		for (i = 1; i < GameBase.num_edicts; i++)
		{
			e = GameBase.g_edicts[i];

			if (!e.inuse)
				continue;

			if (e.team == null)
				continue;

			if ((e.flags & Defines.FL_TEAMSLAVE) != 0)
				continue;

			chain = e;
			e.teammaster = e;
			c++;
			c2++;

			for (j = i + 1; j < GameBase.num_edicts; j++)
			{
				e2 = GameBase.g_edicts[j];

				if (!e2.inuse)
					continue;

				if (null == e2.team)
					continue;

				if ((e2.flags & Defines.FL_TEAMSLAVE) != 0)
					continue;

				if (0 == Lib.strcmp(e.team, e2.team))
				{
					c2++;
					chain.teamchain = e2;
					e2.teammaster = e;
					chain = e2;
					e2.flags |= Defines.FL_TEAMSLAVE;
				}
			}
		}
	}

	/**
     * SpawnEntities
     *
     * Creates a server's entity / program execution context by parsing textual
     * entity definitions out of an ent file.
     */
	public static void SpawnEntities(string mapname, string entities, string spawnpoint)
	{
		Com.dprintln("SpawnEntities(), mapname=" + mapname);
		edict_t ent;
		int inhibit;
		string com_token;
		int i;
		float skill_level;

		//skill.value =2.0f;
		skill_level = (float)Math.Floor(GameBase.skill.value);

		if (skill_level < 0)
			skill_level = 0;

		if (skill_level > 3)
			skill_level = 3;

		if (GameBase.skill.value != skill_level)
			GameBase.gi.cvar_forceset("skill", "" + skill_level);

		PlayerClient.SaveClientData();

		GameBase.level = new();

		for (var n = 0; n < GameBase.game.maxentities; n++)
			GameBase.g_edicts[n] = new(n);

		GameBase.level.mapname = mapname;
		GameBase.game.spawnpoint = spawnpoint;

		// set client fields on player ents
		for (i = 0; i < GameBase.game.maxclients; i++)
			GameBase.g_edicts[i + 1].client = GameBase.game.clients[i];

		ent = null;
		inhibit = 0;

		Com.ParseHelp ph = new(entities);

		while (true)
		{
			// parse the opening brace

			com_token = Com.Parse(ph);

			if (ph.isEof())
				break;

			if (!com_token.StartsWith("{"))
				GameBase.gi.error("ED_LoadFromFile: found " + com_token + " when expecting {");

			if (ent == null)
				ent = GameBase.g_edicts[0];
			else
				ent = GameUtil.G_Spawn();

			GameSpawn.ED_ParseEdict(ph, ent);
			clientserver.Com_DPrintf("spawning ent[" + ent.index + "], classname=" + ent.classname + ", flags=" + ent.spawnflags.ToString("x") + " ");

			// yet another map hack
			if (0 == Lib.Q_stricmp(GameBase.level.mapname, "command")
			    && 0 == Lib.Q_stricmp(ent.classname, "trigger_once")
			    && 0 == Lib.Q_stricmp(ent.model, "*27"))
				ent.spawnflags &= ~Defines.SPAWNFLAG_NOT_HARD;

			// remove things (except the world) from different skill levels or
			// deathmatch
			if (ent != GameBase.g_edicts[0])
			{
				if (GameBase.deathmatch.value != 0)
				{
					if ((ent.spawnflags & Defines.SPAWNFLAG_NOT_DEATHMATCH) != 0)
					{
						clientserver.Com_DPrintf("->inhibited.\n");
						GameUtil.G_FreeEdict(ent);
						inhibit++;

						continue;
					}
				}
				else
				{
					if ( /*
                         * ((coop.value) && (ent.spawnflags &
                         * SPAWNFLAG_NOT_COOP)) ||
                         */
					    (GameBase.skill.value == 0 && (ent.spawnflags & Defines.SPAWNFLAG_NOT_EASY) != 0)
					    || (GameBase.skill.value == 1 && (ent.spawnflags & Defines.SPAWNFLAG_NOT_MEDIUM) != 0)
					    || ((GameBase.skill.value == 2 || GameBase.skill.value == 3) && (ent.spawnflags & Defines.SPAWNFLAG_NOT_HARD) != 0))
					{
						clientserver.Com_DPrintf("->inhibited.\n");
						GameUtil.G_FreeEdict(ent);
						inhibit++;

						continue;
					}
				}

				ent.spawnflags &= ~(Defines.SPAWNFLAG_NOT_EASY
					| Defines.SPAWNFLAG_NOT_MEDIUM
					| Defines.SPAWNFLAG_NOT_HARD
					| Defines.SPAWNFLAG_NOT_COOP
					| Defines.SPAWNFLAG_NOT_DEATHMATCH);
			}

			GameSpawn.ED_CallSpawn(ent);
			clientserver.Com_DPrintf("\n");
		}

		clientserver.Com_DPrintf("player skill level:" + GameBase.skill.value + "\n");
		clientserver.Com_DPrintf(inhibit + " entities inhibited.\n");
		i = 1;
		GameSpawn.G_FindTeams();
		PlayerTrail.Init();
	}

	private static readonly string single_statusbar = "yb	-24 " //	   health
		+ "xv	0 "
		+ "hnum "
		+ "xv	50 "
		+ "pic 0 " //	   ammo
		+ "if 2 "
		+ "	xv	100 "
		+ "	anum "
		+ "	xv	150 "
		+ "	pic 2 "
		+ "endif " //	   armor
		+ "if 4 "
		+ "	xv	200 "
		+ "	rnum "
		+ "	xv	250 "
		+ "	pic 4 "
		+ "endif " //	   selected item
		+ "if 6 "
		+ "	xv	296 "
		+ "	pic 6 "
		+ "endif "
		+ "yb	-50 " //	   picked
		// up
		// item
		+ "if 7 "
		+ "	xv	0 "
		+ "	pic 7 "
		+ "	xv	26 "
		+ "	yb	-42 "
		+ "	stat_string 8 "
		+ "	yb	-50 "
		+ "endif "

		//	   timer
		+ "if 9 "
		+ "	xv	262 "
		+ "	num	2	10 "
		+ "	xv	296 "
		+ "	pic	9 "
		+ "endif "

		//		help / weapon icon
		+ "if 11 "
		+ "	xv	148 "
		+ "	pic	11 "
		+ "endif ";

	private static readonly string dm_statusbar = "yb	-24 " //	   health
		+ "xv	0 "
		+ "hnum "
		+ "xv	50 "
		+ "pic 0 " //	   ammo
		+ "if 2 "
		+ "	xv	100 "
		+ "	anum "
		+ "	xv	150 "
		+ "	pic 2 "
		+ "endif " //	   armor
		+ "if 4 "
		+ "	xv	200 "
		+ "	rnum "
		+ "	xv	250 "
		+ "	pic 4 "
		+ "endif " //	   selected item
		+ "if 6 "
		+ "	xv	296 "
		+ "	pic 6 "
		+ "endif "
		+ "yb	-50 " //	   picked
		// up
		// item
		+ "if 7 "
		+ "	xv	0 "
		+ "	pic 7 "
		+ "	xv	26 "
		+ "	yb	-42 "
		+ "	stat_string 8 "
		+ "	yb	-50 "
		+ "endif "

		//	   timer
		+ "if 9 "
		+ "	xv	246 "
		+ "	num	2	10 "
		+ "	xv	296 "
		+ "	pic	9 "
		+ "endif "

		//		help / weapon icon
		+ "if 11 "
		+ "	xv	148 "
		+ "	pic	11 "
		+ "endif " //		frags
		+ "xr	-50 "
		+ "yt 2 "
		+ "num 3 14 " //	   spectator
		+ "if 17 "
		+ "xv 0 "
		+ "yb -58 "
		+ "string2 \"SPECTATOR MODE\" "
		+ "endif " //	   chase camera
		+ "if 16 "
		+ "xv 0 "
		+ "yb -68 "
		+ "string \"Chasing\" "
		+ "xv 64 "
		+ "stat_string 16 "
		+ "endif ";

	private static readonly spawn_t[] spawns =
	{
		new("item_health", GameSpawn.SP_item_health),
		new("item_health_small", GameSpawn.SP_item_health_small),
		new("item_health_large", GameSpawn.SP_item_health_large),
		new("item_health_mega", GameSpawn.SP_item_health_mega),
		new("info_player_start", GameSpawn.SP_info_player_start),
		new("info_player_deathmatch", GameSpawn.SP_info_player_deathmatch),
		new("info_player_coop", GameSpawn.SP_info_player_coop),
		new("info_player_intermission", GameSpawn.SP_info_player_intermission),
		new("func_plat", GameSpawn.SP_func_plat),
		new("func_button", GameFunc.SP_func_button),
		new("func_door", GameFunc.SP_func_door),
		new("func_door_secret", GameFunc.SP_func_door_secret),
		new("func_door_rotating", GameFunc.SP_func_door_rotating),
		new("func_rotating", GameFunc.SP_func_rotating),
		new("func_train", GameSpawn.SP_func_train),
		new("func_water", GameSpawn.SP_func_water),
		new("func_conveyor", GameFunc.SP_func_conveyor),
		new("func_areaportal", GameMisc.SP_func_areaportal),
		new("func_clock", GameSpawn.SP_func_clock),
		new("func_wall", new("func_wall", ent =>
		{
			GameMisc.SP_func_wall(ent);

			return true;
		})),
		new("func_object", new("SP_func_object", ent =>
		{
			GameMisc.SP_func_object(ent);

			return true;
		})),
		new("func_timer", new("SP_func_timer", ent =>
		{
			GameFunc.SP_func_timer(ent);

			return true;
		})),
		new("func_explosive", new("SP_func_explosive", ent =>
		{
			GameMisc.SP_func_explosive(ent);

			return true;
		})),
		new("func_killbox", GameFunc.SP_func_killbox),
		new("trigger_always", new("SP_trigger_always", ent =>
		{
			GameTrigger.SP_trigger_always(ent);

			return true;
		})),
		new("trigger_once", new("SP_trigger_once", ent =>
		{
			GameTrigger.SP_trigger_once(ent);

			return true;
		})),
		new("trigger_multiple", new("SP_trigger_multiple", ent =>
		{
			GameTrigger.SP_trigger_multiple(ent);

			return true;
		})),
		new("trigger_relay", new("SP_trigger_relay", ent =>
		{
			GameTrigger.SP_trigger_relay(ent);

			return true;
		})),
		new("trigger_push", new("SP_trigger_push", ent =>
		{
			GameTrigger.SP_trigger_push(ent);

			return true;
		})),
		new("trigger_hurt", new("SP_trigger_hurt", ent =>
		{
			GameTrigger.SP_trigger_hurt(ent);

			return true;
		})),
		new("trigger_key", new("SP_trigger_key", ent =>
		{
			GameTrigger.SP_trigger_key(ent);

			return true;
		})),
		new("trigger_counter", new("SP_trigger_counter", ent =>
		{
			GameTrigger.SP_trigger_counter(ent);

			return true;
		})),
		new("trigger_elevator", GameFunc.SP_trigger_elevator),
		new("trigger_gravity", new("SP_trigger_gravity", ent =>
		{
			GameTrigger.SP_trigger_gravity(ent);

			return true;
		})),
		new("trigger_monsterjump", new("SP_trigger_monsterjump", ent =>
		{
			GameTrigger.SP_trigger_monsterjump(ent);

			return true;
		})),
		new("target_temp_entity", new("SP_target_temp_entity", ent =>
		{
			GameTarget.SP_target_temp_entity(ent);

			return true;
		})),
		new("target_speaker", new("SP_target_speaker", ent =>
		{
			GameTarget.SP_target_speaker(ent);

			return true;
		})),
		new("target_explosion", new("SP_target_explosion", ent =>
		{
			GameTarget.SP_target_explosion(ent);

			return true;
		})),
		new("target_changelevel", new("SP_target_changelevel", ent =>
		{
			GameTarget.SP_target_changelevel(ent);

			return true;
		})),
		new("target_secret", new("SP_target_secret", ent =>
		{
			GameTarget.SP_target_secret(ent);

			return true;
		})),
		new("target_goal", new("SP_target_goal", ent =>
		{
			GameTarget.SP_target_goal(ent);

			return true;
		})),
		new("target_splash", new("SP_target_splash", ent =>
		{
			GameTarget.SP_target_splash(ent);

			return true;
		})),
		new("target_spawner", new("SP_target_spawner", ent =>
		{
			GameTarget.SP_target_spawner(ent);

			return true;
		})),
		new("target_blaster", new("SP_target_blaster", ent =>
		{
			GameTarget.SP_target_blaster(ent);

			return true;
		})),
		new("target_crosslevel_trigger", new("SP_target_crosslevel_trigger", ent =>
		{
			GameTarget.SP_target_crosslevel_trigger(ent);

			return true;
		})),
		new("target_crosslevel_target", new("SP_target_crosslevel_target", ent =>
		{
			GameTarget.SP_target_crosslevel_target(ent);

			return true;
		})),
		new("target_laser", new("SP_target_laser", ent =>
		{
			GameTarget.SP_target_laser(ent);

			return true;
		})),
		new("target_help", new("SP_target_help", ent =>
		{
			GameTarget.SP_target_help(ent);

			return true;
		})),
		new("target_actor", new("SP_target_actor", ent =>
		{
			M_Actor.SP_target_actor(ent);

			return true;
		})),
		new("target_lightramp", new("SP_target_lightramp", ent =>
		{
			GameTarget.SP_target_lightramp(ent);

			return true;
		})),
		new("target_earthquake", new("SP_target_earthquake", ent =>
		{
			GameTarget.SP_target_earthquake(ent);

			return true;
		})),
		new("target_character", new("SP_target_character", ent =>
		{
			GameMisc.SP_target_character(ent);

			return true;
		})),
		new("target_string", new("SP_target_string", ent =>
		{
			GameMisc.SP_target_string(ent);

			return true;
		})),
		new("worldspawn", GameSpawn.SP_worldspawn),
		new("viewthing", new("SP_viewthing", ent =>
		{
			GameMisc.SP_viewthing(ent);

			return true;
		})),
		new("light", new("SP_light", ent =>
		{
			GameMisc.SP_light(ent);

			return true;
		})),
		new("light_mine1", new("SP_light_mine1", ent =>
		{
			GameMisc.SP_light_mine1(ent);

			return true;
		})),
		new("light_mine2", new("SP_light_mine2", ent =>
		{
			GameMisc.SP_light_mine2(ent);

			return true;
		})),
		new("info_null", new("SP_info_null", ent =>
		{
			GameMisc.SP_info_null(ent);

			return true;
		})),
		new("func_group", new("SP_info_null", ent =>
		{
			GameMisc.SP_info_null(ent);

			return true;
		})),
		new("info_notnull", new("info_notnull", ent =>
		{
			GameMisc.SP_info_notnull(ent);

			return true;
		})),
		new("path_corner", new("SP_path_corner", ent =>
		{
			GameMisc.SP_path_corner(ent);

			return true;
		})),
		new("point_combat", new("SP_point_combat", ent =>
		{
			GameMisc.SP_point_combat(ent);

			return true;
		})),
		new("misc_explobox", new("SP_misc_explobox", ent =>
		{
			GameMisc.SP_misc_explobox(ent);

			return true;
		})),
		new("misc_banner", new("SP_misc_banner", ent =>
		{
			GameMisc.SP_misc_banner(ent);

			return true;
		})),
		new("misc_satellite_dish", new("SP_misc_satellite_dish", ent =>
		{
			GameMisc.SP_misc_satellite_dish(ent);

			return true;
		})),
		new("misc_actor", new("SP_misc_actor", ent =>
		{
			M_Actor.SP_misc_actor(ent);

			return false;
		})),
		new("misc_gib_arm", new("SP_misc_gib_arm", ent =>
		{
			GameMisc.SP_misc_gib_arm(ent);

			return true;
		})),
		new("misc_gib_leg", new("SP_misc_gib_leg", ent =>
		{
			GameMisc.SP_misc_gib_leg(ent);

			return true;
		})),
		new("misc_gib_head", new("SP_misc_gib_head", ent =>
		{
			GameMisc.SP_misc_gib_head(ent);

			return true;
		})),
		new("misc_insane", new("SP_misc_insane", ent =>
		{
			M_Insane.SP_misc_insane(ent);

			return true;
		})),
		new("misc_deadsoldier", new("SP_misc_deadsoldier", ent =>
		{
			GameMisc.SP_misc_deadsoldier(ent);

			return true;
		})),
		new("misc_viper", new("SP_misc_viper", ent =>
		{
			GameMisc.SP_misc_viper(ent);

			return true;
		})),
		new("misc_viper_bomb", new("SP_misc_viper_bomb", ent =>
		{
			GameMisc.SP_misc_viper_bomb(ent);

			return true;
		})),
		new("misc_bigviper", new("SP_misc_bigviper", ent =>
		{
			GameMisc.SP_misc_bigviper(ent);

			return true;
		})),
		new("misc_strogg_ship", new("SP_misc_strogg_ship", ent =>
		{
			GameMisc.SP_misc_strogg_ship(ent);

			return true;
		})),
		new("misc_teleporter", new("SP_misc_teleporter", ent =>
		{
			GameMisc.SP_misc_teleporter(ent);

			return true;
		})),
		new("misc_teleporter_dest", GameMisc.SP_misc_teleporter_dest),
		new("misc_blackhole", new("SP_misc_blackhole", ent =>
		{
			GameMisc.SP_misc_blackhole(ent);

			return true;
		})),
		new("misc_eastertank", new("SP_misc_eastertank", ent =>
		{
			GameMisc.SP_misc_eastertank(ent);

			return true;
		})),
		new("misc_easterchick", new("SP_misc_easterchick", ent =>
		{
			GameMisc.SP_misc_easterchick(ent);

			return true;
		})),
		new("misc_easterchick2", new("SP_misc_easterchick2", ent =>
		{
			GameMisc.SP_misc_easterchick2(ent);

			return true;
		})),
		new("monster_berserk", new("SP_monster_berserk", ent =>
		{
			M_Berserk.SP_monster_berserk(ent);

			return true;
		})),
		new("monster_gladiator", new("SP_monster_gladiator", ent =>
		{
			M_Gladiator.SP_monster_gladiator(ent);

			return true;
		})),
		new("monster_gunner", new("SP_monster_gunner", ent =>
		{
			M_Gunner.SP_monster_gunner(ent);

			return true;
		})),
		new("monster_infantry", new("SP_monster_infantry", ent =>
		{
			M_Infantry.SP_monster_infantry(ent);

			return true;
		})),
		new("monster_soldier_light", M_Soldier.SP_monster_soldier_light),
		new("monster_soldier", M_Soldier.SP_monster_soldier),
		new("monster_soldier_ss", M_Soldier.SP_monster_soldier_ss),
		new("monster_tank", M_Tank.SP_monster_tank),
		new("monster_tank_commander", M_Tank.SP_monster_tank),
		new("monster_medic", new("SP_monster_medic", ent =>
		{
			M_Medic.SP_monster_medic(ent);

			return true;
		})),
		new("monster_flipper", new("SP_monster_flipper", ent =>
		{
			M_Flipper.SP_monster_flipper(ent);

			return true;
		})),
		new("monster_chick", new("SP_monster_chick", ent =>
		{
			M_Chick.SP_monster_chick(ent);

			return true;
		})),
		new("monster_parasite", M_Parasite.SP_monster_parasite),
		new("monster_flyer", new("SP_monster_flyer", ent =>
		{
			M_Flyer.SP_monster_flyer(ent);

			return true;
		})),
		new("monster_brain", new("SP_monster_brain", ent =>
		{
			M_Brain.SP_monster_brain(ent);

			return true;
		})),
		new("monster_floater", new("SP_monster_floater", ent =>
		{
			M_Float.SP_monster_floater(ent);

			return true;
		})),
		new("monster_hover", new("SP_monster_hover", ent =>
		{
			M_Hover.SP_monster_hover(ent);

			return true;
		})),
		new("monster_mutant", M_Mutant.SP_monster_mutant),
		new("monster_supertank", M_Supertank.SP_monster_supertank),
		new("monster_boss2", new("SP_monster_boss2", ent =>
		{
			M_Boss2.SP_monster_boss2(ent);

			return true;
		})),
		new("monster_boss3_stand", new("SP_monster_boss3_stand", ent =>
		{
			M_Boss3.SP_monster_boss3_stand(ent);

			return true;
		})),
		new("monster_jorg", new("SP_monster_jorg", ent =>
		{
			M_Boss31.SP_monster_jorg(ent);

			return true;
		})),
		new("monster_commander_body", new("SP_monster_commander_body", ent =>
		{
			GameMisc.SP_monster_commander_body(ent);

			return true;
		})),
		new("turret_breach", new("SP_turret_breach", ent =>
		{
			GameTurret.SP_turret_breach(ent);

			return true;
		})),
		new("turret_base", new("SP_turret_base", ent =>
		{
			GameTurret.SP_turret_base(ent);

			return true;
		})),
		new("turret_driver", new("SP_turret_driver", ent =>
		{
			GameTurret.SP_turret_driver(ent);

			return true;
		})),
		new(null, null)
	};

	/**
     * ED_CallSpawn
     *
     * Finds the spawn function for the entity and calls it.
     */
	public static void ED_CallSpawn(edict_t ent)
	{
		spawn_t s;
		gitem_t item;
		int i;

		if (null == ent.classname)
		{
			GameBase.gi.dprintf("ED_CallSpawn: null classname\n");

			return;
		} // check item spawn functions

		for (i = 1; i < GameBase.game.num_items; i++)
		{
			item = GameItemList.itemlist[i];

			if (item == null)
				GameBase.gi.error("ED_CallSpawn: null item in pos " + i);

			if (item.classname == null)
				continue;

			if (item.classname.Equals(ent.classname, StringComparison.OrdinalIgnoreCase))
			{
				// found it
				GameItems.SpawnItem(ent, item);

				return;
			}
		} // check normal spawn functions

		for (i = 0; (s = GameSpawn.spawns[i]) != null && s.name != null; i++)
		{
			if (s.name.Equals(ent.classname, StringComparison.OrdinalIgnoreCase))
			{
				// found it

				if (s.spawn == null)
					GameBase.gi.error("ED_CallSpawn: null-spawn on index=" + i);

				s.spawn.think(ent);

				return;
			}
		}

		GameBase.gi.dprintf(ent.classname + " doesn't have a spawn function\n");
	}

	static GameSpawn()
	{
	}
}
