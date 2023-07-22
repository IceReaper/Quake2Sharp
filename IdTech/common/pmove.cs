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

			if (@out[i] > -STOP_EPSILON && @out[i] < STOP_EPSILON)
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

		Math3D.VectorCopy(pml.velocity, primal_velocity);

		var time_left = pml.frametime;

		for (var bumpcount = 0; bumpcount < numbumps; bumpcount++)
		{
			int i;
			for (i = 0; i < 3; i++)
				end[i] = pml.origin[i] + time_left * pml.velocity[i];

			var trace = pm.trace(pml.origin, pm.mins, pm.maxs, end);

			if (trace.allsolid)
			{
				/* entity is trapped in another solid */
				pml.velocity[2] = 0; /* don't build up falling damage */
				return;
			}

			/* actually covered some distance */
			if (trace.fraction > 0)
				Math3D.VectorCopy(trace.endpos, pml.origin);

			/* moved the entire distance */
			if (trace.fraction == 1)
				break;

			/* save entity for contact */
			if (trace.ent != null)
				pm.touchents.Add(trace.ent);

			time_left -= time_left * trace.fraction;

			var copy = new float[3];
			Math3D.VectorCopy(trace.plane.normal, copy);
			planes.Add(copy);

			/* modify original_velocity so it parallels all of the clip planes */
			for (i = 0; i < planes.Count; i++)
			{
				PM_ClipVelocity(pml.velocity, planes[i], pml.velocity, 1.01f);

				int j;
				for (j = 0; j < planes.Count; j++)
				{
					if (j != i && Math3D.DotProduct(pml.velocity, planes[j]) < 0)
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
					Math3D.VectorCopy(Globals.vec3_origin, pml.velocity);
					break;
				}

				Math3D.CrossProduct(planes[0], planes[1], dir);
				var d = Math3D.DotProduct(dir, pml.velocity);
				Math3D.VectorScale(dir, d, pml.velocity);
			}

			/* if velocity is against the original velocity, stop dead
			   to avoid tiny occilations in sloping corners */
			if (Math3D.DotProduct(pml.velocity, primal_velocity) <= 0)
			{
				Math3D.VectorCopy(Globals.vec3_origin, pml.velocity);
				break;
			}
		}

		if (pm.s.pm_time != 0)
			Math3D.VectorCopy(primal_velocity, pml.velocity);
	}

	private static void PM_StepSlideMove()
	{
		var start_o = new float[3];
		var start_v = new float[3];
		var down_o = new float[3];
		var down_v = new float[3];
		var up = new float[3];
		var down = new float[3];

		Math3D.VectorCopy(pml.origin, start_o);
		Math3D.VectorCopy(pml.velocity, start_v);

		PM_StepSlideMove_();

		Math3D.VectorCopy(pml.origin, down_o);
		Math3D.VectorCopy(pml.velocity, down_v);

		Math3D.VectorCopy(start_o, up);
		up[2] += STEPSIZE;

		var trace = pm.trace(up, pm.mins, pm.maxs, up);

		if (trace.allsolid)
			return; /* can't step up */

		/* try sliding above */
		Math3D.VectorCopy(up, pml.origin);
		Math3D.VectorCopy(start_v, pml.velocity);

		PM_StepSlideMove_();

		/* push down the final amount */
		Math3D.VectorCopy(pml.origin, down);
		down[2] -= STEPSIZE;
		trace = pm.trace(pml.origin, pm.mins, pm.maxs, down);

		if (!trace.allsolid)
			Math3D.VectorCopy(trace.endpos, pml.origin);

		Math3D.VectorCopy(pml.origin, up);

		/* decide which one went farther */
		var down_dist = (down_o[0] - start_o[0]) * (down_o[0] - start_o[0]) + (down_o[1] - start_o[1]) * (down_o[1] - start_o[1]);
		var up_dist = (up[0] - start_o[0]) * (up[0] - start_o[0]) + (up[1] - start_o[1]) * (up[1] - start_o[1]);

		if (down_dist > up_dist || trace.plane.normal[2] < MIN_STEP_NORMAL)
		{
			Math3D.VectorCopy(down_o, pml.origin);
			Math3D.VectorCopy(down_v, pml.velocity);
			return;
		}

		pml.velocity[2] = down_v[2];
	}

	/*
	 * Handles both ground friction and water friction
	 */
	private static void PM_Friction()
	{
		var vel = pml.velocity;

		var speed = (float)Math.Sqrt(vel[0] * vel[0] + vel[1] * vel[1] + vel[2] * vel[2]);

		if (speed < 1)
		{
			vel[0] = 0;
			vel[1] = 0;
			return;
		}

		float drop = 0;

		/* apply ground friction */
		if ((pm.groundentity != null && pml.groundsurface != null && (pml.groundsurface.flags & Defines.SURF_SLICK) == 0) || pml.ladder)
		{
			var friction = pm_friction;
			var control = speed < pm_stopspeed ? pm_stopspeed : speed;
			drop += control * friction * pml.frametime;
		}

		/* apply water friction */
		if (pm.waterlevel != 0 && !pml.ladder)
			drop += speed * pm_waterfriction * pm.waterlevel * pml.frametime;

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
		var currentspeed = Math3D.DotProduct(pml.velocity, wishdir);
		var addspeed = wishspeed - currentspeed;

		if (addspeed <= 0)
			return;

		var accelspeed = accel * pml.frametime * wishspeed;

		if (accelspeed > addspeed)
			accelspeed = addspeed;

		for (var i = 0; i < 3; i++)
			pml.velocity[i] += accelspeed * wishdir[i];
	}

	private static void PM_AirAccelerate(float[] wishdir, float wishspeed, float accel)
	{
		var wishspd = wishspeed;

		if (wishspd > 30)
			wishspd = 30;

		var currentspeed = Math3D.DotProduct(pml.velocity, wishdir);
		var addspeed = wishspd - currentspeed;

		if (addspeed <= 0)
			return;

		var accelspeed = accel * wishspeed * pml.frametime;

		if (accelspeed > addspeed)
			accelspeed = addspeed;

		for (var i = 0; i < 3; i++)
			pml.velocity[i] += accelspeed * wishdir[i];
	}

	private static void PM_AddCurrents(float[] wishvel)
	{
		var v = new float[3];

		/* account for ladders */
		if (pml.ladder && Math.Abs(pml.velocity[2]) <= 200)
		{
			if ((pm.viewangles[Defines.PITCH] <= -15) && (pm.cmd.forwardmove > 0))
				wishvel[2] = 200;
			else if ((pm.viewangles[Defines.PITCH] >= 15) && (pm.cmd.forwardmove > 0))
				wishvel[2] = -200;
			else if (pm.cmd.upmove > 0)
				wishvel[2] = 200;
			else if (pm.cmd.upmove < 0)
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
		if ((pm.watertype & Defines.MASK_CURRENT) != 0)
		{
			Math3D.VectorClear(v);

			if ((pm.watertype & Defines.CONTENTS_CURRENT_0) != 0)
				v[0] += 1;

			if ((pm.watertype & Defines.CONTENTS_CURRENT_90) != 0)
				v[1] += 1;

			if ((pm.watertype & Defines.CONTENTS_CURRENT_180) != 0)
				v[0] -= 1;

			if ((pm.watertype & Defines.CONTENTS_CURRENT_270) != 0)
				v[1] -= 1;

			if ((pm.watertype & Defines.CONTENTS_CURRENT_UP) != 0)
				v[2] += 1;

			if ((pm.watertype & Defines.CONTENTS_CURRENT_DOWN) != 0)
				v[2] -= 1;

			var s = pm_waterspeed;

			if (pm.waterlevel == 1 && pm.groundentity != null)
				s /= 2;

			Math3D.VectorMA(wishvel, s, v, wishvel);
		}

		/* add conveyor belt velocities */
		if (pm.groundentity != null)
		{
			Math3D.VectorClear(v);

			if ((pml.groundcontents & Defines.CONTENTS_CURRENT_0) != 0)
				v[0] += 1;

			if ((pml.groundcontents & Defines.CONTENTS_CURRENT_90) != 0)
				v[1] += 1;

			if ((pml.groundcontents & Defines.CONTENTS_CURRENT_180) != 0)
				v[0] -= 1;

			if ((pml.groundcontents & Defines.CONTENTS_CURRENT_270) != 0)
				v[1] -= 1;

			if ((pml.groundcontents & Defines.CONTENTS_CURRENT_UP) != 0)
				v[2] += 1;

			if ((pml.groundcontents & Defines.CONTENTS_CURRENT_DOWN) != 0)
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
			wishvel[i] = pml.forward[i] * pm.cmd.forwardmove + pml.right[i] * pm.cmd.sidemove;

		if (pm.cmd.forwardmove == 0 && pm.cmd.sidemove == 0 && pm.cmd.upmove == 0)
			wishvel[2] -= 60; /* drift towards bottom */
		else
			wishvel[2] += pm.cmd.upmove;

		PM_AddCurrents(wishvel);

		Math3D.VectorCopy(wishvel, wishdir);
		var wishspeed = Math3D.VectorNormalize(wishdir);

		if (wishspeed > pm_maxspeed)
		{
			Math3D.VectorScale(wishvel, pm_maxspeed / wishspeed, wishvel);
			wishspeed = pm_maxspeed;
		}

		wishspeed *= 0.5f;

		PM_Accelerate(wishdir, wishspeed, pm_wateraccelerate);

		PM_StepSlideMove();
	}

	private static void PM_AirMove()
	{
		var wishvel = new float[3];
		var wishdir = new float[3];

		float fmove = pm.cmd.forwardmove;
		float smove = pm.cmd.sidemove;

		for (var i = 0; i < 2; i++)
			wishvel[i] = pml.forward[i] * fmove + pml.right[i] * smove;

		wishvel[2] = 0;

		PM_AddCurrents(wishvel);

		Math3D.VectorCopy(wishvel, wishdir);
		var wishspeed = Math3D.VectorNormalize(wishdir);

		/* clamp to server defined max speed */
		var maxspeed = (pm.s.pm_flags & pmove_t.PMF_DUCKED) != 0 ? pm_duckspeed : pm_maxspeed;

		if (wishspeed > maxspeed)
		{
			Math3D.VectorScale(wishvel, maxspeed / wishspeed, wishvel);
			wishspeed = maxspeed;
		}

		if (pml.ladder)
		{
			PM_Accelerate(wishdir, wishspeed, pm_accelerate);

			if (wishvel[2] == 0)
			{
				if (pml.velocity[2] > 0)
				{
					pml.velocity[2] -= pm.s.gravity * pml.frametime;

					if (pml.velocity[2] < 0)
					{
						pml.velocity[2] = 0;
					}
				}
				else
				{
					pml.velocity[2] += pm.s.gravity * pml.frametime;

					if (pml.velocity[2] > 0)
					{
						pml.velocity[2] = 0;
					}
				}
			}

			PM_StepSlideMove();
		}
		else if (pm.groundentity != null)
		{
			/* walking on ground */
			pml.velocity[2] = 0;
			PM_Accelerate(wishdir, wishspeed, pm_accelerate);

			if (pm.s.gravity > 0)
				pml.velocity[2] = 0;
			else
				pml.velocity[2] -= pm.s.gravity * pml.frametime;

			if (pml.velocity[0] == 0 && pml.velocity[1] == 0)
				return;

			PM_StepSlideMove();
		}
		else
		{
			/* not on ground, so little effect on velocity */
			if (pm_airaccelerate != 0)
				PM_AirAccelerate(wishdir, wishspeed, pm_accelerate);
			else
				PM_Accelerate(wishdir, wishspeed, 1);

			/* add gravity */
			pml.velocity[2] -= pm.s.gravity * pml.frametime;
			PM_StepSlideMove();
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
		point[0] = pml.origin[0];
		point[1] = pml.origin[1];
		point[2] = pml.origin[2] - 0.25f;

		if (pml.velocity[2] > 180)
		{
			pm.s.pm_flags = (byte)(pm.s.pm_flags & ~pmove_t.PMF_ON_GROUND);
			pm.groundentity = null;
		}
		else
		{
			trace = pm.trace(pml.origin, pm.mins, pm.maxs, point);
			pml.groundplane = trace.plane;
			pml.groundsurface = trace.surface;
			pml.groundcontents = trace.contents;

			if (trace.ent == null || (trace.plane.normal[2] < 0.7 && !trace.startsolid))
			{
				pm.groundentity = null;
				pm.s.pm_flags = (byte)(pm.s.pm_flags & ~pmove_t.PMF_ON_GROUND);
			}
			else
			{
				pm.groundentity = trace.ent;

				/* hitting solid ground will end a waterjump */
				if ((pm.s.pm_flags & pmove_t.PMF_TIME_WATERJUMP) != 0)
				{
					pm.s.pm_flags = (byte)(pm.s.pm_flags & ~(pmove_t.PMF_TIME_WATERJUMP | pmove_t.PMF_TIME_LAND | pmove_t.PMF_TIME_TELEPORT));
					pm.s.pm_time = 0;
				}

				if ((pm.s.pm_flags & pmove_t.PMF_ON_GROUND) == 0)
				{
					/* just hit the ground */
					pm.s.pm_flags |= pmove_t.PMF_ON_GROUND;

					/* don't do landing time if we were just going down a slope */
					if (pml.velocity[2] < -200)
					{
						pm.s.pm_flags |= pmove_t.PMF_TIME_LAND;

						/* don't allow another jump for a little while */
						if (pml.velocity[2] < -400)
							pm.s.pm_time = 25;
						else
							pm.s.pm_time = 18;
					}
				}
			}

			if (trace.ent != null)
				pm.touchents.Add(trace.ent);
		}

		/* get waterlevel, accounting for ducking */
		pm.waterlevel = 0;
		pm.watertype = 0;

		sample2 = pm.viewheight - pm.mins[2];
		sample1 = sample2 / 2;

		point[2] = pml.origin[2] + pm.mins[2] + 1;
		cont = pm.pointcontents(point);

		if ((cont & Defines.MASK_WATER) == 0)
			return;

		pm.watertype = cont;
		pm.waterlevel = 1;
		point[2] = pml.origin[2] + pm.mins[2] + sample1;
		cont = pm.pointcontents(point);

		if ((cont & Defines.MASK_WATER) == 0)
			return;

		pm.waterlevel = 2;
		point[2] = pml.origin[2] + pm.mins[2] + sample2;
		cont = pm.pointcontents(point);

		if ((cont & Defines.MASK_WATER) != 0)
			pm.waterlevel = 3;
	}

	public static void PM_CheckJump()
	{
		/* hasn't been long enough since landing to jump again */
		if ((pm.s.pm_flags & pmove_t.PMF_TIME_LAND) != 0)
			return;

		if (pm.cmd.upmove < 10)
		{
			/* not holding jump */
			pm.s.pm_flags = (byte)(pm.s.pm_flags & ~pmove_t.PMF_JUMP_HELD);
			return;
		}

		/* must wait for jump to be released */
		if ((pm.s.pm_flags & pmove_t.PMF_JUMP_HELD) != 0)
			return;

		if (pm.s.pm_type == Defines.PM_DEAD)
			return;

		if (pm.waterlevel >= 2)
		{
			/* swimming, not jumping */
			pm.groundentity = null;

			if (pml.velocity[2] <= -300)
				return;

			if (pm.watertype == Defines.CONTENTS_WATER)
				pml.velocity[2] = 100;
			else if (pm.watertype == Defines.CONTENTS_SLIME)
				pml.velocity[2] = 80;
			else
				pml.velocity[2] = 50;

			return;
		}

		if (pm.groundentity == null)
			return; /* in air, so no effect */

		pm.s.pm_flags |= pmove_t.PMF_JUMP_HELD;

		pm.groundentity = null;
		pml.velocity[2] += 270;

		if (pml.velocity[2] < 270)
			pml.velocity[2] = 270;
	}

	private static void PM_CheckSpecialMovement()
	{
		var spot = new float[3];
		var flatforward = new float[3];

		if (pm.s.pm_time != 0)
			return;

		pml.ladder = false;

		/* check for ladder */
		flatforward[0] = pml.forward[0];
		flatforward[1] = pml.forward[1];
		flatforward[2] = 0;
		Math3D.VectorNormalize(flatforward);

		Math3D.VectorMA(pml.origin, 1, flatforward, spot);
		var trace = pm.trace(pml.origin, pm.mins, pm.maxs, spot);

		if (trace.fraction < 1 && (trace.contents & Defines.CONTENTS_LADDER) != 0)
			pml.ladder = true;

		/* check for water jump */
		if (pm.waterlevel != 2)
			return;

		Math3D.VectorMA(pml.origin, 30, flatforward, spot);
		spot[2] += 4;
		var cont = pm.pointcontents(spot);

		if ((cont & Defines.CONTENTS_SOLID) == 0)
			return;

		spot[2] += 16;
		cont = pm.pointcontents(spot);

		if (cont != 0)
			return;

		/* jump out of water */
		Math3D.VectorScale(flatforward, 50, pml.velocity);
		pml.velocity[2] = 350;

		pm.s.pm_flags |= pmove_t.PMF_TIME_WATERJUMP;
		pm.s.pm_time = 255;
	}

	public static void PM_FlyMove(bool doclip)
	{
		var wishvel = new float[3];
		var wishdir = new float[3];
		var end = new float[3];

		pm.viewheight = 22;

		/* friction */
		var speed = Math3D.VectorLength(pml.velocity);

		if (speed < 1)
			Math3D.VectorCopy(Globals.vec3_origin, pml.velocity);
		else
		{
			float drop = 0;

			var friction = pm_friction * 1.5f;
			var control = speed < pm_stopspeed ? pm_stopspeed : speed;
			drop += control * friction * pml.frametime;

			/* scale the velocity */
			var newspeed = speed - drop;

			if (newspeed < 0)
				newspeed = 0;

			newspeed /= speed;

			Math3D.VectorScale(pml.velocity, newspeed, pml.velocity);
		}

		/* accelerate */
		float fmove = pm.cmd.forwardmove;
		float smove = pm.cmd.sidemove;

		Math3D.VectorNormalize(pml.forward);
		Math3D.VectorNormalize(pml.right);

		for (var i = 0; i < 3; i++)
			wishvel[i] = pml.forward[i] * fmove + pml.right[i] * smove;

		wishvel[2] += pm.cmd.upmove;

		Math3D.VectorCopy(wishvel, wishdir);
		var wishspeed = Math3D.VectorNormalize(wishdir);

		/* clamp to server defined max speed */
		if (wishspeed > pm_maxspeed)
		{
			Math3D.VectorScale(wishvel, pm_maxspeed / wishspeed, wishvel);
			wishspeed = pm_maxspeed;
		}

		var currentspeed = Math3D.DotProduct(pml.velocity, wishdir);
		var addspeed = wishspeed - currentspeed;

		if (addspeed <= 0)
			return;

		var accelspeed = pm_accelerate * pml.frametime * wishspeed;

		if (accelspeed > addspeed)
			accelspeed = addspeed;

		for (var i = 0; i < 3; i++)
			pml.velocity[i] += accelspeed * wishdir[i];

		if (doclip)
		{
			for (var i = 0; i < 3; i++)
				end[i] = pml.origin[i] + pml.frametime * pml.velocity[i];

			var trace = pm.trace(pml.origin, pm.mins, pm.maxs, end);

			Math3D.VectorCopy(trace.endpos, pml.origin);
		}
		else
			Math3D.VectorMA(pml.origin, pml.frametime, pml.velocity, pml.origin); /* move */
	}

	/*
	 * Sets mins, maxs, and pm.viewheight
	 */
	private static void PM_CheckDuck()
	{
		pm.mins[0] = -16;
		pm.mins[1] = -16;

		pm.maxs[0] = 16;
		pm.maxs[1] = 16;

		if (pm.s.pm_type == Defines.PM_GIB)
		{
			pm.mins[2] = 0;
			pm.maxs[2] = 16;
			pm.viewheight = 8;
			return;
		}

		pm.mins[2] = -24;

		if (pm.s.pm_type == Defines.PM_DEAD)
			pm.s.pm_flags |= pmove_t.PMF_DUCKED;
		else if (pm.cmd.upmove < 0 && (pm.s.pm_flags & pmove_t.PMF_ON_GROUND) != 0)
			pm.s.pm_flags |= pmove_t.PMF_DUCKED; /* duck */
		else
		{
			/* stand up if possible */
			if ((pm.s.pm_flags & pmove_t.PMF_DUCKED) != 0)
			{
				/* try to stand up */
				pm.maxs[2] = 32;
				var trace = pm.trace(pml.origin, pm.mins, pm.maxs, pml.origin);

				if (!trace.allsolid)
					pm.s.pm_flags = (byte)(pm.s.pm_flags & ~pmove_t.PMF_DUCKED);
			}
		}

		if ((pm.s.pm_flags & pmove_t.PMF_DUCKED) != 0)
		{
			pm.maxs[2] = 4;
			pm.viewheight = -2;
		}
		else
		{
			pm.maxs[2] = 32;
			pm.viewheight = 22;
		}
	}

	private static void PM_DeadMove()
	{
		if (pm.groundentity == null)
			return;

		/* extra friction */
		var forward = Math3D.VectorLength(pml.velocity);
		forward -= 20;

		if (forward <= 0)
			Math3D.VectorClear(pml.velocity);
		else
		{
			Math3D.VectorNormalize(pml.velocity);
			Math3D.VectorScale(pml.velocity, forward, pml.velocity);
		}
	}

	private static bool PM_GoodPosition()
	{
		trace_t trace;
		var origin = new float[3];
		var end = new float[3];

		if (pm.s.pm_type == Defines.PM_SPECTATOR)
			return true;

		for (var i = 0; i < 3; i++)
			origin[i] = end[i] = pm.s.origin[i] * 0.125f;

		trace = pm.trace(origin, pm.mins, pm.maxs, end);

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
			pm.s.velocity[i] = (short)(pml.velocity[i] * 8);

		for (var i = 0; i < 3; i++)
		{
			if (pml.origin[i] >= 0)
				sign[i] = 1;
			else
				sign[i] = -1;

			pm.s.origin[i] = (short)(pml.origin[i] * 8);

			if (pm.s.origin[i] * 0.125f == pml.origin[i])
				sign[i] = 0;
		}

		Math3D.VectorCopy(pm.s.origin, @base);

		/* try all combinations */
		for (var j = 0; j < 8; j++)
		{
			var bits = jitterbits[j];
			Math3D.VectorCopy(@base, pm.s.origin);

			for (var i = 0; i < 3; i++)
			{
				if ((bits & (1 << i)) != 0)
					pm.s.origin[i] += sign[i];
			}

			if (PM_GoodPosition())
				return;
		}

		/* go back to the last position */
		Math3D.VectorCopy(pml.previous_origin, pm.s.origin);
	}

	private static void PM_InitialSnapPosition()
	{
		var @base = new short[3];
		var offset = new[]{ 0, -1, 1 };

		Math3D.VectorCopy(pm.s.origin, @base);

		for (var z = 0; z < 3; z++)
		{
			pm.s.origin[2] = (short)(@base[2] + offset[z]);

			for (var y = 0; y < 3; y++)
			{
				pm.s.origin[1] = (short)(@base[1] + offset[y]);

				for (var x = 0; x < 3; x++)
				{
					pm.s.origin[0] = (short)(@base[0] + offset[x]);

					if (PM_GoodPosition())
					{
						pml.origin[0] = pm.s.origin[0] * 0.125f;
						pml.origin[1] = pm.s.origin[1] * 0.125f;
						pml.origin[2] = pm.s.origin[2] * 0.125f;
						Math3D.VectorCopy(pm.s.origin, pml.previous_origin);
						return;
					}
				}
			}
		}

		clientserver.Com_DPrintf("Bad InitialSnapPosition\n");
	}

	private static void PM_ClampAngles()
	{
		if ((pm.s.pm_flags & pmove_t.PMF_TIME_TELEPORT) != 0)
		{
			pm.viewangles[Defines.YAW] = Math3D.SHORT2ANGLE(pm.cmd.angles[Defines.YAW] + pm.s.delta_angles[Defines.YAW]);
			pm.viewangles[Defines.PITCH] = 0;
			pm.viewangles[Defines.ROLL] = 0;
		}
		else
		{
			/* circularly clamp the angles with deltas */
			for (var i = 0; i < 3; i++)
			{
				var temp = (short)(pm.cmd.angles[i] + pm.s.delta_angles[i]);
				pm.viewangles[i] = Math3D.SHORT2ANGLE(temp);
			}

			/* don't let the player look up or down more than 90 degrees */
			if ((pm.viewangles[Defines.PITCH] > 89) && (pm.viewangles[Defines.PITCH] < 180))
				pm.viewangles[Defines.PITCH] = 89;
			else if ((pm.viewangles[Defines.PITCH] < 271) && (pm.viewangles[Defines.PITCH] >= 180))
				pm.viewangles[Defines.PITCH] = 271;
		}

		Math3D.AngleVectors(pm.viewangles, pml.forward, pml.right, pml.up);
	}

	private static void PM_CalculateViewHeightForDemo()
	{
		if (pm.s.pm_type == Defines.PM_GIB)
			pm.viewheight = 8;
		else if ((pm.s.pm_flags & pmove_t.PMF_DUCKED) != 0)
			pm.viewheight = -2;
		else
			pm.viewheight = 22;
	}

	private static void PM_CalculateWaterLevelForDemo()
	{
		var point = new float[3];

		point[0] = pml.origin[0];
		point[1] = pml.origin[1];
		point[2] = pml.origin[2] + pm.viewheight;

		pm.waterlevel = 0;
		pm.watertype = 0;

		var cont = pm.pointcontents(point);

		if ((cont & Defines.MASK_WATER) != 0)
		{
			pm.waterlevel = 3;
			pm.watertype = cont;
		}
	}

	private static bool underwater;

	private static void PM_UpdateUnderwaterSfx()
	{
		if (pm != null && pm.waterlevel == 3 && !underwater)
		{
			underwater = true;
			// TODO implement
			/*snd_is_underwater = 1;

			if (snd_is_underwater_enabled)
				AL_Underwater();*/
		}

		if (pm.waterlevel < 3 && underwater)
		{
			underwater = false;
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
		pm = pmove;

		/* clear results */
		Math3D.VectorClear(pm.viewangles);
		pm.viewheight = 0;
		pm.groundentity = null;
		pm.watertype = 0;
		pm.waterlevel = 0;

		/* clear all pmove local vars */
		pml = new pml_t();

		/* convert origin and velocity to float values */
		pml.origin[0] = pm.s.origin[0] * 0.125f;
		pml.origin[1] = pm.s.origin[1] * 0.125f;
		pml.origin[2] = pm.s.origin[2] * 0.125f;

		pml.velocity[0] = pm.s.velocity[0] * 0.125f;
		pml.velocity[1] = pm.s.velocity[1] * 0.125f;
		pml.velocity[2] = pm.s.velocity[2] * 0.125f;

		/* save old org in case we get stuck */
		Math3D.VectorCopy(pm.s.origin, pml.previous_origin);

		pml.frametime = pm.cmd.msec * 0.001f;

		PM_ClampAngles();

		if (pm.s.pm_type == Defines.PM_SPECTATOR)
		{
			PM_FlyMove(false);
			PM_SnapPosition();
			return;
		}

		if (pm.s.pm_type >= Defines.PM_DEAD)
		{
			pm.cmd.forwardmove = 0;
			pm.cmd.sidemove = 0;
			pm.cmd.upmove = 0;
		}

		if (pm.s.pm_type == Defines.PM_FREEZE)
		{
			if (!Globals.DEDICATED_ONLY && Globals.cl.attractloop)
			{
				PM_CalculateViewHeightForDemo();
				PM_CalculateWaterLevelForDemo();
				PM_UpdateUnderwaterSfx();
			}

			return; /* no movement at all */
		}

		/* set mins, maxs, and viewheight */
		PM_CheckDuck();

		if (pm.snapinitial)
			PM_InitialSnapPosition();

		/* set groundentity, watertype, and waterlevel */
		PM_CatagorizePosition();

		if (pm.s.pm_type == Defines.PM_DEAD)
			PM_DeadMove();

		PM_CheckSpecialMovement();

		/* drop timing counter */
		if (pm.s.pm_time != 0)
		{
			var msec = pm.cmd.msec >> 3;

			if (msec == 0)
				msec = 1;

			if (msec >= pm.s.pm_time)
			{
				pm.s.pm_flags = (byte)(pm.s.pm_flags & ~(pmove_t.PMF_TIME_WATERJUMP | pmove_t.PMF_TIME_LAND | pmove_t.PMF_TIME_TELEPORT));
				pm.s.pm_time = 0;
			}
			else
				pm.s.pm_time -= (byte)msec;
		}

		if ((pm.s.pm_flags & pmove_t.PMF_TIME_TELEPORT) != 0)
		{
			/* teleport pause stays exactly in place */
		}
		else if ((pm.s.pm_flags & pmove_t.PMF_TIME_WATERJUMP) != 0)
		{
			/* waterjump has no control, but falls */
			pml.velocity[2] -= pm.s.gravity * pml.frametime;

			if (pml.velocity[2] < 0)
			{
				/* cancel as soon as we are falling down again */
				pm.s.pm_flags = (byte)(pm.s.pm_flags & ~(pmove_t.PMF_TIME_WATERJUMP | pmove_t.PMF_TIME_LAND | pmove_t.PMF_TIME_TELEPORT));
				pm.s.pm_time = 0;
			}

			PM_StepSlideMove();
		}
		else
		{
			PM_CheckJump();

			PM_Friction();

			if (pm.waterlevel >= 2)
				PM_WaterMove();
			else
			{
				var angles = new float[3];

				Math3D.VectorCopy(pm.viewangles, angles);

				if (angles[Defines.PITCH] > 180)
					angles[Defines.PITCH] -= 360;

				angles[Defines.PITCH] /= 3;

				Math3D.AngleVectors(angles, pml.forward, pml.right, pml.up);

				PM_AirMove();
			}
		}

		/* set groundentity, watertype, and waterlevel for final spot */
		PM_CatagorizePosition();

		if (Globals.DEDICATED_ONLY)
			PM_UpdateUnderwaterSfx();

		PM_SnapPosition();
	}
}
