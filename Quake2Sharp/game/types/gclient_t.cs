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
namespace Quake2Sharp.game.types
{
	using System;
	using System.IO;
	using util;

	public class gclient_t
	{
		public gclient_t(int index)
		{
			this.index = index;
		}

		//	this structure is cleared on each PutClientInServer(),
		//	except for 'client->pers'

		// known to server
		public player_state_t ps = new(); // communicated by server to clients
		public int ping;

		// private to game
		public client_persistant_t pers = new();
		public client_respawn_t resp = new();
		public pmove_state_t old_pmove = new(); // for detecting out-of-pmove changes
		public bool showscores; // set layout stat
		public bool showinventory; // set layout stat
		public bool showhelp;
		public bool showhelpicon;
		public int ammo_index;
		public int buttons;
		public int oldbuttons;
		public int latched_buttons;
		public bool weapon_thunk;
		public gitem_t newweapon;

		// sum up damage over an entire frame, so
		// shotgun blasts give a single big kick
		public int damage_armor; // damage absorbed by armor
		public int damage_parmor; // damage absorbed by power armor
		public int damage_blood; // damage taken out of health
		public int damage_knockback; // impact damage
		public float[] damage_from = { 0, 0, 0 }; // origin for vector calculation
		public float killer_yaw; // when dead, look at killer
		public int weaponstate;
		public float[] kick_angles = { 0, 0, 0 }; // weapon kicks
		public float[] kick_origin = { 0, 0, 0 };
		public float v_dmg_roll, v_dmg_pitch, v_dmg_time; // damage kicks
		public float fall_time, fall_value; // for view drop on fall
		public float damage_alpha;
		public float bonus_alpha;
		public float[] damage_blend = { 0, 0, 0 };
		public float[] v_angle = { 0, 0, 0 }; // aiming direction
		public float bobtime; // so off-ground doesn't change it
		public float[] oldviewangles = { 0, 0, 0 };
		public float[] oldvelocity = { 0, 0, 0 };
		public float next_drown_time;
		public int old_waterlevel;
		public int breather_sound;
		public int machinegun_shots; // for weapon raising

		// animation vars
		public int anim_end;
		public int anim_priority;
		public bool anim_duck;
		public bool anim_run;

		// powerup timers
		public float quad_framenum;
		public float invincible_framenum;
		public float breather_framenum;
		public float enviro_framenum;
		public bool grenade_blew_up;
		public float grenade_time;
		public int silencer_shots;
		public int weapon_sound;
		public float pickup_msg_time;
		public float flood_locktill; // locked from talking
		public float[] flood_when = new float[10]; // when messages were said
		public int flood_whenhead; // head pointer for when said
		public float respawn_time; // can respawn when time > this
		public edict_t chase_target; // player we are chasing
		public bool update_chase; // need to update chase info?
		public int index;

		/** Clears the game client structure. */
		public void clear()
		{
			this.ping = 0;

			this.pers = new();
			this.resp = new();
			this.old_pmove = new();

			this.showscores = false; // set layout stat
			this.showinventory = false; // set layout stat
			this.showhelp = false;
			this.showhelpicon = false;

			this.ammo_index = 0;

			this.buttons = this.oldbuttons = this.latched_buttons = 0;
			this.weapon_thunk = false;
			this.newweapon = null;
			this.damage_armor = 0;
			this.damage_parmor = 0;
			this.damage_blood = 0;
			this.damage_knockback = 0;

			this.killer_yaw = 0;
			this.damage_from = new float[3];
			this.weaponstate = 0;
			this.kick_angles = new float[3];
			this.kick_origin = new float[3];
			this.v_dmg_roll = this.v_dmg_pitch = this.v_dmg_time = 0;
			this.fall_time = this.fall_value = 0;
			this.damage_alpha = 0;
			this.bonus_alpha = 0;
			this.damage_blend = new float[3];
			this.v_angle = new float[3];
			this.bobtime = 0;

			this.oldviewangles = new float[3];

			this.oldvelocity = new float[3];

			this.next_drown_time = 0;

			this.old_waterlevel = 0;

			this.breather_sound = 0;
			this.machinegun_shots = 0;

			this.anim_end = 0;
			this.anim_priority = 0;
			this.anim_duck = false;
			this.anim_run = false;

			// powerup timers
			this.quad_framenum = 0;
			this.invincible_framenum = 0;
			this.breather_framenum = 0;
			this.enviro_framenum = 0;

			this.grenade_blew_up = false;
			this.grenade_time = 0;
			this.silencer_shots = 0;
			this.weapon_sound = 0;

			this.pickup_msg_time = 0;

			this.flood_locktill = 0; // locked from talking
			this.flood_when = new float[10]; // when messages were said
			this.flood_whenhead = 0; // head pointer for when said

			this.respawn_time = 0; // can respawn when time > this

			this.chase_target = null; // player we are chasing
			this.update_chase = false; // need to update chase info?
		}

		/** Reads a game client from the file. */
		public void read(BinaryReader f)
		{
			this.ps.load(f);

			this.ping = f.ReadInt32();

			this.pers.read(f);
			this.resp.read(f);

			this.old_pmove.load(f);

			this.showscores = f.ReadInt32() != 0;
			this.showinventory = f.ReadInt32() != 0;
			this.showhelp = f.ReadInt32() != 0;
			this.showhelpicon = f.ReadInt32() != 0;
			this.ammo_index = f.ReadInt32();

			this.buttons = f.ReadInt32();
			this.oldbuttons = f.ReadInt32();
			this.latched_buttons = f.ReadInt32();

			this.weapon_thunk = f.ReadInt32() != 0;

			this.newweapon = f.ReadItem();

			this.damage_armor = f.ReadInt32();
			this.damage_parmor = f.ReadInt32();
			this.damage_blood = f.ReadInt32();
			this.damage_knockback = f.ReadInt32();

			this.damage_from[0] = f.ReadSingle();
			this.damage_from[1] = f.ReadSingle();
			this.damage_from[2] = f.ReadSingle();

			this.killer_yaw = f.ReadSingle();

			this.weaponstate = f.ReadInt32();

			this.kick_angles[0] = f.ReadSingle();
			this.kick_angles[1] = f.ReadSingle();
			this.kick_angles[2] = f.ReadSingle();

			this.kick_origin[0] = f.ReadSingle();
			this.kick_origin[1] = f.ReadSingle();
			this.kick_origin[2] = f.ReadSingle();

			this.v_dmg_roll = f.ReadSingle();
			this.v_dmg_pitch = f.ReadSingle();
			this.v_dmg_time = f.ReadSingle();
			this.fall_time = f.ReadSingle();
			this.fall_value = f.ReadSingle();
			this.damage_alpha = f.ReadSingle();
			this.bonus_alpha = f.ReadSingle();

			this.damage_blend[0] = f.ReadSingle();
			this.damage_blend[1] = f.ReadSingle();
			this.damage_blend[2] = f.ReadSingle();

			this.v_angle[0] = f.ReadSingle();
			this.v_angle[1] = f.ReadSingle();
			this.v_angle[2] = f.ReadSingle();

			this.bobtime = f.ReadSingle();

			this.oldviewangles[0] = f.ReadSingle();
			this.oldviewangles[1] = f.ReadSingle();
			this.oldviewangles[2] = f.ReadSingle();

			this.oldvelocity[0] = f.ReadSingle();
			this.oldvelocity[1] = f.ReadSingle();
			this.oldvelocity[2] = f.ReadSingle();

			this.next_drown_time = f.ReadSingle();

			this.old_waterlevel = f.ReadInt32();
			this.breather_sound = f.ReadInt32();
			this.machinegun_shots = f.ReadInt32();
			this.anim_end = f.ReadInt32();
			this.anim_priority = f.ReadInt32();
			this.anim_duck = f.ReadInt32() != 0;
			this.anim_run = f.ReadInt32() != 0;

			this.quad_framenum = f.ReadSingle();
			this.invincible_framenum = f.ReadSingle();
			this.breather_framenum = f.ReadSingle();
			this.enviro_framenum = f.ReadSingle();

			this.grenade_blew_up = f.ReadInt32() != 0;
			this.grenade_time = f.ReadSingle();
			this.silencer_shots = f.ReadInt32();
			this.weapon_sound = f.ReadInt32();
			this.pickup_msg_time = f.ReadSingle();
			this.flood_locktill = f.ReadSingle();
			this.flood_when[0] = f.ReadSingle();
			this.flood_when[1] = f.ReadSingle();
			this.flood_when[2] = f.ReadSingle();
			this.flood_when[3] = f.ReadSingle();
			this.flood_when[4] = f.ReadSingle();
			this.flood_when[5] = f.ReadSingle();
			this.flood_when[6] = f.ReadSingle();
			this.flood_when[7] = f.ReadSingle();
			this.flood_when[8] = f.ReadSingle();
			this.flood_when[9] = f.ReadSingle();
			this.flood_whenhead = f.ReadInt32();
			this.respawn_time = f.ReadSingle();
			this.chase_target = f.ReadEdictRef();
			this.update_chase = f.ReadInt32() != 0;

			if (f.ReadInt32() != 8765)
				Console.Error.WriteLine("game client load failed for num=" + this.index);
		}

		/** Writes a game_client_t (a player) to a file. */
		public void write(BinaryWriter f)
		{
			this.ps.write(f);

			f.Write(this.ping);

			this.pers.write(f);
			this.resp.write(f);

			this.old_pmove.write(f);

			f.Write(this.showscores ? 1 : 0);
			f.Write(this.showinventory ? 1 : 0);
			f.Write(this.showhelp ? 1 : 0);
			f.Write(this.showhelpicon ? 1 : 0);
			f.Write(this.ammo_index);

			f.Write(this.buttons);
			f.Write(this.oldbuttons);
			f.Write(this.latched_buttons);

			f.Write(this.weapon_thunk ? 1 : 0);
			f.Write(this.newweapon);

			f.Write(this.damage_armor);
			f.Write(this.damage_parmor);
			f.Write(this.damage_blood);
			f.Write(this.damage_knockback);

			f.Write(this.damage_from[0]);
			f.Write(this.damage_from[1]);
			f.Write(this.damage_from[2]);

			f.Write(this.killer_yaw);

			f.Write(this.weaponstate);

			f.Write(this.kick_angles[0]);
			f.Write(this.kick_angles[1]);
			f.Write(this.kick_angles[2]);

			f.Write(this.kick_origin[0]);
			f.Write(this.kick_origin[1]);
			f.Write(this.kick_origin[2]);

			f.Write(this.v_dmg_roll);
			f.Write(this.v_dmg_pitch);
			f.Write(this.v_dmg_time);
			f.Write(this.fall_time);
			f.Write(this.fall_value);
			f.Write(this.damage_alpha);
			f.Write(this.bonus_alpha);

			f.Write(this.damage_blend[0]);
			f.Write(this.damage_blend[1]);
			f.Write(this.damage_blend[2]);

			f.Write(this.v_angle[0]);
			f.Write(this.v_angle[1]);
			f.Write(this.v_angle[2]);

			f.Write(this.bobtime);

			f.Write(this.oldviewangles[0]);
			f.Write(this.oldviewangles[1]);
			f.Write(this.oldviewangles[2]);

			f.Write(this.oldvelocity[0]);
			f.Write(this.oldvelocity[1]);
			f.Write(this.oldvelocity[2]);

			f.Write(this.next_drown_time);

			f.Write(this.old_waterlevel);
			f.Write(this.breather_sound);
			f.Write(this.machinegun_shots);
			f.Write(this.anim_end);
			f.Write(this.anim_priority);
			f.Write(this.anim_duck ? 1 : 0);
			f.Write(this.anim_run ? 1 : 0);

			f.Write(this.quad_framenum);
			f.Write(this.invincible_framenum);
			f.Write(this.breather_framenum);
			f.Write(this.enviro_framenum);

			f.Write(this.grenade_blew_up ? 1 : 0);
			f.Write(this.grenade_time);
			f.Write(this.silencer_shots);
			f.Write(this.weapon_sound);
			f.Write(this.pickup_msg_time);
			f.Write(this.flood_locktill);
			f.Write(this.flood_when[0]);
			f.Write(this.flood_when[1]);
			f.Write(this.flood_when[2]);
			f.Write(this.flood_when[3]);
			f.Write(this.flood_when[4]);
			f.Write(this.flood_when[5]);
			f.Write(this.flood_when[6]);
			f.Write(this.flood_when[7]);
			f.Write(this.flood_when[8]);
			f.Write(this.flood_when[9]);
			f.Write(this.flood_whenhead);
			f.Write(this.respawn_time);
			f.Write(this.chase_target);
			f.Write(this.update_chase ? 1 : 0);

			f.Write(8765);
		}
	}
}
