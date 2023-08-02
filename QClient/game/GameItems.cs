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
namespace QClient.game;

using adapters;
using qcommon;
using System.Text.RegularExpressions;
using types;
using util;

public class GameItems
{
	public static gitem_armor_t jacketarmor_info = new(25, 50, .30f, .00f, Defines.ARMOR_JACKET);
	public static gitem_armor_t combatarmor_info = new(50, 100, .60f, .30f, Defines.ARMOR_COMBAT);
	public static gitem_armor_t bodyarmor_info = new(100, 200, .80f, .60f, Defines.ARMOR_BODY);
	private static int quad_drop_timeout_hack;
	private static int jacket_armor_index;
	private static int combat_armor_index;
	private static int body_armor_index;
	private static int power_screen_index;
	private static int power_shield_index;

	private static readonly EntThinkAdapter DoRespawn = new("do_respawn", ent =>
	{
		if (ent.team != null)
		{
			edict_t master;
			int count;
			var choice = 0;

			master = ent.teammaster;

			// count the depth
			for (count = 0, ent = master; ent != null; ent = ent.chain, count++)
				;

			choice = Lib.rand() % count;

			for (count = 0, ent = master; count < choice; ent = ent.chain, count++)
				;
		}

		ent.svflags &= ~Defines.SVF_NOCLIENT;
		ent.solid = Defines.SOLID_TRIGGER;
		GameBase.gi.linkentity(ent);

		// send an effect
		ent.s.@event = Defines.EV_ITEM_RESPAWN;

		return false;
	});

	public static EntInteractAdapter Pickup_Pack = new("pickup_pack", (ent, other) =>
	{
		gitem_t item;
		int index;

		if (other.client.pers.max_bullets < 300)
			other.client.pers.max_bullets = 300;

		if (other.client.pers.max_shells < 200)
			other.client.pers.max_shells = 200;

		if (other.client.pers.max_rockets < 100)
			other.client.pers.max_rockets = 100;

		if (other.client.pers.max_grenades < 100)
			other.client.pers.max_grenades = 100;

		if (other.client.pers.max_cells < 300)
			other.client.pers.max_cells = 300;

		if (other.client.pers.max_slugs < 100)
			other.client.pers.max_slugs = 100;

		item = GameItems.FindItem("Bullets");

		if (item != null)
		{
			index = GameItems.ITEM_INDEX(item);
			other.client.pers.inventory[index] += item.quantity;

			if (other.client.pers.inventory[index] > other.client.pers.max_bullets)
				other.client.pers.inventory[index] = other.client.pers.max_bullets;
		}

		item = GameItems.FindItem("Shells");

		if (item != null)
		{
			index = GameItems.ITEM_INDEX(item);
			other.client.pers.inventory[index] += item.quantity;

			if (other.client.pers.inventory[index] > other.client.pers.max_shells)
				other.client.pers.inventory[index] = other.client.pers.max_shells;
		}

		item = GameItems.FindItem("Cells");

		if (item != null)
		{
			index = GameItems.ITEM_INDEX(item);
			other.client.pers.inventory[index] += item.quantity;

			if (other.client.pers.inventory[index] > other.client.pers.max_cells)
				other.client.pers.inventory[index] = other.client.pers.max_cells;
		}

		item = GameItems.FindItem("Grenades");

		if (item != null)
		{
			index = GameItems.ITEM_INDEX(item);
			other.client.pers.inventory[index] += item.quantity;

			if (other.client.pers.inventory[index] > other.client.pers.max_grenades)
				other.client.pers.inventory[index] = other.client.pers.max_grenades;
		}

		item = GameItems.FindItem("Rockets");

		if (item != null)
		{
			index = GameItems.ITEM_INDEX(item);
			other.client.pers.inventory[index] += item.quantity;

			if (other.client.pers.inventory[index] > other.client.pers.max_rockets)
				other.client.pers.inventory[index] = other.client.pers.max_rockets;
		}

		item = GameItems.FindItem("Slugs");

		if (item != null)
		{
			index = GameItems.ITEM_INDEX(item);
			other.client.pers.inventory[index] += item.quantity;

			if (other.client.pers.inventory[index] > other.client.pers.max_slugs)
				other.client.pers.inventory[index] = other.client.pers.max_slugs;
		}

		if (0 == (ent.spawnflags & Defines.DROPPED_ITEM) && GameBase.deathmatch.value != 0)
			GameItems.SetRespawn(ent, ent.item.quantity);

		return true;
	});

	public static readonly EntInteractAdapter Pickup_Health = new("pickup_health", (ent, other) =>
	{
		if (0 == (ent.style & Defines.HEALTH_IGNORE_MAX))
		{
			if (other.health >= other.max_health)
				return false;
		}

		other.health += ent.count;

		if (0 == (ent.style & Defines.HEALTH_IGNORE_MAX))
		{
			if (other.health > other.max_health)
				other.health = other.max_health;
		}

		if (0 != (ent.style & Defines.HEALTH_TIMED))
		{
			ent.think = GameUtil.MegaHealth_think;
			ent.nextthink = GameBase.level.time + 5f;
			ent.owner = other;
			ent.flags |= Defines.FL_RESPAWN;
			ent.svflags |= Defines.SVF_NOCLIENT;
			ent.solid = Defines.SOLID_NOT;
		}
		else
		{
			if (!((ent.spawnflags & Defines.DROPPED_ITEM) != 0) && GameBase.deathmatch.value != 0)
				GameItems.SetRespawn(ent, 30);
		}

		return true;
	});

	public static EntTouchAdapter Touch_Item = new("touch_item", (ent, other, plane, surf) =>
	{
		bool taken;

		if (ent.classname.Equals("item_breather"))
			taken = false;

		if (other.client == null)
			return;

		if (other.health < 1)
			return; // dead people can't pickup

		if (ent.item.pickup == null)
			return; // not a grabbable item?

		taken = ent.item.pickup.interact(ent, other);

		if (taken)
		{
			// flash the screen
			other.client.bonus_alpha = 0.25f;

			// show icon and name on status bar
			other.client.ps.stats[Defines.STAT_PICKUP_ICON] = (short)GameBase.gi.imageindex(ent.item.icon);
			other.client.ps.stats[Defines.STAT_PICKUP_STRING] = (short)(Defines.CS_ITEMS + GameItems.ITEM_INDEX(ent.item));
			other.client.pickup_msg_time = GameBase.level.time + 3.0f;

			// change selected item
			if (ent.item.use != null)
				other.client.pers.selected_item = other.client.ps.stats[Defines.STAT_SELECTED_ITEM] = (short)GameItems.ITEM_INDEX(ent.item);

			if (ent.item.pickup == GameItems.Pickup_Health)
			{
				if (ent.count == 2)
					GameBase.gi.sound(other, Defines.CHAN_ITEM, GameBase.gi.soundindex("items/s_health.wav"), 1, Defines.ATTN_NORM, 0);
				else if (ent.count == 10)
					GameBase.gi.sound(other, Defines.CHAN_ITEM, GameBase.gi.soundindex("items/n_health.wav"), 1, Defines.ATTN_NORM, 0);
				else if (ent.count == 25)
					GameBase.gi.sound(other, Defines.CHAN_ITEM, GameBase.gi.soundindex("items/l_health.wav"), 1, Defines.ATTN_NORM, 0);
				else

					// (ent.count == 100)
					GameBase.gi.sound(other, Defines.CHAN_ITEM, GameBase.gi.soundindex("items/m_health.wav"), 1, Defines.ATTN_NORM, 0);
			}
			else if (ent.item.pickup_sound != null)
				GameBase.gi.sound(other, Defines.CHAN_ITEM, GameBase.gi.soundindex(ent.item.pickup_sound), 1, Defines.ATTN_NORM, 0);
		}

		if (0 == (ent.spawnflags & Defines.ITEM_TARGETS_USED))
		{
			GameUtil.G_UseTargets(ent, other);
			ent.spawnflags |= Defines.ITEM_TARGETS_USED;
		}

		if (!taken)
			return;

		Com.dprintln("Picked up:" + ent.classname);

		if (!(GameBase.coop.value != 0 && (ent.item.flags & Defines.IT_STAY_COOP) != 0)
		    || 0 != (ent.spawnflags & (Defines.DROPPED_ITEM | Defines.DROPPED_PLAYER_ITEM)))
		{
			if ((ent.flags & Defines.FL_RESPAWN) != 0)
				ent.flags &= ~Defines.FL_RESPAWN;
			else
				GameUtil.G_FreeEdict(ent);
		}
	});

	private static readonly EntTouchAdapter drop_temp_touch = new("drop_temp_touch", (ent, other, plane, surf) =>
	{
		if (other == ent.owner)
			return;

		GameItems.Touch_Item.touch(ent, other, plane, surf);
	});

	private static readonly EntThinkAdapter drop_make_touchable = new("drop_make_touchable", ent =>
	{
		ent.touch = GameItems.Touch_Item;

		if (GameBase.deathmatch.value != 0)
		{
			ent.nextthink = GameBase.level.time + 29;
			ent.think = GameUtil.G_FreeEdictA;
		}

		return false;
	});

	public static ItemUseAdapter Use_Quad = new("use_quad", (ent, item) =>
	{
		int timeout;

		ent.client.pers.inventory[GameItems.ITEM_INDEX(item)]--;
		GameUtil.ValidateSelectedItem(ent);

		if (GameItems.quad_drop_timeout_hack != 0)
		{
			timeout = GameItems.quad_drop_timeout_hack;
			GameItems.quad_drop_timeout_hack = 0;
		}
		else
			timeout = 300;

		if (ent.client.quad_framenum > GameBase.level.framenum)
			ent.client.quad_framenum += timeout;
		else
			ent.client.quad_framenum = GameBase.level.framenum + timeout;

		GameBase.gi.sound(ent, Defines.CHAN_ITEM, GameBase.gi.soundindex("items/damage.wav"), 1, Defines.ATTN_NORM, 0);
	});

	public static ItemUseAdapter Use_Invulnerability = new("use_invulnerability", (ent, item) =>
	{
		ent.client.pers.inventory[GameItems.ITEM_INDEX(item)]--;
		GameUtil.ValidateSelectedItem(ent);

		if (ent.client.invincible_framenum > GameBase.level.framenum)
			ent.client.invincible_framenum += 300;
		else
			ent.client.invincible_framenum = GameBase.level.framenum + 300;

		GameBase.gi.sound(ent, Defines.CHAN_ITEM, GameBase.gi.soundindex("items/protect.wav"), 1, Defines.ATTN_NORM, 0);
	});

	public static ItemUseAdapter Use_Breather = new("use_breather", (ent, item) =>
	{
		ent.client.pers.inventory[GameItems.ITEM_INDEX(item)]--;

		GameUtil.ValidateSelectedItem(ent);

		if (ent.client.breather_framenum > GameBase.level.framenum)
			ent.client.breather_framenum += 300;
		else
			ent.client.breather_framenum = GameBase.level.framenum + 300;

		GameBase.gi.sound(ent, Defines.CHAN_ITEM, GameBase.gi.soundindex("items/damage.wav"), 1, Defines.ATTN_NORM, 0);
	});

	public static ItemUseAdapter Use_Envirosuit = new("use_envirosuit", (ent, item) =>
	{
		ent.client.pers.inventory[GameItems.ITEM_INDEX(item)]--;
		GameUtil.ValidateSelectedItem(ent);

		if (ent.client.enviro_framenum > GameBase.level.framenum)
			ent.client.enviro_framenum += 300;
		else
			ent.client.enviro_framenum = GameBase.level.framenum + 300;

		GameBase.gi.sound(ent, Defines.CHAN_ITEM, GameBase.gi.soundindex("items/damage.wav"), 1, Defines.ATTN_NORM, 0);
	});

	public static ItemUseAdapter Use_Silencer = new("use_silencer", (ent, item) =>
	{
		ent.client.pers.inventory[GameItems.ITEM_INDEX(item)]--;
		GameUtil.ValidateSelectedItem(ent);
		ent.client.silencer_shots += 30;

		GameBase.gi.sound(ent, Defines.CHAN_ITEM, GameBase.gi.soundindex("items/damage.wav"), 1, Defines.ATTN_NORM, 0);
	});

	public static EntInteractAdapter Pickup_Key = new("pickup_key", (ent, other) =>
	{
		if (GameBase.coop.value != 0)
		{
			if (Lib.strcmp(ent.classname, "key_power_cube") == 0)
			{
				if ((other.client.pers.power_cubes & ((ent.spawnflags & 0x0000ff00) >> 8)) != 0)
					return false;

				other.client.pers.inventory[GameItems.ITEM_INDEX(ent.item)]++;
				other.client.pers.power_cubes |= (ent.spawnflags & 0x0000ff00) >> 8;
			}
			else
			{
				if (other.client.pers.inventory[GameItems.ITEM_INDEX(ent.item)] != 0)
					return false;

				other.client.pers.inventory[GameItems.ITEM_INDEX(ent.item)] = 1;
			}

			return true;
		}

		other.client.pers.inventory[GameItems.ITEM_INDEX(ent.item)]++;

		return true;
	});

	public static EntInteractAdapter Pickup_Ammo = new("pickup_ammo", (ent, other) =>
	{
		int oldcount;
		int count;
		bool weapon;

		weapon = (ent.item.flags & Defines.IT_WEAPON) != 0;

		if (weapon && ((int)GameBase.dmflags.value & Defines.DF_INFINITE_AMMO) != 0)
			count = 1000;
		else if (ent.count != 0)
			count = ent.count;
		else
			count = ent.item.quantity;

		oldcount = other.client.pers.inventory[GameItems.ITEM_INDEX(ent.item)];

		if (!GameItems.Add_Ammo(other, ent.item, count))
			return false;

		if (weapon && 0 == oldcount)
		{
			if (other.client.pers.weapon != ent.item && (0 == GameBase.deathmatch.value || other.client.pers.weapon == GameItems.FindItem("blaster")))
				other.client.newweapon = ent.item;
		}

		if (0 == (ent.spawnflags & (Defines.DROPPED_ITEM | Defines.DROPPED_PLAYER_ITEM)) && GameBase.deathmatch.value != 0)
			GameItems.SetRespawn(ent, 30);

		return true;
	});

	public static EntInteractAdapter Pickup_Armor = new("pickup_armor", (ent, other) =>
	{
		int old_armor_index;
		gitem_armor_t oldinfo;
		gitem_armor_t newinfo;
		int newcount;
		float salvage;
		int salvagecount;

		// get info on new armor
		newinfo = (gitem_armor_t)ent.item.info;

		old_armor_index = GameItems.ArmorIndex(other);

		// handle armor shards specially
		if (ent.item.tag == Defines.ARMOR_SHARD)
		{
			if (0 == old_armor_index)
				other.client.pers.inventory[GameItems.jacket_armor_index] = 2;
			else
				other.client.pers.inventory[old_armor_index] += 2;
		}

		// if player has no armor, just use it
		else if (0 == old_armor_index)
			other.client.pers.inventory[GameItems.ITEM_INDEX(ent.item)] = newinfo.base_count;

		// use the better armor
		else
		{
			// get info on old armor
			if (old_armor_index == GameItems.jacket_armor_index)
				oldinfo = GameItems.jacketarmor_info;

			else if (old_armor_index == GameItems.combat_armor_index)
				oldinfo = GameItems.combatarmor_info;

			else

				// (old_armor_index == body_armor_index)
				oldinfo = GameItems.bodyarmor_info;

			if (newinfo.normal_protection > oldinfo.normal_protection)
			{
				// calc new armor values
				salvage = oldinfo.normal_protection / newinfo.normal_protection;
				salvagecount = (int)salvage * other.client.pers.inventory[old_armor_index];
				newcount = newinfo.base_count + salvagecount;

				if (newcount > newinfo.max_count)
					newcount = newinfo.max_count;

				// zero count of old armor so it goes away
				other.client.pers.inventory[old_armor_index] = 0;

				// change armor to new item with computed value
				other.client.pers.inventory[GameItems.ITEM_INDEX(ent.item)] = newcount;
			}
			else
			{
				// calc new armor values
				salvage = newinfo.normal_protection / oldinfo.normal_protection;
				salvagecount = (int)salvage * newinfo.base_count;
				newcount = other.client.pers.inventory[old_armor_index] + salvagecount;

				if (newcount > oldinfo.max_count)
					newcount = oldinfo.max_count;

				// if we're already maxed out then we don't need the new
				// armor
				if (other.client.pers.inventory[old_armor_index] >= newcount)
					return false;

				// update current armor value
				other.client.pers.inventory[old_armor_index] = newcount;
			}
		}

		if (0 == (ent.spawnflags & Defines.DROPPED_ITEM) && GameBase.deathmatch.value != 0)
			GameItems.SetRespawn(ent, 20);

		return true;
	});

	public static EntInteractAdapter Pickup_PowerArmor = new("pickup_powerarmor", (ent, other) =>
	{
		int quantity;

		quantity = other.client.pers.inventory[GameItems.ITEM_INDEX(ent.item)];

		other.client.pers.inventory[GameItems.ITEM_INDEX(ent.item)]++;

		if (GameBase.deathmatch.value != 0)
		{
			if (0 == (ent.spawnflags & Defines.DROPPED_ITEM))
				GameItems.SetRespawn(ent, ent.item.quantity);

			// auto-use for DM only if we didn't already have one
			if (0 == quantity)
				ent.item.use.use(other, ent.item);
		}

		return true;
	});

	public static EntInteractAdapter Pickup_Powerup = new("pickup_powerup", (ent, other) =>
	{
		int quantity;

		quantity = other.client.pers.inventory[GameItems.ITEM_INDEX(ent.item)];

		if ((GameBase.skill.value == 1 && quantity >= 2) || (GameBase.skill.value >= 2 && quantity >= 1))
			return false;

		if (GameBase.coop.value != 0 && (ent.item.flags & Defines.IT_STAY_COOP) != 0 && quantity > 0)
			return false;

		other.client.pers.inventory[GameItems.ITEM_INDEX(ent.item)]++;

		if (GameBase.deathmatch.value != 0)
		{
			if (0 == (ent.spawnflags & Defines.DROPPED_ITEM))
				GameItems.SetRespawn(ent, ent.item.quantity);

			if (((int)GameBase.dmflags.value & Defines.DF_INSTANT_ITEMS) != 0
			    || (ent.item.use == GameItems.Use_Quad && 0 != (ent.spawnflags & Defines.DROPPED_PLAYER_ITEM)))
			{
				if (ent.item.use == GameItems.Use_Quad && 0 != (ent.spawnflags & Defines.DROPPED_PLAYER_ITEM))
					GameItems.quad_drop_timeout_hack = (int)((ent.nextthink - GameBase.level.time) / Defines.FRAMETIME);

				ent.item.use.use(other, ent.item);
			}
		}

		return true;
	});

	public static EntInteractAdapter Pickup_Adrenaline = new("pickup_adrenaline", (ent, other) =>
	{
		if (GameBase.deathmatch.value == 0)
			other.max_health += 1;

		if (other.health < other.max_health)
			other.health = other.max_health;

		if (0 == (ent.spawnflags & Defines.DROPPED_ITEM) && GameBase.deathmatch.value != 0)
			GameItems.SetRespawn(ent, ent.item.quantity);

		return true;
	});

	public static EntInteractAdapter Pickup_AncientHead = new("pickup_ancienthead", (ent, other) =>
	{
		other.max_health += 2;

		if (0 == (ent.spawnflags & Defines.DROPPED_ITEM) && GameBase.deathmatch.value != 0)
			GameItems.SetRespawn(ent, ent.item.quantity);

		return true;
	});

	public static EntInteractAdapter Pickup_Bandolier = new("pickup_bandolier", (ent, other) =>
	{
		gitem_t item;
		int index;

		if (other.client.pers.max_bullets < 250)
			other.client.pers.max_bullets = 250;

		if (other.client.pers.max_shells < 150)
			other.client.pers.max_shells = 150;

		if (other.client.pers.max_cells < 250)
			other.client.pers.max_cells = 250;

		if (other.client.pers.max_slugs < 75)
			other.client.pers.max_slugs = 75;

		item = GameItems.FindItem("Bullets");

		if (item != null)
		{
			index = GameItems.ITEM_INDEX(item);
			other.client.pers.inventory[index] += item.quantity;

			if (other.client.pers.inventory[index] > other.client.pers.max_bullets)
				other.client.pers.inventory[index] = other.client.pers.max_bullets;
		}

		item = GameItems.FindItem("Shells");

		if (item != null)
		{
			index = GameItems.ITEM_INDEX(item);
			other.client.pers.inventory[index] += item.quantity;

			if (other.client.pers.inventory[index] > other.client.pers.max_shells)
				other.client.pers.inventory[index] = other.client.pers.max_shells;
		}

		if (0 == (ent.spawnflags & Defines.DROPPED_ITEM) && GameBase.deathmatch.value != 0)
			GameItems.SetRespawn(ent, ent.item.quantity);

		return true;
	});

	public static ItemDropAdapter Drop_Ammo = new("drop_ammo", (ent, item) =>
	{
		edict_t dropped;
		int index;

		index = GameItems.ITEM_INDEX(item);
		dropped = GameItems.Drop_Item(ent, item);

		if (ent.client.pers.inventory[index] >= item.quantity)
			dropped.count = item.quantity;
		else
			dropped.count = ent.client.pers.inventory[index];

		if (ent.client.pers.weapon != null
		    && ent.client.pers.weapon.tag == Defines.AMMO_GRENADES
		    && item.tag == Defines.AMMO_GRENADES
		    && ent.client.pers.inventory[index] - dropped.count <= 0)
		{
			GameBase.gi.cprintf(ent, Defines.PRINT_HIGH, "Can't drop current weapon\n");
			GameUtil.G_FreeEdict(dropped);

			return;
		}

		ent.client.pers.inventory[index] -= dropped.count;
		Cmd.ValidateSelectedItem(ent);
	});

	public static ItemDropAdapter Drop_General = new("drop_general", (ent, item) =>
	{
		GameItems.Drop_Item(ent, item);
		ent.client.pers.inventory[GameItems.ITEM_INDEX(item)]--;
		Cmd.ValidateSelectedItem(ent);
	});

	public static ItemDropAdapter Drop_PowerArmor = new("drop_powerarmor", (ent, item) =>
	{
		if (0 != (ent.flags & Defines.FL_POWER_ARMOR) && ent.client.pers.inventory[GameItems.ITEM_INDEX(item)] == 1)
			GameItems.Use_PowerArmor.use(ent, item);

		GameItems.Drop_General.drop(ent, item);
	});

	public static EntThinkAdapter droptofloor = new("drop_to_floor", ent =>
	{
		trace_t tr;
		float[] dest = { 0, 0, 0 };

		//float v[];

		//v = Lib.tv(-15, -15, -15);
		//Math3D.VectorCopy(v, ent.mins);
		ent.mins[0] = ent.mins[1] = ent.mins[2] = -15;

		//v = Lib.tv(15, 15, 15);
		//Math3D.VectorCopy(v, ent.maxs);
		ent.maxs[0] = ent.maxs[1] = ent.maxs[2] = 15;

		if (ent.model != null)
			GameBase.gi.setmodel(ent, ent.model);
		else
			GameBase.gi.setmodel(ent, ent.item.world_model);

		ent.solid = Defines.SOLID_TRIGGER;
		ent.movetype = Defines.MOVETYPE_TOSS;
		ent.touch = GameItems.Touch_Item;

		float[] v = { 0, 0, -128 };
		Math3D.VectorAdd(ent.s.origin, v, dest);

		tr = GameBase.gi.trace(ent.s.origin, ent.mins, ent.maxs, dest, ent, Defines.MASK_SOLID);

		if (tr.startsolid)
		{
			GameBase.gi.dprintf("droptofloor: " + ent.classname + " startsolid at " + Lib.vtos(ent.s.origin) + "\n");
			GameUtil.G_FreeEdict(ent);

			return true;
		}

		Math3D.VectorCopy(tr.endpos, ent.s.origin);

		if (ent.team != null)
		{
			ent.flags &= ~Defines.FL_TEAMSLAVE;
			ent.chain = ent.teamchain;
			ent.teamchain = null;

			ent.svflags |= Defines.SVF_NOCLIENT;
			ent.solid = Defines.SOLID_NOT;

			if (ent == ent.teammaster)
			{
				ent.nextthink = GameBase.level.time + Defines.FRAMETIME;
				ent.think = GameItems.DoRespawn;
			}
		}

		if ((ent.spawnflags & Defines.ITEM_NO_TOUCH) != 0)
		{
			ent.solid = Defines.SOLID_BBOX;
			ent.touch = null;
			ent.s.effects &= ~Defines.EF_ROTATE;
			ent.s.renderfx &= ~Defines.RF_GLOW;
		}

		if ((ent.spawnflags & Defines.ITEM_TRIGGER_SPAWN) != 0)
		{
			ent.svflags |= Defines.SVF_NOCLIENT;
			ent.solid = Defines.SOLID_NOT;
			ent.use = GameItems.Use_Item;
		}

		GameBase.gi.linkentity(ent);

		return true;
	});

	public static ItemUseAdapter Use_PowerArmor = new("use_powerarmor", (ent, item) =>
	{
		int index;

		if ((ent.flags & Defines.FL_POWER_ARMOR) != 0)
		{
			ent.flags &= ~Defines.FL_POWER_ARMOR;
			GameBase.gi.sound(ent, Defines.CHAN_AUTO, GameBase.gi.soundindex("misc/power2.wav"), 1, Defines.ATTN_NORM, 0);
		}
		else
		{
			index = GameItems.ITEM_INDEX(GameItems.FindItem("cells"));

			if (0 == ent.client.pers.inventory[index])
			{
				GameBase.gi.cprintf(ent, Defines.PRINT_HIGH, "No cells for power armor.\n");

				return;
			}

			ent.flags |= Defines.FL_POWER_ARMOR;
			GameBase.gi.sound(ent, Defines.CHAN_AUTO, GameBase.gi.soundindex("misc/power1.wav"), 1, Defines.ATTN_NORM, 0);
		}
	});

	public static EntUseAdapter Use_Item = new("use_item", (ent, other, activator) =>
	{
		ent.svflags &= ~Defines.SVF_NOCLIENT;
		ent.use = null;

		if ((ent.spawnflags & Defines.ITEM_NO_TOUCH) != 0)
		{
			ent.solid = Defines.SOLID_BBOX;
			ent.touch = null;
		}
		else
		{
			ent.solid = Defines.SOLID_TRIGGER;
			ent.touch = GameItems.Touch_Item;
		}

		GameBase.gi.linkentity(ent);
	});

	/*
	 * =============== GetItemByIndex ===============
	 */
	public static gitem_t GetItemByIndex(int index)
	{
		if (index == 0 || index >= GameBase.game.num_items)
			return null;

		return GameItemList.itemlist[index];
	}

	/*
	 * =============== FindItemByClassname
	 * 
	 * ===============
	 */
	public static gitem_t FindItemByClassname(string classname)
	{
		for (var i = 1; i < GameBase.game.num_items; i++)
		{
			var it = GameItemList.itemlist[i];

			if (it.classname == null)
				continue;

			if (it.classname.Equals(classname, StringComparison.OrdinalIgnoreCase))
				return it;
		}

		return null;
	}

	/*
	 * =============== FindItem ===============
	 */
	//geht.
	public static gitem_t FindItem(string pickup_name)
	{
		for (var i = 1; i < GameBase.game.num_items; i++)
		{
			var it = GameItemList.itemlist[i];

			if (it.pickup_name == null)
				continue;

			if (it.pickup_name.Equals(pickup_name, StringComparison.OrdinalIgnoreCase))
				return it;
		}

		Com.Println("Item not found:" + pickup_name);

		return null;
	}

	public static void SetRespawn(edict_t ent, float delay)
	{
		ent.flags |= Defines.FL_RESPAWN;
		ent.svflags |= Defines.SVF_NOCLIENT;
		ent.solid = Defines.SOLID_NOT;
		ent.nextthink = GameBase.level.time + delay;
		ent.think = GameItems.DoRespawn;
		GameBase.gi.linkentity(ent);
	}

	public static int ITEM_INDEX(gitem_t item)
	{
		return item.index;
	}

	public static edict_t Drop_Item(edict_t ent, gitem_t item)
	{
		edict_t dropped;
		float[] forward = { 0, 0, 0 };
		float[] right = { 0, 0, 0 };
		float[] offset = { 0, 0, 0 };

		dropped = GameUtil.G_Spawn();

		dropped.classname = item.classname;
		dropped.item = item;
		dropped.spawnflags = Defines.DROPPED_ITEM;
		dropped.s.effects = item.world_model_flags;
		dropped.s.renderfx = Defines.RF_GLOW;
		Math3D.VectorSet(dropped.mins, -15, -15, -15);
		Math3D.VectorSet(dropped.maxs, 15, 15, 15);
		GameBase.gi.setmodel(dropped, dropped.item.world_model);
		dropped.solid = Defines.SOLID_TRIGGER;
		dropped.movetype = Defines.MOVETYPE_TOSS;

		dropped.touch = GameItems.drop_temp_touch;

		dropped.owner = ent;

		if (ent.client != null)
		{
			trace_t trace;

			Math3D.AngleVectors(ent.client.v_angle, forward, right, null);
			Math3D.VectorSet(offset, 24, 0, -16);
			Math3D.G_ProjectSource(ent.s.origin, offset, forward, right, dropped.s.origin);
			trace = GameBase.gi.trace(ent.s.origin, dropped.mins, dropped.maxs, dropped.s.origin, ent, Defines.CONTENTS_SOLID);
			Math3D.VectorCopy(trace.endpos, dropped.s.origin);
		}
		else
		{
			Math3D.AngleVectors(ent.s.angles, forward, right, null);
			Math3D.VectorCopy(ent.s.origin, dropped.s.origin);
		}

		Math3D.VectorScale(forward, 100, dropped.velocity);
		dropped.velocity[2] = 300;

		dropped.think = GameItems.drop_make_touchable;
		dropped.nextthink = GameBase.level.time + 1;

		GameBase.gi.linkentity(dropped);

		return dropped;
	}

	public static int PowerArmorType(edict_t ent)
	{
		if (ent.client == null)
			return Defines.POWER_ARMOR_NONE;

		if (0 == (ent.flags & Defines.FL_POWER_ARMOR))
			return Defines.POWER_ARMOR_NONE;

		if (ent.client.pers.inventory[GameItems.power_shield_index] > 0)
			return Defines.POWER_ARMOR_SHIELD;

		if (ent.client.pers.inventory[GameItems.power_screen_index] > 0)
			return Defines.POWER_ARMOR_SCREEN;

		return Defines.POWER_ARMOR_NONE;
	}

	public static int ArmorIndex(edict_t ent)
	{
		if (ent.client == null)
			return 0;

		if (ent.client.pers.inventory[GameItems.jacket_armor_index] > 0)
			return GameItems.jacket_armor_index;

		if (ent.client.pers.inventory[GameItems.combat_armor_index] > 0)
			return GameItems.combat_armor_index;

		if (ent.client.pers.inventory[GameItems.body_armor_index] > 0)
			return GameItems.body_armor_index;

		return 0;
	}

	public static bool Add_Ammo(edict_t ent, gitem_t item, int count)
	{
		int index;
		int max;

		if (null == ent.client)
			return false;

		if (item.tag == Defines.AMMO_BULLETS)
			max = ent.client.pers.max_bullets;
		else if (item.tag == Defines.AMMO_SHELLS)
			max = ent.client.pers.max_shells;
		else if (item.tag == Defines.AMMO_ROCKETS)
			max = ent.client.pers.max_rockets;
		else if (item.tag == Defines.AMMO_GRENADES)
			max = ent.client.pers.max_grenades;
		else if (item.tag == Defines.AMMO_CELLS)
			max = ent.client.pers.max_cells;
		else if (item.tag == Defines.AMMO_SLUGS)
			max = ent.client.pers.max_slugs;
		else
			return false;

		index = GameItems.ITEM_INDEX(item);

		if (ent.client.pers.inventory[index] == max)
			return false;

		ent.client.pers.inventory[index] += count;

		if (ent.client.pers.inventory[index] > max)
			ent.client.pers.inventory[index] = max;

		return true;
	}

	public static void InitItems()
	{
		GameBase.game.num_items = GameItemList.itemlist.Length - 1;
	}

	/*
	 * =============== SetItemNames
	 * 
	 * Called by worldspawn ===============
	 */
	public static void SetItemNames()
	{
		int i;
		gitem_t it;

		for (i = 1; i < GameBase.game.num_items; i++)
		{
			it = GameItemList.itemlist[i];
			GameBase.gi.configstring(Defines.CS_ITEMS + i, it.pickup_name);
		}

		GameItems.jacket_armor_index = GameItems.ITEM_INDEX(GameItems.FindItem("Jacket Armor"));
		GameItems.combat_armor_index = GameItems.ITEM_INDEX(GameItems.FindItem("Combat Armor"));
		GameItems.body_armor_index = GameItems.ITEM_INDEX(GameItems.FindItem("Body Armor"));
		GameItems.power_screen_index = GameItems.ITEM_INDEX(GameItems.FindItem("Power Screen"));
		GameItems.power_shield_index = GameItems.ITEM_INDEX(GameItems.FindItem("Power Shield"));
	}

	public static void SelectNextItem(edict_t ent, int itflags)
	{
		gclient_t cl;
		int i, index;
		gitem_t it;

		cl = ent.client;

		if (cl.chase_target != null)
		{
			GameChase.ChaseNext(ent);

			return;
		}

		// scan for the next valid one
		for (i = 1; i <= Defines.MAX_ITEMS; i++)
		{
			index = (cl.pers.selected_item + i) % Defines.MAX_ITEMS;

			if (0 == cl.pers.inventory[index])
				continue;

			it = GameItemList.itemlist[index];

			if (it.use == null)
				continue;

			if (0 == (it.flags & itflags))
				continue;

			cl.pers.selected_item = index;

			return;
		}

		cl.pers.selected_item = -1;
	}

	public static void SelectPrevItem(edict_t ent, int itflags)
	{
		gclient_t cl;
		int i, index;
		gitem_t it;

		cl = ent.client;

		if (cl.chase_target != null)
		{
			GameChase.ChasePrev(ent);

			return;
		}

		// scan for the next valid one
		for (i = 1; i <= Defines.MAX_ITEMS; i++)
		{
			index = (cl.pers.selected_item + Defines.MAX_ITEMS - i) % Defines.MAX_ITEMS;

			if (0 == cl.pers.inventory[index])
				continue;

			it = GameItemList.itemlist[index];

			if (null == it.use)
				continue;

			if (0 == (it.flags & itflags))
				continue;

			cl.pers.selected_item = index;

			return;
		}

		cl.pers.selected_item = -1;
	}

	/*
	 * =============== PrecacheItem
	 * 
	 * Precaches all data needed for a given item. This will be called for each
	 * item spawned in a level, and for each item in each client's inventory.
	 * ===============
	 */
	public static void PrecacheItem(gitem_t it)
	{
		string s;
		int len;
		gitem_t ammo;

		if (it == null)
			return;

		if (it.pickup_sound != null)
			GameBase.gi.soundindex(it.pickup_sound);

		if (it.world_model != null)
			GameBase.gi.modelindex(it.world_model);

		if (it.view_model != null)
			GameBase.gi.modelindex(it.view_model);

		if (it.icon != null)
			GameBase.gi.imageindex(it.icon);

		// parse everything for its ammo
		if (it.ammo != null && it.ammo.Length != 0)
		{
			ammo = GameItems.FindItem(it.ammo);

			if (ammo != it)
				GameItems.PrecacheItem(ammo);
		}

		// parse the space seperated precache string for other items
		s = it.precaches;

		if (s == null || s.Length == 0)
			return;

		var tk = Regex.Split(s, "\\s");

		foreach (var data in tk)
		{
			len = data.Length;

			if (len >= Defines.MAX_QPATH || len < 5)
				GameBase.gi.error("PrecacheItem: it.classname has bad precache string: " + s);

			// determine type based on extension
			if (data.EndsWith("md2"))
				GameBase.gi.modelindex(data);
			else if (data.EndsWith("sp2"))
				GameBase.gi.modelindex(data);
			else if (data.EndsWith("wav"))
				GameBase.gi.soundindex(data);
			else if (data.EndsWith("pcx"))
				GameBase.gi.imageindex(data);
			else
				GameBase.gi.error("PrecacheItem: bad precache string: " + data);
		}
	}

	/*
	 * ============ SpawnItem
	 * 
	 * Sets the clipping size and plants the object on the floor.
	 * 
	 * Items can't be immediately dropped to floor, because they might be on an
	 * entity that hasn't spawned yet. ============
	 */
	public static void SpawnItem(edict_t ent, gitem_t item)
	{
		GameItems.PrecacheItem(item);

		if (ent.spawnflags != 0)
		{
			if (Lib.strcmp(ent.classname, "key_power_cube") != 0)
			{
				ent.spawnflags = 0;
				GameBase.gi.dprintf("" + ent.classname + " at " + Lib.vtos(ent.s.origin) + " has invalid spawnflags set\n");
			}
		}

		// some items will be prevented in deathmatch
		if (GameBase.deathmatch.value != 0)
		{
			if (((int)GameBase.dmflags.value & Defines.DF_NO_ARMOR) != 0)
			{
				if (item.pickup == GameItems.Pickup_Armor || item.pickup == GameItems.Pickup_PowerArmor)
				{
					GameUtil.G_FreeEdict(ent);

					return;
				}
			}

			if (((int)GameBase.dmflags.value & Defines.DF_NO_ITEMS) != 0)
			{
				if (item.pickup == GameItems.Pickup_Powerup)
				{
					GameUtil.G_FreeEdict(ent);

					return;
				}
			}

			if (((int)GameBase.dmflags.value & Defines.DF_NO_HEALTH) != 0)
			{
				if (item.pickup == GameItems.Pickup_Health || item.pickup == GameItems.Pickup_Adrenaline || item.pickup == GameItems.Pickup_AncientHead)
				{
					GameUtil.G_FreeEdict(ent);

					return;
				}
			}

			if (((int)GameBase.dmflags.value & Defines.DF_INFINITE_AMMO) != 0)
			{
				if (item.flags == Defines.IT_AMMO || Lib.strcmp(ent.classname, "weapon_bfg") == 0)
				{
					GameUtil.G_FreeEdict(ent);

					return;
				}
			}
		}

		if (GameBase.coop.value != 0 && Lib.strcmp(ent.classname, "key_power_cube") == 0)
		{
			ent.spawnflags |= 1 << (8 + GameBase.level.power_cubes);
			GameBase.level.power_cubes++;
		}

		// don't let them drop items that stay in a coop game
		if (GameBase.coop.value != 0 && (item.flags & Defines.IT_STAY_COOP) != 0)
			item.drop = null;

		ent.item = item;
		ent.nextthink = GameBase.level.time + 2 * Defines.FRAMETIME;

		// items start after other solids
		ent.think = GameItems.droptofloor;
		ent.s.effects = item.world_model_flags;
		ent.s.renderfx = Defines.RF_GLOW;

		if (ent.model != null)
			GameBase.gi.modelindex(ent.model);
	}

	/*
	 * QUAKED item_health (.3 .3 1) (-16 -16 -16) (16 16 16)
	 */
	public static void SP_item_health(edict_t self)
	{
		if (GameBase.deathmatch.value != 0 && ((int)GameBase.dmflags.value & Defines.DF_NO_HEALTH) != 0)
			GameUtil.G_FreeEdict(self);

		self.model = "models/items/healing/medium/tris.md2";
		self.count = 10;
		GameItems.SpawnItem(self, GameItems.FindItem("Health"));
		GameBase.gi.soundindex("items/n_health.wav");
	}

	/*
	 * QUAKED item_health_small (.3 .3 1) (-16 -16 -16) (16 16 16)
	 */
	public static void SP_item_health_small(edict_t self)
	{
		if (GameBase.deathmatch.value != 0 && ((int)GameBase.dmflags.value & Defines.DF_NO_HEALTH) != 0)
		{
			GameUtil.G_FreeEdict(self);

			return;
		}

		self.model = "models/items/healing/stimpack/tris.md2";
		self.count = 2;
		GameItems.SpawnItem(self, GameItems.FindItem("Health"));
		self.style = Defines.HEALTH_IGNORE_MAX;
		GameBase.gi.soundindex("items/s_health.wav");
	}

	/*
	 * QUAKED item_health_large (.3 .3 1) (-16 -16 -16) (16 16 16)
	 */
	public static void SP_item_health_large(edict_t self)
	{
		if (GameBase.deathmatch.value != 0 && ((int)GameBase.dmflags.value & Defines.DF_NO_HEALTH) != 0)
		{
			GameUtil.G_FreeEdict(self);

			return;
		}

		self.model = "models/items/healing/large/tris.md2";
		self.count = 25;
		GameItems.SpawnItem(self, GameItems.FindItem("Health"));
		GameBase.gi.soundindex("items/l_health.wav");
	}

	/*
	 * QUAKED item_health_mega (.3 .3 1) (-16 -16 -16) (16 16 16)
	 */
	public static void SP_item_health_mega(edict_t self)
	{
		if (GameBase.deathmatch.value != 0 && ((int)GameBase.dmflags.value & Defines.DF_NO_HEALTH) != 0)
		{
			GameUtil.G_FreeEdict(self);

			return;
		}

		self.model = "models/items/mega_h/tris.md2";
		self.count = 100;
		GameItems.SpawnItem(self, GameItems.FindItem("Health"));
		GameBase.gi.soundindex("items/m_health.wav");
		self.style = Defines.HEALTH_IGNORE_MAX | Defines.HEALTH_TIMED;
	}
}