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
namespace Quake2Sharp;

using qcommon;
using sys;

public class Program
{
	public static Action<int> UpdateLoop;
	private static int last = Timer.Milliseconds();

	public static void Main(string[] args)
	{
		var dedicated = false;

		// check if we are in dedicated mode to hide the dialog.
		for (var n = 0; n < args.Length; n++)
		{
			if (!args[n].Equals("+set"))
				continue;

			if (++n >= args.Length)
				break;

			if (!args[n].Equals("dedicated"))
				continue;

			if (++n >= args.Length)
				break;

			if (!args[n].Equals("1") && !args[n].Equals("\"1\""))
				continue;

			Com.Printf("Starting in dedicated mode.\n");
			dedicated = true;
		}

		Globals.dedicated = Cvar.Get("dedicated", "0", Defines.CVAR_NOSET);

		if (dedicated)
			Globals.dedicated.value = 1.0f;

		Qcommon.Init(args);

		Globals.nostdout = Cvar.Get("nostdout", "0", 0);

		while (true)
		{
			// find time spending rendering last frame
			var now = Timer.Milliseconds();
			var delta = now - Program.last;

			if (Program.UpdateLoop != null)
				Program.UpdateLoop(delta);
			else if (delta > 0)
			{
				Qcommon.FrameUpdate(delta);
				Qcommon.FrameRender(delta);
			}

			Program.last = now;
		}
	}
}