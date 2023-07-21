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

namespace Quake2Sharp.client;

using qcommon;
using sys;
using System.Globalization;
using System.Text.RegularExpressions;

public class CL_view
{
	public static int num_cl_weaponmodels;
	public static string[] cl_weaponmodels = new string[Defines.MAX_CLIENTWEAPONMODELS];

	/*
	 * =================
	 *
	 * CL_PrepRefresh
	 *
	 * Call before entering a new level, or after changing dlls
	 * =================
	 */
	public static void PrepRefresh()
	{
		string mapname;
		int i;
		string name;
		float rotate;
		var axis = new float[3];

		if ((i = Globals.cl.configstrings[Defines.CS_MODELS + 1].Length) == 0)
			return; // no map loaded

		SCR.AddDirtyPoint(0, 0);
		SCR.AddDirtyPoint(Globals.viddef.getWidth() - 1, Globals.viddef.getHeight() - 1);

		// let the render dll load the map
		mapname = Globals.cl.configstrings[Defines.CS_MODELS + 1].Substring(5, i - 9); // skip "maps/"

		// cut off ".bsp"

		// register models, pics, and skins
		clientserver.Com_Printf("Map: " + mapname + "\r");
		SCR.UpdateScreen();
		Globals.re.BeginRegistration(mapname);
		clientserver.Com_Printf("                                     \r");

		// precache status bar pics
		clientserver.Com_Printf("pics\r");
		SCR.UpdateScreen();
		SCR.TouchPics();
		clientserver.Com_Printf("                                     \r");
		TemporaryEntities.RegisterModels();
		CL_view.num_cl_weaponmodels = 1;
		CL_view.cl_weaponmodels[0] = "weapon.md2";

		for (i = 1; i < Defines.MAX_MODELS && Globals.cl.configstrings[Defines.CS_MODELS + i].Length != 0; i++)
		{
			name = new(Globals.cl.configstrings[Defines.CS_MODELS + i]);

			if (name.Length > 37)
				name = name[..36];

			if (name[0] != '*')
				clientserver.Com_Printf(name + "\r");

			SCR.UpdateScreen();
			Sys.SendKeyEvents(); // pump message loop

			if (name[0] == '#')
			{
				// special player weapon model
				if (CL_view.num_cl_weaponmodels < Defines.MAX_CLIENTWEAPONMODELS)
				{
					CL_view.cl_weaponmodels[CL_view.num_cl_weaponmodels] = Globals.cl.configstrings[Defines.CS_MODELS + i][1..];
					CL_view.num_cl_weaponmodels++;
				}
			}
			else
			{
				Globals.cl.model_draw[i] = Globals.re.RegisterModel(Globals.cl.configstrings[Defines.CS_MODELS + i]);

				if (name[0] == '*')
					Globals.cl.model_clip[i] = CM.InlineModel(Globals.cl.configstrings[Defines.CS_MODELS + i]);
				else
					Globals.cl.model_clip[i] = null;
			}

			if (name[0] != '*')
				clientserver.Com_Printf("                                     \r");
		}

		clientserver.Com_Printf("images\r");
		SCR.UpdateScreen();

		for (i = 1; i < Defines.MAX_IMAGES && Globals.cl.configstrings[Defines.CS_IMAGES + i].Length > 0; i++)
		{
			Globals.cl.image_precache[i] = Globals.re.RegisterPic(Globals.cl.configstrings[Defines.CS_IMAGES + i]);
			Sys.SendKeyEvents(); // pump message loop
		}

		clientserver.Com_Printf("                                     \r");

		for (i = 0; i < Defines.MAX_CLIENTS; i++)
		{
			if (Globals.cl.configstrings[Defines.CS_PLAYERSKINS + i].Length == 0)
				continue;

			clientserver.Com_Printf("client " + i + '\r');
			SCR.UpdateScreen();
			Sys.SendKeyEvents(); // pump message loop
			CL_parse.ParseClientinfo(i);
			clientserver.Com_Printf("                                     \r");
		}

		CL_parse.LoadClientinfo(Globals.cl.baseclientinfo, "unnamed\\male/grunt");

		// set sky textures and speed
		clientserver.Com_Printf("sky\r");
		SCR.UpdateScreen();
		rotate = float.Parse(Globals.cl.configstrings[Defines.CS_SKYROTATE], CultureInfo.InvariantCulture);
		var st = Regex.Split(Globals.cl.configstrings[Defines.CS_SKYAXIS], "\\s");
		axis[0] = float.Parse(st[0], CultureInfo.InvariantCulture);
		axis[1] = float.Parse(st[1], CultureInfo.InvariantCulture);
		axis[2] = float.Parse(st[2], CultureInfo.InvariantCulture);
		Globals.re.SetSky(Globals.cl.configstrings[Defines.CS_SKY], rotate, axis);
		clientserver.Com_Printf("                                     \r");

		// the renderer can now free unneeded stuff
		Globals.re.EndRegistration();

		// clear any lines of console text
		QConsole.ClearNotify();
		SCR.UpdateScreen();
		Globals.cl.refresh_prepped = true;
		Globals.cl.force_refdef = true; // make sure we have a valid refdef
	}

	public static void AddNetgraph()
	{
		int i;
		int @in;
		int ping;

		// if using the debuggraph for something else, don't
		// add the net lines
		if (SCR.scr_debuggraph.value == 0.0f || SCR.scr_timegraph.value == 0.0f)
			return;

		for (i = 0; i < Globals.cls.netchan.dropped; i++)
			SCR.DebugGraph(30, 0x40);

		for (i = 0; i < Globals.cl.surpressCount; i++)
			SCR.DebugGraph(30, 0xdf);

		// see what the latency was on this packet
		@in = Globals.cls.netchan.incoming_acknowledged & (Defines.CMD_BACKUP - 1);
		ping = (int)(Globals.cls.realtime - Globals.cl.cmd_time[@in]);
		ping /= 30;

		if (ping > 30)
			ping = 30;

		SCR.DebugGraph(ping, 0xd0);
	}
}
