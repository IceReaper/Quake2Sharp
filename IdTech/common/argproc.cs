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

using Quake2Sharp.qcommon;

namespace IdTech.common;

/* =======================================================================
 *
 * Common argument processing
 *
 * =======================================================================
 */
public static class argproc
{
	private static readonly List<string> com_argv = new();

	/*
	 * Returns the position (1 to argc-1) in the program's argument list
	 * where the given parameter appears, or 0 if not present
	 */
	public static int COM_CheckParm(string parm)
	{
		for (var i = 1; i < com_argv.Count; i++)
		{
			if (parm == com_argv[i])
				return i;
		}

		return 0;
	}

	public static int COM_Argc()
	{
		return com_argv.Count;
	}

	public static string COM_Argv(int arg)
	{
		if (arg < 0 || arg >= com_argv.Count)
			return string.Empty;

		return com_argv[arg];
	}

	public static void COM_ClearArgv(int arg)
	{
		if (arg < 0 || arg >= com_argv.Count || com_argv[arg].Length == 0)
			return;

		com_argv.RemoveAt(arg);
	}

	public static void COM_InitArgv(string[] argv)
	{
		com_argv.Clear();
		com_argv.AddRange(argv);
	}

	/*
	 * Adds the given string at the end of the current argument list
	 */
	public static void COM_AddParm(string parm)
	{
		com_argv.Add(parm);
	}

	public static void Info_Print(string s)
	{
		var i = 0;

		if (s.StartsWith('\\'))
			i++;

		while (i < s.Length)
		{
			var j = i;

			while (j < s.Length && s[j] != '\\')
				j++;

			var key = s.Substring(i, j - i).PadRight(20, ' ');
			i = j;

			if (i < s.Length)
				i++;

			clientserver.Com_Printf(key);

			if (i == s.Length)
			{
				clientserver.Com_Printf("MISSING VALUE\n");
				return;
			}

			j = i;

			while (j < s.Length && s[j] != '\\')
				j++;

			var value = s.Substring(i, j - i);
			i = j;

			if (i < s.Length)
				i++;

			clientserver.Com_Printf($"{value}\n");
		}
	}
}
