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
namespace Quake2Sharp.server;

using game;
using game.types;
using qcommon;
using qcommon.types;
using sys;
using types;
using util;

public class SV_CCMDS
{
	/*
	===============================================================================
	
	OPERATOR CONSOLE ONLY COMMANDS
	
	These commands can only be entered from stdin or by a remote operator datagram
	===============================================================================
	*/

	/*
	====================
	SV_SetMaster_f
	
	Specify a list of master servers
	====================
	*/
	public static void SV_SetMaster_f()
	{
		int i, slot;

		// only dedicated servers send heartbeats
		if (Globals.dedicated.value == 0)
		{
			Com.Printf("Only dedicated servers use masters.\n");

			return;
		}

		// make sure the server is listed public
		Cvar.Set("public", "1");

		for (i = 1; i < Defines.MAX_MASTERS; i++)
			SV_MAIN.master_adr[i] = new();

		slot = 1; // slot 0 will always contain the id master

		for (i = 1; i < Cmd.Argc(); i++)
		{
			if (slot == Defines.MAX_MASTERS)
				break;

			if (!NET.StringToAdr(Cmd.Argv(i), SV_MAIN.master_adr[i]))
			{
				Com.Printf("Bad address: " + Cmd.Argv(i) + "\n");

				continue;
			}

			if (SV_MAIN.master_adr[slot].port == 0)
				SV_MAIN.master_adr[slot].port = Defines.PORT_MASTER;

			Com.Printf("Master server at " + NET.AdrToString(SV_MAIN.master_adr[slot]) + "\n");
			Com.Printf("Sending a ping.\n");

			Netchan.OutOfBandPrint(Defines.NS_SERVER, SV_MAIN.master_adr[slot], "ping");

			slot++;
		}

		SV_INIT.svs.last_heartbeat = -9999999;
	}

	/*
	==================
	SV_SetPlayer
	
	Sets sv_client and sv_player to the player with idnum Cmd.Argv(1)
	==================
	*/
	public static bool SV_SetPlayer()
	{
		client_t cl;
		int i;
		int idnum;
		string s;

		if (Cmd.Argc() < 2)
			return false;

		s = Cmd.Argv(1);

		// numeric values are just slot numbers
		if (s[0] >= '0' && s[0] <= '9')
		{
			idnum = Lib.atoi(Cmd.Argv(1));

			if (idnum < 0 || idnum >= SV_MAIN.maxclients.value)
			{
				Com.Printf("Bad client slot: " + idnum + "\n");

				return false;
			}

			SV_MAIN.sv_client = SV_INIT.svs.clients[idnum];
			SV_USER.sv_player = SV_MAIN.sv_client.edict;

			if (0 == SV_MAIN.sv_client.state)
			{
				Com.Printf("Client " + idnum + " is not active\n");

				return false;
			}

			return true;
		}

		// check for a name match
		for (i = 0; i < SV_MAIN.maxclients.value; i++)
		{
			cl = SV_INIT.svs.clients[i];

			if (0 == cl.state)
				continue;

			if (0 == Lib.strcmp(cl.name, s))
			{
				SV_MAIN.sv_client = cl;
				SV_USER.sv_player = SV_MAIN.sv_client.edict;

				return true;
			}
		}

		Com.Printf("Userid " + s + " is not on the server\n");

		return false;
	}
	/*
	===============================================================================
	
	SAVEGAME FILES
	
	===============================================================================
	*/

	/** Delete save files save/(number)/.  */
	public static void SV_WipeSavegame(string savename)
	{
		Com.DPrintf("SV_WipeSaveGame(" + savename + ")\n");

		var name = FS.Gamedir() + "/save/" + savename + "/server.ssv";

		if (File.Exists(name))
			File.Delete(name);

		name = FS.Gamedir() + "/save/" + savename + "/game.ssv";

		if (File.Exists(name))
			File.Delete(name);

		name = FS.Gamedir() + "/save/" + savename + "/*.sav";

		var f = Sys.FindFirst(name, 0, 0);

		while (f != null)
		{
			File.Delete(f);
			f = Sys.FindNext();
		}

		Sys.FindClose();

		name = FS.Gamedir() + "/save/" + savename + "/*.sv2";

		f = Sys.FindFirst(name, 0, 0);

		while (f != null)
		{
			File.Delete(f);
			f = Sys.FindNext();
		}

		Sys.FindClose();
	}

	/*
	================
	CopyFile
	================
	*/
	public static void CopyFile(string src, string dst)
	{
		File.Copy(src, dst);
	}

	/*
	================
	SV_CopySaveGame
	================
	*/
	public static void SV_CopySaveGame(string src, string dst)
	{
		Com.DPrintf("SV_CopySaveGame(" + src + "," + dst + ")\n");

		SV_CCMDS.SV_WipeSavegame(dst);

		// copy the savegame over
		var name = FS.Gamedir() + "/save/" + src + "/server.ssv";
		var name2 = FS.Gamedir() + "/save/" + dst + "/server.ssv";
		FS.CreatePath(name2);
		SV_CCMDS.CopyFile(name, name2);

		name = FS.Gamedir() + "/save/" + src + "/game.ssv";
		name2 = FS.Gamedir() + "/save/" + dst + "/game.ssv";
		SV_CCMDS.CopyFile(name, name2);

		var name1 = FS.Gamedir() + "/save/" + src + "/";
		name = FS.Gamedir() + "/save/" + src + "/*.sav";

		var found = Sys.FindFirst(name, 0, 0);

		while (found != null)
		{
			name = name1 + Path.GetFileName(found);
			name2 = FS.Gamedir() + "/save/" + dst + "/" + Path.GetFileName(found);

			SV_CCMDS.CopyFile(name, name2);

			// change sav to sv2
			name = name[..^3] + "sv2";
			name2 = name2[..^3] + "sv2";

			SV_CCMDS.CopyFile(name, name2);

			found = Sys.FindNext();
		}

		Sys.FindClose();
	}

	/*
	==============
	SV_WriteLevelFile
	
	==============
	*/
	public static void SV_WriteLevelFile()
	{
		string name;
		BinaryWriter f;

		Com.DPrintf("SV_WriteLevelFile()\n");

		name = FS.Gamedir() + "/save/current/" + SV_INIT.sv.name + ".sv2";

		try
		{
			f = new(File.OpenWrite(name));

			for (var i = 0; i < Defines.MAX_CONFIGSTRINGS; i++)
				f.WriteQ(SV_INIT.sv.configstrings[i]);

			CM.CM_WritePortalState(f);
			f.Close();
		}
		catch (Exception e)
		{
			Com.Printf("Failed to open " + name + "\n");
			Console.WriteLine(e);
		}

		name = FS.Gamedir() + "/save/current/" + SV_INIT.sv.name + ".sav";
		GameSave.WriteLevel(name);
	}

	/*
	==============
	SV_ReadLevelFile
	
	==============
	*/
	public static void SV_ReadLevelFile()
	{
		//char name[MAX_OSPATH];
		string name;
		BinaryReader f;

		Com.DPrintf("SV_ReadLevelFile()\n");

		name = FS.Gamedir() + "/save/current/" + SV_INIT.sv.name + ".sv2";

		try
		{
			f = new(File.OpenRead(name));

			for (var n = 0; n < Defines.MAX_CONFIGSTRINGS; n++)
				SV_INIT.sv.configstrings[n] = f.ReadStringQ();

			CM.CM_ReadPortalState(f);

			f.Close();
		}
		catch (Exception e1)
		{
			Com.Printf("Failed to open " + name + "\n");
			Console.WriteLine(e1);
		}

		name = FS.Gamedir() + "/save/current/" + SV_INIT.sv.name + ".sav";
		GameSave.ReadLevel(name);
	}

	/*
	==============
	SV_WriteServerFile
	
	==============
	*/
	public static void SV_WriteServerFile(bool autosave)
	{
		BinaryWriter f;
		cvar_t var;

		string filename, name, @string, comment;

		Com.DPrintf("SV_WriteServerFile(" + (autosave ? "true" : "false") + ")\n");

		filename = FS.Gamedir() + "/save/current/server.ssv";

		try
		{
			f = new(File.OpenWrite(filename));

			if (!autosave)
			{
				var c = DateTime.UtcNow;
				comment = $"{c.Hour,2}_{c.Minute,2} {c.Month,2}/{c.Day,2}  ";
				comment += SV_INIT.sv.configstrings[Defines.CS_NAME];
			}
			else
			{
				// autosaved
				comment = "ENTERING " + SV_INIT.sv.configstrings[Defines.CS_NAME];
			}

			f.WriteQ(comment);
			f.WriteQ(SV_INIT.svs.mapcmd);

			// write the mapcmd

			// write all CVAR_LATCH cvars
			// these will be things like coop, skill, deathmatch, etc
			for (var = Globals.cvar_vars; var != null; var = var.next)
			{
				if (0 == (var.flags & Defines.CVAR_LATCH))
					continue;

				if (var.name.Length >= Defines.MAX_OSPATH - 1 || var.@string.Length >= 128 - 1)
				{
					Com.Printf("Cvar too long: " + var.name + " = " + var.@string + "\n");

					continue;
				}

				name = var.name;
				@string = var.@string;

				try
				{
					f.WriteQ(name);
					f.WriteQ(@string);
				}
				catch (Exception)
				{
				}
			}

			// rst: for termination.
			f.WriteQ((string)null);
			f.Close();
		}
		catch (Exception)
		{
			Com.Printf("Couldn't write " + filename + "\n");
		}

		// write game state
		filename = FS.Gamedir() + "/save/current/game.ssv";
		GameSave.WriteGame(filename, autosave);
	}

	/*
	==============
	SV_ReadServerFile
	
	==============
	*/
	public static void SV_ReadServerFile()
	{
		string filename = "", name = "", @string, mapcmd;

		try
		{
			BinaryReader f;

			mapcmd = "";

			Com.DPrintf("SV_ReadServerFile()\n");

			filename = FS.Gamedir() + "/save/current/server.ssv";

			f = new(File.OpenRead(filename));

			// read the comment field but ignore
			f.ReadStringQ();

			// read the mapcmd
			mapcmd = f.ReadStringQ();

			// read all CVAR_LATCH cvars
			// these will be things like coop, skill, deathmatch, etc
			while (true)
			{
				name = f.ReadStringQ();

				if (name == null)
					break;

				@string = f.ReadStringQ();

				Com.DPrintf("Set " + name + " = " + @string + "\n");
				Cvar.ForceSet(name, @string);
			}

			f.Close();

			// start a new game fresh with new cvars
			SV_INIT.SV_InitGame();

			SV_INIT.svs.mapcmd = mapcmd;

			// read game state
			filename = FS.Gamedir() + "/save/current/game.ssv";
			GameSave.ReadGame(filename);
		}
		catch (Exception e)
		{
			Com.Printf("Couldn't read file " + filename + "\n");
			Console.WriteLine(e);
		}
	}

	//=========================================================

	/*
	==================
	SV_DemoMap_f
	
	Puts the server in demo mode on a specific map/cinematic
	==================
	*/
	public static void SV_DemoMap_f()
	{
		SV_INIT.SV_Map(true, Cmd.Argv(1), false);
	}

	/*
	==================
	SV_GameMap_f
	
	Saves the state of the map just being exited and goes to a new map.
	
	If the initial character of the map string is '*', the next map is
	in a new unit, so the current savegame directory is cleared of
	map files.
	
	Example:
	
	*inter.cin+jail
	
	Clears the archived maps, plays the inter.cin cinematic, then
	goes to map jail.bsp.
	==================
	*/
	public static void SV_GameMap_f()
	{
		if (Cmd.Argc() != 2)
		{
			Com.Printf("USAGE: gamemap <map>\n");

			return;
		}

		Com.DPrintf("SV_GameMap(" + Cmd.Argv(1) + ")\n");

		FS.CreatePath(FS.Gamedir() + "/save/current/");

		// check for clearing the current savegame
		var map = Cmd.Argv(1);

		if (map[0] == '*')
		{
			// wipe all the *.sav files
			SV_CCMDS.SV_WipeSavegame("current");
		}
		else
		{
			// save the map just exited
			if (SV_INIT.sv.state == Defines.ss_game)
			{
				// clear all the client inuse flags before saving so that
				// when the level is re-entered, the clients will spawn
				// at spawn points instead of occupying body shells
				client_t cl;
				var savedInuse = new bool[(int)SV_MAIN.maxclients.value];

				for (var i = 0; i < SV_MAIN.maxclients.value; i++)
				{
					cl = SV_INIT.svs.clients[i];
					savedInuse[i] = cl.edict.inuse;
					cl.edict.inuse = false;
				}

				SV_CCMDS.SV_WriteLevelFile();

				// we must restore these for clients to transfer over correctly
				for (var i = 0; i < SV_MAIN.maxclients.value; i++)
				{
					cl = SV_INIT.svs.clients[i];
					cl.edict.inuse = savedInuse[i];
				}

				savedInuse = null;
			}
		}

		// start up the next map
		SV_INIT.SV_Map(false, Cmd.Argv(1), false);

		// archive server state
		SV_INIT.svs.mapcmd = Cmd.Argv(1);

		// copy off the level to the autosave slot
		if (0 == Globals.dedicated.value)
		{
			SV_CCMDS.SV_WriteServerFile(true);
			SV_CCMDS.SV_CopySaveGame("current", "save0");
		}
	}

	/*
	==================
	SV_Map_f
	
	Goes directly to a given map without any savegame archiving.
	For development work
	==================
	*/
	public static void SV_Map_f()
	{
		string map;

		//char expanded[MAX_QPATH];
		string expanded;

		// if not a pcx, demo, or cinematic, check to make sure the level exists
		map = Cmd.Argv(1);

		if (map.IndexOf(".") < 0)
		{
			expanded = "maps/" + map + ".bsp";

			if (FS.LoadFile(expanded) == null)
			{
				Com.Printf("Can't find " + expanded + "\n");

				return;
			}
		}

		SV_INIT.sv.state = Defines.ss_dead; // don't save current level when changing

		SV_CCMDS.SV_WipeSavegame("current");
		SV_CCMDS.SV_GameMap_f();
	}
	/*
	=====================================================================
	
	  SAVEGAMES
	
	=====================================================================
	*/

	/*
	==============
	SV_Loadgame_f
	
	==============
	*/
	public static void SV_Loadgame_f()
	{
		if (Cmd.Argc() != 2)
		{
			Com.Printf("USAGE: loadgame <directory>\n");

			return;
		}

		Com.Printf("Loading game...\n");

		var dir = Cmd.Argv(1);

		if (dir.IndexOf("..") > -1 || dir.IndexOf("/") > -1 || dir.IndexOf("\\") > -1)
			Com.Printf("Bad savedir.\n");

		// make sure the server.ssv file exists
		var name = FS.Gamedir() + "/save/" + Cmd.Argv(1) + "/server.ssv";

		if (!File.Exists(name))
		{
			Com.Printf("No such savegame: " + name + "\n");

			return;
		}

		SV_CCMDS.SV_CopySaveGame(Cmd.Argv(1), "current");
		SV_CCMDS.SV_ReadServerFile();

		// go to the map
		SV_INIT.sv.state = Defines.ss_dead; // don't save current level when changing
		SV_INIT.SV_Map(false, SV_INIT.svs.mapcmd, true);
	}

	/*
	==============
	SV_Savegame_f
	
	==============
	*/
	public static void SV_Savegame_f()
	{
		string dir;

		if (SV_INIT.sv.state != Defines.ss_game)
		{
			Com.Printf("You must be in a game to save.\n");

			return;
		}

		if (Cmd.Argc() != 2)
		{
			Com.Printf("USAGE: savegame <directory>\n");

			return;
		}

		if (Cvar.VariableValue("deathmatch") != 0)
		{
			Com.Printf("Can't savegame in a deathmatch\n");

			return;
		}

		if (0 == Lib.strcmp(Cmd.Argv(1), "current"))
		{
			Com.Printf("Can't save to 'current'\n");

			return;
		}

		if (SV_MAIN.maxclients.value == 1 && SV_INIT.svs.clients[0].edict.client.ps.stats[Defines.STAT_HEALTH] <= 0)
		{
			Com.Printf("\nCan't savegame while dead!\n");

			return;
		}

		dir = Cmd.Argv(1);

		if (dir.IndexOf("..") > -1 || dir.IndexOf("/") > -1 || dir.IndexOf("\\") > -1)
			Com.Printf("Bad savedir.\n");

		Com.Printf("Saving game...\n");

		// archive current level, including all client edicts.
		// when the level is reloaded, they will be shells awaiting
		// a connecting client
		SV_CCMDS.SV_WriteLevelFile();

		// save server state
		try
		{
			SV_CCMDS.SV_WriteServerFile(false);
		}
		catch (Exception e)
		{
			Com.Printf("IOError in SV_WriteServerFile: " + e);
		}

		// copy it off
		SV_CCMDS.SV_CopySaveGame("current", dir);
		Com.Printf("Done.\n");
	}

	//===============================================================
	/*
	==================
	SV_Kick_f
	
	Kick a user off of the server
	==================
	*/
	public static void SV_Kick_f()
	{
		if (!SV_INIT.svs.initialized)
		{
			Com.Printf("No server running.\n");

			return;
		}

		if (Cmd.Argc() != 2)
		{
			Com.Printf("Usage: kick <userid>\n");

			return;
		}

		if (!SV_CCMDS.SV_SetPlayer())
			return;

		SV_SEND.SV_BroadcastPrintf(Defines.PRINT_HIGH, SV_MAIN.sv_client.name + " was kicked\n");

		// print directly, because the dropped client won't get the
		// SV_BroadcastPrintf message
		SV_SEND.SV_ClientPrintf(SV_MAIN.sv_client, Defines.PRINT_HIGH, "You were kicked from the game\n");
		SV_MAIN.SV_DropClient(SV_MAIN.sv_client);
		SV_MAIN.sv_client.lastmessage = SV_INIT.svs.realtime; // min case there is a funny zombie
	}

	/*
	================
	SV_Status_f
	================
	*/
	public static void SV_Status_f()
	{
		int i, j, l;
		client_t cl;
		string s;
		int ping;

		if (SV_INIT.svs.clients == null)
		{
			Com.Printf("No server running.\n");

			return;
		}

		Com.Printf("map              : " + SV_INIT.sv.name + "\n");

		Com.Printf("num score ping name            lastmsg address               qport \n");
		Com.Printf("--- ----- ---- --------------- ------- --------------------- ------\n");

		for (i = 0; i < SV_MAIN.maxclients.value; i++)
		{
			cl = SV_INIT.svs.clients[i];

			if (0 == cl.state)
				continue;

			Com.Printf($"{i,3} ");
			Com.Printf($"{cl.edict.client.ps.stats[Defines.STAT_FRAGS],5} ");

			if (cl.state == Defines.cs_connected)
				Com.Printf("CNCT ");
			else if (cl.state == Defines.cs_zombie)
				Com.Printf("ZMBI ");
			else
			{
				ping = cl.ping < 9999 ? cl.ping : 9999;
				Com.Printf($"{ping,4} ");
			}

			Com.Printf(cl.name);
			l = 16 - cl.name.Length;

			for (j = 0; j < l; j++)
				Com.Printf(" ");

			Com.Printf($"{SV_INIT.svs.realtime - cl.lastmessage,7} ");

			s = NET.AdrToString(cl.netchan.remote_address);
			Com.Printf(s);
			l = 22 - s.Length;

			for (j = 0; j < l; j++)
				Com.Printf(" ");

			Com.Printf($"{cl.netchan.qport,5}");

			Com.Printf("\n");
		}

		Com.Printf("\n");
	}

	/*
	==================
	SV_ConSay_f
	==================
	*/
	public static void SV_ConSay_f()
	{
		client_t client;
		int j;
		string p;
		string text; // char[1024];

		if (Cmd.Argc() < 2)
			return;

		text = "console: ";
		p = Cmd.Args();

		if (p[0] == '"')
			p = p[1..];

		text += p;

		for (j = 0; j < SV_MAIN.maxclients.value; j++)
		{
			client = SV_INIT.svs.clients[j];

			if (client.state != Defines.cs_spawned)
				continue;

			SV_SEND.SV_ClientPrintf(client, Defines.PRINT_CHAT, text + "\n");
		}
	}

	/*
	==================
	SV_Heartbeat_f
	==================
	*/
	public static void SV_Heartbeat_f()
	{
		SV_INIT.svs.last_heartbeat = -9999999;
	}

	/*
	===========
	SV_Serverinfo_f
	
	  Examine or change the serverinfo string
	===========
	*/
	public static void SV_Serverinfo_f()
	{
		Com.Printf("Server info settings:\n");
		Info.Print(Cvar.Serverinfo());
	}

	/*
	===========
	SV_DumpUser_f
	
	Examine all a users info strings
	===========
	*/
	public static void SV_DumpUser_f()
	{
		if (Cmd.Argc() != 2)
		{
			Com.Printf("Usage: info <userid>\n");

			return;
		}

		if (!SV_CCMDS.SV_SetPlayer())
			return;

		Com.Printf("userinfo\n");
		Com.Printf("--------\n");
		Info.Print(SV_MAIN.sv_client.userinfo);
	}

	/*
	==============
	SV_ServerRecord_f
	
	Begins server demo recording.  Every entity and every message will be
	recorded, but no playerinfo will be stored.  Primarily for demo merging.
	==============
	*/
	public static void SV_ServerRecord_f()
	{
		//char	name[MAX_OSPATH];
		string name;
		var buf_data = new byte[32768];
		sizebuf_t buf = new();
		int len;
		int i;

		if (Cmd.Argc() != 2)
		{
			Com.Printf("serverrecord <demoname>\n");

			return;
		}

		if (SV_INIT.svs.demofile != null)
		{
			Com.Printf("Already recording.\n");

			return;
		}

		if (SV_INIT.sv.state != Defines.ss_game)
		{
			Com.Printf("You must be in a level to record.\n");

			return;
		}

		//
		// open the demo file
		//
		name = FS.Gamedir() + "/demos/" + Cmd.Argv(1) + ".dm2";

		Com.Printf("recording to " + name + ".\n");
		FS.CreatePath(name);

		try
		{
			SV_INIT.svs.demofile = new(File.OpenWrite(name));
		}
		catch (Exception)
		{
			Com.Printf("ERROR: couldn't open.\n");

			return;
		}

		// setup a buffer to catch all multicasts
		SZ.Init(SV_INIT.svs.demo_multicast, SV_INIT.svs.demo_multicast_buf, SV_INIT.svs.demo_multicast_buf.Length);

		//
		// write a single giant fake message with all the startup info
		//
		SZ.Init(buf, buf_data, buf_data.Length);

		//
		// serverdata needs to go over for all types of servers
		// to make sure the protocol is right, and to set the gamedir
		//
		// send the serverdata
		MSG.WriteByte(buf, Defines.svc_serverdata);
		MSG.WriteLong(buf, Defines.PROTOCOL_VERSION);
		MSG.WriteLong(buf, SV_INIT.svs.spawncount);

		// 2 means server demo
		MSG.WriteByte(buf, 2); // demos are always attract loops
		MSG.WriteString(buf, Cvar.VariableString("gamedir"));
		MSG.WriteShort(buf, -1);

		// send full levelname
		MSG.WriteString(buf, SV_INIT.sv.configstrings[Defines.CS_NAME]);

		for (i = 0; i < Defines.MAX_CONFIGSTRINGS; i++)
		{
			if (SV_INIT.sv.configstrings[i].Length == 0)
			{
				MSG.WriteByte(buf, Defines.svc_configstring);
				MSG.WriteShort(buf, i);
				MSG.WriteString(buf, SV_INIT.sv.configstrings[i]);
			}
		}

		// write it to the demo file
		Com.DPrintf("signon message length: " + buf.cursize + "\n");
		len = EndianHandler.swapInt(buf.cursize);

		//fwrite(len, 4, 1, svs.demofile);
		//fwrite(buf.data, buf.cursize, 1, svs.demofile);
		try
		{
			SV_INIT.svs.demofile.Write(len);
			SV_INIT.svs.demofile.Write(buf.data, 0, buf.cursize);
		}
		catch (Exception e1)
		{
			Console.WriteLine(e1);
		}

		// the rest of the demo file will be individual frames
	}

	/*
	==============
	SV_ServerStop_f
	
	Ends server demo recording
	==============
	*/
	public static void SV_ServerStop_f()
	{
		if (SV_INIT.svs.demofile == null)
		{
			Com.Printf("Not doing a serverrecord.\n");

			return;
		}

		try
		{
			SV_INIT.svs.demofile.Close();
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}

		SV_INIT.svs.demofile = null;
		Com.Printf("Recording completed.\n");
	}

	/*
	===============
	SV_KillServer_f
	
	Kick everyone off, possibly in preparation for a new game
	
	===============
	*/
	public static void SV_KillServer_f()
	{
		if (!SV_INIT.svs.initialized)
			return;

		SV_MAIN.SV_Shutdown("Server was killed.\n", false);
		NET.ConfigServer(false); // close network sockets
	}

	/*
	===============
	SV_ServerCommand_f
	
	Let the game dll handle a command
	===============
	*/
	public static void SV_ServerCommand_f()
	{
		GameSVCmds.ServerCommand();
	}

	//===========================================================

	/*
	==================
	SV_InitOperatorCommands
	==================
	*/
	public static void SV_InitOperatorCommands()
	{
		Cmd.AddCommand("heartbeat", () => { SV_CCMDS.SV_Heartbeat_f(); });

		Cmd.AddCommand("kick", () => { SV_CCMDS.SV_Kick_f(); });

		Cmd.AddCommand("status", () => { SV_CCMDS.SV_Status_f(); });

		Cmd.AddCommand("serverinfo", () => { SV_CCMDS.SV_Serverinfo_f(); });

		Cmd.AddCommand("dumpuser", () => { SV_CCMDS.SV_DumpUser_f(); });

		Cmd.AddCommand("map", () => { SV_CCMDS.SV_Map_f(); });

		Cmd.AddCommand("demomap", () => { SV_CCMDS.SV_DemoMap_f(); });

		Cmd.AddCommand("gamemap", () => { SV_CCMDS.SV_GameMap_f(); });

		Cmd.AddCommand("setmaster", () => { SV_CCMDS.SV_SetMaster_f(); });

		if (Globals.dedicated.value != 0)
			Cmd.AddCommand("say", () => { SV_CCMDS.SV_ConSay_f(); });

		Cmd.AddCommand("serverrecord", () => { SV_CCMDS.SV_ServerRecord_f(); });

		Cmd.AddCommand("serverstop", () => { SV_CCMDS.SV_ServerStop_f(); });

		Cmd.AddCommand("save", () => { SV_CCMDS.SV_Savegame_f(); });

		Cmd.AddCommand("load", () => { SV_CCMDS.SV_Loadgame_f(); });

		Cmd.AddCommand("killserver", () => { SV_CCMDS.SV_KillServer_f(); });

		Cmd.AddCommand("sv", () => { SV_CCMDS.SV_ServerCommand_f(); });
	}
}