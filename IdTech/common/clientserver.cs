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
using Quake2Sharp.client;
using Quake2Sharp.game.types;
using Quake2Sharp.qcommon;
using Quake2Sharp.server;
using Quake2Sharp.sys;
using System.Text;

namespace IdTech.common;

/*
 * =======================================================================
 *
 * Client / Server interactions
 *
 * =======================================================================
 */
public static class clientserver
{
	private static Stream? logfile;
	public static readonly cvar_t logfile_active = cvar.Cvar_Get("logfile", "1", Defines.CVAR_ARCHIVE);

	private static int server_state;
	public static readonly cvar_t color_terminal = cvar.Cvar_Get("colorterminal", "1", Defines.CVAR_ARCHIVE);

	private static int rd_target;
	private static string rd_buffer = string.Empty;
	private static Action<int, string>? rd_flush;

	public static void Com_BeginRedirect(int target, Action<int, string> flush)
	{
		rd_target = target;
		rd_buffer = string.Empty;
		rd_flush = flush;
	}

	public static void Com_EndRedirect()
	{
		rd_flush?.Invoke(rd_target, rd_buffer);

		rd_target = 0;
		rd_buffer = string.Empty;
		rd_flush = null;
	}

	/*
	 * Both client and server can use this, and it will output
	 * to the appropriate place.
	 */
	private static void Com_VPrintf(int print_level, string msg)
	{
		if (print_level == Defines.PRINT_DEVELOPER && (frame.developer == null || frame.developer.value == 0))
			return; /* don't confuse non-developers with techie stuff... */

		if (rd_flush != null)
		{
			rd_buffer += msg;
			return;
		}

		if (!Globals.DEDICATED_ONLY)
			QConsole.Print(msg);

		var chars = msg.ToCharArray();

		// remove unprintable characters
		for (var i = 0; i < chars.Length; i++)
		{
			switch (chars[i])
			{
				// skip color marker
				case (char)0x01 or (char)0x02:
				{
					if (color_terminal.value == 0)
						chars[i] = ' ';
					break;
				}

				case '\t' or '\n' or '\v' or '\f' or '\r':
					break;

				// no idea if the following two are ever sent here, but in conchars.pcx they look like this
				// so do the replacements.. won't hurt I guess..
				case (char)0x10:
					chars[i] = '[';
					break;

				case (char)0x11:
					chars[i] = ']';
					break;

				// horizontal line chars
				case (char)0x1D or (char)0x1F:
					chars[i] = '-';
					break;

				case (char)0x1E:
					chars[i] = '=';
					break;

				// just replace all other unprintable chars with space, should be good enough
				case < ' ':
					chars[i] = ' ';
					break;
			}
		}

		msg = new string(chars);

		/* also echo to debugging console */
		Sys.ConsoleOutput(msg);

		/* logfile */
		if (logfile_active.value == 0)
			return;

		// remove color marker
		if (msg.Length > 0 && (msg[0] == 0x01 || msg[0] == 0x02))
			msg = ' ' + msg[1..];

		logfile ??= File.Open($"{filesystem.FS_Gamedir()}/qconsole.log", logfile_active.value > 2 ? FileMode.Append : FileMode.Create, FileAccess.Write);
		logfile.Write(Encoding.UTF8.GetBytes(msg));

		/* force it to save every time */
		if (logfile_active.value > 0)
			logfile.Flush();
	}

	/*
	 * Both client and server can use this, and it will output
	 * to the appropriate place.
	 */
	public static void Com_Printf(string msg)
	{
		Com_VPrintf(Defines.PRINT_ALL, msg);
	}

	/*
	 * A Com_Printf that only shows up if the "developer" cvar is set
	 */
	public static void Com_DPrintf(string msg)
	{
		Com_VPrintf(Defines.PRINT_DEVELOPER, msg);
	}

	/*
	 * A Com_Printf that only shows up when either the "modder" or "developer"
	 * cvars is set
	 */
	private static void Com_MDPrintf(string msg)
	{
		if (frame.modder.value != 0 || frame.developer.value != 0)
			Com_Printf(msg);
	}

	/*
	 * Both client and server can use this, and it will
	 * do the appropriate things.
	 */
	private static bool recursive;

	public static void Com_Error(int code, string msg)
	{
		if (recursive)
			Sys.Error($"recursive error after: {msg}");

		recursive = true;

		if (code == Defines.ERR_DISCONNECT)
		{
			if (!Globals.DEDICATED_ONLY)
				Cl.Drop();

			recursive = false;
			throw new Exception();
		}

		if (code == Defines.ERR_DROP)
		{
			Com_Printf($"********************\nERROR: {msg}\n********************\n");
			SV_MAIN.SV_Shutdown($"Server crashed: {msg}\n", false);

			if (!Globals.DEDICATED_ONLY)
				Cl.Drop();

			recursive = false;
			throw new Exception();
		}

		SV_MAIN.SV_Shutdown($"Server fatal crashed: {msg}\n", false);

		if (!Globals.DEDICATED_ONLY)
			Cl.Shutdown();

		if (logfile != null)
		{
			logfile.Dispose();
			logfile = null;
		}

		Sys.Error(msg);
		recursive = false;
	}

	/*
	 * Both client and server can use this, and it will
	 * do the appropriate things.
	 */
	public static void Com_Quit()
	{
		Com_Printf("\n----------- shutting down ----------\n");
		SV_MAIN.SV_Shutdown("Server quit\n", false);
		Sys.Quit();
	}

	public static int Com_ServerState()
	{
		return server_state;
	}

	public static void Com_SetServerState(int state)
	{
		server_state = state;
	}
}
