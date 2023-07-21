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
namespace Quake2Sharp.game.monsters;

using adapters;
using types;
using util;

public class M_Berserk
{
	public static readonly int FRAME_stand1 = 0;
	public static readonly int FRAME_stand2 = 1;
	public static readonly int FRAME_stand3 = 2;
	public static readonly int FRAME_stand4 = 3;
	public static readonly int FRAME_stand5 = 4;
	public static readonly int FRAME_standb1 = 5;
	public static readonly int FRAME_standb2 = 6;
	public static readonly int FRAME_standb3 = 7;
	public static readonly int FRAME_standb4 = 8;
	public static readonly int FRAME_standb5 = 9;
	public static readonly int FRAME_standb6 = 10;
	public static readonly int FRAME_standb7 = 11;
	public static readonly int FRAME_standb8 = 12;
	public static readonly int FRAME_standb9 = 13;
	public static readonly int FRAME_standb10 = 14;
	public static readonly int FRAME_standb11 = 15;
	public static readonly int FRAME_standb12 = 16;
	public static readonly int FRAME_standb13 = 17;
	public static readonly int FRAME_standb14 = 18;
	public static readonly int FRAME_standb15 = 19;
	public static readonly int FRAME_standb16 = 20;
	public static readonly int FRAME_standb17 = 21;
	public static readonly int FRAME_standb18 = 22;
	public static readonly int FRAME_standb19 = 23;
	public static readonly int FRAME_standb20 = 24;
	public static readonly int FRAME_walkc1 = 25;
	public static readonly int FRAME_walkc2 = 26;
	public static readonly int FRAME_walkc3 = 27;
	public static readonly int FRAME_walkc4 = 28;
	public static readonly int FRAME_walkc5 = 29;
	public static readonly int FRAME_walkc6 = 30;
	public static readonly int FRAME_walkc7 = 31;
	public static readonly int FRAME_walkc8 = 32;
	public static readonly int FRAME_walkc9 = 33;
	public static readonly int FRAME_walkc10 = 34;
	public static readonly int FRAME_walkc11 = 35;
	public static readonly int FRAME_run1 = 36;
	public static readonly int FRAME_run2 = 37;
	public static readonly int FRAME_run3 = 38;
	public static readonly int FRAME_run4 = 39;
	public static readonly int FRAME_run5 = 40;
	public static readonly int FRAME_run6 = 41;
	public static readonly int FRAME_att_a1 = 42;
	public static readonly int FRAME_att_a2 = 43;
	public static readonly int FRAME_att_a3 = 44;
	public static readonly int FRAME_att_a4 = 45;
	public static readonly int FRAME_att_a5 = 46;
	public static readonly int FRAME_att_a6 = 47;
	public static readonly int FRAME_att_a7 = 48;
	public static readonly int FRAME_att_a8 = 49;
	public static readonly int FRAME_att_a9 = 50;
	public static readonly int FRAME_att_a10 = 51;
	public static readonly int FRAME_att_a11 = 52;
	public static readonly int FRAME_att_a12 = 53;
	public static readonly int FRAME_att_a13 = 54;
	public static readonly int FRAME_att_b1 = 55;
	public static readonly int FRAME_att_b2 = 56;
	public static readonly int FRAME_att_b3 = 57;
	public static readonly int FRAME_att_b4 = 58;
	public static readonly int FRAME_att_b5 = 59;
	public static readonly int FRAME_att_b6 = 60;
	public static readonly int FRAME_att_b7 = 61;
	public static readonly int FRAME_att_b8 = 62;
	public static readonly int FRAME_att_b9 = 63;
	public static readonly int FRAME_att_b10 = 64;
	public static readonly int FRAME_att_b11 = 65;
	public static readonly int FRAME_att_b12 = 66;
	public static readonly int FRAME_att_b13 = 67;
	public static readonly int FRAME_att_b14 = 68;
	public static readonly int FRAME_att_b15 = 69;
	public static readonly int FRAME_att_b16 = 70;
	public static readonly int FRAME_att_b17 = 71;
	public static readonly int FRAME_att_b18 = 72;
	public static readonly int FRAME_att_b19 = 73;
	public static readonly int FRAME_att_b20 = 74;
	public static readonly int FRAME_att_b21 = 75;
	public static readonly int FRAME_att_c1 = 76;
	public static readonly int FRAME_att_c2 = 77;
	public static readonly int FRAME_att_c3 = 78;
	public static readonly int FRAME_att_c4 = 79;
	public static readonly int FRAME_att_c5 = 80;
	public static readonly int FRAME_att_c6 = 81;
	public static readonly int FRAME_att_c7 = 82;
	public static readonly int FRAME_att_c8 = 83;
	public static readonly int FRAME_att_c9 = 84;
	public static readonly int FRAME_att_c10 = 85;
	public static readonly int FRAME_att_c11 = 86;
	public static readonly int FRAME_att_c12 = 87;
	public static readonly int FRAME_att_c13 = 88;
	public static readonly int FRAME_att_c14 = 89;
	public static readonly int FRAME_att_c15 = 90;
	public static readonly int FRAME_att_c16 = 91;
	public static readonly int FRAME_att_c17 = 92;
	public static readonly int FRAME_att_c18 = 93;
	public static readonly int FRAME_att_c19 = 94;
	public static readonly int FRAME_att_c20 = 95;
	public static readonly int FRAME_att_c21 = 96;
	public static readonly int FRAME_att_c22 = 97;
	public static readonly int FRAME_att_c23 = 98;
	public static readonly int FRAME_att_c24 = 99;
	public static readonly int FRAME_att_c25 = 100;
	public static readonly int FRAME_att_c26 = 101;
	public static readonly int FRAME_att_c27 = 102;
	public static readonly int FRAME_att_c28 = 103;
	public static readonly int FRAME_att_c29 = 104;
	public static readonly int FRAME_att_c30 = 105;
	public static readonly int FRAME_att_c31 = 106;
	public static readonly int FRAME_att_c32 = 107;
	public static readonly int FRAME_att_c33 = 108;
	public static readonly int FRAME_att_c34 = 109;
	public static readonly int FRAME_r_att1 = 110;
	public static readonly int FRAME_r_att2 = 111;
	public static readonly int FRAME_r_att3 = 112;
	public static readonly int FRAME_r_att4 = 113;
	public static readonly int FRAME_r_att5 = 114;
	public static readonly int FRAME_r_att6 = 115;
	public static readonly int FRAME_r_att7 = 116;
	public static readonly int FRAME_r_att8 = 117;
	public static readonly int FRAME_r_att9 = 118;
	public static readonly int FRAME_r_att10 = 119;
	public static readonly int FRAME_r_att11 = 120;
	public static readonly int FRAME_r_att12 = 121;
	public static readonly int FRAME_r_att13 = 122;
	public static readonly int FRAME_r_att14 = 123;
	public static readonly int FRAME_r_att15 = 124;
	public static readonly int FRAME_r_att16 = 125;
	public static readonly int FRAME_r_att17 = 126;
	public static readonly int FRAME_r_att18 = 127;
	public static readonly int FRAME_r_attb1 = 128;
	public static readonly int FRAME_r_attb2 = 129;
	public static readonly int FRAME_r_attb3 = 130;
	public static readonly int FRAME_r_attb4 = 131;
	public static readonly int FRAME_r_attb5 = 132;
	public static readonly int FRAME_r_attb6 = 133;
	public static readonly int FRAME_r_attb7 = 134;
	public static readonly int FRAME_r_attb8 = 135;
	public static readonly int FRAME_r_attb9 = 136;
	public static readonly int FRAME_r_attb10 = 137;
	public static readonly int FRAME_r_attb11 = 138;
	public static readonly int FRAME_r_attb12 = 139;
	public static readonly int FRAME_r_attb13 = 140;
	public static readonly int FRAME_r_attb14 = 141;
	public static readonly int FRAME_r_attb15 = 142;
	public static readonly int FRAME_r_attb16 = 143;
	public static readonly int FRAME_r_attb17 = 144;
	public static readonly int FRAME_r_attb18 = 145;
	public static readonly int FRAME_slam1 = 146;
	public static readonly int FRAME_slam2 = 147;
	public static readonly int FRAME_slam3 = 148;
	public static readonly int FRAME_slam4 = 149;
	public static readonly int FRAME_slam5 = 150;
	public static readonly int FRAME_slam6 = 151;
	public static readonly int FRAME_slam7 = 152;
	public static readonly int FRAME_slam8 = 153;
	public static readonly int FRAME_slam9 = 154;
	public static readonly int FRAME_slam10 = 155;
	public static readonly int FRAME_slam11 = 156;
	public static readonly int FRAME_slam12 = 157;
	public static readonly int FRAME_slam13 = 158;
	public static readonly int FRAME_slam14 = 159;
	public static readonly int FRAME_slam15 = 160;
	public static readonly int FRAME_slam16 = 161;
	public static readonly int FRAME_slam17 = 162;
	public static readonly int FRAME_slam18 = 163;
	public static readonly int FRAME_slam19 = 164;
	public static readonly int FRAME_slam20 = 165;
	public static readonly int FRAME_slam21 = 166;
	public static readonly int FRAME_slam22 = 167;
	public static readonly int FRAME_slam23 = 168;
	public static readonly int FRAME_duck1 = 169;
	public static readonly int FRAME_duck2 = 170;
	public static readonly int FRAME_duck3 = 171;
	public static readonly int FRAME_duck4 = 172;
	public static readonly int FRAME_duck5 = 173;
	public static readonly int FRAME_duck6 = 174;
	public static readonly int FRAME_duck7 = 175;
	public static readonly int FRAME_duck8 = 176;
	public static readonly int FRAME_duck9 = 177;
	public static readonly int FRAME_duck10 = 178;
	public static readonly int FRAME_fall1 = 179;
	public static readonly int FRAME_fall2 = 180;
	public static readonly int FRAME_fall3 = 181;
	public static readonly int FRAME_fall4 = 182;
	public static readonly int FRAME_fall5 = 183;
	public static readonly int FRAME_fall6 = 184;
	public static readonly int FRAME_fall7 = 185;
	public static readonly int FRAME_fall8 = 186;
	public static readonly int FRAME_fall9 = 187;
	public static readonly int FRAME_fall10 = 188;
	public static readonly int FRAME_fall11 = 189;
	public static readonly int FRAME_fall12 = 190;
	public static readonly int FRAME_fall13 = 191;
	public static readonly int FRAME_fall14 = 192;
	public static readonly int FRAME_fall15 = 193;
	public static readonly int FRAME_fall16 = 194;
	public static readonly int FRAME_fall17 = 195;
	public static readonly int FRAME_fall18 = 196;
	public static readonly int FRAME_fall19 = 197;
	public static readonly int FRAME_fall20 = 198;
	public static readonly int FRAME_painc1 = 199;
	public static readonly int FRAME_painc2 = 200;
	public static readonly int FRAME_painc3 = 201;
	public static readonly int FRAME_painc4 = 202;
	public static readonly int FRAME_painb1 = 203;
	public static readonly int FRAME_painb2 = 204;
	public static readonly int FRAME_painb3 = 205;
	public static readonly int FRAME_painb4 = 206;
	public static readonly int FRAME_painb5 = 207;
	public static readonly int FRAME_painb6 = 208;
	public static readonly int FRAME_painb7 = 209;
	public static readonly int FRAME_painb8 = 210;
	public static readonly int FRAME_painb9 = 211;
	public static readonly int FRAME_painb10 = 212;
	public static readonly int FRAME_painb11 = 213;
	public static readonly int FRAME_painb12 = 214;
	public static readonly int FRAME_painb13 = 215;
	public static readonly int FRAME_painb14 = 216;
	public static readonly int FRAME_painb15 = 217;
	public static readonly int FRAME_painb16 = 218;
	public static readonly int FRAME_painb17 = 219;
	public static readonly int FRAME_painb18 = 220;
	public static readonly int FRAME_painb19 = 221;
	public static readonly int FRAME_painb20 = 222;
	public static readonly int FRAME_death1 = 223;
	public static readonly int FRAME_death2 = 224;
	public static readonly int FRAME_death3 = 225;
	public static readonly int FRAME_death4 = 226;
	public static readonly int FRAME_death5 = 227;
	public static readonly int FRAME_death6 = 228;
	public static readonly int FRAME_death7 = 229;
	public static readonly int FRAME_death8 = 230;
	public static readonly int FRAME_death9 = 231;
	public static readonly int FRAME_death10 = 232;
	public static readonly int FRAME_death11 = 233;
	public static readonly int FRAME_death12 = 234;
	public static readonly int FRAME_death13 = 235;
	public static readonly int FRAME_deathc1 = 236;
	public static readonly int FRAME_deathc2 = 237;
	public static readonly int FRAME_deathc3 = 238;
	public static readonly int FRAME_deathc4 = 239;
	public static readonly int FRAME_deathc5 = 240;
	public static readonly int FRAME_deathc6 = 241;
	public static readonly int FRAME_deathc7 = 242;
	public static readonly int FRAME_deathc8 = 243;
	public static readonly float MODEL_SCALE = 1.000000f;
	private static int sound_pain;
	private static int sound_die;
	private static int sound_idle;
	private static int sound_punch;
	private static int sound_sight;
	private static int sound_search;

	private static readonly EntInteractAdapter berserk_sight = new("berserk_sight", (self, other) =>
	{
		GameBase.gi.sound(self, Defines.CHAN_VOICE, M_Berserk.sound_sight, 1, Defines.ATTN_NORM, 0);

		return true;
	});

	private static readonly EntThinkAdapter berserk_search = new("berserk_search", self =>
	{
		GameBase.gi.sound(self, Defines.CHAN_VOICE, M_Berserk.sound_search, 1, Defines.ATTN_NORM, 0);

		return true;
	});

	private static readonly EntThinkAdapter berserk_fidget = new("berserk_fidget", self =>
	{
		if ((self.monsterinfo.aiflags & Defines.AI_STAND_GROUND) != 0)
			return true;

		if (Lib.random() > 0.15f)
			return true;

		self.monsterinfo.currentmove = M_Berserk.berserk_move_stand_fidget;
		GameBase.gi.sound(self, Defines.CHAN_WEAPON, M_Berserk.sound_idle, 1, Defines.ATTN_IDLE, 0);

		return true;
	});

	private static readonly mframe_t[] berserk_frames_stand =
	{
		new(GameAI.ai_stand, 0, M_Berserk.berserk_fidget),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null)
	};

	private static readonly mmove_t berserk_move_stand = new(M_Berserk.FRAME_stand1, M_Berserk.FRAME_stand5, M_Berserk.berserk_frames_stand, null);

	private static readonly EntThinkAdapter berserk_stand = new("berserk_stand", self =>
	{
		self.monsterinfo.currentmove = M_Berserk.berserk_move_stand;

		return true;
	});

	private static readonly mframe_t[] berserk_frames_stand_fidget =
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
		new(GameAI.ai_stand, 0, null)
	};

	private static readonly mmove_t berserk_move_stand_fidget =
		new(M_Berserk.FRAME_standb1, M_Berserk.FRAME_standb20, M_Berserk.berserk_frames_stand_fidget, M_Berserk.berserk_stand);

	private static readonly mframe_t[] berserk_frames_walk =
	{
		new(GameAI.ai_walk, 9.1f, null),
		new(GameAI.ai_walk, 6.3f, null),
		new(GameAI.ai_walk, 4.9f, null),
		new(GameAI.ai_walk, 6.7f, null),
		new(GameAI.ai_walk, 6.0f, null),
		new(GameAI.ai_walk, 8.2f, null),
		new(GameAI.ai_walk, 7.2f, null),
		new(GameAI.ai_walk, 6.1f, null),
		new(GameAI.ai_walk, 4.9f, null),
		new(GameAI.ai_walk, 4.7f, null),
		new(GameAI.ai_walk, 4.7f, null),
		new(GameAI.ai_walk, 4.8f, null)
	};

	private static readonly mmove_t berserk_move_walk = new(M_Berserk.FRAME_walkc1, M_Berserk.FRAME_walkc11, M_Berserk.berserk_frames_walk, null);

	private static readonly EntThinkAdapter berserk_walk = new("berserk_walk", self =>
	{
		self.monsterinfo.currentmove = M_Berserk.berserk_move_walk;

		return true;
	});

	/*
	 * 
	 * **************************** SKIPPED THIS FOR NOW!
	 * ****************************
	 * 
	 * Running . Arm raised in air
	 * 
	 * void() berserk_runb1 =[ $r_att1 , berserk_runb2 ] {ai_run(21);}; void()
	 * berserk_runb2 =[ $r_att2 , berserk_runb3 ] {ai_run(11);}; void()
	 * berserk_runb3 =[ $r_att3 , berserk_runb4 ] {ai_run(21);}; void()
	 * berserk_runb4 =[ $r_att4 , berserk_runb5 ] {ai_run(25);}; void()
	 * berserk_runb5 =[ $r_att5 , berserk_runb6 ] {ai_run(18);}; void()
	 * berserk_runb6 =[ $r_att6 , berserk_runb7 ] {ai_run(19);}; // running with
	 * arm in air : start loop void() berserk_runb7 =[ $r_att7 , berserk_runb8 ]
	 * {ai_run(21);}; void() berserk_runb8 =[ $r_att8 , berserk_runb9 ]
	 * {ai_run(11);}; void() berserk_runb9 =[ $r_att9 , berserk_runb10 ]
	 * {ai_run(21);}; void() berserk_runb10 =[ $r_att10 , berserk_runb11 ]
	 * {ai_run(25);}; void() berserk_runb11 =[ $r_att11 , berserk_runb12 ]
	 * {ai_run(18);}; void() berserk_runb12 =[ $r_att12 , berserk_runb7 ]
	 * {ai_run(19);}; // running with arm in air : end loop
	 */

	private static readonly mframe_t[] berserk_frames_run1 =
	{
		new(GameAI.ai_run, 21, null),
		new(GameAI.ai_run, 11, null),
		new(GameAI.ai_run, 21, null),
		new(GameAI.ai_run, 25, null),
		new(GameAI.ai_run, 18, null),
		new(GameAI.ai_run, 19, null)
	};

	private static readonly mmove_t berserk_move_run1 = new(M_Berserk.FRAME_run1, M_Berserk.FRAME_run6, M_Berserk.berserk_frames_run1, null);

	private static readonly EntThinkAdapter berserk_run = new("berserk_run", self =>
	{
		if ((self.monsterinfo.aiflags & Defines.AI_STAND_GROUND) != 0)
			self.monsterinfo.currentmove = M_Berserk.berserk_move_stand;
		else
			self.monsterinfo.currentmove = M_Berserk.berserk_move_run1;

		return true;
	});

	private static readonly EntThinkAdapter berserk_attack_spike = new("berserk_attack_spike", self =>
	{
		float[] aim = { Defines.MELEE_DISTANCE, 0f, -24f };

		GameWeapon.fire_hit(self, aim, 15 + Lib.rand() % 6, 400);

		//	Faster attack -- upwards and backwards

		return true;
	});

	private static readonly EntThinkAdapter berserk_swing = new("berserk_swing", self =>
	{
		GameBase.gi.sound(self, Defines.CHAN_WEAPON, M_Berserk.sound_punch, 1, Defines.ATTN_NORM, 0);

		return true;
	});

	private static readonly mframe_t[] berserk_frames_attack_spike =
	{
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, M_Berserk.berserk_swing),
		new(GameAI.ai_charge, 0, M_Berserk.berserk_attack_spike),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, null)
	};

	private static readonly mmove_t berserk_move_attack_spike =
		new(M_Berserk.FRAME_att_c1, M_Berserk.FRAME_att_c8, M_Berserk.berserk_frames_attack_spike, M_Berserk.berserk_run);

	private static readonly EntThinkAdapter berserk_attack_club = new("berserk_attack_club", self =>
	{
		float[] aim = { 0, 0, 0 };

		Math3D.VectorSet(aim, Defines.MELEE_DISTANCE, self.mins[0], -4);
		GameWeapon.fire_hit(self, aim, 5 + Lib.rand() % 6, 400); // Slower

		// attack

		return true;
	});

	private static readonly mframe_t[] berserk_frames_attack_club =
	{
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, M_Berserk.berserk_swing),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, M_Berserk.berserk_attack_club),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, null)
	};

	private static readonly mmove_t berserk_move_attack_club =
		new(M_Berserk.FRAME_att_c9, M_Berserk.FRAME_att_c20, M_Berserk.berserk_frames_attack_club, M_Berserk.berserk_run);

	private static readonly EntThinkAdapter berserk_strike = new("berserk_strike", self => { return true; });

	private static readonly mframe_t[] berserk_frames_attack_strike =
	{
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, M_Berserk.berserk_swing),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, M_Berserk.berserk_strike),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 9.7f, null),
		new(GameAI.ai_move, 13.6f, null)
	};

	private static mmove_t berserk_move_attack_strike =
		new(M_Berserk.FRAME_att_c21, M_Berserk.FRAME_att_c34, M_Berserk.berserk_frames_attack_strike, M_Berserk.berserk_run);

	private static readonly EntThinkAdapter berserk_melee = new("berserk_melee", self =>
	{
		if (Lib.rand() % 2 == 0)
			self.monsterinfo.currentmove = M_Berserk.berserk_move_attack_spike;
		else
			self.monsterinfo.currentmove = M_Berserk.berserk_move_attack_club;

		return true;
	});

	/*
	 * void() berserk_atke1 =[ $r_attb1, berserk_atke2 ] {ai_run(9);}; void()
	 * berserk_atke2 =[ $r_attb2, berserk_atke3 ] {ai_run(6);}; void()
	 * berserk_atke3 =[ $r_attb3, berserk_atke4 ] {ai_run(18.4);}; void()
	 * berserk_atke4 =[ $r_attb4, berserk_atke5 ] {ai_run(25);}; void()
	 * berserk_atke5 =[ $r_attb5, berserk_atke6 ] {ai_run(14);}; void()
	 * berserk_atke6 =[ $r_attb6, berserk_atke7 ] {ai_run(20);}; void()
	 * berserk_atke7 =[ $r_attb7, berserk_atke8 ] {ai_run(8.5);}; void()
	 * berserk_atke8 =[ $r_attb8, berserk_atke9 ] {ai_run(3);}; void()
	 * berserk_atke9 =[ $r_attb9, berserk_atke10 ] {ai_run(17.5);}; void()
	 * berserk_atke10 =[ $r_attb10, berserk_atke11 ] {ai_run(17);}; void()
	 * berserk_atke11 =[ $r_attb11, berserk_atke12 ] {ai_run(9);}; void()
	 * berserk_atke12 =[ $r_attb12, berserk_atke13 ] {ai_run(25);}; void()
	 * berserk_atke13 =[ $r_attb13, berserk_atke14 ] {ai_run(3.7);}; void()
	 * berserk_atke14 =[ $r_attb14, berserk_atke15 ] {ai_run(2.6);}; void()
	 * berserk_atke15 =[ $r_attb15, berserk_atke16 ] {ai_run(19);}; void()
	 * berserk_atke16 =[ $r_attb16, berserk_atke17 ] {ai_run(25);}; void()
	 * berserk_atke17 =[ $r_attb17, berserk_atke18 ] {ai_run(19.6);}; void()
	 * berserk_atke18 =[ $r_attb18, berserk_run1 ] {ai_run(7.8);};
	 */

	private static readonly mframe_t[] berserk_frames_pain1 =
	{
		new(GameAI.ai_move, 0, null), new(GameAI.ai_move, 0, null), new(GameAI.ai_move, 0, null), new(GameAI.ai_move, 0, null)
	};

	private static readonly mmove_t berserk_move_pain1 =
		new(M_Berserk.FRAME_painc1, M_Berserk.FRAME_painc4, M_Berserk.berserk_frames_pain1, M_Berserk.berserk_run);

	private static readonly mframe_t[] berserk_frames_pain2 =
	{
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
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null)
	};

	private static readonly mmove_t berserk_move_pain2 =
		new(M_Berserk.FRAME_painb1, M_Berserk.FRAME_painb20, M_Berserk.berserk_frames_pain2, M_Berserk.berserk_run);

	private static readonly EntPainAdapter berserk_pain = new("berserk_pain", (self, other, kick, damage) =>
	{
		if (self.health < self.max_health / 2)
			self.s.skinnum = 1;

		if (GameBase.level.time < self.pain_debounce_time)
			return;

		self.pain_debounce_time = GameBase.level.time + 3;
		GameBase.gi.sound(self, Defines.CHAN_VOICE, M_Berserk.sound_pain, 1, Defines.ATTN_NORM, 0);

		if (GameBase.skill.value == 3)
			return; // no pain anims in nightmare

		if (damage < 20 || Lib.random() < 0.5)
			self.monsterinfo.currentmove = M_Berserk.berserk_move_pain1;
		else
			self.monsterinfo.currentmove = M_Berserk.berserk_move_pain2;
	});

	private static readonly EntThinkAdapter berserk_dead = new("berserk_dead", self =>
	{
		Math3D.VectorSet(self.mins, -16, -16, -24);
		Math3D.VectorSet(self.maxs, 16, 16, -8);
		self.movetype = Defines.MOVETYPE_TOSS;
		self.svflags |= Defines.SVF_DEADMONSTER;
		self.nextthink = 0;
		GameBase.gi.linkentity(self);

		return true;
	});

	private static readonly mframe_t[] berserk_frames_death1 =
	{
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

	private static readonly mmove_t berserk_move_death1 =
		new(M_Berserk.FRAME_death1, M_Berserk.FRAME_death13, M_Berserk.berserk_frames_death1, M_Berserk.berserk_dead);

	private static readonly mframe_t[] berserk_frames_death2 =
	{
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null)
	};

	private static readonly mmove_t berserk_move_death2 =
		new(M_Berserk.FRAME_deathc1, M_Berserk.FRAME_deathc8, M_Berserk.berserk_frames_death2, M_Berserk.berserk_dead);

	private static readonly EntDieAdapter berserk_die = new("berserk_die", (self, inflictor, attacker, damage, point) =>
	{
		int n;

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

		GameBase.gi.sound(self, Defines.CHAN_VOICE, M_Berserk.sound_die, 1, Defines.ATTN_NORM, 0);
		self.deadflag = Defines.DEAD_DEAD;
		self.takedamage = Defines.DAMAGE_YES;

		if (damage >= 50)
			self.monsterinfo.currentmove = M_Berserk.berserk_move_death1;
		else
			self.monsterinfo.currentmove = M_Berserk.berserk_move_death2;
	});

	/*
	 * QUAKED monster_berserk (1 .5 0) (-16 -16 -24) (16 16 32) Ambush
	 * Trigger_Spawn Sight
	 */
	public static void SP_monster_berserk(edict_t self)
	{
		if (GameBase.deathmatch.value != 0)
		{
			GameUtil.G_FreeEdict(self);

			return;
		}

		// pre-caches
		M_Berserk.sound_pain = GameBase.gi.soundindex("berserk/berpain2.wav");
		M_Berserk.sound_die = GameBase.gi.soundindex("berserk/berdeth2.wav");
		M_Berserk.sound_idle = GameBase.gi.soundindex("berserk/beridle1.wav");
		M_Berserk.sound_punch = GameBase.gi.soundindex("berserk/attack.wav");
		M_Berserk.sound_search = GameBase.gi.soundindex("berserk/bersrch1.wav");
		M_Berserk.sound_sight = GameBase.gi.soundindex("berserk/sight.wav");

		self.s.modelindex = GameBase.gi.modelindex("models/monsters/berserk/tris.md2");
		Math3D.VectorSet(self.mins, -16, -16, -24);
		Math3D.VectorSet(self.maxs, 16, 16, 32);
		self.movetype = Defines.MOVETYPE_STEP;
		self.solid = Defines.SOLID_BBOX;

		self.health = 240;
		self.gib_health = -60;
		self.mass = 250;

		self.pain = M_Berserk.berserk_pain;
		self.die = M_Berserk.berserk_die;

		self.monsterinfo.stand = M_Berserk.berserk_stand;
		self.monsterinfo.walk = M_Berserk.berserk_walk;
		self.monsterinfo.run = M_Berserk.berserk_run;
		self.monsterinfo.dodge = null;
		self.monsterinfo.attack = null;
		self.monsterinfo.melee = M_Berserk.berserk_melee;
		self.monsterinfo.sight = M_Berserk.berserk_sight;
		self.monsterinfo.search = M_Berserk.berserk_search;

		self.monsterinfo.currentmove = M_Berserk.berserk_move_stand;
		self.monsterinfo.scale = M_Berserk.MODEL_SCALE;

		GameBase.gi.linkentity(self);

		GameAI.walkmonster_start.think(self);
	}
}