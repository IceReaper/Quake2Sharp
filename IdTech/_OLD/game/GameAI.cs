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

using Quake2Sharp.client;
using Quake2Sharp.game.adapters;
using Quake2Sharp.game.types;
using Quake2Sharp.util;

namespace Quake2Sharp.game;

public class GameAI
{
	public static void AttackFinished(edict_t self, float time)
	{
		self.monsterinfo.attack_finished = GameBase.level.time + time;
	}

	/**
     * Checks, if the monster should turn left/right.
     */
	public static bool FacingIdeal(edict_t self)
	{
		float delta;

		delta = Math3D.anglemod(self.s.angles[Defines.YAW] - self.ideal_yaw);

		if (delta > 45 && delta < 315)
			return false;

		return true;
	}

	/**
     * Turn and close until within an angle to launch a melee attack.
     */
	public static void ai_run_melee(edict_t self)
	{
		self.ideal_yaw = GameAI.enemy_yaw;
		M.M_ChangeYaw(self);

		if (GameAI.FacingIdeal(self))
		{
			self.monsterinfo.melee.think(self);
			self.monsterinfo.attack_state = Defines.AS_STRAIGHT;
		}
	}

	/**
     * Turn in place until within an angle to launch a missile attack.
     */
	public static void ai_run_missile(edict_t self)
	{
		self.ideal_yaw = GameAI.enemy_yaw;
		M.M_ChangeYaw(self);

		if (GameAI.FacingIdeal(self))
		{
			self.monsterinfo.attack.think(self);
			self.monsterinfo.attack_state = Defines.AS_STRAIGHT;
		}
	}

	/**
     * Strafe sideways, but stay at aproximately the same range.
     */
	public static void ai_run_slide(edict_t self, float distance)
	{
		float ofs;

		self.ideal_yaw = GameAI.enemy_yaw;
		M.M_ChangeYaw(self);

		if (self.monsterinfo.lefty != 0)
			ofs = 90;
		else
			ofs = -90;

		if (M.M_walkmove(self, self.ideal_yaw + ofs, distance))
			return;

		self.monsterinfo.lefty = 1 - self.monsterinfo.lefty;
		M.M_walkmove(self, self.ideal_yaw - ofs, distance);
	}

	/**
     * Decides if we're going to attack or do something else used by ai_run and
     * ai_stand.
     *
     * .enemy Will be world if not currently angry at anyone.
     *
     * .movetarget The next path spot to walk toward. If .enemy, ignore
     * .movetarget. When an enemy is killed, the monster will try to return to
     * it's path.
     *
     * .hunt_time Set to time + something when the player is in sight, but
     * movement straight for him is blocked. This causes the monster to use wall
     * following code for movement direction instead of sighting on the player.
     *
     * .ideal_yaw A yaw angle of the intended direction, which will be turned
     * towards at up to 45 deg / state. If the enemy is in view and hunt_time is
     * not active, this will be the exact line towards the enemy.
     *
     * .pausetime A monster will leave it's stand state and head towards it's
     * .movetarget when time > .pausetime.
     *
     * walkmove(angle, speed) primitive is all or nothing
     */
	public static bool ai_checkattack(edict_t self, float dist)
	{
		float[] temp = { 0, 0, 0 };

		bool hesDeadJim;

		// this causes monsters to run blindly to the combat point w/o firing
		if (self.goalentity != null)
		{
			if ((self.monsterinfo.aiflags & Defines.AI_COMBAT_POINT) != 0)
				return false;

			if ((self.monsterinfo.aiflags & Defines.AI_SOUND_TARGET) != 0)
			{
				if (GameBase.level.time - self.enemy.teleport_time > 5.0)
				{
					if (self.goalentity == self.enemy)
					{
						if (self.movetarget != null)
							self.goalentity = self.movetarget;
						else
							self.goalentity = null;
					}

					self.monsterinfo.aiflags &= ~Defines.AI_SOUND_TARGET;

					if ((self.monsterinfo.aiflags & Defines.AI_TEMP_STAND_GROUND) != 0)
						self.monsterinfo.aiflags &= ~(Defines.AI_STAND_GROUND | Defines.AI_TEMP_STAND_GROUND);
				}
				else
				{
					self.show_hostile = (int)GameBase.level.time + 1;

					return false;
				}
			}
		}

		GameAI.enemy_vis = false;

		// see if the enemy is dead
		hesDeadJim = false;

		if (null == self.enemy || !self.enemy.inuse)
			hesDeadJim = true;
		else if ((self.monsterinfo.aiflags & Defines.AI_MEDIC) != 0)
		{
			if (self.enemy.health > 0)
			{
				hesDeadJim = true;
				self.monsterinfo.aiflags &= ~Defines.AI_MEDIC;
			}
		}
		else
		{
			if ((self.monsterinfo.aiflags & Defines.AI_BRUTAL) != 0)
			{
				if (self.enemy.health <= -80)
					hesDeadJim = true;
			}
			else
			{
				if (self.enemy.health <= 0)
					hesDeadJim = true;
			}
		}

		if (hesDeadJim)
		{
			self.enemy = null;

			// FIXME: look all around for other targets
			if (self.oldenemy != null && self.oldenemy.health > 0)
			{
				self.enemy = self.oldenemy;
				self.oldenemy = null;
				GameAI.HuntTarget(self);
			}
			else
			{
				if (self.movetarget != null)
				{
					self.goalentity = self.movetarget;
					self.monsterinfo.walk.think(self);
				}
				else
				{
					// we need the pausetime otherwise the stand code
					// will just revert to walking with no target and
					// the monsters will wonder around aimlessly trying
					// to hunt the world entity
					self.monsterinfo.pausetime = GameBase.level.time + 100000000;
					self.monsterinfo.stand.think(self);
				}

				return true;
			}
		}

		self.show_hostile = (int)GameBase.level.time + 1; // wake up other

		// monsters check knowledge of enemy
		GameAI.enemy_vis = GameUtil.visible(self, self.enemy);

		if (GameAI.enemy_vis)
		{
			self.monsterinfo.search_time = GameBase.level.time + 5;
			Math3D.VectorCopy(self.enemy.s.origin, self.monsterinfo.last_sighting);
		}

		GameAI.enemy_infront = GameUtil.infront(self, self.enemy);
		GameAI.enemy_range = GameUtil.range(self, self.enemy);
		Math3D.VectorSubtract(self.enemy.s.origin, self.s.origin, temp);
		GameAI.enemy_yaw = Math3D.vectoyaw(temp);

		// JDC self.ideal_yaw = enemy_yaw;

		if (self.monsterinfo.attack_state == Defines.AS_MISSILE)
		{
			GameAI.ai_run_missile(self);

			return true;
		}

		if (self.monsterinfo.attack_state == Defines.AS_MELEE)
		{
			GameAI.ai_run_melee(self);

			return true;
		}

		// if enemy is not currently visible, we will never attack
		if (!GameAI.enemy_vis)
			return false;

		return self.monsterinfo.checkattack.think(self);
	}

	/**
     * Called once each frame to set level.sight_client to the player to be
     * checked for in findtarget.
     *
     * If all clients are either dead or in notarget, sight_client will be null.
     *
     * In coop games, sight_client will cycle between the clients.
     */
	public static void AI_SetSightClient()
	{
		edict_t ent;
		int start, check;

		if (GameBase.level.sight_client == null)
			start = 1;
		else
			start = GameBase.level.sight_client.index;

		check = start;

		while (true)
		{
			check++;

			if (check > GameBase.game.maxclients)
				check = 1;

			ent = GameBase.g_edicts[check];

			if (ent.inuse && ent.health > 0 && (ent.flags & Defines.FL_NOTARGET) == 0)
			{
				GameBase.level.sight_client = ent;

				return; // got one
			}

			if (check == start)
			{
				GameBase.level.sight_client = null;

				return; // nobody to see
			}
		}
	}

	/**
     * Decides running or standing according to flag AI_STAND_GROUND.
     */
	public static void HuntTarget(edict_t self)
	{
		float[] vec = { 0, 0, 0 };

		self.goalentity = self.enemy;

		if ((self.monsterinfo.aiflags & Defines.AI_STAND_GROUND) != 0)
			self.monsterinfo.stand.think(self);
		else
			self.monsterinfo.run.think(self);

		Math3D.VectorSubtract(self.enemy.s.origin, self.s.origin, vec);
		self.ideal_yaw = Math3D.vectoyaw(vec);

		// wait a while before first attack
		if (0 == (self.monsterinfo.aiflags & Defines.AI_STAND_GROUND))
			GameUtil.AttackFinished(self, 1);
	}

	public static EntThinkAdapter walkmonster_start_go = new("walkmonster_start_go", self =>
	{
		if (0 == (self.spawnflags & 2) && GameBase.level.time < 1)
		{
			M.M_droptofloor.think(self);

			if (self.groundentity != null)
			{
				if (!M.M_walkmove(self, 0, 0))
					GameBase.gi.dprintf(self.classname + " in solid at " + Lib.vtos(self.s.origin) + "\n");
			}
		}

		if (0 == self.yaw_speed)
			self.yaw_speed = 40;

		self.viewheight = 25;

		Monster.monster_start_go(self);

		if ((self.spawnflags & 2) != 0)
			Monster.monster_triggered_start.think(self);

		return true;
	});

	public static EntThinkAdapter walkmonster_start = new("walkmonster_start", self =>
	{
		self.think = GameAI.walkmonster_start_go;
		Monster.monster_start(self);

		return true;
	});

	public static EntThinkAdapter flymonster_start_go = new("flymonster_start_go", self =>
	{
		if (!M.M_walkmove(self, 0, 0))
			GameBase.gi.dprintf(self.classname + " in solid at " + Lib.vtos(self.s.origin) + "\n");

		if (0 == self.yaw_speed)
			self.yaw_speed = 20;

		self.viewheight = 25;

		Monster.monster_start_go(self);

		if ((self.spawnflags & 2) != 0)
			Monster.monster_triggered_start.think(self);

		return true;
	});

	public static EntThinkAdapter flymonster_start = new("flymonster_start", self =>
	{
		self.flags |= Defines.FL_FLY;
		self.think = GameAI.flymonster_start_go;
		Monster.monster_start(self);

		return true;
	});

	public static EntThinkAdapter swimmonster_start_go = new("swimmonster_start_go", self =>
	{
		if (0 == self.yaw_speed)
			self.yaw_speed = 20;

		self.viewheight = 10;

		Monster.monster_start_go(self);

		if ((self.spawnflags & 2) != 0)
			Monster.monster_triggered_start.think(self);

		return true;
	});

	public static EntThinkAdapter swimmonster_start = new("swimmonster_start", self =>
	{
		self.flags |= Defines.FL_SWIM;
		self.think = GameAI.swimmonster_start_go;
		Monster.monster_start(self);

		return true;
	});

	/**
     * Don't move, but turn towards ideal_yaw Distance is for slight position
     * adjustments needed by the animations
     */
	public static AIAdapter ai_turn = new("ai_turn", (self, dist) =>
	{
		if (dist != 0)
			M.M_walkmove(self, self.s.angles[Defines.YAW], dist);

		if (GameUtil.FindTarget(self))
			return;

		M.M_ChangeYaw(self);
	});

	/**
     * Move the specified distance at current facing. This replaces the QC
     * functions: ai_forward, ai_back, ai_pain, and ai_painforward
     */
	public static AIAdapter ai_move = new("ai_move", (self, dist) => { M.M_walkmove(self, self.s.angles[Defines.YAW], dist); });

	/**
     * The monster is walking it's beat.
     */
	public static AIAdapter ai_walk = new("ai_walk", (self, dist) =>
	{
		M.M_MoveToGoal(self, dist);

		// check for noticing a player
		if (GameUtil.FindTarget(self))
			return;

		if (self.monsterinfo.search != null && GameBase.level.time > self.monsterinfo.idle_time)
		{
			if (self.monsterinfo.idle_time != 0)
			{
				self.monsterinfo.search.think(self);
				self.monsterinfo.idle_time = GameBase.level.time + 15 + (float)Globals.rnd.NextDouble() * 15;
			}
			else
				self.monsterinfo.idle_time = GameBase.level.time + (float)Globals.rnd.NextDouble() * 15;
		}
	});

	/**
     * Used for standing around and looking for players Distance is for slight
     * position adjustments needed by the animations.
     */
	public static AIAdapter ai_stand = new("ai_stand", (self, dist) =>
	{
		float[] v = { 0, 0, 0 };

		if (dist != 0)
			M.M_walkmove(self, self.s.angles[Defines.YAW], dist);

		if ((self.monsterinfo.aiflags & Defines.AI_STAND_GROUND) != 0)
		{
			if (self.enemy != null)
			{
				Math3D.VectorSubtract(self.enemy.s.origin, self.s.origin, v);
				self.ideal_yaw = Math3D.vectoyaw(v);

				if (self.s.angles[Defines.YAW] != self.ideal_yaw && 0 != (self.monsterinfo.aiflags & Defines.AI_TEMP_STAND_GROUND))
				{
					self.monsterinfo.aiflags &= ~(Defines.AI_STAND_GROUND | Defines.AI_TEMP_STAND_GROUND);
					self.monsterinfo.run.think(self);
				}

				M.M_ChangeYaw(self);
				GameAI.ai_checkattack(self, 0);
			}
			else
				GameUtil.FindTarget(self);

			return;
		}

		if (GameUtil.FindTarget(self))
			return;

		if (GameBase.level.time > self.monsterinfo.pausetime)
		{
			self.monsterinfo.walk.think(self);

			return;
		}

		if (0 == (self.spawnflags & 1) && self.monsterinfo.idle != null && GameBase.level.time > self.monsterinfo.idle_time)
		{
			if (self.monsterinfo.idle_time != 0)
			{
				self.monsterinfo.idle.think(self);
				self.monsterinfo.idle_time = GameBase.level.time + 15 + (float)Globals.rnd.NextDouble() * 15;
			}
			else
				self.monsterinfo.idle_time = GameBase.level.time + (float)Globals.rnd.NextDouble() * 15;
		}
	});

	/**
     * Turns towards target and advances Use this call with a distnace of 0 to
     * replace ai_face.
     */
	public static AIAdapter ai_charge = new("ai_charge", (self, dist) =>
	{
		float[] v = { 0, 0, 0 };

		Math3D.VectorSubtract(self.enemy.s.origin, self.s.origin, v);
		self.ideal_yaw = Math3D.vectoyaw(v);
		M.M_ChangeYaw(self);

		if (dist != 0)
			M.M_walkmove(self, self.s.angles[Defines.YAW], dist);
	});

	/**
     * The monster has an enemy it is trying to kill.
     */
	public static AIAdapter ai_run = new("ai_run", (self, dist) =>
	{
		float[] v = { 0, 0, 0 };

		edict_t tempgoal;
		edict_t save;
		bool new1;
		edict_t marker;
		float d1, d2;
		trace_t tr; // mem
		float[] v_forward = { 0, 0, 0 }, v_right = { 0, 0, 0 };
		float left, center, right;
		float[] left_target = { 0, 0, 0 }, right_target = { 0, 0, 0 };

		// if we're going to a combat point, just proceed
		if ((self.monsterinfo.aiflags & Defines.AI_COMBAT_POINT) != 0)
		{
			M.M_MoveToGoal(self, dist);

			return;
		}

		if ((self.monsterinfo.aiflags & Defines.AI_SOUND_TARGET) != 0)
		{
			Math3D.VectorSubtract(self.s.origin, self.enemy.s.origin, v);

			// ...and reached it
			if (Math3D.VectorLength(v) < 64)
			{
				//don't move, just stand and listen.
				//self.monsterinfo.aiflags |= (Defines.AI_STAND_GROUND | Defines.AI_TEMP_STAND_GROUND);
				self.monsterinfo.stand.think(self);

				// since now it is aware and does not to be triggered again.
				self.spawnflags &= ~1;
				self.enemy = null;
			}
			else
				M.M_MoveToGoal(self, dist);

			// look for new targets
			if (!GameUtil.FindTarget(self))
				return;
		}

		if (GameAI.ai_checkattack(self, dist))
			return;

		if (self.monsterinfo.attack_state == Defines.AS_SLIDING)
		{
			GameAI.ai_run_slide(self, dist);

			return;
		}

		if (GameAI.enemy_vis)
		{
			//if (self.aiflags & AI_LOST_SIGHT)
			//   dprint("regained sight\n");
			M.M_MoveToGoal(self, dist);
			self.monsterinfo.aiflags &= ~Defines.AI_LOST_SIGHT;
			Math3D.VectorCopy(self.enemy.s.origin, self.monsterinfo.last_sighting);
			self.monsterinfo.trail_time = GameBase.level.time;

			return;
		}

		// coop will change to another enemy if visible
		if (GameBase.coop.value != 0)
		{
			// FIXME: insane guys get mad with this, which causes crashes!
			if (GameUtil.FindTarget(self))
				return;
		}

		if (self.monsterinfo.search_time != 0 && GameBase.level.time > self.monsterinfo.search_time + 20)
		{
			M.M_MoveToGoal(self, dist);
			self.monsterinfo.search_time = 0;

			//dprint("search timeout\n");
			return;
		}

		save = self.goalentity;
		tempgoal = GameUtil.G_Spawn();
		self.goalentity = tempgoal;

		new1 = false;

		if (0 == (self.monsterinfo.aiflags & Defines.AI_LOST_SIGHT))
		{
			// just lost sight of the player, decide where to go first
			// dprint("lost sight of player, last seen at ");
			// dprint(vtos(self.last_sighting));
			// dprint("\n");
			self.monsterinfo.aiflags |= Defines.AI_LOST_SIGHT | Defines.AI_PURSUIT_LAST_SEEN;
			self.monsterinfo.aiflags &= ~(Defines.AI_PURSUE_NEXT | Defines.AI_PURSUE_TEMP);
			new1 = true;
		}

		if ((self.monsterinfo.aiflags & Defines.AI_PURSUE_NEXT) != 0)
		{
			self.monsterinfo.aiflags &= ~Defines.AI_PURSUE_NEXT;

			// dprint("reached current goal: ");
			// dprint(vtos(self.origin));
			// dprint(" ");
			// dprint(vtos(self.last_sighting));
			// dprint(" ");
			// dprint(ftos(vlen(self.origin - self.last_sighting)));
			// dprint("\n");

			// give ourself more time since we got this far
			self.monsterinfo.search_time = GameBase.level.time + 5;

			if ((self.monsterinfo.aiflags & Defines.AI_PURSUE_TEMP) != 0)
			{
				// dprint("was temp goal; retrying original\n");
				self.monsterinfo.aiflags &= ~Defines.AI_PURSUE_TEMP;
				marker = null;
				Math3D.VectorCopy(self.monsterinfo.saved_goal, self.monsterinfo.last_sighting);
				new1 = true;
			}
			else if ((self.monsterinfo.aiflags & Defines.AI_PURSUIT_LAST_SEEN) != 0)
			{
				self.monsterinfo.aiflags &= ~Defines.AI_PURSUIT_LAST_SEEN;
				marker = PlayerTrail.PickFirst(self);
			}
			else
				marker = PlayerTrail.PickNext(self);

			if (marker != null)
			{
				Math3D.VectorCopy(marker.s.origin, self.monsterinfo.last_sighting);
				self.monsterinfo.trail_time = marker.timestamp;
				self.s.angles[Defines.YAW] = self.ideal_yaw = marker.s.angles[Defines.YAW];

				// dprint("heading is ");
				// dprint(ftos(self.ideal_yaw));
				// dprint("\n");
				// debug_drawline(self.origin, self.last_sighting, 52);
				new1 = true;
			}
		}

		Math3D.VectorSubtract(self.s.origin, self.monsterinfo.last_sighting, v);
		d1 = Math3D.VectorLength(v);

		if (d1 <= dist)
		{
			self.monsterinfo.aiflags |= Defines.AI_PURSUE_NEXT;
			dist = d1;
		}

		Math3D.VectorCopy(self.monsterinfo.last_sighting, self.goalentity.s.origin);

		if (new1)
		{
			// gi.dprintf("checking for course correction\n");

			tr = GameBase.gi.trace(self.s.origin, self.mins, self.maxs, self.monsterinfo.last_sighting, self, Defines.MASK_PLAYERSOLID);

			if (tr.fraction < 1)
			{
				Math3D.VectorSubtract(self.goalentity.s.origin, self.s.origin, v);
				d1 = Math3D.VectorLength(v);
				center = tr.fraction;
				d2 = d1 * ((center + 1) / 2);
				self.s.angles[Defines.YAW] = self.ideal_yaw = Math3D.vectoyaw(v);
				Math3D.AngleVectors(self.s.angles, v_forward, v_right, null);

				Math3D.VectorSet(v, d2, -16, 0);
				Math3D.G_ProjectSource(self.s.origin, v, v_forward, v_right, left_target);
				tr = GameBase.gi.trace(self.s.origin, self.mins, self.maxs, left_target, self, Defines.MASK_PLAYERSOLID);
				left = tr.fraction;

				Math3D.VectorSet(v, d2, 16, 0);
				Math3D.G_ProjectSource(self.s.origin, v, v_forward, v_right, right_target);
				tr = GameBase.gi.trace(self.s.origin, self.mins, self.maxs, right_target, self, Defines.MASK_PLAYERSOLID);
				right = tr.fraction;

				center = d1 * center / d2;

				if (left >= center && left > right)
				{
					if (left < 1)
					{
						Math3D.VectorSet(v, d2 * left * 0.5f, -16f, 0f);
						Math3D.G_ProjectSource(self.s.origin, v, v_forward, v_right, left_target);

						// gi.dprintf("incomplete path, go part way and adjust again\n");
					}

					Math3D.VectorCopy(self.monsterinfo.last_sighting, self.monsterinfo.saved_goal);
					self.monsterinfo.aiflags |= Defines.AI_PURSUE_TEMP;
					Math3D.VectorCopy(left_target, self.goalentity.s.origin);
					Math3D.VectorCopy(left_target, self.monsterinfo.last_sighting);
					Math3D.VectorSubtract(self.goalentity.s.origin, self.s.origin, v);
					self.s.angles[Defines.YAW] = self.ideal_yaw = Math3D.vectoyaw(v);

					// gi.dprintf("adjusted left\n");
					// debug_drawline(self.origin, self.last_sighting, 152);
				}
				else if (right >= center && right > left)
				{
					if (right < 1)
					{
						Math3D.VectorSet(v, d2 * right * 0.5f, 16f, 0f);
						Math3D.G_ProjectSource(self.s.origin, v, v_forward, v_right, right_target);

						// gi.dprintf("incomplete path, go part way and adjust again\n");
					}

					Math3D.VectorCopy(self.monsterinfo.last_sighting, self.monsterinfo.saved_goal);
					self.monsterinfo.aiflags |= Defines.AI_PURSUE_TEMP;
					Math3D.VectorCopy(right_target, self.goalentity.s.origin);
					Math3D.VectorCopy(right_target, self.monsterinfo.last_sighting);
					Math3D.VectorSubtract(self.goalentity.s.origin, self.s.origin, v);
					self.s.angles[Defines.YAW] = self.ideal_yaw = Math3D.vectoyaw(v);

					// gi.dprintf("adjusted right\n");
					// debug_drawline(self.origin, self.last_sighting, 152);
				}
			}

			// else gi.dprintf("course was fine\n");
		}

		M.M_MoveToGoal(self, dist);

		GameUtil.G_FreeEdict(tempgoal);

		if (self != null)
			self.goalentity = save;
	});

	private static bool enemy_vis;
	private static bool enemy_infront;
	public static int enemy_range;
	private static float enemy_yaw;
}
