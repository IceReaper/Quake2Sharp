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
namespace QClient.server;

using client;
using game;
using game.types;
using qcommon;
using sys;
using types;
using util;

public class SV_INIT
{
	/**
     * SV_FindIndex.
     */
	public static int SV_FindIndex(string name, int start, int max, bool create)
	{
		int i;

		if (name == null || name.Length == 0)
			return 0;

		for (i = 1; i < max && SV_INIT.sv.configstrings[start + i] != null; i++)
		{
			if (0 == Lib.strcmp(SV_INIT.sv.configstrings[start + i], name))
				return i;
		}

		if (!create)
			return 0;

		if (i == max)
			Com.Error(Defines.ERR_DROP, "*Index: overflow");

		SV_INIT.sv.configstrings[start + i] = name;

		if (SV_INIT.sv.state != Defines.ss_loading)
		{
			// send the update to everyone
			SZ.Clear(SV_INIT.sv.multicast);
			MSG.WriteChar(SV_INIT.sv.multicast, Defines.svc_configstring);
			MSG.WriteShort(SV_INIT.sv.multicast, start + i);
			MSG.WriteString(SV_INIT.sv.multicast, name);
			SV_SEND.SV_Multicast(Globals.vec3_origin, Defines.MULTICAST_ALL_R);
		}

		return i;
	}

	public static int SV_ModelIndex(string name)
	{
		return SV_INIT.SV_FindIndex(name, Defines.CS_MODELS, Defines.MAX_MODELS, true);
	}

	public static int SV_SoundIndex(string name)
	{
		return SV_INIT.SV_FindIndex(name, Defines.CS_SOUNDS, Defines.MAX_SOUNDS, true);
	}

	public static int SV_ImageIndex(string name)
	{
		return SV_INIT.SV_FindIndex(name, Defines.CS_IMAGES, Defines.MAX_IMAGES, true);
	}

	/**
     * SV_CreateBaseline
     * 
     * Entity baselines are used to compress the update messages to the clients --
     * only the fields that differ from the baseline will be transmitted.
     */
	public static void SV_CreateBaseline()
	{
		edict_t svent;
		int entnum;

		for (entnum = 1; entnum < GameBase.num_edicts; entnum++)
		{
			svent = GameBase.g_edicts[entnum];

			if (!svent.inuse)
				continue;

			if (0 == svent.s.modelindex && 0 == svent.s.sound && 0 == svent.s.effects)
				continue;

			svent.s.number = entnum;

			// take current state as baseline
			Math3D.VectorCopy(svent.s.origin, svent.s.old_origin);
			SV_INIT.sv.baselines[entnum].set(svent.s);
		}
	}

	/** 
     * SV_CheckForSavegame.
     */
	public static void SV_CheckForSavegame()
	{
		string name;

		int i;

		if (SV_MAIN.sv_noreload.value != 0)
			return;

		if (Cvar.VariableValue("deathmatch") != 0)
			return;

		name = FS.Gamedir() + "/save/current/" + SV_INIT.sv.name + ".sav";

		if (!File.Exists(name))
			return;

		SV_WORLD.SV_ClearWorld();

		// get configstrings and areaportals
		SV_CCMDS.SV_ReadLevelFile();

		if (!SV_INIT.sv.loadgame)
		{
			// coming back to a level after being in a different
			// level, so run it for ten seconds

			// rlava2 was sending too many lightstyles, and overflowing the
			// reliable data. temporarily changing the server state to loading
			// prevents these from being passed down.
			int previousState; // PGM

			previousState = SV_INIT.sv.state; // PGM
			SV_INIT.sv.state = Defines.ss_loading; // PGM

			for (i = 0; i < 100; i++)
				GameBase.G_RunFrame();

			SV_INIT.sv.state = previousState; // PGM
		}
	}

	/**
     * SV_SpawnServer.
     * 
     * Change the server to a new map, taking all connected clients along with
     * it.
     */
	public static void SV_SpawnServer(string server, string spawnpoint, int serverstate, bool attractloop, bool loadgame)
	{
		int i;
		var checksum = 0;

		if (attractloop)
			Cvar.Set("paused", "0");

		Com.Printf("------- Server Initialization -------\n");

		Com.DPrintf("SpawnServer: " + server + "\n");

		if (SV_INIT.sv.demofile != null)
		{
			try
			{
				SV_INIT.sv.demofile.Close();
			}
			catch (Exception)
			{
			}
		}

		// any partially connected client will be restarted
		SV_INIT.svs.spawncount++;

		SV_INIT.sv.state = Defines.ss_dead;

		Globals.server_state = SV_INIT.sv.state;

		// wipe the entire per-level structure
		SV_INIT.sv = new();

		SV_INIT.svs.realtime = 0;
		SV_INIT.sv.loadgame = loadgame;
		SV_INIT.sv.attractloop = attractloop;

		// save name for levels that don't set message
		SV_INIT.sv.configstrings[Defines.CS_NAME] = server;

		if (Cvar.VariableValue("deathmatch") != 0)
		{
			SV_INIT.sv.configstrings[Defines.CS_AIRACCEL] = "" + SV_MAIN.sv_airaccelerate.value;
			PMove.pm_airaccelerate = SV_MAIN.sv_airaccelerate.value;
		}
		else
		{
			SV_INIT.sv.configstrings[Defines.CS_AIRACCEL] = "0";
			PMove.pm_airaccelerate = 0;
		}

		SZ.Init(SV_INIT.sv.multicast, SV_INIT.sv.multicast_buf, SV_INIT.sv.multicast_buf.Length);

		SV_INIT.sv.name = server;

		// leave slots at start for clients only
		for (i = 0; i < SV_MAIN.maxclients.value; i++)
		{
			// needs to reconnect
			if (SV_INIT.svs.clients[i].state > Defines.cs_connected)
				SV_INIT.svs.clients[i].state = Defines.cs_connected;

			SV_INIT.svs.clients[i].lastframe = -1;
		}

		SV_INIT.sv.time = 1000;

		SV_INIT.sv.name = server;
		SV_INIT.sv.configstrings[Defines.CS_NAME] = server;

		int[] iw = { checksum };

		if (serverstate != Defines.ss_game)
			SV_INIT.sv.models[1] = CM.CM_LoadMap("", false, iw); // no real map
		else
		{
			SV_INIT.sv.configstrings[Defines.CS_MODELS + 1] = "maps/" + server + ".bsp";
			SV_INIT.sv.models[1] = CM.CM_LoadMap(SV_INIT.sv.configstrings[Defines.CS_MODELS + 1], false, iw);
		}

		checksum = iw[0];
		SV_INIT.sv.configstrings[Defines.CS_MAPCHECKSUM] = "" + checksum;

		// clear physics interaction links

		SV_WORLD.SV_ClearWorld();

		for (i = 1; i < CM.CM_NumInlineModels(); i++)
		{
			SV_INIT.sv.configstrings[Defines.CS_MODELS + 1 + i] = "*" + i;

			// copy references
			SV_INIT.sv.models[i + 1] = CM.InlineModel(SV_INIT.sv.configstrings[Defines.CS_MODELS + 1 + i]);
		}

		// spawn the rest of the entities on the map

		// precache and static commands can be issued during
		// map initialization

		SV_INIT.sv.state = Defines.ss_loading;
		Globals.server_state = SV_INIT.sv.state;

		// load and spawn all other entities
		GameSpawn.SpawnEntities(SV_INIT.sv.name, CM.CM_EntityString(), spawnpoint);

		// run two frames to allow everything to settle
		GameBase.G_RunFrame();
		GameBase.G_RunFrame();

		// all precaches are complete
		SV_INIT.sv.state = serverstate;
		Globals.server_state = SV_INIT.sv.state;

		// create a baseline for more efficient communications
		SV_INIT.SV_CreateBaseline();

		// check for a savegame
		SV_INIT.SV_CheckForSavegame();

		// set serverinfo variable
		Cvar.FullSet("mapname", SV_INIT.sv.name, Defines.CVAR_SERVERINFO | Defines.CVAR_NOSET);
	}

	/**
     * SV_InitGame.
     * 
     * A brand new game has been started.
     */
	public static void SV_InitGame()
	{
		int i;
		edict_t ent;

		//char idmaster[32];
		string idmaster;

		if (SV_INIT.svs.initialized)
		{
			// cause any connected clients to reconnect
			SV_MAIN.SV_Shutdown("Server restarted\n", true);
		}
		else
		{
			// make sure the client is down
			Cl.Drop();
			SCR.BeginLoadingPlaque();
		}

		// get any latched variable changes (maxclients, etc)
		Cvar.GetLatchedVars();

		SV_INIT.svs.initialized = true;

		if (Cvar.VariableValue("coop") != 0 && Cvar.VariableValue("deathmatch") != 0)
		{
			Com.Printf("Deathmatch and Coop both set, disabling Coop\n");
			Cvar.FullSet("coop", "0", Defines.CVAR_SERVERINFO | Defines.CVAR_LATCH);
		}

		// dedicated servers are can't be single player and are usually DM
		// so unless they explicity set coop, force it to deathmatch
		if (Globals.dedicated.value != 0)
		{
			if (0 == Cvar.VariableValue("coop"))
				Cvar.FullSet("deathmatch", "1", Defines.CVAR_SERVERINFO | Defines.CVAR_LATCH);
		}

		// init clients
		if (Cvar.VariableValue("deathmatch") != 0)
		{
			if (SV_MAIN.maxclients.value <= 1)
				Cvar.FullSet("maxclients", "8", Defines.CVAR_SERVERINFO | Defines.CVAR_LATCH);
			else if (SV_MAIN.maxclients.value > Defines.MAX_CLIENTS)
				Cvar.FullSet("maxclients", "" + Defines.MAX_CLIENTS, Defines.CVAR_SERVERINFO | Defines.CVAR_LATCH);
		}
		else if (Cvar.VariableValue("coop") != 0)
		{
			if (SV_MAIN.maxclients.value <= 1 || SV_MAIN.maxclients.value > 4)
				Cvar.FullSet("maxclients", "4", Defines.CVAR_SERVERINFO | Defines.CVAR_LATCH);
		}
		else // non-deathmatch, non-coop is one player
			Cvar.FullSet("maxclients", "1", Defines.CVAR_SERVERINFO | Defines.CVAR_LATCH);

		SV_INIT.svs.spawncount = Lib.rand();
		SV_INIT.svs.clients = new client_t[(int)SV_MAIN.maxclients.value];

		for (var n = 0; n < SV_INIT.svs.clients.Length; n++)
		{
			SV_INIT.svs.clients[n] = new();
			SV_INIT.svs.clients[n].serverindex = n;
		}

		SV_INIT.svs.num_client_entities = (int)SV_MAIN.maxclients.value * Defines.UPDATE_BACKUP * 64; //ok.

		SV_INIT.svs.client_entities = new entity_state_t[SV_INIT.svs.num_client_entities];

		for (var n = 0; n < SV_INIT.svs.client_entities.Length; n++)
			SV_INIT.svs.client_entities[n] = new(null);

		// init network stuff
		NET.ConfigServer(SV_MAIN.maxclients.value > 1);

		// heartbeats will always be sent to the id master
		SV_INIT.svs.last_heartbeat = -99999; // send immediately
		idmaster = "192.246.40.37:" + Defines.PORT_MASTER;
		NET.StringToAdr(idmaster, SV_MAIN.master_adr[0]);

		// init game
		SV_GAME.SV_InitGameProgs();

		for (i = 0; i < SV_MAIN.maxclients.value; i++)
		{
			ent = GameBase.g_edicts[i + 1];
			SV_INIT.svs.clients[i].edict = ent;
			SV_INIT.svs.clients[i].lastcmd = new();
		}
	}

	private static string firstmap = "";

	/**
     * SV_Map
     * 
     * the full syntax is:
     * 
     * map [*] <map>$ <startspot>+ <nextserver>
     * 
     * command from the console or progs. Map can also be a.cin, .pcx, or .dm2 file.
     * 
     * Nextserver is used to allow a cinematic to play, then proceed to
     * another level:
     * 
     * map tram.cin+jail_e3
     */
	public static void SV_Map(bool attractloop, string levelstring, bool loadgame)
	{
		int l;
		string level, spawnpoint;

		SV_INIT.sv.loadgame = loadgame;
		SV_INIT.sv.attractloop = attractloop;

		if (SV_INIT.sv.state == Defines.ss_dead && !SV_INIT.sv.loadgame)
			SV_INIT.SV_InitGame(); // the game is just starting

		level = levelstring; // bis hier her ok.

		// if there is a + in the map, set nextserver to the remainder

		var c = level.IndexOf('+');

		if (c != -1)
		{
			Cvar.Set("nextserver", "gamemap \"" + level[(c + 1)..] + "\"");
			level = level[..c];
		}
		else
			Cvar.Set("nextserver", "");

		// rst: base1 works for full, damo1 works for demo, so we need to store first map.
		if (SV_INIT.firstmap.Length == 0)
		{
			if (!levelstring.EndsWith(".cin") && !levelstring.EndsWith(".pcx") && !levelstring.EndsWith(".dm2"))
			{
				var pos1 = levelstring.IndexOf('+');
				SV_INIT.firstmap = levelstring[(pos1 + 1)..];
			}
		}

		// ZOID: special hack for end game screen in coop mode
		if (Cvar.VariableValue("coop") != 0 && level.Equals("victory.pcx"))
			Cvar.Set("nextserver", "gamemap \"*" + SV_INIT.firstmap + "\"");

		// if there is a $, use the remainder as a spawnpoint
		var pos = level.IndexOf('$');

		if (pos != -1)
		{
			spawnpoint = level[(pos + 1)..];
			level = level[..pos];
		}
		else
			spawnpoint = "";

		// skip the end-of-unit flag * if necessary
		if (level[0] == '*')
			level = level[1..];

		l = level.Length;

		if (l > 4 && level.EndsWith(".cin"))
		{
			SCR.BeginLoadingPlaque(); // for local system
			SV_SEND.SV_BroadcastCommand("changing\n");
			SV_INIT.SV_SpawnServer(level, spawnpoint, Defines.ss_cinematic, attractloop, loadgame);
		}
		else if (l > 4 && level.EndsWith(".dm2"))
		{
			SCR.BeginLoadingPlaque(); // for local system
			SV_SEND.SV_BroadcastCommand("changing\n");
			SV_INIT.SV_SpawnServer(level, spawnpoint, Defines.ss_demo, attractloop, loadgame);
		}
		else if (l > 4 && level.EndsWith(".pcx"))
		{
			SCR.BeginLoadingPlaque(); // for local system
			SV_SEND.SV_BroadcastCommand("changing\n");
			SV_INIT.SV_SpawnServer(level, spawnpoint, Defines.ss_pic, attractloop, loadgame);
		}
		else
		{
			SCR.BeginLoadingPlaque(); // for local system
			SV_SEND.SV_BroadcastCommand("changing\n");
			SV_SEND.SV_SendClientMessages();
			SV_INIT.SV_SpawnServer(level, spawnpoint, Defines.ss_game, attractloop, loadgame);
			Cbuf.CopyToDefer();
		}

		SV_SEND.SV_BroadcastCommand("reconnect\n");
	}

	public static server_static_t svs = new(); // persistant

	// server info
	public static server_t sv = new(); // local server
}