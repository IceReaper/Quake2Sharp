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

using System.IO;
using util;

/** Client data that stays across deathmatch respawns.*/
public class client_respawn_t
{
	/** What to set client->pers to on a respawn */
	public client_persistant_t coop_respawn = new();

	/** Level.framenum the client entered the game. */
	public int enterframe;

	/** frags, etc. */
	public int score;

	/** angles sent over in the last command. */
	public float[] cmd_angles = { 0, 0, 0 };

	/** client is a spectator. */
	public bool spectator;

	/** Copies the client respawn data. */
	public void set(client_respawn_t from)
	{
		this.coop_respawn.set(from.coop_respawn);
		this.enterframe = from.enterframe;
		this.score = from.score;
		Math3D.VectorCopy(from.cmd_angles, this.cmd_angles);
		this.spectator = from.spectator;
	}

	/** Clears the client reaspawn informations. */
	public void clear()
	{
		this.coop_respawn = new();
		this.enterframe = 0;
		this.score = 0;
		Math3D.VectorClear(this.cmd_angles);
		this.spectator = false;
	}

	/** Reads a client_respawn from a file. */
	public void read(BinaryReader f)
	{
		this.coop_respawn.read(f);
		this.enterframe = f.ReadInt32();
		this.score = f.ReadInt32();
		this.cmd_angles[0] = f.ReadSingle();
		this.cmd_angles[1] = f.ReadSingle();
		this.cmd_angles[2] = f.ReadSingle();
		this.spectator = f.ReadInt32() != 0;
	}

	/** Writes a client_respawn to a file. */
	public void write(BinaryWriter f)
	{
		this.coop_respawn.write(f);
		f.Write(this.enterframe);
		f.Write(this.score);
		f.Write(this.cmd_angles[0]);
		f.Write(this.cmd_angles[1]);
		f.Write(this.cmd_angles[2]);
		f.Write(this.spectator ? 1 : 0);
	}
}