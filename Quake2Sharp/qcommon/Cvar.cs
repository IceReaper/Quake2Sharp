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
namespace Quake2Sharp.qcommon;

using game;
using game.types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using util;

/**
 * Cvar implements console variables. The original code is located in cvar.c
 */
public class Cvar
{
	/**
     * @param var_name
     * @param var_value
     * @param flags
     * @return
     */
	public static cvar_t Get(string var_name, string var_value, int flags)
	{
		cvar_t var;

		if ((flags & (Defines.CVAR_USERINFO | Defines.CVAR_SERVERINFO)) != 0)
		{
			if (!Cvar.InfoValidate(var_name))
			{
				Com.Printf("invalid info cvar name\n");

				return null;
			}
		}

		var = Cvar.FindVar(var_name);

		if (var != null)
		{
			var.flags |= flags;

			return var;
		}

		if (var_value == null)
			return null;

		if ((flags & (Defines.CVAR_USERINFO | Defines.CVAR_SERVERINFO)) != 0)
		{
			if (!Cvar.InfoValidate(var_value))
			{
				Com.Printf("invalid info cvar value\n");

				return null;
			}
		}

		var = new();
		var.name = new(var_name);
		var.@string = new(var_value);
		var.modified = true;
		var.value = Lib.atof(var.@string);

		// link the variable in
		var.next = Globals.cvar_vars;
		Globals.cvar_vars = var;

		var.flags = flags;

		return var;
	}

	public static void Init()
	{
		Cmd.AddCommand("set", Cvar.Set_f);
		Cmd.AddCommand("cvarlist", Cvar.List_f);
	}

	public static string VariableString(string var_name)
	{
		cvar_t var;
		var = Cvar.FindVar(var_name);

		return var == null ? "" : var.@string;
	}

	private static cvar_t FindVar(string var_name)
	{
		cvar_t var;

		for (var = Globals.cvar_vars; var != null; var = var.next)
		{
			if (var_name.Equals(var.name))
				return var;
		}

		return null;
	}

	/**
     * Creates a variable if not found and sets their value, the parsed float value and their flags.
     */
	public static cvar_t FullSet(string var_name, string value, int flags)
	{
		cvar_t var;

		var = Cvar.FindVar(var_name);

		if (null == var)
		{
			// create it
			return Cvar.Get(var_name, value, flags);
		}

		var.modified = true;

		if ((var.flags & Defines.CVAR_USERINFO) != 0)
			Globals.userinfo_modified = true; // transmit at next oportunity

		var.@string = value;
		var.value = Lib.atof(var.@string);
		var.flags = flags;

		return var;
	}

	/** 
     * Sets the value of the variable without forcing. 
     */
	public static cvar_t Set(string var_name, string value)
	{
		return Cvar.Set2(var_name, value, false);
	}

	/** 
     * Sets the value of the variable with forcing. 
     */
	public static cvar_t ForceSet(string var_name, string value)
	{
		return Cvar.Set2(var_name, value, true);
	}

	/**
     * Gereric set function, sets the value of the variable, with forcing its even possible to 
     * override the variables write protection. 
     */
	private static cvar_t Set2(string var_name, string value, bool force)
	{
		var var = Cvar.FindVar(var_name);

		if (var == null)
		{
			// create it
			return Cvar.Get(var_name, value, 0);
		}

		if ((var.flags & (Defines.CVAR_USERINFO | Defines.CVAR_SERVERINFO)) != 0)
		{
			if (!Cvar.InfoValidate(value))
			{
				Com.Printf("invalid info cvar value\n");

				return var;
			}
		}

		if (!force)
		{
			if ((var.flags & Defines.CVAR_NOSET) != 0)
			{
				Com.Printf(var_name + " is write protected.\n");

				return var;
			}

			if ((var.flags & Defines.CVAR_LATCH) != 0)
			{
				if (var.latched_string != null)
				{
					if (value.Equals(var.latched_string))
						return var;

					var.latched_string = null;
				}
				else
				{
					if (value.Equals(var.@string))
						return var;
				}

				if (Globals.server_state != 0)
				{
					Com.Printf(var_name + " will be changed for next game.\n");
					var.latched_string = value;
				}
				else
				{
					var.@string = value;
					var.value = Lib.atof(var.@string);

					if (var.name.Equals("game"))
					{
						FS.SetGamedir(var.@string);
						FS.ExecAutoexec();
					}
				}

				return var;
			}
		}
		else
		{
			if (var.latched_string != null)
				var.latched_string = null;
		}

		if (value.Equals(var.@string))
			return var; // not changed

		var.modified = true;

		if ((var.flags & Defines.CVAR_USERINFO) != 0)
			Globals.userinfo_modified = true; // transmit at next oportunity

		var.@string = value;

		try
		{
			var.value = float.Parse(var.@string, CultureInfo.InvariantCulture);
		}
		catch (Exception)
		{
			var.value = 0.0f;
		}

		return var;
	}

	/** 
     * Set command, sets variables.
     */
	private static readonly Action Set_f = () =>
	{
		int c;
		int flags;

		c = Cmd.Argc();

		if (c != 3 && c != 4)
		{
			Com.Printf("usage: set <variable> <value> [u / s]\n");

			return;
		}

		if (c == 4)
		{
			if (Cmd.Argv(3).Equals("u"))
				flags = Defines.CVAR_USERINFO;
			else if (Cmd.Argv(3).Equals("s"))
				flags = Defines.CVAR_SERVERINFO;
			else
			{
				Com.Printf("flags can only be 'u' or 's'\n");

				return;
			}

			Cvar.FullSet(Cmd.Argv(1), Cmd.Argv(2), flags);
		}
		else
			Cvar.Set(Cmd.Argv(1), Cmd.Argv(2));
	};

	/**
     * List command, lists all available commands.
     */
	private static readonly Action List_f = () =>
	{
		cvar_t var;
		int i;

		i = 0;

		for (var = Globals.cvar_vars; var != null; var = var.next, i++)
		{
			if ((var.flags & Defines.CVAR_ARCHIVE) != 0)
				Com.Printf("*");
			else
				Com.Printf(" ");

			if ((var.flags & Defines.CVAR_USERINFO) != 0)
				Com.Printf("U");
			else
				Com.Printf(" ");

			if ((var.flags & Defines.CVAR_SERVERINFO) != 0)
				Com.Printf("S");
			else
				Com.Printf(" ");

			if ((var.flags & Defines.CVAR_NOSET) != 0)
				Com.Printf("-");
			else if ((var.flags & Defines.CVAR_LATCH) != 0)
				Com.Printf("L");
			else
				Com.Printf(" ");

			Com.Printf(" " + var.name + " \"" + var.@string + "\"\n");
		}

		Com.Printf(i + " cvars\n");
	};

	/** 
     * Sets a float value of a variable.
     * 
     * The overloading is very important, there was a problem with 
     * networt "rate" string --> 10000 became "10000.0" and that wasn't right.
     */
	public static void SetValue(string var_name, int value)
	{
		Cvar.Set(var_name, "" + value);
	}

	public static void SetValue(string var_name, float value)
	{
		if (value == (int)value)
			Cvar.Set(var_name, "" + (int)value);
		else
			Cvar.Set(var_name, "" + value.ToString(CultureInfo.InvariantCulture));
	}

	/**
     * Returns the float value of a variable.
     */
	public static float VariableValue(string var_name)
	{
		var var = Cvar.FindVar(var_name);

		if (var == null)
			return 0;

		return Lib.atof(var.@string);
	}

	/**
     * Handles variable inspection and changing from the console.
     */
	public static bool Command()
	{
		cvar_t v;

		// check variables
		v = Cvar.FindVar(Cmd.Argv(0));

		if (v == null)
			return false;

		// perform a variable print or set
		if (Cmd.Argc() == 1)
		{
			Com.Printf("\"" + v.name + "\" is \"" + v.@string + "\"\n");

			return true;
		}

		Cvar.Set(v.name, Cmd.Argv(1));

		return true;
	}

	public static string BitInfo(int bit)
	{
		string info;
		cvar_t var;

		info = "";

		for (var = Globals.cvar_vars; var != null; var = var.next)
		{
			if ((var.flags & bit) != 0)
				info = Info.Info_SetValueForKey(info, var.name, var.@string);
		}

		return info;
	}

	/**
     * Returns an info string containing all the CVAR_SERVERINFO cvars. 
     */
	public static string Serverinfo()
	{
		return Cvar.BitInfo(Defines.CVAR_SERVERINFO);
	}

	/**
     * Any variables with latched values will be updated.
     */
	public static void GetLatchedVars()
	{
		cvar_t var;

		for (var = Globals.cvar_vars; var != null; var = var.next)
		{
			if (var.latched_string == null || var.latched_string.Length == 0)
				continue;

			var.@string = var.latched_string;
			var.latched_string = null;
			var.value = Lib.atof(var.@string);

			if (var.name.Equals("game"))
			{
				FS.SetGamedir(var.@string);
				FS.ExecAutoexec();
			}
		}
	}

	/**
     * Returns an info string containing all the CVAR_USERINFO cvars.
     */
	public static string Userinfo()
	{
		return Cvar.BitInfo(Defines.CVAR_USERINFO);
	}

	/**
     * Appends lines containing \"set vaqriable value\" for all variables
     * with the archive flag set true. 
     */
	public static void WriteVariables(string path)
	{
		cvar_t var;
		StreamWriter f;
		string buffer;

		f = File.AppendText(path);

		if (f == null)
			return;

		for (var = Globals.cvar_vars; var != null; var = var.next)
		{
			if ((var.flags & Defines.CVAR_ARCHIVE) != 0)
			{
				buffer = "set " + var.name + " \"" + var.@string + "\"\n";

				try
				{
					f.Write(buffer);
				}
				catch (Exception)
				{
				}
			}
		}

		f.Close();
	}

	/**
     * Variable typing auto completition.
     */
	public static List<string> CompleteVariable(string partial)
	{
		List<string> vars = new();

		// check match
		for (var cvar = Globals.cvar_vars; cvar != null; cvar = cvar.next)
		{
			if (cvar.name.StartsWith(partial))
				vars.Add(cvar.name);
		}

		return vars;
	}

	/**
     * Some characters are invalid for info strings.
     */
	private static bool InfoValidate(string s)
	{
		if (s.IndexOf("\\") != -1)
			return false;

		if (s.IndexOf("\"") != -1)
			return false;

		if (s.IndexOf(";") != -1)
			return false;

		return true;
	}
}