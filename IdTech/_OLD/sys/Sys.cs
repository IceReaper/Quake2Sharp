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

using IdTech.common;

namespace Quake2Sharp.sys;

using client;

/**
 * Sys
 */
public class Sys
{
	public static void Error(string error)
	{
		Cl.Shutdown();

		//StackTrace();
		Console.WriteLine(new Exception(error));
		Environment.Exit(0);
	}

	public static void Quit()
	{
		Cl.Shutdown();
		Environment.Exit(0);
	}

	//ok!
	public static string[] FindAll(string path, int musthave, int canthave)
	{
		var index = path.LastIndexOf('/');

		if (index != -1)
		{
			Sys.findbase = path[..index];
			Sys.findpattern = path[(index + 1)..];
		}
		else
		{
			Sys.findbase = path;
			Sys.findpattern = "*";
		}

		if (Sys.findpattern.Equals("*.*"))
			Sys.findpattern = "*";

		if (!Directory.Exists(Sys.findbase))
			return null;

		var filter = (canthave & Defines.SFF_SUBDIR) == 0 ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
		var files = Directory.GetFiles(Sys.findbase, Sys.findpattern, filter);
		var folders = Directory.GetDirectories(Sys.findbase, Sys.findpattern, filter);

		return folders.Concat(files).Select(s => s.Replace("\\", "/")).ToArray();
	}

	//============================================
	private static string[] fdir;
	private static int fileindex;
	private static string findbase;
	private static string findpattern;

	// ok.
	public static string FindFirst(string path, int musthave, int canthave)
	{
		if (Sys.fdir != null)
			Sys.Error("Sys_BeginFind without close");

		//	COM_FilePath (path, findbase);

		Sys.fdir = Sys.FindAll(path, canthave, musthave);
		Sys.fileindex = 0;

		if (Sys.fdir == null)
			return null;

		return Sys.FindNext();
	}

	public static string FindNext()
	{
		if (Sys.fileindex >= Sys.fdir.Length)
			return null;

		return Sys.fdir[Sys.fileindex++];
	}

	public static void FindClose()
	{
		Sys.fdir = null;
	}

	public static void SendKeyEvents()
	{
		Globals.re.getKeyboardHandler().Update();

		// grab frame time
		Globals.sys_frame_time = Timer.Sys_Milliseconds();
	}

	public static string GetClipboardData()
	{
		return null;
	}

	// Buffer for the dedicated server console
	private static string consoleText = string.Empty;

	public static string? Sys_ConsoleInput()
	{
		if (frame.dedicated.value == 0)
		{
			return null;
		}

		while (true)
		{
			if (!Console.KeyAvailable)
			{
				break;
			}

			var ch = Console.ReadKey(true).KeyChar;

			switch (ch)
			{
				case '\n':

					Console.Write("\r\n");

					if (consoleText.Length > 0)
					{
						var dummy = consoleText;
						consoleText = string.Empty;
						return dummy;
					}

					break;

				case '\b':

					if (consoleText.Length > 0)
					{
						consoleText = consoleText[..^1];
						Console.Write("\b \b");
					}

					break;

				default:

					if (ch >= ' ')
					{
						Console.Write(ch);
						consoleText += ch;
					}

					break;
			}
		}

		return null;
	}

	public static void ConsoleOutput(string msg)
	{
		if (Globals.nostdout != null && Globals.nostdout.value != 0)
			return;

		Console.Write(msg);
	}

	private static void Sys_Mkdir(string path)
	{
		if (!Sys_IsDir(path))
		{
			if (!Directory.CreateDirectory(path).Exists)
			{
				clientserver.Com_Error(Defines.ERR_FATAL, $"Couldn't create dir {path}\n");
			}
		}
	}

	private static bool Sys_IsDir(string path)
	{
		return Directory.Exists(path);
	}

	public static string GetHomeDir()
	{
		var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Replace('\\', '/');
		var gdir = $"{profile}/{Globals.CFGDIR}/";

		Sys_Mkdir(gdir);

		return gdir;
	}

	public static string GetWorkDir()
	{
		return Directory.GetCurrentDirectory();
	}

	public static string Realpath(string rawDir)
	{
		return Path.GetFullPath(rawDir);
	}
}
