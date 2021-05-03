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
namespace Quake2Sharp.game
{
	using adapters;
	using System;
	using types;
	using util;

	public class GameWeapon
	{
		private static readonly EntTouchAdapter blaster_touch = new("blaster_touch", (self, other, plane, surf) =>
		{
			int mod;

			if (other == self.owner)
				return;

			if (surf != null && (surf.flags & Defines.SURF_SKY) != 0)
			{
				GameUtil.G_FreeEdict(self);

				return;
			}

			if (self.owner.client != null)
				PlayerWeapon.PlayerNoise(self.owner, self.s.origin, Defines.PNOISE_IMPACT);

			if (other.takedamage != 0)
			{
				if ((self.spawnflags & 1) != 0)
					mod = Defines.MOD_HYPERBLASTER;
				else
					mod = Defines.MOD_BLASTER;

				// bugfix null plane rst
				float[] normal;

				if (plane == null)
					normal = new float[3];
				else
					normal = plane.normal;

				GameCombat.T_Damage(other, self, self.owner, self.velocity, self.s.origin, normal, self.dmg, 1, Defines.DAMAGE_ENERGY, mod);
			}
			else
			{
				GameBase.gi.WriteByte(Defines.svc_temp_entity);
				GameBase.gi.WriteByte(Defines.TE_BLASTER);
				GameBase.gi.WritePosition(self.s.origin);

				if (plane == null)
					GameBase.gi.WriteDir(Globals.vec3_origin);
				else
					GameBase.gi.WriteDir(plane.normal);

				GameBase.gi.multicast(self.s.origin, Defines.MULTICAST_PVS);
			}

			GameUtil.G_FreeEdict(self);
		});

		private static readonly EntThinkAdapter Grenade_Explode = new("Grenade_Explode", ent =>
		{
			float[] origin = { 0, 0, 0 };
			int mod;

			if (ent.owner.client != null)
				PlayerWeapon.PlayerNoise(ent.owner, ent.s.origin, Defines.PNOISE_IMPACT);

			//FIXME: if we are onground then raise our Z just a bit since we
			// are a point?
			if (ent.enemy != null)
			{
				float points = 0;
				float[] v = { 0, 0, 0 };
				float[] dir = { 0, 0, 0 };

				Math3D.VectorAdd(ent.enemy.mins, ent.enemy.maxs, v);
				Math3D.VectorMA(ent.enemy.s.origin, 0.5f, v, v);
				Math3D.VectorSubtract(ent.s.origin, v, v);
				points = ent.dmg - 0.5f * Math3D.VectorLength(v);
				Math3D.VectorSubtract(ent.enemy.s.origin, ent.s.origin, dir);

				if ((ent.spawnflags & 1) != 0)
					mod = Defines.MOD_HANDGRENADE;
				else
					mod = Defines.MOD_GRENADE;

				GameCombat.T_Damage(ent.enemy, ent, ent.owner, dir, ent.s.origin, Globals.vec3_origin, (int)points, (int)points, Defines.DAMAGE_RADIUS, mod);
			}

			if ((ent.spawnflags & 2) != 0)
				mod = Defines.MOD_HELD_GRENADE;
			else if ((ent.spawnflags & 1) != 0)
				mod = Defines.MOD_HG_SPLASH;
			else
				mod = Defines.MOD_G_SPLASH;

			GameCombat.T_RadiusDamage(ent, ent.owner, ent.dmg, ent.enemy, ent.dmg_radius, mod);

			Math3D.VectorMA(ent.s.origin, -0.02f, ent.velocity, origin);
			GameBase.gi.WriteByte(Defines.svc_temp_entity);

			if (ent.waterlevel != 0)
			{
				if (ent.groundentity != null)
					GameBase.gi.WriteByte(Defines.TE_GRENADE_EXPLOSION_WATER);
				else
					GameBase.gi.WriteByte(Defines.TE_ROCKET_EXPLOSION_WATER);
			}
			else
			{
				if (ent.groundentity != null)
					GameBase.gi.WriteByte(Defines.TE_GRENADE_EXPLOSION);
				else
					GameBase.gi.WriteByte(Defines.TE_ROCKET_EXPLOSION);
			}

			GameBase.gi.WritePosition(origin);
			GameBase.gi.multicast(ent.s.origin, Defines.MULTICAST_PHS);

			GameUtil.G_FreeEdict(ent);

			return true;
		});

		private static readonly EntTouchAdapter Grenade_Touch = new("Grenade_Touch", (ent, other, plane, surf) =>
		{
			if (other == ent.owner)
				return;

			if (surf != null && 0 != (surf.flags & Defines.SURF_SKY))
			{
				GameUtil.G_FreeEdict(ent);

				return;
			}

			if (other.takedamage == 0)
			{
				if ((ent.spawnflags & 1) != 0)
				{
					if (Lib.random() > 0.5f)
						GameBase.gi.sound(ent, Defines.CHAN_VOICE, GameBase.gi.soundindex("weapons/hgrenb1a.wav"), 1, Defines.ATTN_NORM, 0);
					else
						GameBase.gi.sound(ent, Defines.CHAN_VOICE, GameBase.gi.soundindex("weapons/hgrenb2a.wav"), 1, Defines.ATTN_NORM, 0);
				}
				else
					GameBase.gi.sound(ent, Defines.CHAN_VOICE, GameBase.gi.soundindex("weapons/grenlb1b.wav"), 1, Defines.ATTN_NORM, 0);

				return;
			}

			ent.enemy = other;
			GameWeapon.Grenade_Explode.think(ent);
		});

		/*
		 * ================= 
		 * fire_rocket 
		 * =================
		 */
		private static readonly EntTouchAdapter rocket_touch = new("rocket_touch", (ent, other, plane, surf) =>
		{
			float[] origin = { 0, 0, 0 };
			int n;

			if (other == ent.owner)
				return;

			if (surf != null && (surf.flags & Defines.SURF_SKY) != 0)
			{
				GameUtil.G_FreeEdict(ent);

				return;
			}

			if (ent.owner.client != null)
				PlayerWeapon.PlayerNoise(ent.owner, ent.s.origin, Defines.PNOISE_IMPACT);

			// calculate position for the explosion entity
			Math3D.VectorMA(ent.s.origin, -0.02f, ent.velocity, origin);

			if (other.takedamage != 0)
				GameCombat.T_Damage(other, ent, ent.owner, ent.velocity, ent.s.origin, plane.normal, ent.dmg, 0, 0, Defines.MOD_ROCKET);
			else
			{
				// don't throw any debris in net games
				if (GameBase.deathmatch.value == 0 && 0 == GameBase.coop.value)
				{
					if (surf != null && 0 == (surf.flags & (Defines.SURF_WARP | Defines.SURF_TRANS33 | Defines.SURF_TRANS66 | Defines.SURF_FLOWING)))
					{
						n = Lib.rand() % 5;

						while (n-- > 0)
							GameMisc.ThrowDebris(ent, "models/objects/debris2/tris.md2", 2, ent.s.origin);
					}
				}
			}

			GameCombat.T_RadiusDamage(ent, ent.owner, ent.radius_dmg, other, ent.dmg_radius, Defines.MOD_R_SPLASH);

			GameBase.gi.WriteByte(Defines.svc_temp_entity);

			if (ent.waterlevel != 0)
				GameBase.gi.WriteByte(Defines.TE_ROCKET_EXPLOSION_WATER);
			else
				GameBase.gi.WriteByte(Defines.TE_ROCKET_EXPLOSION);

			GameBase.gi.WritePosition(origin);
			GameBase.gi.multicast(ent.s.origin, Defines.MULTICAST_PHS);

			GameUtil.G_FreeEdict(ent);
		});

		/*
		 * ================= 
		 * fire_bfg 
		 * =================
		 */
		private static readonly EntThinkAdapter bfg_explode = new("bfg_explode", self =>
		{
			edict_t ent;
			float points;
			float[] v = { 0, 0, 0 };
			float dist;

			EdictIterator edit = null;

			if (self.s.frame == 0)
			{
				// the BFG effect
				ent = null;

				while ((edit = GameBase.findradius(edit, self.s.origin, self.dmg_radius)) != null)
				{
					ent = edit.o;

					if (ent.takedamage == 0)
						continue;

					if (ent == self.owner)
						continue;

					if (!GameCombat.CanDamage(ent, self))
						continue;

					if (!GameCombat.CanDamage(ent, self.owner))
						continue;

					Math3D.VectorAdd(ent.mins, ent.maxs, v);
					Math3D.VectorMA(ent.s.origin, 0.5f, v, v);
					Math3D.VectorSubtract(self.s.origin, v, v);
					dist = Math3D.VectorLength(v);
					points = (float)(self.radius_dmg * (1.0 - Math.Sqrt(dist / self.dmg_radius)));

					if (ent == self.owner)
						points = points * 0.5f;

					GameBase.gi.WriteByte(Defines.svc_temp_entity);
					GameBase.gi.WriteByte(Defines.TE_BFG_EXPLOSION);
					GameBase.gi.WritePosition(ent.s.origin);
					GameBase.gi.multicast(ent.s.origin, Defines.MULTICAST_PHS);

					GameCombat.T_Damage(
						ent,
						self,
						self.owner,
						self.velocity,
						ent.s.origin,
						Globals.vec3_origin,
						(int)points,
						0,
						Defines.DAMAGE_ENERGY,
						Defines.MOD_BFG_EFFECT
					);
				}
			}

			self.nextthink = GameBase.level.time + Defines.FRAMETIME;
			self.s.frame++;

			if (self.s.frame == 5)
				self.think = GameUtil.G_FreeEdictA;

			return true;
		});

		private static readonly EntTouchAdapter bfg_touch = new("bfg_touch", (self, other, plane, surf) =>
		{
			if (other == self.owner)
				return;

			if (surf != null && (surf.flags & Defines.SURF_SKY) != 0)
			{
				GameUtil.G_FreeEdict(self);

				return;
			}

			if (self.owner.client != null)
				PlayerWeapon.PlayerNoise(self.owner, self.s.origin, Defines.PNOISE_IMPACT);

			// core explosion - prevents firing it into the wall/floor
			if (other.takedamage != 0)
				GameCombat.T_Damage(other, self, self.owner, self.velocity, self.s.origin, plane.normal, 200, 0, 0, Defines.MOD_BFG_BLAST);

			GameCombat.T_RadiusDamage(self, self.owner, 200, other, 100, Defines.MOD_BFG_BLAST);

			GameBase.gi.sound(self, Defines.CHAN_VOICE, GameBase.gi.soundindex("weapons/bfg__x1b.wav"), 1, Defines.ATTN_NORM, 0);
			self.solid = Defines.SOLID_NOT;
			self.touch = null;
			Math3D.VectorMA(self.s.origin, -1 * Defines.FRAMETIME, self.velocity, self.s.origin);
			Math3D.VectorClear(self.velocity);
			self.s.modelindex = GameBase.gi.modelindex("sprites/s_bfg3.sp2");
			self.s.frame = 0;
			self.s.sound = 0;
			self.s.effects &= ~Defines.EF_ANIM_ALLFAST;
			self.think = GameWeapon.bfg_explode;
			self.nextthink = GameBase.level.time + Defines.FRAMETIME;
			self.enemy = other;

			GameBase.gi.WriteByte(Defines.svc_temp_entity);
			GameBase.gi.WriteByte(Defines.TE_BFG_BIGEXPLOSION);
			GameBase.gi.WritePosition(self.s.origin);
			GameBase.gi.multicast(self.s.origin, Defines.MULTICAST_PVS);
		});

		private static readonly EntThinkAdapter bfg_think = new("bfg_think", self =>
		{
			edict_t ent;
			edict_t ignore;
			float[] point = { 0, 0, 0 };
			float[] dir = { 0, 0, 0 };
			float[] start = { 0, 0, 0 };
			float[] end = { 0, 0, 0 };
			int dmg;
			trace_t tr;

			if (GameBase.deathmatch.value != 0)
				dmg = 5;
			else
				dmg = 10;

			EdictIterator edit = null;

			while ((edit = GameBase.findradius(edit, self.s.origin, 256)) != null)
			{
				ent = edit.o;

				if (ent == self)
					continue;

				if (ent == self.owner)
					continue;

				if (ent.takedamage == 0)
					continue;

				if (0 == (ent.svflags & Defines.SVF_MONSTER) && null == ent.client && Lib.strcmp(ent.classname, "misc_explobox") != 0)
					continue;

				Math3D.VectorMA(ent.absmin, 0.5f, ent.size, point);

				Math3D.VectorSubtract(point, self.s.origin, dir);
				Math3D.VectorNormalize(dir);

				ignore = self;
				Math3D.VectorCopy(self.s.origin, start);
				Math3D.VectorMA(start, 2048, dir, end);

				while (true)
				{
					tr = GameBase.gi.trace(start, null, null, end, ignore, Defines.CONTENTS_SOLID | Defines.CONTENTS_MONSTER | Defines.CONTENTS_DEADMONSTER);

					if (null == tr.ent)
						break;

					// hurt it if we can
					if (tr.ent.takedamage != 0 && 0 == (tr.ent.flags & Defines.FL_IMMUNE_LASER) && tr.ent != self.owner)
					{
						GameCombat.T_Damage(
							tr.ent,
							self,
							self.owner,
							dir,
							tr.endpos,
							Globals.vec3_origin,
							dmg,
							1,
							Defines.DAMAGE_ENERGY,
							Defines.MOD_BFG_LASER
						);
					}

					// if we hit something that's not a monster or player we're
					// done
					if (0 == (tr.ent.svflags & Defines.SVF_MONSTER) && null == tr.ent.client)
					{
						GameBase.gi.WriteByte(Defines.svc_temp_entity);
						GameBase.gi.WriteByte(Defines.TE_LASER_SPARKS);
						GameBase.gi.WriteByte(4);
						GameBase.gi.WritePosition(tr.endpos);
						GameBase.gi.WriteDir(tr.plane.normal);
						GameBase.gi.WriteByte(self.s.skinnum);
						GameBase.gi.multicast(tr.endpos, Defines.MULTICAST_PVS);

						break;
					}

					ignore = tr.ent;
					Math3D.VectorCopy(tr.endpos, start);
				}

				GameBase.gi.WriteByte(Defines.svc_temp_entity);
				GameBase.gi.WriteByte(Defines.TE_BFG_LASER);
				GameBase.gi.WritePosition(self.s.origin);
				GameBase.gi.WritePosition(tr.endpos);
				GameBase.gi.multicast(self.s.origin, Defines.MULTICAST_PHS);
			}

			self.nextthink = GameBase.level.time + Defines.FRAMETIME;

			return true;
		});

		/*
		 * ================= 
		 * check_dodge
		 * 
		 * This is a support routine used when a client is firing a non-instant
		 * attack weapon. It checks to see if a monster's dodge function should be
		 * called. 
		 * =================
		 */
		private static void check_dodge(edict_t self, float[] start, float[] dir, int speed)
		{
			float[] end = { 0, 0, 0 };
			float[] v = { 0, 0, 0 };
			trace_t tr;
			float eta;

			// easy mode only ducks one quarter the time
			if (GameBase.skill.value == 0)
			{
				if (Lib.random() > 0.25)
					return;
			}

			Math3D.VectorMA(start, 8192, dir, end);
			tr = GameBase.gi.trace(start, null, null, end, self, Defines.MASK_SHOT);

			if (tr.ent != null
				&& (tr.ent.svflags & Defines.SVF_MONSTER) != 0
				&& tr.ent.health > 0
				&& null != tr.ent.monsterinfo.dodge
				&& GameUtil.infront(tr.ent, self))
			{
				Math3D.VectorSubtract(tr.endpos, start, v);
				eta = (Math3D.VectorLength(v) - tr.ent.maxs[0]) / speed;
				tr.ent.monsterinfo.dodge.dodge(tr.ent, self, eta);
			}
		}

		/*
		 * ================= 
		 * fire_hit
		 * 
		 * Used for all impact (hit/punch/slash) attacks 
		 * =================
		 */
		public static bool fire_hit(edict_t self, float[] aim, int damage, int kick)
		{
			trace_t tr;
			float[] forward = { 0, 0, 0 }, right = { 0, 0, 0 }, up = { 0, 0, 0 };
			float[] v = { 0, 0, 0 };
			float[] point = { 0, 0, 0 };
			float range;
			float[] dir = { 0, 0, 0 };

			//see if enemy is in range
			Math3D.VectorSubtract(self.enemy.s.origin, self.s.origin, dir);
			range = Math3D.VectorLength(dir);

			if (range > aim[0])
				return false;

			if (aim[1] > self.mins[0] && aim[1] < self.maxs[0])
			{
				// the hit is straight on so back the range up to the edge of their
				// bbox
				range -= self.enemy.maxs[0];
			}
			else
			{
				// this is a side hit so adjust the "right" value out to the edge of
				// their bbox
				if (aim[1] < 0)
					aim[1] = self.enemy.mins[0];
				else
					aim[1] = self.enemy.maxs[0];
			}

			Math3D.VectorMA(self.s.origin, range, dir, point);

			tr = GameBase.gi.trace(self.s.origin, null, null, point, self, Defines.MASK_SHOT);

			if (tr.fraction < 1)
			{
				if (0 == tr.ent.takedamage)
					return false;

				// if it will hit any client/monster then hit the one we wanted to
				// hit
				if ((tr.ent.svflags & Defines.SVF_MONSTER) != 0 || tr.ent.client != null)
					tr.ent = self.enemy;
			}

			Math3D.AngleVectors(self.s.angles, forward, right, up);
			Math3D.VectorMA(self.s.origin, range, forward, point);
			Math3D.VectorMA(point, aim[1], right, point);
			Math3D.VectorMA(point, aim[2], up, point);
			Math3D.VectorSubtract(point, self.enemy.s.origin, dir);

			// do the damage
			GameCombat.T_Damage(tr.ent, self, self, dir, point, Globals.vec3_origin, damage, kick / 2, Defines.DAMAGE_NO_KNOCKBACK, Defines.MOD_HIT);

			if (0 == (tr.ent.svflags & Defines.SVF_MONSTER) && null == tr.ent.client)
				return false;

			// do our special form of knockback here
			Math3D.VectorMA(self.enemy.absmin, 0.5f, self.enemy.size, v);
			Math3D.VectorSubtract(v, point, v);
			Math3D.VectorNormalize(v);
			Math3D.VectorMA(self.enemy.velocity, kick, v, self.enemy.velocity);

			if (self.enemy.velocity[2] > 0)
				self.enemy.groundentity = null;

			return true;
		}

		/*
		 * ================= 
		 * fire_lead
		 * 
		 * This is an internal support routine used for bullet/pellet based weapons.
		 * =================
		 */
		public static void fire_lead(edict_t self, float[] start, float[] aimdir, int damage, int kick, int te_impact, int hspread, int vspread, int mod)
		{
			trace_t tr;
			float[] dir = { 0, 0, 0 };
			float[] forward = { 0, 0, 0 }, right = { 0, 0, 0 }, up = { 0, 0, 0 };
			float[] end = { 0, 0, 0 };
			float r;
			float u;
			float[] water_start = { 0, 0, 0 };
			var water = false;
			var content_mask = Defines.MASK_SHOT | Defines.MASK_WATER;

			tr = GameBase.gi.trace(self.s.origin, null, null, start, self, Defines.MASK_SHOT);

			if (!(tr.fraction < 1.0))
			{
				Math3D.vectoangles(aimdir, dir);
				Math3D.AngleVectors(dir, forward, right, up);

				r = Lib.crandom() * hspread;
				u = Lib.crandom() * vspread;
				Math3D.VectorMA(start, 8192, forward, end);
				Math3D.VectorMA(end, r, right, end);
				Math3D.VectorMA(end, u, up, end);

				if ((GameBase.gi.pointcontents(start) & Defines.MASK_WATER) != 0)
				{
					water = true;
					Math3D.VectorCopy(start, water_start);
					content_mask &= ~Defines.MASK_WATER;
				}

				tr = GameBase.gi.trace(start, null, null, end, self, content_mask);

				// see if we hit water
				if ((tr.contents & Defines.MASK_WATER) != 0)
				{
					int color;

					water = true;
					Math3D.VectorCopy(tr.endpos, water_start);

					if (!Math3D.VectorEquals(start, tr.endpos))
					{
						if ((tr.contents & Defines.CONTENTS_WATER) != 0)
						{
							if (Lib.strcmp(tr.surface.name, "*brwater") == 0)
								color = Defines.SPLASH_BROWN_WATER;
							else
								color = Defines.SPLASH_BLUE_WATER;
						}
						else if ((tr.contents & Defines.CONTENTS_SLIME) != 0)
							color = Defines.SPLASH_SLIME;
						else if ((tr.contents & Defines.CONTENTS_LAVA) != 0)
							color = Defines.SPLASH_LAVA;
						else
							color = Defines.SPLASH_UNKNOWN;

						if (color != Defines.SPLASH_UNKNOWN)
						{
							GameBase.gi.WriteByte(Defines.svc_temp_entity);
							GameBase.gi.WriteByte(Defines.TE_SPLASH);
							GameBase.gi.WriteByte(8);
							GameBase.gi.WritePosition(tr.endpos);
							GameBase.gi.WriteDir(tr.plane.normal);
							GameBase.gi.WriteByte(color);
							GameBase.gi.multicast(tr.endpos, Defines.MULTICAST_PVS);
						}

						// change bullet's course when it enters water
						Math3D.VectorSubtract(end, start, dir);
						Math3D.vectoangles(dir, dir);
						Math3D.AngleVectors(dir, forward, right, up);
						r = Lib.crandom() * hspread * 2;
						u = Lib.crandom() * vspread * 2;
						Math3D.VectorMA(water_start, 8192, forward, end);
						Math3D.VectorMA(end, r, right, end);
						Math3D.VectorMA(end, u, up, end);
					}

					// re-trace ignoring water this time
					tr = GameBase.gi.trace(water_start, null, null, end, self, Defines.MASK_SHOT);
				}
			}

			// send gun puff / flash
			if (!(tr.surface != null && 0 != (tr.surface.flags & Defines.SURF_SKY)))
			{
				if (tr.fraction < 1.0)
				{
					if (tr.ent.takedamage != 0)
						GameCombat.T_Damage(tr.ent, self, self, aimdir, tr.endpos, tr.plane.normal, damage, kick, Defines.DAMAGE_BULLET, mod);
					else
					{
						if (!"sky".Equals(tr.surface.name))
						{
							GameBase.gi.WriteByte(Defines.svc_temp_entity);
							GameBase.gi.WriteByte(te_impact);
							GameBase.gi.WritePosition(tr.endpos);
							GameBase.gi.WriteDir(tr.plane.normal);
							GameBase.gi.multicast(tr.endpos, Defines.MULTICAST_PVS);

							if (self.client != null)
								PlayerWeapon.PlayerNoise(self, tr.endpos, Defines.PNOISE_IMPACT);
						}
					}
				}
			}

			// if went through water, determine where the end and make a bubble
			// trail
			if (water)
			{
				float[] pos = { 0, 0, 0 };

				Math3D.VectorSubtract(tr.endpos, water_start, dir);
				Math3D.VectorNormalize(dir);
				Math3D.VectorMA(tr.endpos, -2, dir, pos);

				if ((GameBase.gi.pointcontents(pos) & Defines.MASK_WATER) != 0)
					Math3D.VectorCopy(pos, tr.endpos);
				else
					tr = GameBase.gi.trace(pos, null, null, water_start, tr.ent, Defines.MASK_WATER);

				Math3D.VectorAdd(water_start, tr.endpos, pos);
				Math3D.VectorScale(pos, 0.5f, pos);

				GameBase.gi.WriteByte(Defines.svc_temp_entity);
				GameBase.gi.WriteByte(Defines.TE_BUBBLETRAIL);
				GameBase.gi.WritePosition(water_start);
				GameBase.gi.WritePosition(tr.endpos);
				GameBase.gi.multicast(pos, Defines.MULTICAST_PVS);
			}
		}

		/*
		 * ================= fire_bullet
		 * 
		 * Fires a single round. Used for machinegun and chaingun. Would be fine for
		 * pistols, rifles, etc.... =================
		 */
		public static void fire_bullet(edict_t self, float[] start, float[] aimdir, int damage, int kick, int hspread, int vspread, int mod)
		{
			GameWeapon.fire_lead(self, start, aimdir, damage, kick, Defines.TE_GUNSHOT, hspread, vspread, mod);
		}

		/*
		 * ================= 
		 * fire_shotgun
		 * 
		 * Shoots shotgun pellets. Used by shotgun and super shotgun.
		 * =================
		 */
		public static void fire_shotgun(edict_t self, float[] start, float[] aimdir, int damage, int kick, int hspread, int vspread, int count, int mod)
		{
			int i;

			for (i = 0; i < count; i++)
				GameWeapon.fire_lead(self, start, aimdir, damage, kick, Defines.TE_SHOTGUN, hspread, vspread, mod);
		}

		/*
		 * ================= 
		 * fire_blaster
		 * 
		 * Fires a single blaster bolt. Used by the blaster and hyper blaster.
		 * =================
		 */

		public static void fire_blaster(edict_t self, float[] start, float[] dir, int damage, int speed, int effect, bool hyper)
		{
			edict_t bolt;
			trace_t tr;

			Math3D.VectorNormalize(dir);

			bolt = GameUtil.G_Spawn();
			bolt.svflags = Defines.SVF_DEADMONSTER;

			// yes, I know it looks weird that projectiles are deadmonsters
			// what this means is that when prediction is used against the object
			// (blaster/hyperblaster shots), the player won't be solid clipped
			// against
			// the object. Right now trying to run into a firing hyperblaster
			// is very jerky since you are predicted 'against' the shots.
			Math3D.VectorCopy(start, bolt.s.origin);
			Math3D.VectorCopy(start, bolt.s.old_origin);
			Math3D.vectoangles(dir, bolt.s.angles);
			Math3D.VectorScale(dir, speed, bolt.velocity);
			bolt.movetype = Defines.MOVETYPE_FLYMISSILE;
			bolt.clipmask = Defines.MASK_SHOT;
			bolt.solid = Defines.SOLID_BBOX;
			bolt.s.effects |= effect;
			Math3D.VectorClear(bolt.mins);
			Math3D.VectorClear(bolt.maxs);
			bolt.s.modelindex = GameBase.gi.modelindex("models/objects/laser/tris.md2");
			bolt.s.sound = GameBase.gi.soundindex("misc/lasfly.wav");
			bolt.owner = self;
			bolt.touch = GameWeapon.blaster_touch;
			bolt.nextthink = GameBase.level.time + 2;
			bolt.think = GameUtil.G_FreeEdictA;
			bolt.dmg = damage;
			bolt.classname = "bolt";

			if (hyper)
				bolt.spawnflags = 1;

			GameBase.gi.linkentity(bolt);

			if (self.client != null)
				GameWeapon.check_dodge(self, bolt.s.origin, dir, speed);

			tr = GameBase.gi.trace(self.s.origin, null, null, bolt.s.origin, bolt, Defines.MASK_SHOT);

			if (tr.fraction < 1.0)
			{
				Math3D.VectorMA(bolt.s.origin, -10, dir, bolt.s.origin);
				bolt.touch.touch(bolt, tr.ent, GameBase.dummyplane, null);
			}
		}

		public static void fire_grenade(edict_t self, float[] start, float[] aimdir, int damage, int speed, float timer, float damage_radius)
		{
			edict_t grenade;
			float[] dir = { 0, 0, 0 };
			float[] forward = { 0, 0, 0 }, right = { 0, 0, 0 }, up = { 0, 0, 0 };

			Math3D.vectoangles(aimdir, dir);
			Math3D.AngleVectors(dir, forward, right, up);

			grenade = GameUtil.G_Spawn();
			Math3D.VectorCopy(start, grenade.s.origin);
			Math3D.VectorScale(aimdir, speed, grenade.velocity);
			Math3D.VectorMA(grenade.velocity, 200f + Lib.crandom() * 10.0f, up, grenade.velocity);
			Math3D.VectorMA(grenade.velocity, Lib.crandom() * 10.0f, right, grenade.velocity);
			Math3D.VectorSet(grenade.avelocity, 300, 300, 300);
			grenade.movetype = Defines.MOVETYPE_BOUNCE;
			grenade.clipmask = Defines.MASK_SHOT;
			grenade.solid = Defines.SOLID_BBOX;
			grenade.s.effects |= Defines.EF_GRENADE;
			Math3D.VectorClear(grenade.mins);
			Math3D.VectorClear(grenade.maxs);
			grenade.s.modelindex = GameBase.gi.modelindex("models/objects/grenade/tris.md2");
			grenade.owner = self;
			grenade.touch = GameWeapon.Grenade_Touch;
			grenade.nextthink = GameBase.level.time + timer;
			grenade.think = GameWeapon.Grenade_Explode;
			grenade.dmg = damage;
			grenade.dmg_radius = damage_radius;
			grenade.classname = "grenade";

			GameBase.gi.linkentity(grenade);
		}

		public static void fire_grenade2(edict_t self, float[] start, float[] aimdir, int damage, int speed, float timer, float damage_radius, bool held)
		{
			edict_t grenade;
			float[] dir = { 0, 0, 0 };
			float[] forward = { 0, 0, 0 }, right = { 0, 0, 0 }, up = { 0, 0, 0 };

			Math3D.vectoangles(aimdir, dir);
			Math3D.AngleVectors(dir, forward, right, up);

			grenade = GameUtil.G_Spawn();
			Math3D.VectorCopy(start, grenade.s.origin);
			Math3D.VectorScale(aimdir, speed, grenade.velocity);
			Math3D.VectorMA(grenade.velocity, 200f + Lib.crandom() * 10.0f, up, grenade.velocity);
			Math3D.VectorMA(grenade.velocity, Lib.crandom() * 10.0f, right, grenade.velocity);
			Math3D.VectorSet(grenade.avelocity, 300f, 300f, 300f);
			grenade.movetype = Defines.MOVETYPE_BOUNCE;
			grenade.clipmask = Defines.MASK_SHOT;
			grenade.solid = Defines.SOLID_BBOX;
			grenade.s.effects |= Defines.EF_GRENADE;
			Math3D.VectorClear(grenade.mins);
			Math3D.VectorClear(grenade.maxs);
			grenade.s.modelindex = GameBase.gi.modelindex("models/objects/grenade2/tris.md2");
			grenade.owner = self;
			grenade.touch = GameWeapon.Grenade_Touch;
			grenade.nextthink = GameBase.level.time + timer;
			grenade.think = GameWeapon.Grenade_Explode;
			grenade.dmg = damage;
			grenade.dmg_radius = damage_radius;
			grenade.classname = "hgrenade";

			if (held)
				grenade.spawnflags = 3;
			else
				grenade.spawnflags = 1;

			grenade.s.sound = GameBase.gi.soundindex("weapons/hgrenc1b.wav");

			if (timer <= 0.0)
				GameWeapon.Grenade_Explode.think(grenade);
			else
			{
				GameBase.gi.sound(self, Defines.CHAN_WEAPON, GameBase.gi.soundindex("weapons/hgrent1a.wav"), 1, Defines.ATTN_NORM, 0);
				GameBase.gi.linkentity(grenade);
			}
		}

		public static void fire_rocket(edict_t self, float[] start, float[] dir, int damage, int speed, float damage_radius, int radius_damage)
		{
			edict_t rocket;

			rocket = GameUtil.G_Spawn();
			Math3D.VectorCopy(start, rocket.s.origin);
			Math3D.VectorCopy(dir, rocket.movedir);
			Math3D.vectoangles(dir, rocket.s.angles);
			Math3D.VectorScale(dir, speed, rocket.velocity);
			rocket.movetype = Defines.MOVETYPE_FLYMISSILE;
			rocket.clipmask = Defines.MASK_SHOT;
			rocket.solid = Defines.SOLID_BBOX;
			rocket.s.effects |= Defines.EF_ROCKET;
			Math3D.VectorClear(rocket.mins);
			Math3D.VectorClear(rocket.maxs);
			rocket.s.modelindex = GameBase.gi.modelindex("models/objects/rocket/tris.md2");
			rocket.owner = self;
			rocket.touch = GameWeapon.rocket_touch;
			rocket.nextthink = GameBase.level.time + 8000 / speed;
			rocket.think = GameUtil.G_FreeEdictA;
			rocket.dmg = damage;
			rocket.radius_dmg = radius_damage;
			rocket.dmg_radius = damage_radius;
			rocket.s.sound = GameBase.gi.soundindex("weapons/rockfly.wav");
			rocket.classname = "rocket";

			if (self.client != null)
				GameWeapon.check_dodge(self, rocket.s.origin, dir, speed);

			GameBase.gi.linkentity(rocket);
		}

		/*
		 * ================= 
		 * fire_rail 
		 * =================
		 */
		public static void fire_rail(edict_t self, float[] start, float[] aimdir, int damage, int kick)
		{
			float[] from = { 0, 0, 0 };
			float[] end = { 0, 0, 0 };
			trace_t tr = null;
			edict_t ignore;
			int mask;
			bool water;

			Math3D.VectorMA(start, 8192f, aimdir, end);
			Math3D.VectorCopy(start, from);
			ignore = self;
			water = false;
			mask = Defines.MASK_SHOT | Defines.CONTENTS_SLIME | Defines.CONTENTS_LAVA;

			while (ignore != null)
			{
				tr = GameBase.gi.trace(from, null, null, end, ignore, mask);

				if ((tr.contents & (Defines.CONTENTS_SLIME | Defines.CONTENTS_LAVA)) != 0)
				{
					mask &= ~(Defines.CONTENTS_SLIME | Defines.CONTENTS_LAVA);
					water = true;
				}
				else
				{
					//ZOID--added so rail goes through SOLID_BBOX entities (gibs,
					// etc)
					if ((tr.ent.svflags & Defines.SVF_MONSTER) != 0 || tr.ent.client != null || tr.ent.solid == Defines.SOLID_BBOX)
						ignore = tr.ent;
					else
						ignore = null;

					if (tr.ent != self && tr.ent.takedamage != 0)
						GameCombat.T_Damage(tr.ent, self, self, aimdir, tr.endpos, tr.plane.normal, damage, kick, 0, Defines.MOD_RAILGUN);
				}

				Math3D.VectorCopy(tr.endpos, from);
			}

			// send gun puff / flash
			GameBase.gi.WriteByte(Defines.svc_temp_entity);
			GameBase.gi.WriteByte(Defines.TE_RAILTRAIL);
			GameBase.gi.WritePosition(start);
			GameBase.gi.WritePosition(tr.endpos);
			GameBase.gi.multicast(self.s.origin, Defines.MULTICAST_PHS);

			// gi.multicast (start, MULTICAST_PHS);
			if (water)
			{
				GameBase.gi.WriteByte(Defines.svc_temp_entity);
				GameBase.gi.WriteByte(Defines.TE_RAILTRAIL);
				GameBase.gi.WritePosition(start);
				GameBase.gi.WritePosition(tr.endpos);
				GameBase.gi.multicast(tr.endpos, Defines.MULTICAST_PHS);
			}

			if (self.client != null)
				PlayerWeapon.PlayerNoise(self, tr.endpos, Defines.PNOISE_IMPACT);
		}

		public static void fire_bfg(edict_t self, float[] start, float[] dir, int damage, int speed, float damage_radius)
		{
			edict_t bfg;

			bfg = GameUtil.G_Spawn();
			Math3D.VectorCopy(start, bfg.s.origin);
			Math3D.VectorCopy(dir, bfg.movedir);
			Math3D.vectoangles(dir, bfg.s.angles);
			Math3D.VectorScale(dir, speed, bfg.velocity);
			bfg.movetype = Defines.MOVETYPE_FLYMISSILE;
			bfg.clipmask = Defines.MASK_SHOT;
			bfg.solid = Defines.SOLID_BBOX;
			bfg.s.effects |= Defines.EF_BFG | Defines.EF_ANIM_ALLFAST;
			Math3D.VectorClear(bfg.mins);
			Math3D.VectorClear(bfg.maxs);
			bfg.s.modelindex = GameBase.gi.modelindex("sprites/s_bfg1.sp2");
			bfg.owner = self;
			bfg.touch = GameWeapon.bfg_touch;
			bfg.nextthink = GameBase.level.time + 8000 / speed;
			bfg.think = GameUtil.G_FreeEdictA;
			bfg.radius_dmg = damage;
			bfg.dmg_radius = damage_radius;
			bfg.classname = "bfg blast";
			bfg.s.sound = GameBase.gi.soundindex("weapons/bfg__l1a.wav");

			bfg.think = GameWeapon.bfg_think;
			bfg.nextthink = GameBase.level.time + Defines.FRAMETIME;
			bfg.teammaster = bfg;
			bfg.teamchain = null;

			if (self.client != null)
				GameWeapon.check_dodge(self, bfg.s.origin, dir, speed);

			GameBase.gi.linkentity(bfg);
		}
	}
}
