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
namespace Quake2Sharp.client
{
	using game.monsters;
	using game.types;
	using qcommon;
	using sound;
	using System;
	using types;
	using util;

	/**
 * Client Graphics Effects.
 */
	public class CL_fx
	{
		public class cdlight_t
		{
			public int key; // so entities can reuse same entry
			public float[] color = { 0, 0, 0 };
			public float[] origin = { 0, 0, 0 };
			public float radius;
			public float die; // stop lighting after this time
			public float minlight; // don't add when contributing less

			public void clear()
			{
				this.radius = this.minlight = this.color[0] = this.color[1] = this.color[2] = 0;
			}
		}

		private static readonly cparticle_t[] particles = new cparticle_t[Defines.MAX_PARTICLES];

		private static void CL_fx1()
		{
			for (var i = 0; i < CL_fx.particles.Length; i++)
				CL_fx.particles[i] = new();
		}

		private static int cl_numparticles = Defines.MAX_PARTICLES;
		public static readonly float INSTANT_PARTICLE = -10000.0f;
		private static readonly float[][] avelocities = new float[Defines.NUMVERTEXNORMALS][];

		private static void CL_fx2()
		{
			for (var i = 0; i < CL_fx.avelocities.Length; i++)
				CL_fx.avelocities[i] = new float[3];
		}

		private static readonly clightstyle_t[] cl_lightstyle = new clightstyle_t[Defines.MAX_LIGHTSTYLES];
		private static int lastofs;

		private static void CL_fx3()
		{
			for (var i = 0; i < CL_fx.cl_lightstyle.Length; i++)
				CL_fx.cl_lightstyle[i] = new();
		}

		/*
		 * ==============================================================
		 * 
		 * DLIGHT MANAGEMENT
		 * 
		 * ==============================================================
		 */
		private static readonly cdlight_t[] cl_dlights = new cdlight_t[Defines.MAX_DLIGHTS];

		private static void CL_fx4()
		{
			for (var i = 0; i < CL_fx.cl_dlights.Length; i++)
				CL_fx.cl_dlights[i] = new();
		}

		/*
		 * ================ CL_ClearDlights ================
		 */
		private static void ClearDlights()
		{
			//		memset (cl_dlights, 0, sizeof(cl_dlights));
			for (var i = 0; i < CL_fx.cl_dlights.Length; i++)
				CL_fx.cl_dlights[i].clear();
		}

		/*
		 * ================ CL_ClearLightStyles ================
		 */
		private static void ClearLightStyles()
		{
			//memset (cl_lightstyle, 0, sizeof(cl_lightstyle));
			for (var i = 0; i < CL_fx.cl_lightstyle.Length; i++)
				CL_fx.cl_lightstyle[i].clear();

			CL_fx.lastofs = -1;
		}

		/*
		 * ================ CL_RunLightStyles ================
		 */
		public static void RunLightStyles()
		{
			clightstyle_t ls;
			var ofs = Globals.cl.time / 100;

			if (ofs == CL_fx.lastofs)
				return;

			CL_fx.lastofs = ofs;

			for (var i = 0; i < CL_fx.cl_lightstyle.Length; i++)
			{
				ls = CL_fx.cl_lightstyle[i];

				if (ls.length == 0)
				{
					ls.value[0] = ls.value[1] = ls.value[2] = 1.0f;

					continue;
				}

				if (ls.length == 1)
					ls.value[0] = ls.value[1] = ls.value[2] = ls.map[0];
				else
					ls.value[0] = ls.value[1] = ls.value[2] = ls.map[ofs % ls.length];
			}
		}

		public static void SetLightstyle(int i)
		{
			string s;
			int j, k;
			s = Globals.cl.configstrings[i + Defines.CS_LIGHTS];
			j = s.Length;

			if (j >= Defines.MAX_QPATH)
				Com.Error(Defines.ERR_DROP, "svc_lightstyle length=" + j);

			CL_fx.cl_lightstyle[i].length = j;

			for (k = 0; k < j; k++)
				CL_fx.cl_lightstyle[i].map[k] = (float)(s[k] - 'a') / (float)('m' - 'a');
		}

		/*
		 * ================ CL_AddLightStyles ================
		 */
		public static void AddLightStyles()
		{
			clightstyle_t ls;

			for (var i = 0; i < CL_fx.cl_lightstyle.Length; i++)
			{
				ls = CL_fx.cl_lightstyle[i];
				V.AddLightStyle(i, ls.value[0], ls.value[1], ls.value[2]);
			}
		}

		/*
		 * =============== CL_AllocDlight
		 * 
		 * ===============
		 */
		public static cdlight_t AllocDlight(int key)
		{
			int i;
			cdlight_t dl;

			//	   first look for an exact key match
			if (key != 0)
			{
				for (i = 0; i < Defines.MAX_DLIGHTS; i++)
				{
					dl = CL_fx.cl_dlights[i];

					if (dl.key == key)
					{
						//memset (dl, 0, sizeof(*dl));
						dl.clear();
						dl.key = key;

						return dl;
					}
				}
			}

			//	   then look for anything else
			for (i = 0; i < Defines.MAX_DLIGHTS; i++)
			{
				dl = CL_fx.cl_dlights[i];

				if (dl.die < Globals.cl.time)
				{
					//memset (dl, 0, sizeof(*dl));
					dl.clear();
					dl.key = key;

					return dl;
				}
			}

			//dl = &cl_dlights[0];
			//memset (dl, 0, sizeof(*dl));
			dl = CL_fx.cl_dlights[0];
			dl.clear();
			dl.key = key;

			return dl;
		}

		/*
		 * =============== 
		 * CL_RunDLights
		 * ===============
		 */
		public static void RunDLights()
		{
			cdlight_t dl;

			for (var i = 0; i < Defines.MAX_DLIGHTS; i++)
			{
				dl = CL_fx.cl_dlights[i];

				if (dl.radius == 0.0f)
					continue;

				if (dl.die < Globals.cl.time)
				{
					dl.radius = 0.0f;

					return;
				}
			}
		}

		// stack variable
		private static readonly float[] fv = { 0, 0, 0 };
		private static readonly float[] rv = { 0, 0, 0 };

		/*
		 * ==============
		 *  CL_ParseMuzzleFlash
		 * ==============
		 */
		public static void ParseMuzzleFlash()
		{
			float volume;
			string soundname;
			int i = MSG.ReadShort(Globals.net_message);

			if (i < 1 || i >= Defines.MAX_EDICTS)
				Com.Error(Defines.ERR_DROP, "CL_ParseMuzzleFlash: bad entity");

			var weapon = MSG.ReadByte(Globals.net_message);
			var silenced = weapon & Defines.MZ_SILENCED;
			weapon &= ~Defines.MZ_SILENCED;
			var pl = Globals.cl_entities[i];
			var dl = CL_fx.AllocDlight(i);
			Math3D.VectorCopy(pl.current.origin, dl.origin);
			Math3D.AngleVectors(pl.current.angles, CL_fx.fv, CL_fx.rv, null);
			Math3D.VectorMA(dl.origin, 18, CL_fx.fv, dl.origin);
			Math3D.VectorMA(dl.origin, 16, CL_fx.rv, dl.origin);

			if (silenced != 0)
				dl.radius = 100 + (Globals.rnd.Next() & 31);
			else
				dl.radius = 200 + (Globals.rnd.Next() & 31);

			dl.minlight = 32;
			dl.die = Globals.cl.time; // + 0.1;

			if (silenced != 0)
				volume = 0.2f;
			else
				volume = 1;

			switch (weapon)
			{
				case Defines.MZ_BLASTER:
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound("weapons/blastf1a.wav"), volume, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ_BLUEHYPERBLASTER:
					dl.color[0] = 0;
					dl.color[1] = 0;
					dl.color[2] = 1;
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound("weapons/hyprbf1a.wav"), volume, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ_HYPERBLASTER:
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound("weapons/hyprbf1a.wav"), volume, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ_MACHINEGUN:
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;

					//Com_sprintf(soundname, sizeof(soundname),
					// "weapons/machgf%ib.wav", (rand() % 5) + 1);
					soundname = "weapons/machgf" + (Globals.rnd.Next(5) + 1) + "b.wav";
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound(soundname), volume, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ_SHOTGUN:
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound("weapons/shotgf1b.wav"), volume, Defines.ATTN_NORM, 0);
					S.StartSound(null, i, Defines.CHAN_AUTO, S.RegisterSound("weapons/shotgr1b.wav"), volume, Defines.ATTN_NORM, 0.1f);

					break;

				case Defines.MZ_SSHOTGUN:
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound("weapons/sshotf1b.wav"), volume, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ_CHAINGUN1:
					dl.radius = 200 + (Globals.rnd.Next() & 31);
					dl.color[0] = 1;
					dl.color[1] = 0.25f;
					dl.color[2] = 0;

					//Com_sprintf(soundname, sizeof(soundname),
					// "weapons/machgf%ib.wav", (rand() % 5) + 1);
					soundname = "weapons/machgf" + (Globals.rnd.Next(5) + 1) + "b.wav";
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound(soundname), volume, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ_CHAINGUN2:
					dl.radius = 225 + (Globals.rnd.Next() & 31);
					dl.color[0] = 1;
					dl.color[1] = 0.5f;
					dl.color[2] = 0;
					dl.die = Globals.cl.time + 0.1f; // long delay

					//Com_sprintf(soundname, sizeof(soundname),
					// "weapons/machgf%ib.wav", (rand() % 5) + 1);
					soundname = "weapons/machgf" + (Globals.rnd.Next(5) + 1) + "b.wav";
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound(soundname), volume, Defines.ATTN_NORM, 0);

					//Com_sprintf(soundname, sizeof(soundname),
					// "weapons/machgf%ib.wav", (rand() % 5) + 1);
					soundname = "weapons/machgf" + (Globals.rnd.Next(5) + 1) + "b.wav";
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound(soundname), volume, Defines.ATTN_NORM, 0.05f);

					break;

				case Defines.MZ_CHAINGUN3:
					dl.radius = 250 + (Globals.rnd.Next() & 31);
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					dl.die = Globals.cl.time + 0.1f; // long delay

					//Com_sprintf(soundname, sizeof(soundname),
					// "weapons/machgf%ib.wav", (rand() % 5) + 1);
					soundname = "weapons/machgf" + (Globals.rnd.Next(5) + 1) + "b.wav";
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound(soundname), volume, Defines.ATTN_NORM, 0);

					//Com_sprintf(soundname, sizeof(soundname),
					// "weapons/machgf%ib.wav", (rand() % 5) + 1);
					soundname = "weapons/machgf" + (Globals.rnd.Next(5) + 1) + "b.wav";
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound(soundname), volume, Defines.ATTN_NORM, 0.033f);

					//Com_sprintf(soundname, sizeof(soundname),
					// "weapons/machgf%ib.wav", (rand() % 5) + 1);
					soundname = "weapons/machgf" + (Globals.rnd.Next(5) + 1) + "b.wav";
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound(soundname), volume, Defines.ATTN_NORM, 0.066f);

					break;

				case Defines.MZ_RAILGUN:
					dl.color[0] = 0.5f;
					dl.color[1] = 0.5f;
					dl.color[2] = 1.0f;
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound("weapons/railgf1a.wav"), volume, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ_ROCKET:
					dl.color[0] = 1;
					dl.color[1] = 0.5f;
					dl.color[2] = 0.2f;
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound("weapons/rocklf1a.wav"), volume, Defines.ATTN_NORM, 0);
					S.StartSound(null, i, Defines.CHAN_AUTO, S.RegisterSound("weapons/rocklr1b.wav"), volume, Defines.ATTN_NORM, 0.1f);

					break;

				case Defines.MZ_GRENADE:
					dl.color[0] = 1;
					dl.color[1] = 0.5f;
					dl.color[2] = 0;
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound("weapons/grenlf1a.wav"), volume, Defines.ATTN_NORM, 0);
					S.StartSound(null, i, Defines.CHAN_AUTO, S.RegisterSound("weapons/grenlr1b.wav"), volume, Defines.ATTN_NORM, 0.1f);

					break;

				case Defines.MZ_BFG:
					dl.color[0] = 0;
					dl.color[1] = 1;
					dl.color[2] = 0;
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound("weapons/bfg__f1y.wav"), volume, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ_LOGIN:
					dl.color[0] = 0;
					dl.color[1] = 1;
					dl.color[2] = 0;
					dl.die = Globals.cl.time + 1.0f;
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound("weapons/grenlf1a.wav"), 1, Defines.ATTN_NORM, 0);
					CL_fx.LogoutEffect(pl.current.origin, weapon);

					break;

				case Defines.MZ_LOGOUT:
					dl.color[0] = 1;
					dl.color[1] = 0;
					dl.color[2] = 0;
					dl.die = Globals.cl.time + 1.0f;
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound("weapons/grenlf1a.wav"), 1, Defines.ATTN_NORM, 0);
					CL_fx.LogoutEffect(pl.current.origin, weapon);

					break;

				case Defines.MZ_RESPAWN:
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					dl.die = Globals.cl.time + 1.0f;
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound("weapons/grenlf1a.wav"), 1, Defines.ATTN_NORM, 0);
					CL_fx.LogoutEffect(pl.current.origin, weapon);

					break;

				// RAFAEL
				case Defines.MZ_PHALANX:
					dl.color[0] = 1;
					dl.color[1] = 0.5f;
					dl.color[2] = 0.5f;
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound("weapons/plasshot.wav"), volume, Defines.ATTN_NORM, 0);

					break;

				// RAFAEL
				case Defines.MZ_IONRIPPER:
					dl.color[0] = 1;
					dl.color[1] = 0.5f;
					dl.color[2] = 0.5f;
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound("weapons/rippfire.wav"), volume, Defines.ATTN_NORM, 0);

					break;

				//	   ======================
				//	   PGM
				case Defines.MZ_ETF_RIFLE:
					dl.color[0] = 0.9f;
					dl.color[1] = 0.7f;
					dl.color[2] = 0;
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound("weapons/nail1.wav"), volume, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ_SHOTGUN2:
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound("weapons/shotg2.wav"), volume, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ_HEATBEAM:
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					dl.die = Globals.cl.time + 100;

					//			S.StartSound (null, i, CHAN_WEAPON,
					// S.RegisterSound("weapons/bfg__l1a.wav"), volume, ATTN_NORM, 0);
					break;

				case Defines.MZ_BLASTER2:
					dl.color[0] = 0;
					dl.color[1] = 1;
					dl.color[2] = 0;

					// FIXME - different sound for blaster2 ??
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound("weapons/blastf1a.wav"), volume, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ_TRACKER:
					// negative flashes handled the same in gl/soft until CL_AddDLights
					dl.color[0] = -1;
					dl.color[1] = -1;
					dl.color[2] = -1;
					S.StartSound(null, i, Defines.CHAN_WEAPON, S.RegisterSound("weapons/disint2.wav"), volume, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ_NUKE1:
					dl.color[0] = 1;
					dl.color[1] = 0;
					dl.color[2] = 0;
					dl.die = Globals.cl.time + 100;

					break;

				case Defines.MZ_NUKE2:
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					dl.die = Globals.cl.time + 100;

					break;

				case Defines.MZ_NUKE4:
					dl.color[0] = 0;
					dl.color[1] = 0;
					dl.color[2] = 1;
					dl.die = Globals.cl.time + 100;

					break;

				case Defines.MZ_NUKE8:
					dl.color[0] = 0;
					dl.color[1] = 1;
					dl.color[2] = 1;
					dl.die = Globals.cl.time + 100;

					break;

				//	   PGM
				//	   ======================
			}
		}

		// stack variable
		private static readonly float[] origin = { 0, 0, 0 };
		private static readonly float[] forward = { 0, 0, 0 };
		private static readonly float[] right = { 0, 0, 0 };

		/*
		 * ============== CL_ParseMuzzleFlash2 ==============
		 */
		public static void ParseMuzzleFlash2()
		{
			string soundname;
			int ent = MSG.ReadShort(Globals.net_message);

			if (ent < 1 || ent >= Defines.MAX_EDICTS)
				Com.Error(Defines.ERR_DROP, "CL_ParseMuzzleFlash2: bad entity");

			var flash_number = MSG.ReadByte(Globals.net_message);

			// locate the origin
			Math3D.AngleVectors(Globals.cl_entities[ent].current.angles, CL_fx.forward, CL_fx.right, null);

			CL_fx.origin[0] = Globals.cl_entities[ent].current.origin[0]
				+ CL_fx.forward[0] * M_Flash.monster_flash_offset[flash_number][0]
				+ CL_fx.right[0] * M_Flash.monster_flash_offset[flash_number][1];

			CL_fx.origin[1] = Globals.cl_entities[ent].current.origin[1]
				+ CL_fx.forward[1] * M_Flash.monster_flash_offset[flash_number][0]
				+ CL_fx.right[1] * M_Flash.monster_flash_offset[flash_number][1];

			CL_fx.origin[2] = Globals.cl_entities[ent].current.origin[2]
				+ CL_fx.forward[2] * M_Flash.monster_flash_offset[flash_number][0]
				+ CL_fx.right[2] * M_Flash.monster_flash_offset[flash_number][1]
				+ M_Flash.monster_flash_offset[flash_number][2];

			var dl = CL_fx.AllocDlight(ent);
			Math3D.VectorCopy(CL_fx.origin, dl.origin);
			dl.radius = 200 + (Globals.rnd.Next() & 31);
			dl.minlight = 32;
			dl.die = Globals.cl.time; // + 0.1;

			switch (flash_number)
			{
				case Defines.MZ2_INFANTRY_MACHINEGUN_1:
				case Defines.MZ2_INFANTRY_MACHINEGUN_2:
				case Defines.MZ2_INFANTRY_MACHINEGUN_3:
				case Defines.MZ2_INFANTRY_MACHINEGUN_4:
				case Defines.MZ2_INFANTRY_MACHINEGUN_5:
				case Defines.MZ2_INFANTRY_MACHINEGUN_6:
				case Defines.MZ2_INFANTRY_MACHINEGUN_7:
				case Defines.MZ2_INFANTRY_MACHINEGUN_8:
				case Defines.MZ2_INFANTRY_MACHINEGUN_9:
				case Defines.MZ2_INFANTRY_MACHINEGUN_10:
				case Defines.MZ2_INFANTRY_MACHINEGUN_11:
				case Defines.MZ2_INFANTRY_MACHINEGUN_12:
				case Defines.MZ2_INFANTRY_MACHINEGUN_13:
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					CL_fx.ParticleEffect(CL_fx.origin, Globals.vec3_origin, 0, 40);
					CL_tent.SmokeAndFlash(CL_fx.origin);
					S.StartSound(null, ent, Defines.CHAN_WEAPON, S.RegisterSound("infantry/infatck1.wav"), 1, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ2_SOLDIER_MACHINEGUN_1:
				case Defines.MZ2_SOLDIER_MACHINEGUN_2:
				case Defines.MZ2_SOLDIER_MACHINEGUN_3:
				case Defines.MZ2_SOLDIER_MACHINEGUN_4:
				case Defines.MZ2_SOLDIER_MACHINEGUN_5:
				case Defines.MZ2_SOLDIER_MACHINEGUN_6:
				case Defines.MZ2_SOLDIER_MACHINEGUN_7:
				case Defines.MZ2_SOLDIER_MACHINEGUN_8:
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					CL_fx.ParticleEffect(CL_fx.origin, Globals.vec3_origin, 0, 40);
					CL_tent.SmokeAndFlash(CL_fx.origin);
					S.StartSound(null, ent, Defines.CHAN_WEAPON, S.RegisterSound("soldier/solatck3.wav"), 1, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ2_GUNNER_MACHINEGUN_1:
				case Defines.MZ2_GUNNER_MACHINEGUN_2:
				case Defines.MZ2_GUNNER_MACHINEGUN_3:
				case Defines.MZ2_GUNNER_MACHINEGUN_4:
				case Defines.MZ2_GUNNER_MACHINEGUN_5:
				case Defines.MZ2_GUNNER_MACHINEGUN_6:
				case Defines.MZ2_GUNNER_MACHINEGUN_7:
				case Defines.MZ2_GUNNER_MACHINEGUN_8:
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					CL_fx.ParticleEffect(CL_fx.origin, Globals.vec3_origin, 0, 40);
					CL_tent.SmokeAndFlash(CL_fx.origin);
					S.StartSound(null, ent, Defines.CHAN_WEAPON, S.RegisterSound("gunner/gunatck2.wav"), 1, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ2_ACTOR_MACHINEGUN_1:
				case Defines.MZ2_SUPERTANK_MACHINEGUN_1:
				case Defines.MZ2_SUPERTANK_MACHINEGUN_2:
				case Defines.MZ2_SUPERTANK_MACHINEGUN_3:
				case Defines.MZ2_SUPERTANK_MACHINEGUN_4:
				case Defines.MZ2_SUPERTANK_MACHINEGUN_5:
				case Defines.MZ2_SUPERTANK_MACHINEGUN_6:
				case Defines.MZ2_TURRET_MACHINEGUN: // PGM
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					CL_fx.ParticleEffect(CL_fx.origin, Globals.vec3_origin, 0, 40);
					CL_tent.SmokeAndFlash(CL_fx.origin);
					S.StartSound(null, ent, Defines.CHAN_WEAPON, S.RegisterSound("infantry/infatck1.wav"), 1, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ2_BOSS2_MACHINEGUN_L1:
				case Defines.MZ2_BOSS2_MACHINEGUN_L2:
				case Defines.MZ2_BOSS2_MACHINEGUN_L3:
				case Defines.MZ2_BOSS2_MACHINEGUN_L4:
				case Defines.MZ2_BOSS2_MACHINEGUN_L5:
				case Defines.MZ2_CARRIER_MACHINEGUN_L1: // PMM
				case Defines.MZ2_CARRIER_MACHINEGUN_L2: // PMM
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					CL_fx.ParticleEffect(CL_fx.origin, Globals.vec3_origin, 0, 40);
					CL_tent.SmokeAndFlash(CL_fx.origin);
					S.StartSound(null, ent, Defines.CHAN_WEAPON, S.RegisterSound("infantry/infatck1.wav"), 1, Defines.ATTN_NONE, 0);

					break;

				case Defines.MZ2_SOLDIER_BLASTER_1:
				case Defines.MZ2_SOLDIER_BLASTER_2:
				case Defines.MZ2_SOLDIER_BLASTER_3:
				case Defines.MZ2_SOLDIER_BLASTER_4:
				case Defines.MZ2_SOLDIER_BLASTER_5:
				case Defines.MZ2_SOLDIER_BLASTER_6:
				case Defines.MZ2_SOLDIER_BLASTER_7:
				case Defines.MZ2_SOLDIER_BLASTER_8:
				case Defines.MZ2_TURRET_BLASTER: // PGM
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					S.StartSound(null, ent, Defines.CHAN_WEAPON, S.RegisterSound("soldier/solatck2.wav"), 1, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ2_FLYER_BLASTER_1:
				case Defines.MZ2_FLYER_BLASTER_2:
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					S.StartSound(null, ent, Defines.CHAN_WEAPON, S.RegisterSound("flyer/flyatck3.wav"), 1, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ2_MEDIC_BLASTER_1:
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					S.StartSound(null, ent, Defines.CHAN_WEAPON, S.RegisterSound("medic/medatck1.wav"), 1, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ2_HOVER_BLASTER_1:
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					S.StartSound(null, ent, Defines.CHAN_WEAPON, S.RegisterSound("hover/hovatck1.wav"), 1, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ2_FLOAT_BLASTER_1:
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					S.StartSound(null, ent, Defines.CHAN_WEAPON, S.RegisterSound("floater/fltatck1.wav"), 1, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ2_SOLDIER_SHOTGUN_1:
				case Defines.MZ2_SOLDIER_SHOTGUN_2:
				case Defines.MZ2_SOLDIER_SHOTGUN_3:
				case Defines.MZ2_SOLDIER_SHOTGUN_4:
				case Defines.MZ2_SOLDIER_SHOTGUN_5:
				case Defines.MZ2_SOLDIER_SHOTGUN_6:
				case Defines.MZ2_SOLDIER_SHOTGUN_7:
				case Defines.MZ2_SOLDIER_SHOTGUN_8:
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					CL_tent.SmokeAndFlash(CL_fx.origin);
					S.StartSound(null, ent, Defines.CHAN_WEAPON, S.RegisterSound("soldier/solatck1.wav"), 1, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ2_TANK_BLASTER_1:
				case Defines.MZ2_TANK_BLASTER_2:
				case Defines.MZ2_TANK_BLASTER_3:
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					S.StartSound(null, ent, Defines.CHAN_WEAPON, S.RegisterSound("tank/tnkatck3.wav"), 1, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ2_TANK_MACHINEGUN_1:
				case Defines.MZ2_TANK_MACHINEGUN_2:
				case Defines.MZ2_TANK_MACHINEGUN_3:
				case Defines.MZ2_TANK_MACHINEGUN_4:
				case Defines.MZ2_TANK_MACHINEGUN_5:
				case Defines.MZ2_TANK_MACHINEGUN_6:
				case Defines.MZ2_TANK_MACHINEGUN_7:
				case Defines.MZ2_TANK_MACHINEGUN_8:
				case Defines.MZ2_TANK_MACHINEGUN_9:
				case Defines.MZ2_TANK_MACHINEGUN_10:
				case Defines.MZ2_TANK_MACHINEGUN_11:
				case Defines.MZ2_TANK_MACHINEGUN_12:
				case Defines.MZ2_TANK_MACHINEGUN_13:
				case Defines.MZ2_TANK_MACHINEGUN_14:
				case Defines.MZ2_TANK_MACHINEGUN_15:
				case Defines.MZ2_TANK_MACHINEGUN_16:
				case Defines.MZ2_TANK_MACHINEGUN_17:
				case Defines.MZ2_TANK_MACHINEGUN_18:
				case Defines.MZ2_TANK_MACHINEGUN_19:
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					CL_fx.ParticleEffect(CL_fx.origin, Globals.vec3_origin, 0, 40);
					CL_tent.SmokeAndFlash(CL_fx.origin);

					//Com_sprintf(soundname, sizeof(soundname), "tank/tnkatk2%c.wav",
					// 'a' + rand() % 5);
					soundname = "tank/tnkatk2" + (char)('a' + Globals.rnd.Next(5)) + ".wav";
					S.StartSound(null, ent, Defines.CHAN_WEAPON, S.RegisterSound(soundname), 1, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ2_CHICK_ROCKET_1:
				case Defines.MZ2_TURRET_ROCKET: // PGM
					dl.color[0] = 1;
					dl.color[1] = 0.5f;
					dl.color[2] = 0.2f;
					S.StartSound(null, ent, Defines.CHAN_WEAPON, S.RegisterSound("chick/chkatck2.wav"), 1, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ2_TANK_ROCKET_1:
				case Defines.MZ2_TANK_ROCKET_2:
				case Defines.MZ2_TANK_ROCKET_3:
					dl.color[0] = 1;
					dl.color[1] = 0.5f;
					dl.color[2] = 0.2f;
					S.StartSound(null, ent, Defines.CHAN_WEAPON, S.RegisterSound("tank/tnkatck1.wav"), 1, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ2_SUPERTANK_ROCKET_1:
				case Defines.MZ2_SUPERTANK_ROCKET_2:
				case Defines.MZ2_SUPERTANK_ROCKET_3:
				case Defines.MZ2_BOSS2_ROCKET_1:
				case Defines.MZ2_BOSS2_ROCKET_2:
				case Defines.MZ2_BOSS2_ROCKET_3:
				case Defines.MZ2_BOSS2_ROCKET_4:
				case Defines.MZ2_CARRIER_ROCKET_1:
					//		case MZ2_CARRIER_ROCKET_2:
					//		case MZ2_CARRIER_ROCKET_3:
					//		case MZ2_CARRIER_ROCKET_4:
					dl.color[0] = 1;
					dl.color[1] = 0.5f;
					dl.color[2] = 0.2f;
					S.StartSound(null, ent, Defines.CHAN_WEAPON, S.RegisterSound("tank/rocket.wav"), 1, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ2_GUNNER_GRENADE_1:
				case Defines.MZ2_GUNNER_GRENADE_2:
				case Defines.MZ2_GUNNER_GRENADE_3:
				case Defines.MZ2_GUNNER_GRENADE_4:
					dl.color[0] = 1;
					dl.color[1] = 0.5f;
					dl.color[2] = 0;
					S.StartSound(null, ent, Defines.CHAN_WEAPON, S.RegisterSound("gunner/gunatck3.wav"), 1, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ2_GLADIATOR_RAILGUN_1:
				// PMM
				case Defines.MZ2_CARRIER_RAILGUN:
				case Defines.MZ2_WIDOW_RAIL:
					// pmm
					dl.color[0] = 0.5f;
					dl.color[1] = 0.5f;
					dl.color[2] = 1.0f;

					break;

				//	   --- Xian's shit starts ---
				case Defines.MZ2_MAKRON_BFG:
					dl.color[0] = 0.5f;
					dl.color[1] = 1;
					dl.color[2] = 0.5f;

					//S.StartSound (null, ent, CHAN_WEAPON,
					// S.RegisterSound("makron/bfg_fire.wav"), 1, ATTN_NORM, 0);
					break;

				case Defines.MZ2_MAKRON_BLASTER_1:
				case Defines.MZ2_MAKRON_BLASTER_2:
				case Defines.MZ2_MAKRON_BLASTER_3:
				case Defines.MZ2_MAKRON_BLASTER_4:
				case Defines.MZ2_MAKRON_BLASTER_5:
				case Defines.MZ2_MAKRON_BLASTER_6:
				case Defines.MZ2_MAKRON_BLASTER_7:
				case Defines.MZ2_MAKRON_BLASTER_8:
				case Defines.MZ2_MAKRON_BLASTER_9:
				case Defines.MZ2_MAKRON_BLASTER_10:
				case Defines.MZ2_MAKRON_BLASTER_11:
				case Defines.MZ2_MAKRON_BLASTER_12:
				case Defines.MZ2_MAKRON_BLASTER_13:
				case Defines.MZ2_MAKRON_BLASTER_14:
				case Defines.MZ2_MAKRON_BLASTER_15:
				case Defines.MZ2_MAKRON_BLASTER_16:
				case Defines.MZ2_MAKRON_BLASTER_17:
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					S.StartSound(null, ent, Defines.CHAN_WEAPON, S.RegisterSound("makron/blaster.wav"), 1, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ2_JORG_MACHINEGUN_L1:
				case Defines.MZ2_JORG_MACHINEGUN_L2:
				case Defines.MZ2_JORG_MACHINEGUN_L3:
				case Defines.MZ2_JORG_MACHINEGUN_L4:
				case Defines.MZ2_JORG_MACHINEGUN_L5:
				case Defines.MZ2_JORG_MACHINEGUN_L6:
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					CL_fx.ParticleEffect(CL_fx.origin, Globals.vec3_origin, 0, 40);
					CL_tent.SmokeAndFlash(CL_fx.origin);
					S.StartSound(null, ent, Defines.CHAN_WEAPON, S.RegisterSound("boss3/xfire.wav"), 1, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ2_JORG_MACHINEGUN_R1:
				case Defines.MZ2_JORG_MACHINEGUN_R2:
				case Defines.MZ2_JORG_MACHINEGUN_R3:
				case Defines.MZ2_JORG_MACHINEGUN_R4:
				case Defines.MZ2_JORG_MACHINEGUN_R5:
				case Defines.MZ2_JORG_MACHINEGUN_R6:
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					CL_fx.ParticleEffect(CL_fx.origin, Globals.vec3_origin, 0, 40);
					CL_tent.SmokeAndFlash(CL_fx.origin);

					break;

				case Defines.MZ2_JORG_BFG_1:
					dl.color[0] = 0.5f;
					dl.color[1] = 1;
					dl.color[2] = 0.5f;

					break;

				case Defines.MZ2_BOSS2_MACHINEGUN_R1:
				case Defines.MZ2_BOSS2_MACHINEGUN_R2:
				case Defines.MZ2_BOSS2_MACHINEGUN_R3:
				case Defines.MZ2_BOSS2_MACHINEGUN_R4:
				case Defines.MZ2_BOSS2_MACHINEGUN_R5:
				case Defines.MZ2_CARRIER_MACHINEGUN_R1: // PMM
				case Defines.MZ2_CARRIER_MACHINEGUN_R2: // PMM
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					CL_fx.ParticleEffect(CL_fx.origin, Globals.vec3_origin, 0, 40);
					CL_tent.SmokeAndFlash(CL_fx.origin);

					break;

				//	   ======
				//	   ROGUE
				case Defines.MZ2_STALKER_BLASTER:
				case Defines.MZ2_DAEDALUS_BLASTER:
				case Defines.MZ2_MEDIC_BLASTER_2:
				case Defines.MZ2_WIDOW_BLASTER:
				case Defines.MZ2_WIDOW_BLASTER_SWEEP1:
				case Defines.MZ2_WIDOW_BLASTER_SWEEP2:
				case Defines.MZ2_WIDOW_BLASTER_SWEEP3:
				case Defines.MZ2_WIDOW_BLASTER_SWEEP4:
				case Defines.MZ2_WIDOW_BLASTER_SWEEP5:
				case Defines.MZ2_WIDOW_BLASTER_SWEEP6:
				case Defines.MZ2_WIDOW_BLASTER_SWEEP7:
				case Defines.MZ2_WIDOW_BLASTER_SWEEP8:
				case Defines.MZ2_WIDOW_BLASTER_SWEEP9:
				case Defines.MZ2_WIDOW_BLASTER_100:
				case Defines.MZ2_WIDOW_BLASTER_90:
				case Defines.MZ2_WIDOW_BLASTER_80:
				case Defines.MZ2_WIDOW_BLASTER_70:
				case Defines.MZ2_WIDOW_BLASTER_60:
				case Defines.MZ2_WIDOW_BLASTER_50:
				case Defines.MZ2_WIDOW_BLASTER_40:
				case Defines.MZ2_WIDOW_BLASTER_30:
				case Defines.MZ2_WIDOW_BLASTER_20:
				case Defines.MZ2_WIDOW_BLASTER_10:
				case Defines.MZ2_WIDOW_BLASTER_0:
				case Defines.MZ2_WIDOW_BLASTER_10L:
				case Defines.MZ2_WIDOW_BLASTER_20L:
				case Defines.MZ2_WIDOW_BLASTER_30L:
				case Defines.MZ2_WIDOW_BLASTER_40L:
				case Defines.MZ2_WIDOW_BLASTER_50L:
				case Defines.MZ2_WIDOW_BLASTER_60L:
				case Defines.MZ2_WIDOW_BLASTER_70L:
				case Defines.MZ2_WIDOW_RUN_1:
				case Defines.MZ2_WIDOW_RUN_2:
				case Defines.MZ2_WIDOW_RUN_3:
				case Defines.MZ2_WIDOW_RUN_4:
				case Defines.MZ2_WIDOW_RUN_5:
				case Defines.MZ2_WIDOW_RUN_6:
				case Defines.MZ2_WIDOW_RUN_7:
				case Defines.MZ2_WIDOW_RUN_8:
					dl.color[0] = 0;
					dl.color[1] = 1;
					dl.color[2] = 0;
					S.StartSound(null, ent, Defines.CHAN_WEAPON, S.RegisterSound("tank/tnkatck3.wav"), 1, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ2_WIDOW_DISRUPTOR:
					dl.color[0] = -1;
					dl.color[1] = -1;
					dl.color[2] = -1;
					S.StartSound(null, ent, Defines.CHAN_WEAPON, S.RegisterSound("weapons/disint2.wav"), 1, Defines.ATTN_NORM, 0);

					break;

				case Defines.MZ2_WIDOW_PLASMABEAM:
				case Defines.MZ2_WIDOW2_BEAMER_1:
				case Defines.MZ2_WIDOW2_BEAMER_2:
				case Defines.MZ2_WIDOW2_BEAMER_3:
				case Defines.MZ2_WIDOW2_BEAMER_4:
				case Defines.MZ2_WIDOW2_BEAMER_5:
				case Defines.MZ2_WIDOW2_BEAM_SWEEP_1:
				case Defines.MZ2_WIDOW2_BEAM_SWEEP_2:
				case Defines.MZ2_WIDOW2_BEAM_SWEEP_3:
				case Defines.MZ2_WIDOW2_BEAM_SWEEP_4:
				case Defines.MZ2_WIDOW2_BEAM_SWEEP_5:
				case Defines.MZ2_WIDOW2_BEAM_SWEEP_6:
				case Defines.MZ2_WIDOW2_BEAM_SWEEP_7:
				case Defines.MZ2_WIDOW2_BEAM_SWEEP_8:
				case Defines.MZ2_WIDOW2_BEAM_SWEEP_9:
				case Defines.MZ2_WIDOW2_BEAM_SWEEP_10:
				case Defines.MZ2_WIDOW2_BEAM_SWEEP_11:
					dl.radius = 300 + (Globals.rnd.Next() & 100);
					dl.color[0] = 1;
					dl.color[1] = 1;
					dl.color[2] = 0;
					dl.die = Globals.cl.time + 200;

					break;

				//	   ROGUE
				//	   ======

				//	   --- Xian's shit ends ---
			}
		}

		/*
		 * =============== CL_AddDLights
		 * 
		 * ===============
		 */
		public static void AddDLights()
		{
			cdlight_t dl;

			//	  =====
			//	  PGM
			if (Globals.vidref_val == Defines.VIDREF_GL)
			{
				for (var i = 0; i < Defines.MAX_DLIGHTS; i++)
				{
					dl = CL_fx.cl_dlights[i];

					if (dl.radius == 0.0f)
						continue;

					V.AddLight(dl.origin, dl.radius, dl.color[0], dl.color[1], dl.color[2]);
				}
			}
			else
			{
				for (var i = 0; i < Defines.MAX_DLIGHTS; i++)
				{
					dl = CL_fx.cl_dlights[i];

					if (dl.radius == 0.0f)
						continue;

					// negative light in software. only black allowed
					if (dl.color[0] < 0 || dl.color[1] < 0 || dl.color[2] < 0)
					{
						dl.radius = -dl.radius;
						dl.color[0] = 1;
						dl.color[1] = 1;
						dl.color[2] = 1;
					}

					V.AddLight(dl.origin, dl.radius, dl.color[0], dl.color[1], dl.color[2]);
				}
			}

			//	  PGM
			//	  =====
		}

		/*
		 * =============== CL_ClearParticles ===============
		 */
		private static void ClearParticles()
		{
			CL_fx.free_particles = CL_fx.particles[0];
			CL_fx.active_particles = null;

			for (var i = 0; i < CL_fx.particles.Length - 1; i++)
				CL_fx.particles[i].next = CL_fx.particles[i + 1];

			CL_fx.particles[CL_fx.particles.Length - 1].next = null;
		}

		/*
		 * =============== CL_ParticleEffect
		 * 
		 * Wall impact puffs ===============
		 */
		public static void ParticleEffect(float[] org, float[] dir, int color, int count)
		{
			int j;
			cparticle_t p;
			float d;

			for (var i = 0; i < count; i++)
			{
				if (CL_fx.free_particles == null)
					return;

				p = CL_fx.free_particles;
				CL_fx.free_particles = p.next;
				p.next = CL_fx.active_particles;
				CL_fx.active_particles = p;
				p.time = Globals.cl.time;
				p.color = color + (Lib.rand() & 7);
				d = Lib.rand() & 31;

				for (j = 0; j < 3; j++)
				{
					p.org[j] = org[j] + ((Lib.rand() & 7) - 4) + d * dir[j];
					p.vel[j] = Lib.crand() * 20;
				}

				p.accel[0] = p.accel[1] = 0;
				p.accel[2] = -CL_fx.PARTICLE_GRAVITY;
				p.alpha = 1.0f;
				p.alphavel = -1.0f / (0.5f + (float)Globals.rnd.NextDouble() * 0.3f);
			}
		}

		/*
		 * =============== CL_ParticleEffect2 ===============
		 */
		public static void ParticleEffect2(float[] org, float[] dir, int color, int count)
		{
			int j;
			cparticle_t p;
			float d;

			for (var i = 0; i < count; i++)
			{
				if (CL_fx.free_particles == null)
					return;

				p = CL_fx.free_particles;
				CL_fx.free_particles = p.next;
				p.next = CL_fx.active_particles;
				CL_fx.active_particles = p;
				p.time = Globals.cl.time;
				p.color = color;
				d = Lib.rand() & 7;

				for (j = 0; j < 3; j++)
				{
					p.org[j] = org[j] + ((Lib.rand() & 7) - 4) + d * dir[j];
					p.vel[j] = Lib.crand() * 20;
				}

				p.accel[0] = p.accel[1] = 0;
				p.accel[2] = -CL_fx.PARTICLE_GRAVITY;
				p.alpha = 1.0f;
				p.alphavel = -1.0f / (0.5f + (float)Globals.rnd.NextDouble() * 0.3f);
			}
		}

		//	   RAFAEL
		/*
		 * =============== CL_ParticleEffect3 ===============
		 */
		public static void ParticleEffect3(float[] org, float[] dir, int color, int count)
		{
			int j;
			cparticle_t p;
			float d;

			for (var i = 0; i < count; i++)
			{
				if (CL_fx.free_particles == null)
					return;

				p = CL_fx.free_particles;
				CL_fx.free_particles = p.next;
				p.next = CL_fx.active_particles;
				CL_fx.active_particles = p;
				p.time = Globals.cl.time;
				p.color = color;
				d = Lib.rand() & 7;

				for (j = 0; j < 3; j++)
				{
					p.org[j] = org[j] + ((Lib.rand() & 7) - 4) + d * dir[j];
					p.vel[j] = Lib.crand() * 20;
				}

				p.accel[0] = p.accel[1] = 0;
				p.accel[2] = CL_fx.PARTICLE_GRAVITY;
				p.alpha = 1.0f;
				p.alphavel = -1.0f / (0.5f + (float)Globals.rnd.NextDouble() * 0.3f);
			}
		}

		/*
		 * =============== CL_TeleporterParticles ===============
		 */
		public static void TeleporterParticles(entity_state_t ent)
		{
			int j;
			cparticle_t p;

			for (var i = 0; i < 8; i++)
			{
				if (CL_fx.free_particles == null)
					return;

				p = CL_fx.free_particles;
				CL_fx.free_particles = p.next;
				p.next = CL_fx.active_particles;
				CL_fx.active_particles = p;
				p.time = Globals.cl.time;
				p.color = 0xdb;

				for (j = 0; j < 2; j++)
				{
					p.org[j] = ent.origin[j] - 16 + (Lib.rand() & 31);
					p.vel[j] = Lib.crand() * 14;
				}

				p.org[2] = ent.origin[2] - 8 + (Lib.rand() & 7);
				p.vel[2] = 80 + (Lib.rand() & 7);
				p.accel[0] = p.accel[1] = 0;
				p.accel[2] = -CL_fx.PARTICLE_GRAVITY;
				p.alpha = 1.0f;
				p.alphavel = -0.5f;
			}
		}

		/*
		 * =============== CL_LogoutEffect
		 * 
		 * ===============
		 */
		private static void LogoutEffect(float[] org, int type)
		{
			int j;
			cparticle_t p;

			for (var i = 0; i < 500; i++)
			{
				if (CL_fx.free_particles == null)
					return;

				p = CL_fx.free_particles;
				CL_fx.free_particles = p.next;
				p.next = CL_fx.active_particles;
				CL_fx.active_particles = p;
				p.time = Globals.cl.time;

				if (type == Defines.MZ_LOGIN)
					p.color = 0xd0 + (Lib.rand() & 7); // green
				else if (type == Defines.MZ_LOGOUT)
					p.color = 0x40 + (Lib.rand() & 7); // red
				else
					p.color = 0xe0 + (Lib.rand() & 7); // yellow

				p.org[0] = org[0] - 16 + (float)Globals.rnd.NextDouble() * 32;
				p.org[1] = org[1] - 16 + (float)Globals.rnd.NextDouble() * 32;
				p.org[2] = org[2] - 24 + (float)Globals.rnd.NextDouble() * 56;

				for (j = 0; j < 3; j++)
					p.vel[j] = Lib.crand() * 20;

				p.accel[0] = p.accel[1] = 0;
				p.accel[2] = -CL_fx.PARTICLE_GRAVITY;
				p.alpha = 1.0f;
				p.alphavel = -1.0f / (1.0f + (float)Globals.rnd.NextDouble() * 0.3f);
			}
		}

		/*
		 * =============== CL_ItemRespawnParticles
		 * 
		 * ===============
		 */
		private static void ItemRespawnParticles(float[] org)
		{
			int j;
			cparticle_t p;

			for (var i = 0; i < 64; i++)
			{
				if (CL_fx.free_particles == null)
					return;

				p = CL_fx.free_particles;
				CL_fx.free_particles = p.next;
				p.next = CL_fx.active_particles;
				CL_fx.active_particles = p;
				p.time = Globals.cl.time;
				p.color = 0xd4 + (Lib.rand() & 3); // green
				p.org[0] = org[0] + Lib.crand() * 8;
				p.org[1] = org[1] + Lib.crand() * 8;
				p.org[2] = org[2] + Lib.crand() * 8;

				for (j = 0; j < 3; j++)
					p.vel[j] = Lib.crand() * 8;

				p.accel[0] = p.accel[1] = 0;
				p.accel[2] = -CL_fx.PARTICLE_GRAVITY * 0.2f;
				p.alpha = 1.0f;
				p.alphavel = -1.0f / (1.0f + (float)Globals.rnd.NextDouble() * 0.3f);
			}
		}

		/*
		 * =============== CL_ExplosionParticles ===============
		 */
		public static void ExplosionParticles(float[] org)
		{
			int j;
			cparticle_t p;

			for (var i = 0; i < 256; i++)
			{
				if (CL_fx.free_particles == null)
					return;

				p = CL_fx.free_particles;
				CL_fx.free_particles = p.next;
				p.next = CL_fx.active_particles;
				CL_fx.active_particles = p;
				p.time = Globals.cl.time;
				p.color = 0xe0 + (Lib.rand() & 7);

				for (j = 0; j < 3; j++)
				{
					p.org[j] = org[j] + (Lib.rand() % 32 - 16);
					p.vel[j] = Lib.rand() % 384 - 192;
				}

				p.accel[0] = p.accel[1] = 0.0f;
				p.accel[2] = -CL_fx.PARTICLE_GRAVITY;
				p.alpha = 1.0f;
				p.alphavel = -0.8f / (0.5f + (float)Globals.rnd.NextDouble() * 0.3f);
			}
		}

		public static void BigTeleportParticles(float[] org)
		{
			cparticle_t p;
			float angle, dist;

			for (var i = 0; i < 4096; i++)
			{
				if (CL_fx.free_particles == null)
					return;

				p = CL_fx.free_particles;
				CL_fx.free_particles = p.next;
				p.next = CL_fx.active_particles;
				CL_fx.active_particles = p;
				p.time = Globals.cl.time;
				p.color = CL_fx.colortable[Lib.rand() & 3];
				angle = (float)(Math.PI * 2 * (Lib.rand() & 1023) / 1023.0);
				dist = Lib.rand() & 31;
				p.org[0] = (float)(org[0] + Math.Cos(angle) * dist);
				p.vel[0] = (float)(Math.Cos(angle) * (70 + (Lib.rand() & 63)));
				p.accel[0] = (float)(-Math.Cos(angle) * 100);
				p.org[1] = (float)(org[1] + Math.Sin(angle) * dist);
				p.vel[1] = (float)(Math.Sin(angle) * (70 + (Lib.rand() & 63)));
				p.accel[1] = (float)(-Math.Sin(angle) * 100);
				p.org[2] = org[2] + 8 + Lib.rand() % 90;
				p.vel[2] = -100 + (Lib.rand() & 31);
				p.accel[2] = CL_fx.PARTICLE_GRAVITY * 4;
				p.alpha = 1.0f;
				p.alphavel = -0.3f / (0.5f + (float)Globals.rnd.NextDouble() * 0.3f);
			}
		}

		/*
		 * =============== CL_BlasterParticles
		 * 
		 * Wall impact puffs ===============
		 */
		public static void BlasterParticles(float[] org, float[] dir)
		{
			int j;
			cparticle_t p;
			float d;
			var count = 40;

			for (var i = 0; i < count; i++)
			{
				if (CL_fx.free_particles == null)
					return;

				p = CL_fx.free_particles;
				CL_fx.free_particles = p.next;
				p.next = CL_fx.active_particles;
				CL_fx.active_particles = p;
				p.time = Globals.cl.time;
				p.color = 0xe0 + (Lib.rand() & 7);
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
		private static readonly float[] move = { 0, 0, 0 };
		private static readonly float[] vec = { 0, 0, 0 };

		/*
		 * =============== CL_BlasterTrail
		 * 
		 * ===============
		 */
		public static void BlasterTrail(float[] start, float[] end)
		{
			float len;
			int j;
			cparticle_t p;
			int dec;
			Math3D.VectorCopy(start, CL_fx.move);
			Math3D.VectorSubtract(end, start, CL_fx.vec);
			len = Math3D.VectorNormalize(CL_fx.vec);
			dec = 5;
			Math3D.VectorScale(CL_fx.vec, 5, CL_fx.vec);

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
				p.color = 0xe0;

				for (j = 0; j < 3; j++)
				{
					p.org[j] = CL_fx.move[j] + Lib.crand();
					p.vel[j] = Lib.crand() * 5;
					p.accel[j] = 0;
				}

				Math3D.VectorAdd(CL_fx.move, CL_fx.vec, CL_fx.move);
			}
		}

		// stack variable
		// move, vec
		/*
		 * ===============
		 *  CL_FlagTrail
		 * ===============
		 */
		public static void FlagTrail(float[] start, float[] end, float color)
		{
			float len;
			int j;
			cparticle_t p;
			int dec;
			Math3D.VectorCopy(start, CL_fx.move);
			Math3D.VectorSubtract(end, start, CL_fx.vec);
			len = Math3D.VectorNormalize(CL_fx.vec);
			dec = 5;
			Math3D.VectorScale(CL_fx.vec, 5, CL_fx.vec);

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
				p.alphavel = -1.0f / (0.8f + (float)Globals.rnd.NextDouble() * 0.2f);
				p.color = color;

				for (j = 0; j < 3; j++)
				{
					p.org[j] = CL_fx.move[j] + Lib.crand() * 16;
					p.vel[j] = Lib.crand() * 5;
					p.accel[j] = 0;
				}

				Math3D.VectorAdd(CL_fx.move, CL_fx.vec, CL_fx.move);
			}
		}

		// stack variable
		// move, vec
		/*
		 * =============== CL_DiminishingTrail
		 * 
		 * ===============
		 */
		public static void DiminishingTrail(float[] start, float[] end, centity_t old, int flags)
		{
			cparticle_t p;
			float orgscale;
			float velscale;
			Math3D.VectorCopy(start, CL_fx.move);
			Math3D.VectorSubtract(end, start, CL_fx.vec);
			var len = Math3D.VectorNormalize(CL_fx.vec);
			var dec = 0.5f;
			Math3D.VectorScale(CL_fx.vec, dec, CL_fx.vec);

			if (old.trailcount > 900)
			{
				orgscale = 4;
				velscale = 15;
			}
			else if (old.trailcount > 800)
			{
				orgscale = 2;
				velscale = 10;
			}
			else
			{
				orgscale = 1;
				velscale = 5;
			}

			while (len > 0)
			{
				len -= dec;

				if (CL_fx.free_particles == null)
					return;

				// drop less particles as it flies
				if ((Lib.rand() & 1023) < old.trailcount)
				{
					p = CL_fx.free_particles;
					CL_fx.free_particles = p.next;
					p.next = CL_fx.active_particles;
					CL_fx.active_particles = p;
					Math3D.VectorClear(p.accel);
					p.time = Globals.cl.time;

					if ((flags & Defines.EF_GIB) != 0)
					{
						p.alpha = 1.0f;
						p.alphavel = -1.0f / (1.0f + (float)Globals.rnd.NextDouble() * 0.4f);
						p.color = 0xe8 + (Lib.rand() & 7);

						for (var j = 0; j < 3; j++)
						{
							p.org[j] = CL_fx.move[j] + Lib.crand() * orgscale;
							p.vel[j] = Lib.crand() * velscale;
							p.accel[j] = 0;
						}

						p.vel[2] -= CL_fx.PARTICLE_GRAVITY;
					}
					else if ((flags & Defines.EF_GREENGIB) != 0)
					{
						p.alpha = 1.0f;
						p.alphavel = -1.0f / (1.0f + (float)Globals.rnd.NextDouble() * 0.4f);
						p.color = 0xdb + (Lib.rand() & 7);

						for (var j = 0; j < 3; j++)
						{
							p.org[j] = CL_fx.move[j] + Lib.crand() * orgscale;
							p.vel[j] = Lib.crand() * velscale;
							p.accel[j] = 0;
						}

						p.vel[2] -= CL_fx.PARTICLE_GRAVITY;
					}
					else
					{
						p.alpha = 1.0f;
						p.alphavel = -1.0f / (1.0f + (float)Globals.rnd.NextDouble() * 0.2f);
						p.color = 4 + (Lib.rand() & 7);

						for (var j = 0; j < 3; j++)
						{
							p.org[j] = CL_fx.move[j] + Lib.crand() * orgscale;
							p.vel[j] = Lib.crand() * velscale;
						}

						p.accel[2] = 20;
					}
				}

				old.trailcount -= 5;

				if (old.trailcount < 100)
					old.trailcount = 100;

				Math3D.VectorAdd(CL_fx.move, CL_fx.vec, CL_fx.move);
			}
		}

		// stack variable
		// move, vec
		/*
		 * =============== CL_RocketTrail
		 * 
		 * ===============
		 */
		public static void RocketTrail(float[] start, float[] end, centity_t old)
		{
			float len;
			int j;
			cparticle_t p;
			float dec;

			// smoke
			CL_fx.DiminishingTrail(start, end, old, Defines.EF_ROCKET);

			// fire
			Math3D.VectorCopy(start, CL_fx.move);
			Math3D.VectorSubtract(end, start, CL_fx.vec);
			len = Math3D.VectorNormalize(CL_fx.vec);
			dec = 1;
			Math3D.VectorScale(CL_fx.vec, dec, CL_fx.vec);

			while (len > 0)
			{
				len -= dec;

				if (CL_fx.free_particles == null)
					return;

				if ((Lib.rand() & 7) == 0)
				{
					p = CL_fx.free_particles;
					CL_fx.free_particles = p.next;
					p.next = CL_fx.active_particles;
					CL_fx.active_particles = p;
					Math3D.VectorClear(p.accel);
					p.time = Globals.cl.time;
					p.alpha = 1.0f;
					p.alphavel = -1.0f / (1.0f + (float)Globals.rnd.NextDouble() * 0.2f);
					p.color = 0xdc + (Lib.rand() & 3);

					for (j = 0; j < 3; j++)
					{
						p.org[j] = CL_fx.move[j] + Lib.crand() * 5;
						p.vel[j] = Lib.crand() * 20;
					}

					p.accel[2] = -CL_fx.PARTICLE_GRAVITY;
				}

				Math3D.VectorAdd(CL_fx.move, CL_fx.vec, CL_fx.move);
			}
		}

		// stack variable
		// move, vec
		/*
		 * =============== CL_RailTrail
		 * 
		 * ===============
		 */
		public static void RailTrail(float[] start, float[] end)
		{
			float len;
			int j;
			cparticle_t p;
			float dec;
			var right = new float[3];
			var up = new float[3];
			int i;
			float d, c, s;
			var dir = new float[3];
			byte clr = 0x74;
			Math3D.VectorCopy(start, CL_fx.move);
			Math3D.VectorSubtract(end, start, CL_fx.vec);
			len = Math3D.VectorNormalize(CL_fx.vec);
			Math3D.MakeNormalVectors(CL_fx.vec, right, up);

			for (i = 0; i < len; i++)
			{
				if (CL_fx.free_particles == null)
					return;

				p = CL_fx.free_particles;
				CL_fx.free_particles = p.next;
				p.next = CL_fx.active_particles;
				CL_fx.active_particles = p;
				p.time = Globals.cl.time;
				Math3D.VectorClear(p.accel);
				d = i * 0.1f;
				c = (float)Math.Cos(d);
				s = (float)Math.Sin(d);
				Math3D.VectorScale(right, c, dir);
				Math3D.VectorMA(dir, s, up, dir);
				p.alpha = 1.0f;
				p.alphavel = -1.0f / (1.0f + (float)Globals.rnd.NextDouble() * 0.2f);
				p.color = clr + (Lib.rand() & 7);

				for (j = 0; j < 3; j++)
				{
					p.org[j] = CL_fx.move[j] + dir[j] * 3;
					p.vel[j] = dir[j] * 6;
				}

				Math3D.VectorAdd(CL_fx.move, CL_fx.vec, CL_fx.move);
			}

			dec = 0.75f;
			Math3D.VectorScale(CL_fx.vec, dec, CL_fx.vec);
			Math3D.VectorCopy(start, CL_fx.move);

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
				p.alpha = 1.0f;
				p.alphavel = -1.0f / (0.6f + (float)Globals.rnd.NextDouble() * 0.2f);
				p.color = (0x0 + Lib.rand()) & 15;

				for (j = 0; j < 3; j++)
				{
					p.org[j] = CL_fx.move[j] + Lib.crand() * 3;
					p.vel[j] = Lib.crand() * 3;
					p.accel[j] = 0;
				}

				Math3D.VectorAdd(CL_fx.move, CL_fx.vec, CL_fx.move);
			}
		}

		// stack variable
		// move, vec
		/*
		 * =============== CL_IonripperTrail ===============
		 */
		public static void IonripperTrail(float[] start, float[] ent)
		{
			float len;
			int j;
			cparticle_t p;
			int dec;
			var left = 0;
			Math3D.VectorCopy(start, CL_fx.move);
			Math3D.VectorSubtract(ent, start, CL_fx.vec);
			len = Math3D.VectorNormalize(CL_fx.vec);
			dec = 5;
			Math3D.VectorScale(CL_fx.vec, 5, CL_fx.vec);

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
				p.alpha = 0.5f;
				p.alphavel = -1.0f / (0.3f + (float)Globals.rnd.NextDouble() * 0.2f);
				p.color = 0xe4 + (Lib.rand() & 3);

				for (j = 0; j < 3; j++)
				{
					p.org[j] = CL_fx.move[j];
					p.accel[j] = 0;
				}

				if (left != 0)
				{
					left = 0;
					p.vel[0] = 10;
				}
				else
				{
					left = 1;
					p.vel[0] = -10;
				}

				p.vel[1] = 0;
				p.vel[2] = 0;
				Math3D.VectorAdd(CL_fx.move, CL_fx.vec, CL_fx.move);
			}
		}

		// stack variable
		// move, vec
		/*
		 * =============== CL_BubbleTrail
		 * 
		 * ===============
		 */
		public static void BubbleTrail(float[] start, float[] end)
		{
			float len;
			int i, j;
			cparticle_t p;
			int dec;
			Math3D.VectorCopy(start, CL_fx.move);
			Math3D.VectorSubtract(end, start, CL_fx.vec);
			len = Math3D.VectorNormalize(CL_fx.vec);
			dec = 32;
			Math3D.VectorScale(CL_fx.vec, dec, CL_fx.vec);

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
				p.alphavel = -1.0f / (1.0f + (float)Globals.rnd.NextDouble() * 0.2f);
				p.color = 4 + (Lib.rand() & 7);

				for (j = 0; j < 3; j++)
				{
					p.org[j] = CL_fx.move[j] + Lib.crand() * 2;
					p.vel[j] = Lib.crand() * 5;
				}

				p.vel[2] += 6;
				Math3D.VectorAdd(CL_fx.move, CL_fx.vec, CL_fx.move);
			}
		}

		// stack variable
		// forward
		/*
		 * =============== CL_FlyParticles ===============
		 */
		private static void FlyParticles(float[] origin, int count)
		{
			int i;
			cparticle_t p;
			float angle;
			float sp, sy, cp, cy;
			float dist = 64;
			float ltime;

			if (count > Defines.NUMVERTEXNORMALS)
				count = Defines.NUMVERTEXNORMALS;

			if (CL_fx.avelocities[0][0] == 0.0f)
			{
				for (i = 0; i < Defines.NUMVERTEXNORMALS; i++)
				{
					CL_fx.avelocities[i][0] = (Lib.rand() & 255) * 0.01f;
					CL_fx.avelocities[i][1] = (Lib.rand() & 255) * 0.01f;
					CL_fx.avelocities[i][2] = (Lib.rand() & 255) * 0.01f;
				}
			}

			ltime = Globals.cl.time / 1000.0f;

			for (i = 0; i < count; i += 2)
			{
				angle = ltime * CL_fx.avelocities[i][0];
				sy = (float)Math.Sin(angle);
				cy = (float)Math.Cos(angle);
				angle = ltime * CL_fx.avelocities[i][1];
				sp = (float)Math.Sin(angle);
				cp = (float)Math.Cos(angle);
				angle = ltime * CL_fx.avelocities[i][2];
				CL_fx.forward[0] = cp * cy;
				CL_fx.forward[1] = cp * sy;
				CL_fx.forward[2] = -sp;

				if (CL_fx.free_particles == null)
					return;

				p = CL_fx.free_particles;
				CL_fx.free_particles = p.next;
				p.next = CL_fx.active_particles;
				CL_fx.active_particles = p;
				p.time = Globals.cl.time;
				dist = (float)Math.Sin(ltime + i) * 64;
				p.org[0] = origin[0] + Globals.bytedirs[i][0] * dist + CL_fx.forward[0] * CL_fx.BEAMLENGTH;
				p.org[1] = origin[1] + Globals.bytedirs[i][1] * dist + CL_fx.forward[1] * CL_fx.BEAMLENGTH;
				p.org[2] = origin[2] + Globals.bytedirs[i][2] * dist + CL_fx.forward[2] * CL_fx.BEAMLENGTH;
				Math3D.VectorClear(p.vel);
				Math3D.VectorClear(p.accel);
				p.color = 0;

				//p.colorvel = 0;
				p.alpha = 1;
				p.alphavel = -100;
			}
		}

		public static void FlyEffect(centity_t ent, float[] origin)
		{
			int n;
			int count;
			int starttime;

			if (ent.flyStoptime < Globals.cl.time)
			{
				starttime = Globals.cl.time;
				ent.flyStoptime = Globals.cl.time + 60000;
			}
			else
				starttime = ent.flyStoptime - 60000;

			n = Globals.cl.time - starttime;

			if (n < 20000)
				count = (int)(n * 162 / 20000.0);
			else
			{
				n = ent.flyStoptime - Globals.cl.time;

				if (n < 20000)
					count = (int)(n * 162 / 20000.0);
				else
					count = 162;
			}

			CL_fx.FlyParticles(origin, count);
		}

		// stack variable
		private static readonly float[] v = { 0, 0, 0 };

		// forward
		/*
		 * =============== CL_BfgParticles ===============
		 */
		//#define BEAMLENGTH 16
		public static void BfgParticles(entity_t ent)
		{
			int i;
			cparticle_t p;
			float angle;
			float sp, sy, cp, cy;
			float dist = 64;
			float ltime;

			if (CL_fx.avelocities[0][0] == 0.0f)
			{
				for (i = 0; i < Defines.NUMVERTEXNORMALS; i++)
				{
					CL_fx.avelocities[i][0] = (Lib.rand() & 255) * 0.01f;
					CL_fx.avelocities[i][1] = (Lib.rand() & 255) * 0.01f;
					CL_fx.avelocities[i][2] = (Lib.rand() & 255) * 0.01f;
				}
			}

			ltime = Globals.cl.time / 1000.0f;

			for (i = 0; i < Defines.NUMVERTEXNORMALS; i++)
			{
				angle = ltime * CL_fx.avelocities[i][0];
				sy = (float)Math.Sin(angle);
				cy = (float)Math.Cos(angle);
				angle = ltime * CL_fx.avelocities[i][1];
				sp = (float)Math.Sin(angle);
				cp = (float)Math.Cos(angle);
				angle = ltime * CL_fx.avelocities[i][2];
				CL_fx.forward[0] = cp * cy;
				CL_fx.forward[1] = cp * sy;
				CL_fx.forward[2] = -sp;

				if (CL_fx.free_particles == null)
					return;

				p = CL_fx.free_particles;
				CL_fx.free_particles = p.next;
				p.next = CL_fx.active_particles;
				CL_fx.active_particles = p;
				p.time = Globals.cl.time;
				dist = (float)(Math.Sin(ltime + i) * 64);
				p.org[0] = ent.origin[0] + Globals.bytedirs[i][0] * dist + CL_fx.forward[0] * CL_fx.BEAMLENGTH;
				p.org[1] = ent.origin[1] + Globals.bytedirs[i][1] * dist + CL_fx.forward[1] * CL_fx.BEAMLENGTH;
				p.org[2] = ent.origin[2] + Globals.bytedirs[i][2] * dist + CL_fx.forward[2] * CL_fx.BEAMLENGTH;
				Math3D.VectorClear(p.vel);
				Math3D.VectorClear(p.accel);
				Math3D.VectorSubtract(p.org, ent.origin, CL_fx.v);
				dist = Math3D.VectorLength(CL_fx.v) / 90.0f;
				p.color = (float)Math.Floor(0xd0 + dist * 7);

				//p.colorvel = 0;
				p.alpha = 1.0f - dist;
				p.alphavel = -100;
			}
		}

		// stack variable
		// move, vec
		private static readonly float[] start = { 0, 0, 0 };
		private static readonly float[] end = { 0, 0, 0 };

		/*
		 * =============== CL_TrapParticles ===============
		 */
		//	   RAFAEL
		public static void TrapParticles(entity_t ent)
		{
			float len;
			int j;
			cparticle_t p;
			int dec;
			ent.origin[2] -= 14;
			Math3D.VectorCopy(ent.origin, CL_fx.start);
			Math3D.VectorCopy(ent.origin, CL_fx.end);
			CL_fx.end[2] += 64;
			Math3D.VectorCopy(CL_fx.start, CL_fx.move);
			Math3D.VectorSubtract(CL_fx.end, CL_fx.start, CL_fx.vec);
			len = Math3D.VectorNormalize(CL_fx.vec);
			dec = 5;
			Math3D.VectorScale(CL_fx.vec, 5, CL_fx.vec);

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
				p.color = 0xe0;

				for (j = 0; j < 3; j++)
				{
					p.org[j] = CL_fx.move[j] + Lib.crand();
					p.vel[j] = Lib.crand() * 15;
					p.accel[j] = 0;
				}

				p.accel[2] = CL_fx.PARTICLE_GRAVITY;
				Math3D.VectorAdd(CL_fx.move, CL_fx.vec, CL_fx.move);
			}

			int i, k;

			//cparticle_t p;
			float vel;
			var dir = new float[3];
			var org = new float[3];
			ent.origin[2] += 14;
			Math3D.VectorCopy(ent.origin, org);

			for (i = -2; i <= 2; i += 4)
			for (j = -2; j <= 2; j += 4)
			for (k = -2; k <= 4; k += 4)
			{
				if (CL_fx.free_particles == null)
					return;

				p = CL_fx.free_particles;
				CL_fx.free_particles = p.next;
				p.next = CL_fx.active_particles;
				CL_fx.active_particles = p;
				p.time = Globals.cl.time;
				p.color = 0xe0 + (Lib.rand() & 3);
				p.alpha = 1.0f;
				p.alphavel = -1.0f / (0.3f + (Lib.rand() & 7) * 0.02f);
				p.org[0] = org[0] + i + (Lib.rand() & 23) * Lib.crand();
				p.org[1] = org[1] + j + (Lib.rand() & 23) * Lib.crand();
				p.org[2] = org[2] + k + (Lib.rand() & 23) * Lib.crand();
				dir[0] = j * 8;
				dir[1] = i * 8;
				dir[2] = k * 8;
				Math3D.VectorNormalize(dir);
				vel = (50 + Lib.rand()) & 63;
				Math3D.VectorScale(dir, vel, p.vel);
				p.accel[0] = p.accel[1] = 0;
				p.accel[2] = -CL_fx.PARTICLE_GRAVITY;
			}
		}

		/*
		 * =============== CL_BFGExplosionParticles ===============
		 */
		//	  FIXME combined with CL_ExplosionParticles
		public static void BFGExplosionParticles(float[] org)
		{
			int j;
			cparticle_t p;

			for (var i = 0; i < 256; i++)
			{
				if (CL_fx.free_particles == null)
					return;

				p = CL_fx.free_particles;
				CL_fx.free_particles = p.next;
				p.next = CL_fx.active_particles;
				CL_fx.active_particles = p;
				p.time = Globals.cl.time;
				p.color = 0xd0 + (Lib.rand() & 7);

				for (j = 0; j < 3; j++)
				{
					p.org[j] = org[j] + (Lib.rand() % 32 - 16);
					p.vel[j] = Lib.rand() % 384 - 192;
				}

				p.accel[0] = p.accel[1] = 0;
				p.accel[2] = -CL_fx.PARTICLE_GRAVITY;
				p.alpha = 1.0f;
				p.alphavel = -0.8f / (0.5f + (float)Globals.rnd.NextDouble() * 0.3f);
			}
		}

		// stack variable
		private static readonly float[] dir = { 0, 0, 0 };

		/*
		 * =============== CL_TeleportParticles
		 * 
		 * ===============
		 */
		public static void TeleportParticles(float[] org)
		{
			cparticle_t p;
			float vel;

			for (var i = -16; i <= 16; i += 4)
			for (var j = -16; j <= 16; j += 4)
			for (var k = -16; k <= 32; k += 4)
			{
				if (CL_fx.free_particles == null)
					return;

				p = CL_fx.free_particles;
				CL_fx.free_particles = p.next;
				p.next = CL_fx.active_particles;
				CL_fx.active_particles = p;
				p.time = Globals.cl.time;
				p.color = 7 + (Lib.rand() & 7);
				p.alpha = 1.0f;
				p.alphavel = -1.0f / (0.3f + (Lib.rand() & 7) * 0.02f);
				p.org[0] = org[0] + i + (Lib.rand() & 3);
				p.org[1] = org[1] + j + (Lib.rand() & 3);
				p.org[2] = org[2] + k + (Lib.rand() & 3);
				CL_fx.dir[0] = j * 8;
				CL_fx.dir[1] = i * 8;
				CL_fx.dir[2] = k * 8;
				Math3D.VectorNormalize(CL_fx.dir);
				vel = 50 + (Lib.rand() & 63);
				Math3D.VectorScale(CL_fx.dir, vel, p.vel);
				p.accel[0] = p.accel[1] = 0;
				p.accel[2] = -CL_fx.PARTICLE_GRAVITY;
			}
		}

		// stack variable
		private static readonly float[] org = { 0, 0, 0 };

		/*
		 * =============== CL_AddParticles ===============
		 */
		public static void AddParticles()
		{
			cparticle_t p, next;
			float alpha;
			var time = 0.0f;
			float time2;
			int color;
			cparticle_t active, tail;
			active = null;
			tail = null;

			for (p = CL_fx.active_particles; p != null; p = next)
			{
				next = p.next;

				// PMM - added INSTANT_PARTICLE handling for heat beam
				if (p.alphavel != CL_fx.INSTANT_PARTICLE)
				{
					time = (Globals.cl.time - p.time) * 0.001f;
					alpha = p.alpha + time * p.alphavel;

					if (alpha <= 0)
					{
						// faded out
						p.next = CL_fx.free_particles;
						CL_fx.free_particles = p;

						continue;
					}
				}
				else
					alpha = p.alpha;

				p.next = null;

				if (tail == null)
					active = tail = p;
				else
				{
					tail.next = p;
					tail = p;
				}

				if (alpha > 1.0)
					alpha = 1;

				color = (int)p.color;
				time2 = time * time;
				CL_fx.org[0] = p.org[0] + p.vel[0] * time + p.accel[0] * time2;
				CL_fx.org[1] = p.org[1] + p.vel[1] * time + p.accel[1] * time2;
				CL_fx.org[2] = p.org[2] + p.vel[2] * time + p.accel[2] * time2;
				V.AddParticle(CL_fx.org, color, alpha);

				// PMM
				if (p.alphavel == CL_fx.INSTANT_PARTICLE)
				{
					p.alphavel = 0.0f;
					p.alpha = 0.0f;
				}
			}

			CL_fx.active_particles = active;
		}

		/*
		 * ============== CL_EntityEvent
		 * 
		 * An entity has just been parsed that has an event value
		 * 
		 * the female events are there for backwards compatability ==============
		 */
		public static void EntityEvent(entity_state_t ent)
		{
			switch (ent.@event)
			{
				case Defines.EV_ITEM_RESPAWN:
					S.StartSound(null, ent.number, Defines.CHAN_WEAPON, S.RegisterSound("items/respawn1.wav"), 1, Defines.ATTN_IDLE, 0);
					CL_fx.ItemRespawnParticles(ent.origin);

					break;

				case Defines.EV_PLAYER_TELEPORT:
					S.StartSound(null, ent.number, Defines.CHAN_WEAPON, S.RegisterSound("misc/tele1.wav"), 1, Defines.ATTN_IDLE, 0);
					CL_fx.TeleportParticles(ent.origin);

					break;

				case Defines.EV_FOOTSTEP:
					if (Globals.cl_footsteps.value != 0.0f)
						S.StartSound(null, ent.number, Defines.CHAN_BODY, CL_tent.cl_sfx_footsteps[Lib.rand() & 3], 1, Defines.ATTN_NORM, 0);

					break;

				case Defines.EV_FALLSHORT:
					S.StartSound(null, ent.number, Defines.CHAN_AUTO, S.RegisterSound("player/land1.wav"), 1, Defines.ATTN_NORM, 0);

					break;

				case Defines.EV_FALL:
					S.StartSound(null, ent.number, Defines.CHAN_AUTO, S.RegisterSound("*fall2.wav"), 1, Defines.ATTN_NORM, 0);

					break;

				case Defines.EV_FALLFAR:
					S.StartSound(null, ent.number, Defines.CHAN_AUTO, S.RegisterSound("*fall1.wav"), 1, Defines.ATTN_NORM, 0);

					break;
			}
		}

		/*
		 * ============== CL_ClearEffects
		 * 
		 * ==============
		 */
		public static void ClearEffects()
		{
			CL_fx.ClearParticles();
			CL_fx.ClearDlights();
			CL_fx.ClearLightStyles();
		}

		/*
		 * ==============================================================
		 * 
		 * PARTICLE MANAGEMENT
		 * 
		 * ==============================================================
		 */
		public static readonly int PARTICLE_GRAVITY = 40;
		public static cparticle_t active_particles;
		public static cparticle_t free_particles;

		/*
		 * =============== CL_BigTeleportParticles ===============
		 */
		private static readonly int[] colortable = { 2 * 8, 13 * 8, 21 * 8, 18 * 8 };
		private static readonly int BEAMLENGTH = 16;

		static CL_fx()
		{
			CL_fx.CL_fx1();
			CL_fx.CL_fx2();
			CL_fx.CL_fx3();
			CL_fx.CL_fx4();
		}
	}
}
