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
using types;
using util;

public class GameFunc
{
	private static void Move_Calc(edict_t ent, float[] dest, EntThinkAdapter func)
	{
		Math3D.VectorClear(ent.velocity);
		Math3D.VectorSubtract(dest, ent.s.origin, ent.moveinfo.dir);
		ent.moveinfo.remaining_distance = Math3D.VectorNormalize(ent.moveinfo.dir);

		ent.moveinfo.endfunc = func;

		if (ent.moveinfo.speed == ent.moveinfo.accel && ent.moveinfo.speed == ent.moveinfo.decel)
		{
			if (GameBase.level.current_entity == ((ent.flags & Defines.FL_TEAMSLAVE) != 0 ? ent.teammaster : ent))
				GameFunc.Move_Begin.think(ent);
			else
			{
				ent.nextthink = GameBase.level.time + Defines.FRAMETIME;
				ent.think = GameFunc.Move_Begin;
			}
		}
		else
		{
			// accelerative
			ent.moveinfo.current_speed = 0;
			ent.think = GameFunc.Think_AccelMove;
			ent.nextthink = GameBase.level.time + Defines.FRAMETIME;
		}
	}

	private static void AngleMove_Calc(edict_t ent, EntThinkAdapter func)
	{
		Math3D.VectorClear(ent.avelocity);
		ent.moveinfo.endfunc = func;

		if (GameBase.level.current_entity == ((ent.flags & Defines.FL_TEAMSLAVE) != 0 ? ent.teammaster : ent))
			GameFunc.AngleMove_Begin.think(ent);
		else
		{
			ent.nextthink = GameBase.level.time + Defines.FRAMETIME;
			ent.think = GameFunc.AngleMove_Begin;
		}
	}

	/**
     * Think_AccelMove
     * 
     * The team has completed a frame of movement, so change the speed for the
     * next frame.
     */
	private static float AccelerationDistance(float target, float rate)
	{
		return target * (target / rate + 1) / 2;
	}

	private static void plat_CalcAcceleratedMove(moveinfo_t moveinfo)
	{
		float accel_dist;
		float decel_dist;

		moveinfo.move_speed = moveinfo.speed;

		if (moveinfo.remaining_distance < moveinfo.accel)
		{
			moveinfo.current_speed = moveinfo.remaining_distance;

			return;
		}

		accel_dist = GameFunc.AccelerationDistance(moveinfo.speed, moveinfo.accel);
		decel_dist = GameFunc.AccelerationDistance(moveinfo.speed, moveinfo.decel);

		if (moveinfo.remaining_distance - accel_dist - decel_dist < 0)
		{
			float f;

			f = (moveinfo.accel + moveinfo.decel) / (moveinfo.accel * moveinfo.decel);
			moveinfo.move_speed = (float)((-2 + Math.Sqrt(4 - 4 * f * (-2 * moveinfo.remaining_distance))) / (2 * f));
			decel_dist = GameFunc.AccelerationDistance(moveinfo.move_speed, moveinfo.decel);
		}

		moveinfo.decel_distance = decel_dist;
	}

	private static void plat_Accelerate(moveinfo_t moveinfo)
	{
		// are we decelerating?
		if (moveinfo.remaining_distance <= moveinfo.decel_distance)
		{
			if (moveinfo.remaining_distance < moveinfo.decel_distance)
			{
				if (moveinfo.next_speed != 0)
				{
					moveinfo.current_speed = moveinfo.next_speed;
					moveinfo.next_speed = 0;

					return;
				}

				if (moveinfo.current_speed > moveinfo.decel)
					moveinfo.current_speed -= moveinfo.decel;
			}

			return;
		}

		// are we at full speed and need to start decelerating during this move?
		if (moveinfo.current_speed == moveinfo.move_speed)
		{
			if (moveinfo.remaining_distance - moveinfo.current_speed < moveinfo.decel_distance)
			{
				float p1_distance;
				float p2_distance;
				float distance;

				p1_distance = moveinfo.remaining_distance - moveinfo.decel_distance;
				p2_distance = moveinfo.move_speed * (1.0f - p1_distance / moveinfo.move_speed);
				distance = p1_distance + p2_distance;
				moveinfo.current_speed = moveinfo.move_speed;
				moveinfo.next_speed = moveinfo.move_speed - moveinfo.decel * (p2_distance / distance);

				return;
			}
		}

		// are we accelerating?
		if (moveinfo.current_speed < moveinfo.speed)
		{
			float old_speed;
			float p1_distance;
			float p1_speed;
			float p2_distance;
			float distance;

			old_speed = moveinfo.current_speed;

			// figure simple acceleration up to move_speed
			moveinfo.current_speed += moveinfo.accel;

			if (moveinfo.current_speed > moveinfo.speed)
				moveinfo.current_speed = moveinfo.speed;

			// are we accelerating throughout this entire move?
			if (moveinfo.remaining_distance - moveinfo.current_speed >= moveinfo.decel_distance)
				return;

			// during this move we will accelrate from current_speed to
			// move_speed
			// and cross over the decel_distance; figure the average speed for
			// the
			// entire move
			p1_distance = moveinfo.remaining_distance - moveinfo.decel_distance;
			p1_speed = (old_speed + moveinfo.move_speed) / 2.0f;
			p2_distance = moveinfo.move_speed * (1.0f - p1_distance / p1_speed);
			distance = p1_distance + p2_distance;
			moveinfo.current_speed = p1_speed * (p1_distance / distance) + moveinfo.move_speed * (p2_distance / distance);
			moveinfo.next_speed = moveinfo.move_speed - moveinfo.decel * (p2_distance / distance);

			return;
		}

		// we are at constant velocity (move_speed)
		return;
	}

	private static void plat_go_up(edict_t ent)
	{
		if (0 == (ent.flags & Defines.FL_TEAMSLAVE))
		{
			if (ent.moveinfo.sound_start != 0)
				GameBase.gi.sound(ent, Defines.CHAN_NO_PHS_ADD + Defines.CHAN_VOICE, ent.moveinfo.sound_start, 1, Defines.ATTN_STATIC, 0);

			ent.s.sound = ent.moveinfo.sound_middle;
		}

		ent.moveinfo.state = GameFunc.STATE_UP;
		GameFunc.Move_Calc(ent, ent.moveinfo.start_origin, GameFunc.plat_hit_top);
	}

	private static void plat_spawn_inside_trigger(edict_t ent)
	{
		edict_t trigger;
		float[] tmin = { 0, 0, 0 }, tmax = { 0, 0, 0 };

		//
		//	   middle trigger
		//	
		trigger = GameUtil.G_Spawn();
		trigger.touch = GameFunc.Touch_Plat_Center;
		trigger.movetype = Defines.MOVETYPE_NONE;
		trigger.solid = Defines.SOLID_TRIGGER;
		trigger.enemy = ent;

		tmin[0] = ent.mins[0] + 25;
		tmin[1] = ent.mins[1] + 25;
		tmin[2] = ent.mins[2];

		tmax[0] = ent.maxs[0] - 25;
		tmax[1] = ent.maxs[1] - 25;
		tmax[2] = ent.maxs[2] + 8;

		tmin[2] = tmax[2] - (ent.pos1[2] - ent.pos2[2] + GameBase.st.lip);

		if ((ent.spawnflags & GameFunc.PLAT_LOW_TRIGGER) != 0)
			tmax[2] = tmin[2] + 8;

		if (tmax[0] - tmin[0] <= 0)
		{
			tmin[0] = (ent.mins[0] + ent.maxs[0]) * 0.5f;
			tmax[0] = tmin[0] + 1;
		}

		if (tmax[1] - tmin[1] <= 0)
		{
			tmin[1] = (ent.mins[1] + ent.maxs[1]) * 0.5f;
			tmax[1] = tmin[1] + 1;
		}

		Math3D.VectorCopy(tmin, trigger.mins);
		Math3D.VectorCopy(tmax, trigger.maxs);

		GameBase.gi.linkentity(trigger);
	}

	/**
     * QUAKED func_plat (0 .5 .8) ? PLAT_LOW_TRIGGER speed default 150
     * 
     * Plats are always drawn in the extended position, so they will light
     * correctly.
     * 
     * If the plat is the target of another trigger or button, it will start out
     * disabled in the extended position until it is trigger, when it will lower
     * and become a normal plat.
     * 
     * "speed" overrides default 200. "accel" overrides default 500 "lip"
     * overrides default 8 pixel lip
     * 
     * If the "height" key is set, that will determine the amount the plat
     * moves, instead of being implicitly determoveinfoned by the model's
     * height.
     * 
     * Set "sounds" to one of the following: 1) base fast 2) chain slow
     */
	public static void SP_func_plat(edict_t ent)
	{
		Math3D.VectorClear(ent.s.angles);
		ent.solid = Defines.SOLID_BSP;
		ent.movetype = Defines.MOVETYPE_PUSH;

		GameBase.gi.setmodel(ent, ent.model);

		ent.blocked = GameFunc.plat_blocked;

		if (0 == ent.speed)
			ent.speed = 20;
		else
			ent.speed *= 0.1f;

		if (ent.accel == 0)
			ent.accel = 5;
		else
			ent.accel *= 0.1f;

		if (ent.decel == 0)
			ent.decel = 5;
		else
			ent.decel *= 0.1f;

		if (ent.dmg == 0)
			ent.dmg = 2;

		if (GameBase.st.lip == 0)
			GameBase.st.lip = 8;

		// pos1 is the top position, pos2 is the bottom
		Math3D.VectorCopy(ent.s.origin, ent.pos1);
		Math3D.VectorCopy(ent.s.origin, ent.pos2);

		if (GameBase.st.height != 0)
			ent.pos2[2] -= GameBase.st.height;
		else
			ent.pos2[2] -= ent.maxs[2] - ent.mins[2] - GameBase.st.lip;

		ent.use = GameFunc.Use_Plat;

		GameFunc.plat_spawn_inside_trigger(ent); // the "start moving" trigger

		if (ent.targetname != null)
			ent.moveinfo.state = GameFunc.STATE_UP;
		else
		{
			Math3D.VectorCopy(ent.pos2, ent.s.origin);
			GameBase.gi.linkentity(ent);
			ent.moveinfo.state = GameFunc.STATE_BOTTOM;
		}

		ent.moveinfo.speed = ent.speed;
		ent.moveinfo.accel = ent.accel;
		ent.moveinfo.decel = ent.decel;
		ent.moveinfo.wait = ent.wait;
		Math3D.VectorCopy(ent.pos1, ent.moveinfo.start_origin);
		Math3D.VectorCopy(ent.s.angles, ent.moveinfo.start_angles);
		Math3D.VectorCopy(ent.pos2, ent.moveinfo.end_origin);
		Math3D.VectorCopy(ent.s.angles, ent.moveinfo.end_angles);

		ent.moveinfo.sound_start = GameBase.gi.soundindex("plats/pt1_strt.wav");
		ent.moveinfo.sound_middle = GameBase.gi.soundindex("plats/pt1_mid.wav");
		ent.moveinfo.sound_end = GameBase.gi.soundindex("plats/pt1_end.wav");
	}

	/**
     * DOORS
     * 
     * spawn a trigger surrounding the entire team unless it is already targeted
     * by another.
     * 
     */
	/**
	 * QUAKED func_door (0 .5 .8) ? START_OPEN x CRUSHER NOMONSTER ANIMATED
	 * TOGGLE ANIMATED_FAST TOGGLE wait in both the start and end states for a
	 * trigger event. START_OPEN the door to moves to its destination when
	 * spawned, and operate in reverse. It is used to temporarily or permanently
	 * close off an area when triggered (not useful for touch or takedamage
	 * doors). NOMONSTER monsters will not trigger this door
	 * 
	 * "message" is printed when the door is touched if it is a trigger door and
	 * it hasn't been fired yet "angle" determines the opening direction
	 * "targetname" if set, no touch field will be spawned and a remote button
	 * or trigger field activates the door. "health" if set, door must be shot
	 * open "speed" movement speed (100 default) "wait" wait before returning (3
	 * default, -1 = never return) "lip" lip remaining at end of move (8
	 * default) "dmg" damage to inflict when blocked (2 default) "sounds" 1)
	 * silent 2) light 3) medium 4) heavy
	 */
	private static void door_use_areaportals(edict_t self, bool open)
	{
		edict_t t = null;

		if (self.target == null)
			return;

		EdictIterator edit = null;

		while ((edit = GameBase.G_Find(edit, GameBase.findByTarget, self.target)) != null)
		{
			t = edit.o;

			if (Lib.Q_stricmp(t.classname, "func_areaportal") == 0)
				GameBase.gi.SetAreaPortalState(t.style, open);
		}
	}

	private static void door_go_up(edict_t self, edict_t activator)
	{
		if (self.moveinfo.state == GameFunc.STATE_UP)
			return; // already going up

		if (self.moveinfo.state == GameFunc.STATE_TOP)
		{
			// reset top wait time
			if (self.moveinfo.wait >= 0)
				self.nextthink = GameBase.level.time + self.moveinfo.wait;

			return;
		}

		if (0 == (self.flags & Defines.FL_TEAMSLAVE))
		{
			if (self.moveinfo.sound_start != 0)
				GameBase.gi.sound(self, Defines.CHAN_NO_PHS_ADD + Defines.CHAN_VOICE, self.moveinfo.sound_start, 1, Defines.ATTN_STATIC, 0);

			self.s.sound = self.moveinfo.sound_middle;
		}

		self.moveinfo.state = GameFunc.STATE_UP;

		if (Lib.strcmp(self.classname, "func_door") == 0)
			GameFunc.Move_Calc(self, self.moveinfo.end_origin, GameFunc.door_hit_top);
		else if (Lib.strcmp(self.classname, "func_door_rotating") == 0)
			GameFunc.AngleMove_Calc(self, GameFunc.door_hit_top);

		GameUtil.G_UseTargets(self, activator);
		GameFunc.door_use_areaportals(self, true);
	}

	/**
     * QUAKED func_water (0 .5 .8) ? START_OPEN func_water is a moveable water
     * brush. It must be targeted to operate. Use a non-water texture at your
     * own risk.
     * 
     * START_OPEN causes the water to move to its destination when spawned and
     * operate in reverse.
     * 
     * "angle" determines the opening direction (up or down only) "speed"
     * movement speed (25 default) "wait" wait before returning (-1 default, -1 =
     * TOGGLE) "lip" lip remaining at end of move (0 default) "sounds" (yes,
     * these need to be changed) 0) no sound 1) water 2) lava
     */
	public static void SP_func_water(edict_t self)
	{
		float[] abs_movedir = { 0, 0, 0 };

		GameBase.G_SetMovedir(self.s.angles, self.movedir);
		self.movetype = Defines.MOVETYPE_PUSH;
		self.solid = Defines.SOLID_BSP;
		GameBase.gi.setmodel(self, self.model);

		switch (self.sounds)
		{
			default:
				break;

			case 1: // water
				self.moveinfo.sound_start = GameBase.gi.soundindex("world/mov_watr.wav");
				self.moveinfo.sound_end = GameBase.gi.soundindex("world/stp_watr.wav");

				break;

			case 2: // lava
				self.moveinfo.sound_start = GameBase.gi.soundindex("world/mov_watr.wav");
				self.moveinfo.sound_end = GameBase.gi.soundindex("world/stp_watr.wav");

				break;
		}

		// calculate second position
		Math3D.VectorCopy(self.s.origin, self.pos1);
		abs_movedir[0] = Math.Abs(self.movedir[0]);
		abs_movedir[1] = Math.Abs(self.movedir[1]);
		abs_movedir[2] = Math.Abs(self.movedir[2]);
		self.moveinfo.distance = abs_movedir[0] * self.size[0] + abs_movedir[1] * self.size[1] + abs_movedir[2] * self.size[2] - GameBase.st.lip;
		Math3D.VectorMA(self.pos1, self.moveinfo.distance, self.movedir, self.pos2);

		// if it starts open, switch the positions
		if ((self.spawnflags & GameFunc.DOOR_START_OPEN) != 0)
		{
			Math3D.VectorCopy(self.pos2, self.s.origin);
			Math3D.VectorCopy(self.pos1, self.pos2);
			Math3D.VectorCopy(self.s.origin, self.pos1);
		}

		Math3D.VectorCopy(self.pos1, self.moveinfo.start_origin);
		Math3D.VectorCopy(self.s.angles, self.moveinfo.start_angles);
		Math3D.VectorCopy(self.pos2, self.moveinfo.end_origin);
		Math3D.VectorCopy(self.s.angles, self.moveinfo.end_angles);

		self.moveinfo.state = GameFunc.STATE_BOTTOM;

		if (0 == self.speed)
			self.speed = 25;

		self.moveinfo.accel = self.moveinfo.decel = self.moveinfo.speed = self.speed;

		if (0 == self.wait)
			self.wait = -1;

		self.moveinfo.wait = self.wait;

		self.use = GameFunc.door_use;

		if (self.wait == -1)
			self.spawnflags |= GameFunc.DOOR_TOGGLE;

		self.classname = "func_door";

		GameBase.gi.linkentity(self);
	}

	private static void train_resume(edict_t self)
	{
		edict_t ent;
		float[] dest = { 0, 0, 0 };

		ent = self.target_ent;

		Math3D.VectorSubtract(ent.s.origin, self.mins, dest);
		self.moveinfo.state = GameFunc.STATE_TOP;
		Math3D.VectorCopy(self.s.origin, self.moveinfo.start_origin);
		Math3D.VectorCopy(dest, self.moveinfo.end_origin);
		GameFunc.Move_Calc(self, dest, GameFunc.train_wait);
		self.spawnflags |= GameFunc.TRAIN_START_ON;
	}

	public static void SP_func_train(edict_t self)
	{
		self.movetype = Defines.MOVETYPE_PUSH;

		Math3D.VectorClear(self.s.angles);
		self.blocked = GameFunc.train_blocked;

		if ((self.spawnflags & GameFunc.TRAIN_BLOCK_STOPS) != 0)
			self.dmg = 0;
		else
		{
			if (0 == self.dmg)
				self.dmg = 100;
		}

		self.solid = Defines.SOLID_BSP;
		GameBase.gi.setmodel(self, self.model);

		if (GameBase.st.noise != null)
			self.moveinfo.sound_middle = GameBase.gi.soundindex(GameBase.st.noise);

		if (0 == self.speed)
			self.speed = 100;

		self.moveinfo.speed = self.speed;
		self.moveinfo.accel = self.moveinfo.decel = self.moveinfo.speed;

		self.use = GameFunc.train_use;

		GameBase.gi.linkentity(self);

		if (self.target != null)
		{
			// start trains on the second frame, to make sure their targets have
			// had
			// a chance to spawn
			self.nextthink = GameBase.level.time + Defines.FRAMETIME;
			self.think = GameFunc.func_train_find;
		}
		else
			GameBase.gi.dprintf("func_train without a target at " + Lib.vtos(self.absmin) + "\n");
	}

	public static void SP_func_timer(edict_t self)
	{
		if (0 == self.wait)
			self.wait = 1.0f;

		self.use = GameFunc.func_timer_use;
		self.think = GameFunc.func_timer_think;

		if (self.random >= self.wait)
		{
			self.random = self.wait - Defines.FRAMETIME;
			GameBase.gi.dprintf("func_timer at " + Lib.vtos(self.s.origin) + " has random >= wait\n");
		}

		if ((self.spawnflags & 1) != 0)
		{
			self.nextthink = GameBase.level.time + 1.0f + GameBase.st.pausetime + self.delay + self.wait + Lib.crandom() * self.random;
			self.activator = self;
		}

		self.svflags = Defines.SVF_NOCLIENT;
	}

	/**
     * PLATS
     * 
     * movement options:
     * 
     * linear smooth start, hard stop smooth start, smooth stop
     * 
     * start end acceleration speed deceleration begin sound end sound target
     * fired when reaching end wait at end
     * 
     * object characteristics that use move segments
     * --------------------------------------------- movetype_push, or
     * movetype_stop action when touched action when blocked action when used
     * disabled? auto trigger spawning
     * 
     */
	public static readonly int PLAT_LOW_TRIGGER = 1;

	public static readonly int STATE_TOP = 0;
	public static readonly int STATE_BOTTOM = 1;
	public static readonly int STATE_UP = 2;
	public static readonly int STATE_DOWN = 3;
	public static readonly int DOOR_START_OPEN = 1;
	public static readonly int DOOR_REVERSE = 2;
	public static readonly int DOOR_CRUSHER = 4;
	public static readonly int DOOR_NOMONSTER = 8;
	public static readonly int DOOR_TOGGLE = 32;
	public static readonly int DOOR_X_AXIS = 64;
	public static readonly int DOOR_Y_AXIS = 128;

	//
	//	   Support routines for movement (changes in origin using velocity)
	//

	private static readonly EntThinkAdapter Move_Done = new("move_done", ent =>
	{
		Math3D.VectorClear(ent.velocity);
		ent.moveinfo.endfunc.think(ent);

		return true;
	});

	private static readonly EntThinkAdapter Move_Final = new("move_final", ent =>
	{
		if (ent.moveinfo.remaining_distance == 0)
		{
			GameFunc.Move_Done.think(ent);

			return true;
		}

		Math3D.VectorScale(ent.moveinfo.dir, ent.moveinfo.remaining_distance / Defines.FRAMETIME, ent.velocity);

		ent.think = GameFunc.Move_Done;
		ent.nextthink = GameBase.level.time + Defines.FRAMETIME;

		return true;
	});

	private static readonly EntThinkAdapter Move_Begin = new("move_begin", ent =>
	{
		float frames;

		if (ent.moveinfo.speed * Defines.FRAMETIME >= ent.moveinfo.remaining_distance)
		{
			GameFunc.Move_Final.think(ent);

			return true;
		}

		Math3D.VectorScale(ent.moveinfo.dir, ent.moveinfo.speed, ent.velocity);
		frames = (float)Math.Floor(ent.moveinfo.remaining_distance / ent.moveinfo.speed / Defines.FRAMETIME);
		ent.moveinfo.remaining_distance -= frames * ent.moveinfo.speed * Defines.FRAMETIME;
		ent.nextthink = GameBase.level.time + frames * Defines.FRAMETIME;
		ent.think = GameFunc.Move_Final;

		return true;
	});

	//
	//	   Support routines for angular movement (changes in angle using avelocity)
	//

	private static readonly EntThinkAdapter AngleMove_Done = new("agnle_move_done", ent =>
	{
		Math3D.VectorClear(ent.avelocity);
		ent.moveinfo.endfunc.think(ent);

		return true;
	});

	private static readonly EntThinkAdapter AngleMove_Final = new("angle_move_final", ent =>
	{
		float[] move = { 0, 0, 0 };

		if (ent.moveinfo.state == GameFunc.STATE_UP)
			Math3D.VectorSubtract(ent.moveinfo.end_angles, ent.s.angles, move);
		else
			Math3D.VectorSubtract(ent.moveinfo.start_angles, ent.s.angles, move);

		if (Math3D.VectorEquals(move, Globals.vec3_origin))
		{
			GameFunc.AngleMove_Done.think(ent);

			return true;
		}

		Math3D.VectorScale(move, 1.0f / Defines.FRAMETIME, ent.avelocity);

		ent.think = GameFunc.AngleMove_Done;
		ent.nextthink = GameBase.level.time + Defines.FRAMETIME;

		return true;
	});

	private static readonly EntThinkAdapter AngleMove_Begin = new("angle_move_begin", ent =>
	{
		float[] destdelta = { 0, 0, 0 };
		float len;
		float traveltime;
		float frames;

		// set destdelta to the vector needed to move
		if (ent.moveinfo.state == GameFunc.STATE_UP)
			Math3D.VectorSubtract(ent.moveinfo.end_angles, ent.s.angles, destdelta);
		else
			Math3D.VectorSubtract(ent.moveinfo.start_angles, ent.s.angles, destdelta);

		// calculate length of vector
		len = Math3D.VectorLength(destdelta);

		// divide by speed to get time to reach dest
		traveltime = len / ent.moveinfo.speed;

		if (traveltime < Defines.FRAMETIME)
		{
			GameFunc.AngleMove_Final.think(ent);

			return true;
		}

		frames = (float)Math.Floor(traveltime / Defines.FRAMETIME);

		// scale the destdelta vector by the time spent traveling to get
		// velocity
		Math3D.VectorScale(destdelta, 1.0f / traveltime, ent.avelocity);

		// set nextthink to trigger a think when dest is reached
		ent.nextthink = GameBase.level.time + frames * Defines.FRAMETIME;
		ent.think = GameFunc.AngleMove_Final;

		return true;
	});

	private static readonly EntThinkAdapter Think_AccelMove = new("thinc_accelmove", ent =>
	{
		ent.moveinfo.remaining_distance -= ent.moveinfo.current_speed;

		if (ent.moveinfo.current_speed == 0) // starting or blocked
			GameFunc.plat_CalcAcceleratedMove(ent.moveinfo);

		GameFunc.plat_Accelerate(ent.moveinfo);

		// will the entire move complete on next frame?
		if (ent.moveinfo.remaining_distance <= ent.moveinfo.current_speed)
		{
			GameFunc.Move_Final.think(ent);

			return true;
		}

		Math3D.VectorScale(ent.moveinfo.dir, ent.moveinfo.current_speed * 10, ent.velocity);
		ent.nextthink = GameBase.level.time + Defines.FRAMETIME;
		ent.think = GameFunc.Think_AccelMove;

		return true;
	});

	private static readonly EntThinkAdapter plat_hit_top = new("plat_hit_top", ent =>
	{
		if (0 == (ent.flags & Defines.FL_TEAMSLAVE))
		{
			if (ent.moveinfo.sound_end != 0)
				GameBase.gi.sound(ent, Defines.CHAN_NO_PHS_ADD + Defines.CHAN_VOICE, ent.moveinfo.sound_end, 1, Defines.ATTN_STATIC, 0);

			ent.s.sound = 0;
		}

		ent.moveinfo.state = GameFunc.STATE_TOP;

		ent.think = GameFunc.plat_go_down;
		ent.nextthink = GameBase.level.time + 3;

		return true;
	});

	private static readonly EntThinkAdapter plat_hit_bottom = new("plat_hit_bottom", ent =>
	{
		if (0 == (ent.flags & Defines.FL_TEAMSLAVE))
		{
			if (ent.moveinfo.sound_end != 0)
				GameBase.gi.sound(ent, Defines.CHAN_NO_PHS_ADD + Defines.CHAN_VOICE, ent.moveinfo.sound_end, 1, Defines.ATTN_STATIC, 0);

			ent.s.sound = 0;
		}

		ent.moveinfo.state = GameFunc.STATE_BOTTOM;

		return true;
	});

	private static readonly EntThinkAdapter plat_go_down = new("plat_go_down", ent =>
	{
		if (0 == (ent.flags & Defines.FL_TEAMSLAVE))
		{
			if (ent.moveinfo.sound_start != 0)
				GameBase.gi.sound(ent, Defines.CHAN_NO_PHS_ADD + Defines.CHAN_VOICE, ent.moveinfo.sound_start, 1, Defines.ATTN_STATIC, 0);

			ent.s.sound = ent.moveinfo.sound_middle;
		}

		ent.moveinfo.state = GameFunc.STATE_DOWN;
		GameFunc.Move_Calc(ent, ent.moveinfo.end_origin, GameFunc.plat_hit_bottom);

		return true;
	});

	private static readonly EntBlockedAdapter plat_blocked = new("plat_blocked", (self, other) =>
	{
		if (0 == (other.svflags & Defines.SVF_MONSTER) && null == other.client)
		{
			// give it a chance to go away on it's own terms (like gibs)
			GameCombat.T_Damage(other, self, self, Globals.vec3_origin, other.s.origin, Globals.vec3_origin, 100000, 1, 0, Defines.MOD_CRUSH);

			// if it's still there, nuke it
			if (other != null)
				GameMisc.BecomeExplosion1(other);

			return;
		}

		GameCombat.T_Damage(other, self, self, Globals.vec3_origin, other.s.origin, Globals.vec3_origin, self.dmg, 1, 0, Defines.MOD_CRUSH);

		if (self.moveinfo.state == GameFunc.STATE_UP)
			GameFunc.plat_go_down.think(self);
		else if (self.moveinfo.state == GameFunc.STATE_DOWN)
			GameFunc.plat_go_up(self);
	});

	private static readonly EntUseAdapter Use_Plat = new("use_plat", (ent, other, activator) =>
	{
		if (ent.think != null)
			return; // already down

		GameFunc.plat_go_down.think(ent);
	});

	private static readonly EntTouchAdapter Touch_Plat_Center = new("touch_plat_center", (ent, other, plane, surf) =>
	{
		if (other.client == null)
			return;

		if (other.health <= 0)
			return;

		ent = ent.enemy; // now point at the plat, not the trigger

		if (ent.moveinfo.state == GameFunc.STATE_BOTTOM)
			GameFunc.plat_go_up(ent);
		else if (ent.moveinfo.state == GameFunc.STATE_TOP)
		{
			ent.nextthink = GameBase.level.time + 1; // the player is still

			// on the plat, so
			// delay going down
		}
	});

	/**
     * QUAKED func_rotating (0 .5 .8) ? START_ON REVERSE X_AXIS Y_AXIS
     * TOUCH_PAIN STOP ANIMATED ANIMATED_FAST You need to have an origin brush
     * as part of this entity. The center of that brush will be the point around
     * which it is rotated. It will rotate around the Z axis by default. You can
     * check either the X_AXIS or Y_AXIS box to change that.
     * 
     * "speed" determines how fast it moves; default value is 100. "dmg" damage
     * to inflict when blocked (2 default)
     * 
     * REVERSE will cause the it to rotate in the opposite direction. STOP mean
     * it will stop moving instead of pushing entities
     */
	private static readonly EntBlockedAdapter rotating_blocked = new("rotating_blocked", (self, other) =>
	{
		GameCombat.T_Damage(other, self, self, Globals.vec3_origin, other.s.origin, Globals.vec3_origin, self.dmg, 1, 0, Defines.MOD_CRUSH);
	});

	private static readonly EntTouchAdapter rotating_touch = new("rotating_touch", (self, other, plane, surf) =>
	{
		if (self.avelocity[0] != 0 || self.avelocity[1] != 0 || self.avelocity[2] != 0)
			GameCombat.T_Damage(other, self, self, Globals.vec3_origin, other.s.origin, Globals.vec3_origin, self.dmg, 1, 0, Defines.MOD_CRUSH);
	});

	private static readonly EntUseAdapter rotating_use = new("rotating_use", (self, other, activator) =>
	{
		if (!Math3D.VectorEquals(self.avelocity, Globals.vec3_origin))
		{
			self.s.sound = 0;
			Math3D.VectorClear(self.avelocity);
			self.touch = null;
		}
		else
		{
			self.s.sound = self.moveinfo.sound_middle;
			Math3D.VectorScale(self.movedir, self.speed, self.avelocity);

			if ((self.spawnflags & 16) != 0)
				self.touch = GameFunc.rotating_touch;
		}
	});

	public static EntThinkAdapter SP_func_rotating = new("sp_func_rotating", ent =>
	{
		ent.solid = Defines.SOLID_BSP;

		if ((ent.spawnflags & 32) != 0)
			ent.movetype = Defines.MOVETYPE_STOP;
		else
			ent.movetype = Defines.MOVETYPE_PUSH;

		// set the axis of rotation
		Math3D.VectorClear(ent.movedir);

		if ((ent.spawnflags & 4) != 0)
			ent.movedir[2] = 1.0f;
		else if ((ent.spawnflags & 8) != 0)
			ent.movedir[0] = 1.0f;
		else

			// Z_AXIS
			ent.movedir[1] = 1.0f;

		// check for reverse rotation
		if ((ent.spawnflags & 2) != 0)
			Math3D.VectorNegate(ent.movedir, ent.movedir);

		if (0 == ent.speed)
			ent.speed = 100;

		if (0 == ent.dmg)
			ent.dmg = 2;

		//		ent.moveinfo.sound_middle = "doors/hydro1.wav";

		ent.use = GameFunc.rotating_use;

		if (ent.dmg != 0)
			ent.blocked = GameFunc.rotating_blocked;

		if ((ent.spawnflags & 1) != 0)
			ent.use.use(ent, null, null);

		if ((ent.spawnflags & 64) != 0)
			ent.s.effects |= Defines.EF_ANIM_ALL;

		if ((ent.spawnflags & 128) != 0)
			ent.s.effects |= Defines.EF_ANIM_ALLFAST;

		GameBase.gi.setmodel(ent, ent.model);
		GameBase.gi.linkentity(ent);

		return true;
	});

	/*
	 * ======================================================================
	 * 
	 * BUTTONS
	 * 
	 * ======================================================================
	 */
	/*
	 * QUAKED func_button (0 .5 .8) ? When a button is touched, it moves some
	 * distance in the direction of it's angle, triggers all of it's targets,
	 * waits some time, then returns to it's original position where it can be
	 * triggered again.
	 * 
	 * "angle" determines the opening direction "target" all entities with a
	 * matching targetname will be used "speed" override the default 40 speed
	 * "wait" override the default 1 second wait (-1 = never return) "lip"
	 * override the default 4 pixel lip remaining at end of move "health" if
	 * set, the button must be killed instead of touched "sounds" 1) silent 2)
	 * steam metal 3) wooden clunk 4) metallic click 5) in-out
	 */

	private static readonly EntThinkAdapter button_done = new("button_done", self =>
	{
		self.moveinfo.state = GameFunc.STATE_BOTTOM;
		self.s.effects &= ~Defines.EF_ANIM23;
		self.s.effects |= Defines.EF_ANIM01;

		return true;
	});

	private static readonly EntThinkAdapter button_return = new("button_return", self =>
	{
		self.moveinfo.state = GameFunc.STATE_DOWN;

		GameFunc.Move_Calc(self, self.moveinfo.start_origin, GameFunc.button_done);

		self.s.frame = 0;

		if (self.health != 0)
			self.takedamage = Defines.DAMAGE_YES;

		return true;
	});

	private static readonly EntThinkAdapter button_wait = new("button_wait", self =>
	{
		self.moveinfo.state = GameFunc.STATE_TOP;
		self.s.effects &= ~Defines.EF_ANIM01;
		self.s.effects |= Defines.EF_ANIM23;

		GameUtil.G_UseTargets(self, self.activator);
		self.s.frame = 1;

		if (self.moveinfo.wait >= 0)
		{
			self.nextthink = GameBase.level.time + self.moveinfo.wait;
			self.think = GameFunc.button_return;
		}

		return true;
	});

	private static readonly EntThinkAdapter button_fire = new("button_fire", self =>
	{
		if (self.moveinfo.state == GameFunc.STATE_UP || self.moveinfo.state == GameFunc.STATE_TOP)
			return true;

		self.moveinfo.state = GameFunc.STATE_UP;

		if (self.moveinfo.sound_start != 0 && 0 == (self.flags & Defines.FL_TEAMSLAVE))
			GameBase.gi.sound(self, Defines.CHAN_NO_PHS_ADD + Defines.CHAN_VOICE, self.moveinfo.sound_start, 1, Defines.ATTN_STATIC, 0);

		GameFunc.Move_Calc(self, self.moveinfo.end_origin, GameFunc.button_wait);

		return true;
	});

	private static readonly EntUseAdapter button_use = new("button_use", (self, other, activator) =>
	{
		self.activator = activator;
		GameFunc.button_fire.think(self);

		return;
	});

	private static readonly EntTouchAdapter button_touch = new("button_touch", (self, other, plane, surf) =>
	{
		if (null == other.client)
			return;

		if (other.health <= 0)
			return;

		self.activator = other;
		GameFunc.button_fire.think(self);
	});

	private static readonly EntDieAdapter button_killed = new("button_killed", (self, inflictor, attacker, damage, point) =>
	{
		self.activator = attacker;
		self.health = self.max_health;
		self.takedamage = Defines.DAMAGE_NO;
		GameFunc.button_fire.think(self);
	});

	public static EntThinkAdapter SP_func_button = new("sp_func_button", ent =>
	{
		float[] abs_movedir = { 0, 0, 0 };
		float dist;

		GameBase.G_SetMovedir(ent.s.angles, ent.movedir);
		ent.movetype = Defines.MOVETYPE_STOP;
		ent.solid = Defines.SOLID_BSP;
		GameBase.gi.setmodel(ent, ent.model);

		if (ent.sounds != 1)
			ent.moveinfo.sound_start = GameBase.gi.soundindex("switches/butn2.wav");

		if (0 == ent.speed)
			ent.speed = 40;

		if (0 == ent.accel)
			ent.accel = ent.speed;

		if (0 == ent.decel)
			ent.decel = ent.speed;

		if (0 == ent.wait)
			ent.wait = 3;

		if (0 == GameBase.st.lip)
			GameBase.st.lip = 4;

		Math3D.VectorCopy(ent.s.origin, ent.pos1);
		abs_movedir[0] = (float)Math.Abs(ent.movedir[0]);
		abs_movedir[1] = (float)Math.Abs(ent.movedir[1]);
		abs_movedir[2] = (float)Math.Abs(ent.movedir[2]);
		dist = abs_movedir[0] * ent.size[0] + abs_movedir[1] * ent.size[1] + abs_movedir[2] * ent.size[2] - GameBase.st.lip;
		Math3D.VectorMA(ent.pos1, dist, ent.movedir, ent.pos2);

		ent.use = GameFunc.button_use;
		ent.s.effects |= Defines.EF_ANIM01;

		if (ent.health != 0)
		{
			ent.max_health = ent.health;
			ent.die = GameFunc.button_killed;
			ent.takedamage = Defines.DAMAGE_YES;
		}
		else if (null == ent.targetname)
			ent.touch = GameFunc.button_touch;

		ent.moveinfo.state = GameFunc.STATE_BOTTOM;

		ent.moveinfo.speed = ent.speed;
		ent.moveinfo.accel = ent.accel;
		ent.moveinfo.decel = ent.decel;
		ent.moveinfo.wait = ent.wait;
		Math3D.VectorCopy(ent.pos1, ent.moveinfo.start_origin);
		Math3D.VectorCopy(ent.s.angles, ent.moveinfo.start_angles);
		Math3D.VectorCopy(ent.pos2, ent.moveinfo.end_origin);
		Math3D.VectorCopy(ent.s.angles, ent.moveinfo.end_angles);

		GameBase.gi.linkentity(ent);

		return true;
	});

	private static readonly EntThinkAdapter door_hit_top = new("door_hit_top", self =>
	{
		if (0 == (self.flags & Defines.FL_TEAMSLAVE))
		{
			if (self.moveinfo.sound_end != 0)
				GameBase.gi.sound(self, Defines.CHAN_NO_PHS_ADD + Defines.CHAN_VOICE, self.moveinfo.sound_end, 1, Defines.ATTN_STATIC, 0);

			self.s.sound = 0;
		}

		self.moveinfo.state = GameFunc.STATE_TOP;

		if ((self.spawnflags & GameFunc.DOOR_TOGGLE) != 0)
			return true;

		if (self.moveinfo.wait >= 0)
		{
			self.think = GameFunc.door_go_down;
			self.nextthink = GameBase.level.time + self.moveinfo.wait;
		}

		return true;
	});

	private static readonly EntThinkAdapter door_hit_bottom = new("door_hit_bottom", self =>
	{
		if (0 == (self.flags & Defines.FL_TEAMSLAVE))
		{
			if (self.moveinfo.sound_end != 0)
				GameBase.gi.sound(self, Defines.CHAN_NO_PHS_ADD + Defines.CHAN_VOICE, self.moveinfo.sound_end, 1, Defines.ATTN_STATIC, 0);

			self.s.sound = 0;
		}

		self.moveinfo.state = GameFunc.STATE_BOTTOM;
		GameFunc.door_use_areaportals(self, false);

		return true;
	});

	private static readonly EntThinkAdapter door_go_down = new("door_go_down", self =>
	{
		if (0 == (self.flags & Defines.FL_TEAMSLAVE))
		{
			if (self.moveinfo.sound_start != 0)
				GameBase.gi.sound(self, Defines.CHAN_NO_PHS_ADD + Defines.CHAN_VOICE, self.moveinfo.sound_start, 1, Defines.ATTN_STATIC, 0);

			self.s.sound = self.moveinfo.sound_middle;
		}

		if (self.max_health != 0)
		{
			self.takedamage = Defines.DAMAGE_YES;
			self.health = self.max_health;
		}

		self.moveinfo.state = GameFunc.STATE_DOWN;

		if (Lib.strcmp(self.classname, "func_door") == 0)
			GameFunc.Move_Calc(self, self.moveinfo.start_origin, GameFunc.door_hit_bottom);
		else if (Lib.strcmp(self.classname, "func_door_rotating") == 0)
			GameFunc.AngleMove_Calc(self, GameFunc.door_hit_bottom);

		return true;
	});

	private static readonly EntUseAdapter door_use = new("door_use", (self, other, activator) =>
	{
		edict_t ent;

		if ((self.flags & Defines.FL_TEAMSLAVE) != 0)
			return;

		if ((self.spawnflags & GameFunc.DOOR_TOGGLE) != 0)
		{
			if (self.moveinfo.state == GameFunc.STATE_UP || self.moveinfo.state == GameFunc.STATE_TOP)
			{
				// trigger all paired doors
				for (ent = self; ent != null; ent = ent.teamchain)
				{
					ent.message = null;
					ent.touch = null;
					GameFunc.door_go_down.think(ent);
				}

				return;
			}
		}

		// trigger all paired doors
		for (ent = self; ent != null; ent = ent.teamchain)
		{
			ent.message = null;
			ent.touch = null;
			GameFunc.door_go_up(ent, activator);
		}
	});

	private static readonly EntTouchAdapter Touch_DoorTrigger = new("touch_door_trigger", (self, other, plane, surf) =>
	{
		if (other.health <= 0)
			return;

		if (0 == (other.svflags & Defines.SVF_MONSTER) && null == other.client)
			return;

		if (0 != (self.owner.spawnflags & GameFunc.DOOR_NOMONSTER) && 0 != (other.svflags & Defines.SVF_MONSTER))
			return;

		if (GameBase.level.time < self.touch_debounce_time)
			return;

		self.touch_debounce_time = GameBase.level.time + 1.0f;

		GameFunc.door_use.use(self.owner, other, other);
	});

	private static readonly EntThinkAdapter Think_CalcMoveSpeed = new("think_calc_movespeed", self =>
	{
		edict_t ent;
		float min;
		float time;
		float newspeed;
		float ratio;
		float dist;

		if ((self.flags & Defines.FL_TEAMSLAVE) != 0)
			return true; // only the team master does this

		// find the smallest distance any member of the team will be moving
		min = Math.Abs(self.moveinfo.distance);

		for (ent = self.teamchain; ent != null; ent = ent.teamchain)
		{
			dist = Math.Abs(ent.moveinfo.distance);

			if (dist < min)
				min = dist;
		}

		time = min / self.moveinfo.speed;

		// adjust speeds so they will all complete at the same time
		for (ent = self; ent != null; ent = ent.teamchain)
		{
			newspeed = Math.Abs(ent.moveinfo.distance) / time;
			ratio = newspeed / ent.moveinfo.speed;

			if (ent.moveinfo.accel == ent.moveinfo.speed)
				ent.moveinfo.accel = newspeed;
			else
				ent.moveinfo.accel *= ratio;

			if (ent.moveinfo.decel == ent.moveinfo.speed)
				ent.moveinfo.decel = newspeed;
			else
				ent.moveinfo.decel *= ratio;

			ent.moveinfo.speed = newspeed;
		}

		return true;
	});

	private static readonly EntThinkAdapter Think_SpawnDoorTrigger = new("think_spawn_door_trigger", ent =>
	{
		edict_t other;
		float[] mins = { 0, 0, 0 }, maxs = { 0, 0, 0 };

		if ((ent.flags & Defines.FL_TEAMSLAVE) != 0)
			return true; // only the team leader spawns a trigger

		Math3D.VectorCopy(ent.absmin, mins);
		Math3D.VectorCopy(ent.absmax, maxs);

		for (other = ent.teamchain; other != null; other = other.teamchain)
		{
			GameBase.AddPointToBounds(other.absmin, mins, maxs);
			GameBase.AddPointToBounds(other.absmax, mins, maxs);
		}

		// expand
		mins[0] -= 60;
		mins[1] -= 60;
		maxs[0] += 60;
		maxs[1] += 60;

		other = GameUtil.G_Spawn();
		Math3D.VectorCopy(mins, other.mins);
		Math3D.VectorCopy(maxs, other.maxs);
		other.owner = ent;
		other.solid = Defines.SOLID_TRIGGER;
		other.movetype = Defines.MOVETYPE_NONE;
		other.touch = GameFunc.Touch_DoorTrigger;
		GameBase.gi.linkentity(other);

		if ((ent.spawnflags & GameFunc.DOOR_START_OPEN) != 0)
			GameFunc.door_use_areaportals(ent, true);

		GameFunc.Think_CalcMoveSpeed.think(ent);

		return true;
	});

	private static readonly EntBlockedAdapter door_blocked = new("door_blocked", (self, other) =>
	{
		edict_t ent;

		if (0 == (other.svflags & Defines.SVF_MONSTER) && null == other.client)
		{
			// give it a chance to go away on it's own terms (like gibs)
			GameCombat.T_Damage(other, self, self, Globals.vec3_origin, other.s.origin, Globals.vec3_origin, 100000, 1, 0, Defines.MOD_CRUSH);

			// if it's still there, nuke it
			if (other != null)
				GameMisc.BecomeExplosion1(other);

			return;
		}

		GameCombat.T_Damage(other, self, self, Globals.vec3_origin, other.s.origin, Globals.vec3_origin, self.dmg, 1, 0, Defines.MOD_CRUSH);

		if ((self.spawnflags & GameFunc.DOOR_CRUSHER) != 0)
			return;

		// if a door has a negative wait, it would never come back if
		// blocked,
		// so let it just squash the object to death real fast
		if (self.moveinfo.wait >= 0)
		{
			if (self.moveinfo.state == GameFunc.STATE_DOWN)
			{
				for (ent = self.teammaster; ent != null; ent = ent.teamchain)
					GameFunc.door_go_up(ent, ent.activator);
			}
			else
			{
				for (ent = self.teammaster; ent != null; ent = ent.teamchain)
					GameFunc.door_go_down.think(ent);
			}
		}
	});

	private static readonly EntDieAdapter door_killed = new("door_killed", (self, inflictor, attacker, damage, point) =>
	{
		edict_t ent;

		for (ent = self.teammaster; ent != null; ent = ent.teamchain)
		{
			ent.health = ent.max_health;
			ent.takedamage = Defines.DAMAGE_NO;
		}

		GameFunc.door_use.use(self.teammaster, attacker, attacker);
	});

	private static readonly EntTouchAdapter door_touch = new("door_touch", (self, other, plane, surf) =>
	{
		if (null == other.client)
			return;

		if (GameBase.level.time < self.touch_debounce_time)
			return;

		self.touch_debounce_time = GameBase.level.time + 5.0f;

		GameBase.gi.centerprintf(other, self.message);
		GameBase.gi.sound(other, Defines.CHAN_AUTO, GameBase.gi.soundindex("misc/talk1.wav"), 1, Defines.ATTN_NORM, 0);
	});

	public static EntThinkAdapter SP_func_door = new("sp_func_door", ent =>
	{
		float[] abs_movedir = { 0, 0, 0 };

		if (ent.sounds != 1)
		{
			ent.moveinfo.sound_start = GameBase.gi.soundindex("doors/dr1_strt.wav");
			ent.moveinfo.sound_middle = GameBase.gi.soundindex("doors/dr1_mid.wav");
			ent.moveinfo.sound_end = GameBase.gi.soundindex("doors/dr1_end.wav");
		}

		GameBase.G_SetMovedir(ent.s.angles, ent.movedir);
		ent.movetype = Defines.MOVETYPE_PUSH;
		ent.solid = Defines.SOLID_BSP;
		GameBase.gi.setmodel(ent, ent.model);

		ent.blocked = GameFunc.door_blocked;
		ent.use = GameFunc.door_use;

		if (0 == ent.speed)
			ent.speed = 100;

		if (GameBase.deathmatch.value != 0)
			ent.speed *= 2;

		if (0 == ent.accel)
			ent.accel = ent.speed;

		if (0 == ent.decel)
			ent.decel = ent.speed;

		if (0 == ent.wait)
			ent.wait = 3;

		if (0 == GameBase.st.lip)
			GameBase.st.lip = 8;

		if (0 == ent.dmg)
			ent.dmg = 2;

		// calculate second position
		Math3D.VectorCopy(ent.s.origin, ent.pos1);
		abs_movedir[0] = Math.Abs(ent.movedir[0]);
		abs_movedir[1] = Math.Abs(ent.movedir[1]);
		abs_movedir[2] = Math.Abs(ent.movedir[2]);
		ent.moveinfo.distance = abs_movedir[0] * ent.size[0] + abs_movedir[1] * ent.size[1] + abs_movedir[2] * ent.size[2] - GameBase.st.lip;

		Math3D.VectorMA(ent.pos1, ent.moveinfo.distance, ent.movedir, ent.pos2);

		// if it starts open, switch the positions
		if ((ent.spawnflags & GameFunc.DOOR_START_OPEN) != 0)
		{
			Math3D.VectorCopy(ent.pos2, ent.s.origin);
			Math3D.VectorCopy(ent.pos1, ent.pos2);
			Math3D.VectorCopy(ent.s.origin, ent.pos1);
		}

		ent.moveinfo.state = GameFunc.STATE_BOTTOM;

		if (ent.health != 0)
		{
			ent.takedamage = Defines.DAMAGE_YES;
			ent.die = GameFunc.door_killed;
			ent.max_health = ent.health;
		}
		else if (ent.targetname != null && ent.message != null)
		{
			GameBase.gi.soundindex("misc/talk.wav");
			ent.touch = GameFunc.door_touch;
		}

		ent.moveinfo.speed = ent.speed;
		ent.moveinfo.accel = ent.accel;
		ent.moveinfo.decel = ent.decel;
		ent.moveinfo.wait = ent.wait;
		Math3D.VectorCopy(ent.pos1, ent.moveinfo.start_origin);
		Math3D.VectorCopy(ent.s.angles, ent.moveinfo.start_angles);
		Math3D.VectorCopy(ent.pos2, ent.moveinfo.end_origin);
		Math3D.VectorCopy(ent.s.angles, ent.moveinfo.end_angles);

		if ((ent.spawnflags & 16) != 0)
			ent.s.effects |= Defines.EF_ANIM_ALL;

		if ((ent.spawnflags & 64) != 0)
			ent.s.effects |= Defines.EF_ANIM_ALLFAST;

		// to simplify logic elsewhere, make non-teamed doors into a team of
		// one
		if (null == ent.team)
			ent.teammaster = ent;

		GameBase.gi.linkentity(ent);

		ent.nextthink = GameBase.level.time + Defines.FRAMETIME;

		if (ent.health != 0 || ent.targetname != null)
			ent.think = GameFunc.Think_CalcMoveSpeed;
		else
			ent.think = GameFunc.Think_SpawnDoorTrigger;

		return true;
	});

	/*
	 * QUAKED func_door_rotating (0 .5 .8) ? START_OPEN REVERSE CRUSHER
	 * NOMONSTER ANIMATED TOGGLE X_AXIS Y_AXIS TOGGLE causes the door to wait in
	 * both the start and end states for a trigger event.
	 * 
	 * START_OPEN the door to moves to its destination when spawned, and operate
	 * in reverse. It is used to temporarily or permanently close off an area
	 * when triggered (not useful for touch or takedamage doors). NOMONSTER
	 * monsters will not trigger this door
	 * 
	 * You need to have an origin brush as part of this entity. The center of
	 * that brush will be the point around which it is rotated. It will rotate
	 * around the Z axis by default. You can check either the X_AXIS or Y_AXIS
	 * box to change that.
	 * 
	 * "distance" is how many degrees the door will be rotated. "speed"
	 * determines how fast the door moves; default value is 100.
	 * 
	 * REVERSE will cause the door to rotate in the opposite direction.
	 * 
	 * "message" is printed when the door is touched if it is a trigger door and
	 * it hasn't been fired yet "angle" determines the opening direction
	 * "targetname" if set, no touch field will be spawned and a remote button
	 * or trigger field activates the door. "health" if set, door must be shot
	 * open "speed" movement speed (100 default) "wait" wait before returning (3
	 * default, -1 = never return) "dmg" damage to inflict when blocked (2
	 * default) "sounds" 1) silent 2) light 3) medium 4) heavy
	 */

	public static EntThinkAdapter SP_func_door_rotating = new("sp_func_door_rotating", ent =>
	{
		Math3D.VectorClear(ent.s.angles);

		// set the axis of rotation
		Math3D.VectorClear(ent.movedir);

		if ((ent.spawnflags & GameFunc.DOOR_X_AXIS) != 0)
			ent.movedir[2] = 1.0f;
		else if ((ent.spawnflags & GameFunc.DOOR_Y_AXIS) != 0)
			ent.movedir[0] = 1.0f;
		else

			// Z_AXIS
			ent.movedir[1] = 1.0f;

		// check for reverse rotation
		if ((ent.spawnflags & GameFunc.DOOR_REVERSE) != 0)
			Math3D.VectorNegate(ent.movedir, ent.movedir);

		if (0 == GameBase.st.distance)
		{
			GameBase.gi.dprintf(ent.classname + " at " + Lib.vtos(ent.s.origin) + " with no distance set\n");
			GameBase.st.distance = 90;
		}

		Math3D.VectorCopy(ent.s.angles, ent.pos1);
		Math3D.VectorMA(ent.s.angles, GameBase.st.distance, ent.movedir, ent.pos2);
		ent.moveinfo.distance = GameBase.st.distance;

		ent.movetype = Defines.MOVETYPE_PUSH;
		ent.solid = Defines.SOLID_BSP;
		GameBase.gi.setmodel(ent, ent.model);

		ent.blocked = GameFunc.door_blocked;
		ent.use = GameFunc.door_use;

		if (0 == ent.speed)
			ent.speed = 100;

		if (0 == ent.accel)
			ent.accel = ent.speed;

		if (0 == ent.decel)
			ent.decel = ent.speed;

		if (0 == ent.wait)
			ent.wait = 3;

		if (0 == ent.dmg)
			ent.dmg = 2;

		if (ent.sounds != 1)
		{
			ent.moveinfo.sound_start = GameBase.gi.soundindex("doors/dr1_strt.wav");
			ent.moveinfo.sound_middle = GameBase.gi.soundindex("doors/dr1_mid.wav");
			ent.moveinfo.sound_end = GameBase.gi.soundindex("doors/dr1_end.wav");
		}

		// if it starts open, switch the positions
		if ((ent.spawnflags & GameFunc.DOOR_START_OPEN) != 0)
		{
			Math3D.VectorCopy(ent.pos2, ent.s.angles);
			Math3D.VectorCopy(ent.pos1, ent.pos2);
			Math3D.VectorCopy(ent.s.angles, ent.pos1);
			Math3D.VectorNegate(ent.movedir, ent.movedir);
		}

		if (ent.health != 0)
		{
			ent.takedamage = Defines.DAMAGE_YES;
			ent.die = GameFunc.door_killed;
			ent.max_health = ent.health;
		}

		if (ent.targetname != null && ent.message != null)
		{
			GameBase.gi.soundindex("misc/talk.wav");
			ent.touch = GameFunc.door_touch;
		}

		ent.moveinfo.state = GameFunc.STATE_BOTTOM;
		ent.moveinfo.speed = ent.speed;
		ent.moveinfo.accel = ent.accel;
		ent.moveinfo.decel = ent.decel;
		ent.moveinfo.wait = ent.wait;
		Math3D.VectorCopy(ent.s.origin, ent.moveinfo.start_origin);
		Math3D.VectorCopy(ent.pos1, ent.moveinfo.start_angles);
		Math3D.VectorCopy(ent.s.origin, ent.moveinfo.end_origin);
		Math3D.VectorCopy(ent.pos2, ent.moveinfo.end_angles);

		if ((ent.spawnflags & 16) != 0)
			ent.s.effects |= Defines.EF_ANIM_ALL;

		// to simplify logic elsewhere, make non-teamed doors into a team of
		// one
		if (ent.team == null)
			ent.teammaster = ent;

		GameBase.gi.linkentity(ent);

		ent.nextthink = GameBase.level.time + Defines.FRAMETIME;

		if (ent.health != 0 || ent.targetname != null)
			ent.think = GameFunc.Think_CalcMoveSpeed;
		else
			ent.think = GameFunc.Think_SpawnDoorTrigger;

		return true;
	});

	public static readonly int TRAIN_START_ON = 1;
	public static readonly int TRAIN_TOGGLE = 2;

	public static readonly int TRAIN_BLOCK_STOPS = 4;
	/*
	 * QUAKED func_train (0 .5 .8) ? START_ON TOGGLE BLOCK_STOPS Trains are
	 * moving platforms that players can ride. The targets origin specifies the
	 * min point of the train at each corner. The train spawns at the first
	 * target it is pointing at. If the train is the target of a button or
	 * trigger, it will not begin moving until activated. speed default 100 dmg
	 * default 2 noise looping sound to play when the train is in motion
	 *  
	 */

	private static readonly EntBlockedAdapter train_blocked = new("train_blocked", (self, other) =>
	{
		if (0 == (other.svflags & Defines.SVF_MONSTER) && null == other.client)
		{
			// give it a chance to go away on it's own terms (like gibs)
			GameCombat.T_Damage(other, self, self, Globals.vec3_origin, other.s.origin, Globals.vec3_origin, 100000, 1, 0, Defines.MOD_CRUSH);

			// if it's still there, nuke it
			if (other != null)
				GameMisc.BecomeExplosion1(other);

			return;
		}

		if (GameBase.level.time < self.touch_debounce_time)
			return;

		if (self.dmg == 0)
			return;

		self.touch_debounce_time = GameBase.level.time + 0.5f;
		GameCombat.T_Damage(other, self, self, Globals.vec3_origin, other.s.origin, Globals.vec3_origin, self.dmg, 1, 0, Defines.MOD_CRUSH);
	});

	private static readonly EntThinkAdapter train_wait = new("train_wait", self =>
	{
		if (self.target_ent.pathtarget != null)
		{
			string savetarget;
			edict_t ent;

			ent = self.target_ent;
			savetarget = ent.target;
			ent.target = ent.pathtarget;
			GameUtil.G_UseTargets(ent, self.activator);
			ent.target = savetarget;

			// make sure we didn't get killed by a killtarget
			if (!self.inuse)
				return true;
		}

		if (self.moveinfo.wait != 0)
		{
			if (self.moveinfo.wait > 0)
			{
				self.nextthink = GameBase.level.time + self.moveinfo.wait;
				self.think = GameFunc.train_next;
			}
			else if (0 != (self.spawnflags & GameFunc.TRAIN_TOGGLE)) // && wait < 0
			{
				GameFunc.train_next.think(self);
				self.spawnflags &= ~GameFunc.TRAIN_START_ON;
				Math3D.VectorClear(self.velocity);
				self.nextthink = 0;
			}

			if (0 == (self.flags & Defines.FL_TEAMSLAVE))
			{
				if (self.moveinfo.sound_end != 0)
					GameBase.gi.sound(self, Defines.CHAN_NO_PHS_ADD + Defines.CHAN_VOICE, self.moveinfo.sound_end, 1, Defines.ATTN_STATIC, 0);

				self.s.sound = 0;
			}
		}
		else
			GameFunc.train_next.think(self);

		return true;
	});

	private static readonly EntThinkAdapter train_next = new("train_next", self =>
	{
		edict_t ent = null;
		float[] dest = { 0, 0, 0 };
		bool first;

		first = true;

		var dogoto = true;

		while (dogoto)
		{
			if (null == self.target)
			{
				//			gi.dprintf ("train_next: no next target\n");
				return true;
			}

			ent = GameBase.G_PickTarget(self.target);

			if (null == ent)
			{
				GameBase.gi.dprintf("train_next: bad target " + self.target + "\n");

				return true;
			}

			self.target = ent.target;
			dogoto = false;

			// check for a teleport path_corner
			if ((ent.spawnflags & 1) != 0)
			{
				if (!first)
				{
					GameBase.gi.dprintf("connected teleport path_corners, see " + ent.classname + " at " + Lib.vtos(ent.s.origin) + "\n");

					return true;
				}

				first = false;
				Math3D.VectorSubtract(ent.s.origin, self.mins, self.s.origin);
				Math3D.VectorCopy(self.s.origin, self.s.old_origin);
				self.s.@event = Defines.EV_OTHER_TELEPORT;
				GameBase.gi.linkentity(self);
				dogoto = true;
			}
		}

		self.moveinfo.wait = ent.wait;
		self.target_ent = ent;

		if (0 == (self.flags & Defines.FL_TEAMSLAVE))
		{
			if (self.moveinfo.sound_start != 0)
				GameBase.gi.sound(self, Defines.CHAN_NO_PHS_ADD + Defines.CHAN_VOICE, self.moveinfo.sound_start, 1, Defines.ATTN_STATIC, 0);

			self.s.sound = self.moveinfo.sound_middle;
		}

		Math3D.VectorSubtract(ent.s.origin, self.mins, dest);
		self.moveinfo.state = GameFunc.STATE_TOP;
		Math3D.VectorCopy(self.s.origin, self.moveinfo.start_origin);
		Math3D.VectorCopy(dest, self.moveinfo.end_origin);
		GameFunc.Move_Calc(self, dest, GameFunc.train_wait);
		self.spawnflags |= GameFunc.TRAIN_START_ON;

		return true;
	});

	public static EntThinkAdapter func_train_find = new("func_train_find", self =>
	{
		edict_t ent;

		if (null == self.target)
		{
			GameBase.gi.dprintf("train_find: no target\n");

			return true;
		}

		ent = GameBase.G_PickTarget(self.target);

		if (null == ent)
		{
			GameBase.gi.dprintf("train_find: target " + self.target + " not found\n");

			return true;
		}

		self.target = ent.target;

		Math3D.VectorSubtract(ent.s.origin, self.mins, self.s.origin);
		GameBase.gi.linkentity(self);

		// if not triggered, start immediately
		if (null == self.targetname)
			self.spawnflags |= GameFunc.TRAIN_START_ON;

		if ((self.spawnflags & GameFunc.TRAIN_START_ON) != 0)
		{
			self.nextthink = GameBase.level.time + Defines.FRAMETIME;
			self.think = GameFunc.train_next;
			self.activator = self;
		}

		return true;
	});

	public static EntUseAdapter train_use = new("train_use", (self, other, activator) =>
	{
		self.activator = activator;

		if ((self.spawnflags & GameFunc.TRAIN_START_ON) != 0)
		{
			if (0 == (self.spawnflags & GameFunc.TRAIN_TOGGLE))
				return;

			self.spawnflags &= ~GameFunc.TRAIN_START_ON;
			Math3D.VectorClear(self.velocity);
			self.nextthink = 0;
		}
		else
		{
			if (self.target_ent != null)
				GameFunc.train_resume(self);
			else
				GameFunc.train_next.think(self);
		}
	});

	/*
	 * QUAKED trigger_elevator (0.3 0.1 0.6) (-8 -8 -8) (8 8 8)
	 */
	private static readonly EntUseAdapter trigger_elevator_use = new("trigger_elevator_use", (self, other, activator) =>
	{
		edict_t target;

		if (0 != self.movetarget.nextthink)
		{
			//			gi.dprintf("elevator busy\n");
			return;
		}

		if (null == other.pathtarget)
		{
			GameBase.gi.dprintf("elevator used with no pathtarget\n");

			return;
		}

		target = GameBase.G_PickTarget(other.pathtarget);

		if (null == target)
		{
			GameBase.gi.dprintf("elevator used with bad pathtarget: " + other.pathtarget + "\n");

			return;
		}

		self.movetarget.target_ent = target;
		GameFunc.train_resume(self.movetarget);
	});

	private static readonly EntThinkAdapter trigger_elevator_init = new("trigger_elevator_init", self =>
	{
		if (null == self.target)
		{
			GameBase.gi.dprintf("trigger_elevator has no target\n");

			return true;
		}

		self.movetarget = GameBase.G_PickTarget(self.target);

		if (null == self.movetarget)
		{
			GameBase.gi.dprintf("trigger_elevator unable to find target " + self.target + "\n");

			return true;
		}

		if (Lib.strcmp(self.movetarget.classname, "func_train") != 0)
		{
			GameBase.gi.dprintf("trigger_elevator target " + self.target + " is not a train\n");

			return true;
		}

		self.use = GameFunc.trigger_elevator_use;
		self.svflags = Defines.SVF_NOCLIENT;

		return true;
	});

	public static EntThinkAdapter SP_trigger_elevator = new("sp_trigger_elevator", self =>
	{
		self.think = GameFunc.trigger_elevator_init;
		self.nextthink = GameBase.level.time + Defines.FRAMETIME;

		return true;
	});

	/*
	 * QUAKED func_timer (0.3 0.1 0.6) (-8 -8 -8) (8 8 8) START_ON "wait" base
	 * time between triggering all targets, default is 1 "random" wait variance,
	 * default is 0
	 * 
	 * so, the basic time between firing is a random time between (wait -
	 * random) and (wait + random)
	 * 
	 * "delay" delay before first firing when turned on, default is 0
	 * 
	 * "pausetime" additional delay used only the very first time and only if
	 * spawned with START_ON
	 * 
	 * These can used but not touched.
	 */

	private static readonly EntThinkAdapter func_timer_think = new("func_timer_think", self =>
	{
		GameUtil.G_UseTargets(self, self.activator);
		self.nextthink = GameBase.level.time + self.wait + Lib.crandom() * self.random;

		return true;
	});

	private static readonly EntUseAdapter func_timer_use = new("func_timer_use", (self, other, activator) =>
	{
		self.activator = activator;

		// if on, turn it off
		if (self.nextthink != 0)
		{
			self.nextthink = 0;

			return;
		}

		// turn it on
		if (self.delay != 0)
			self.nextthink = GameBase.level.time + self.delay;
		else
			GameFunc.func_timer_think.think(self);
	});

	/*
	 * QUAKED func_conveyor (0 .5 .8) ? START_ON TOGGLE Conveyors are stationary
	 * brushes that move what's on them. The brush should be have a surface with
	 * at least one current content enabled. speed default 100
	 */

	private static readonly EntUseAdapter func_conveyor_use = new("func_conveyor_use", (self, other, activator) =>
	{
		if ((self.spawnflags & 1) != 0)
		{
			self.speed = 0;
			self.spawnflags &= ~1;
		}
		else
		{
			self.speed = self.count;
			self.spawnflags |= 1;
		}

		if (0 == (self.spawnflags & 2))
			self.count = 0;
	});

	public static EntThinkAdapter SP_func_conveyor = new("sp_func_conveyor", self =>
	{
		if (0 == self.speed)
			self.speed = 100;

		if (0 == (self.spawnflags & 1))
		{
			self.count = (int)self.speed;
			self.speed = 0;
		}

		self.use = GameFunc.func_conveyor_use;

		GameBase.gi.setmodel(self, self.model);
		self.solid = Defines.SOLID_BSP;
		GameBase.gi.linkentity(self);

		return true;
	});

	/*
	 * QUAKED func_door_secret (0 .5 .8) ? always_shoot 1st_left 1st_down A
	 * secret door. Slide back and then to the side.
	 * 
	 * open_once doors never closes 1st_left 1st move is left of arrow 1st_down
	 * 1st move is down from arrow always_shoot door is shootebale even if
	 * targeted
	 * 
	 * "angle" determines the direction "dmg" damage to inflic when blocked
	 * (default 2) "wait" how long to hold in the open position (default 5, -1
	 * means hold)
	 */
	public static readonly int SECRET_ALWAYS_SHOOT = 1;
	public static readonly int SECRET_1ST_LEFT = 2;
	public static readonly int SECRET_1ST_DOWN = 4;

	private static readonly EntUseAdapter door_secret_use = new("door_secret_use", (self, other, activator) =>
	{
		// make sure we're not already moving
		if (!Math3D.VectorEquals(self.s.origin, Globals.vec3_origin))
			return;

		GameFunc.Move_Calc(self, self.pos1, GameFunc.door_secret_move1);
		GameFunc.door_use_areaportals(self, true);
	});

	private static readonly EntThinkAdapter door_secret_move1 = new("door_secret_move1", self =>
	{
		self.nextthink = GameBase.level.time + 1.0f;
		self.think = GameFunc.door_secret_move2;

		return true;
	});

	private static readonly EntThinkAdapter door_secret_move2 = new("door_secret_move2", self =>
	{
		GameFunc.Move_Calc(self, self.pos2, GameFunc.door_secret_move3);

		return true;
	});

	private static readonly EntThinkAdapter door_secret_move3 = new("door_secret_move3", self =>
	{
		if (self.wait == -1)
			return true;

		self.nextthink = GameBase.level.time + self.wait;
		self.think = GameFunc.door_secret_move4;

		return true;
	});

	private static readonly EntThinkAdapter door_secret_move4 = new("door_secret_move4", self =>
	{
		GameFunc.Move_Calc(self, self.pos1, GameFunc.door_secret_move5);

		return true;
	});

	private static readonly EntThinkAdapter door_secret_move5 = new("door_secret_move5", self =>
	{
		self.nextthink = GameBase.level.time + 1.0f;
		self.think = GameFunc.door_secret_move6;

		return true;
	});

	private static readonly EntThinkAdapter door_secret_move6 = new("door_secret_move6", self =>
	{
		GameFunc.Move_Calc(self, Globals.vec3_origin, GameFunc.door_secret_done);

		return true;
	});

	private static readonly EntThinkAdapter door_secret_done = new("door_secret_move7", self =>
	{
		if (null == self.targetname || 0 != (self.spawnflags & GameFunc.SECRET_ALWAYS_SHOOT))
		{
			self.health = 0;
			self.takedamage = Defines.DAMAGE_YES;
		}

		GameFunc.door_use_areaportals(self, false);

		return true;
	});

	private static readonly EntBlockedAdapter door_secret_blocked = new("door_secret_blocked", (self, other) =>
	{
		if (0 == (other.svflags & Defines.SVF_MONSTER) && null == other.client)
		{
			// give it a chance to go away on it's own terms (like gibs)
			GameCombat.T_Damage(other, self, self, Globals.vec3_origin, other.s.origin, Globals.vec3_origin, 100000, 1, 0, Defines.MOD_CRUSH);

			// if it's still there, nuke it
			if (other != null)
				GameMisc.BecomeExplosion1(other);

			return;
		}

		if (GameBase.level.time < self.touch_debounce_time)
			return;

		self.touch_debounce_time = GameBase.level.time + 0.5f;

		GameCombat.T_Damage(other, self, self, Globals.vec3_origin, other.s.origin, Globals.vec3_origin, self.dmg, 1, 0, Defines.MOD_CRUSH);
	});

	private static readonly EntDieAdapter door_secret_die = new("door_secret_die", (self, inflictor, attacker, damage, point) =>
	{
		self.takedamage = Defines.DAMAGE_NO;
		GameFunc.door_secret_use.use(self, attacker, attacker);
	});

	public static EntThinkAdapter SP_func_door_secret = new("sp_func_door_secret", ent =>
	{
		float[] forward = { 0, 0, 0 }, right = { 0, 0, 0 }, up = { 0, 0, 0 };
		float side;
		float width;
		float length;

		ent.moveinfo.sound_start = GameBase.gi.soundindex("doors/dr1_strt.wav");
		ent.moveinfo.sound_middle = GameBase.gi.soundindex("doors/dr1_mid.wav");
		ent.moveinfo.sound_end = GameBase.gi.soundindex("doors/dr1_end.wav");

		ent.movetype = Defines.MOVETYPE_PUSH;
		ent.solid = Defines.SOLID_BSP;
		GameBase.gi.setmodel(ent, ent.model);

		ent.blocked = GameFunc.door_secret_blocked;
		ent.use = GameFunc.door_secret_use;

		if (null == ent.targetname || 0 != (ent.spawnflags & GameFunc.SECRET_ALWAYS_SHOOT))
		{
			ent.health = 0;
			ent.takedamage = Defines.DAMAGE_YES;
			ent.die = GameFunc.door_secret_die;
		}

		if (0 == ent.dmg)
			ent.dmg = 2;

		if (0 == ent.wait)
			ent.wait = 5;

		ent.moveinfo.accel = ent.moveinfo.decel = ent.moveinfo.speed = 50;

		// calculate positions
		Math3D.AngleVectors(ent.s.angles, forward, right, up);
		Math3D.VectorClear(ent.s.angles);
		side = 1.0f - (ent.spawnflags & GameFunc.SECRET_1ST_LEFT);

		if ((ent.spawnflags & GameFunc.SECRET_1ST_DOWN) != 0)
			width = Math.Abs(Math3D.DotProduct(up, ent.size));
		else
			width = Math.Abs(Math3D.DotProduct(right, ent.size));

		length = Math.Abs(Math3D.DotProduct(forward, ent.size));

		if ((ent.spawnflags & GameFunc.SECRET_1ST_DOWN) != 0)
			Math3D.VectorMA(ent.s.origin, -1 * width, up, ent.pos1);
		else
			Math3D.VectorMA(ent.s.origin, side * width, right, ent.pos1);

		Math3D.VectorMA(ent.pos1, length, forward, ent.pos2);

		if (ent.health != 0)
		{
			ent.takedamage = Defines.DAMAGE_YES;
			ent.die = GameFunc.door_killed;
			ent.max_health = ent.health;
		}
		else if (ent.targetname != null && ent.message != null)
		{
			GameBase.gi.soundindex("misc/talk.wav");
			ent.touch = GameFunc.door_touch;
		}

		ent.classname = "func_door";

		GameBase.gi.linkentity(ent);

		return true;
	});

	/**
     * QUAKED func_killbox (1 0 0) ? Kills everything inside when fired,
     * irrespective of protection.
     */
	private static readonly EntUseAdapter use_killbox = new("use_killbox", (self, other, activator) => { GameUtil.KillBox(self); });

	public static EntThinkAdapter SP_func_killbox = new("sp_func_killbox", ent =>
	{
		GameBase.gi.setmodel(ent, ent.model);
		ent.use = GameFunc.use_killbox;
		ent.svflags = Defines.SVF_NOCLIENT;

		return true;
	});
}