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

public class M_Boss32
{
	public static readonly int FRAME_attak101 = 0;
	public static readonly int FRAME_attak102 = 1;
	public static readonly int FRAME_attak103 = 2;
	public static readonly int FRAME_attak104 = 3;
	public static readonly int FRAME_attak105 = 4;
	public static readonly int FRAME_attak106 = 5;
	public static readonly int FRAME_attak107 = 6;
	public static readonly int FRAME_attak108 = 7;
	public static readonly int FRAME_attak109 = 8;
	public static readonly int FRAME_attak110 = 9;
	public static readonly int FRAME_attak111 = 10;
	public static readonly int FRAME_attak112 = 11;
	public static readonly int FRAME_attak113 = 12;
	public static readonly int FRAME_attak114 = 13;
	public static readonly int FRAME_attak115 = 14;
	public static readonly int FRAME_attak116 = 15;
	public static readonly int FRAME_attak117 = 16;
	public static readonly int FRAME_attak118 = 17;
	public static readonly int FRAME_attak201 = 18;
	public static readonly int FRAME_attak202 = 19;
	public static readonly int FRAME_attak203 = 20;
	public static readonly int FRAME_attak204 = 21;
	public static readonly int FRAME_attak205 = 22;
	public static readonly int FRAME_attak206 = 23;
	public static readonly int FRAME_attak207 = 24;
	public static readonly int FRAME_attak208 = 25;
	public static readonly int FRAME_attak209 = 26;
	public static readonly int FRAME_attak210 = 27;
	public static readonly int FRAME_attak211 = 28;
	public static readonly int FRAME_attak212 = 29;
	public static readonly int FRAME_attak213 = 30;
	public static readonly int FRAME_death01 = 31;
	public static readonly int FRAME_death02 = 32;
	public static readonly int FRAME_death03 = 33;
	public static readonly int FRAME_death04 = 34;
	public static readonly int FRAME_death05 = 35;
	public static readonly int FRAME_death06 = 36;
	public static readonly int FRAME_death07 = 37;
	public static readonly int FRAME_death08 = 38;
	public static readonly int FRAME_death09 = 39;
	public static readonly int FRAME_death10 = 40;
	public static readonly int FRAME_death11 = 41;
	public static readonly int FRAME_death12 = 42;
	public static readonly int FRAME_death13 = 43;
	public static readonly int FRAME_death14 = 44;
	public static readonly int FRAME_death15 = 45;
	public static readonly int FRAME_death16 = 46;
	public static readonly int FRAME_death17 = 47;
	public static readonly int FRAME_death18 = 48;
	public static readonly int FRAME_death19 = 49;
	public static readonly int FRAME_death20 = 50;
	public static readonly int FRAME_death21 = 51;
	public static readonly int FRAME_death22 = 52;
	public static readonly int FRAME_death23 = 53;
	public static readonly int FRAME_death24 = 54;
	public static readonly int FRAME_death25 = 55;
	public static readonly int FRAME_death26 = 56;
	public static readonly int FRAME_death27 = 57;
	public static readonly int FRAME_death28 = 58;
	public static readonly int FRAME_death29 = 59;
	public static readonly int FRAME_death30 = 60;
	public static readonly int FRAME_death31 = 61;
	public static readonly int FRAME_death32 = 62;
	public static readonly int FRAME_death33 = 63;
	public static readonly int FRAME_death34 = 64;
	public static readonly int FRAME_death35 = 65;
	public static readonly int FRAME_death36 = 66;
	public static readonly int FRAME_death37 = 67;
	public static readonly int FRAME_death38 = 68;
	public static readonly int FRAME_death39 = 69;
	public static readonly int FRAME_death40 = 70;
	public static readonly int FRAME_death41 = 71;
	public static readonly int FRAME_death42 = 72;
	public static readonly int FRAME_death43 = 73;
	public static readonly int FRAME_death44 = 74;
	public static readonly int FRAME_death45 = 75;
	public static readonly int FRAME_death46 = 76;
	public static readonly int FRAME_death47 = 77;
	public static readonly int FRAME_death48 = 78;
	public static readonly int FRAME_death49 = 79;
	public static readonly int FRAME_death50 = 80;
	public static readonly int FRAME_pain101 = 81;
	public static readonly int FRAME_pain102 = 82;
	public static readonly int FRAME_pain103 = 83;
	public static readonly int FRAME_pain201 = 84;
	public static readonly int FRAME_pain202 = 85;
	public static readonly int FRAME_pain203 = 86;
	public static readonly int FRAME_pain301 = 87;
	public static readonly int FRAME_pain302 = 88;
	public static readonly int FRAME_pain303 = 89;
	public static readonly int FRAME_pain304 = 90;
	public static readonly int FRAME_pain305 = 91;
	public static readonly int FRAME_pain306 = 92;
	public static readonly int FRAME_pain307 = 93;
	public static readonly int FRAME_pain308 = 94;
	public static readonly int FRAME_pain309 = 95;
	public static readonly int FRAME_pain310 = 96;
	public static readonly int FRAME_pain311 = 97;
	public static readonly int FRAME_pain312 = 98;
	public static readonly int FRAME_pain313 = 99;
	public static readonly int FRAME_pain314 = 100;
	public static readonly int FRAME_pain315 = 101;
	public static readonly int FRAME_pain316 = 102;
	public static readonly int FRAME_pain317 = 103;
	public static readonly int FRAME_pain318 = 104;
	public static readonly int FRAME_pain319 = 105;
	public static readonly int FRAME_pain320 = 106;
	public static readonly int FRAME_pain321 = 107;
	public static readonly int FRAME_pain322 = 108;
	public static readonly int FRAME_pain323 = 109;
	public static readonly int FRAME_pain324 = 110;
	public static readonly int FRAME_pain325 = 111;
	public static readonly int FRAME_stand01 = 112;
	public static readonly int FRAME_stand02 = 113;
	public static readonly int FRAME_stand03 = 114;
	public static readonly int FRAME_stand04 = 115;
	public static readonly int FRAME_stand05 = 116;
	public static readonly int FRAME_stand06 = 117;
	public static readonly int FRAME_stand07 = 118;
	public static readonly int FRAME_stand08 = 119;
	public static readonly int FRAME_stand09 = 120;
	public static readonly int FRAME_stand10 = 121;
	public static readonly int FRAME_stand11 = 122;
	public static readonly int FRAME_stand12 = 123;
	public static readonly int FRAME_stand13 = 124;
	public static readonly int FRAME_stand14 = 125;
	public static readonly int FRAME_stand15 = 126;
	public static readonly int FRAME_stand16 = 127;
	public static readonly int FRAME_stand17 = 128;
	public static readonly int FRAME_stand18 = 129;
	public static readonly int FRAME_stand19 = 130;
	public static readonly int FRAME_stand20 = 131;
	public static readonly int FRAME_stand21 = 132;
	public static readonly int FRAME_stand22 = 133;
	public static readonly int FRAME_stand23 = 134;
	public static readonly int FRAME_stand24 = 135;
	public static readonly int FRAME_stand25 = 136;
	public static readonly int FRAME_stand26 = 137;
	public static readonly int FRAME_stand27 = 138;
	public static readonly int FRAME_stand28 = 139;
	public static readonly int FRAME_stand29 = 140;
	public static readonly int FRAME_stand30 = 141;
	public static readonly int FRAME_stand31 = 142;
	public static readonly int FRAME_stand32 = 143;
	public static readonly int FRAME_stand33 = 144;
	public static readonly int FRAME_stand34 = 145;
	public static readonly int FRAME_stand35 = 146;
	public static readonly int FRAME_stand36 = 147;
	public static readonly int FRAME_stand37 = 148;
	public static readonly int FRAME_stand38 = 149;
	public static readonly int FRAME_stand39 = 150;
	public static readonly int FRAME_stand40 = 151;
	public static readonly int FRAME_stand41 = 152;
	public static readonly int FRAME_stand42 = 153;
	public static readonly int FRAME_stand43 = 154;
	public static readonly int FRAME_stand44 = 155;
	public static readonly int FRAME_stand45 = 156;
	public static readonly int FRAME_stand46 = 157;
	public static readonly int FRAME_stand47 = 158;
	public static readonly int FRAME_stand48 = 159;
	public static readonly int FRAME_stand49 = 160;
	public static readonly int FRAME_stand50 = 161;
	public static readonly int FRAME_stand51 = 162;
	public static readonly int FRAME_walk01 = 163;
	public static readonly int FRAME_walk02 = 164;
	public static readonly int FRAME_walk03 = 165;
	public static readonly int FRAME_walk04 = 166;
	public static readonly int FRAME_walk05 = 167;
	public static readonly int FRAME_walk06 = 168;
	public static readonly int FRAME_walk07 = 169;
	public static readonly int FRAME_walk08 = 170;
	public static readonly int FRAME_walk09 = 171;
	public static readonly int FRAME_walk10 = 172;
	public static readonly int FRAME_walk11 = 173;
	public static readonly int FRAME_walk12 = 174;
	public static readonly int FRAME_walk13 = 175;
	public static readonly int FRAME_walk14 = 176;
	public static readonly int FRAME_walk15 = 177;
	public static readonly int FRAME_walk16 = 178;
	public static readonly int FRAME_walk17 = 179;
	public static readonly int FRAME_walk18 = 180;
	public static readonly int FRAME_walk19 = 181;
	public static readonly int FRAME_walk20 = 182;
	public static readonly int FRAME_walk21 = 183;
	public static readonly int FRAME_walk22 = 184;
	public static readonly int FRAME_walk23 = 185;
	public static readonly int FRAME_walk24 = 186;
	public static readonly int FRAME_walk25 = 187;
	public static readonly int FRAME_active01 = 188;
	public static readonly int FRAME_active02 = 189;
	public static readonly int FRAME_active03 = 190;
	public static readonly int FRAME_active04 = 191;
	public static readonly int FRAME_active05 = 192;
	public static readonly int FRAME_active06 = 193;
	public static readonly int FRAME_active07 = 194;
	public static readonly int FRAME_active08 = 195;
	public static readonly int FRAME_active09 = 196;
	public static readonly int FRAME_active10 = 197;
	public static readonly int FRAME_active11 = 198;
	public static readonly int FRAME_active12 = 199;
	public static readonly int FRAME_active13 = 200;
	public static readonly int FRAME_attak301 = 201;
	public static readonly int FRAME_attak302 = 202;
	public static readonly int FRAME_attak303 = 203;
	public static readonly int FRAME_attak304 = 204;
	public static readonly int FRAME_attak305 = 205;
	public static readonly int FRAME_attak306 = 206;
	public static readonly int FRAME_attak307 = 207;
	public static readonly int FRAME_attak308 = 208;
	public static readonly int FRAME_attak401 = 209;
	public static readonly int FRAME_attak402 = 210;
	public static readonly int FRAME_attak403 = 211;
	public static readonly int FRAME_attak404 = 212;
	public static readonly int FRAME_attak405 = 213;
	public static readonly int FRAME_attak406 = 214;
	public static readonly int FRAME_attak407 = 215;
	public static readonly int FRAME_attak408 = 216;
	public static readonly int FRAME_attak409 = 217;
	public static readonly int FRAME_attak410 = 218;
	public static readonly int FRAME_attak411 = 219;
	public static readonly int FRAME_attak412 = 220;
	public static readonly int FRAME_attak413 = 221;
	public static readonly int FRAME_attak414 = 222;
	public static readonly int FRAME_attak415 = 223;
	public static readonly int FRAME_attak416 = 224;
	public static readonly int FRAME_attak417 = 225;
	public static readonly int FRAME_attak418 = 226;
	public static readonly int FRAME_attak419 = 227;
	public static readonly int FRAME_attak420 = 228;
	public static readonly int FRAME_attak421 = 229;
	public static readonly int FRAME_attak422 = 230;
	public static readonly int FRAME_attak423 = 231;
	public static readonly int FRAME_attak424 = 232;
	public static readonly int FRAME_attak425 = 233;
	public static readonly int FRAME_attak426 = 234;
	public static readonly int FRAME_attak501 = 235;
	public static readonly int FRAME_attak502 = 236;
	public static readonly int FRAME_attak503 = 237;
	public static readonly int FRAME_attak504 = 238;
	public static readonly int FRAME_attak505 = 239;
	public static readonly int FRAME_attak506 = 240;
	public static readonly int FRAME_attak507 = 241;
	public static readonly int FRAME_attak508 = 242;
	public static readonly int FRAME_attak509 = 243;
	public static readonly int FRAME_attak510 = 244;
	public static readonly int FRAME_attak511 = 245;
	public static readonly int FRAME_attak512 = 246;
	public static readonly int FRAME_attak513 = 247;
	public static readonly int FRAME_attak514 = 248;
	public static readonly int FRAME_attak515 = 249;
	public static readonly int FRAME_attak516 = 250;
	public static readonly int FRAME_death201 = 251;
	public static readonly int FRAME_death202 = 252;
	public static readonly int FRAME_death203 = 253;
	public static readonly int FRAME_death204 = 254;
	public static readonly int FRAME_death205 = 255;
	public static readonly int FRAME_death206 = 256;
	public static readonly int FRAME_death207 = 257;
	public static readonly int FRAME_death208 = 258;
	public static readonly int FRAME_death209 = 259;
	public static readonly int FRAME_death210 = 260;
	public static readonly int FRAME_death211 = 261;
	public static readonly int FRAME_death212 = 262;
	public static readonly int FRAME_death213 = 263;
	public static readonly int FRAME_death214 = 264;
	public static readonly int FRAME_death215 = 265;
	public static readonly int FRAME_death216 = 266;
	public static readonly int FRAME_death217 = 267;
	public static readonly int FRAME_death218 = 268;
	public static readonly int FRAME_death219 = 269;
	public static readonly int FRAME_death220 = 270;
	public static readonly int FRAME_death221 = 271;
	public static readonly int FRAME_death222 = 272;
	public static readonly int FRAME_death223 = 273;
	public static readonly int FRAME_death224 = 274;
	public static readonly int FRAME_death225 = 275;
	public static readonly int FRAME_death226 = 276;
	public static readonly int FRAME_death227 = 277;
	public static readonly int FRAME_death228 = 278;
	public static readonly int FRAME_death229 = 279;
	public static readonly int FRAME_death230 = 280;
	public static readonly int FRAME_death231 = 281;
	public static readonly int FRAME_death232 = 282;
	public static readonly int FRAME_death233 = 283;
	public static readonly int FRAME_death234 = 284;
	public static readonly int FRAME_death235 = 285;
	public static readonly int FRAME_death236 = 286;
	public static readonly int FRAME_death237 = 287;
	public static readonly int FRAME_death238 = 288;
	public static readonly int FRAME_death239 = 289;
	public static readonly int FRAME_death240 = 290;
	public static readonly int FRAME_death241 = 291;
	public static readonly int FRAME_death242 = 292;
	public static readonly int FRAME_death243 = 293;
	public static readonly int FRAME_death244 = 294;
	public static readonly int FRAME_death245 = 295;
	public static readonly int FRAME_death246 = 296;
	public static readonly int FRAME_death247 = 297;
	public static readonly int FRAME_death248 = 298;
	public static readonly int FRAME_death249 = 299;
	public static readonly int FRAME_death250 = 300;
	public static readonly int FRAME_death251 = 301;
	public static readonly int FRAME_death252 = 302;
	public static readonly int FRAME_death253 = 303;
	public static readonly int FRAME_death254 = 304;
	public static readonly int FRAME_death255 = 305;
	public static readonly int FRAME_death256 = 306;
	public static readonly int FRAME_death257 = 307;
	public static readonly int FRAME_death258 = 308;
	public static readonly int FRAME_death259 = 309;
	public static readonly int FRAME_death260 = 310;
	public static readonly int FRAME_death261 = 311;
	public static readonly int FRAME_death262 = 312;
	public static readonly int FRAME_death263 = 313;
	public static readonly int FRAME_death264 = 314;
	public static readonly int FRAME_death265 = 315;
	public static readonly int FRAME_death266 = 316;
	public static readonly int FRAME_death267 = 317;
	public static readonly int FRAME_death268 = 318;
	public static readonly int FRAME_death269 = 319;
	public static readonly int FRAME_death270 = 320;
	public static readonly int FRAME_death271 = 321;
	public static readonly int FRAME_death272 = 322;
	public static readonly int FRAME_death273 = 323;
	public static readonly int FRAME_death274 = 324;
	public static readonly int FRAME_death275 = 325;
	public static readonly int FRAME_death276 = 326;
	public static readonly int FRAME_death277 = 327;
	public static readonly int FRAME_death278 = 328;
	public static readonly int FRAME_death279 = 329;
	public static readonly int FRAME_death280 = 330;
	public static readonly int FRAME_death281 = 331;
	public static readonly int FRAME_death282 = 332;
	public static readonly int FRAME_death283 = 333;
	public static readonly int FRAME_death284 = 334;
	public static readonly int FRAME_death285 = 335;
	public static readonly int FRAME_death286 = 336;
	public static readonly int FRAME_death287 = 337;
	public static readonly int FRAME_death288 = 338;
	public static readonly int FRAME_death289 = 339;
	public static readonly int FRAME_death290 = 340;
	public static readonly int FRAME_death291 = 341;
	public static readonly int FRAME_death292 = 342;
	public static readonly int FRAME_death293 = 343;
	public static readonly int FRAME_death294 = 344;
	public static readonly int FRAME_death295 = 345;
	public static readonly int FRAME_death301 = 346;
	public static readonly int FRAME_death302 = 347;
	public static readonly int FRAME_death303 = 348;
	public static readonly int FRAME_death304 = 349;
	public static readonly int FRAME_death305 = 350;
	public static readonly int FRAME_death306 = 351;
	public static readonly int FRAME_death307 = 352;
	public static readonly int FRAME_death308 = 353;
	public static readonly int FRAME_death309 = 354;
	public static readonly int FRAME_death310 = 355;
	public static readonly int FRAME_death311 = 356;
	public static readonly int FRAME_death312 = 357;
	public static readonly int FRAME_death313 = 358;
	public static readonly int FRAME_death314 = 359;
	public static readonly int FRAME_death315 = 360;
	public static readonly int FRAME_death316 = 361;
	public static readonly int FRAME_death317 = 362;
	public static readonly int FRAME_death318 = 363;
	public static readonly int FRAME_death319 = 364;
	public static readonly int FRAME_death320 = 365;
	public static readonly int FRAME_jump01 = 366;
	public static readonly int FRAME_jump02 = 367;
	public static readonly int FRAME_jump03 = 368;
	public static readonly int FRAME_jump04 = 369;
	public static readonly int FRAME_jump05 = 370;
	public static readonly int FRAME_jump06 = 371;
	public static readonly int FRAME_jump07 = 372;
	public static readonly int FRAME_jump08 = 373;
	public static readonly int FRAME_jump09 = 374;
	public static readonly int FRAME_jump10 = 375;
	public static readonly int FRAME_jump11 = 376;
	public static readonly int FRAME_jump12 = 377;
	public static readonly int FRAME_jump13 = 378;
	public static readonly int FRAME_pain401 = 379;
	public static readonly int FRAME_pain402 = 380;
	public static readonly int FRAME_pain403 = 381;
	public static readonly int FRAME_pain404 = 382;
	public static readonly int FRAME_pain501 = 383;
	public static readonly int FRAME_pain502 = 384;
	public static readonly int FRAME_pain503 = 385;
	public static readonly int FRAME_pain504 = 386;
	public static readonly int FRAME_pain601 = 387;
	public static readonly int FRAME_pain602 = 388;
	public static readonly int FRAME_pain603 = 389;
	public static readonly int FRAME_pain604 = 390;
	public static readonly int FRAME_pain605 = 391;
	public static readonly int FRAME_pain606 = 392;
	public static readonly int FRAME_pain607 = 393;
	public static readonly int FRAME_pain608 = 394;
	public static readonly int FRAME_pain609 = 395;
	public static readonly int FRAME_pain610 = 396;
	public static readonly int FRAME_pain611 = 397;
	public static readonly int FRAME_pain612 = 398;
	public static readonly int FRAME_pain613 = 399;
	public static readonly int FRAME_pain614 = 400;
	public static readonly int FRAME_pain615 = 401;
	public static readonly int FRAME_pain616 = 402;
	public static readonly int FRAME_pain617 = 403;
	public static readonly int FRAME_pain618 = 404;
	public static readonly int FRAME_pain619 = 405;
	public static readonly int FRAME_pain620 = 406;
	public static readonly int FRAME_pain621 = 407;
	public static readonly int FRAME_pain622 = 408;
	public static readonly int FRAME_pain623 = 409;
	public static readonly int FRAME_pain624 = 410;
	public static readonly int FRAME_pain625 = 411;
	public static readonly int FRAME_pain626 = 412;
	public static readonly int FRAME_pain627 = 413;
	public static readonly int FRAME_stand201 = 414;
	public static readonly int FRAME_stand202 = 415;
	public static readonly int FRAME_stand203 = 416;
	public static readonly int FRAME_stand204 = 417;
	public static readonly int FRAME_stand205 = 418;
	public static readonly int FRAME_stand206 = 419;
	public static readonly int FRAME_stand207 = 420;
	public static readonly int FRAME_stand208 = 421;
	public static readonly int FRAME_stand209 = 422;
	public static readonly int FRAME_stand210 = 423;
	public static readonly int FRAME_stand211 = 424;
	public static readonly int FRAME_stand212 = 425;
	public static readonly int FRAME_stand213 = 426;
	public static readonly int FRAME_stand214 = 427;
	public static readonly int FRAME_stand215 = 428;
	public static readonly int FRAME_stand216 = 429;
	public static readonly int FRAME_stand217 = 430;
	public static readonly int FRAME_stand218 = 431;
	public static readonly int FRAME_stand219 = 432;
	public static readonly int FRAME_stand220 = 433;
	public static readonly int FRAME_stand221 = 434;
	public static readonly int FRAME_stand222 = 435;
	public static readonly int FRAME_stand223 = 436;
	public static readonly int FRAME_stand224 = 437;
	public static readonly int FRAME_stand225 = 438;
	public static readonly int FRAME_stand226 = 439;
	public static readonly int FRAME_stand227 = 440;
	public static readonly int FRAME_stand228 = 441;
	public static readonly int FRAME_stand229 = 442;
	public static readonly int FRAME_stand230 = 443;
	public static readonly int FRAME_stand231 = 444;
	public static readonly int FRAME_stand232 = 445;
	public static readonly int FRAME_stand233 = 446;
	public static readonly int FRAME_stand234 = 447;
	public static readonly int FRAME_stand235 = 448;
	public static readonly int FRAME_stand236 = 449;
	public static readonly int FRAME_stand237 = 450;
	public static readonly int FRAME_stand238 = 451;
	public static readonly int FRAME_stand239 = 452;
	public static readonly int FRAME_stand240 = 453;
	public static readonly int FRAME_stand241 = 454;
	public static readonly int FRAME_stand242 = 455;
	public static readonly int FRAME_stand243 = 456;
	public static readonly int FRAME_stand244 = 457;
	public static readonly int FRAME_stand245 = 458;
	public static readonly int FRAME_stand246 = 459;
	public static readonly int FRAME_stand247 = 460;
	public static readonly int FRAME_stand248 = 461;
	public static readonly int FRAME_stand249 = 462;
	public static readonly int FRAME_stand250 = 463;
	public static readonly int FRAME_stand251 = 464;
	public static readonly int FRAME_stand252 = 465;
	public static readonly int FRAME_stand253 = 466;
	public static readonly int FRAME_stand254 = 467;
	public static readonly int FRAME_stand255 = 468;
	public static readonly int FRAME_stand256 = 469;
	public static readonly int FRAME_stand257 = 470;
	public static readonly int FRAME_stand258 = 471;
	public static readonly int FRAME_stand259 = 472;
	public static readonly int FRAME_stand260 = 473;
	public static readonly int FRAME_walk201 = 474;
	public static readonly int FRAME_walk202 = 475;
	public static readonly int FRAME_walk203 = 476;
	public static readonly int FRAME_walk204 = 477;
	public static readonly int FRAME_walk205 = 478;
	public static readonly int FRAME_walk206 = 479;
	public static readonly int FRAME_walk207 = 480;
	public static readonly int FRAME_walk208 = 481;
	public static readonly int FRAME_walk209 = 482;
	public static readonly int FRAME_walk210 = 483;
	public static readonly int FRAME_walk211 = 484;
	public static readonly int FRAME_walk212 = 485;
	public static readonly int FRAME_walk213 = 486;
	public static readonly int FRAME_walk214 = 487;
	public static readonly int FRAME_walk215 = 488;
	public static readonly int FRAME_walk216 = 489;
	public static readonly int FRAME_walk217 = 490;
	public static readonly float MODEL_SCALE = 1.000000f;
	private static int sound_pain4;
	private static int sound_pain5;
	private static int sound_pain6;
	private static int sound_death;
	private static int sound_step_left;
	private static int sound_step_right;
	private static int sound_attack_bfg;
	private static int sound_brainsplorch;
	private static int sound_prerailgun;
	private static int sound_popup;
	private static int sound_taunt1;
	private static int sound_taunt2;
	private static int sound_taunt3;
	private static int sound_hit;

	private static readonly EntThinkAdapter makron_taunt = new("makron_taunt", self =>
	{
		float r;

		r = Lib.random();

		if (r <= 0.3)
			GameBase.gi.sound(self, Defines.CHAN_AUTO, M_Boss32.sound_taunt1, 1, Defines.ATTN_NONE, 0);
		else if (r <= 0.6)
			GameBase.gi.sound(self, Defines.CHAN_AUTO, M_Boss32.sound_taunt2, 1, Defines.ATTN_NONE, 0);
		else
			GameBase.gi.sound(self, Defines.CHAN_AUTO, M_Boss32.sound_taunt3, 1, Defines.ATTN_NONE, 0);

		return true;
	});

	//
	//	   stand
	//
	private static readonly EntThinkAdapter makron_stand = new("makron_stand", self =>
	{
		self.monsterinfo.currentmove = M_Boss32.makron_move_stand;

		return true;
	});

	/*
	 * static EntThinkAdapter xxx = new EntThinkAdapter() { public boolean
	 * think(edict_t self) { return true; } };
	 */

	private static readonly EntThinkAdapter makron_hit = new("makron_hit", self =>
	{
		GameBase.gi.sound(self, Defines.CHAN_AUTO, M_Boss32.sound_hit, 1, Defines.ATTN_NONE, 0);

		return true;
	});

	private static readonly EntThinkAdapter makron_popup = new("makron_popup", self =>
	{
		GameBase.gi.sound(self, Defines.CHAN_BODY, M_Boss32.sound_popup, 1, Defines.ATTN_NONE, 0);

		return true;
	});

	private static readonly EntThinkAdapter makron_step_left = new("makron_step_left", self =>
	{
		GameBase.gi.sound(self, Defines.CHAN_BODY, M_Boss32.sound_step_left, 1, Defines.ATTN_NORM, 0);

		return true;
	});

	private static readonly EntThinkAdapter makron_step_right = new("makron_step_right", self =>
	{
		GameBase.gi.sound(self, Defines.CHAN_BODY, M_Boss32.sound_step_right, 1, Defines.ATTN_NORM, 0);

		return true;
	});

	private static readonly EntThinkAdapter makron_brainsplorch = new("makron_brainsplorch", self =>
	{
		GameBase.gi.sound(self, Defines.CHAN_VOICE, M_Boss32.sound_brainsplorch, 1, Defines.ATTN_NORM, 0);

		return true;
	});

	private static readonly EntThinkAdapter makron_prerailgun = new("makron_prerailgun", self =>
	{
		GameBase.gi.sound(self, Defines.CHAN_WEAPON, M_Boss32.sound_prerailgun, 1, Defines.ATTN_NORM, 0);

		return true;
	});

	private static readonly mframe_t[] makron_frames_stand =
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

		// 10
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

		// 20
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

		// 30
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

		// 40
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

		// 50
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null),
		new(GameAI.ai_stand, 0, null) // 60
	};

	private static readonly mmove_t makron_move_stand = new(M_Boss32.FRAME_stand201, M_Boss32.FRAME_stand260, M_Boss32.makron_frames_stand, null);

	private static readonly mframe_t[] makron_frames_run =
	{
		new(GameAI.ai_run, 3, M_Boss32.makron_step_left),
		new(GameAI.ai_run, 12, null),
		new(GameAI.ai_run, 8, null),
		new(GameAI.ai_run, 8, null),
		new(GameAI.ai_run, 8, M_Boss32.makron_step_right),
		new(GameAI.ai_run, 6, null),
		new(GameAI.ai_run, 12, null),
		new(GameAI.ai_run, 9, null),
		new(GameAI.ai_run, 6, null),
		new(GameAI.ai_run, 12, null)
	};

	private static readonly mmove_t makron_move_run = new(M_Boss32.FRAME_walk204, M_Boss32.FRAME_walk213, M_Boss32.makron_frames_run, null);

	private static mframe_t[] makron_frames_walk =
	{
		new(GameAI.ai_walk, 3, M_Boss32.makron_step_left),
		new(GameAI.ai_walk, 12, null),
		new(GameAI.ai_walk, 8, null),
		new(GameAI.ai_walk, 8, null),
		new(GameAI.ai_walk, 8, M_Boss32.makron_step_right),
		new(GameAI.ai_walk, 6, null),
		new(GameAI.ai_walk, 12, null),
		new(GameAI.ai_walk, 9, null),
		new(GameAI.ai_walk, 6, null),
		new(GameAI.ai_walk, 12, null)
	};

	private static readonly mmove_t makron_move_walk = new(M_Boss32.FRAME_walk204, M_Boss32.FRAME_walk213, M_Boss32.makron_frames_run, null);

	//
	//	   death
	//
	private static readonly EntThinkAdapter makron_dead = new("makron_dead", self =>
	{
		Math3D.VectorSet(self.mins, -60, -60, 0);
		Math3D.VectorSet(self.maxs, 60, 60, 72);
		self.movetype = Defines.MOVETYPE_TOSS;
		self.svflags |= Defines.SVF_DEADMONSTER;
		self.nextthink = 0;
		GameBase.gi.linkentity(self);

		return true;
	});

	private static readonly EntThinkAdapter makron_walk = new("makron_walk", self =>
	{
		self.monsterinfo.currentmove = M_Boss32.makron_move_walk;

		return true;
	});

	private static readonly EntThinkAdapter makron_run = new("makron_run", self =>
	{
		if ((self.monsterinfo.aiflags & Defines.AI_STAND_GROUND) != 0)
			self.monsterinfo.currentmove = M_Boss32.makron_move_stand;
		else
			self.monsterinfo.currentmove = M_Boss32.makron_move_run;

		return true;
	});

	private static readonly mframe_t[] makron_frames_pain6 =
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

		// 10
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, M_Boss32.makron_popup),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),

		// 20
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, M_Boss32.makron_taunt),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null)
	};

	private static readonly mmove_t makron_move_pain6 =
		new(M_Boss32.FRAME_pain601, M_Boss32.FRAME_pain627, M_Boss32.makron_frames_pain6, M_Boss32.makron_run);

	private static readonly mframe_t[] makron_frames_pain5 =
	{
		new(GameAI.ai_move, 0, null), new(GameAI.ai_move, 0, null), new(GameAI.ai_move, 0, null), new(GameAI.ai_move, 0, null)
	};

	private static readonly mmove_t makron_move_pain5 =
		new(M_Boss32.FRAME_pain501, M_Boss32.FRAME_pain504, M_Boss32.makron_frames_pain5, M_Boss32.makron_run);

	private static readonly mframe_t[] makron_frames_pain4 =
	{
		new(GameAI.ai_move, 0, null), new(GameAI.ai_move, 0, null), new(GameAI.ai_move, 0, null), new(GameAI.ai_move, 0, null)
	};

	private static readonly mmove_t makron_move_pain4 =
		new(M_Boss32.FRAME_pain401, M_Boss32.FRAME_pain404, M_Boss32.makron_frames_pain4, M_Boss32.makron_run);

	private static readonly mframe_t[] makron_frames_death2 =
	{
		new(GameAI.ai_move, -15, null),
		new(GameAI.ai_move, 3, null),
		new(GameAI.ai_move, -12, null),
		new(GameAI.ai_move, 0, M_Boss32.makron_step_left),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),

		// 10
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 11, null),
		new(GameAI.ai_move, 12, null),
		new(GameAI.ai_move, 11, M_Boss32.makron_step_right),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),

		// 20
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

		// 30
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 5, null),
		new(GameAI.ai_move, 7, null),
		new(GameAI.ai_move, 6, M_Boss32.makron_step_left),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, -1, null),
		new(GameAI.ai_move, 2, null),

		// 40
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

		// 50
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, -6, null),
		new(GameAI.ai_move, -4, null),
		new(GameAI.ai_move, -6, M_Boss32.makron_step_right),
		new(GameAI.ai_move, -4, null),
		new(GameAI.ai_move, -4, M_Boss32.makron_step_left),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),

		// 60
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, -2, null),
		new(GameAI.ai_move, -5, null),
		new(GameAI.ai_move, -3, M_Boss32.makron_step_right),
		new(GameAI.ai_move, -8, null),
		new(GameAI.ai_move, -3, M_Boss32.makron_step_left),
		new(GameAI.ai_move, -7, null),
		new(GameAI.ai_move, -4, null),
		new(GameAI.ai_move, -4, M_Boss32.makron_step_right),

		// 70
		new(GameAI.ai_move, -6, null),
		new(GameAI.ai_move, -7, null),
		new(GameAI.ai_move, 0, M_Boss32.makron_step_left),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),

		// 80
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, -2, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 2, null),
		new(GameAI.ai_move, 0, null),

		// 90
		new(GameAI.ai_move, 27, M_Boss32.makron_hit),
		new(GameAI.ai_move, 26, null),
		new(GameAI.ai_move, 0, M_Boss32.makron_brainsplorch),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null) // 95
	};

	private static readonly mmove_t makron_move_death2 =
		new(M_Boss32.FRAME_death201, M_Boss32.FRAME_death295, M_Boss32.makron_frames_death2, M_Boss32.makron_dead);

	private static readonly mframe_t[] makron_frames_death3 =
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

	private static mmove_t makron_move_death3 = new(M_Boss32.FRAME_death301, M_Boss32.FRAME_death320, M_Boss32.makron_frames_death3, null);

	private static readonly mframe_t[] makron_frames_sight =
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

	private static readonly mmove_t makron_move_sight =
		new(M_Boss32.FRAME_active01, M_Boss32.FRAME_active13, M_Boss32.makron_frames_sight, M_Boss32.makron_run);

	private static readonly EntThinkAdapter makronBFG = new("makronBFG", self =>
	{
		float[] forward = { 0, 0, 0 }, right = { 0, 0, 0 };

		float[] start = { 0, 0, 0 };
		float[] dir = { 0, 0, 0 };
		float[] vec = { 0, 0, 0 };

		Math3D.AngleVectors(self.s.angles, forward, right, null);
		Math3D.G_ProjectSource(self.s.origin, M_Flash.monster_flash_offset[Defines.MZ2_MAKRON_BFG], forward, right, start);

		Math3D.VectorCopy(self.enemy.s.origin, vec);
		vec[2] += self.enemy.viewheight;
		Math3D.VectorSubtract(vec, start, dir);
		Math3D.VectorNormalize(dir);
		GameBase.gi.sound(self, Defines.CHAN_VOICE, M_Boss32.sound_attack_bfg, 1, Defines.ATTN_NORM, 0);
		Monster.monster_fire_bfg(self, start, dir, 50, 300, 100, 300, Defines.MZ2_MAKRON_BFG);

		return true;
	});

	private static readonly EntThinkAdapter MakronSaveloc = new("MakronSaveloc", self =>
	{
		Math3D.VectorCopy(self.enemy.s.origin, self.pos1); //save for

		// aiming the
		// shot
		self.pos1[2] += self.enemy.viewheight;

		return true;
	});

	//	   FIXME: He's not firing from the proper Z

	private static readonly EntThinkAdapter MakronRailgun = new("MakronRailgun", self =>
	{
		float[] start = { 0, 0, 0 };
		float[] dir = { 0, 0, 0 };
		float[] forward = { 0, 0, 0 }, right = { 0, 0, 0 };

		Math3D.AngleVectors(self.s.angles, forward, right, null);
		Math3D.G_ProjectSource(self.s.origin, M_Flash.monster_flash_offset[Defines.MZ2_MAKRON_RAILGUN_1], forward, right, start);

		// calc direction to where we targted
		Math3D.VectorSubtract(self.pos1, start, dir);
		Math3D.VectorNormalize(dir);

		Monster.monster_fire_railgun(self, start, dir, 50, 100, Defines.MZ2_MAKRON_RAILGUN_1);

		return true;
	});

	//	   FIXME: This is all wrong. He's not firing at the proper angles.

	private static readonly EntThinkAdapter MakronHyperblaster = new("MakronHyperblaster", self =>
	{
		float[] dir = { 0, 0, 0 };
		float[] vec = { 0, 0, 0 };
		float[] start = { 0, 0, 0 };
		float[] forward = { 0, 0, 0 }, right = { 0, 0, 0 };
		int flash_number;

		flash_number = Defines.MZ2_MAKRON_BLASTER_1 + (self.s.frame - M_Boss32.FRAME_attak405);

		Math3D.AngleVectors(self.s.angles, forward, right, null);
		Math3D.G_ProjectSource(self.s.origin, M_Flash.monster_flash_offset[flash_number], forward, right, start);

		if (self.enemy != null)
		{
			Math3D.VectorCopy(self.enemy.s.origin, vec);
			vec[2] += self.enemy.viewheight;
			Math3D.VectorSubtract(vec, start, vec);
			Math3D.vectoangles(vec, vec);
			dir[0] = vec[0];
		}
		else
			dir[0] = 0;

		if (self.s.frame <= M_Boss32.FRAME_attak413)
			dir[1] = self.s.angles[1] - 10 * (self.s.frame - M_Boss32.FRAME_attak413);
		else
			dir[1] = self.s.angles[1] + 10 * (self.s.frame - M_Boss32.FRAME_attak421);

		dir[2] = 0;

		Math3D.AngleVectors(dir, forward, null, null);

		Monster.monster_fire_blaster(self, start, forward, 15, 1000, Defines.MZ2_MAKRON_BLASTER_1, Defines.EF_BLASTER);

		return true;
	});

	private static readonly EntPainAdapter makron_pain = new("makron_pain", (self, other, kick, damage) =>
	{
		if (self.health < self.max_health / 2)
			self.s.skinnum = 1;

		if (GameBase.level.time < self.pain_debounce_time)
			return;

		// Lessen the chance of him going into his pain frames
		if (damage <= 25)
		{
			if (Lib.random() < 0.2)
				return;
		}

		self.pain_debounce_time = GameBase.level.time + 3;

		if (GameBase.skill.value == 3)
			return; // no pain anims in nightmare

		if (damage <= 40)
		{
			GameBase.gi.sound(self, Defines.CHAN_VOICE, M_Boss32.sound_pain4, 1, Defines.ATTN_NONE, 0);
			self.monsterinfo.currentmove = M_Boss32.makron_move_pain4;
		}
		else if (damage <= 110)
		{
			GameBase.gi.sound(self, Defines.CHAN_VOICE, M_Boss32.sound_pain5, 1, Defines.ATTN_NONE, 0);
			self.monsterinfo.currentmove = M_Boss32.makron_move_pain5;
		}
		else
		{
			if (damage <= 150)
			{
				if (Lib.random() <= 0.45)
				{
					GameBase.gi.sound(self, Defines.CHAN_VOICE, M_Boss32.sound_pain6, 1, Defines.ATTN_NONE, 0);
					self.monsterinfo.currentmove = M_Boss32.makron_move_pain6;
				}
				else if (Lib.random() <= 0.35)
				{
					GameBase.gi.sound(self, Defines.CHAN_VOICE, M_Boss32.sound_pain6, 1, Defines.ATTN_NONE, 0);
					self.monsterinfo.currentmove = M_Boss32.makron_move_pain6;
				}
			}
		}
	});

	private static readonly EntInteractAdapter makron_sight = new("makron_sight", (self, other) =>
	{
		self.monsterinfo.currentmove = M_Boss32.makron_move_sight;

		return true;
	});

	private static readonly EntThinkAdapter makron_attack = new("makron_attack", self =>
	{
		float[] vec = { 0, 0, 0 };
		float range;
		float r;

		r = Lib.random();

		Math3D.VectorSubtract(self.enemy.s.origin, self.s.origin, vec);
		range = Math3D.VectorLength(vec);

		if (r <= 0.3)
			self.monsterinfo.currentmove = M_Boss32.makron_move_attack3;
		else if (r <= 0.6)
			self.monsterinfo.currentmove = M_Boss32.makron_move_attack4;
		else
			self.monsterinfo.currentmove = M_Boss32.makron_move_attack5;

		return true;
	});

	/*
	 * --- Makron Torso. This needs to be spawned in ---
	 */

	private static readonly EntThinkAdapter makron_torso_think = new("makron_torso_think", self =>
	{
		if (++self.s.frame < 365)
			self.nextthink = GameBase.level.time + Defines.FRAMETIME;
		else
		{
			self.s.frame = 346;
			self.nextthink = GameBase.level.time + Defines.FRAMETIME;
		}

		return true;
	});

	private static readonly EntThinkAdapter makron_torso = new("makron_torso", ent =>
	{
		ent.movetype = Defines.MOVETYPE_NONE;
		ent.solid = Defines.SOLID_NOT;
		Math3D.VectorSet(ent.mins, -8, -8, 0);
		Math3D.VectorSet(ent.maxs, 8, 8, 8);
		ent.s.frame = 346;
		ent.s.modelindex = GameBase.gi.modelindex("models/monsters/boss3/rider/tris.md2");
		ent.think = M_Boss32.makron_torso_think;
		ent.nextthink = GameBase.level.time + 2 * Defines.FRAMETIME;
		ent.s.sound = GameBase.gi.soundindex("makron/spine.wav");
		GameBase.gi.linkentity(ent);

		return true;
	});

	private static readonly EntDieAdapter makron_die = new("makron_die", (self, inflictor, attacker, damage, point) =>
	{
		edict_t tempent;

		int n;

		self.s.sound = 0;

		// check for gib
		if (self.health <= self.gib_health)
		{
			GameBase.gi.sound(self, Defines.CHAN_VOICE, GameBase.gi.soundindex("misc/udeath.wav"), 1, Defines.ATTN_NORM, 0);

			for (n = 0; n < 1 /* 4 */; n++)
				GameMisc.ThrowGib(self, "models/objects/gibs/sm_meat/tris.md2", damage, Defines.GIB_ORGANIC);

			for (n = 0; n < 4; n++)
				GameMisc.ThrowGib(self, "models/objects/gibs/sm_metal/tris.md2", damage, Defines.GIB_METALLIC);

			GameMisc.ThrowHead(self, "models/objects/gibs/gear/tris.md2", damage, Defines.GIB_METALLIC);
			self.deadflag = Defines.DEAD_DEAD;

			return;
		}

		if (self.deadflag == Defines.DEAD_DEAD)
			return;

		//	   regular death
		GameBase.gi.sound(self, Defines.CHAN_VOICE, M_Boss32.sound_death, 1, Defines.ATTN_NONE, 0);
		self.deadflag = Defines.DEAD_DEAD;
		self.takedamage = Defines.DAMAGE_YES;

		tempent = GameUtil.G_Spawn();
		Math3D.VectorCopy(self.s.origin, tempent.s.origin);
		Math3D.VectorCopy(self.s.angles, tempent.s.angles);
		tempent.s.origin[1] -= 84;
		M_Boss32.makron_torso.think(tempent);

		self.monsterinfo.currentmove = M_Boss32.makron_move_death2;
	});

	private static readonly EntThinkAdapter Makron_CheckAttack = new("Makron_CheckAttack", self =>
	{
		float[] spot1 = { 0, 0, 0 }, spot2 = { 0, 0, 0 };
		float[] temp = { 0, 0, 0 };
		float chance;
		trace_t tr;

		int enemy_range;
		float enemy_yaw;

		if (self.enemy.health > 0)
		{
			// see if any entities are in the way of the shot
			Math3D.VectorCopy(self.s.origin, spot1);
			spot1[2] += self.viewheight;
			Math3D.VectorCopy(self.enemy.s.origin, spot2);
			spot2[2] += self.enemy.viewheight;

			tr = GameBase.gi.trace(
				spot1,
				null,
				null,
				spot2,
				self,
				Defines.CONTENTS_SOLID | Defines.CONTENTS_MONSTER | Defines.CONTENTS_SLIME | Defines.CONTENTS_LAVA
			);

			// do we have a clear shot?
			if (tr.ent != self.enemy)
				return false;
		}

		enemy_range = GameUtil.range(self, self.enemy);
		Math3D.VectorSubtract(self.enemy.s.origin, self.s.origin, temp);
		enemy_yaw = Math3D.vectoyaw(temp);

		self.ideal_yaw = enemy_yaw;

		// melee attack
		if (enemy_range == Defines.RANGE_MELEE)
		{
			if (self.monsterinfo.melee != null)
				self.monsterinfo.attack_state = Defines.AS_MELEE;
			else
				self.monsterinfo.attack_state = Defines.AS_MISSILE;

			return true;
		}

		//	   missile attack
		if (null != self.monsterinfo.attack)
			return false;

		if (GameBase.level.time < self.monsterinfo.attack_finished)
			return false;

		if (enemy_range == Defines.RANGE_FAR)
			return false;

		if ((self.monsterinfo.aiflags & Defines.AI_STAND_GROUND) != 0)
			chance = 0.4f;
		else if (enemy_range == Defines.RANGE_MELEE)
			chance = 0.8f;
		else if (enemy_range == Defines.RANGE_NEAR)
			chance = 0.4f;
		else if (enemy_range == Defines.RANGE_MID)
			chance = 0.2f;
		else
			return false;

		if (Lib.random() < chance)
		{
			self.monsterinfo.attack_state = Defines.AS_MISSILE;
			self.monsterinfo.attack_finished = GameBase.level.time + 2 * Lib.random();

			return true;
		}

		if ((self.flags & Defines.FL_FLY) != 0)
		{
			if (Lib.random() < 0.3)
				self.monsterinfo.attack_state = Defines.AS_SLIDING;
			else
				self.monsterinfo.attack_state = Defines.AS_STRAIGHT;
		}

		return false;
	});

	private static readonly mframe_t[] makron_frames_attack3 =
	{
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, M_Boss32.makronBFG),

		// FIXME: BFG Attack here
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null)
	};

	private static readonly mmove_t makron_move_attack3 =
		new(M_Boss32.FRAME_attak301, M_Boss32.FRAME_attak308, M_Boss32.makron_frames_attack3, M_Boss32.makron_run);

	private static readonly mframe_t[] makron_frames_attack4 =
	{
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_move, 0, M_Boss32.MakronHyperblaster),

		// fire
		new(GameAI.ai_move, 0, M_Boss32.MakronHyperblaster), // fire
		new(GameAI.ai_move, 0, M_Boss32.MakronHyperblaster), // fire
		new(GameAI.ai_move, 0, M_Boss32.MakronHyperblaster), // fire
		new(GameAI.ai_move, 0, M_Boss32.MakronHyperblaster), // fire
		new(GameAI.ai_move, 0, M_Boss32.MakronHyperblaster), // fire
		new(GameAI.ai_move, 0, M_Boss32.MakronHyperblaster), // fire
		new(GameAI.ai_move, 0, M_Boss32.MakronHyperblaster), // fire
		new(GameAI.ai_move, 0, M_Boss32.MakronHyperblaster), // fire
		new(GameAI.ai_move, 0, M_Boss32.MakronHyperblaster), // fire
		new(GameAI.ai_move, 0, M_Boss32.MakronHyperblaster), // fire
		new(GameAI.ai_move, 0, M_Boss32.MakronHyperblaster), // fire
		new(GameAI.ai_move, 0, M_Boss32.MakronHyperblaster), // fire
		new(GameAI.ai_move, 0, M_Boss32.MakronHyperblaster), // fire
		new(GameAI.ai_move, 0, M_Boss32.MakronHyperblaster), // fire
		new(GameAI.ai_move, 0, M_Boss32.MakronHyperblaster), // fire
		new(GameAI.ai_move, 0, M_Boss32.MakronHyperblaster), // fire
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null)
	};

	private static readonly mmove_t makron_move_attack4 =
		new(M_Boss32.FRAME_attak401, M_Boss32.FRAME_attak426, M_Boss32.makron_frames_attack4, M_Boss32.makron_run);

	private static readonly mframe_t[] makron_frames_attack5 =
	{
		new(GameAI.ai_charge, 0, M_Boss32.makron_prerailgun),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, null),
		new(GameAI.ai_charge, 0, M_Boss32.MakronSaveloc),
		new(GameAI.ai_move, 0, M_Boss32.MakronRailgun),

		// Fire railgun
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null),
		new(GameAI.ai_move, 0, null)
	};

	private static readonly mmove_t makron_move_attack5 =
		new(M_Boss32.FRAME_attak501, M_Boss32.FRAME_attak516, M_Boss32.makron_frames_attack5, M_Boss32.makron_run);

	/*
	 * ================= MakronSpawn
	 * 
	 * =================
	 */
	private static readonly EntThinkAdapter MakronSpawn = new("MakronSpawn", self =>
	{
		float[] vec = { 0, 0, 0 };

		edict_t player;

		M_Boss32.SP_monster_makron(self);

		// jump at player
		player = GameBase.level.sight_client;

		if (player == null)
			return true;

		Math3D.VectorSubtract(player.s.origin, self.s.origin, vec);
		self.s.angles[Defines.YAW] = Math3D.vectoyaw(vec);
		Math3D.VectorNormalize(vec);
		Math3D.VectorMA(Globals.vec3_origin, 400, vec, self.velocity);
		self.velocity[2] = 200;
		self.groundentity = null;

		return true;
	});

	public static EntThinkAdapter MakronToss = new("MakronToss", self =>
	{
		edict_t ent;

		ent = GameUtil.G_Spawn();
		ent.nextthink = GameBase.level.time + 0.8f;
		ent.think = M_Boss32.MakronSpawn;
		ent.target = self.target;
		Math3D.VectorCopy(self.s.origin, ent.s.origin);

		return true;
	});

	//
	//	   monster_makron
	//

	public static void MakronPrecache()
	{
		M_Boss32.sound_pain4 = GameBase.gi.soundindex("makron/pain3.wav");
		M_Boss32.sound_pain5 = GameBase.gi.soundindex("makron/pain2.wav");
		M_Boss32.sound_pain6 = GameBase.gi.soundindex("makron/pain1.wav");
		M_Boss32.sound_death = GameBase.gi.soundindex("makron/death.wav");
		M_Boss32.sound_step_left = GameBase.gi.soundindex("makron/step1.wav");
		M_Boss32.sound_step_right = GameBase.gi.soundindex("makron/step2.wav");
		M_Boss32.sound_attack_bfg = GameBase.gi.soundindex("makron/bfg_fire.wav");
		M_Boss32.sound_brainsplorch = GameBase.gi.soundindex("makron/brain1.wav");
		M_Boss32.sound_prerailgun = GameBase.gi.soundindex("makron/rail_up.wav");
		M_Boss32.sound_popup = GameBase.gi.soundindex("makron/popup.wav");
		M_Boss32.sound_taunt1 = GameBase.gi.soundindex("makron/voice4.wav");
		M_Boss32.sound_taunt2 = GameBase.gi.soundindex("makron/voice3.wav");
		M_Boss32.sound_taunt3 = GameBase.gi.soundindex("makron/voice.wav");
		M_Boss32.sound_hit = GameBase.gi.soundindex("makron/bhit.wav");

		GameBase.gi.modelindex("models/monsters/boss3/rider/tris.md2");
	}

	/*
	 * QUAKED monster_makron (1 .5 0) (-30 -30 0) (30 30 90) Ambush
	 * Trigger_Spawn Sight
	 */
	private static void SP_monster_makron(edict_t self)
	{
		if (GameBase.deathmatch.value != 0)
		{
			GameUtil.G_FreeEdict(self);

			return;
		}

		M_Boss32.MakronPrecache();

		self.movetype = Defines.MOVETYPE_STEP;
		self.solid = Defines.SOLID_BBOX;
		self.s.modelindex = GameBase.gi.modelindex("models/monsters/boss3/rider/tris.md2");
		Math3D.VectorSet(self.mins, -30, -30, 0);
		Math3D.VectorSet(self.maxs, 30, 30, 90);

		self.health = 3000;
		self.gib_health = -2000;
		self.mass = 500;

		self.pain = M_Boss32.makron_pain;
		self.die = M_Boss32.makron_die;
		self.monsterinfo.stand = M_Boss32.makron_stand;
		self.monsterinfo.walk = M_Boss32.makron_walk;
		self.monsterinfo.run = M_Boss32.makron_run;
		self.monsterinfo.dodge = null;
		self.monsterinfo.attack = M_Boss32.makron_attack;
		self.monsterinfo.melee = null;
		self.monsterinfo.sight = M_Boss32.makron_sight;
		self.monsterinfo.checkattack = M_Boss32.Makron_CheckAttack;

		GameBase.gi.linkentity(self);

		//		self.monsterinfo.currentmove = &makron_move_stand;
		self.monsterinfo.currentmove = M_Boss32.makron_move_sight;
		self.monsterinfo.scale = M_Boss32.MODEL_SCALE;

		GameAI.walkmonster_start.think(self);
	}
}