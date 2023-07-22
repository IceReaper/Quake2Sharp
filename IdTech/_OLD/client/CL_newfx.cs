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

using Quake2Sharp.client.types;
using Quake2Sharp.util;
using System.Numerics;

namespace Quake2Sharp.client;

/**
 * CL_newfx
 */
public class CL_newfx
{
	public static void Flashlight(int ent, Vector3 pos)
	{
		CL_newfx.Flashlight(ent, new[] { pos.X, pos.Y, pos.Z });
	}

	public static void Flashlight(int ent, float[] pos)
	{
		CL_fx.cdlight_t dl;
		dl = CL_fx.AllocDlight(ent);
		Math3D.VectorCopy(pos, dl.origin);
		dl.radius = 400;
		dl.minlight = 250;
		dl.die = Globals.cl.time + 100;
		dl.color[0] = 1;
		dl.color[1] = 1;
		dl.color[2] = 1;
	}

	/*
	 * ====== CL_ColorFlash - flash of light ======
	 */
	public static void ColorFlash(Vector3 pos, int ent, int intensity, float r, float g, float b)
	{
		CL_newfx.ColorFlash(new[] { pos.X, pos.Y, pos.Z }, ent, intensity, r, g, b);
	}

	public static void ColorFlash(float[] pos, int ent, int intensity, float r, float g, float b)
	{
		CL_fx.cdlight_t dl;

		if (Globals.vidref_val == Defines.VIDREF_SOFT && (r < 0 || g < 0 || b < 0))
		{
			intensity = -intensity;
			r = -r;
			g = -g;
			b = -b;
		}

		dl = CL_fx.AllocDlight(ent);
		Math3D.VectorCopy(pos, dl.origin);
		dl.radius = intensity;
		dl.minlight = 250;
		dl.die = Globals.cl.time + 100;
		dl.color[0] = r;
		dl.color[1] = g;
		dl.color[2] = b;
	}

	// stack variable
	private static readonly float[] move = { 0, 0, 0 };
	private static readonly float[] vec = { 0, 0, 0 };
	private static readonly float[] right = { 0, 0, 0 };
	private static readonly float[] up = { 0, 0, 0 };

	/*
	 * ====== CL_DebugTrail ======
	 */
	public static void DebugTrail(Vector3 start, Vector3 end)
	{
		CL_newfx.DebugTrail(new[] { start.X, start.Y, start.Z }, new[] { end.X, end.Y, end.Z });
	}

	public static void DebugTrail(float[] start, float[] end)
	{
		float len;

		//		int j;
		cparticle_t p;
		float dec;

		//		int i;
		//		float d, c, s;
		//		float[] dir;
		Math3D.VectorCopy(start, CL_newfx.move);
		Math3D.VectorSubtract(end, start, CL_newfx.vec);
		len = Math3D.VectorNormalize(CL_newfx.vec);
		Math3D.MakeNormalVectors(CL_newfx.vec, CL_newfx.right, CL_newfx.up);

		//		VectorScale(vec, RT2_SKIP, vec);

		//		dec = 1.0;
		//		dec = 0.75;
		dec = 3;
		Math3D.VectorScale(CL_newfx.vec, dec, CL_newfx.vec);
		Math3D.VectorCopy(start, CL_newfx.move);

		while (len > 0)
		{
			len -= dec;

			if (CL_fx.free_particles == null)
				return;

			p = CL_fx.free_particles;
			CL_fx.free_particles = p.next;
			p.next = CL_fx.active_particles;
			CL_fx.active_particles = p;
			p.time = Globals.cl.time;
			Math3D.VectorClear(p.accel);
			Math3D.VectorClear(p.vel);
			p.alpha = 1.0f;
			p.alphavel = -0.1f;

			//			p.alphavel = 0;
			p.color = 0x74 + (Lib.rand() & 7);
			Math3D.VectorCopy(CL_newfx.move, p.org);
			/*
			 * for (j=0 ; j <3 ; j++) { p.org[j] = move[j] + crand()*2; p.vel[j] =
			 * crand()*3; p.accel[j] = 0; }
			 */
			Math3D.VectorAdd(CL_newfx.move, CL_newfx.vec, CL_newfx.move);
		}
	}

	// stack variable
	// move, vec
	public static void ForceWall(Vector3 start, Vector3 end, int color)
	{
		CL_newfx.ForceWall(new[] { start.X, start.Y, start.Z }, new[] { end.X, end.Y, end.Z }, color);
	}

	public static void ForceWall(float[] start, float[] end, int color)
	{
		float len;
		int j;
		cparticle_t p;
		Math3D.VectorCopy(start, CL_newfx.move);
		Math3D.VectorSubtract(end, start, CL_newfx.vec);
		len = Math3D.VectorNormalize(CL_newfx.vec);
		Math3D.VectorScale(CL_newfx.vec, 4, CL_newfx.vec);

		// FIXME: this is a really silly way to have a loop
		while (len > 0)
		{
			len -= 4;

			if (CL_fx.free_particles == null)
				return;

			if (Globals.rnd.NextDouble() > 0.3)
			{
				p = CL_fx.free_particles;
				CL_fx.free_particles = p.next;
				p.next = CL_fx.active_particles;
				CL_fx.active_particles = p;
				Math3D.VectorClear(p.accel);
				p.time = Globals.cl.time;
				p.alpha = 1.0f;
				p.alphavel = -1.0f / (3.0f + (float)Globals.rnd.NextDouble() * 0.5f);
				p.color = color;

				for (j = 0; j < 3; j++)
				{
					p.org[j] = CL_newfx.move[j] + Lib.crand() * 3;
					p.accel[j] = 0;
				}

				p.vel[0] = 0;
				p.vel[1] = 0;
				p.vel[2] = -40 - Lib.crand() * 10;
			}

			Math3D.VectorAdd(CL_newfx.move, CL_newfx.vec, CL_newfx.move);
		}
	}

	// stack variable
	// move, vec
	/*
	 * =============== CL_BubbleTrail2 (lets you control the # of bubbles by
	 * setting the distance between the spawns)
	 *
	 * ===============
	 */
	public static void BubbleTrail2(Vector3 start, Vector3 end, int dist)
	{
		CL_newfx.BubbleTrail2(new[] { start.X, start.Y, start.Z }, new[] { end.X, end.Y, end.Z }, dist);
	}

	public static void BubbleTrail2(float[] start, float[] end, int dist)
	{
		float len;
		int i, j;
		cparticle_t p;
		int dec;
		Math3D.VectorCopy(start, CL_newfx.move);
		Math3D.VectorSubtract(end, start, CL_newfx.vec);
		len = Math3D.VectorNormalize(CL_newfx.vec);
		dec = dist;
		Math3D.VectorScale(CL_newfx.vec, dec, CL_newfx.vec);

		for (i = 0; i < len; i += dec)
		{
			if (CL_fx.free_particles == null)
				return;

			p = CL_fx.free_particles;
			CL_fx.free_particles = p.next;
			p.next = CL_fx.active_particles;
			CL_fx.active_particles = p;
			Math3D.VectorClear(p.accel);
			p.time = Globals.cl.time;
			p.alpha = 1.0f;
			p.alphavel = -1.0f / (1 + (float)Globals.rnd.NextDouble() * 0.1f);
			p.color = 4 + (Lib.rand() & 7);

			for (j = 0; j < 3; j++)
			{
				p.org[j] = CL_newfx.move[j] + Lib.crand() * 2;
				p.vel[j] = Lib.crand() * 10;
			}

			p.org[2] -= 4;

			//			p.vel[2] += 6;
			p.vel[2] += 20;
			Math3D.VectorAdd(CL_newfx.move, CL_newfx.vec, CL_newfx.move);
		}
	}

	// stack variable
	// move, vec, right, up
	private static readonly float[] dir = { 0, 0, 0 };
	private static readonly float[] end = { 0, 0, 0 };

	public static void Heatbeam(float[] start, float[] forward)
	{
		float len;
		int j;
		cparticle_t p;
		int i;
		float c, s;
		float ltime;
		var step = 32;
		float rstep;
		float start_pt;
		float rot;
		float variance;
		Math3D.VectorMA(start, 4096, forward, CL_newfx.end);
		Math3D.VectorCopy(start, CL_newfx.move);
		Math3D.VectorSubtract(CL_newfx.end, start, CL_newfx.vec);
		len = Math3D.VectorNormalize(CL_newfx.vec);

		// FIXME - pmm - these might end up using old values?
		//		MakeNormalVectors (vec, right, up);
		Math3D.VectorCopy(Globals.cl.v_right, CL_newfx.right);
		Math3D.VectorCopy(Globals.cl.v_up, CL_newfx.up);

		if (Globals.vidref_val == Defines.VIDREF_GL)
		{
			// GL mode
			Math3D.VectorMA(CL_newfx.move, -0.5f, CL_newfx.right, CL_newfx.move);
			Math3D.VectorMA(CL_newfx.move, -0.5f, CL_newfx.up, CL_newfx.move);
		}

		// otherwise assume SOFT
		ltime = (float)Globals.cl.time / 1000.0f;
		start_pt = ltime * 96.0f % step;
		Math3D.VectorMA(CL_newfx.move, start_pt, CL_newfx.vec, CL_newfx.move);
		Math3D.VectorScale(CL_newfx.vec, step, CL_newfx.vec);

		//		Com_Printf ("%f\n", ltime);
		rstep = (float)(Math.PI / 10.0);
		var M_PI2 = (float)(Math.PI * 2.0);

		for (i = (int)start_pt; i < len; i += step)
		{
			if (i > step * 5) // don't bother after the 5th ring
				break;

			for (rot = 0; rot < M_PI2; rot += rstep)
			{
				if (CL_fx.free_particles == null)
					return;

				p = CL_fx.free_particles;
				CL_fx.free_particles = p.next;
				p.next = CL_fx.active_particles;
				CL_fx.active_particles = p;
				p.time = Globals.cl.time;
				Math3D.VectorClear(p.accel);

				//				rot+= fmod(ltime, 12.0)*M_PI;
				//				c = cos(rot)/2.0;
				//				s = sin(rot)/2.0;
				//				variance = 0.4 + ((float)rand()/(float)RAND_MAX) *0.2;
				variance = 0.5f;
				c = (float)(Math.Cos(rot) * variance);
				s = (float)(Math.Sin(rot) * variance);

				// trim it so it looks like it's starting at the origin
				if (i < 10)
				{
					Math3D.VectorScale(CL_newfx.right, c * (i / 10.0f), CL_newfx.dir);
					Math3D.VectorMA(CL_newfx.dir, s * (i / 10.0f), CL_newfx.up, CL_newfx.dir);
				}
				else
				{
					Math3D.VectorScale(CL_newfx.right, c, CL_newfx.dir);
					Math3D.VectorMA(CL_newfx.dir, s, CL_newfx.up, CL_newfx.dir);
				}

				p.alpha = 0.5f;

				//		p.alphavel = -1.0 / (1+frand()*0.2);
				p.alphavel = -1000.0f;

				//		p.color = 0x74 + (rand()&7);
				p.color = 223 - (Lib.rand() & 7);

				for (j = 0; j < 3; j++)
				{
					p.org[j] = CL_newfx.move[j] + CL_newfx.dir[j] * 3;

					//			p.vel[j] = dir[j]*6;
					p.vel[j] = 0;
				}
			}

			Math3D.VectorAdd(CL_newfx.move, CL_newfx.vec, CL_newfx.move);
		}
	}

	// stack variable
	private static readonly float[] r = { 0, 0, 0 };
	private static readonly float[] u = { 0, 0, 0 };

	/*
	 * =============== CL_ParticleSteamEffect
	 *
	 * Puffs with velocity along direction, with some randomness thrown in
	 * ===============
	 */
	public static void ParticleSteamEffect(Vector3 org, Vector3 dir, int color, int count, int magnitude)
	{
		CL_newfx.ParticleSteamEffect(new[] { org.X, org.Y, org.Z }, new[] { dir.X, dir.Y, dir.Z }, color, count, magnitude);
	}

	public static void ParticleSteamEffect(float[] org, float[] dir, int color, int count, int magnitude)
	{
		int i, j;
		cparticle_t p;
		float d;

		//		vectoangles2 (dir, angle_dir);
		//		AngleVectors (angle_dir, f, r, u);
		Math3D.MakeNormalVectors(dir, CL_newfx.r, CL_newfx.u);

		for (i = 0; i < count; i++)
		{
			if (CL_fx.free_particles == null)
				return;

			p = CL_fx.free_particles;
			CL_fx.free_particles = p.next;
			p.next = CL_fx.active_particles;
			CL_fx.active_particles = p;
			p.time = Globals.cl.time;
			p.color = color + (Lib.rand() & 7);

			for (j = 0; j < 3; j++)
				p.org[j] = org[j] + magnitude * 0.1f * Lib.crand();

			//				p.vel[j] = dir[j]*magnitude;
			Math3D.VectorScale(dir, magnitude, p.vel);
			d = Lib.crand() * magnitude / 3;
			Math3D.VectorMA(p.vel, d, CL_newfx.r, p.vel);
			d = Lib.crand() * magnitude / 3;
			Math3D.VectorMA(p.vel, d, CL_newfx.u, p.vel);
			p.accel[0] = p.accel[1] = 0;
			p.accel[2] = -CL_fx.PARTICLE_GRAVITY / 2;
			p.alpha = 1.0f;
			p.alphavel = -1.0f / (0.5f + (float)Globals.rnd.NextDouble() * 0.3f);
		}
	}

	// stack variable
	// r, u, dir
	public static void ParticleSteamEffect2(Sustain self)

		//	  float[] org, float[] dir, int color, int count, int magnitude)
	{
		int i, j;
		cparticle_t p;
		float d;

		//		vectoangles2 (dir, angle_dir);
		//		AngleVectors (angle_dir, f, r, u);
		Math3D.VectorCopy(self.Direction, CL_newfx.dir);
		Math3D.MakeNormalVectors(CL_newfx.dir, CL_newfx.r, CL_newfx.u);

		for (i = 0; i < self.Count; i++)
		{
			if (CL_fx.free_particles == null)
				return;

			p = CL_fx.free_particles;
			CL_fx.free_particles = p.next;
			p.next = CL_fx.active_particles;
			CL_fx.active_particles = p;
			p.time = Globals.cl.time;
			p.color = self.Color + (Lib.rand() & 7);

			p.org[0] = self.Position.X + self.Magnitude * 0.1f * Lib.crand();
			p.org[1] = self.Position.Y + self.Magnitude * 0.1f * Lib.crand();
			p.org[2] = self.Position.Z + self.Magnitude * 0.1f * Lib.crand();

			//				p.vel[j] = dir[j]*magnitude;
			Math3D.VectorScale(CL_newfx.dir, self.Magnitude, p.vel);
			d = Lib.crand() * self.Magnitude / 3;
			Math3D.VectorMA(p.vel, d, CL_newfx.r, p.vel);
			d = Lib.crand() * self.Magnitude / 3;
			Math3D.VectorMA(p.vel, d, CL_newfx.u, p.vel);
			p.accel[0] = p.accel[1] = 0;
			p.accel[2] = -CL_fx.PARTICLE_GRAVITY / 2;
			p.alpha = 1.0f;
			p.alphavel = -1.0f / (0.5f + (float)Globals.rnd.NextDouble() * 0.3f);
		}

		self.NextThink += self.ThinkInterval;
	}

	// stack variable
	// move, vec, right, up
	private static readonly float[] forward = { 0, 0, 0 };
	private static readonly float[] angle_dir = { 0, 0, 0 };

	/*
	 * =============== CL_TrackerTrail ===============
	 */
	public static void TrackerTrail(float[] start, float[] end, int particleColor)
	{
		float len;
		cparticle_t p;
		int dec;
		float dist;
		Math3D.VectorCopy(start, CL_newfx.move);
		Math3D.VectorSubtract(end, start, CL_newfx.vec);
		len = Math3D.VectorNormalize(CL_newfx.vec);
		Math3D.VectorCopy(CL_newfx.vec, CL_newfx.forward);
		Math3D.vectoangles(CL_newfx.forward, CL_newfx.angle_dir);
		Math3D.AngleVectors(CL_newfx.angle_dir, CL_newfx.forward, CL_newfx.right, CL_newfx.up);
		dec = 3;
		Math3D.VectorScale(CL_newfx.vec, 3, CL_newfx.vec);

		// FIXME: this is a really silly way to have a loop
		while (len > 0)
		{
			len -= dec;

			if (CL_fx.free_particles == null)
				return;

			p = CL_fx.free_particles;
			CL_fx.free_particles = p.next;
			p.next = CL_fx.active_particles;
			CL_fx.active_particles = p;
			Math3D.VectorClear(p.accel);
			p.time = Globals.cl.time;
			p.alpha = 1.0f;
			p.alphavel = -2.0f;
			p.color = particleColor;
			dist = Math3D.DotProduct(CL_newfx.move, CL_newfx.forward);
			Math3D.VectorMA(CL_newfx.move, (float)(8 * Math.Cos(dist)), CL_newfx.up, p.org);

			for (var j = 0; j < 3; j++)
			{
				p.vel[j] = 0;
				p.accel[j] = 0;
			}

			p.vel[2] = 5;
			Math3D.VectorAdd(CL_newfx.move, CL_newfx.vec, CL_newfx.move);
		}
	}

	// stack variable
	// dir
	public static void Tracker_Shell(float[] origin)
	{
		cparticle_t p;

		for (var i = 0; i < 300; i++)
		{
			if (CL_fx.free_particles == null)
				return;

			p = CL_fx.free_particles;
			CL_fx.free_particles = p.next;
			p.next = CL_fx.active_particles;
			CL_fx.active_particles = p;
			Math3D.VectorClear(p.accel);
			p.time = Globals.cl.time;
			p.alpha = 1.0f;
			p.alphavel = CL_fx.INSTANT_PARTICLE;
			p.color = 0;
			CL_newfx.dir[0] = Lib.crand();
			CL_newfx.dir[1] = Lib.crand();
			CL_newfx.dir[2] = Lib.crand();
			Math3D.VectorNormalize(CL_newfx.dir);
			Math3D.VectorMA(origin, 40, CL_newfx.dir, p.org);
		}
	}

	// stack variable
	// dir
	public static void MonsterPlasma_Shell(Vector3 origin)
	{
		CL_newfx.MonsterPlasma_Shell(new[] { origin.X, origin.Y, origin.Z });
	}

	public static void MonsterPlasma_Shell(float[] origin)
	{
		cparticle_t p;

		for (var i = 0; i < 40; i++)
		{
			if (CL_fx.free_particles == null)
				return;

			p = CL_fx.free_particles;
			CL_fx.free_particles = p.next;
			p.next = CL_fx.active_particles;
			CL_fx.active_particles = p;
			Math3D.VectorClear(p.accel);
			p.time = Globals.cl.time;
			p.alpha = 1.0f;
			p.alphavel = CL_fx.INSTANT_PARTICLE;
			p.color = 0xe0;
			CL_newfx.dir[0] = Lib.crand();
			CL_newfx.dir[1] = Lib.crand();
			CL_newfx.dir[2] = Lib.crand();
			Math3D.VectorNormalize(CL_newfx.dir);
			Math3D.VectorMA(origin, 10, CL_newfx.dir, p.org);

			//			VectorMA(origin, 10*(((rand () & 0x7fff) / ((float)0x7fff))),
			// dir, p.org);
		}
	}

	private static readonly int[] wb_colortable = { 2 * 8, 13 * 8, 21 * 8, 18 * 8 };

	// stack variable
	// dir
	public static void Widowbeamout(Sustain self)
	{
		int i;
		cparticle_t p;
		float ratio;
		ratio = 1.0f - ((float)self.EndTime - (float)Globals.cl.time) / 2100.0f;

		for (i = 0; i < 300; i++)
		{
			if (CL_fx.free_particles == null)
				return;

			p = CL_fx.free_particles;
			CL_fx.free_particles = p.next;
			p.next = CL_fx.active_particles;
			CL_fx.active_particles = p;
			Math3D.VectorClear(p.accel);
			p.time = Globals.cl.time;
			p.alpha = 1.0f;
			p.alphavel = CL_fx.INSTANT_PARTICLE;
			p.color = CL_newfx.wb_colortable[Lib.rand() & 3];
			CL_newfx.dir[0] = Lib.crand();
			CL_newfx.dir[1] = Lib.crand();
			CL_newfx.dir[2] = Lib.crand();
			Math3D.VectorNormalize(CL_newfx.dir);
			Math3D.VectorMA(self.Position, 45.0f * ratio, CL_newfx.dir, p.org);

			//			VectorMA(origin, 10*(((rand () & 0x7fff) / ((float)0x7fff))),
			// dir, p.org);
		}
	}

	private static readonly int[] nb_colortable = { 110, 112, 114, 116 };

	// stack variable
	// dir
	public static void Nukeblast(Sustain self)
	{
		int i;
		cparticle_t p;
		float ratio;
		ratio = 1.0f - ((float)self.EndTime - (float)Globals.cl.time) / 1000.0f;

		for (i = 0; i < 700; i++)
		{
			if (CL_fx.free_particles == null)
				return;

			p = CL_fx.free_particles;
			CL_fx.free_particles = p.next;
			p.next = CL_fx.active_particles;
			CL_fx.active_particles = p;
			Math3D.VectorClear(p.accel);
			p.time = Globals.cl.time;
			p.alpha = 1.0f;
			p.alphavel = CL_fx.INSTANT_PARTICLE;
			p.color = CL_newfx.nb_colortable[Lib.rand() & 3];
			CL_newfx.dir[0] = Lib.crand();
			CL_newfx.dir[1] = Lib.crand();
			CL_newfx.dir[2] = Lib.crand();
			Math3D.VectorNormalize(CL_newfx.dir);
			Math3D.VectorMA(self.Position, 200.0f * ratio, CL_newfx.dir, p.org);

			//			VectorMA(origin, 10*(((rand () & 0x7fff) / ((float)0x7fff))),
			// dir, p.org);
		}
	}

	private static readonly int[] ws_colortable = { 2 * 8, 13 * 8, 21 * 8, 18 * 8 };

	// stack variable
	// dir
	public static void WidowSplash(Vector3 origin)
	{
		CL_newfx.WidowSplash(new[] { origin.X, origin.Y, origin.Z });
	}

	public static void WidowSplash(float[] org)
	{
		int i;
		cparticle_t p;

		for (i = 0; i < 256; i++)
		{
			if (CL_fx.free_particles == null)
				return;

			p = CL_fx.free_particles;
			CL_fx.free_particles = p.next;
			p.next = CL_fx.active_particles;
			CL_fx.active_particles = p;
			p.time = Globals.cl.time;
			p.color = CL_newfx.ws_colortable[Lib.rand() & 3];
			CL_newfx.dir[0] = Lib.crand();
			CL_newfx.dir[1] = Lib.crand();
			CL_newfx.dir[2] = Lib.crand();
			Math3D.VectorNormalize(CL_newfx.dir);
			Math3D.VectorMA(org, 45.0f, CL_newfx.dir, p.org);
			Math3D.VectorMA(Globals.vec3_origin, 40.0f, CL_newfx.dir, p.vel);
			p.accel[0] = p.accel[1] = 0;
			p.alpha = 1.0f;
			p.alphavel = -0.8f / (0.5f + (float)Globals.rnd.NextDouble() * 0.3f);
		}
	}

	// stack variable
	// move, vec
	/*
	 * ===============
	 *  CL_TagTrail
	 * ===============
	 */
	public static void TagTrail(float[] start, float[] end, float color)
	{
		float len;
		int j;
		cparticle_t p;
		int dec;
		Math3D.VectorCopy(start, CL_newfx.move);
		Math3D.VectorSubtract(end, start, CL_newfx.vec);
		len = Math3D.VectorNormalize(CL_newfx.vec);
		dec = 5;
		Math3D.VectorScale(CL_newfx.vec, 5, CL_newfx.vec);

		while (len >= 0)
		{
			len -= dec;

			if (CL_fx.free_particles == null)
				return;

			p = CL_fx.free_particles;
			CL_fx.free_particles = p.next;
			p.next = CL_fx.active_particles;
			CL_fx.active_particles = p;
			Math3D.VectorClear(p.accel);
			p.time = Globals.cl.time;
			p.alpha = 1.0f;
			p.alphavel = -1.0f / (0.8f + (float)Globals.rnd.NextDouble() * 0.2f);
			p.color = color;

			for (j = 0; j < 3; j++)
			{
				p.org[j] = CL_newfx.move[j] + Lib.crand() * 16;
				p.vel[j] = Lib.crand() * 5;
				p.accel[j] = 0;
			}

			Math3D.VectorAdd(CL_newfx.move, CL_newfx.vec, CL_newfx.move);
		}
	}

	/*
	 * =============== CL_ColorExplosionParticles ===============
	 */
	public static void ColorExplosionParticles(Vector3 org, int color, int run)
	{
		CL_newfx.ColorExplosionParticles(new[] { org.X, org.Y, org.Z }, color, run);
	}

	public static void ColorExplosionParticles(float[] org, int color, int run)
	{
		int i, j;
		cparticle_t p;

		for (i = 0; i < 128; i++)
		{
			if (CL_fx.free_particles == null)
				return;

			p = CL_fx.free_particles;
			CL_fx.free_particles = p.next;
			p.next = CL_fx.active_particles;
			CL_fx.active_particles = p;
			p.time = Globals.cl.time;
			p.color = color + Lib.rand() % run;

			for (j = 0; j < 3; j++)
			{
				p.org[j] = org[j] + (Lib.rand() % 32 - 16);
				p.vel[j] = Lib.rand() % 256 - 128;
			}

			p.accel[0] = p.accel[1] = 0;
			p.accel[2] = -CL_fx.PARTICLE_GRAVITY;
			p.alpha = 1.0f;
			p.alphavel = -0.4f / (0.6f + (float)Globals.rnd.NextDouble() * 0.2f);
		}
	}

	// stack variable
	// r, u
	/*
	 * =============== CL_ParticleSmokeEffect - like the steam effect, but
	 * unaffected by gravity ===============
	 */
	public static void ParticleSmokeEffect(Vector3 org, Vector3 dir, int color, int count, int magnitude)
	{
		CL_newfx.ParticleSmokeEffect(new[] { org.X, org.Y, org.Z }, new[] { dir.X, dir.Y, dir.Z }, color, count, magnitude);
	}

	public static void ParticleSmokeEffect(float[] org, float[] dir, int color, int count, int magnitude)
	{
		int i, j;
		cparticle_t p;
		float d;
		Math3D.MakeNormalVectors(dir, CL_newfx.r, CL_newfx.u);

		for (i = 0; i < count; i++)
		{
			if (CL_fx.free_particles == null)
				return;

			p = CL_fx.free_particles;
			CL_fx.free_particles = p.next;
			p.next = CL_fx.active_particles;
			CL_fx.active_particles = p;
			p.time = Globals.cl.time;
			p.color = color + (Lib.rand() & 7);

			for (j = 0; j < 3; j++)
				p.org[j] = org[j] + magnitude * 0.1f * Lib.crand();

			//				p.vel[j] = dir[j]*magnitude;
			Math3D.VectorScale(dir, magnitude, p.vel);
			d = Lib.crand() * magnitude / 3;
			Math3D.VectorMA(p.vel, d, CL_newfx.r, p.vel);
			d = Lib.crand() * magnitude / 3;
			Math3D.VectorMA(p.vel, d, CL_newfx.u, p.vel);
			p.accel[0] = p.accel[1] = p.accel[2] = 0;
			p.alpha = 1.0f;
			p.alphavel = -1.0f / (0.5f + (float)Globals.rnd.NextDouble() * 0.3f);
		}
	}

	/*
	 * =============== CL_BlasterParticles2
	 *
	 * Wall impact puffs (Green) ===============
	 */
	public static void BlasterParticles2(Vector3 org, Vector3 dir, long color)
	{
		CL_newfx.BlasterParticles2(new[] { org.X, org.Y, org.Z }, new[] { dir.X, dir.Y, dir.Z }, color);
	}

	public static void BlasterParticles2(float[] org, float[] dir, long color)
	{
		int i, j;
		cparticle_t p;
		float d;
		int count;
		count = 40;

		for (i = 0; i < count; i++)
		{
			if (CL_fx.free_particles == null)
				return;

			p = CL_fx.free_particles;
			CL_fx.free_particles = p.next;
			p.next = CL_fx.active_particles;
			CL_fx.active_particles = p;
			p.time = Globals.cl.time;
			p.color = color + (Lib.rand() & 7);
			d = Lib.rand() & 15;

			for (j = 0; j < 3; j++)
			{
				p.org[j] = org[j] + ((Lib.rand() & 7) - 4) + d * dir[j];
				p.vel[j] = dir[j] * 30 + Lib.crand() * 40;
			}

			p.accel[0] = p.accel[1] = 0;
			p.accel[2] = -CL_fx.PARTICLE_GRAVITY;
			p.alpha = 1.0f;
			p.alphavel = -1.0f / (0.5f + (float)Globals.rnd.NextDouble() * 0.3f);
		}
	}

	// stack variable
	// move, vec
	/*
	 * =============== CL_BlasterTrail2
	 *
	 * Green! ===============
	 */
	public static void BlasterTrail2(float[] start, float[] end)
	{
		float len;
		int j;
		cparticle_t p;
		int dec;
		Math3D.VectorCopy(start, CL_newfx.move);
		Math3D.VectorSubtract(end, start, CL_newfx.vec);
		len = Math3D.VectorNormalize(CL_newfx.vec);
		dec = 5;
		Math3D.VectorScale(CL_newfx.vec, 5, CL_newfx.vec);

		// FIXME: this is a really silly way to have a loop
		while (len > 0)
		{
			len -= dec;

			if (CL_fx.free_particles == null)
				return;

			p = CL_fx.free_particles;
			CL_fx.free_particles = p.next;
			p.next = CL_fx.active_particles;
			CL_fx.active_particles = p;
			Math3D.VectorClear(p.accel);
			p.time = Globals.cl.time;
			p.alpha = 1.0f;
			p.alphavel = -1.0f / (0.3f + (float)Globals.rnd.NextDouble() * 0.2f);
			p.color = 0xd0;

			for (j = 0; j < 3; j++)
			{
				p.org[j] = CL_newfx.move[j] + Lib.crand();
				p.vel[j] = Lib.crand() * 5;
				p.accel[j] = 0;
			}

			Math3D.VectorAdd(CL_newfx.move, CL_newfx.vec, CL_newfx.move);
		}
	}
}
