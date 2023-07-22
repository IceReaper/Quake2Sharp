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

namespace IdTech.backend;

/*
 * =======================================================================
 *
 * This file is the starting point of the program. It does some platform
 * specific initialization stuff and calls the common initialization code.
 *
 * =======================================================================
 */
public static class main
{
	public static bool DEDICATED_ONLY;

	/*
	 * Main function.
	 * Contains the initialization code and the main loop
	 */
	public static void Main(string[] argv)
	{
		// crappy argument parser can't parse.
		for (var i = 0; i < argv.Length; i++)
		{
			// Running in dedicated only mode?
			if (argv[i] == "-dedicated")
			{
				main.DEDICATED_ONLY = true;
				frame.dedicated.value = 1;
			}

			// Are we portable?
			if (argv[i] == "-portable")
				frame.is_portable = true;

			// Inject a custom data dir.
			if (argv[i] == "-datadir")
			{
				// Mkay, did the user give us an argument?
				if (i != argv.Length - 1)
				{
					if (Path.Exists(argv[i + 1]))
					{
						if (!Directory.Exists(argv[i + 1]))
						{
							Console.WriteLine($"-datadir {argv[i + 1]} is not a directory");
							return;
						}

						filesystem.datadir = argv[i + 1];
					}
					else
					{
						Console.WriteLine($"-datadir {argv[i + 1]} could not be found");
						return;
					}
				}
				else
				{
					Console.WriteLine("-datadir needs an argument");
					return;
				}
			}

			// Inject a custom config dir.
			if (argv[i] == "-cfgdir")
			{
				// We need an argument.
				if (i != argv.Length - 1)
					system.cfgdir = argv[i + 1];
				else
				{
					Console.WriteLine("-cfgdir needs an argument");
					return;
				}
			}
		}
	}
}
