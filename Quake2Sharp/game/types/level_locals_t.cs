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

using util;

public class level_locals_t
{
	//	this structure is cleared as each map is entered
	//	it is read/written to the level.sav file for savegames
	//
	public int framenum;
	public float time;
	public string level_name = ""; // the descriptive name (Outer Base, etc)
	public string mapname = ""; // the server name (base1, etc)
	public string nextmap = ""; // go here when fraglimit is hit

	// intermission state
	public float intermissiontime; // time the intermission was started
	public string changemap;
	public bool exitintermission;
	public float[] intermission_origin = { 0, 0, 0 };
	public float[] intermission_angle = { 0, 0, 0 };
	public edict_t sight_client; // changed once each frame for coop games
	public edict_t sight_entity;
	public int sight_entity_framenum;
	public edict_t sound_entity;
	public int sound_entity_framenum;
	public edict_t sound2_entity;
	public int sound2_entity_framenum;
	public int pic_health;
	public int total_secrets;
	public int found_secrets;
	public int total_goals;
	public int found_goals;
	public int total_monsters;
	public int killed_monsters;
	public edict_t current_entity; // entity running from G_RunFrame
	public int body_que; // dead bodies
	public int power_cubes; // ugly necessity for coop

	/** Writes the levellocales to the file.*/
	public void write(BinaryWriter f)
	{
		f.Write(this.framenum);
		f.Write(this.time);
		f.WriteQ(this.level_name);
		f.WriteQ(this.mapname);
		f.WriteQ(this.nextmap);
		f.Write(this.intermissiontime);
		f.WriteQ(this.changemap);
		f.Write(this.exitintermission);
		f.Write(this.intermission_origin);
		f.Write(this.intermission_angle);
		f.Write(this.sight_client);

		f.Write(this.sight_entity);
		f.Write(this.sight_entity_framenum);

		f.Write(this.sound_entity);
		f.Write(this.sound_entity_framenum);
		f.Write(this.sound2_entity);
		f.Write(this.sound2_entity_framenum);

		f.Write(this.pic_health);

		f.Write(this.total_secrets);
		f.Write(this.found_secrets);

		f.Write(this.total_goals);
		f.Write(this.found_goals);
		f.Write(this.total_monsters);
		f.Write(this.killed_monsters);

		f.Write(this.current_entity);
		f.Write(this.body_que); // dead bodies
		f.Write(this.power_cubes); // ugly necessity for coop

		// rst's checker :-)
		f.Write(4711);
	}

	/** Reads the level locals from the file. */
	public void read(BinaryReader f)
	{
		this.framenum = f.ReadInt32();
		this.time = f.ReadSingle();
		this.level_name = f.ReadStringQ();
		this.mapname = f.ReadStringQ();
		this.nextmap = f.ReadStringQ();
		this.intermissiontime = f.ReadSingle();
		this.changemap = f.ReadStringQ();
		this.exitintermission = f.ReadBoolean();
		this.intermission_origin = f.ReadVector();
		this.intermission_angle = f.ReadVector();
		this.sight_client = f.ReadEdictRef();

		this.sight_entity = f.ReadEdictRef();
		this.sight_entity_framenum = f.ReadInt32();

		this.sound_entity = f.ReadEdictRef();
		this.sound_entity_framenum = f.ReadInt32();
		this.sound2_entity = f.ReadEdictRef();
		this.sound2_entity_framenum = f.ReadInt32();

		this.pic_health = f.ReadInt32();

		this.total_secrets = f.ReadInt32();
		this.found_secrets = f.ReadInt32();

		this.total_goals = f.ReadInt32();
		this.found_goals = f.ReadInt32();
		this.total_monsters = f.ReadInt32();
		this.killed_monsters = f.ReadInt32();

		this.current_entity = f.ReadEdictRef();
		this.body_que = f.ReadInt32(); // dead bodies
		this.power_cubes = f.ReadInt32(); // ugly necessity for coop

		// rst's checker :-)
		if (f.ReadInt32() != 4711)
			Console.WriteLine("error in reading level_locals.");
	}
}