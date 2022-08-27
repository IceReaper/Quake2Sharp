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

public class client_persistant_t
{
	public void set(client_persistant_t from)
	{
		this.userinfo = from.userinfo;
		this.netname = from.netname;
		this.hand = from.hand;
		this.connected = from.connected;
		this.health = from.health;
		this.max_health = from.max_health;
		this.savedFlags = from.savedFlags;
		this.selected_item = from.selected_item;
		Array.Copy(from.inventory, 0, this.inventory, 0, this.inventory.Length);
		this.max_bullets = from.max_bullets;
		this.max_shells = from.max_shells;
		this.max_rockets = from.max_rockets;
		this.max_grenades = from.max_grenades;
		this.max_cells = from.max_cells;
		this.max_slugs = from.max_slugs;
		this.weapon = from.weapon;
		this.lastweapon = from.lastweapon;
		this.power_cubes = from.power_cubes;
		this.score = from.score;
		this.game_helpchanged = from.game_helpchanged;
		this.helpchanged = from.helpchanged;
		this.spectator = from.spectator;
	}

	//	client data that stays across multiple level loads
	public string userinfo = "";
	public string netname = "";
	public int hand;
	public bool connected; // a loadgame will leave valid entities that

	// just don't have a connection yet

	// values saved and restored from edicts when changing levels
	public int health;
	public int max_health;
	public int savedFlags;
	public int selected_item;
	public int[] inventory = new int[Defines.MAX_ITEMS];

	// ammo capacities
	public int max_bullets;
	public int max_shells;
	public int max_rockets;
	public int max_grenades;
	public int max_cells;
	public int max_slugs;

	//pointer
	public gitem_t weapon;

	//pointer
	public gitem_t lastweapon;
	public int power_cubes; // used for tracking the cubes in coop games
	public int score; // for calculating total unit score in coop games
	public int game_helpchanged;
	public int helpchanged;
	public bool spectator; // client is a spectator

	/** Reads a client_persistant structure from a file. */
	public void read(BinaryReader f)
	{
		this.userinfo = f.ReadStringQ();
		this.netname = f.ReadStringQ();

		this.hand = f.ReadInt32();

		this.connected = f.ReadInt32() != 0;
		this.health = f.ReadInt32();

		this.max_health = f.ReadInt32();
		this.savedFlags = f.ReadInt32();
		this.selected_item = f.ReadInt32();

		for (var n = 0; n < Defines.MAX_ITEMS; n++)
			this.inventory[n] = f.ReadInt32();

		this.max_bullets = f.ReadInt32();
		this.max_shells = f.ReadInt32();
		this.max_rockets = f.ReadInt32();
		this.max_grenades = f.ReadInt32();
		this.max_cells = f.ReadInt32();
		this.max_slugs = f.ReadInt32();

		this.weapon = f.ReadItem();
		this.lastweapon = f.ReadItem();
		this.power_cubes = f.ReadInt32();
		this.score = f.ReadInt32();

		this.game_helpchanged = f.ReadInt32();
		this.helpchanged = f.ReadInt32();
		this.spectator = f.ReadInt32() != 0;
	}

	/** Writes a client_persistant structure to a file. */
	public void write(BinaryWriter f)
	{
		// client persistant_t
		f.WriteQ(this.userinfo);
		f.WriteQ(this.netname);

		f.Write(this.hand);

		f.Write(this.connected ? 1 : 0);
		f.Write(this.health);

		f.Write(this.max_health);
		f.Write(this.savedFlags);
		f.Write(this.selected_item);

		for (var n = 0; n < Defines.MAX_ITEMS; n++)
			f.Write(this.inventory[n]);

		f.Write(this.max_bullets);
		f.Write(this.max_shells);
		f.Write(this.max_rockets);
		f.Write(this.max_grenades);
		f.Write(this.max_cells);
		f.Write(this.max_slugs);

		f.Write(this.weapon);
		f.Write(this.lastweapon);
		f.Write(this.power_cubes);
		f.Write(this.score);

		f.Write(this.game_helpchanged);
		f.Write(this.helpchanged);
		f.Write(this.spectator ? 1 : 0);
	}
}