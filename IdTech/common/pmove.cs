/*
 * Copyright (C) 1997-2001 Id Software, Inc.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or (at
 * your option) any later version.
 *
 * This program is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 *
 * See the GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA
 * 02111-1307, USA.
 */

using IdTech.backend;
using Quake2Sharp;
using Quake2Sharp.game.types;
using Quake2Sharp.util;

namespace IdTech.common;

/*
 * =======================================================================
 *
 * Player movement code. This is the core of Quake IIs legendary physics
 * engine
 *
 * =======================================================================
 */
public static class pmove
{
	private const int STEPSIZE = 18;

	/* all of the locals will be zeroed before each
	 * pmove, just to make damn sure we don't have
	 * any differences when running on client or server */
	private class pml_t
	{
		public float[] origin = { 0, 0, 0 }; /* full float precision */
		public float[] velocity = { 0, 0, 0 }; /* full float precision */

		public float[] forward = { 0, 0, 0 };
		public float[] right = { 0, 0, 0 };
		public float[] up = { 0, 0, 0 };
		public float frametime;

		public csurface_t? groundsurface;
		public cplane_t? groundplane;
		public int groundcontents;

		public float[] previous_origin = new float[3];
		public bool ladder;
	}

	private static pmove_t? pm;
	private static pml_t? pml;

	/* movement parameters */
	private const float pm_stopspeed = 100;
	private const float pm_maxspeed = 300;
	private const float pm_duckspeed = 100;
	private const float pm_accelerate = 10;
	public static float pm_airaccelerate = 0;
	private const float pm_wateraccelerate = 10;
	private const float pm_friction = 6;
	private const float pm_waterfriction = 1;
	private const float pm_waterspeed = 400;

	private const float STOP_EPSILON = 0.1f; /* Slide off of the impacting object returns the blocked flags (1 = floor, 2 = step / wall) */
	private const float MIN_STEP_NORMAL = 0.7f; /* can't step up onto very steep slopes */

	private static void PM_ClipVelocity(float[] @in, float[] normal, float[] @out, float overbounce)
	{
		var backoff = Math3D.DotProduct(@in, normal) * overbounce;

		for (var i = 0; i < 3; i++)
		{
			var change = normal[i] * backoff;
			@out[i] = @in[i] - change;

			if (@out[i] > -pmove.STOP_EPSILON && @out[i] < pmove.STOP_EPSILON)
				@out[i] = 0;
		}
	}

	/*
	 * Each intersection will try to step over the obstruction instead of
	 * sliding along it.
	 *
	 * Returns a new origin, velocity, and contact entity
	 * Does not modify any world state?
	 */
	private static void PM_StepSlideMove_()
	{
		var dir = new float[3];
		var planes = new List<float[]>();
		var primal_velocity = new float[3];
		var end = new float[3];

		var numbumps = 4;

		Math3D.VectorCopy(pmove.pml.velocity, primal_velocity);

		var time_left = pmove.pml.frametime;

		for (var bumpcount = 0; bumpcount < numbumps; bumpcount++)
		{
			int i;
			for (i = 0; i < 3; i++)
				end[i] = pmove.pml.origin[i] + time_left * pmove.pml.velocity[i];

			var trace = pmove.pm.trace(pmove.pml.origin, pmove.pm.mins, pmove.pm.maxs, end);

			if (trace.allsolid)
			{
				/* entity is trapped in another solid */
				pmove.pml.velocity[2] = 0; /* don't build up falling damage */
				return;
			}

			/* actually covered some distance */
			if (trace.fraction > 0)
				Math3D.VectorCopy(trace.endpos, pmove.pml.origin);

			/* moved the entire distance */
			if (trace.fraction == 1)
				break;

			/* save entity for contact */
			if (trace.ent != null)
				pmove.pm.touchents.Add(trace.ent);

			time_left -= time_left * trace.fraction;

			var copy = new float[3];
			Math3D.VectorCopy(trace.plane.normal, copy);
			planes.Add(copy);

			/* modify original_velocity so it parallels all of the clip planes */
			for (i = 0; i < planes.Count; i++)
			{
				pmove.PM_ClipVelocity(pmove.pml.velocity, planes[i], pmove.pml.velocity, 1.01f);

				int j;
				for (j = 0; j < planes.Count; j++)
				{
					if (j != i && Math3D.DotProduct(pmove.pml.velocity, planes[j]) < 0)
						break; /* not ok */
				}

				if (j == planes.Count)
					break;
			}

			if (i != planes.Count)
			{
				/* go along this plane */
			}
			else
			{
				/* go along the crease */
				if (planes.Count != 2)
				{
					Math3D.VectorCopy(Globals.vec3_origin, pmove.pml.velocity);
					break;
				}

				Math3D.CrossProduct(planes[0], planes[1], dir);
				var d = Math3D.DotProduct(dir, pmove.pml.velocity);
				Math3D.VectorScale(dir, d, pmove.pml.velocity);
			}

			/* if velocity is against the original velocity, stop dead
			   to avoid tiny occilations in sloping corners */
			if (Math3D.DotProduct(pmove.pml.velocity, primal_velocity) <= 0)
			{
				Math3D.VectorCopy(Globals.vec3_origin, pmove.pml.velocity);
				break;
			}
		}

		if (pmove.pm.s.pm_time != 0)
			Math3D.VectorCopy(primal_velocity, pmove.pml.velocity);
	}

	private static void PM_StepSlideMove()
	{
		var start_o = new float[3];
		var start_v = new float[3];
		var down_o = new float[3];
		var down_v = new float[3];
		var up = new float[3];
		var down = new float[3];

		Math3D.VectorCopy(pmove.pml.origin, start_o);
		Math3D.VectorCopy(pmove.pml.velocity, start_v);

		pmove.PM_StepSlideMove_();

		Math3D.VectorCopy(pmove.pml.origin, down_o);
		Math3D.VectorCopy(pmove.pml.velocity, down_v);

		Math3D.VectorCopy(start_o, up);
		up[2] += pmove.STEPSIZE;

		var trace = pmove.pm.trace(up, pmove.pm.mins, pmove.pm.maxs, up);

		if (trace.allsolid)
			return; /* can't step up */

		/* try sliding above */
		Math3D.VectorCopy(up, pmove.pml.origin);
		Math3D.VectorCopy(start_v, pmove.pml.velocity);

		pmove.PM_StepSlideMove_();

		/* push down the final amount */
		Math3D.VectorCopy(pmove.pml.origin, down);
		down[2] -= pmove.STEPSIZE;
		trace = pmove.pm.trace(pmove.pml.origin, pmove.pm.mins, pmove.pm.maxs, down);

		if (!trace.allsolid)
			Math3D.VectorCopy(trace.endpos, pmove.pml.origin);

		Math3D.VectorCopy(pmove.pml.origin, up);

		/* decide which one went farther */
		var down_dist = (down_o[0] - start_o[0]) * (down_o[0] - start_o[0]) + (down_o[1] - start_o[1]) * (down_o[1] - start_o[1]);
		var up_dist = (up[0] - start_o[0]) * (up[0] - start_o[0]) + (up[1] - start_o[1]) * (up[1] - start_o[1]);

		if (down_dist > up_dist || trace.plane.normal[2] < pmove.MIN_STEP_NORMAL)
		{
			Math3D.VectorCopy(down_o, pmove.pml.origin);
			Math3D.VectorCopy(down_v, pmove.pml.velocity);
			return;
		}

		pmove.pml.velocity[2] = down_v[2];
	}

	/*
	 * Handles both ground friction and water friction
	 */
	private static void PM_Friction()
	{
		var vel = pmove.pml.velocity;

		var speed = (float)Math.Sqrt(vel[0] * vel[0] + vel[1] * vel[1] + vel[2] * vel[2]);

		if (speed < 1)
		{
			vel[0] = 0;
			vel[1] = 0;
			return;
		}

		float drop = 0;

		/* apply ground friction */
		if ((pmove.pm.groundentity != null && pmove.pml.groundsurface != null && (pmove.pml.groundsurface.flags & Defines.SURF_SLICK) == 0) || pmove.pml.ladder)
		{
			var friction = pmove.pm_friction;
			var control = speed < pmove.pm_stopspeed ? pmove.pm_stopspeed : speed;
			drop += control * friction * pmove.pml.frametime;
		}

		/* apply water friction */
		if (pmove.pm.waterlevel != 0 && !pmove.pml.ladder)
			drop += speed * pmove.pm_waterfriction * pmove.pm.waterlevel * pmove.pml.frametime;

		/* scale the velocity */
		var newspeed = speed - drop;

		if (newspeed < 0)
			newspeed = 0;

		newspeed /= speed;

		vel[0] = vel[0] * newspeed;
		vel[1] = vel[1] * newspeed;
		vel[2] = vel[2] * newspeed;
	}

	/*
	 * Handles user intended acceleration
	 */
	private static void PM_Accelerate(float[] wishdir, float wishspeed, float accel)
	{
		var currentspeed = Math3D.DotProduct(pmove.pml.velocity, wishdir);
		var addspeed = wishspeed - currentspeed;

		if (addspeed <= 0)
			return;

		var accelspeed = accel * pmove.pml.frametime * wishspeed;

		if (accelspeed > addspeed)
			accelspeed = addspeed;

		for (var i = 0; i < 3; i++)
			pmove.pml.velocity[i] += accelspeed * wishdir[i];
	}

	private static void PM_AirAccelerate(float[] wishdir, float wishspeed, float accel)
	{
		var wishspd = wishspeed;

		if (wishspd > 30)
			wishspd = 30;

		var currentspeed = Math3D.DotProduct(pmove.pml.velocity, wishdir);
		var addspeed = wishspd - currentspeed;

		if (addspeed <= 0)
			return;

		var accelspeed = accel * wishspeed * pmove.pml.frametime;

		if (accelspeed > addspeed)
			accelspeed = addspeed;

		for (var i = 0; i < 3; i++)
			pmove.pml.velocity[i] += accelspeed * wishdir[i];
	}

	private static void PM_AddCurrents(float[] wishvel)
	{
		var v = new float[3];

		/* account for ladders */
		if (pmove.pml.ladder && Math.Abs(pmove.pml.velocity[2]) <= 200)
		{
			if ((pmove.pm.viewangles[Defines.PITCH] <= -15) && (pmove.pm.cmd.forwardmove > 0))
				wishvel[2] = 200;
			else if ((pmove.pm.viewangles[Defines.PITCH] >= 15) && (pmove.pm.cmd.forwardmove > 0))
				wishvel[2] = -200;
			else if (pmove.pm.cmd.upmove > 0)
				wishvel[2] = 200;
			else if (pmove.pm.cmd.upmove < 0)
				wishvel[2] = -200;
			else
				wishvel[2] = 0;

			/* limit horizontal speed when on a ladder */
			if (wishvel[0] < -25)
				wishvel[0] = -25;
			else if (wishvel[0] > 25)
				wishvel[0] = 25;

			if (wishvel[1] < -25)
				wishvel[1] = -25;
			else if (wishvel[1] > 25)
				wishvel[1] = 25;
		}

		/* add water currents  */
		if ((pmove.pm.watertype & Defines.MASK_CURRENT) != 0)
		{
			Math3D.VectorClear(v);

			if ((pmove.pm.watertype & Defines.CONTENTS_CURRENT_0) != 0)
				v[0] += 1;

			if ((pmove.pm.watertype & Defines.CONTENTS_CURRENT_90) != 0)
				v[1] += 1;

			if ((pmove.pm.watertype & Defines.CONTENTS_CURRENT_180) != 0)
				v[0] -= 1;

			if ((pmove.pm.watertype & Defines.CONTENTS_CURRENT_270) != 0)
				v[1] -= 1;

			if ((pmove.pm.watertype & Defines.CONTENTS_CURRENT_UP) != 0)
				v[2] += 1;

			if ((pmove.pm.watertype & Defines.CONTENTS_CURRENT_DOWN) != 0)
				v[2] -= 1;

			var s = pmove.pm_waterspeed;

			if (pmove.pm.waterlevel == 1 && pmove.pm.groundentity != null)
				s /= 2;

			Math3D.VectorMA(wishvel, s, v, wishvel);
		}

		/* add conveyor belt velocities */
		if (pmove.pm.groundentity != null)
		{
			Math3D.VectorClear(v);

			if ((pmove.pml.groundcontents & Defines.CONTENTS_CURRENT_0) != 0)
				v[0] += 1;

			if ((pmove.pml.groundcontents & Defines.CONTENTS_CURRENT_90) != 0)
				v[1] += 1;

			if ((pmove.pml.groundcontents & Defines.CONTENTS_CURRENT_180) != 0)
				v[0] -= 1;

			if ((pmove.pml.groundcontents & Defines.CONTENTS_CURRENT_270) != 0)
				v[1] -= 1;

			if ((pmove.pml.groundcontents & Defines.CONTENTS_CURRENT_UP) != 0)
				v[2] += 1;

			if ((pmove.pml.groundcontents & Defines.CONTENTS_CURRENT_DOWN) != 0)
				v[2] -= 1;

			Math3D.VectorMA(wishvel, 100, v, wishvel);
		}
	}

	private static void PM_WaterMove()
	{
		var wishvel = new float[3];
		var wishdir = new float[3];

		/* user intentions */
		for (var i = 0; i < 3; i++)
			wishvel[i] = pmove.pml.forward[i] * pmove.pm.cmd.forwardmove + pmove.pml.right[i] * pmove.pm.cmd.sidemove;

		if (pmove.pm.cmd.forwardmove == 0 && pmove.pm.cmd.sidemove == 0 && pmove.pm.cmd.upmove == 0)
			wishvel[2] -= 60; /* drift towards bottom */
		else
			wishvel[2] += pmove.pm.cmd.upmove;

		pmove.PM_AddCurrents(wishvel);

		Math3D.VectorCopy(wishvel, wishdir);
		var wishspeed = Math3D.VectorNormalize(wishdir);

		if (wishspeed > pmove.pm_maxspeed)
		{
			Math3D.VectorScale(wishvel, pmove.pm_maxspeed / wishspeed, wishvel);
			wishspeed = pmove.pm_maxspeed;
		}

		wishspeed *= 0.5f;

		pmove.PM_Accelerate(wishdir, wishspeed, pmove.pm_wateraccelerate);

		pmove.PM_StepSlideMove();
	}

	private static void PM_AirMove()
	{
		var wishvel = new float[3];
		var wishdir = new float[3];

		float fmove = pmove.pm.cmd.forwardmove;
		float smove = pmove.pm.cmd.sidemove;

		for (var i = 0; i < 2; i++)
			wishvel[i] = pmove.pml.forward[i] * fmove + pmove.pml.right[i] * smove;

		wishvel[2] = 0;

		pmove.PM_AddCurrents(wishvel);

		Math3D.VectorCopy(wishvel, wishdir);
		var wishspeed = Math3D.VectorNormalize(wishdir);

		/* clamp to server defined max speed */
		var maxspeed = (pmove.pm.s.pm_flags & pmove_t.PMF_DUCKED) != 0 ? pmove.pm_duckspeed : pmove.pm_maxspeed;

		if (wishspeed > maxspeed)
		{
			Math3D.VectorScale(wishvel, maxspeed / wishspeed, wishvel);
			wishspeed = maxspeed;
		}

		if (pmove.pml.ladder)
		{
			pmove.PM_Accelerate(wishdir, wishspeed, pmove.pm_accelerate);

			if (wishvel[2] == 0)
			{
				if (pmove.pml.velocity[2] > 0)
				{
					pmove.pml.velocity[2] -= pmove.pm.s.gravity * pmove.pml.frametime;

					if (pmove.pml.velocity[2] < 0)
					{
						pmove.pml.velocity[2] = 0;
					}
				}
				else
				{
					pmove.pml.velocity[2] += pmove.pm.s.gravity * pmove.pml.frametime;

					if (pmove.pml.velocity[2] > 0)
					{
						pmove.pml.velocity[2] = 0;
					}
				}
			}

			pmove.PM_StepSlideMove();
		}
		else if (pmove.pm.groundentity != null)
		{
			/* walking on ground */
			pmove.pml.velocity[2] = 0;
			pmove.PM_Accelerate(wishdir, wishspeed, pmove.pm_accelerate);

			if (pmove.pm.s.gravity > 0)
				pmove.pml.velocity[2] = 0;
			else
				pmove.pml.velocity[2] -= pmove.pm.s.gravity * pmove.pml.frametime;

			if (pmove.pml.velocity[0] == 0 && pmove.pml.velocity[1] == 0)
				return;

			pmove.PM_StepSlideMove();
		}
		else
		{
			/* not on ground, so little effect on velocity */
			if (pmove.pm_airaccelerate != 0)
				pmove.PM_AirAccelerate(wishdir, wishspeed, pmove.pm_accelerate);
			else
				pmove.PM_Accelerate(wishdir, wishspeed, 1);

			/* add gravity */
			pmove.pml.velocity[2] -= pmove.pm.s.gravity * pmove.pml.frametime;
			pmove.PM_StepSlideMove();
		}
	}

	private static void PM_CatagorizePosition()
	{
		var point = new float[3];
		int cont;
		trace_t trace;
		float sample1;
		float sample2;

		/* if the player hull point one unit down
		   is solid, the player is on ground */

		/* see if standing on something solid */
		point[0] = pmove.pml.origin[0];
		point[1] = pmove.pml.origin[1];
		point[2] = pmove.pml.origin[2] - 0.25f;

		if (pmove.pml.velocity[2] > 180)
		{
			pmove.pm.s.pm_flags = (byte)(pmove.pm.s.pm_flags & ~pmove_t.PMF_ON_GROUND);
			pmove.pm.groundentity = null;
		}
		else
		{
			trace = pmove.pm.trace(pmove.pml.origin, pmove.pm.mins, pmove.pm.maxs, point);
			pmove.pml.groundplane = trace.plane;
			pmove.pml.groundsurface = trace.surface;
			pmove.pml.groundcontents = trace.contents;

			if (trace.ent == null || (trace.plane.normal[2] < 0.7 && !trace.startsolid))
			{
				pmove.pm.groundentity = null;
				pmove.pm.s.pm_flags = (byte)(pmove.pm.s.pm_flags & ~pmove_t.PMF_ON_GROUND);
			}
			else
			{
				pmove.pm.groundentity = trace.ent;

				/* hitting solid ground will end a waterjump */
				if ((pmove.pm.s.pm_flags & pmove_t.PMF_TIME_WATERJUMP) != 0)
				{
					pmove.pm.s.pm_flags = (byte)(pmove.pm.s.pm_flags & ~(pmove_t.PMF_TIME_WATERJUMP | pmove_t.PMF_TIME_LAND | pmove_t.PMF_TIME_TELEPORT));
					pmove.pm.s.pm_time = 0;
				}

				if ((pmove.pm.s.pm_flags & pmove_t.PMF_ON_GROUND) == 0)
				{
					/* just hit the ground */
					pmove.pm.s.pm_flags |= pmove_t.PMF_ON_GROUND;

					/* don't do landing time if we were just going down a slope */
					if (pmove.pml.velocity[2] < -200)
					{
						pmove.pm.s.pm_flags |= pmove_t.PMF_TIME_LAND;

						/* don't allow another jump for a little while */
						if (pmove.pml.velocity[2] < -400)
							pmove.pm.s.pm_time = 25;
						else
							pmove.pm.s.pm_time = 18;
					}
				}
			}

			if (trace.ent != null)
				pmove.pm.touchents.Add(trace.ent);
		}

		/* get waterlevel, accounting for ducking */
		pmove.pm.waterlevel = 0;
		pmove.pm.watertype = 0;

		sample2 = pmove.pm.viewheight - pmove.pm.mins[2];
		sample1 = sample2 / 2;

		point[2] = pmove.pml.origin[2] + pmove.pm.mins[2] + 1;
		cont = pmove.pm.pointcontents(point);

		if ((cont & Defines.MASK_WATER) == 0)
			return;

		pmove.pm.watertype = cont;
		pmove.pm.waterlevel = 1;
		point[2] = pmove.pml.origin[2] + pmove.pm.mins[2] + sample1;
		cont = pmove.pm.pointcontents(point);

		if ((cont & Defines.MASK_WATER) == 0)
			return;

		pmove.pm.waterlevel = 2;
		point[2] = pmove.pml.origin[2] + pmove.pm.mins[2] + sample2;
		cont = pmove.pm.pointcontents(point);

		if ((cont & Defines.MASK_WATER) != 0)
			pmove.pm.waterlevel = 3;
	}

	public static void PM_CheckJump()
	{
		/* hasn't been long enough since landing to jump again */
		if ((pmove.pm.s.pm_flags & pmove_t.PMF_TIME_LAND) != 0)
			return;

		if (pmove.pm.cmd.upmove < 10)
		{
			/* not holding jump */
			pmove.pm.s.pm_flags = (byte)(pmove.pm.s.pm_flags & ~pmove_t.PMF_JUMP_HELD);
			return;
		}

		/* must wait for jump to be released */
		if ((pmove.pm.s.pm_flags & pmove_t.PMF_JUMP_HELD) != 0)
			return;

		if (pmove.pm.s.pm_type == Defines.PM_DEAD)
			return;

		if (pmove.pm.waterlevel >= 2)
		{
			/* swimming, not jumping */
			pmove.pm.groundentity = null;

			if (pmove.pml.velocity[2] <= -300)
				return;

			if (pmove.pm.watertype == Defines.CONTENTS_WATER)
				pmove.pml.velocity[2] = 100;
			else if (pmove.pm.watertype == Defines.CONTENTS_SLIME)
				pmove.pml.velocity[2] = 80;
			else
				pmove.pml.velocity[2] = 50;

			return;
		}

		if (pmove.pm.groundentity == null)
			return; /* in air, so no effect */

		pmove.pm.s.pm_flags |= pmove_t.PMF_JUMP_HELD;

		pmove.pm.groundentity = null;
		pmove.pml.velocity[2] += 270;

		if (pmove.pml.velocity[2] < 270)
			pmove.pml.velocity[2] = 270;
	}

	private static void PM_CheckSpecialMovement()
	{
		var spot = new float[3];
		var flatforward = new float[3];

		if (pmove.pm.s.pm_time != 0)
			return;

		pmove.pml.ladder = false;

		/* check for ladder */
		flatforward[0] = pmove.pml.forward[0];
		flatforward[1] = pmove.pml.forward[1];
		flatforward[2] = 0;
		Math3D.VectorNormalize(flatforward);

		Math3D.VectorMA(pmove.pml.origin, 1, flatforward, spot);
		var trace = pmove.pm.trace(pmove.pml.origin, pmove.pm.mins, pmove.pm.maxs, spot);

		if (trace.fraction < 1 && (trace.contents & Defines.CONTENTS_LADDER) != 0)
			pmove.pml.ladder = true;

		/* check for water jump */
		if (pmove.pm.waterlevel != 2)
			return;

		Math3D.VectorMA(pmove.pml.origin, 30, flatforward, spot);
		spot[2] += 4;
		var cont = pmove.pm.pointcontents(spot);

		if ((cont & Defines.CONTENTS_SOLID) == 0)
			return;

		spot[2] += 16;
		cont = pmove.pm.pointcontents(spot);

		if (cont != 0)
			return;

		/* jump out of water */
		Math3D.VectorScale(flatforward, 50, pmove.pml.velocity);
		pmove.pml.velocity[2] = 350;

		pmove.pm.s.pm_flags |= pmove_t.PMF_TIME_WATERJUMP;
		pmove.pm.s.pm_time = 255;
	}

	public static void PM_FlyMove(bool doclip)
	{
		var wishvel = new float[3];
		var wishdir = new float[3];
		var end = new float[3];

		pmove.pm.viewheight = 22;

		/* friction */
		var speed = Math3D.VectorLength(pmove.pml.velocity);

		if (speed < 1)
			Math3D.VectorCopy(Globals.vec3_origin, pmove.pml.velocity);
		else
		{
			float drop = 0;

			var friction = pmove.pm_friction * 1.5f;
			var control = speed < pmove.pm_stopspeed ? pmove.pm_stopspeed : speed;
			drop += control * friction * pmove.pml.frametime;

			/* scale the velocity */
			var newspeed = speed - drop;

			if (newspeed < 0)
				newspeed = 0;

			newspeed /= speed;

			Math3D.VectorScale(pmove.pml.velocity, newspeed, pmove.pml.velocity);
		}

		/* accelerate */
		float fmove = pmove.pm.cmd.forwardmove;
		float smove = pmove.pm.cmd.sidemove;

		Math3D.VectorNormalize(pmove.pml.forward);
		Math3D.VectorNormalize(pmove.pml.right);

		for (var i = 0; i < 3; i++)
			wishvel[i] = pmove.pml.forward[i] * fmove + pmove.pml.right[i] * smove;

		wishvel[2] += pmove.pm.cmd.upmove;

		Math3D.VectorCopy(wishvel, wishdir);
		var wishspeed = Math3D.VectorNormalize(wishdir);

		/* clamp to server defined max speed */
		if (wishspeed > pmove.pm_maxspeed)
		{
			Math3D.VectorScale(wishvel, pmove.pm_maxspeed / wishspeed, wishvel);
			wishspeed = pmove.pm_maxspeed;
		}

		var currentspeed = Math3D.DotProduct(pmove.pml.velocity, wishdir);
		var addspeed = wishspeed - currentspeed;

		if (addspeed <= 0)
			return;

		var accelspeed = pmove.pm_accelerate * pmove.pml.frametime * wishspeed;

		if (accelspeed > addspeed)
			accelspeed = addspeed;

		for (var i = 0; i < 3; i++)
			pmove.pml.velocity[i] += accelspeed * wishdir[i];

		if (doclip)
		{
			for (var i = 0; i < 3; i++)
				end[i] = pmove.pml.origin[i] + pmove.pml.frametime * pmove.pml.velocity[i];

			var trace = pmove.pm.trace(pmove.pml.origin, pmove.pm.mins, pmove.pm.maxs, end);

			Math3D.VectorCopy(trace.endpos, pmove.pml.origin);
		}
		else
			Math3D.VectorMA(pmove.pml.origin, pmove.pml.frametime, pmove.pml.velocity, pmove.pml.origin); /* move */
	}

	/*
	 * Sets mins, maxs, and pm.viewheight
	 */
	private static void PM_CheckDuck()
	{
		pmove.pm.mins[0] = -16;
		pmove.pm.mins[1] = -16;

		pmove.pm.maxs[0] = 16;
		pmove.pm.maxs[1] = 16;

		if (pmove.pm.s.pm_type == Defines.PM_GIB)
		{
			pmove.pm.mins[2] = 0;
			pmove.pm.maxs[2] = 16;
			pmove.pm.viewheight = 8;
			return;
		}

		pmove.pm.mins[2] = -24;

		if (pmove.pm.s.pm_type == Defines.PM_DEAD)
			pmove.pm.s.pm_flags |= pmove_t.PMF_DUCKED;
		else if (pmove.pm.cmd.upmove < 0 && (pmove.pm.s.pm_flags & pmove_t.PMF_ON_GROUND) != 0)
			pmove.pm.s.pm_flags |= pmove_t.PMF_DUCKED; /* duck */
		else
		{
			/* stand up if possible */
			if ((pmove.pm.s.pm_flags & pmove_t.PMF_DUCKED) != 0)
			{
				/* try to stand up */
				pmove.pm.maxs[2] = 32;
				var trace = pmove.pm.trace(pmove.pml.origin, pmove.pm.mins, pmove.pm.maxs, pmove.pml.origin);

				if (!trace.allsolid)
					pmove.pm.s.pm_flags = (byte)(pmove.pm.s.pm_flags & ~pmove_t.PMF_DUCKED);
			}
		}

		if ((pmove.pm.s.pm_flags & pmove_t.PMF_DUCKED) != 0)
		{
			pmove.pm.maxs[2] = 4;
			pmove.pm.viewheight = -2;
		}
		else
		{
			pmove.pm.maxs[2] = 32;
			pmove.pm.viewheight = 22;
		}
	}

	private static void PM_DeadMove()
	{
		if (pmove.pm.groundentity == null)
			return;

		/* extra friction */
		var forward = Math3D.VectorLength(pmove.pml.velocity);
		forward -= 20;

		if (forward <= 0)
			Math3D.VectorClear(pmove.pml.velocity);
		else
		{
			Math3D.VectorNormalize(pmove.pml.velocity);
			Math3D.VectorScale(pmove.pml.velocity, forward, pmove.pml.velocity);
		}
	}

	private static bool PM_GoodPosition()
	{
		trace_t trace;
		var origin = new float[3];
		var end = new float[3];

		if (pmove.pm.s.pm_type == Defines.PM_SPECTATOR)
			return true;

		for (var i = 0; i < 3; i++)
			origin[i] = end[i] = pmove.pm.s.origin[i] * 0.125f;

		trace = pmove.pm.trace(origin, pmove.pm.mins, pmove.pm.maxs, end);

		return !trace.allsolid;
	}

	/*
	 * On exit, the origin will have a value that is pre-quantized to the 0.125
	 * precision of the network channel and in a valid position.
	 */
	private static void PM_SnapPosition()
	{
		var sign = new short[3];
		var @base = new short[3];

		/* try all single bits first */
		var jitterbits = new[] { 0, 4, 1, 2, 3, 5, 6, 7 };

		/* snap velocity to eigths */
		for (var i = 0; i < 3; i++)
			pmove.pm.s.velocity[i] = (short)(pmove.pml.velocity[i] * 8);

		for (var i = 0; i < 3; i++)
		{
			if (pmove.pml.origin[i] >= 0)
				sign[i] = 1;
			else
				sign[i] = -1;

			pmove.pm.s.origin[i] = (short)(pmove.pml.origin[i] * 8);

			if (pmove.pm.s.origin[i] * 0.125f == pmove.pml.origin[i])
				sign[i] = 0;
		}

		Math3D.VectorCopy(pmove.pm.s.origin, @base);

		/* try all combinations */
		for (var j = 0; j < 8; j++)
		{
			var bits = jitterbits[j];
			Math3D.VectorCopy(@base, pmove.pm.s.origin);

			for (var i = 0; i < 3; i++)
			{
				if ((bits & (1 << i)) != 0)
					pmove.pm.s.origin[i] += sign[i];
			}

			if (pmove.PM_GoodPosition())
				return;
		}

		/* go back to the last position */
		Math3D.VectorCopy(pmove.pml.previous_origin, pmove.pm.s.origin);
	}

	private static void PM_InitialSnapPosition()
	{
		var @base = new short[3];
		var offset = new[]{ 0, -1, 1 };

		Math3D.VectorCopy(pmove.pm.s.origin, @base);

		for (var z = 0; z < 3; z++)
		{
			pmove.pm.s.origin[2] = (short)(@base[2] + offset[z]);

			for (var y = 0; y < 3; y++)
			{
				pmove.pm.s.origin[1] = (short)(@base[1] + offset[y]);

				for (var x = 0; x < 3; x++)
				{
					pmove.pm.s.origin[0] = (short)(@base[0] + offset[x]);

					if (pmove.PM_GoodPosition())
					{
						pmove.pml.origin[0] = pmove.pm.s.origin[0] * 0.125f;
						pmove.pml.origin[1] = pmove.pm.s.origin[1] * 0.125f;
						pmove.pml.origin[2] = pmove.pm.s.origin[2] * 0.125f;
						Math3D.VectorCopy(pmove.pm.s.origin, pmove.pml.previous_origin);
						return;
					}
				}
			}
		}

		clientserver.Com_DPrintf("Bad InitialSnapPosition\n");
	}

	private static void PM_ClampAngles()
	{
		if ((pmove.pm.s.pm_flags & pmove_t.PMF_TIME_TELEPORT) != 0)
		{
			pmove.pm.viewangles[Defines.YAW] = Math3D.SHORT2ANGLE(pmove.pm.cmd.angles[Defines.YAW] + pmove.pm.s.delta_angles[Defines.YAW]);
			pmove.pm.viewangles[Defines.PITCH] = 0;
			pmove.pm.viewangles[Defines.ROLL] = 0;
		}
		else
		{
			/* circularly clamp the angles with deltas */
			for (var i = 0; i < 3; i++)
			{
				var temp = (short)(pmove.pm.cmd.angles[i] + pmove.pm.s.delta_angles[i]);
				pmove.pm.viewangles[i] = Math3D.SHORT2ANGLE(temp);
			}

			/* don't let the player look up or down more than 90 degrees */
			if ((pmove.pm.viewangles[Defines.PITCH] > 89) && (pmove.pm.viewangles[Defines.PITCH] < 180))
				pmove.pm.viewangles[Defines.PITCH] = 89;
			else if ((pmove.pm.viewangles[Defines.PITCH] < 271) && (pmove.pm.viewangles[Defines.PITCH] >= 180))
				pmove.pm.viewangles[Defines.PITCH] = 271;
		}

		Math3D.AngleVectors(pmove.pm.viewangles, pmove.pml.forward, pmove.pml.right, pmove.pml.up);
	}

	private static void PM_CalculateViewHeightForDemo()
	{
		if (pmove.pm.s.pm_type == Defines.PM_GIB)
			pmove.pm.viewheight = 8;
		else if ((pmove.pm.s.pm_flags & pmove_t.PMF_DUCKED) != 0)
			pmove.pm.viewheight = -2;
		else
			pmove.pm.viewheight = 22;
	}

	private static void PM_CalculateWaterLevelForDemo()
	{
		var point = new float[3];

		point[0] = pmove.pml.origin[0];
		point[1] = pmove.pml.origin[1];
		point[2] = pmove.pml.origin[2] + pmove.pm.viewheight;

		pmove.pm.waterlevel = 0;
		pmove.pm.watertype = 0;

		var cont = pmove.pm.pointcontents(point);

		if ((cont & Defines.MASK_WATER) != 0)
		{
			pmove.pm.waterlevel = 3;
			pmove.pm.watertype = cont;
		}
	}

	private static bool underwater;

	private static void PM_UpdateUnderwaterSfx()
	{
		if (pmove.pm != null && pmove.pm.waterlevel == 3 && !pmove.underwater)
		{
			pmove.underwater = true;
			// TODO implement
			/*snd_is_underwater = 1;

			if (snd_is_underwater_enabled)
				AL_Underwater();*/
		}

		if (pmove.pm.waterlevel < 3 && pmove.underwater)
		{
			pmove.underwater = false;
			// TODO implement
			/*snd_is_underwater = 0;

			if (snd_is_underwater_enabled)
				AL_Overwater();*/
		}
	}

	/*
	 * Can be called by either the server or the client
	 */
	public static void Pmove(pmove_t pmove)
	{
		common.pmove.pm = pmove;

		/* clear results */
		Math3D.VectorClear(common.pmove.pm.viewangles);
		common.pmove.pm.viewheight = 0;
		common.pmove.pm.groundentity = null;
		common.pmove.pm.watertype = 0;
		common.pmove.pm.waterlevel = 0;

		/* clear all pmove local vars */
		common.pmove.pml = new pml_t();

		/* convert origin and velocity to float values */
		common.pmove.pml.origin[0] = common.pmove.pm.s.origin[0] * 0.125f;
		common.pmove.pml.origin[1] = common.pmove.pm.s.origin[1] * 0.125f;
		common.pmove.pml.origin[2] = common.pmove.pm.s.origin[2] * 0.125f;

		common.pmove.pml.velocity[0] = common.pmove.pm.s.velocity[0] * 0.125f;
		common.pmove.pml.velocity[1] = common.pmove.pm.s.velocity[1] * 0.125f;
		common.pmove.pml.velocity[2] = common.pmove.pm.s.velocity[2] * 0.125f;

		/* save old org in case we get stuck */
		Math3D.VectorCopy(common.pmove.pm.s.origin, common.pmove.pml.previous_origin);

		common.pmove.pml.frametime = common.pmove.pm.cmd.msec * 0.001f;

		common.pmove.PM_ClampAngles();

		if (common.pmove.pm.s.pm_type == Defines.PM_SPECTATOR)
		{
			common.pmove.PM_FlyMove(false);
			common.pmove.PM_SnapPosition();
			return;
		}

		if (common.pmove.pm.s.pm_type >= Defines.PM_DEAD)
		{
			common.pmove.pm.cmd.forwardmove = 0;
			common.pmove.pm.cmd.sidemove = 0;
			common.pmove.pm.cmd.upmove = 0;
		}

		if (common.pmove.pm.s.pm_type == Defines.PM_FREEZE)
		{
			if (!main.DEDICATED_ONLY && Globals.cl.attractloop)
			{
				common.pmove.PM_CalculateViewHeightForDemo();
				common.pmove.PM_CalculateWaterLevelForDemo();
				common.pmove.PM_UpdateUnderwaterSfx();
			}

			return; /* no movement at all */
		}

		/* set mins, maxs, and viewheight */
		common.pmove.PM_CheckDuck();

		if (common.pmove.pm.snapinitial)
			common.pmove.PM_InitialSnapPosition();

		/* set groundentity, watertype, and waterlevel */
		common.pmove.PM_CatagorizePosition();

		if (common.pmove.pm.s.pm_type == Defines.PM_DEAD)
			common.pmove.PM_DeadMove();

		common.pmove.PM_CheckSpecialMovement();

		/* drop timing counter */
		if (common.pmove.pm.s.pm_time != 0)
		{
			var msec = common.pmove.pm.cmd.msec >> 3;

			if (msec == 0)
				msec = 1;

			if (msec >= common.pmove.pm.s.pm_time)
			{
				common.pmove.pm.s.pm_flags = (byte)(common.pmove.pm.s.pm_flags & ~(pmove_t.PMF_TIME_WATERJUMP | pmove_t.PMF_TIME_LAND | pmove_t.PMF_TIME_TELEPORT));
				common.pmove.pm.s.pm_time = 0;
			}
			else
				common.pmove.pm.s.pm_time -= (byte)msec;
		}

		if ((common.pmove.pm.s.pm_flags & pmove_t.PMF_TIME_TELEPORT) != 0)
		{
			/* teleport pause stays exactly in place */
		}
		else if ((common.pmove.pm.s.pm_flags & pmove_t.PMF_TIME_WATERJUMP) != 0)
		{
			/* waterjump has no control, but falls */
			common.pmove.pml.velocity[2] -= common.pmove.pm.s.gravity * common.pmove.pml.frametime;

			if (common.pmove.pml.velocity[2] < 0)
			{
				/* cancel as soon as we are falling down again */
				common.pmove.pm.s.pm_flags = (byte)(common.pmove.pm.s.pm_flags & ~(pmove_t.PMF_TIME_WATERJUMP | pmove_t.PMF_TIME_LAND | pmove_t.PMF_TIME_TELEPORT));
				common.pmove.pm.s.pm_time = 0;
			}

			common.pmove.PM_StepSlideMove();
		}
		else
		{
			common.pmove.PM_CheckJump();

			common.pmove.PM_Friction();

			if (common.pmove.pm.waterlevel >= 2)
				common.pmove.PM_WaterMove();
			else
			{
				var angles = new float[3];

				Math3D.VectorCopy(common.pmove.pm.viewangles, angles);

				if (angles[Defines.PITCH] > 180)
					angles[Defines.PITCH] -= 360;

				angles[Defines.PITCH] /= 3;

				Math3D.AngleVectors(angles, common.pmove.pml.forward, common.pmove.pml.right, common.pmove.pml.up);

				common.pmove.PM_AirMove();
			}
		}

		/* set groundentity, watertype, and waterlevel for final spot */
		common.pmove.PM_CatagorizePosition();

		if (main.DEDICATED_ONLY)
			common.pmove.PM_UpdateUnderwaterSfx();

		common.pmove.PM_SnapPosition();
	}
}
