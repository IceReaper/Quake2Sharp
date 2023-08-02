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
namespace QClient.game.types;

using adapters;

public class gitem_t
{
	private static int id;

	public gitem_t(int xxx)
	{
		this.index = xxx;
	}

	public gitem_t(string classname, EntInteractAdapter pickup, ItemUseAdapter use, ItemDropAdapter drop, EntThinkAdapter weaponthink)
	{
	}

	public gitem_t(
		string classname,
		EntInteractAdapter pickup,
		ItemUseAdapter use,
		ItemDropAdapter drop,
		EntThinkAdapter weaponthink,
		string pickup_sound,
		string world_model,
		int world_model_flags,
		string view_model,
		string icon,
		string pickup_name,
		int count_width,
		int quantity,
		string ammo,
		int flags,
		int weapmodel,
		gitem_armor_t info,
		int tag,
		string precaches
	)
	{
		this.classname = classname;
		this.pickup = pickup;
		this.use = use;
		this.drop = drop;
		this.weaponthink = weaponthink;
		this.pickup_sound = pickup_sound;
		this.world_model = world_model;
		this.world_model_flags = world_model_flags;
		this.view_model = view_model;
		this.icon = icon;
		this.pickup_name = pickup_name;
		this.count_width = count_width;
		this.quantity = quantity;
		this.ammo = ammo;
		this.flags = flags;
		this.weapmodel = weapmodel;
		this.info = info;
		this.tag = tag;
		this.precaches = precaches;

		this.index = gitem_t.id++;
	}

	public string classname; // spawning name
	public EntInteractAdapter pickup;
	public ItemUseAdapter use;
	public ItemDropAdapter drop;
	public EntThinkAdapter weaponthink;
	public string pickup_sound;
	public string world_model;
	public int world_model_flags;
	public string view_model;

	// client side info
	public string icon;
	public string pickup_name; // for printing on pickup
	private int count_width; // number of digits to display by icon
	public int quantity; // for ammo how much, for weapons how much is used per shot
	public string ammo; // for weapons
	public int flags; // IT_* flags
	public int weapmodel; // weapon model index (for weapons)
	public gitem_armor_t info;
	public int tag;
	public string precaches; // string of all models, sounds, and images this item will

	// use
	public int index;
}