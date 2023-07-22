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

using IdTech.common;
using Quake2Sharp.client.types;
using Quake2Sharp.qcommon;
using Quake2Sharp.render.types;
using Quake2Sharp.sound;
using Quake2Sharp.sound.types;
using Quake2Sharp.util;
using System.Drawing;
using System.Numerics;

namespace Quake2Sharp.client;

/* =================
 * CL_tent
 * ================= */
public static class TemporaryEntities
{
	private static readonly List<Explosion> Explosions = new();
	private static readonly List<Beam> Beams = new();
	private static readonly List<Beam> PlayerBeams = new();
	private static readonly List<Laser> Lasers = new();
	private static readonly List<Sustain> Sustains = new();

	private const int ex_free = 0;
	private const int ex_explosion = 1;
	private const int ex_misc = 2;
	private const int ex_flash = 3;
	private const int ex_mflash = 4;
	private const int ex_poly = 5;
	private const int ex_poly2 = 6;

	private static sfx_t cl_sfx_ric1;
	private static sfx_t cl_sfx_ric2;
	private static sfx_t cl_sfx_ric3;
	private static sfx_t cl_sfx_lashit;
	private static sfx_t cl_sfx_spark5;
	private static sfx_t cl_sfx_spark6;
	private static sfx_t cl_sfx_spark7;
	private static sfx_t cl_sfx_railg;
	private static sfx_t cl_sfx_rockexp;
	private static sfx_t cl_sfx_grenexp;
	private static sfx_t cl_sfx_watrexp;

	public static sfx_t[] cl_sfx_footsteps = new sfx_t[4];
	private static model_t cl_mod_explode;
	private static model_t cl_mod_smoke;
	private static model_t cl_mod_flash;
	private static model_t cl_mod_parasite_segment;
	private static model_t cl_mod_grapple_cable;
	private static model_t cl_mod_parasite_tip;
	private static model_t cl_mod_explo4;
	private static model_t cl_mod_bfg_explo;
	private static model_t cl_mod_powerscreen;

	private static sfx_t cl_sfx_lightning;
	private static sfx_t cl_sfx_disrexp;
	private static model_t cl_mod_lightning;
	private static model_t cl_mod_heatbeam;
	private static model_t cl_mod_monster_heatbeam;
	private static model_t cl_mod_explo4_big;

	/* =================
	 * CL_RegisterTEntSounds
	 * ================= */
	public static void RegisterSounds()
	{
		TemporaryEntities.cl_sfx_ric1 = S.RegisterSound("world/ric1.wav");
		TemporaryEntities.cl_sfx_ric2 = S.RegisterSound("world/ric2.wav");
		TemporaryEntities.cl_sfx_ric3 = S.RegisterSound("world/ric3.wav");
		TemporaryEntities.cl_sfx_lashit = S.RegisterSound("weapons/lashit.wav");
		TemporaryEntities.cl_sfx_spark5 = S.RegisterSound("world/spark5.wav");
		TemporaryEntities.cl_sfx_spark6 = S.RegisterSound("world/spark6.wav");
		TemporaryEntities.cl_sfx_spark7 = S.RegisterSound("world/spark7.wav");
		TemporaryEntities.cl_sfx_railg = S.RegisterSound("weapons/railgf1a.wav");
		TemporaryEntities.cl_sfx_rockexp = S.RegisterSound("weapons/rocklx1a.wav");
		TemporaryEntities.cl_sfx_grenexp = S.RegisterSound("weapons/grenlx1a.wav");
		TemporaryEntities.cl_sfx_watrexp = S.RegisterSound("weapons/xpld_wat.wav");

		S.RegisterSound("player/land1.wav");
		S.RegisterSound("player/fall2.wav");
		S.RegisterSound("player/fall1.wav");

		TemporaryEntities.cl_sfx_lightning = S.RegisterSound("weapons/tesla.wav");
		TemporaryEntities.cl_sfx_disrexp = S.RegisterSound("weapons/disrupthit.wav");
	}

	/* =================
	 * CL_RegisterTEntModels
	 * ================= */
	public static void RegisterModels()
	{
		TemporaryEntities.cl_mod_explode = Globals.re.RegisterModel("models/objects/explode/tris.md2");
		TemporaryEntities.cl_mod_smoke = Globals.re.RegisterModel("models/objects/smoke/tris.md2");
		TemporaryEntities.cl_mod_flash = Globals.re.RegisterModel("models/objects/flash/tris.md2");
		TemporaryEntities.cl_mod_parasite_segment = Globals.re.RegisterModel("models/monsters/parasite/segment/tris.md2");
		TemporaryEntities.cl_mod_grapple_cable = Globals.re.RegisterModel("models/ctf/segment/tris.md2");
		TemporaryEntities.cl_mod_parasite_tip = Globals.re.RegisterModel("models/monsters/parasite/tip/tris.md2");
		TemporaryEntities.cl_mod_explo4 = Globals.re.RegisterModel("models/objects/r_explode/tris.md2");
		TemporaryEntities.cl_mod_bfg_explo = Globals.re.RegisterModel("sprites/s_bfg2.sp2");
		TemporaryEntities.cl_mod_powerscreen = Globals.re.RegisterModel("models/items/armor/effect/tris.md2");

		Globals.re.RegisterModel("models/objects/laser/tris.md2");
		Globals.re.RegisterModel("models/objects/grenade2/tris.md2");
		Globals.re.RegisterModel("models/weapons/v_machn/tris.md2");
		Globals.re.RegisterModel("models/weapons/v_handgr/tris.md2");
		Globals.re.RegisterModel("models/weapons/v_shotg2/tris.md2");
		Globals.re.RegisterModel("models/objects/gibs/bone/tris.md2");
		Globals.re.RegisterModel("models/objects/gibs/sm_meat/tris.md2");
		Globals.re.RegisterModel("models/objects/gibs/bone2/tris.md2");

		Globals.re.RegisterPic("w_machinegun");
		Globals.re.RegisterPic("a_bullets");
		Globals.re.RegisterPic("i_health");
		Globals.re.RegisterPic("a_grenades");

		TemporaryEntities.cl_mod_explo4_big = Globals.re.RegisterModel("models/objects/r_explode2/tris.md2");
		TemporaryEntities.cl_mod_lightning = Globals.re.RegisterModel("models/proj/lightning/tris.md2");
		TemporaryEntities.cl_mod_heatbeam = Globals.re.RegisterModel("models/proj/beam/tris.md2");
		TemporaryEntities.cl_mod_monster_heatbeam = Globals.re.RegisterModel("models/proj/widowbeam/tris.md2");
	}

	/* =================
	 * CL_ClearTEnts
	 * ================= */
	public static void Clear()
	{
		TemporaryEntities.Beams.Clear();
		TemporaryEntities.Explosions.Clear();
		TemporaryEntities.Lasers.Clear();
		TemporaryEntities.PlayerBeams.Clear();
		TemporaryEntities.Sustains.Clear();
	}

	/* =================
	 * CL_SmokeAndFlash
	 * ================= */
	public static void SmokeAndFlash(Vector3 origin)
	{
		var explosion = new Explosion
		{
			Type = TemporaryEntities.ex_misc,
			Frames = 4,
			Entity = { flags = Defines.RF_TRANSLUCENT, model = TemporaryEntities.cl_mod_smoke },
			Start = Globals.cl.frame.servertime - 100
		};

		Math3D.VectorCopy(origin, explosion.Entity.origin);
		TemporaryEntities.Explosions.Add(explosion);

		explosion = new()
		{
			Type = TemporaryEntities.ex_flash,
			Entity = { flags = Defines.RF_FULLBRIGHT, model = TemporaryEntities.cl_mod_flash },
			Frames = 2,
			Start = Globals.cl.frame.servertime - 100
		};

		Math3D.VectorCopy(origin, explosion.Entity.origin);
		TemporaryEntities.Explosions.Add(explosion);
	}

	/* =================
	 * CL_ParseBeam
	 * ================= */
	private static void ParseBeam(model_t model)
	{
		var entity = MSG.ReadShort(Globals.net_message);
		var beam = TemporaryEntities.Beams.FirstOrDefault(beam => beam.Entity == entity);

		if (beam == null)
			TemporaryEntities.Beams.Add(beam = new());

		beam.Entity = entity;
		beam.Model = model;
		beam.EndTime = Globals.cl.time + 200;
		beam.Start = MSG.ReadPos(Globals.net_message);
		beam.End = MSG.ReadPos(Globals.net_message);
		beam.Offset = Vector3.Zero;
	}

	/* =================
	 * CL_ParseBeam2
	 * ================= */
	private static void ParseBeam2(model_t model)
	{
		var entity = MSG.ReadShort(Globals.net_message);
		var beam = TemporaryEntities.Beams.FirstOrDefault(beam => beam.Entity == entity);

		if (beam == null)
			TemporaryEntities.Beams.Add(beam = new());

		beam.Entity = entity;
		beam.Model = model;
		beam.EndTime = Globals.cl.time + 200;
		beam.Start = MSG.ReadPos(Globals.net_message);
		beam.End = MSG.ReadPos(Globals.net_message);
		beam.Offset = MSG.ReadPos(Globals.net_message);
	}

	/* =================
	 * CL_ParsePlayerBeam
	 * ================= */
	private static void ParsePlayerBeam(model_t model)
	{
		var entity = MSG.ReadShort(Globals.net_message);
		var beam = TemporaryEntities.PlayerBeams.FirstOrDefault(beam => beam.Entity == entity);

		if (beam == null)
			TemporaryEntities.PlayerBeams.Add(beam = new());

		beam.Entity = entity;
		beam.Model = model;
		beam.EndTime = Globals.cl.time + 200;
		beam.Start = MSG.ReadPos(Globals.net_message);
		beam.End = MSG.ReadPos(Globals.net_message);

		if (model == TemporaryEntities.cl_mod_heatbeam)
			beam.Offset = new(2, 7, -3);
		else if (model == TemporaryEntities.cl_mod_monster_heatbeam)
		{
			beam.Model = TemporaryEntities.cl_mod_heatbeam;
			beam.Offset = Vector3.Zero;
		}
		else
			beam.Offset = MSG.ReadPos(Globals.net_message);
	}

	/* =================
	 * CL_ParseLightning
	 * ================= */
	private static int ParseLightning(model_t model)
	{
		var entity = MSG.ReadShort(Globals.net_message);
		var entityTarget = MSG.ReadShort(Globals.net_message);
		var beam = TemporaryEntities.Beams.FirstOrDefault(beam => beam.Entity == entity && beam.EntityTarget == entityTarget);

		if (beam == null)
			TemporaryEntities.Beams.Add(beam = new());

		beam.Entity = entity;
		beam.EntityTarget = entityTarget;
		beam.Model = model;
		beam.EndTime = Globals.cl.time + 200;
		beam.Start = MSG.ReadPos(Globals.net_message);
		beam.End = MSG.ReadPos(Globals.net_message);
		beam.Offset = Vector3.Zero;

		return entity;
	}

	/* =================
	 * CL_ParseLaser
	 * ================= */
	private static void ParseLaser(int colors)
	{
		var laser = new Laser
		{
			Entity =
			{
				flags = Defines.RF_TRANSLUCENT | Defines.RF_BEAM,
				alpha = 0.30f,
				skinnum = (colors >> (Lib.rand() % 4 * 8)) & 0xff,
				model = null,
				frame = 4
			},
			EndTime = Globals.cl.time + 100
		};

		Math3D.VectorCopy(MSG.ReadPos(Globals.net_message), laser.Entity.origin);
		Math3D.VectorCopy(MSG.ReadPos(Globals.net_message), laser.Entity.oldorigin);

		TemporaryEntities.Lasers.Add(laser);
	}

	/* =================
	 * CL_ParseSteam
	 * ================= */
	private static void ParseSteam()
	{
		var id = MSG.ReadShort(Globals.net_message);

		if (id != -1)
		{
			var sustain = new Sustain
			{
				Id = id,
				Count = MSG.ReadByte(Globals.net_message),
				Position = MSG.ReadPos(Globals.net_message),
				Direction = MSG.ReadDir(Globals.net_message),
				Color = MSG.ReadByte(Globals.net_message) & 0xff,
				Magnitude = MSG.ReadShort(Globals.net_message),
				EndTime = Globals.cl.time + MSG.ReadLong(Globals.net_message),
				Think = CL_newfx.ParticleSteamEffect2,
				ThinkInterval = 100,
				NextThink = Globals.cl.time
			};

			TemporaryEntities.Sustains.Add(sustain);
		}
		else
		{
			var count = MSG.ReadByte(Globals.net_message);
			var pos = MSG.ReadPos(Globals.net_message);
			var dir = MSG.ReadDir(Globals.net_message);
			var color = MSG.ReadByte(Globals.net_message) & 0xff;
			var magnitude = MSG.ReadShort(Globals.net_message);
			CL_newfx.ParticleSteamEffect(pos, dir, color, count, magnitude);
		}
	}

	/* =================
	 * CL_ParseWidow
	 * ================= */
	private static void ParseWidow()
	{
		var id = MSG.ReadShort(Globals.net_message);

		var sustain = new Sustain
		{
			Id = id,
			Position = MSG.ReadPos(Globals.net_message),
			EndTime = Globals.cl.time + 2100,
			Think = CL_newfx.Widowbeamout,
			ThinkInterval = 1,
			NextThink = Globals.cl.time
		};

		TemporaryEntities.Sustains.Add(sustain);
	}

	/* =================
	 * CL_ParseNuke
	 * ================= */
	private static void ParseNuke()
	{
		var id = MSG.ReadShort(Globals.net_message);

		var sustain = new Sustain
		{
			Id = id,
			Position = MSG.ReadPos(Globals.net_message),
			EndTime = Globals.cl.time + 1000,
			Think = CL_newfx.Nukeblast,
			ThinkInterval = 1,
			NextThink = Globals.cl.time
		};

		TemporaryEntities.Sustains.Add(sustain);
	}

	/* =================
	 * CL_ParseTEnt
	 * ================= */
	public static void Parse()
	{
		var type = MSG.ReadByte(Globals.net_message);

		switch (type)
		{
			case Defines.TE_BLOOD:
			{
				var pos = MSG.ReadPos(Globals.net_message);
				var dir = MSG.ReadDir(Globals.net_message);

				CL_fx.ParticleEffect(pos, dir, 0xe8, 60);

				break;
			}

			case Defines.TE_GUNSHOT:
			case Defines.TE_SPARKS:
			case Defines.TE_BULLET_SPARKS:
			{
				var pos = MSG.ReadPos(Globals.net_message);
				var dir = MSG.ReadDir(Globals.net_message);

				if (type == Defines.TE_GUNSHOT)
					CL_fx.ParticleEffect(pos, dir, 0, 40);
				else
					CL_fx.ParticleEffect(pos, dir, 0xe0, 6);

				if (type != Defines.TE_SPARKS)
				{
					TemporaryEntities.SmokeAndFlash(pos);

					var random = Lib.rand() & 15;

					var sound = random switch
					{
						1 => TemporaryEntities.cl_sfx_ric1,
						2 => TemporaryEntities.cl_sfx_ric2,
						3 => TemporaryEntities.cl_sfx_ric3,
						_ => null
					};

					if (sound != null)
						S.StartSound(pos, 0, 0, sound, 1, Defines.ATTN_NORM, 0);
				}

				break;
			}

			case Defines.TE_SCREEN_SPARKS:
			case Defines.TE_SHIELD_SPARKS:
			{
				var pos = MSG.ReadPos(Globals.net_message);
				var dir = MSG.ReadDir(Globals.net_message);

				CL_fx.ParticleEffect(pos, dir, type == Defines.TE_SCREEN_SPARKS ? 0xd0 : 0xb0, 40);
				S.StartSound(pos, 0, 0, TemporaryEntities.cl_sfx_lashit, 1, Defines.ATTN_NORM, 0);

				break;
			}

			case Defines.TE_SHOTGUN:
			{
				var pos = MSG.ReadPos(Globals.net_message);
				var dir = MSG.ReadDir(Globals.net_message);

				CL_fx.ParticleEffect(pos, dir, 0, 20);
				TemporaryEntities.SmokeAndFlash(pos);

				break;
			}

			case Defines.TE_SPLASH:
			{
				var count = MSG.ReadByte(Globals.net_message);
				var pos = MSG.ReadPos(Globals.net_message);
				var dir = MSG.ReadDir(Globals.net_message);
				var r = MSG.ReadByte(Globals.net_message);
				var color = r > 6 ? 0x00 : new byte[] { 0x00, 0xe0, 0xb0, 0x50, 0xd0, 0xe0, 0xe8 }[r];

				CL_fx.ParticleEffect(pos, dir, color, count);

				if (r == Defines.SPLASH_SPARKS)
				{
					r = Lib.rand() & 3;

					S.StartSound(
						pos,
						0,
						0,
						r switch
						{
							0 => TemporaryEntities.cl_sfx_spark5,
							1 => TemporaryEntities.cl_sfx_spark6,
							_ => TemporaryEntities.cl_sfx_spark7
						},
						1,
						Defines.ATTN_STATIC,
						0
					);
				}

				break;
			}

			case Defines.TE_LASER_SPARKS:
			{
				var count = MSG.ReadByte(Globals.net_message);
				var pos = MSG.ReadPos(Globals.net_message);
				var dir = MSG.ReadDir(Globals.net_message);
				var color = MSG.ReadByte(Globals.net_message);

				CL_fx.ParticleEffect2(pos, dir, color, count);

				break;
			}

			case Defines.TE_BLUEHYPERBLASTER:
			{
				var pos = MSG.ReadPos(Globals.net_message);
				var dir = MSG.ReadDir(Globals.net_message);

				CL_fx.BlasterParticles(pos, dir);

				break;
			}

			case Defines.TE_BLASTER:
			{
				var pos = MSG.ReadPos(Globals.net_message);
				var dir = MSG.ReadDir(Globals.net_message);

				CL_fx.BlasterParticles(pos, dir);

				var explosion = new Explosion
				{
					Type = TemporaryEntities.ex_misc,
					Entity = { flags = Defines.RF_FULLBRIGHT | Defines.RF_TRANSLUCENT, model = TemporaryEntities.cl_mod_explode },
					Start = Globals.cl.frame.servertime - 100,
					Light = 150,
					LightColor = Color.FromArgb(0xff, 0xff, 0x00),
					Frames = 4
				};

				explosion.Entity.angles[0] = (float)(Math.Acos(dir.Z) / Math.PI * 180);

				explosion.Entity.angles[1] = dir.X != 0.0f
					? (float)(Math.Atan2(dir.Y, dir.X) / Math.PI * 180)
					: dir.Y switch
					{
						> 0 => 90,
						< 0 => 270,
						_ => 0
					};

				Math3D.VectorCopy(pos, explosion.Entity.origin);
				TemporaryEntities.Explosions.Add(explosion);

				S.StartSound(pos, 0, 0, TemporaryEntities.cl_sfx_lashit, 1, Defines.ATTN_NORM, 0);

				break;
			}

			case Defines.TE_RAILTRAIL:
			{
				var pos = MSG.ReadPos(Globals.net_message);
				var pos2 = MSG.ReadPos(Globals.net_message);

				CL_fx.RailTrail(pos, pos2);
				S.StartSound(pos2, 0, 0, TemporaryEntities.cl_sfx_railg, 1, Defines.ATTN_NORM, 0);

				break;
			}

			case Defines.TE_EXPLOSION2:
			case Defines.TE_GRENADE_EXPLOSION:
			case Defines.TE_GRENADE_EXPLOSION_WATER:
			{
				var pos = MSG.ReadPos(Globals.net_message);

				var explosion = new Explosion
				{
					Type = TemporaryEntities.ex_poly,
					Entity = { flags = Defines.RF_FULLBRIGHT, model = TemporaryEntities.cl_mod_explo4 },
					Start = Globals.cl.frame.servertime - 100,
					Light = 350,
					LightColor = Color.FromArgb(0xff, 0x80, 0x80),
					Frames = 19,
					BaseFrame = 30
				};

				explosion.Entity.angles[1] = Lib.rand() % 360;

				Math3D.VectorCopy(pos, explosion.Entity.origin);
				TemporaryEntities.Explosions.Add(explosion);

				CL_fx.ExplosionParticles(pos);

				S.StartSound(
					pos,
					0,
					0,
					type == Defines.TE_GRENADE_EXPLOSION_WATER ? TemporaryEntities.cl_sfx_watrexp : TemporaryEntities.cl_sfx_grenexp,
					1,
					Defines.ATTN_NORM,
					0
				);

				break;
			}

			case Defines.TE_PLASMA_EXPLOSION:
			{
				var pos = MSG.ReadPos(Globals.net_message);

				var explosion = new Explosion
				{
					Type = TemporaryEntities.ex_poly,
					Entity = { flags = Defines.RF_FULLBRIGHT, model = TemporaryEntities.cl_mod_explo4, angles = { [1] = Lib.rand() % 360 } },
					Start = Globals.cl.frame.servertime - 100,
					Light = 350,
					LightColor = Color.FromArgb(0xff, 0x80, 0x80),
					Frames = 15,
					BaseFrame = Globals.rnd.NextDouble() < 0.5 ? 15 : 0
				};

				Math3D.VectorCopy(pos, explosion.Entity.origin);
				TemporaryEntities.Explosions.Add(explosion);

				CL_fx.ExplosionParticles(pos);
				S.StartSound(pos, 0, 0, TemporaryEntities.cl_sfx_rockexp, 1, Defines.ATTN_NORM, 0);

				break;
			}

			case Defines.TE_EXPLOSION1:
			case Defines.TE_EXPLOSION1_BIG:
			case Defines.TE_ROCKET_EXPLOSION:
			case Defines.TE_ROCKET_EXPLOSION_WATER:
			case Defines.TE_EXPLOSION1_NP:
			{
				var pos = MSG.ReadPos(Globals.net_message);

				var explosion = new Explosion
				{
					Type = TemporaryEntities.ex_poly,
					Entity =
					{
						flags = Defines.RF_FULLBRIGHT,
						model = type != Defines.TE_EXPLOSION1_BIG ? TemporaryEntities.cl_mod_explo4 : TemporaryEntities.cl_mod_explo4_big,
						angles = { [1] = Lib.rand() % 360 }
					},
					Start = Globals.cl.frame.servertime - 100,
					Light = 350,
					LightColor = Color.FromArgb(0xff, 0x80, 0x80),
					BaseFrame = Globals.rnd.NextDouble() < 0.5 ? 15 : 0,
					Frames = 15
				};

				Math3D.VectorCopy(pos, explosion.Entity.origin);
				TemporaryEntities.Explosions.Add(explosion);

				if (type != Defines.TE_EXPLOSION1_BIG && type != Defines.TE_EXPLOSION1_NP)
					CL_fx.ExplosionParticles(pos);

				S.StartSound(
					pos,
					0,
					0,
					type == Defines.TE_ROCKET_EXPLOSION_WATER ? TemporaryEntities.cl_sfx_watrexp : TemporaryEntities.cl_sfx_rockexp,
					1,
					Defines.ATTN_NORM,
					0
				);

				break;
			}

			case Defines.TE_BFG_EXPLOSION:
			{
				var pos = MSG.ReadPos(Globals.net_message);

				var explosion = new Explosion
				{
					Type = TemporaryEntities.ex_poly,
					Entity = { flags = Defines.RF_FULLBRIGHT, model = TemporaryEntities.cl_mod_bfg_explo },
					Start = Globals.cl.frame.servertime - 100,
					Light = 350,
					LightColor = Color.FromArgb(0x00, 0xff, 0x00)
				};

				explosion.Entity.flags |= Defines.RF_TRANSLUCENT;
				explosion.Entity.alpha = 0.30f;
				explosion.Frames = 4;
				Math3D.VectorCopy(pos, explosion.Entity.origin);
				TemporaryEntities.Explosions.Add(explosion);

				break;
			}

			case Defines.TE_BFG_BIGEXPLOSION:
			{
				var pos = MSG.ReadPos(Globals.net_message);

				CL_fx.BFGExplosionParticles(pos);

				break;
			}

			case Defines.TE_BFG_LASER:
			{
				TemporaryEntities.ParseLaser(unchecked((int)0xd0d1d2d3));

				break;
			}

			case Defines.TE_BUBBLETRAIL:
			{
				var pos = MSG.ReadPos(Globals.net_message);
				var pos2 = MSG.ReadPos(Globals.net_message);

				CL_fx.BubbleTrail(pos, pos2);

				break;
			}

			case Defines.TE_PARASITE_ATTACK:
			case Defines.TE_MEDIC_CABLE_ATTACK:
			{
				TemporaryEntities.ParseBeam(TemporaryEntities.cl_mod_parasite_segment);

				break;
			}

			case Defines.TE_BOSSTPORT:
			{
				var pos = MSG.ReadPos(Globals.net_message);

				CL_fx.BigTeleportParticles(pos);
				S.StartSound(pos, 0, 0, S.RegisterSound("misc/bigtele.wav"), 1, Defines.ATTN_NONE, 0);

				break;
			}

			case Defines.TE_GRAPPLE_CABLE:
			{
				TemporaryEntities.ParseBeam2(TemporaryEntities.cl_mod_grapple_cable);

				break;
			}

			case Defines.TE_WELDING_SPARKS:
			{
				var count = MSG.ReadByte(Globals.net_message);
				var pos = MSG.ReadPos(Globals.net_message);
				var dir = MSG.ReadDir(Globals.net_message);
				var color = MSG.ReadByte(Globals.net_message);

				CL_fx.ParticleEffect2(pos, dir, color, count);

				var explosion = new Explosion
				{
					Type = TemporaryEntities.ex_flash,
					Entity = { flags = Defines.RF_BEAM, model = TemporaryEntities.cl_mod_flash },
					Start = Globals.cl.frame.servertime - 0.1f,
					Light = 100 + Lib.rand() % 75,
					LightColor = Color.FromArgb(0xff, 0xff, 0x4c),
					Frames = 2
				};

				Math3D.VectorCopy(pos, explosion.Entity.origin);
				TemporaryEntities.Explosions.Add(explosion);

				break;
			}

			case Defines.TE_GREENBLOOD:
			{
				var pos = MSG.ReadPos(Globals.net_message);
				var dir = MSG.ReadDir(Globals.net_message);

				CL_fx.ParticleEffect2(pos, dir, 0xdf, 30);

				break;
			}

			case Defines.TE_TUNNEL_SPARKS:
			{
				var count = MSG.ReadByte(Globals.net_message);
				var pos = MSG.ReadPos(Globals.net_message);
				var dir = MSG.ReadDir(Globals.net_message);
				var color = MSG.ReadByte(Globals.net_message);

				CL_fx.ParticleEffect3(pos, dir, color, count);

				break;
			}

			case Defines.TE_BLASTER2:
			case Defines.TE_FLECHETTE:
			{
				var pos = MSG.ReadPos(Globals.net_message);
				var dir = MSG.ReadDir(Globals.net_message);

				CL_newfx.BlasterParticles2(pos, dir, type == Defines.TE_BLASTER2 ? 0xd0 : 0x6f);

				var explosion = new Explosion
				{
					Type = TemporaryEntities.ex_misc,
					Entity =
					{
						flags = Defines.RF_FULLBRIGHT | Defines.RF_TRANSLUCENT,
						skinnum = type == Defines.TE_BLASTER2 ? 1 : 2,
						model = TemporaryEntities.cl_mod_explode,
						angles = new[]
						{
							(float)(Math.Acos(dir.Z) / Math.PI * 180),
							dir.X != 0.0f
								? (float)(Math.Atan2(dir.Y, dir.X) / Math.PI * 180)
								: dir.Y switch
								{
									> 0 => 90,
									< 0 => 270,
									_ => 0
								},
							0
						}
					},
					Start = Globals.cl.frame.servertime - 100,
					Light = 150,
					LightColor = type == Defines.TE_BLASTER2 ? Color.FromArgb(0, 0xff, 0) : Color.FromArgb(0x30, 0x68, 0xbf),
					Frames = 4
				};

				TemporaryEntities.Explosions.Add(explosion);
				Math3D.VectorCopy(pos, explosion.Entity.origin);

				S.StartSound(pos, 0, 0, TemporaryEntities.cl_sfx_lashit, 1, Defines.ATTN_NORM, 0);

				break;
			}

			case Defines.TE_LIGHTNING:
			{
				var entity = TemporaryEntities.ParseLightning(TemporaryEntities.cl_mod_lightning);
				S.StartSound((Vector3?)null, entity, Defines.CHAN_WEAPON, TemporaryEntities.cl_sfx_lightning, 1, Defines.ATTN_NORM, 0);

				break;
			}

			case Defines.TE_DEBUGTRAIL:
			{
				var pos = MSG.ReadPos(Globals.net_message);
				var pos2 = MSG.ReadPos(Globals.net_message);

				CL_newfx.DebugTrail(pos, pos2);

				break;
			}

			case Defines.TE_PLAIN_EXPLOSION:
			{
				var pos = MSG.ReadPos(Globals.net_message);

				var explosion = new Explosion
				{
					Type = TemporaryEntities.ex_poly,
					Entity = { flags = Defines.RF_FULLBRIGHT, angles = new[] { 0, Lib.rand() % 360f, 0 }, model = TemporaryEntities.cl_mod_explo4 },
					Start = Globals.cl.frame.servertime - 100,
					Light = 350,
					LightColor = Color.FromArgb(0xff, 0x80, 0x80),
					BaseFrame = Globals.rnd.NextDouble() < 0.5 ? 15 : 0,
					Frames = 15
				};

				TemporaryEntities.Explosions.Add(explosion);
				Math3D.VectorCopy(pos, explosion.Entity.origin);

				S.StartSound(pos, 0, 0, TemporaryEntities.cl_sfx_rockexp, 1, Defines.ATTN_NORM, 0);

				break;
			}

			case Defines.TE_FLASHLIGHT:
			{
				var pos = MSG.ReadPos(Globals.net_message);

				var entity = MSG.ReadShort(Globals.net_message);
				CL_newfx.Flashlight(entity, pos);

				break;
			}

			case Defines.TE_FORCEWALL:
			{
				var pos = MSG.ReadPos(Globals.net_message);
				var pos2 = MSG.ReadPos(Globals.net_message);
				var color = MSG.ReadByte(Globals.net_message);

				CL_newfx.ForceWall(pos, pos2, color);

				break;
			}

			case Defines.TE_HEATBEAM:
			{
				TemporaryEntities.ParsePlayerBeam(TemporaryEntities.cl_mod_heatbeam);

				break;
			}

			case Defines.TE_MONSTER_HEATBEAM:
			{
				TemporaryEntities.ParsePlayerBeam(TemporaryEntities.cl_mod_monster_heatbeam);

				break;
			}

			case Defines.TE_HEATBEAM_SPARKS:
			{
				var pos = MSG.ReadPos(Globals.net_message);
				var dir = MSG.ReadDir(Globals.net_message);

				CL_newfx.ParticleSteamEffect(pos, dir, 0x08, 50, 60);
				S.StartSound(pos, 0, 0, TemporaryEntities.cl_sfx_lashit, 1, Defines.ATTN_NORM, 0);

				break;
			}

			case Defines.TE_HEATBEAM_STEAM:
			{
				var pos = MSG.ReadPos(Globals.net_message);
				var dir = MSG.ReadDir(Globals.net_message);

				CL_newfx.ParticleSteamEffect(pos, dir, 0xe0, 20, 60);
				S.StartSound(pos, 0, 0, TemporaryEntities.cl_sfx_lashit, 1, Defines.ATTN_NORM, 0);

				break;
			}

			case Defines.TE_STEAM:
			{
				TemporaryEntities.ParseSteam();

				break;
			}

			case Defines.TE_BUBBLETRAIL2:
			{
				var pos = MSG.ReadPos(Globals.net_message);
				var pos2 = MSG.ReadPos(Globals.net_message);

				CL_newfx.BubbleTrail2(pos, pos2, 8);
				S.StartSound(pos, 0, 0, TemporaryEntities.cl_sfx_lashit, 1, Defines.ATTN_NORM, 0);

				break;
			}

			case Defines.TE_MOREBLOOD:
			{
				var pos = MSG.ReadPos(Globals.net_message);
				var dir = MSG.ReadDir(Globals.net_message);

				CL_fx.ParticleEffect(pos, dir, 0xe8, 250);

				break;
			}

			case Defines.TE_CHAINFIST_SMOKE:
			{
				var pos = MSG.ReadPos(Globals.net_message);

				CL_newfx.ParticleSmokeEffect(pos, new(0, 0, 1), 0, 20, 20);

				break;
			}

			case Defines.TE_ELECTRIC_SPARKS:
			{
				var pos = MSG.ReadPos(Globals.net_message);
				var dir = MSG.ReadDir(Globals.net_message);

				CL_fx.ParticleEffect(pos, dir, 0x75, 40);
				S.StartSound(pos, 0, 0, TemporaryEntities.cl_sfx_lashit, 1, Defines.ATTN_NORM, 0);

				break;
			}

			case Defines.TE_TRACKER_EXPLOSION:
			{
				var pos = MSG.ReadPos(Globals.net_message);

				CL_newfx.ColorFlash(pos, 0, 150, -1, -1, -1);
				CL_newfx.ColorExplosionParticles(pos, 0, 1);
				S.StartSound(pos, 0, 0, TemporaryEntities.cl_sfx_disrexp, 1, Defines.ATTN_NORM, 0);

				break;
			}

			case Defines.TE_TELEPORT_EFFECT:
			case Defines.TE_DBALL_GOAL:
			{
				var pos = MSG.ReadPos(Globals.net_message);

				CL_fx.TeleportParticles(pos);

				break;
			}

			case Defines.TE_WIDOWBEAMOUT:
			{
				TemporaryEntities.ParseWidow();

				break;
			}

			case Defines.TE_NUKEBLAST:
			{
				TemporaryEntities.ParseNuke();

				break;
			}

			case Defines.TE_WIDOWSPLASH:
			{
				var pos = MSG.ReadPos(Globals.net_message);

				CL_newfx.WidowSplash(pos);

				break;
			}

			default:
			{
				clientserver.Com_Error(Defines.ERR_DROP, "CL_ParseTEnt: bad type");

				break;
			}
		}
	}

	/* =================
	 * CL_AddBeams
	 * ================= */
	private static void AddBeams()
	{
		TemporaryEntities.Beams.RemoveAll(static beam => beam.EndTime < Globals.cl.time);

		foreach (var beam in TemporaryEntities.Beams)
		{
			if (beam.Entity == Globals.cl.playernum + 1)
			{
				Math3D.VectorCopy(Globals.cl.refdef.vieworg, ref beam.Start);
				beam.Start.Z -= 22;
			}

			var dist = new float[3];
			var org = new float[3];

			Math3D.VectorAdd(beam.Start, beam.Offset, org);
			Math3D.VectorSubtract(beam.End, org, dist);

			float yaw;
			float pitch;

			if (dist[1] == 0 && dist[0] == 0)
			{
				yaw = 0;
				pitch = dist[2] > 0 ? 90 : 270;
			}
			else
			{
				if (dist[0] != 0)
					yaw = (float)(Math.Atan2(dist[1], dist[0]) * 180 / Math.PI);
				else if (dist[1] > 0)
					yaw = 90;
				else
					yaw = 270;

				if (yaw < 0)
					yaw += 360;

				var forward = (float)Math.Sqrt(dist[0] * dist[0] + dist[1] * dist[1]);

				pitch = (float)(Math.Atan2(dist[2], forward) * -180 / Math.PI);

				if (pitch < 0)
					pitch += 360;
			}

			var entity = new entity_t();

			var d = Math3D.VectorNormalize(dist);

			float modelLength;

			if (beam.Model == TemporaryEntities.cl_mod_lightning)
			{
				modelLength = 35;
				d -= 20;
			}
			else
				modelLength = 30;

			var steps = (float)Math.Ceiling(d / modelLength);
			var len = (d - modelLength) / (steps - 1);

			if (beam.Model == TemporaryEntities.cl_mod_lightning && d <= modelLength)
			{
				Math3D.VectorCopy(beam.End, entity.origin);

				entity.model = beam.Model;
				entity.flags = Defines.RF_FULLBRIGHT;
				entity.angles[0] = pitch;
				entity.angles[1] = yaw;
				entity.angles[2] = Lib.rand() % 360;

				V.AddEntity(entity);

				return;
			}

			while (d > 0)
			{
				Math3D.VectorCopy(org, entity.origin);

				entity.model = beam.Model;

				if (beam.Model == TemporaryEntities.cl_mod_lightning)
				{
					entity.flags = Defines.RF_FULLBRIGHT;
					entity.angles[0] = -pitch;
					entity.angles[1] = yaw + 180;
					entity.angles[2] = Lib.rand() % 360;
				}
				else
				{
					entity.angles[0] = pitch;
					entity.angles[1] = yaw;
					entity.angles[2] = Lib.rand() % 360;
				}

				V.AddEntity(entity);

				for (var i = 0; i < 3; i++)
					org[i] += dist[i] * len;

				d -= modelLength;
			}
		}
	}

	/* =================
	 * CL_AddPlayerBeams
	 * ================= */
	private static void AddPlayerBeams()
	{
		TemporaryEntities.PlayerBeams.RemoveAll(static beam => beam.EndTime < Globals.cl.time);

		var frame = 0;
		var dist = new float[3];
		var org = new float[3];
		var f = new float[3];
		var u = new float[3];
		var r = new float[3];

		var handMultiplier = Globals.hand switch
		{
			{ value: 2 } => 0f,
			{ value: 1 } => -1f,
			_ => 1f
		};

		foreach (var beam in TemporaryEntities.PlayerBeams)
		{
			if (TemporaryEntities.cl_mod_heatbeam != null && beam.Model == TemporaryEntities.cl_mod_heatbeam)
			{
				if (beam.Entity == Globals.cl.playernum + 1)
				{
					var ps = Globals.cl.frame.playerstate;
					var j = (Globals.cl.frame.serverframe - 1) & Defines.UPDATE_MASK;
					var frameOld = Globals.cl.frames[j];

					if (frameOld.serverframe != Globals.cl.frame.serverframe - 1 || !frameOld.valid)
						frameOld = Globals.cl.frame;

					var ops = frameOld.playerstate;

					beam.Start = new(
						Globals.cl.refdef.vieworg[0] + ops.gunoffset[0] + Globals.cl.lerpfrac * (ps.gunoffset[0] - ops.gunoffset[0]),
						Globals.cl.refdef.vieworg[1] + ops.gunoffset[1] + Globals.cl.lerpfrac * (ps.gunoffset[1] - ops.gunoffset[1]),
						Globals.cl.refdef.vieworg[2] + ops.gunoffset[2] + Globals.cl.lerpfrac * (ps.gunoffset[2] - ops.gunoffset[2])
					);

					Math3D.VectorMA(beam.Start, handMultiplier * beam.Offset.X, Globals.cl.v_right, org);
					Math3D.VectorMA(org, beam.Offset.Y, Globals.cl.v_forward, org);
					Math3D.VectorMA(org, beam.Offset.Z, Globals.cl.v_up, org);

					if (Globals.hand is { value: 2 })
						Math3D.VectorMA(org, -1, Globals.cl.v_up, org);

					Math3D.VectorCopy(Globals.cl.v_right, r);
					Math3D.VectorCopy(Globals.cl.v_forward, f);
					Math3D.VectorCopy(Globals.cl.v_up, u);
				}
				else
					Math3D.VectorCopy(beam.Start, org);
			}
			else
			{
				if (beam.Entity == Globals.cl.playernum + 1)
				{
					Math3D.VectorCopy(Globals.cl.refdef.vieworg, ref beam.Start);
					beam.Start.Z -= 22;
				}

				Math3D.VectorAdd(beam.Start, beam.Offset, org);
			}

			Math3D.VectorSubtract(beam.End, org, dist);

			float len;

			if (TemporaryEntities.cl_mod_heatbeam != null && beam.Model == TemporaryEntities.cl_mod_heatbeam && beam.Entity == Globals.cl.playernum + 1)
			{
				len = Math3D.VectorLength(dist);
				Math3D.VectorScale(f, len, dist);
				Math3D.VectorMA(dist, handMultiplier * beam.Offset.X, r, dist);
				Math3D.VectorMA(dist, beam.Offset.Y, f, dist);
				Math3D.VectorMA(dist, beam.Offset.Z, u, dist);

				if (Globals.hand is { value: 2 })
					Math3D.VectorMA(org, -1, Globals.cl.v_up, org);
			}

			float yaw;
			float pitch;

			if (dist[1] == 0 && dist[0] == 0)
			{
				yaw = 0;
				pitch = dist[2] > 0 ? 90 : 270;
			}
			else
			{
				if (dist[0] != 0)
					yaw = (float)(Math.Atan2(dist[1], dist[0]) * 180 / Math.PI);
				else if (dist[1] > 0)
					yaw = 90;
				else
					yaw = 270;

				if (yaw < 0)
					yaw += 360;

				var forward = (float)Math.Sqrt(dist[0] * dist[0] + dist[1] * dist[1]);
				pitch = (float)(Math.Atan2(dist[2], forward) * -180 / Math.PI);

				if (pitch < 0)
					pitch += 360;
			}

			var entity = new entity_t();

			if (TemporaryEntities.cl_mod_heatbeam != null && beam.Model == TemporaryEntities.cl_mod_heatbeam)
			{
				if (beam.Entity != Globals.cl.playernum + 1)
				{
					frame = 2;

					entity.angles[0] = -pitch;
					entity.angles[1] = yaw + 180;
					entity.angles[2] = 0;

					Math3D.AngleVectors(entity.angles, f, r, u);

					if (!Math3D.VectorEquals(beam.Offset, Globals.vec3_origin))
					{
						Math3D.VectorMA(org, -beam.Offset.X + 1, r, org);
						Math3D.VectorMA(org, -beam.Offset.Y, f, org);
						Math3D.VectorMA(org, -beam.Offset.Z - 10, u, org);
					}
					else
						CL_newfx.MonsterPlasma_Shell(beam.Start);
				}
				else
					frame = 1;
			}

			if (TemporaryEntities.cl_mod_heatbeam != null && beam.Model == TemporaryEntities.cl_mod_heatbeam && beam.Entity == Globals.cl.playernum + 1)
				CL_newfx.Heatbeam(org, dist);

			var d = Math3D.VectorNormalize(dist);

			float modelLength;

			if (beam.Model == TemporaryEntities.cl_mod_heatbeam)
				modelLength = 32;
			else if (beam.Model == TemporaryEntities.cl_mod_lightning)
			{
				modelLength = 35;
				d -= 20;
			}
			else
				modelLength = 30;

			var steps = (float)Math.Ceiling(d / modelLength);
			len = (d - modelLength) / (steps - 1);

			if (beam.Model == TemporaryEntities.cl_mod_lightning && d <= modelLength)
			{
				Math3D.VectorCopy(beam.End, entity.origin);

				entity.model = beam.Model;
				entity.flags = Defines.RF_FULLBRIGHT;
				entity.angles[0] = pitch;
				entity.angles[1] = yaw;
				entity.angles[2] = Lib.rand() % 360;

				V.AddEntity(entity);

				return;
			}

			while (d > 0)
			{
				Math3D.VectorCopy(org, entity.origin);
				entity.model = beam.Model;

				if (TemporaryEntities.cl_mod_heatbeam != null && beam.Model == TemporaryEntities.cl_mod_heatbeam)
				{
					entity.flags = Defines.RF_FULLBRIGHT;
					entity.angles[0] = -pitch;
					entity.angles[1] = yaw + 180;
					entity.angles[2] = Globals.cl.time % 360;
					entity.frame = frame;
				}
				else if (beam.Model == TemporaryEntities.cl_mod_lightning)
				{
					entity.flags = Defines.RF_FULLBRIGHT;
					entity.angles[0] = -pitch;
					entity.angles[1] = yaw + 180;
					entity.angles[2] = Lib.rand() % 360;
				}
				else
				{
					entity.angles[0] = pitch;
					entity.angles[1] = yaw;
					entity.angles[2] = Lib.rand() % 360;
				}

				V.AddEntity(entity);

				for (var i = 0; i < 3; i++)
					org[i] += dist[i] * len;

				d -= modelLength;
			}
		}
	}

	/* =================
	 * CL_AddExplosions
	 * ================= */
	private static void AddExplosions()
	{
		TemporaryEntities.Explosions.RemoveAll(static explosion => explosion.Type == TemporaryEntities.ex_free);

		foreach (var explosion in TemporaryEntities.Explosions)
		{
			var frac = (Globals.cl.time - explosion.Start) / 100;
			var fracBase = (int)Math.Floor(frac);
			var entity = explosion.Entity;

			switch (explosion.Type)
			{
				case TemporaryEntities.ex_mflash:
					if (fracBase >= explosion.Frames - 1)
						explosion.Type = TemporaryEntities.ex_free;

					break;

				case TemporaryEntities.ex_misc:
					if (fracBase >= explosion.Frames - 1)
					{
						explosion.Type = TemporaryEntities.ex_free;

						break;
					}

					entity.alpha = 1 - frac / (explosion.Frames - 1);

					break;

				case TemporaryEntities.ex_flash:
					if (fracBase >= 1)
					{
						explosion.Type = TemporaryEntities.ex_free;

						break;
					}

					entity.alpha = 1;

					break;

				case TemporaryEntities.ex_poly:
					if (fracBase >= explosion.Frames - 1)
					{
						explosion.Type = TemporaryEntities.ex_free;

						break;
					}

					entity.alpha = (16 - fracBase) / 16f;

					if (fracBase < 10)
					{
						entity.skinnum = fracBase >> 1;

						if (entity.skinnum < 0)
							entity.skinnum = 0;
					}
					else
					{
						entity.flags |= Defines.RF_TRANSLUCENT;
						entity.skinnum = fracBase < 13 ? 5 : 6;
					}

					break;

				case TemporaryEntities.ex_poly2:
					if (fracBase >= explosion.Frames - 1)
					{
						explosion.Type = TemporaryEntities.ex_free;

						break;
					}

					entity.alpha = (5 - fracBase) / 5f;
					entity.skinnum = 0;
					entity.flags |= Defines.RF_TRANSLUCENT;

					break;
			}

			if (explosion.Type == TemporaryEntities.ex_free)
				continue;

			if (explosion.Light != 0)
			{
				V.AddLight(
					entity.origin,
					explosion.Light * entity.alpha,
					explosion.LightColor.R / 255f,
					explosion.LightColor.G / 255f,
					explosion.LightColor.B / 255f
				);
			}

			Math3D.VectorCopy(entity.origin, entity.oldorigin);

			if (fracBase < 0)
				fracBase = 0;

			entity.frame = explosion.BaseFrame + fracBase + 1;
			entity.oldframe = explosion.BaseFrame + fracBase;
			entity.backlerp = 1.0f - Globals.cl.lerpfrac;
			V.AddEntity(entity);
		}
	}

	/* =================
	 * CL_AddLasers
	 * ================= */
	private static void AddLasers()
	{
		TemporaryEntities.Lasers.RemoveAll(static laser => laser.EndTime < Globals.cl.time);

		foreach (var laser in TemporaryEntities.Lasers)
			V.AddEntity(laser.Entity);
	}

	/* =================
	 * CL_Sustains
	 * ================= */
	private static void ProcessSustain()
	{
		TemporaryEntities.Sustains.RemoveAll(static sustain => sustain.EndTime < Globals.cl.time);

		foreach (var sustain in TemporaryEntities.Sustains.Where(static sustain => Globals.cl.time >= sustain.NextThink))
			sustain.Think?.Invoke(sustain);
	}

	/* =================
	 * CL_AddTEnts
	 * ================= */
	public static void Add()
	{
		TemporaryEntities.AddBeams();
		TemporaryEntities.AddPlayerBeams();
		TemporaryEntities.AddExplosions();
		TemporaryEntities.AddLasers();
		TemporaryEntities.ProcessSustain();
	}
}
