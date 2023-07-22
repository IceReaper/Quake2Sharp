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
using Quake2Sharp.game;
using Quake2Sharp.game.types;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace IdTech.common;

/*
 * =======================================================================
 *
 * The Quake II CVAR subsystem. Implements dynamic variable handling.
 *
 * =======================================================================
 */
public static class cvar
{
	public static readonly List<cvar_t> cvar_vars = new();

	private static cvar_t badCvar = new()
	{
		name = string.Empty,
		@string = string.Empty,
		flags = 0,
		modified = false,
		value = 0,
		default_string = string.Empty
	};

	/* An ugly hack to rewrite CVARs loaded from config.cfg */
	private static Dictionary<string, string> replacements = new()
	{
		["cd_shuffle"] = "ogg_shuffle",
		["cl_anglekicks"] = "cl_kickangles",
		["cl_drawfps"] = "cl_showfps",
		["gl_drawentities"] = "r_drawentities",
		["gl_drawworld"] = "r_drawworld",
		["gl_fullbright"] = "r_fullbright",
		["gl_lerpmodels"] = "r_lerpmodels",
		["gl_lightlevel"] = "r_lightlevel",
		["gl_norefresh"] = "r_norefresh",
		["gl_novis"] = "r_novis",
		["gl_speeds"] = "r_speeds",
		["gl_clear"] = "r_clear",
		["gl_consolescale"] = "r_consolescale",
		["gl_hudscale"] = "r_hudscale",
		["gl_menuscale"] = "r_scale",
		["gl_customheight"] = "r_customheight",
		["gl_customwidth"] = "r_customheight",
		["gl_dynamic"] = "gl1_dynamic",
		["gl_farsee"] = "r_farsee",
		["gl_flashblend"] = "gl1_flashblend",
		["gl_lockpvs"] = "r_lockpvs",
		["gl_maxfps"] = "vid_maxfps",
		["gl_mode"] = "r_mode",
		["gl_modulate"] = "r_modulate",
		["gl_overbrightbits"] = "gl1_overbrightbits",
		["gl_palettedtextures"] = "gl1_palettedtextures",
		["gl_particle_min_size"] = "gl1_particle_min_size",
		["gl_particle_max_size"] = "gl1_particle_max_size",
		["gl_particle_size"] = "gl1_particle_size",
		["gl_particle_att_a"] = "gl1_particle_att_a",
		["gl_particle_att_b"] = "gl1_particle_att_b",
		["gl_particle_att_c"] = "gl1_particle_att_c",
		["gl_picmip"] = "gl1_picmip",
		["gl_pointparameters"] = "gl1_pointparameters",
		["gl_polyblend"] = "gl1_polyblend",
		["gl_round_down"] = "gl1_round_down",
		["gl_saturatelightning"] = "gl1_saturatelightning",
		["gl_stencilshadows"] = "gl1_stencilshadows",
		["gl_stereo"] = "gl1_stereo",
		["gl_stereo_separation"] = "gl1_stereo_separation",
		["gl_stereo_anaglyph_colors"] = "gl1_stereo_anaglyph_colors",
		["gl_stereo_convergence"] = "gl1_stereo_convergence",
		["gl_swapinterval"] = "r_vsync",
		["gl_texturealphamode"] = "gl1_texturealphamode",
		["gl_texturesolidmode"] = "gl1_texturesolidmode",
		["gl_ztrick"] = "gl1_ztrick",
		["gl_msaa_samples"] = "r_msaa_samples",
		["gl_nolerp_list"] = "r_nolerp_list",
		["gl_retexturing"] = "r_retexturing",
		["gl_shadows"] = "r_shadows",
		["gl_anisotropic"] = "r_anisotropic",
		["gl_lightmap"] = "r_lighmap",
		["intensity"] = "gl1_intensity"
	};

	private static bool Cvar_InfoValidate(string s)
	{
		return !s.Contains('\\') && !s.Contains('"') && !s.Contains(';');
	}

	private static cvar_t? Cvar_FindVar(string var_name)
	{
		/* An ugly hack to rewrite changed CVARs */
		if (cvar.replacements.TryGetValue(var_name, out var replacement))
		{
			clientserver.Com_Printf($"cvar {var_name} ist deprecated, use {replacement} instead\n");
			var_name = replacement;
		}

		foreach (var var in cvar.cvar_vars)
		{
			if (var.name == var_name)
				return var;
		}

		return null;
	}

	private static bool Cvar_IsFloat(string s)
	{
		return float.TryParse(s, CultureInfo.InvariantCulture, out _);
	}

	public static float Cvar_VariableValue(string var_name)
	{
		var var = cvar.Cvar_FindVar(var_name);

		return var?.value ?? 0;
	}

	public static string Cvar_VariableString(string var_name)
	{
		var var = cvar.Cvar_FindVar(var_name);

		return var == null ? string.Empty : var.@string;
	}

	/*
	 * If the variable already exists, the value will not be set
	 * The flags will be or'ed in if the variable exists.
	 */
	public static cvar_t Cvar_Get(string var_name, string var_value, int flags)
	{
		if ((flags & (Defines.CVAR_USERINFO | Defines.CVAR_SERVERINFO)) != 0)
		{
			if (!cvar.Cvar_InfoValidate(var_name))
			{
				clientserver.Com_Printf("invalid info cvar name\n");
				return cvar.badCvar;
			}
		}

		var var = cvar.Cvar_FindVar(var_name);

		if (var != null)
		{
			var.flags |= flags;
			var.default_string = var_value;
			return var;
		}

		if ((flags & (Defines.CVAR_USERINFO | Defines.CVAR_SERVERINFO)) != 0)
		{
			if (!cvar.Cvar_InfoValidate(var_value))
			{
				clientserver.Com_Printf("invalid info cvar value\n");
				return cvar.badCvar;
			}
		}

		// if $game is the default one ("baseq2"), then use "" instead because
		// other code assumes this behavior (e.g. FS_BuildGameSpecificSearchPath())
		if (var_name == "game" && var_value == Globals.BASEDIRNAME)
			var_value = string.Empty;

		var = new cvar_t
		{
			name = var_name,
			@string = var_value,
			default_string = var_value,
			modified = true,
			flags = flags,
			value = float.TryParse(var_value, CultureInfo.InvariantCulture, out var floatValue) ? floatValue : 0
		};

		/* link the variable in */
		cvar.cvar_vars.Add(var);

		return var;
	}

	private static cvar_t Cvar_Set2(string var_name, string value, bool force)
	{
		var var = cvar.Cvar_FindVar(var_name);

		if (var == null)
			return cvar.Cvar_Get(var_name, value, 0) ?? throw new UnreachableException();

		if ((var.flags & (Defines.CVAR_USERINFO | Defines.CVAR_SERVERINFO)) != 0)
		{
			if (!cvar.Cvar_InfoValidate(value))
			{
				clientserver.Com_Printf("invalid info cvar value\n");
				return var;
			}
		}

		// if $game is the default one ("baseq2"), then use "" instead because
		// other code assumes this behavior (e.g. FS_BuildGameSpecificSearchPath())
		if (var_name == "game" && value == Globals.BASEDIRNAME)
			value = string.Empty;

		if (!force)
		{
			if ((var.flags & Defines.CVAR_NOSET) != 0)
			{
				clientserver.Com_Printf($"{var_name} is write protected.\n");
				return var;
			}

			if ((var.flags & Defines.CVAR_LATCH) != 0)
			{
				if (var.latched_string != null)
				{
					if (value == var.latched_string)
						return var;

					var.latched_string = null;
				}
				else if (value == var.@string)
					return var;

				if (clientserver.Com_ServerState() != 0)
				{
					clientserver.Com_Printf($"{var_name} will be changed for next game.\n");
					var.latched_string = value;
				}
				else
				{
					var.@string = value;
					var.value = float.TryParse(value, CultureInfo.InvariantCulture, out var floatValue) ? floatValue : 0;

					if (var.name == "game")
						filesystem.FS_BuildGameSpecificSearchPath(var.@string);
				}

				return var;
			}
		}

		var.latched_string = null;

		if (value == var.@string)
			return var;

		var.modified = true;

		if ((var.flags & Defines.CVAR_USERINFO) != 0)
			cvar.userinfo_modified = true;

		var.@string = value;
		var.value = float.TryParse(value, CultureInfo.InvariantCulture, out var fValue) ? fValue : 0;

		return var;
	}

	public static cvar_t Cvar_ForceSet(string var_name, string value)
	{
		return cvar.Cvar_Set2(var_name, value, true);
	}

	public static cvar_t Cvar_Set(string var_name, string value)
	{
		return cvar.Cvar_Set2(var_name, value, false);
	}

	public static cvar_t? Cvar_FullSet(string var_name, string value, int flags)
	{
		var var = cvar.Cvar_FindVar(var_name);

		if (var == null)
			return cvar.Cvar_Get(var_name, value, flags);

		var.modified = true;

		if ((var.flags & Defines.CVAR_USERINFO) != 0)
			cvar.userinfo_modified = true;

		// if $game is the default one ("baseq2"), then use "" instead because
		// other code assumes this behavior (e.g. FS_BuildGameSpecificSearchPath())
		if (var_name == "game" && value == Globals.BASEDIRNAME)
			value = string.Empty;

		var.@string = value;
		var.value = float.TryParse(value, CultureInfo.InvariantCulture, out var floatValue) ? floatValue : 0;
		var.flags = flags;

		return var;
	}

	public static cvar_t Cvar_SetValue(string var_name, float value)
	{
		return cvar.Cvar_Set(var_name, value.ToString(CultureInfo.InvariantCulture));
	}

	/*
	 * Any variables with latched values will now be updated
	 */
	public static void Cvar_GetLatchedVars()
	{
		foreach (var var in cvar.cvar_vars)
		{
			if (var.latched_string == null)
				continue;

			var.@string = var.latched_string;
			var.value = float.TryParse(var.latched_string, CultureInfo.InvariantCulture, out var floatValue) ? floatValue : 0;
			var.latched_string = null;

			if (var.name == "game")
				filesystem.FS_BuildGameSpecificSearchPath(var.@string);
		}
	}

	/*
	 * Handles variable inspection and changing from the console
	 */
	public static bool Cvar_Command()
	{
		/* check variables */
		var v = cvar.Cvar_FindVar(cmdparser.Cmd_Argv(0));

		if (v == null)
			return false;

		/* perform a variable print or set */
		if (cmdparser.Cmd_Argc() == 1)
		{
			clientserver.Com_Printf($"\"{v.name}\" is \"{v.@string}\"\n");
			return true;
		}

		/* Another evil hack: The user has just changed 'game' trough
		   the console. We reset userGivenGame to that value, otherwise
		   we would revert to the initialy given game at disconnect. */
		if (v.name == "game")
			frame.userGivenGame = cmdparser.Cmd_Argv(1);

		cvar.Cvar_Set(v.name, cmdparser.Cmd_Argv(1));
		return true;
	}

	/*
	 * Allows setting and defining of arbitrary cvars from console
	 */
	private static void Cvar_Set_f()
	{
		if (cmdparser.Cmd_Argc() != 3 && cmdparser.Cmd_Argc() != 4)
		{
			clientserver.Com_Printf("usage: set <variable> <value> [u / s]\n");
			return;
		}

		var firstarg = cmdparser.Cmd_Argv(1);

		/* An ugly hack to rewrite changed CVARs */
		if (cvar.replacements.TryGetValue(firstarg, out var replacement))
			firstarg = replacement;

		if (cmdparser.Cmd_Argc() == 4)
		{
			int flags;

			if (cmdparser.Cmd_Argv(3) == "u")
				flags = Defines.CVAR_USERINFO;
			else if (cmdparser.Cmd_Argv(3) == "s")
				flags = Defines.CVAR_SERVERINFO;
			else
			{
				clientserver.Com_Printf("flags can only be 'u' or 's'\n");
				return;
			}

			cvar.Cvar_FullSet(firstarg, cmdparser.Cmd_Argv(2), flags);
		}
		else
			cvar.Cvar_Set(firstarg, cmdparser.Cmd_Argv(2));
	}

	/*
	 * Appends lines containing "set variable value" for all variables
	 * with the archive flag set to true.
	 */
	public static void Cvar_WriteVariables(string path)
	{
		var stream = File.Open(path, FileMode.Append, FileAccess.Write);
		var writer = new BinaryWriter(stream);

		foreach (var variable in cvar.cvar_vars)
		{
			if ((variable.flags & Defines.CVAR_ARCHIVE) == 0)
				continue;

			writer.Write(Encoding.UTF8.GetBytes($"set {variable.name} \"{variable.@string}\"\n"));
		}

		stream.Dispose();
	}

	private static void Cvar_List_f()
	{
		foreach (var variable in cvar.cvar_vars)
		{
			clientserver.Com_Printf((variable.flags & Defines.CVAR_ARCHIVE) != 0 ? "*" : " ");
			clientserver.Com_Printf((variable.flags & Defines.CVAR_USERINFO) != 0 ? "U" : " ");
			clientserver.Com_Printf((variable.flags & Defines.CVAR_SERVERINFO) != 0 ? "S" : " ");
			clientserver.Com_Printf((variable.flags & Defines.CVAR_NOSET) != 0 ? "-" : (variable.flags & Defines.CVAR_LATCH) != 0 ? "L" : " ");
			clientserver.Com_Printf($" {variable.name} \"{variable.@string}\"\n");
		}

		clientserver.Com_Printf($"{cvar.cvar_vars.Count} cvars\n");
	}

	private static bool userinfo_modified;

	private static string Cvar_BitInfo(int bit)
	{
		var info = string.Empty;

		foreach (var var in cvar.cvar_vars)
		{
			if ((var.flags & bit) != 0)
				info = Info.Info_SetValueForKey(info, var.name, var.@string);
		}

		return info;
	}

	/*
	 * returns an info string containing
	 * all the CVAR_USERINFO cvars
	 */
	public static string Cvar_Userinfo()
	{
		return cvar.Cvar_BitInfo(Defines.CVAR_USERINFO);
	}

	/*
	 * returns an info string containing
	 * all the CVAR_SERVERINFO cvars
	 */
	public static string Cvar_Serverinfo()
	{
		return cvar.Cvar_BitInfo(Defines.CVAR_SERVERINFO);
	}

	/*
	 * Increments the given cvar by 1 or adds the
	 * optional given float value to it.
	 */
	private static void Cvar_Inc_f()
	{
		if (cmdparser.Cmd_Argc() < 2)
		{
			clientserver.Com_Printf($"Usage: {cmdparser.Cmd_Argv(0)} <cvar> [value]\n");
			return;
		}

		var variable = cvar.Cvar_FindVar(cmdparser.Cmd_Argv(1));

		if (variable == null)
		{
			clientserver.Com_Printf($"{cmdparser.Cmd_Argv(1)} is not a cvar\n");
			return;
		}

		if (!cvar.Cvar_IsFloat(variable.@string))
		{
			clientserver.Com_Printf($"\"{variable.name}\" is \"{variable.@string}\", can't {cmdparser.Cmd_Argv(0)}\n");
			return;
		}

		var value = 1f;

		if (cmdparser.Cmd_Argc() > 2 && float.TryParse(cmdparser.Cmd_Argv(2), CultureInfo.InvariantCulture, out var parsed))
			value = parsed;

		if (cmdparser.Cmd_Argv(0) == "dec")
			value = -value;

		cvar.Cvar_Set(variable.name, (variable.value + value).ToString(CultureInfo.InvariantCulture));
	}

	/*
	 * Resets a cvar to its default value.
	 */
	private static void Cvar_Reset_f()
	{
		if (cmdparser.Cmd_Argc() < 2)
		{
			clientserver.Com_Printf($"Usage: {cmdparser.Cmd_Argv(0)} <cvar>\n");
			return;
		}

		var variable = cvar.Cvar_FindVar(cmdparser.Cmd_Argv(1));

		if (variable == null)
		{
			clientserver.Com_Printf($"{cmdparser.Cmd_Argv(1)} is not a cvar\n");
			return;
		}

		clientserver.Com_Printf($"{variable.name}: {variable.default_string}\n");
		cvar.Cvar_Set(variable.name, variable.default_string);
	}

	/*
	 * Resets all known cvar (with the exception of `game') to
	 * their default values.
	 */
	private static void Cvar_ResetAll_f()
	{
		foreach (var variable in cvar.cvar_vars)
		{
			if ((variable.flags & Defines.CVAR_NOSET) != 0)
				continue;

			if (variable.name == "game")
				continue;

			cvar.Cvar_Set(variable.name, variable.default_string);
		}
	}

	/*
	 * Toggles a cvar between 0 and 1 or the given values.
	 */
	private static void Cvar_Toggle_f()
	{
		if (cmdparser.Cmd_Argc() < 2)
		{
			clientserver.Com_Printf($"Usage: {cmdparser.Cmd_Argv(0)} <cvar> [values]\n");
			return;
		}

		var var = cvar.Cvar_FindVar(cmdparser.Cmd_Argv(1));

		if (var == null)
		{
			clientserver.Com_Printf($"{cmdparser.Cmd_Argv(1)} is not a cvar\n");
			return;
		}

		if (cmdparser.Cmd_Argc() < 3)
		{
			switch (var.@string)
			{
				case "0":
					cvar.Cvar_Set(var.name, "1");
					break;

				case "1":
					cvar.Cvar_Set(var.name, "0");
					break;

				default:
					clientserver.Com_Printf($"\"{var.name}\" is \"{var.@string}\", can't toggle\n");
					break;
			}

			return;
		}

		for (var i = 0; i < cmdparser.Cmd_Argc() - 2; i++)
		{
			if (var.@string != cmdparser.Cmd_Argv(2 + i))
				continue;

			cvar.Cvar_Set(var.name, cmdparser.Cmd_Argv(2 + (i + 1) % (cmdparser.Cmd_Argc() - 2)));
			return;
		}

		clientserver.Com_Printf($"\"{var.name}\" is \"{var.@string}\", can't cycle\n");
	}

	/*
	 * Reads in all archived cvars
	 */
	public static void Cvar_Init()
	{
		cmdparser.Cmd_AddCommand("cvarlist", cvar.Cvar_List_f);
		cmdparser.Cmd_AddCommand("dec", cvar.Cvar_Inc_f);
		cmdparser.Cmd_AddCommand("inc", cvar.Cvar_Inc_f);
		cmdparser.Cmd_AddCommand("reset", cvar.Cvar_Reset_f);
		cmdparser.Cmd_AddCommand("resetall", cvar.Cvar_ResetAll_f);
		cmdparser.Cmd_AddCommand("set", cvar.Cvar_Set_f);
		cmdparser.Cmd_AddCommand("toggle", cvar.Cvar_Toggle_f);
	}

	/*
	 * Free list of cvars
	 */
	public static void Cvar_Fini()
	{
		cvar.cvar_vars.Clear();

		cmdparser.Cmd_RemoveCommand("cvarlist");
		cmdparser.Cmd_RemoveCommand("dec");
		cmdparser.Cmd_RemoveCommand("inc");
		cmdparser.Cmd_RemoveCommand("reset");
		cmdparser.Cmd_RemoveCommand("resetall");
		cmdparser.Cmd_RemoveCommand("set");
		cmdparser.Cmd_RemoveCommand("toggle");
	}
}
