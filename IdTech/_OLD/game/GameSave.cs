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
using Quake2Sharp.util;
using System.Runtime.CompilerServices;

namespace Quake2Sharp.game;

public class GameSave
{
	public static void CreateEdicts()
	{
		GameBase.g_edicts = new edict_t[GameBase.game.maxentities];

		for (var i = 0; i < GameBase.game.maxentities; i++)
			GameBase.g_edicts[i] = new(i);
	}

	public static void CreateClients()
	{
		GameBase.game.clients = new gclient_t[GameBase.game.maxclients];

		for (var i = 0; i < GameBase.game.maxclients; i++)
			GameBase.game.clients[i] = new(i);
	}

	private static readonly Type[] preloadclasslist =
	{
		typeof(PlayerWeapon),
		typeof(AIAdapter),
		typeof(Cmd),
		typeof(EdictIterator),
		typeof(EndianHandler),
		typeof(EntBlockedAdapter),
		typeof(EntDieAdapter),
		typeof(EntDodgeAdapter),
		typeof(EntInteractAdapter),
		typeof(EntPainAdapter),
		typeof(EntThinkAdapter),
		typeof(EntTouchAdapter),
		typeof(EntUseAdapter),
		typeof(GameAI),
		typeof(GameBase),
		typeof(GameChase),
		typeof(GameCombat),
		typeof(GameFunc),
		typeof(GameMisc),
		typeof(GameSVCmds),
		typeof(GameSave),
		typeof(GameSpawn),
		typeof(GameTarget),
		typeof(GameTrigger),
		typeof(GameTurret),
		typeof(GameUtil),
		typeof(GameWeapon),
		typeof(Info),
		typeof(ItemDropAdapter),
		typeof(ItemUseAdapter),
		typeof(Monster),
		typeof(PlayerClient),
		typeof(PlayerHud),
		typeof(PlayerTrail),
		typeof(PlayerView),
		typeof(SuperAdapter),
		typeof(M_Actor),
		typeof(M_Berserk),
		typeof(M_Boss2),
		typeof(M_Boss3),
		typeof(M_Boss31),
		typeof(M_Boss32),
		typeof(M_Brain),
		typeof(M_Chick),
		typeof(M_Flash),
		typeof(M_Flipper),
		typeof(M_Float),
		typeof(M_Flyer),
		typeof(M_Gladiator),
		typeof(M_Gunner),
		typeof(M_Hover),
		typeof(M_Infantry),
		typeof(M_Insane),
		typeof(M_Medic),
		typeof(M_Mutant),
		typeof(M_Parasite),
		typeof(M_Player),
		typeof(M_Soldier),
		typeof(M_Supertank),
		typeof(M_Tank),
		typeof(GameItems),

		// DANGER! init as last, when all adatpers are != null
		typeof(GameItemList)
	};

	/**
     * InitGame
     *
     * This will be called when the dll is first loaded, which only happens when
     * a new game is started or a save game is loaded.
     */
	public static void InitGame()
	{
		GameBase.gi.dprintf("==== InitGame ====\n");

		// preload all classes to register the adapters
		for (var n = 0; n < GameSave.preloadclasslist.Length; n++)
		{
			try
			{
				RuntimeHelpers.RunClassConstructor(GameSave.preloadclasslist[n].TypeHandle);
			}
			catch (Exception)
			{
				clientserver.Com_DPrintf("error loading class: " + GameSave.preloadclasslist[n]);
			}
		}

		GameBase.gun_x = GameBase.gi.cvar("gun_x", "0", 0);
		GameBase.gun_y = GameBase.gi.cvar("gun_y", "0", 0);
		GameBase.gun_z = GameBase.gi.cvar("gun_z", "0", 0);

		//FIXME: sv_ prefix are wrong names for these variables
		GameBase.sv_rollspeed = GameBase.gi.cvar("sv_rollspeed", "200", 0);
		GameBase.sv_rollangle = GameBase.gi.cvar("sv_rollangle", "2", 0);
		GameBase.sv_maxvelocity = GameBase.gi.cvar("sv_maxvelocity", "2000", 0);
		GameBase.sv_gravity = GameBase.gi.cvar("sv_gravity", "800", 0);

		// latched vars
		GameBase.sv_cheats = GameBase.gi.cvar("cheats", "0", Defines.CVAR_SERVERINFO | Defines.CVAR_LATCH);
		GameBase.gi.cvar("gamename", Defines.GAMEVERSION, Defines.CVAR_SERVERINFO | Defines.CVAR_LATCH);

		GameBase.maxclients = GameBase.gi.cvar("maxclients", "4", Defines.CVAR_SERVERINFO | Defines.CVAR_LATCH);
		GameBase.maxspectators = GameBase.gi.cvar("maxspectators", "4", Defines.CVAR_SERVERINFO);
		GameBase.deathmatch = GameBase.gi.cvar("deathmatch", "0", Defines.CVAR_LATCH);
		GameBase.coop = GameBase.gi.cvar("coop", "0", Defines.CVAR_LATCH);
		GameBase.skill = GameBase.gi.cvar("skill", "0", Defines.CVAR_LATCH);
		GameBase.maxentities = GameBase.gi.cvar("maxentities", "1024", Defines.CVAR_LATCH);

		// change anytime vars
		GameBase.dmflags = GameBase.gi.cvar("dmflags", "0", Defines.CVAR_SERVERINFO);
		GameBase.fraglimit = GameBase.gi.cvar("fraglimit", "0", Defines.CVAR_SERVERINFO);
		GameBase.timelimit = GameBase.gi.cvar("timelimit", "0", Defines.CVAR_SERVERINFO);
		GameBase.password = GameBase.gi.cvar("password", "", Defines.CVAR_USERINFO);
		GameBase.spectator_password = GameBase.gi.cvar("spectator_password", "", Defines.CVAR_USERINFO);
		GameBase.needpass = GameBase.gi.cvar("needpass", "0", Defines.CVAR_SERVERINFO);
		GameBase.filterban = GameBase.gi.cvar("filterban", "1", 0);

		GameBase.g_select_empty = GameBase.gi.cvar("g_select_empty", "0", Defines.CVAR_ARCHIVE);

		GameBase.run_pitch = GameBase.gi.cvar("run_pitch", "0.002", 0);
		GameBase.run_roll = GameBase.gi.cvar("run_roll", "0.005", 0);
		GameBase.bob_up = GameBase.gi.cvar("bob_up", "0.005", 0);
		GameBase.bob_pitch = GameBase.gi.cvar("bob_pitch", "0.002", 0);
		GameBase.bob_roll = GameBase.gi.cvar("bob_roll", "0.002", 0);

		// flood control
		GameBase.flood_msgs = GameBase.gi.cvar("flood_msgs", "4", 0);
		GameBase.flood_persecond = GameBase.gi.cvar("flood_persecond", "4", 0);
		GameBase.flood_waitdelay = GameBase.gi.cvar("flood_waitdelay", "10", 0);

		// dm map list
		GameBase.sv_maplist = GameBase.gi.cvar("sv_maplist", "", 0);

		// items
		GameItems.InitItems();

		GameBase.game.helpmessage1 = "";
		GameBase.game.helpmessage2 = "";

		// initialize all entities for this game
		GameBase.game.maxentities = (int)GameBase.maxentities.value;
		GameSave.CreateEdicts();

		// initialize all clients for this game
		GameBase.game.maxclients = (int)GameBase.maxclients.value;

		GameSave.CreateClients();

		GameBase.num_edicts = GameBase.game.maxclients + 1;
	}

	/**
     * WriteGame
     *
     * This will be called whenever the game goes to a new level, and when the
     * user explicitly saves the game.
     *
     * Game information include cross level data, like multi level triggers,
     * help computer info, and all client states.
     *
     * A single player death will automatically restore from the last save
     * position.
     */
	public static void WriteGame(string filename, bool autosave)
	{
		try
		{
			BinaryWriter f;

			if (!autosave)
				PlayerClient.SaveClientData();

			f = new(File.OpenWrite(filename));

			if (f == null)
				GameBase.gi.error("Couldn't write to " + filename);

			GameBase.game.autosaved = autosave;
			GameBase.game.write(f);
			GameBase.game.autosaved = false;

			for (var i = 0; i < GameBase.game.maxclients; i++)
				GameBase.game.clients[i].write(f);

			f.Close();
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}
	}

	public static void ReadGame(string filename)
	{
		BinaryReader f = null;

		try
		{
			f = new(File.OpenRead(filename));
			GameSave.CreateEdicts();

			GameBase.game.load(f);

			for (var i = 0; i < GameBase.game.maxclients; i++)
			{
				GameBase.game.clients[i] = new(i);
				GameBase.game.clients[i].read(f);
			}

			f.Close();
		}

		catch (Exception e)
		{
			Console.WriteLine(e);
		}
	}

	/**
     * WriteLevel
     */
	public static void WriteLevel(string filename)
	{
		try
		{
			int i;
			edict_t ent;
			BinaryWriter f;

			f = new(File.OpenWrite(filename));

			if (f == null)
				GameBase.gi.error("Couldn't open for writing: " + filename);

			// write out level_locals_t
			GameBase.level.write(f);

			// write out all the entities
			for (i = 0; i < GameBase.num_edicts; i++)
			{
				ent = GameBase.g_edicts[i];

				if (!ent.inuse)
					continue;

				f.Write(i);
				ent.write(f);
			}

			i = -1;
			f.Write(-1);

			f.Close();
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}
	}

	/**
     * ReadLevel
     *
     * SpawnEntities will allready have been called on the level the same way it
     * was when the level was saved.
     *
     * That is necessary to get the baselines set up identically.
     *
     * The server will have cleared all of the world links before calling
     * ReadLevel.
     *
     * No clients are connected yet.
     */
	public static void ReadLevel(string filename)
	{
		try
		{
			edict_t ent;

			BinaryReader f = new(File.OpenRead(filename));

			if (f == null)
				GameBase.gi.error("Couldn't read level file " + filename);

			// wipe all the entities
			GameSave.CreateEdicts();

			GameBase.num_edicts = (int)GameBase.maxclients.value + 1;

			// load the level locals
			GameBase.level.read(f);

			// load all the entities
			while (true)
			{
				var entnum = f.ReadInt32();

				if (entnum == -1)
					break;

				if (entnum >= GameBase.num_edicts)
					GameBase.num_edicts = entnum + 1;

				ent = GameBase.g_edicts[entnum];
				ent.read(f);
				ent.cleararealinks();
				GameBase.gi.linkentity(ent);
			}

			f.Close();

			// mark all clients as unconnected
			for (var i = 0; i < GameBase.maxclients.value; i++)
			{
				ent = GameBase.g_edicts[i + 1];
				ent.client = GameBase.game.clients[i];
				ent.client.pers.connected = false;
			}

			// do any load time things at this point
			for (var i = 0; i < GameBase.num_edicts; i++)
			{
				ent = GameBase.g_edicts[i];

				if (!ent.inuse)
					continue;

				// fire any cross-level triggers
				if (ent.classname != null)
				{
					if (Lib.strcmp(ent.classname, "target_crosslevel_target") == 0)
						ent.nextthink = GameBase.level.time + ent.delay;
				}
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}
	}
}
