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
namespace Quake2Sharp.game.types;

using qcommon;
using util;

public class game_locals_t
{
	//
	//	this structure is left intact through an entire game
	//	it should be initialized at dll load time, and read/written to
	//	the server.ssv file for savegames
	//
	public string helpmessage1 = "";
	public string helpmessage2 = "";
	public int helpchanged; // flash F1 icon if non 0, play sound

	// and increment only if 1, 2, or 3
	public gclient_t[] clients = new gclient_t[Defines.MAX_CLIENTS];

	// can't store spawnpoint in level, because
	// it would get overwritten by the savegame restore
	public string spawnpoint = ""; // needed for coop respawns

	// store latched cvars here that we want to get at often
	public int maxclients;
	public int maxentities;

	// cross level triggers
	public int serverflags;

	// items
	public int num_items;
	public bool autosaved;

	/** Reads the game locals from a file. */
	public void load(BinaryReader f)
	{
		var date = f.ReadStringQ();

		this.helpmessage1 = f.ReadStringQ();
		this.helpmessage2 = f.ReadStringQ();

		this.helpchanged = f.ReadInt32();

		// gclient_t*

		this.spawnpoint = f.ReadStringQ();
		this.maxclients = f.ReadInt32();
		this.maxentities = f.ReadInt32();
		this.serverflags = f.ReadInt32();
		this.num_items = f.ReadInt32();
		this.autosaved = f.ReadInt32() != 0;

		// rst's checker :-)
		if (f.ReadInt32() != 1928)
			Com.DPrintf("error in loading game_locals, 1928\n");
	}

	/** Writes the game locals to a file. */
	public void write(BinaryWriter f)
	{
		f.WriteQ(DateTime.Now.ToString());

		f.WriteQ(this.helpmessage1);
		f.WriteQ(this.helpmessage2);

		f.Write(this.helpchanged);

		// gclient_t*

		f.WriteQ(this.spawnpoint);
		f.Write(this.maxclients);
		f.Write(this.maxentities);
		f.Write(this.serverflags);
		f.Write(this.num_items);
		f.Write(this.autosaved ? 1 : 0);

		// rst's checker :-)
		f.Write(1928);
	}
}