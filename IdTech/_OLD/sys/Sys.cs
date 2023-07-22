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

using IdTech.backend;

namespace Quake2Sharp.sys;

/**
 * Sys
 */
public class Sys
{
	//============================================
	private static string[] fdir;
	private static int fileindex;
	private static string findbase;
	private static string findpattern;

	public static void SendKeyEvents()
	{
		Globals.re.getKeyboardHandler().Update();

		// grab frame time
		Globals.sys_frame_time = system.Sys_Milliseconds();
	}

	public static string GetClipboardData()
	{
		return null;
	}
}
