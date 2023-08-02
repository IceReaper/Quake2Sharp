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
namespace QClient.game.monsters;

using adapters;
using types;
using util;

public class M_Brain
{
	public static readonly int FRAME_walk101 = 0;
	public static readonly int FRAME_walk102 = 1;
	public static readonly int FRAME_walk103 = 2;
	public static readonly int FRAME_walk104 = 3;
	public static readonly int FRAME_walk105 = 4;
	public static readonly int FRAME_walk106 = 5;
	public static readonly int FRAME_walk107 = 6;
	public static readonly int FRAME_walk108 = 7;
	public static readonly int FRAME_walk109 = 8;
	public static readonly int FRAME_walk110 = 9;
	public static readonly int FRAME_walk111 = 10;
	public static readonly int FRAME_walk112 = 11;
	public static readonly int FRAME_walk113 = 12;
	public static readonly int FRAME_walk201 = 13;
	public static readonly int FRAME_walk202 = 14;
	public static readonly int FRAME_walk203 = 15;
	public static readonly int FRAME_walk204 = 16;
	public static readonly int FRAME_walk205 = 17;
	public static readonly int FRAME_walk206 = 18;
	public static readonly int FRAME_walk207 = 19;
	public static readonly int FRAME_walk208 = 20;
	public static readonly int FRAME_walk209 = 21;
	public static readonly int FRAME_walk210 = 22;
	public static readonly int FRAME_walk211 = 23;
	public static readonly int FRAME_walk212 = 24;
	public static readonly int FRAME_walk213 = 25;
	public static readonly int FRAME_walk214 = 26;
	public static readonly int FRAME_walk215 = 27;
	public static readonly int FRAME_walk216 = 28;
	public static readonly int FRAME_walk217 = 29;
	public static readonly int FRAME_walk218 = 30;
	public static readonly int FRAME_walk219 = 31;
	public static readonly int FRAME_walk220 = 32;
	public static readonly int FRAME_walk221 = 33;
	public static readonly int FRAME_walk222 = 34;
	public static readonly int FRAME_walk223 = 35;
	public static readonly int FRAME_walk224 = 36;
	public static readonly int FRAME_walk225 = 37;
	public static readonly int FRAME_walk226 = 38;
	public static readonly int FRAME_walk227 = 39;
	public static readonly int FRAME_walk228 = 40;
	public static readonly int FRAME_walk229 = 41;
	public static readonly int FRAME_walk230 = 42;
	public static readonly int FRAME_walk231 = 43;
	public static readonly int FRAME_walk232 = 44;
	public static readonly int FRAME_walk233 = 45;
	public static readonly int FRAME_walk234 = 46;
	public static readonly int FRAME_walk235 = 47;
	public static readonly int FRAME_walk236 = 48;
	public static readonly int FRAME_walk237 = 49;
	public static readonly int FRAME_walk238 = 50;
	public static readonly int FRAME_walk239 = 51;
	public static readonly int FRAME_walk240 = 52;
	public static readonly int FRAME_attak101 = 53;
	public static readonly int FRAME_attak102 = 54;
	public static readonly int FRAME_attak103 = 55;
	public static readonly int FRAME_attak104 = 56;
	public static readonly int FRAME_attak105 = 57;
	public static readonly int FRAME_attak106 = 58;
	public static readonly int FRAME_attak107 = 59;
	public static readonly int FRAME_attak108 = 60;
	public static readonly int FRAME_attak109 = 61;
	public static readonly int FRAME_attak110 = 62;
	public static readonly int FRAME_attak111 = 63;
	public static readonly int FRAME_attak112 = 64;
	public static readonly int FRAME_attak113 = 65;
	public static readonly int FRAME_attak114 = 66;
	public static readonly int FRAME_attak115 = 67;
	public static readonly int FRAME_attak116 = 68;
	public static readonly int FRAME_attak117 = 69;
	public static readonly int FRAME_attak118 = 70;
	public static readonly int FRAME_attak201 = 71;
	public static readonly int FRAME_attak202 = 72;
	public static readonly int FRAME_attak203 = 73;
	public static readonly int FRAME_attak204 = 74;
	public static readonly int FRAME_attak205 = 75;
	public static readonly int FRAME_attak206 = 76;
	public static readonly int FRAME_attak207 = 77;
	public static readonly int FRAME_attak208 = 78;
	public static readonly int FRAME_attak209 = 79;
	public static readonly int FRAME_attak210 = 80;
	public static readonly int FRAME_attak211 = 81;
	public static readonly int FRAME_attak212 = 82;
	public static readonly int FRAME_attak213 = 83;
	public static readonly int FRAME_attak214 = 84;
	public static readonly int FRAME_attak215 = 85;
	public static readonly int FRAME_attak216 = 86;
	public static readonly int FRAME_attak217 = 87;
	public static readonly int FRAME_pain101 = 88;
	public static readonly int FRAME_pain102 = 89;
	public static readonly int FRAME_pain103 = 90;
	public static readonly int FRAME_pain104 = 91;
	public static readonly int FRAME_pain105 = 92;
	public static readonly int FRAME_pain106 = 93;
	public static readonly int FRAME_pain107 = 94;
	public static readonly int FRAME_pain108 = 95;
	public static readonly int FRAME_pain109 = 96;
	public static readonly int FRAME_pain110 = 97;
	public static readonly int FRAME_pain111 = 98;
	public static readonly int FRAME_pain112 = 99;
	public static readonly int FRAME_pain113 = 100;
	public static readonly int FRAME_pain114 = 101;
	public static readonly int FRAME_pain115 = 102;
	public static readonly int FRAME_pain116 = 103;
	public static readonly int FRAME_pain117 = 104;
	public static readonly int FRAME_pain118 = 105;
	public static readonly int FRAME_pain119 = 106;
	public static readonly int FRAME_pain120 = 107;
	public static readonly int FRAME_pain121 = 108;
	public static readonly int FRAME_pain201 = 109;
	public static readonly int FRAME_pain202 = 110;
	public static readonly int FRAME_pain203 = 111;
	public static readonly int FRAME_pain204 = 112;
	public static readonly int FRAME_pain205 = 113;
	public static readonly int FRAME_pain206 = 114;
	public static readonly int FRAME_pain207 = 115;
	public static readonly int FRAME_pain208 = 116;
	public static readonly int FRAME_pain301 = 117;
	public static readonly int FRAME_pain302 = 118;
	public static readonly int FRAME_pain303 = 119;
	public static readonly int FRAME_pain304 = 120;
	public static readonly int FRAME_pain305 = 121;
	public static readonly int FRAME_pain306 = 122;
	public static readonly int FRAME_death101 = 123;
	public static readonly int FRAME_death102 = 124;
	public static readonly int FRAME_death103 = 125;
	public static readonly int FRAME_death104 = 126;
	public static readonly int FRAME_death105 = 127;
	public static readonly int FRAME_death106 = 128;
	public static readonly int FRAME_death107 = 129;
	public static readonly int FRAME_death108 = 130;
	public static readonly int FRAME_death109 = 131;
	public static readonly int FRAME_death110 = 132;
	public static readonly int FRAME_death111 = 133;
	public static readonly int FRAME_death112 = 134;
	public static readonly int FRAME_death113 = 135;
	public static readonly int FRAME_death114 = 136;
	public static readonly int FRAME_death115 = 137;
	public static readonly int FRAME_death116 = 138;
	public static readonly int FRAME_death117 = 139;
	public static readonly int FRAME_death118 = 140;
	public static readonly int FRAME_death201 = 141;
	public static readonly int FRAME_death202 = 142;
	public static readonly int FRAME_death203 = 143;
	public static readonly int FRAME_death204 = 144;
	public static readonly int FRAME_death205 = 145;
	public static readonly int FRAME_duck01 = 146;
	public static readonly int FRAME_duck02 = 147;
	public static readonly int FRAME_duck03 = 148;
	public static readonly int FRAME_duck04 = 149;
	public static readonly int FRAME_duck05 = 150;
	public static readonly int FRAME_duck06 = 151;
	public static readonly int FRAME_duck07 = 152;
	public static readonly int FRAME_duck08 = 153;
	public static readonly int FRAME_defens01 = 154;
	public static readonly int FRAME_defens02 = 155;
	public static readonly int FRAME_defens03 = 156;
	public static readonly int FRAME_defens04 = 157;
	public static readonly int FRAME_defens05 = 158;
	public static readonly int FRAME_defens06 = 159;
	public static readonly int FRAME_defens07 = 160;
	public static readonly int FRAME_defens08 = 161;
	public static readonly int FRAME_stand01 = 162;
	public static readonly int FRAME_stand02 = 163;
	public static readonly int FRAME_stand03 = 164;
	public static readonly int FRAME_stand04 = 165;
	public static readonly int FRAME_stand05 = 166;
	public static readonly int FRAME_stand06 = 167;
	public static readonly int FRAME_stand07 = 168;
	public static readonly int FRAME_stand08 = 169;
	public static readonly int FRAME_stand09 = 170;
	public static readonly int FRAME_stand10 = 171;
	public static readonly int FRAME_stand11 = 172;
	public static readonly int FRAME_stand12 = 173;
	public static readonly int FRAME_stand13 = 174;
	public static readonly int FRAME_stand14 = 175;
	public static readonly int FRAME_stand15 = 176;
	public static readonly int FRAME_stand16 = 177;
	public static readonly int FRAME_stand17 = 178;
	public static readonly int FRAME_stand18 = 179;
	public static readonly int FRAME_stand19 = 180;
	public static readonly int FRAME_stand20 = 181;
	public static readonly int FRAME_stand21 = 182;
	public static readonly int FRAME_stand22 = 183;
	public static readonly int FRAME_stand23 = 184;
	public static readonly int FRAME_stand24 = 185;
	public static readonly int FRAME_stand25 = 186;
	public static readonly int FRAME_stand26 = 187;
	public static readonly int FRAME_stand27 = 188;
	public static readonly int FRAME_stand28 = 189;
	public static readonly int FRAME_stand29 = 190;
	public static readonly int FRAME_stand30 = 191;
	public static readonly int FRAME_stand31 = 192;
	public static readonly int FRAME_stand32 = 193;
	public static readonly int FRAME_stand33 = 194;
	public static readonly int FRAME_stand34 = 195;
	public static readonly int FRAME_stand35 = 196;
	public static readonly int FRAME_stand36 = 197;
	public static readonly int FRAME_stand37 = 198;
	public static readonly int FRAME_stand38 = 199;
	public static readonly int FRAME_stand39 = 200;
	public static readonly int FRAME_stand40 = 201;
	public static readonly int FRAME_stand41 = 202;
	public static readonly int FRAME_stand42 = 203;
	public static readonly int FRAME_stand43 = 204;
	public static readonly int FRAME_stand44 = 205;
	public static readonly int FRAME_stand45 = 206;
	public static readonly int FRAME_stand46 = 207;
	public static readonly int FRAME_stand47 = 208;
	public static readonly int FRAME_stand48 = 209;
	public static readonly int FRAME_stand49 = 210;
	public static readonly int FRAME_stand50 = 211;
	public static readonly int FRAME_stand51 = 212;
	public static readonly int FRAME_stand52 = 213;
	public static readonly int FRAME_stand53 = 214;
	public static readonly int FRAME_stand54 = 215;
	public static readonly int FRAME_stand55 = 216;
	public static readonly int FRAME_stand56 = 217;
	public static readonly int FRAME_stand57 = 218;
	public static readonly int FRAME_stand58 = 219;
	public static readonly int FRAME_stand59 = 220;
	public static readonly int FRAME_stand60 = 221;
	public static readonly float MODEL_SCALE = 1.000000f;
	private static int sound_chest_open;
	private static int sound_tentacles_extend;
	private static int sound_tentacles_retract;
	private static int sound_death;
	private static int sound_idle1;
	private static int sound_idle2;
	private static int sound_idle3;
	private static int sound_pain1;
	private static int sound_pain2;
	private static int sound_sight;
	private static int sound_search;
	private static int sound_melee1;
	private static int sound_melee2;
	private static int sound_melee3;

	private static readonly EntInteractAdapter brain_sight = new("brain_sight", (self, other) =>
	{
		GameBase.gi.sound(self, Defines.CHAN_VOICE, M_Brain.sound_sight, 1, Defines.ATTN_NORM, 0);

		return true;
	});

	private static readonly EntThinkAdapter brain_search = new("brain_search", self =>
	{
		GameBase.gi.sound(self, Defines.CHAN_VOICE, M_Brain.sound_search, 1, Defines.ATTN_NORM, 0);

		return true;
	});

	//
	//	   STAND
	//

	private static readonly mframe_t[] brain_frames_stand =
	{
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null)
	};

	private static readonly mmove_t brain_move_stand = new(M_Brain.FRAME_stand01, M_Brain.FRAME_stand30, M_Brain.brain_frames_stand, null);

	private static readonly EntThinkAdapter brain_stand = new("brain_stand", self =>
	{
		self.monsterinfo.currentmove = M_Brain.brain_move_stand;

		return true;
	});

	//
	//	   IDLE
	//

	private static readonly mframe_t[] brain_frames_idle =
	{
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null)
	};

	private static readonly mmove_t brain_move_idle = new(M_Brain.FRAME_stand31, M_Brain.FRAME_stand60, M_Brain.brain_frames_idle, M_Brain.brain_stand);

	private static readonly EntThinkAdapter brain_idle = new("brain_idle", self =>
	{
		GameBase.gi.sound(self, Defines.CHAN_AUTO, M_Brain.sound_idle3, 1, Defines.ATTN_IDLE, 0);
		self.monsterinfo.currentmove = M_Brain.brain_move_idle;

		return true;
	});

	//
	//	   WALK
	//
	private static readonly mframe_t[] brain_frames_walk1 =
	{
		new(GameAI.ai_walk, 7, null),
		new(GameAI.ai_walk, 2, null),
		new(GameAI.ai_walk, 3, null),
		new(GameAI.ai_walk, 3, null),
		new(GameAI.ai_walk, 1, null),
		new(GameAI.ai_walk, 0, null),
		new(GameAI.ai_walk, 0, null),
		new(GameAI.ai_walk, 9, null),
		new(GameAI.ai_walk, -4, null),
		new(GameAI.ai_walk, -1, null),
		new(GameAI.ai_walk, 2, null)
	};

	private static readonly mmove_t brain_move_walk1 = new(M_Brain.FRAME_walk101, M_Brain.FRAME_walk111, M_Brain.brain_frames_walk1, null);

	//	   walk2 is FUBAR, do not use
	/*
	 * # if 0 void brain_walk2_cycle(edict_t self) { if (random() > 0.1)
	 * self.monsterinfo.nextframe= FRAME_walk220; }
	 * 
	 * static mframe_t brain_frames_walk2[]= new mframe_t[] { new
	 * mframe_t(ai_walk, 3, null), new mframe_t(ai_walk, -2, null), new
	 * mframe_t(ai_walk, -4, null), new mframe_t(ai_walk, -3, null), new
	 * mframe_t(ai_walk, 0, null), new mframe_t(ai_walk, 1, null), new
	 * mframe_t(ai_walk, 12, null), new mframe_t(ai_walk, 0, null), new
	 * mframe_t(ai_walk, -3, null), new mframe_t(ai_walk, 0, null), new
	 * mframe_t(ai_walk, -2, null), new mframe_t(ai_walk, 0, null), new
	 * mframe_t(ai_walk, 0, null), new mframe_t(ai_walk, 1, null), new
	 * mframe_t(ai_walk, 0, null), new mframe_t(ai_walk, 0, null), new
	 * mframe_t(ai_walk, 0, null), new mframe_t(ai_walk, 0, null), new
	 * mframe_t(ai_walk, 0, null), new mframe_t(ai_walk, 10, null, // Cycle
	 * Start)
	 * 
	 * new mframe_t(ai_walk, -1, null), new mframe_t(ai_walk, 7, null), new
	 * mframe_t(ai_walk, 0, null), new mframe_t(ai_walk, 3, null), new
	 * mframe_t(ai_walk, -3, null), new mframe_t(ai_walk, 2, null), new
	 * mframe_t(ai_walk, 4, null), new mframe_t(ai_walk, -3, null), new
	 * mframe_t(ai_walk, 2, null), new mframe_t(ai_walk, 0, null), new
	 * mframe_t(ai_walk, 4, brain_walk2_cycle), new mframe_t(ai_walk, -1, null),
	 * new mframe_t(ai_walk, -1, null), new mframe_t(ai_walk, -8, null,) new
	 * mframe_t(ai_walk, 0, null), new mframe_t(ai_walk, 1, null), new
	 * mframe_t(ai_walk, 5, null), new mframe_t(ai_walk, 2, null), new
	 * mframe_t(ai_walk, -1, null), new mframe_t(ai_walk, -5, null)}; static
	 * mmove_t brain_move_walk2= new mmove_t(FRAME_walk201, FRAME_walk240,
	 * brain_frames_walk2, null);
	 *  # endif
	 */
	private static readonly EntThinkAdapter brain_walk = new("brain_walk", self =>
	{
		//			if (random() <= 0.5)
		self.monsterinfo.currentmove = M_Brain.brain_move_walk1;

		//		else
		//			self.monsterinfo.currentmove = &brain_move_walk2;
		return true;
	});

	//
	//	   DUCK
	//

	private static readonly EntThinkAdapter brain_duck_down = new("brain_duck_down", self =>
	{
		if ((self.monsterinfo.aiflags & Defines.AI_DUCKED) != 0)
			return true;

		self.monsterinfo.aiflags |= Defines.AI_DUCKED;
		self.maxs[2] -= 32;
		self.takedamage = Defines.DAMAGE_YES;
		GameBase.gi.linkentity(self);

		return true;
	});

	private static readonly EntThinkAdapter brain_duck_hold = new("brain_duck_hold", self =>
	{
		if (GameBase.level.time >= self.monsterinfo.pausetime)
			self.monsterinfo.aiflags &= ~Defines.AI_HOLD_FRAME;
		else
			self.monsterinfo.aiflags |= Defines.AI_HOLD_FRAME;

		return true;
	});

	private static readonly EntThinkAdapter brain_duck_up = new("brain_duck_up", self =>
	{
		self.monsterinfo.aiflags &= ~Defines.AI_DUCKED;
		self.maxs[2] += 32;
		self.takedamage = Defines.DAMAGE_AIM;
		GameBase.gi.linkentity(self);

		return true;
	});

	private static readonly EntDodgeAdapter brain_dodge = new("brain_dodge", (self, attacker, eta) =>
	{
		if (Lib.random() > 0.25)
			return;

		if (self.enemy == null)
			self.enemy = attacker;

		self.monsterinfo.pausetime = GameBase.level.time + eta + 0.5f;
		self.monsterinfo.currentmove = M_Brain.brain_move_duck;

		return;
	});

	private static readonly mframe_t[] brain_frames_death2 =
	{
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 9, null),
		new(GameAI.ai_move, 0, null)
	};

	private static readonly EntThinkAdapter brain_dead = new("brain_dead", self =>
	{
		Math3D.VectorSet(self.mins, -16, -16, -24);
		Math3D.VectorSet(self.maxs, 16, 16, -8);
		self.movetype = Defines.MOVETYPE_TOSS;
		self.svflags |= Defines.SVF_DEADMONSTER;
		self.nextthink = 0;
		GameBase.gi.linkentity(self);

		return true;
	});

	private static readonly mmove_t brain_move_death2 =
		new(M_Brain.FRAME_death201, M_Brain.FRAME_death205, M_Brain.brain_frames_death2, M_Brain.brain_dead);

	private static readonly mframe_t[] brain_frames_death1 =
	{
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, -2, null),
		new(GameAI.ai_move, 9, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null)
	};

	private static readonly mmove_t brain_move_death1 =
		new(M_Brain.FRAME_death101, M_Brain.FRAME_death118, M_Brain.brain_frames_death1, M_Brain.brain_dead);

	//
	//	   MELEE
	//

	private static readonly EntThinkAdapter brain_swing_right = new("brain_swing_right", self =>
	{
		GameBase.gi.sound(self, Defines.CHAN_BODY, M_Brain.sound_melee1, 1, Defines.ATTN_NORM, 0);

		return true;
	});

	private static readonly EntThinkAdapter brain_hit_right = new("brain_hit_right", self =>
	{
		float[] aim = { 0, 0, 0 };

		Math3D.VectorSet(aim, Defines.MELEE_DISTANCE, self.maxs[0], 8);

		if (GameWeapon.fire_hit(self, aim, 15 + Lib.rand() % 5, 40))
			GameBase.gi.sound(self, Defines.CHAN_WEAPON, M_Brain.sound_melee3, 1, Defines.ATTN_NORM, 0);

		return true;
	});

	private static readonly EntThinkAdapter brain_swing_left = new("brain_swing_left", self =>
	{
		GameBase.gi.sound(self, Defines.CHAN_BODY, M_Brain.sound_melee2, 1, Defines.ATTN_NORM, 0);

		return true;
	});

	private static readonly EntThinkAdapter brain_hit_left = new("brain_hit_left", self =>
	{
		float[] aim = { 0, 0, 0 };

		Math3D.VectorSet(aim, Defines.MELEE_DISTANCE, self.mins[0], 8);

		if (GameWeapon.fire_hit(self, aim, 15 + Lib.rand() % 5, 40))
			GameBase.gi.sound(self, Defines.CHAN_WEAPON, M_Brain.sound_melee3, 1, Defines.ATTN_NORM, 0);

		return true;
	});

	private static readonly EntThinkAdapter brain_chest_open = new("brain_chest_open", self =>
	{
		self.spawnflags &= ~65536;
		self.monsterinfo.power_armor_type = Defines.POWER_ARMOR_NONE;
		GameBase.gi.sound(self, Defines.CHAN_BODY, M_Brain.sound_chest_open, 1, Defines.ATTN_NORM, 0);

		return true;
	});

	private static readonly EntThinkAdapter brain_tentacle_attack = new("brain_tentacle_attack", self =>
	{
		float[] aim = { 0, 0, 0 };

		Math3D.VectorSet(aim, Defines.MELEE_DISTANCE, 0, 8);

		if (GameWeapon.fire_hit(self, aim, 10 + Lib.rand() % 5, -600) && GameBase.skill.value > 0)
			self.spawnflags |= 65536;

		GameBase.gi.sound(self, Defines.CHAN_WEAPON, M_Brain.sound_tentacles_retract, 1, Defines.ATTN_NORM, 0);

		return true;
	});

	private static readonly mframe_t[] brain_frames_attack1 =
	{
		new(GameAI.ai_charge, 8, null),
		new(GameAI.ai_charge, 3, null),
		new(GameAI.ai_charge, 5, null),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, -3, M_Brain.brain_swing_right),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, -5, null),
		new(GameAI.ai_charge, -7, M_Brain.brain_hit_right),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 6, M_Brain.brain_swing_left),
		new(GameAI.ai_charge, 1, null),
		new(GameAI.ai_charge, 2, M_Brain.brain_hit_left),
		new(GameAI.ai_charge, -3, null),
		new(GameAI.ai_charge, 6, null),
		new(GameAI.ai_charge, -1, null),
		new(GameAI.ai_charge, -3, null),
		new(GameAI.ai_charge, 2, null),
		new(GameAI.ai_charge, -11, null)
	};

	private static readonly EntThinkAdapter brain_chest_closed = new("brain_chest_closed", self =>
	{
		self.monsterinfo.power_armor_type = Defines.POWER_ARMOR_SCREEN;

		if ((self.spawnflags & 65536) != 0)
		{
			self.spawnflags &= ~65536;
			self.monsterinfo.currentmove = M_Brain.brain_move_attack1;
		}

		return true;
	});

	private static readonly mframe_t[] brain_frames_attack2 =
	{
		new(GameAI.ai_charge, 5, null),
		new(GameAI.ai_charge, -4, null),
		new(GameAI.ai_charge, -4, null),
		new(GameAI.ai_charge, -3, null),
		new(GameAI.ai_charge, 0, M_Brain.brain_chest_open),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 13, M_Brain.brain_tentacle_attack),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 2, null),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, -9, M_Brain.brain_chest_closed),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 4, null),
		new(GameAI.ai_charge, 3, null),
		new(GameAI.ai_charge, 2, null),
		new(GameAI.ai_charge, -3, null),
		new(GameAI.ai_charge, -6, null)
	};

	private static readonly EntThinkAdapter brain_melee = new("brain_melee", self =>
	{
		if (Lib.random() <= 0.5)
			self.monsterinfo.currentmove = M_Brain.brain_move_attack1;
		else
			self.monsterinfo.currentmove = M_Brain.brain_move_attack2;

		return true;
	});

	//
	//	   RUN
	//

	private static readonly mframe_t[] brain_frames_run =
	{
		new(GameAI.ai_run, 9, null),
		new(GameAI.ai_run, 2, null),
		new(GameAI.ai_run, 3, null),
		new(GameAI.ai_run, 3, null),
		new(GameAI.ai_run, 1, null),
		new(GameAI.ai_run, 0, null),
		new(GameAI.ai_run, 0, null),
		new(GameAI.ai_run, 10, null),
		new(GameAI.ai_run, -4, null),
		new(GameAI.ai_run, -1, null),
		new(GameAI.ai_run, 2, null)
	};

	private static readonly mmove_t brain_move_run = new(M_Brain.FRAME_walk101, M_Brain.FRAME_walk111, M_Brain.brain_frames_run, null);

	private static readonly EntThinkAdapter brain_run = new("brain_run", self =>
	{
		self.monsterinfo.power_armor_type = Defines.POWER_ARMOR_SCREEN;

		if ((self.monsterinfo.aiflags & Defines.AI_STAND_GROUND) != 0)
			self.monsterinfo.currentmove = M_Brain.brain_move_stand;
		else
			self.monsterinfo.currentmove = M_Brain.brain_move_run;

		return true;
	});

	private static readonly mframe_t[] brain_frames_defense =
	{
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null)
	};

	private static mmove_t brain_move_defense = new(M_Brain.FRAME_defens01, M_Brain.FRAME_defens08, M_Brain.brain_frames_defense, null);

	private static readonly mframe_t[] brain_frames_pain3 =
	{
		new(GameAI.ai_move, -2, null),
		new(GameAI.ai_move, 2, null),
		new(GameAI.ai_move, 1, null),
		new(GameAI.ai_move, 3, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, -4, null)
	};

	private static readonly mmove_t brain_move_pain3 = new(M_Brain.FRAME_pain301, M_Brain.FRAME_pain306, M_Brain.brain_frames_pain3, M_Brain.brain_run);

	private static readonly mframe_t[] brain_frames_pain2 =
	{
		new(GameAI.ai_move, -2, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 3, null),
		new(GameAI.ai_move, 1, null),
		new(GameAI.ai_move, -2, null)
	};

	private static readonly mmove_t brain_move_pain2 = new(M_Brain.FRAME_pain201, M_Brain.FRAME_pain208, M_Brain.brain_frames_pain2, M_Brain.brain_run);

	private static readonly mframe_t[] brain_frames_pain1 =
	{
		new(GameAI.ai_move, -6, null),
		new(GameAI.ai_move, -2, null),
		new(GameAI.ai_move, -6, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 2, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 2, null),
		new(GameAI.ai_move, 1, null),
		new(GameAI.ai_move, 7, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 3, null),
		new(GameAI.ai_move, -1, null)
	};

	private static readonly mmove_t brain_move_pain1 = new(M_Brain.FRAME_pain101, M_Brain.FRAME_pain121, M_Brain.brain_frames_pain1, M_Brain.brain_run);

	private static readonly mframe_t[] brain_frames_duck =
	{
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, -2, M_Brain.brain_duck_down),
		new(GameAI.ai_move, 17, M_Brain.brain_duck_hold),
		new(GameAI.ai_move, -3, null),
		new(GameAI.ai_move, -1, M_Brain.brain_duck_up),
		new(GameAI.ai_move, -5, null),
		new(GameAI.ai_move, -6, null),
		new(GameAI.ai_move, -6, null)
	};

	private static readonly mmove_t brain_move_duck = new(M_Brain.FRAME_duck01, M_Brain.FRAME_duck08, M_Brain.brain_frames_duck, M_Brain.brain_run);

	private static readonly EntPainAdapter brain_pain = new("brain_pain", (self, other, kick, damage) =>
	{
		float r;

		if (self.health < self.max_health / 2)
			self.s.skinnum = 1;

		if (GameBase.level.time < self.pain_debounce_time)
			return;

		self.pain_debounce_time = GameBase.level.time + 3;

		if (GameBase.skill.value == 3)
			return; // no pain anims in nightmare

		r = Lib.random();

		if (r < 0.33)
		{
			GameBase.gi.sound(self, Defines.CHAN_VOICE, M_Brain.sound_pain1, 1, Defines.ATTN_NORM, 0);
			self.monsterinfo.currentmove = M_Brain.brain_move_pain1;
		}
		else if (r < 0.66)
		{
			GameBase.gi.sound(self, Defines.CHAN_VOICE, M_Brain.sound_pain2, 1, Defines.ATTN_NORM, 0);
			self.monsterinfo.currentmove = M_Brain.brain_move_pain2;
		}
		else
		{
			GameBase.gi.sound(self, Defines.CHAN_VOICE, M_Brain.sound_pain1, 1, Defines.ATTN_NORM, 0);
			self.monsterinfo.currentmove = M_Brain.brain_move_pain3;
		}
	});

	private static readonly EntDieAdapter brain_die = new("brain_die", (self, inflictor, attacker, damage, point) =>
	{
		int n;

		self.s.effects = 0;
		self.monsterinfo.power_armor_type = Defines.POWER_ARMOR_NONE;

		//	   check for gib
		if (self.health <= self.gib_health)
		{
			GameBase.gi.sound(self, Defines.CHAN_VOICE, GameBase.gi.soundindex("misc/udeath.wav"), 1, Defines.ATTN_NORM, 0);

			for (n = 0; n < 2; n++)
				GameMisc.ThrowGib(self, "models/objects/gibs/bone/tris.md2", damage, Defines.GIB_ORGANIC);

			for (n = 0; n < 4; n++)
				GameMisc.ThrowGib(self, "models/objects/gibs/sm_meat/tris.md2", damage, Defines.GIB_ORGANIC);

			GameMisc.ThrowHead(self, "models/objects/gibs/head2/tris.md2", damage, Defines.GIB_ORGANIC);
			self.deadflag = Defines.DEAD_DEAD;

			return;
		}

		if (self.deadflag == Defines.DEAD_DEAD)
			return;

		//	   regular death
		GameBase.gi.sound(self, Defines.CHAN_VOICE, M_Brain.sound_death, 1, Defines.ATTN_NORM, 0);
		self.deadflag = Defines.DEAD_DEAD;
		self.takedamage = Defines.DAMAGE_YES;

		if (Lib.random() <= 0.5)
			self.monsterinfo.currentmove = M_Brain.brain_move_death1;
		else
			self.monsterinfo.currentmove = M_Brain.brain_move_death2;
	});

	private static readonly mmove_t brain_move_attack1 =
		new(M_Brain.FRAME_attak101, M_Brain.FRAME_attak118, M_Brain.brain_frames_attack1, M_Brain.brain_run);

	private static readonly mmove_t brain_move_attack2 =
		new(M_Brain.FRAME_attak201, M_Brain.FRAME_attak217, M_Brain.brain_frames_attack2, M_Brain.brain_run);

	/*
	 * QUAKED monster_brain (1 .5 0) (-16 -16 -24) (16 16 32) Ambush
	 * Trigger_Spawn Sight
	 */
	public static void SP_monster_brain(edict_t self)
	{
		if (GameBase.deathmatch.value != 0)
		{
			GameUtil.G_FreeEdict(self);

			return;
		}

		M_Brain.sound_chest_open = GameBase.gi.soundindex("brain/brnatck1.wav");
		M_Brain.sound_tentacles_extend = GameBase.gi.soundindex("brain/brnatck2.wav");
		M_Brain.sound_tentacles_retract = GameBase.gi.soundindex("brain/brnatck3.wav");
		M_Brain.sound_death = GameBase.gi.soundindex("brain/brndeth1.wav");
		M_Brain.sound_idle1 = GameBase.gi.soundindex("brain/brnidle1.wav");
		M_Brain.sound_idle2 = GameBase.gi.soundindex("brain/brnidle2.wav");
		M_Brain.sound_idle3 = GameBase.gi.soundindex("brain/brnlens1.wav");
		M_Brain.sound_pain1 = GameBase.gi.soundindex("brain/brnpain1.wav");
		M_Brain.sound_pain2 = GameBase.gi.soundindex("brain/brnpain2.wav");
		M_Brain.sound_sight = GameBase.gi.soundindex("brain/brnsght1.wav");
		M_Brain.sound_search = GameBase.gi.soundindex("brain/brnsrch1.wav");
		M_Brain.sound_melee1 = GameBase.gi.soundindex("brain/melee1.wav");
		M_Brain.sound_melee2 = GameBase.gi.soundindex("brain/melee2.wav");
		M_Brain.sound_melee3 = GameBase.gi.soundindex("brain/melee3.wav");

		self.movetype = Defines.MOVETYPE_STEP;
		self.solid = Defines.SOLID_BBOX;
		self.s.modelindex = GameBase.gi.modelindex("models/monsters/brain/tris.md2");
		Math3D.VectorSet(self.mins, -16, -16, -24);
		Math3D.VectorSet(self.maxs, 16, 16, 32);

		self.health = 300;
		self.gib_health = -150;
		self.mass = 400;

		self.pain = M_Brain.brain_pain;
		self.die = M_Brain.brain_die;

		self.monsterinfo.stand = M_Brain.brain_stand;
		self.monsterinfo.walk = M_Brain.brain_walk;
		self.monsterinfo.run = M_Brain.brain_run;
		self.monsterinfo.dodge = M_Brain.brain_dodge;

		//		self.monsterinfo.attack = brain_attack;
		self.monsterinfo.melee = M_Brain.brain_melee;
		self.monsterinfo.sight = M_Brain.brain_sight;
		self.monsterinfo.search = M_Brain.brain_search;
		self.monsterinfo.idle = M_Brain.brain_idle;

		self.monsterinfo.power_armor_type = Defines.POWER_ARMOR_SCREEN;
		self.monsterinfo.power_armor_power = 100;

		GameBase.gi.linkentity(self);

		self.monsterinfo.currentmove = M_Brain.brain_move_stand;
		self.monsterinfo.scale = M_Brain.MODEL_SCALE;

		GameAI.walkmonster_start.think(self);
	}
}