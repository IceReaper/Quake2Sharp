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

using IdTech.common;
using Quake2Sharp;
using Quake2Sharp.client;
using Quake2Sharp.game.types;
using System.Diagnostics;
using System.Reflection;

namespace IdTech.backend;

/*
 * =======================================================================
 *
 * This file implements all system dependent generic functions.
 *
 * =======================================================================
 */
public static class system
{
	// Game library handle
	private static Assembly? gameLibrary;

	// Config dir
	public static string cfgdir = Globals.CFGDIR;

	// Buffer for the dedicated server console
	private static string consoleText = string.Empty;

	/* ================================================================ */

	public static void Sys_Error(string error)
	{
		if (!main.DEDICATED_ONLY)
			Cl.Shutdown();

		frame.Qcommon_Shutdown();

		Console.Error.WriteLine($"Error: {error}");
		Console.WriteLine($"Error: {error}");

		Environment.Exit(0);
	}

	public static void Sys_Quit()
	{
		if (!main.DEDICATED_ONLY)
			Cl.Shutdown();

		frame.Qcommon_Shutdown();

		Console.WriteLine("------------------------------------");

		Environment.Exit(0);
	}

	/* ================================================================ */

	public static string? Sys_ConsoleInput()
	{
		if (frame.dedicated.value == 0)
			return null;

		while (true)
		{
			if (!Console.KeyAvailable)
				break;

			var ch = Console.ReadKey(true).KeyChar;

			switch (ch)
			{
				case '\n':

					Console.Write("\r\n");

					if (system.consoleText.Length > 0)
					{
						var dummy = system.consoleText;
						system.consoleText = string.Empty;
						return dummy;
					}

					break;

				case '\b':

					if (system.consoleText.Length > 0)
					{
						system.consoleText = system.consoleText[..^1];
						Console.Write("\b \b");
					}

					break;

				default:

					if (ch >= ' ')
					{
						Console.Write(ch);
						system.consoleText += ch;
					}

					break;
			}
		}

		return null;
	}

	public static void Sys_ConsoleOutput(string @string)
	{
		if (@string[0] == 0x01 || @string[0] == 0x02)
		{
			// remove color marker
			@string = @string[1..];
		}

		if (frame.dedicated.value == 0)
			Console.Write(@string);
		else
		{
			for (var i = 0; i < system.consoleText.Length; i++)
				Console.Write('\b');

			Console.Write(@string);

			Console.Write(system.consoleText);
		}
	}

	/* ================================================================ */

	private static Stopwatch? stopwatch;

	public static int Sys_Milliseconds()
	{
		system.stopwatch ??= Stopwatch.StartNew();

		return (int)system.stopwatch.Elapsed.TotalMilliseconds;
	}

	/* ================================================================ */

	// File searching
	private static readonly List<string> FindResults = new();

	public static string? Sys_FindFirst(string path)
	{
		if (system.FindResults.Count > 0)
			system.Sys_Error("Sys_BeginFind without close");

		if (Directory.Exists(Path.GetDirectoryName(path)))
			system.FindResults.AddRange(Directory.GetFiles(Path.GetDirectoryName(path) ?? ".", Path.GetFileName(path), SearchOption.AllDirectories));

		return system.Sys_FindNext();
	}

	public static string? Sys_FindNext()
	{
		if (system.FindResults.Count == 0)
			return null;

		var findpath = system.FindResults[0];
		system.FindResults.RemoveAt(0);
		return findpath;
	}

	public static void Sys_FindClose()
	{
		system.FindResults.Clear();
	}

	/* ================================================================ */

	private static void Sys_UnloadGame()
	{
		system.gameLibrary = null;
	}

	public static game_export_t? Sys_GetGameAPI(game_import_t parms)
	{
		if (system.gameLibrary != null)
			clientserver.Com_Error(Defines.ERR_FATAL, "Sys_GetGameAPI without Sys_UnloadingGame");

		/* now run through the search paths */
		string? path = null;

		while (true)
		{
			path = filesystem.FS_NextPath(path);

			if (path == null)
				return null; /* couldn't find one anywhere */

			/* Try game.dll */
			var name = $"{path}/game.dll";

			try
			{
				system.gameLibrary = Assembly.LoadFrom(name);
			}
			catch
			{
				// ignored
			}

			if (system.gameLibrary != null)
			{
				clientserver.Com_DPrintf($"Loading library: {name}\n");
				break;
			}
		}

		var gameApiType = system.gameLibrary.GetTypes().FirstOrDefault(t => t.IsClass && t.IsAssignableTo(typeof(IGameApi)));
		var gameApi = gameApiType == null ? null : Activator.CreateInstance(gameApiType) as IGameApi;

		if (gameApi == null)
		{
			system.Sys_UnloadGame();
			return null;
		}

		return gameApi.GetGameAPI(parms);
	}

	/* ======================================================================= */

	private static void Sys_Mkdir(string path)
	{
		if (!system.Sys_IsDir(path) && !Directory.CreateDirectory(path).Exists)
			clientserver.Com_Error(Defines.ERR_FATAL, $"Couldn't create dir {path}\n");
	}

	private static bool Sys_IsDir(string path)
	{
		return Directory.Exists(path);
	}

	public static bool Sys_IsFile(string path)
	{
		return File.Exists(path);
	}

	public static string Sys_GetHomeDir()
	{
		var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Replace('\\', '/');
		var gdir = $"{profile}/{system.cfgdir}/";

		system.Sys_Mkdir(gdir);

		return gdir;
	}

	public static void Sys_Remove(string path)
	{
		File.Delete(path);
	}

	public static void Sys_Rename(string from, string to)
	{
		File.Move(from, to);
	}

	public static void Sys_RemoveDir(string path)
	{
		Directory.Delete(path, true);
	}

	public static string Sys_Realpath(string @in)
	{
		return Path.GetFullPath(@in);
	}

	/* ======================================================================= */

	public static string Sys_GetWorkDir()
	{
		return Directory.GetCurrentDirectory();
	}

	public static void Sys_SetWorkDir(string path)
	{
		Directory.SetCurrentDirectory(path);
	}
}
