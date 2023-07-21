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

namespace Quake2Sharp.game;

using client;
using qcommon;
using server;
using System.Text.RegularExpressions;
using types;
using util;

public class GameBase
{
	public static cplane_t dummyplane = new();
	public static game_locals_t game = new();
	public static level_locals_t level = new();
	public static game_import_t gi = new();
	public static spawn_temp_t st = new();
	public static int sm_meat_index;
	public static int snd_fry;
	public static int meansOfDeath;
	public static int num_edicts;
	public static edict_t[] g_edicts = new edict_t[Defines.MAX_EDICTS];

	private static void GameBase1()
	{
		for (var n = 0; n < Defines.MAX_EDICTS; n++)
			GameBase.g_edicts[n] = new(n);
	}

	public static cvar_t deathmatch = new();
	public static cvar_t coop = new();
	public static cvar_t dmflags = new();
	public static cvar_t skill; // = new cvar_t();
	public static cvar_t fraglimit = new();
	public static cvar_t timelimit = new();
	public static cvar_t password = new();
	public static cvar_t spectator_password = new();
	public static cvar_t needpass = new();
	public static cvar_t maxclients = new();
	public static cvar_t maxspectators = new();
	public static cvar_t maxentities = new();
	public static cvar_t g_select_empty = new();
	public static cvar_t filterban = new();
	public static cvar_t sv_maxvelocity = new();
	public static cvar_t sv_gravity = new();
	public static cvar_t sv_rollspeed = new();
	public static cvar_t sv_rollangle = new();
	public static cvar_t gun_x = new();
	public static cvar_t gun_y = new();
	public static cvar_t gun_z = new();
	public static cvar_t run_pitch = new();
	public static cvar_t run_roll = new();
	public static cvar_t bob_up = new();
	public static cvar_t bob_pitch = new();
	public static cvar_t bob_roll = new();
	public static cvar_t sv_cheats = new();
	public static cvar_t flood_msgs = new();
	public static cvar_t flood_persecond = new();
	public static cvar_t flood_waitdelay = new();
	public static cvar_t sv_maplist = new();
	public static readonly float STOP_EPSILON = 0.1f;

	/**
     * Slide off of the impacting object returns the blocked flags (1 = floor, 2 =
     * step / wall).
     */
	public static int ClipVelocity(float[] @in, float[] normal, float[] @out, float overbounce)
	{
		float backoff;
		float change;
		int i, blocked;

		blocked = 0;

		if (normal[2] > 0)
			blocked |= 1; // floor

		if (normal[2] == 0.0f)
			blocked |= 2; // step

		backoff = Math3D.DotProduct(@in, normal) * overbounce;

		for (i = 0; i < 3; i++)
		{
			change = normal[i] * backoff;
			@out[i] = @in[i] - change;

			if (@out[i] > -GameBase.STOP_EPSILON && @out[i] < GameBase.STOP_EPSILON)
				@out[i] = 0;
		}

		return blocked;
	}

	/**
     * Searches all active entities for the next one that holds the matching
     * string at fieldofs (use the FOFS() macro) in the structure.
     *
     * Searches beginning at the edict after from, or the beginning if null null
     * will be returned if the end of the list is reached.
     *
     */
	public static EdictIterator G_Find(EdictIterator from, Func<edict_t, string, bool> eff, string s)
	{
		if (from == null)
			from = new(0);
		else
			from.i++;

		for (; from.i < GameBase.num_edicts; from.i++)
		{
			from.o = GameBase.g_edicts[from.i];

			if (from.o.classname == null)
				clientserver.Com_Printf("edict with classname = null" + @from.o.index);

			if (!from.o.inuse)
				continue;

			if (eff(from.o, s))
				return from;
		}

		return null;
	}

	// comfort version (rst)
	public static edict_t G_FindEdict(EdictIterator from, Func<edict_t, string, bool> eff, string s)
	{
		var ei = GameBase.G_Find(from, eff, s);

		if (ei == null)
			return null;

		return ei.o;
	}

	/**
     * Returns entities that have origins within a spherical area.
     */
	public static EdictIterator findradius(EdictIterator from, float[] org, float rad)
	{
		float[] eorg = { 0, 0, 0 };
		int j;

		if (from == null)
			from = new(0);
		else
			from.i++;

		for (; from.i < GameBase.num_edicts; from.i++)
		{
			from.o = GameBase.g_edicts[from.i];

			if (!from.o.inuse)
				continue;

			if (from.o.solid == Defines.SOLID_NOT)
				continue;

			for (j = 0; j < 3; j++)
				eorg[j] = org[j] - (from.o.s.origin[j] + (from.o.mins[j] + from.o.maxs[j]) * 0.5f);

			if (Math3D.VectorLength(eorg) > rad)
				continue;

			return from;
		}

		return null;
	}

	/**
     * Searches all active entities for the next one that holds the matching
     * string at fieldofs (use the FOFS() macro) in the structure.
     *
     * Searches beginning at the edict after from, or the beginning if null null
     * will be returned if the end of the list is reached.
     */
	public static int MAXCHOICES = 8;

	public static edict_t G_PickTarget(string targetname)
	{
		var num_choices = 0;
		var choice = new edict_t[GameBase.MAXCHOICES];

		if (targetname == null)
		{
			GameBase.gi.dprintf("G_PickTarget called with null targetname\n");

			return null;
		}

		EdictIterator es = null;

		while ((es = GameBase.G_Find(es, GameBase.findByTarget, targetname)) != null)
		{
			choice[num_choices++] = es.o;

			if (num_choices == GameBase.MAXCHOICES)
				break;
		}

		if (num_choices == 0)
		{
			GameBase.gi.dprintf("G_PickTarget: target " + targetname + " not found\n");

			return null;
		}

		return choice[Lib.rand() % num_choices];
	}

	public static float[] VEC_UP = { 0, -1, 0 };
	public static float[] MOVEDIR_UP = { 0, 0, 1 };
	public static float[] VEC_DOWN = { 0, -2, 0 };
	public static float[] MOVEDIR_DOWN = { 0, 0, -1 };

	public static void G_SetMovedir(float[] angles, float[] movedir)
	{
		if (Math3D.VectorEquals(angles, GameBase.VEC_UP))
			Math3D.VectorCopy(GameBase.MOVEDIR_UP, movedir);
		else if (Math3D.VectorEquals(angles, GameBase.VEC_DOWN))
			Math3D.VectorCopy(GameBase.MOVEDIR_DOWN, movedir);
		else
			Math3D.AngleVectors(angles, movedir, null, null);

		Math3D.VectorClear(angles);
	}

	public static string G_CopyString(string @in)
	{
		return new(@in);
	}

	/**
     * G_TouchTriggers
     */
	private static readonly edict_t[] touch = new edict_t[Defines.MAX_EDICTS];

	public static void G_TouchTriggers(edict_t ent)
	{
		int i, num;
		edict_t hit;

		// dead things don't activate triggers!
		if ((ent.client != null || (ent.svflags & Defines.SVF_MONSTER) != 0) && ent.health <= 0)
			return;

		num = GameBase.gi.BoxEdicts(ent.absmin, ent.absmax, GameBase.touch, Defines.MAX_EDICTS, Defines.AREA_TRIGGERS);

		// be careful, it is possible to have an entity in this
		// list removed before we get to it (killtriggered)
		for (i = 0; i < num; i++)
		{
			hit = GameBase.touch[i];

			if (!hit.inuse)
				continue;

			if (hit.touch == null)
				continue;

			hit.touch.touch(hit, ent, GameBase.dummyplane, null);
		}
	}

	public static pushed_t[] pushed = new pushed_t[Defines.MAX_EDICTS];

	private static void GameBase2()
	{
		for (var n = 0; n < Defines.MAX_EDICTS; n++)
			GameBase.pushed[n] = new();
	}

	public static int pushed_p;
	public static edict_t obstacle;
	public static int c_yes, c_no;
	public static int STEPSIZE = 18;

	/**
     * G_RunEntity
     */
	public static void G_RunEntity(edict_t ent)
	{
		if (ent.prethink != null)
			ent.prethink.think(ent);

		switch ((int)ent.movetype)
		{
			case Defines.MOVETYPE_PUSH:
			case Defines.MOVETYPE_STOP:
				SV.SV_Physics_Pusher(ent);

				break;

			case Defines.MOVETYPE_NONE:
				SV.SV_Physics_None(ent);

				break;

			case Defines.MOVETYPE_NOCLIP:
				SV.SV_Physics_Noclip(ent);

				break;

			case Defines.MOVETYPE_STEP:
				SV.SV_Physics_Step(ent);

				break;

			case Defines.MOVETYPE_TOSS:
			case Defines.MOVETYPE_BOUNCE:
			case Defines.MOVETYPE_FLY:
			case Defines.MOVETYPE_FLYMISSILE:
				SV.SV_Physics_Toss(ent);

				break;

			default:
				GameBase.gi.error("SV_Physics: bad movetype " + (int)ent.movetype);

				break;
		}
	}

	public static void ClearBounds(float[] mins, float[] maxs)
	{
		mins[0] = mins[1] = mins[2] = 99999;
		maxs[0] = maxs[1] = maxs[2] = -99999;
	}

	public static void AddPointToBounds(float[] v, float[] mins, float[] maxs)
	{
		int i;
		float val;

		for (i = 0; i < 3; i++)
		{
			val = v[i];

			if (val < mins[i])
				mins[i] = val;

			if (val > maxs[i])
				maxs[i] = val;
		}
	}

	public static Func<edict_t, string, bool> findByTarget = (e, s) =>
	{
		if (e.targetname == null)
			return false;

		return e.targetname.Equals(s, StringComparison.OrdinalIgnoreCase);
	};

	public static Func<edict_t, string, bool> findByClass = (e, s) => { return e.classname.Equals(s, StringComparison.OrdinalIgnoreCase); };

	public static void ShutdownGame()
	{
		GameBase.gi.dprintf("==== ShutdownGame ====\n");
	}

	/**
     * ClientEndServerFrames.
     */
	public static void ClientEndServerFrames()
	{
		int i;
		edict_t ent;

		// calc the player views now that all pushing
		// and damage has been added
		for (i = 0; i < GameBase.maxclients.value; i++)
		{
			ent = GameBase.g_edicts[1 + i];

			if (!ent.inuse || null == ent.client)
				continue;

			PlayerView.ClientEndServerFrame(ent);
		}
	}

	/**
     * Returns the created target changelevel.
     */
	public static edict_t CreateTargetChangeLevel(string map)
	{
		edict_t ent;

		ent = GameUtil.G_Spawn();
		ent.classname = "target_changelevel";
		GameBase.level.nextmap = map;
		ent.map = GameBase.level.nextmap;

		return ent;
	}

	/**
     * The timelimit or fraglimit has been exceeded.
     */
	public static void EndDMLevel()
	{
		edict_t ent;

		//char * s, * t, * f;
		//static const char * seps = " ,\n\r";
		string s, t, f;
		var seps = " ,\n\r";

		// stay on same level flag
		if (((int)GameBase.dmflags.value & Defines.DF_SAME_LEVEL) != 0)
		{
			PlayerHud.BeginIntermission(GameBase.CreateTargetChangeLevel(GameBase.level.mapname));

			return;
		}

		// see if it's in the map list
		if (GameBase.sv_maplist.@string.Length > 0)
		{
			s = GameBase.sv_maplist.@string;
			f = null;
			var tk = Regex.Split(s, $"[{seps}]");

			for (var i = 0; i < tk.Length; i++)
			{
				t = tk[i];

				// store first map
				if (f == null)
					f = t;

				if (t.Equals(GameBase.level.mapname, StringComparison.OrdinalIgnoreCase))
				{
					// it's in the list, go to the next one
					if (i + 1 == tk.Length)
					{
						// end of list, go to first one
						if (f == null) // there isn't a first one, same level
							PlayerHud.BeginIntermission(GameBase.CreateTargetChangeLevel(GameBase.level.mapname));
						else
							PlayerHud.BeginIntermission(GameBase.CreateTargetChangeLevel(f));
					}
					else
						PlayerHud.BeginIntermission(GameBase.CreateTargetChangeLevel(tk[++i]));

					return;
				}
			}
		}

		//not in the map list
		if (GameBase.level.nextmap.Length > 0) // go to a specific map
			PlayerHud.BeginIntermission(GameBase.CreateTargetChangeLevel(GameBase.level.nextmap));
		else
		{
			// search for a changelevel
			EdictIterator edit = null;
			edit = GameBase.G_Find(edit, GameBase.findByClass, "target_changelevel");

			if (edit == null)
			{
				// the map designer didn't include a
				// changelevel,
				// so create a fake ent that goes back to the same level
				PlayerHud.BeginIntermission(GameBase.CreateTargetChangeLevel(GameBase.level.mapname));

				return;
			}

			ent = edit.o;
			PlayerHud.BeginIntermission(ent);
		}
	}

	/**
     * CheckNeedPass.
     */
	public static void CheckNeedPass()
	{
		int need;

		// if password or spectator_password has changed, update needpass
		// as needed
		if (GameBase.password.modified || GameBase.spectator_password.modified)
		{
			GameBase.password.modified = GameBase.spectator_password.modified = false;

			need = 0;

			if (GameBase.password.@string.Length > 0 && 0 != Lib.Q_stricmp(GameBase.password.@string, "none"))
				need |= 1;

			if (GameBase.spectator_password.@string.Length > 0 && 0 != Lib.Q_stricmp(GameBase.spectator_password.@string, "none"))
				need |= 2;

			GameBase.gi.cvar_set("needpass", "" + need);
		}
	}

	/**
     * CheckDMRules.
     */
	public static void CheckDMRules()
	{
		int i;
		gclient_t cl;

		if (GameBase.level.intermissiontime != 0)
			return;

		if (0 == GameBase.deathmatch.value)
			return;

		if (GameBase.timelimit.value != 0)
		{
			if (GameBase.level.time >= GameBase.timelimit.value * 60)
			{
				GameBase.gi.bprintf(Defines.PRINT_HIGH, "Timelimit hit.\n");
				GameBase.EndDMLevel();

				return;
			}
		}

		if (GameBase.fraglimit.value != 0)
		{
			for (i = 0; i < GameBase.maxclients.value; i++)
			{
				cl = GameBase.game.clients[i];

				if (!GameBase.g_edicts[i + 1].inuse)
					continue;

				if (cl.resp.score >= GameBase.fraglimit.value)
				{
					GameBase.gi.bprintf(Defines.PRINT_HIGH, "Fraglimit hit.\n");
					GameBase.EndDMLevel();

					return;
				}
			}
		}
	}

	/**
     * Exits a level.
     */
	public static void ExitLevel()
	{
		int i;
		edict_t ent;

		var command = "gamemap \"" + GameBase.level.changemap + "\"\n";
		GameBase.gi.AddCommandString(command);
		GameBase.level.changemap = null;
		GameBase.level.exitintermission = false;
		GameBase.level.intermissiontime = 0;
		GameBase.ClientEndServerFrames();

		// clear some things before going to next level
		for (i = 0; i < GameBase.maxclients.value; i++)
		{
			ent = GameBase.g_edicts[1 + i];

			if (!ent.inuse)
				continue;

			if (ent.health > ent.client.pers.max_health)
				ent.health = ent.client.pers.max_health;
		}
	}

	/**
     * G_RunFrame
     *
     * Advances the world by Defines.FRAMETIME (0.1) seconds.
     */
	public static void G_RunFrame()
	{
		int i;
		edict_t ent;

		GameBase.level.framenum++;
		GameBase.level.time = GameBase.level.framenum * Defines.FRAMETIME;

		// choose a client for monsters to target this frame
		GameAI.AI_SetSightClient();

		// exit intermissions

		if (GameBase.level.exitintermission)
		{
			GameBase.ExitLevel();

			return;
		}

		//
		// treat each object in turn
		// even the world gets a chance to think
		//

		for (i = 0; i < GameBase.num_edicts; i++)
		{
			ent = GameBase.g_edicts[i];

			if (!ent.inuse)
				continue;

			GameBase.level.current_entity = ent;

			Math3D.VectorCopy(ent.s.origin, ent.s.old_origin);

			// if the ground entity moved, make sure we are still on it
			if (ent.groundentity != null && ent.groundentity.linkcount != ent.groundentity_linkcount)
			{
				ent.groundentity = null;

				if (0 == (ent.flags & (Defines.FL_SWIM | Defines.FL_FLY)) && (ent.svflags & Defines.SVF_MONSTER) != 0)
					M.M_CheckGround(ent);
			}

			if (i > 0 && i <= GameBase.maxclients.value)
			{
				PlayerClient.ClientBeginServerFrame(ent);

				continue;
			}

			GameBase.G_RunEntity(ent);
		}

		// see if it is time to end a deathmatch
		GameBase.CheckDMRules();

		// see if needpass needs updated
		GameBase.CheckNeedPass();

		// build the playerstate_t structures for all players
		GameBase.ClientEndServerFrames();
	}

	/**
     * This return a pointer to the structure with all entry points and global
     * variables.
     */
	public static void GetGameApi(game_import_t imp)
	{
		GameBase.gi = imp;

		GameBase.gi.pointcontents = o => { return SV_WORLD.SV_PointContents(o); };
	}

	static GameBase()
	{
		GameBase.GameBase1();
		GameBase.GameBase2();
	}
}
