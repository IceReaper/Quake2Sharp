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
using Quake2Sharp.game.types;
using Quake2Sharp.qcommon.types;
using Quake2Sharp.render;
using Quake2Sharp.render.types;
using System.Runtime.InteropServices;

namespace Quake2Sharp;

/**
 * Globals ist the collection of global variables and constants.
 * It is more elegant to use these vars by inheritance to separate
 * it with eclipse refactoring later.
 *
 * As consequence you dont have to touch that much code this time.
 */
public class Globals
{
	public const string YQ2VERSION = "8.20";
	public const string BASEDIRNAME = "baseq2";
	public static readonly string YQ2OSTYPE = RuntimeInformation.OSDescription;
	public static readonly string YQ2ARCH = RuntimeInformation.OSArchitecture.ToString();
	public static readonly DateTime BUILD_DATE = DateTime.UtcNow;
	public const string CFGDIR = "YamagiQ2";

	/*
	 * global variables
	 */
	public static int c_traces;
	public static int c_brush_traces;
	public static int c_pointcontents;
	public static cvar_t cl_add_blend;
	public static cvar_t cl_add_entities;
	public static cvar_t cl_add_lights;
	public static cvar_t cl_add_particles;
	public static cvar_t cl_anglespeedkey;
	public static cvar_t cl_autoskins;
	public static cvar_t cl_footsteps;
	public static cvar_t cl_forwardspeed;
	public static cvar_t cl_gun;
	public static cvar_t cl_noskins;
	public static cvar_t cl_pitchspeed;
	public static cvar_t cl_predict;
	public static cvar_t cl_run;
	public static cvar_t cl_sidespeed;
	public static cvar_t cl_stereo;
	public static cvar_t cl_stereo_separation;
	public static cvar_t cl_timeout;
	public static cvar_t cl_upspeed;
	public static cvar_t cl_yawspeed;
	public static cvar_t freelook;
	public static cvar_t lookspring;
	public static cvar_t lookstrafe;
	public static cvar_t nostdout;
	public static cvar_t sensitivity;
	public static cvar_t in_mouse;
	public static cvar_t in_joystick;

	public static sizebuf_t net_message = new();
	/*
	=============================================================================

							COMMAND BUFFER

	=============================================================================
	*/

	//=============================================================================
	public static byte[] net_message_buffer = new byte[Defines.MAX_MSGLEN];
	public static cvar_t m_pitch;
	public static cvar_t m_yaw;
	public static cvar_t m_forward;
	public static cvar_t m_side;
	public static cvar_t cl_lightlevel;

	//
	//	   userinfo
	//
	public static cvar_t info_password;
	public static cvar_t info_spectator;
	public static cvar_t name;
	public static cvar_t skin;
	public static cvar_t rate;
	public static cvar_t fov;
	public static cvar_t msg;
	public static cvar_t hand;
	public static cvar_t gender;
	public static cvar_t gender_auto;
	public static cvar_t cl_vwep;
	public static client_static_t cls = new();
	public static client_state_t cl = new();
	public static centity_t[] cl_entities = new centity_t[Defines.MAX_EDICTS];

	private static void Globals1()
	{
		for (var i = 0; i < Globals.cl_entities.Length; i++)
			Globals.cl_entities[i] = new();
	}

	public static entity_state_t[] cl_parse_entities = new entity_state_t[Defines.MAX_PARSE_ENTITIES];

	private static void Globals2()
	{
		for (var i = 0; i < Globals.cl_parse_entities.Length; i++)
			Globals.cl_parse_entities[i] = new(null);
	}

	public static cvar_t rcon_client_password;
	public static cvar_t rcon_address;
	public static cvar_t cl_shownet;
	public static cvar_t cl_showmiss;
	public static cvar_t cl_showclamp;
	public static cvar_t cl_paused;

	// client/anorms.h
	public static readonly float[][] bytedirs =
	{
		/**
									   */
		new[] { -0.525731f, 0.000000f, 0.850651f },
		new[] { -0.442863f, 0.238856f, 0.864188f },
		new[] { -0.295242f, 0.000000f, 0.955423f },
		new[] { -0.309017f, 0.500000f, 0.809017f },
		new[] { -0.162460f, 0.262866f, 0.951056f },
		new[] { 0.000000f, 0.000000f, 1.000000f },
		new[] { 0.000000f, 0.850651f, 0.525731f },
		new[] { -0.147621f, 0.716567f, 0.681718f },
		new[] { 0.147621f, 0.716567f, 0.681718f },
		new[] { 0.000000f, 0.525731f, 0.850651f },
		new[] { 0.309017f, 0.500000f, 0.809017f },
		new[] { 0.525731f, 0.000000f, 0.850651f },
		new[] { 0.295242f, 0.000000f, 0.955423f },
		new[] { 0.442863f, 0.238856f, 0.864188f },
		new[] { 0.162460f, 0.262866f, 0.951056f },
		new[] { -0.681718f, 0.147621f, 0.716567f },
		new[] { -0.809017f, 0.309017f, 0.500000f },
		new[] { -0.587785f, 0.425325f, 0.688191f },
		new[] { -0.850651f, 0.525731f, 0.000000f },
		new[] { -0.864188f, 0.442863f, 0.238856f },
		new[] { -0.716567f, 0.681718f, 0.147621f },
		new[] { -0.688191f, 0.587785f, 0.425325f },
		new[] { -0.500000f, 0.809017f, 0.309017f },
		new[] { -0.238856f, 0.864188f, 0.442863f },
		new[] { -0.425325f, 0.688191f, 0.587785f },
		new[] { -0.716567f, 0.681718f, -0.147621f },
		new[] { -0.500000f, 0.809017f, -0.309017f },
		new[] { -0.525731f, 0.850651f, 0.000000f },
		new[] { 0.000000f, 0.850651f, -0.525731f },
		new[] { -0.238856f, 0.864188f, -0.442863f },
		new[] { 0.000000f, 0.955423f, -0.295242f },
		new[] { -0.262866f, 0.951056f, -0.162460f },
		new[] { 0.000000f, 1.000000f, 0.000000f },
		new[] { 0.000000f, 0.955423f, 0.295242f },
		new[] { -0.262866f, 0.951056f, 0.162460f },
		new[] { 0.238856f, 0.864188f, 0.442863f },
		new[] { 0.262866f, 0.951056f, 0.162460f },
		new[] { 0.500000f, 0.809017f, 0.309017f },
		new[] { 0.238856f, 0.864188f, -0.442863f },
		new[] { 0.262866f, 0.951056f, -0.162460f },
		new[] { 0.500000f, 0.809017f, -0.309017f },
		new[] { 0.850651f, 0.525731f, 0.000000f },
		new[] { 0.716567f, 0.681718f, 0.147621f },
		new[] { 0.716567f, 0.681718f, -0.147621f },
		new[] { 0.525731f, 0.850651f, 0.000000f },
		new[] { 0.425325f, 0.688191f, 0.587785f },
		new[] { 0.864188f, 0.442863f, 0.238856f },
		new[] { 0.688191f, 0.587785f, 0.425325f },
		new[] { 0.809017f, 0.309017f, 0.500000f },
		new[] { 0.681718f, 0.147621f, 0.716567f },
		new[] { 0.587785f, 0.425325f, 0.688191f },
		new[] { 0.955423f, 0.295242f, 0.000000f },
		new[] { 1.000000f, 0.000000f, 0.000000f },
		new[] { 0.951056f, 0.162460f, 0.262866f },
		new[] { 0.850651f, -0.525731f, 0.000000f },
		new[] { 0.955423f, -0.295242f, 0.000000f },
		new[] { 0.864188f, -0.442863f, 0.238856f },
		new[] { 0.951056f, -0.162460f, 0.262866f },
		new[] { 0.809017f, -0.309017f, 0.500000f },
		new[] { 0.681718f, -0.147621f, 0.716567f },
		new[] { 0.850651f, 0.000000f, 0.525731f },
		new[] { 0.864188f, 0.442863f, -0.238856f },
		new[] { 0.809017f, 0.309017f, -0.500000f },
		new[] { 0.951056f, 0.162460f, -0.262866f },
		new[] { 0.525731f, 0.000000f, -0.850651f },
		new[] { 0.681718f, 0.147621f, -0.716567f },
		new[] { 0.681718f, -0.147621f, -0.716567f },
		new[] { 0.850651f, 0.000000f, -0.525731f },
		new[] { 0.809017f, -0.309017f, -0.500000f },
		new[] { 0.864188f, -0.442863f, -0.238856f },
		new[] { 0.951056f, -0.162460f, -0.262866f },
		new[] { 0.147621f, 0.716567f, -0.681718f },
		new[] { 0.309017f, 0.500000f, -0.809017f },
		new[] { 0.425325f, 0.688191f, -0.587785f },
		new[] { 0.442863f, 0.238856f, -0.864188f },
		new[] { 0.587785f, 0.425325f, -0.688191f },
		new[] { 0.688191f, 0.587785f, -0.425325f },
		new[] { -0.147621f, 0.716567f, -0.681718f },
		new[] { -0.309017f, 0.500000f, -0.809017f },
		new[] { 0.000000f, 0.525731f, -0.850651f },
		new[] { -0.525731f, 0.000000f, -0.850651f },
		new[] { -0.442863f, 0.238856f, -0.864188f },
		new[] { -0.295242f, 0.000000f, -0.955423f },
		new[] { -0.162460f, 0.262866f, -0.951056f },
		new[] { 0.000000f, 0.000000f, -1.000000f },
		new[] { 0.295242f, 0.000000f, -0.955423f },
		new[] { 0.162460f, 0.262866f, -0.951056f },
		new[] { -0.442863f, -0.238856f, -0.864188f },
		new[] { -0.309017f, -0.500000f, -0.809017f },
		new[] { -0.162460f, -0.262866f, -0.951056f },
		new[] { 0.000000f, -0.850651f, -0.525731f },
		new[] { -0.147621f, -0.716567f, -0.681718f },
		new[] { 0.147621f, -0.716567f, -0.681718f },
		new[] { 0.000000f, -0.525731f, -0.850651f },
		new[] { 0.309017f, -0.500000f, -0.809017f },
		new[] { 0.442863f, -0.238856f, -0.864188f },
		new[] { 0.162460f, -0.262866f, -0.951056f },
		new[] { 0.238856f, -0.864188f, -0.442863f },
		new[] { 0.500000f, -0.809017f, -0.309017f },
		new[] { 0.425325f, -0.688191f, -0.587785f },
		new[] { 0.716567f, -0.681718f, -0.147621f },
		new[] { 0.688191f, -0.587785f, -0.425325f },
		new[] { 0.587785f, -0.425325f, -0.688191f },
		new[] { 0.000000f, -0.955423f, -0.295242f },
		new[] { 0.000000f, -1.000000f, 0.000000f },
		new[] { 0.262866f, -0.951056f, -0.162460f },
		new[] { 0.000000f, -0.850651f, 0.525731f },
		new[] { 0.000000f, -0.955423f, 0.295242f },
		new[] { 0.238856f, -0.864188f, 0.442863f },
		new[] { 0.262866f, -0.951056f, 0.162460f },
		new[] { 0.500000f, -0.809017f, 0.309017f },
		new[] { 0.716567f, -0.681718f, 0.147621f },
		new[] { 0.525731f, -0.850651f, 0.000000f },
		new[] { -0.238856f, -0.864188f, -0.442863f },
		new[] { -0.500000f, -0.809017f, -0.309017f },
		new[] { -0.262866f, -0.951056f, -0.162460f },
		new[] { -0.850651f, -0.525731f, 0.000000f },
		new[] { -0.716567f, -0.681718f, -0.147621f },
		new[] { -0.716567f, -0.681718f, 0.147621f },
		new[] { -0.525731f, -0.850651f, 0.000000f },
		new[] { -0.500000f, -0.809017f, 0.309017f },
		new[] { -0.238856f, -0.864188f, 0.442863f },
		new[] { -0.262866f, -0.951056f, 0.162460f },
		new[] { -0.864188f, -0.442863f, 0.238856f },
		new[] { -0.809017f, -0.309017f, 0.500000f },
		new[] { -0.688191f, -0.587785f, 0.425325f },
		new[] { -0.681718f, -0.147621f, 0.716567f },
		new[] { -0.442863f, -0.238856f, 0.864188f },
		new[] { -0.587785f, -0.425325f, 0.688191f },
		new[] { -0.309017f, -0.500000f, 0.809017f },
		new[] { -0.147621f, -0.716567f, 0.681718f },
		new[] { -0.425325f, -0.688191f, 0.587785f },
		new[] { -0.162460f, -0.262866f, 0.951056f },
		new[] { 0.442863f, -0.238856f, 0.864188f },
		new[] { 0.162460f, -0.262866f, 0.951056f },
		new[] { 0.309017f, -0.500000f, 0.809017f },
		new[] { 0.147621f, -0.716567f, 0.681718f },
		new[] { 0.000000f, -0.525731f, 0.850651f },
		new[] { 0.425325f, -0.688191f, 0.587785f },
		new[] { 0.587785f, -0.425325f, 0.688191f },
		new[] { 0.688191f, -0.587785f, 0.425325f },
		new[] { -0.955423f, 0.295242f, 0.000000f },
		new[] { -0.951056f, 0.162460f, 0.262866f },
		new[] { -1.000000f, 0.000000f, 0.000000f },
		new[] { -0.850651f, 0.000000f, 0.525731f },
		new[] { -0.955423f, -0.295242f, 0.000000f },
		new[] { -0.951056f, -0.162460f, 0.262866f },
		new[] { -0.864188f, 0.442863f, -0.238856f },
		new[] { -0.951056f, 0.162460f, -0.262866f },
		new[] { -0.809017f, 0.309017f, -0.500000f },
		new[] { -0.864188f, -0.442863f, -0.238856f },
		new[] { -0.951056f, -0.162460f, -0.262866f },
		new[] { -0.809017f, -0.309017f, -0.500000f },
		new[] { -0.681718f, 0.147621f, -0.716567f },
		new[] { -0.681718f, -0.147621f, -0.716567f },
		new[] { -0.850651f, 0.000000f, -0.525731f },
		new[] { -0.688191f, 0.587785f, -0.425325f },
		new[] { -0.587785f, 0.425325f, -0.688191f },
		new[] { -0.425325f, 0.688191f, -0.587785f },
		new[] { -0.425325f, -0.688191f, -0.587785f },
		new[] { -0.587785f, -0.425325f, -0.688191f },
		new[] { -0.688191f, -0.587785f, -0.425325f }
	};

	public static bool userinfo_modified = false;
	public static readonly console_t con = new();
	public static cvar_t con_notifytime;
	public static viddef_t viddef = new();

	// Renderer interface used by VID, SCR, ...
	public static refexport_t re = new DummyRenderer();
	public static string[] keybindings = new string[256];
	public static bool[] keydown = new bool[256];
	public static bool chat_team = false;
	public static string chat_buffer = "";
	public static char[][] key_lines = new char[32][];
	public static int key_linepos;

	private static void Globals3()
	{
		for (var i = 0; i < Globals.key_lines.Length; i++)
			Globals.key_lines[i] = new char[Defines.MAXCMDLINE];
	}

	public static int edit_line;
	public static cvar_t crosshair;
	public static vrect_t scr_vrect = new();
	public static int sys_frame_time;
	public static int chat_bufferlen = 0;
	public static int gun_frame;
	public static model_t gun_model;
	public static netadr_t net_from = new();

	// logfile
	public static StreamWriter logfile = null;
	public static float[] vec3_origin = { 0.0f, 0.0f, 0.0f };
	public static cvar_t m_filter;
	public static int vidref_val = Defines.VIDREF_GL;
	public static Random rnd = new();

	//=============================================================================

	static Globals()
	{
		Globals.Globals1();
		Globals.Globals2();
		Globals.Globals3();
	}
}
