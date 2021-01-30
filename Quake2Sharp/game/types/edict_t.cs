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
	using adapters;
	using qcommon;
	using System;
	using System.IO;
	using util;

	public class edict_t
	{
		/** Constructor. */
		public edict_t(int i)
		{
			this.s = new(this);
			this.s.number = i;
			this.index = i;
			this.area = new(this);
		}

		/** Used during level loading. */
		public void cleararealinks()
		{
			this.area = new(this);
		}

		/** Integrated entity state. */
		public entity_state_t s;

		public bool inuse;
		public int linkcount;

		/**
     * FIXME: move these fields to a server private sv_entity_t. linked to a
     * division node or leaf.
     */
		public link_t area;

		/** if -1, use headnode instead. */
		public int num_clusters;

		public int[] clusternums = new int[Defines.MAX_ENT_CLUSTERS];

		/** unused if num_clusters != -1. */
		public int headnode;

		public int areanum, areanum2;

		//================================

		/** SVF_NOCLIENT, SVF_DEADMONSTER, SVF_MONSTER, etc. */
		public int svflags;

		public float[] mins = { 0, 0, 0 };
		public float[] maxs = { 0, 0, 0 };
		public float[] absmin = { 0, 0, 0 };
		public float[] absmax = { 0, 0, 0 };
		public float[] size = { 0, 0, 0 };
		public int solid;
		public int clipmask;

		//================================
		public int movetype;
		public int flags;
		public string model;

		/** sv.time when the object was freed. */
		public float freetime;

		//
		// only used locally in game, not by server
		//
		public string message;
		public string classname = "";
		public int spawnflags;
		public float timestamp;

		/** set in qe3, -1 = up, -2 = down */
		public float angle;

		public string target;
		public string targetname;
		public string killtarget;
		public string team;
		public string pathtarget;
		public string deathtarget;
		public string combattarget;
		public edict_t target_ent;
		public float speed, accel, decel;
		public float[] movedir = { 0, 0, 0 };
		public float[] pos1 = { 0, 0, 0 };
		public float[] pos2 = { 0, 0, 0 };
		public float[] velocity = { 0, 0, 0 };
		public float[] avelocity = { 0, 0, 0 };
		public int mass;
		public float air_finished;

		/** per entity gravity multiplier (1.0 is normal). */
		public float gravity;

		/** use for lowgrav artifact, flares. */
		public edict_t goalentity;

		public edict_t movetarget;
		public float yaw_speed;
		public float ideal_yaw;
		public float nextthink;
		public EntThinkAdapter prethink;
		public EntThinkAdapter think;
		public EntBlockedAdapter blocked;
		public EntTouchAdapter touch;
		public EntUseAdapter use;
		public EntPainAdapter pain;
		public EntDieAdapter die;

		/** Are all these legit? do we need more/less of them? */
		public float touch_debounce_time;

		public float pain_debounce_time;
		public float damage_debounce_time;

		/** Move to clientinfo. */
		public float fly_sound_debounce_time;

		public float last_move_time;
		public int health;
		public int max_health;
		public int gib_health;
		public int deadflag;
		public int show_hostile;
		public float powerarmor_time;

		/** target_changelevel. */
		public string map;

		/** Height above origin where eyesight is determined. */
		public int viewheight;

		public int takedamage;
		public int dmg;
		public int radius_dmg;
		public float dmg_radius;

		/** make this a spawntemp var? */
		public int sounds;

		public int count;
		public edict_t chain;
		public edict_t enemy;
		public edict_t oldenemy;
		public edict_t activator;
		public edict_t groundentity;
		public int groundentity_linkcount;
		public edict_t teamchain;
		public edict_t teammaster;

		/** can go in client only. */
		public edict_t mynoise;

		public edict_t mynoise2;
		public int noise_index;
		public int noise_index2;
		public float volume;
		public float attenuation;

		/** Timing variables. */
		public float wait;

		/** before firing targets... */
		public float delay;

		public float random;
		public float teleport_time;
		public int watertype;
		public int waterlevel;
		public float[] move_origin = { 0, 0, 0 };
		public float[] move_angles = { 0, 0, 0 };

		/** move this to clientinfo? . */
		public int light_level;

		/** also used as areaportal number. */
		public int style;

		public gitem_t item; // for bonus items

		/** common integrated data blocks. */
		public moveinfo_t moveinfo = new();

		public monsterinfo_t monsterinfo = new();
		public gclient_t client;
		public edict_t owner;

		/** Introduced by rst. */
		public int index;

		/////////////////////////////////////////////////

		public bool setField(string key, string value)
		{
			if (key.Equals("classname"))
			{
				this.classname = GameSpawn.ED_NewString(value);

				return true;
			} // F_LSTRING),

			if (key.Equals("model"))
			{
				this.model = GameSpawn.ED_NewString(value);

				return true;
			} // F_LSTRING),

			if (key.Equals("spawnflags"))
			{
				this.spawnflags = Lib.atoi(value);

				return true;
			} // F_INT),

			if (key.Equals("speed"))
			{
				this.speed = Lib.atof(value);

				return true;
			} // F_FLOAT),

			if (key.Equals("accel"))
			{
				this.accel = Lib.atof(value);

				return true;
			} // F_FLOAT),

			if (key.Equals("decel"))
			{
				this.decel = Lib.atof(value);

				return true;
			} // F_FLOAT),

			if (key.Equals("target"))
			{
				this.target = GameSpawn.ED_NewString(value);

				return true;
			} // F_LSTRING),

			if (key.Equals("targetname"))
			{
				this.targetname = GameSpawn.ED_NewString(value);

				return true;
			} // F_LSTRING),

			if (key.Equals("pathtarget"))
			{
				this.pathtarget = GameSpawn.ED_NewString(value);

				return true;
			} // F_LSTRING),

			if (key.Equals("deathtarget"))
			{
				this.deathtarget = GameSpawn.ED_NewString(value);

				return true;
			} // F_LSTRING),

			if (key.Equals("killtarget"))
			{
				this.killtarget = GameSpawn.ED_NewString(value);

				return true;
			} // F_LSTRING),

			if (key.Equals("combattarget"))
			{
				this.combattarget = GameSpawn.ED_NewString(value);

				return true;
			} // F_LSTRING),

			if (key.Equals("message"))
			{
				this.message = GameSpawn.ED_NewString(value);

				return true;
			} // F_LSTRING),

			if (key.Equals("team"))
			{
				this.team = GameSpawn.ED_NewString(value);
				Com.dprintln("Monster Team:" + this.team);

				return true;
			} // F_LSTRING),

			if (key.Equals("wait"))
			{
				this.wait = Lib.atof(value);

				return true;
			} // F_FLOAT),

			if (key.Equals("delay"))
			{
				this.delay = Lib.atof(value);

				return true;
			} // F_FLOAT),

			if (key.Equals("random"))
			{
				this.random = Lib.atof(value);

				return true;
			} // F_FLOAT),

			if (key.Equals("move_origin"))
			{
				this.move_origin = Lib.atov(value);

				return true;
			} // F_VECTOR),

			if (key.Equals("move_angles"))
			{
				this.move_angles = Lib.atov(value);

				return true;
			} // F_VECTOR),

			if (key.Equals("style"))
			{
				this.style = Lib.atoi(value);

				return true;
			} // F_INT),

			if (key.Equals("count"))
			{
				this.count = Lib.atoi(value);

				return true;
			} // F_INT),

			if (key.Equals("health"))
			{
				this.health = Lib.atoi(value);

				return true;
			} // F_INT),

			if (key.Equals("sounds"))
			{
				this.sounds = Lib.atoi(value);

				return true;
			} // F_INT),

			if (key.Equals("light"))
			{
				return true;
			} // F_IGNORE),

			if (key.Equals("dmg"))
			{
				this.dmg = Lib.atoi(value);

				return true;
			} // F_INT),

			if (key.Equals("mass"))
			{
				this.mass = Lib.atoi(value);

				return true;
			} // F_INT),

			if (key.Equals("volume"))
			{
				this.volume = Lib.atof(value);

				return true;
			} // F_FLOAT),

			if (key.Equals("attenuation"))
			{
				this.attenuation = Lib.atof(value);

				return true;
			} // F_FLOAT),

			if (key.Equals("map"))
			{
				this.map = GameSpawn.ED_NewString(value);

				return true;
			} // F_LSTRING),

			if (key.Equals("origin"))
			{
				this.s.origin = Lib.atov(value);

				return true;
			} // F_VECTOR),

			if (key.Equals("angles"))
			{
				this.s.angles = Lib.atov(value);

				return true;
			} // F_VECTOR),

			if (key.Equals("angle"))
			{
				this.s.angles = new float[] { 0, Lib.atof(value), 0 };

				return true;
			} // F_ANGLEHACK),

			if (key.Equals("item"))
			{
				GameBase.gi.error("ent.set(\"item\") called.");

				return true;
			} // F_ITEM)

			return false;
		}

		/** Writes the entity to the file. */
		public void write(BinaryWriter f)
		{
			this.s.write(f);
			f.Write(this.inuse);
			f.Write(this.linkcount);
			f.Write(this.num_clusters);

			f.Write(9999);

			if (this.clusternums == null)
				f.Write(-1);
			else
			{
				f.Write(Defines.MAX_ENT_CLUSTERS);

				for (var n = 0; n < Defines.MAX_ENT_CLUSTERS; n++)
					f.Write(this.clusternums[n]);
			}

			f.Write(this.headnode);
			f.Write(this.areanum);
			f.Write(this.areanum2);
			f.Write(this.svflags);
			f.Write(this.mins);
			f.Write(this.maxs);
			f.Write(this.absmin);
			f.Write(this.absmax);
			f.Write(this.size);
			f.Write(this.solid);
			f.Write(this.clipmask);

			f.Write(this.movetype);
			f.Write(this.flags);

			f.WriteQ(this.model);
			f.Write(this.freetime);
			f.WriteQ(this.message);
			f.WriteQ(this.classname);
			f.Write(this.spawnflags);
			f.Write(this.timestamp);

			f.Write(this.angle);

			f.WriteQ(this.target);
			f.WriteQ(this.targetname);
			f.WriteQ(this.killtarget);
			f.WriteQ(this.team);
			f.WriteQ(this.pathtarget);
			f.WriteQ(this.deathtarget);
			f.WriteQ(this.combattarget);

			f.Write(this.target_ent);

			f.Write(this.speed);
			f.Write(this.accel);
			f.Write(this.decel);

			f.Write(this.movedir);

			f.Write(this.pos1);
			f.Write(this.pos2);

			f.Write(this.velocity);
			f.Write(this.avelocity);

			f.Write(this.mass);
			f.Write(this.air_finished);

			f.Write(this.gravity);

			f.Write(this.goalentity);
			f.Write(this.movetarget);

			f.Write(this.yaw_speed);
			f.Write(this.ideal_yaw);

			f.Write(this.nextthink);

			f.Write(this.prethink);
			f.Write(this.think);
			f.Write(this.blocked);

			f.Write(this.touch);
			f.Write(this.use);
			f.Write(this.pain);
			f.Write(this.die);

			f.Write(this.touch_debounce_time);
			f.Write(this.pain_debounce_time);
			f.Write(this.damage_debounce_time);

			f.Write(this.fly_sound_debounce_time);
			f.Write(this.last_move_time);

			f.Write(this.health);
			f.Write(this.max_health);

			f.Write(this.gib_health);
			f.Write(this.deadflag);
			f.Write(this.show_hostile);

			f.Write(this.powerarmor_time);

			f.WriteQ(this.map);

			f.Write(this.viewheight);
			f.Write(this.takedamage);
			f.Write(this.dmg);
			f.Write(this.radius_dmg);
			f.Write(this.dmg_radius);

			f.Write(this.sounds);
			f.Write(this.count);

			f.Write(this.chain);
			f.Write(this.enemy);
			f.Write(this.oldenemy);
			f.Write(this.activator);
			f.Write(this.groundentity);
			f.Write(this.groundentity_linkcount);
			f.Write(this.teamchain);
			f.Write(this.teammaster);

			f.Write(this.mynoise);
			f.Write(this.mynoise2);

			f.Write(this.noise_index);
			f.Write(this.noise_index2);

			f.Write(this.volume);
			f.Write(this.attenuation);
			f.Write(this.wait);
			f.Write(this.delay);
			f.Write(this.random);

			f.Write(this.teleport_time);

			f.Write(this.watertype);
			f.Write(this.waterlevel);
			f.Write(this.move_origin);
			f.Write(this.move_angles);

			f.Write(this.light_level);
			f.Write(this.style);

			f.Write(this.item);

			this.moveinfo.write(f);
			this.monsterinfo.write(f);

			if (this.client == null)
				f.Write(-1);
			else
				f.Write(this.client.index);

			f.Write(this.owner);

			// rst's checker :-)
			f.Write(9876);
		}

		/** Reads the entity from the file. */
		public void read(BinaryReader f)
		{
			this.s.read(f);
			this.inuse = f.ReadBoolean();
			this.linkcount = f.ReadInt32();
			this.num_clusters = f.ReadInt32();

			if (f.ReadInt32() != 9999)
				Console.WriteLine(new Exception("wrong read pos!"));

			var len = f.ReadInt32();

			if (len == -1)
				this.clusternums = null;
			else
			{
				this.clusternums = new int[Defines.MAX_ENT_CLUSTERS];

				for (var n = 0; n < Defines.MAX_ENT_CLUSTERS; n++)
					this.clusternums[n] = f.ReadInt32();
			}

			this.headnode = f.ReadInt32();
			this.areanum = f.ReadInt32();
			this.areanum2 = f.ReadInt32();
			this.svflags = f.ReadInt32();
			this.mins = f.ReadVector();
			this.maxs = f.ReadVector();
			this.absmin = f.ReadVector();
			this.absmax = f.ReadVector();
			this.size = f.ReadVector();
			this.solid = f.ReadInt32();
			this.clipmask = f.ReadInt32();

			this.movetype = f.ReadInt32();
			this.flags = f.ReadInt32();

			this.model = f.ReadStringQ();
			this.freetime = f.ReadSingle();
			this.message = f.ReadStringQ();
			this.classname = f.ReadStringQ();
			this.spawnflags = f.ReadInt32();
			this.timestamp = f.ReadSingle();

			this.angle = f.ReadSingle();

			this.target = f.ReadStringQ();
			this.targetname = f.ReadStringQ();
			this.killtarget = f.ReadStringQ();
			this.team = f.ReadStringQ();
			this.pathtarget = f.ReadStringQ();
			this.deathtarget = f.ReadStringQ();
			this.combattarget = f.ReadStringQ();

			this.target_ent = f.ReadEdictRef();

			this.speed = f.ReadSingle();
			this.accel = f.ReadSingle();
			this.decel = f.ReadSingle();

			this.movedir = f.ReadVector();

			this.pos1 = f.ReadVector();
			this.pos2 = f.ReadVector();

			this.velocity = f.ReadVector();
			this.avelocity = f.ReadVector();

			this.mass = f.ReadInt32();
			this.air_finished = f.ReadSingle();

			this.gravity = f.ReadSingle();

			this.goalentity = f.ReadEdictRef();
			this.movetarget = f.ReadEdictRef();

			this.yaw_speed = f.ReadSingle();
			this.ideal_yaw = f.ReadSingle();

			this.nextthink = f.ReadSingle();

			this.prethink = (EntThinkAdapter)f.ReadAdapter();
			this.think = (EntThinkAdapter)f.ReadAdapter();
			this.blocked = (EntBlockedAdapter)f.ReadAdapter();

			this.touch = (EntTouchAdapter)f.ReadAdapter();
			this.use = (EntUseAdapter)f.ReadAdapter();
			this.pain = (EntPainAdapter)f.ReadAdapter();
			this.die = (EntDieAdapter)f.ReadAdapter();

			this.touch_debounce_time = f.ReadSingle();
			this.pain_debounce_time = f.ReadSingle();
			this.damage_debounce_time = f.ReadSingle();

			this.fly_sound_debounce_time = f.ReadSingle();
			this.last_move_time = f.ReadSingle();

			this.health = f.ReadInt32();
			this.max_health = f.ReadInt32();

			this.gib_health = f.ReadInt32();
			this.deadflag = f.ReadInt32();
			this.show_hostile = f.ReadInt32();

			this.powerarmor_time = f.ReadSingle();

			this.map = f.ReadStringQ();

			this.viewheight = f.ReadInt32();
			this.takedamage = f.ReadInt32();
			this.dmg = f.ReadInt32();
			this.radius_dmg = f.ReadInt32();
			this.dmg_radius = f.ReadSingle();

			this.sounds = f.ReadInt32();
			this.count = f.ReadInt32();

			this.chain = f.ReadEdictRef();
			this.enemy = f.ReadEdictRef();

			this.oldenemy = f.ReadEdictRef();
			this.activator = f.ReadEdictRef();
			this.groundentity = f.ReadEdictRef();

			this.groundentity_linkcount = f.ReadInt32();
			this.teamchain = f.ReadEdictRef();
			this.teammaster = f.ReadEdictRef();

			this.mynoise = f.ReadEdictRef();
			this.mynoise2 = f.ReadEdictRef();

			this.noise_index = f.ReadInt32();
			this.noise_index2 = f.ReadInt32();

			this.volume = f.ReadSingle();
			this.attenuation = f.ReadSingle();
			this.wait = f.ReadSingle();
			this.delay = f.ReadSingle();
			this.random = f.ReadSingle();

			this.teleport_time = f.ReadSingle();

			this.watertype = f.ReadInt32();
			this.waterlevel = f.ReadInt32();
			this.move_origin = f.ReadVector();
			this.move_angles = f.ReadVector();

			this.light_level = f.ReadInt32();
			this.style = f.ReadInt32();

			this.item = f.ReadItem();

			this.moveinfo.read(f);
			this.monsterinfo.read(f);

			var ndx = f.ReadInt32();

			if (ndx == -1)
				this.client = null;
			else
				this.client = GameBase.game.clients[ndx];

			this.owner = f.ReadEdictRef();

			// rst's checker :-)
			if (f.ReadInt32() != 9876)
				Console.Error.WriteLine("ent load check failed for num " + this.index);
		}
	}
}
