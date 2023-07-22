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
using IdTech.common;
using Quake2Sharp.client.types;
using Quake2Sharp.game.types;
using Quake2Sharp.qcommon;
using Quake2Sharp.qcommon.types;
using Quake2Sharp.sound;
using System.Drawing;
using System.Globalization;
using System.Text;

namespace Quake2Sharp.client;

/**
 * SCR
 */
public class SCR
{
	//	cl_scrn.c -- master for refresh, status bar, console, chat, notify, etc

	private static readonly string[][] sb_nums =
	{
		new[] { "num_0", "num_1", "num_2", "num_3", "num_4", "num_5", "num_6", "num_7", "num_8", "num_9", "num_minus" },
		new[] { "anum_0", "anum_1", "anum_2", "anum_3", "anum_4", "anum_5", "anum_6", "anum_7", "anum_8", "anum_9", "anum_minus" }
	};

	/*
	 * full screen console put up loading plaque blanked background with loading
	 * plaque blanked background with menu cinematics full screen image for quit
	 * and victory
	 *
	 * end of unit intermissions
	 */
	private static float scr_con_current; // aproaches scr_conlines at scr_conspeed
	private static float scr_conlines; // 0.0 to 1.0 lines of console to display
	private static bool scr_initialized; // ready to draw
	private static int scr_draw_loading;

	// scr_vrect ist in Globals definiert
	// position of render window on screen
	public static cvar_t scr_viewsize;
	private static cvar_t scr_conspeed;
	private static cvar_t scr_centertime;
	private static cvar_t scr_showturtle;
	private static cvar_t scr_showpause;
	private static cvar_t scr_printspeed;
	private static cvar_t scr_netgraph;
	public static cvar_t scr_timegraph;
	public static cvar_t scr_debuggraph;
	private static cvar_t scr_graphheight;
	private static cvar_t scr_graphscale;
	private static cvar_t scr_graphshift;
	private static cvar_t scr_drawall;
	public static cvar_t fps = new();
	private static readonly dirty_t scr_dirty = new();
	private static readonly dirty_t[] scr_old_dirty = { new(), new() };
	private static string crosshair_pic;

	private static int crosshair_width, crosshair_height;
	/*
	 * ===============================================================================
	 *
	 * BAR GRAPHS
	 *
	 * ===============================================================================
	 */

	private static int current;
	private static readonly graphsamp_t[] values = new graphsamp_t[1024];

	static SCR()
	{
		for (var n = 0; n < 1024; n++)
			SCR.values[n] = new();
	}

	/*
	 * ============== SCR_DebugGraph ==============
	 */
	public static void DebugGraph(float value, int color)
	{
		SCR.values[SCR.current & 1023].value = value;
		SCR.values[SCR.current & 1023].color = color;
		SCR.current++;
	}

	/*
	 * ============== SCR_DrawDebugGraph ==============
	 */
	private static void DrawDebugGraph()
	{
		int a, x, y, w, i, h;
		float v;
		int color;

		// draw the graph
		w = Globals.scr_vrect.width;
		x = Globals.scr_vrect.x;
		y = Globals.scr_vrect.y + Globals.scr_vrect.height;
		Globals.re.DrawFill(x, (int)(y - SCR.scr_graphheight.value), w, (int)SCR.scr_graphheight.value, 8);

		for (a = 0; a < w; a++)
		{
			i = (SCR.current - 1 - a + 1024) & 1023;
			v = SCR.values[i].value;
			color = SCR.values[i].color;
			v = v * SCR.scr_graphscale.value + SCR.scr_graphshift.value;

			if (v < 0)
				v += SCR.scr_graphheight.value * (1 + (int)(-v / SCR.scr_graphheight.value));

			h = (int)v % (int)SCR.scr_graphheight.value;
			Globals.re.DrawFill(x + w - 1 - a, y - h, 1, h, color);
		}
	}

	/*
	 * ===============================================================================
	 *
	 * CENTER PRINTING
	 *
	 * ===============================================================================
	 */

	// char scr_centerstring[1024];
	private static string scr_centerstring;
	private static float scr_centertime_start; // for slow victory printing
	private static float scr_centertime_off;
	private static int scr_center_lines;

	/*
	 * ============== SCR_CenterPrint
	 *
	 * Called for important messages that should stay in the center of the
	 * screen for a few moments ==============
	 */
	public static void CenterPrint(string str)
	{
		//char *s;
		int s;
		StringBuilder line = new(64);
		int i, j, l;

		//strncpy (scr_centerstring, str, sizeof(scr_centerstring)-1);
		SCR.scr_centerstring = str;
		SCR.scr_centertime_off = SCR.scr_centertime.value;
		SCR.scr_centertime_start = Globals.cl.time;

		// count the number of lines for centering
		SCR.scr_center_lines = 1;
		s = 0;

		while (s < str.Length)
		{
			if (str[s] == '\n')
				SCR.scr_center_lines++;

			s++;
		}

		// echo it to the console
		clientserver.Com_Printf(
			"\n\n\035\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\037\n\n"
		);

		s = 0;

		if (str.Length != 0)
		{
			do
			{
				// scan the width of the line

				for (l = 0; l < 40 && l + s < str.Length; l++)
				{
					if (str[s + l] == '\n' || str[s + l] == 0)
						break;
				}

				for (i = 0; i < (40 - l) / 2; i++)
					line.Append(' ');

				for (j = 0; j < l; j++)
					line.Append(str[s + j]);

				line.Append('\n');
				clientserver.Com_Printf(line.ToString());

				while (s < str.Length && str[s] != '\n')
					s++;

				if (s == str.Length)
					break;

				s++; // skip the \n
			}
			while (true);
		}

		clientserver.Com_Printf(
			"\n\n\035\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\036\037\n\n"
		);

		QConsole.ClearNotify();
	}

	private static void DrawCenterString()
	{
		var cs = SCR.scr_centerstring + "\0";
		int start;
		int l;
		int j;
		int x, y;
		int remaining;

		if (cs == null)
			return;

		if (cs.Length == 0)
			return;

		// the finale prints the characters one at a time
		remaining = 9999;
		start = 0;

		if (SCR.scr_center_lines <= 4)
			y = (int)(Globals.viddef.getHeight() * 0.35);
		else
			y = 48;

		do
		{
			// scan the width of the line
			for (l = 0; l < 40; l++)
			{
				if (start + l == cs.Length - 1 || cs[start + l] == '\n')
					break;
			}

			x = (Globals.viddef.getWidth() - l * 8) / 2;
			SCR.AddDirtyPoint(x, y);

			for (j = 0; j < l; j++, x += 8)
			{
				Globals.re.DrawChar(x, y, cs[start + j]);

				if (remaining == 0)
					return;

				remaining--;
			}

			SCR.AddDirtyPoint(x, y + 8);
			y += 8;

			while (start < cs.Length && cs[start] != '\n')
				start++;

			if (start == cs.Length)
				break;

			start++; // skip the \n
		}
		while (true);
	}

	private static void CheckDrawCenterString()
	{
		SCR.scr_centertime_off -= Globals.cls.frametime;

		if (SCR.scr_centertime_off <= 0)
			return;

		SCR.DrawCenterString();
	}

	// =============================================================================

	/*
	 * ================= SCR_CalcVrect
	 *
	 * Sets scr_vrect, the coordinates of the rendered window =================
	 */
	private static void CalcVrect()
	{
		int size;

		// bound viewsize
		if (SCR.scr_viewsize.value < 40)
			cvar.Cvar_Set("viewsize", "40");

		if (SCR.scr_viewsize.value > 100)
			cvar.Cvar_Set("viewsize", "100");

		size = (int)SCR.scr_viewsize.value;
		Globals.scr_vrect.width = Globals.viddef.getWidth() * size / 100;
		Globals.scr_vrect.width &= ~7;
		Globals.scr_vrect.height = Globals.viddef.getHeight() * size / 100;
		Globals.scr_vrect.height &= ~1;
		Globals.scr_vrect.x = (Globals.viddef.getWidth() - Globals.scr_vrect.width) / 2;
		Globals.scr_vrect.y = (Globals.viddef.getHeight() - Globals.scr_vrect.height) / 2;
	}

	/*
	 * ================= SCR_SizeUp_f
	 *
	 * Keybinding command =================
	 */
	private static void SizeUp_f()
	{
		cvar.Cvar_SetValue("viewsize", SCR.scr_viewsize.value + 10);
	}

	/*
	 * ================= SCR_SizeDown_f
	 *
	 * Keybinding command =================
	 */
	private static void SizeDown_f()
	{
		cvar.Cvar_SetValue("viewsize", SCR.scr_viewsize.value - 10);
	}

	/*
	 * ================= SCR_Sky_f
	 *
	 * Set a specific sky and rotation speed =================
	 */
	private static void Sky_f()
	{
		float rotate;
		float[] axis = { 0, 0, 0 };

		if (cmdparser.Cmd_Argc() < 2)
		{
			clientserver.Com_Printf("Usage: sky <basename> <rotate> <axis x y z>\n");

			return;
		}

		if (cmdparser.Cmd_Argc() > 2)
			rotate = float.Parse(cmdparser.Cmd_Argv(2), CultureInfo.InvariantCulture);
		else
			rotate = 0;

		if (cmdparser.Cmd_Argc() == 6)
		{
			axis[0] = float.Parse(cmdparser.Cmd_Argv(3), CultureInfo.InvariantCulture);
			axis[1] = float.Parse(cmdparser.Cmd_Argv(4), CultureInfo.InvariantCulture);
			axis[2] = float.Parse(cmdparser.Cmd_Argv(5), CultureInfo.InvariantCulture);
		}
		else
		{
			axis[0] = 0;
			axis[1] = 0;
			axis[2] = 1;
		}

		Globals.re.SetSky(cmdparser.Cmd_Argv(1), rotate, axis);
	}

	// ============================================================================

	/*
	 * ================== SCR_Init ==================
	 */
	public static void Init()
	{
		SCR.scr_viewsize = cvar.Cvar_Get("viewsize", "100", Defines.CVAR_ARCHIVE);
		SCR.scr_conspeed = cvar.Cvar_Get("scr_conspeed", "3", 0);
		SCR.scr_showturtle = cvar.Cvar_Get("scr_showturtle", "0", 0);
		SCR.scr_showpause = cvar.Cvar_Get("scr_showpause", "1", 0);
		SCR.scr_centertime = cvar.Cvar_Get("scr_centertime", "2.5", 0);
		SCR.scr_printspeed = cvar.Cvar_Get("scr_printspeed", "8", 0);
		SCR.scr_netgraph = cvar.Cvar_Get("netgraph", "1", 0);
		SCR.scr_timegraph = cvar.Cvar_Get("timegraph", "1", 0);
		SCR.scr_debuggraph = cvar.Cvar_Get("debuggraph", "1", 0);
		SCR.scr_graphheight = cvar.Cvar_Get("graphheight", "32", 0);
		SCR.scr_graphscale = cvar.Cvar_Get("graphscale", "1", 0);
		SCR.scr_graphshift = cvar.Cvar_Get("graphshift", "0", 0);
		SCR.scr_drawall = cvar.Cvar_Get("scr_drawall", "1", 0);
		SCR.fps = cvar.Cvar_Get("fps", "0", 0);

		//
		// register our commands
		//
		cmdparser.Cmd_AddCommand("timerefresh", () => { SCR.TimeRefresh_f(); });
		cmdparser.Cmd_AddCommand("loading", () => { SCR.Loading_f(); });
		cmdparser.Cmd_AddCommand("sizeup", () => { SCR.SizeUp_f(); });
		cmdparser.Cmd_AddCommand("sizedown", () => { SCR.SizeDown_f(); });
		cmdparser.Cmd_AddCommand("sky", () => { SCR.Sky_f(); });
		SCR.scr_initialized = true;
	}

	/*
	 * ============== SCR_DrawNet ==============
	 */
	private static void DrawNet()
	{
		if (Globals.cls.netchan.outgoing_sequence - Globals.cls.netchan.incoming_acknowledged < Defines.CMD_BACKUP - 1)
			return;

		Globals.re.DrawPic(Globals.scr_vrect.x + 64, Globals.scr_vrect.y, "net");
	}

	/*
	 * ============== SCR_DrawPause ==============
	 */
	private static void DrawPause()
	{
		var dim = new Size();

		if (SCR.scr_showpause.value == 0) // turn off for screenshots
			return;

		if (Globals.cl_paused.value == 0)
			return;

		Globals.re.DrawGetPicSize(ref dim, "pause");
		Globals.re.DrawPic((Globals.viddef.getWidth() - dim.Width) / 2, Globals.viddef.getHeight() / 2 + 8, "pause");
	}

	/*
	 * ============== SCR_DrawLoading ==============
	 */
	private static void DrawLoading()
	{
		var dim = new Size();

		if (SCR.scr_draw_loading == 0)
			return;

		SCR.scr_draw_loading = 0;
		Globals.re.DrawGetPicSize(ref dim, "loading");
		Globals.re.DrawPic((Globals.viddef.getWidth() - dim.Width) / 2, (Globals.viddef.getHeight() - dim.Height) / 2, "loading");
	}

	// =============================================================================

	/*
	 * ================== SCR_RunConsole
	 *
	 * Scroll it up or down ==================
	 */
	public static void RunConsole()
	{
		// decide on the height of the console
		if (Globals.cls.key_dest == Defines.key_console)
			SCR.scr_conlines = 0.5f; // half screen
		else
			SCR.scr_conlines = 0; // none visible

		if (SCR.scr_conlines < SCR.scr_con_current)
		{
			SCR.scr_con_current -= SCR.scr_conspeed.value * Globals.cls.frametime;

			if (SCR.scr_conlines > SCR.scr_con_current)
				SCR.scr_con_current = SCR.scr_conlines;
		}
		else if (SCR.scr_conlines > SCR.scr_con_current)
		{
			SCR.scr_con_current += SCR.scr_conspeed.value * Globals.cls.frametime;

			if (SCR.scr_conlines < SCR.scr_con_current)
				SCR.scr_con_current = SCR.scr_conlines;
		}
	}

	/*
	 * ================== SCR_DrawConsole ==================
	 */
	private static void DrawConsole()
	{
		QConsole.CheckResize();

		if (Globals.cls.state == Defines.ca_disconnected || Globals.cls.state == Defines.ca_connecting)
		{
			// forced
			// full
			// screen
			// console
			QConsole.DrawConsole(1.0f);

			return;
		}

		if (Globals.cls.state != Defines.ca_active || !Globals.cl.refresh_prepped)
		{
			// connected, but
			// can't render
			QConsole.DrawConsole(0.5f);
			Globals.re.DrawFill(0, Globals.viddef.getHeight() / 2, Globals.viddef.getWidth(), Globals.viddef.getHeight() / 2, 0);

			return;
		}

		if (SCR.scr_con_current != 0)
			QConsole.DrawConsole(SCR.scr_con_current);
		else
		{
			if (Globals.cls.key_dest == Defines.key_game || Globals.cls.key_dest == Defines.key_message)
				QConsole.DrawNotify(); // only draw notify in game
		}
	}

	// =============================================================================

	/*
	 * ================ SCR_BeginLoadingPlaque ================
	 */
	public static void BeginLoadingPlaque()
	{
		S.StopAllSounds();
		Globals.cl.sound_prepped = false; // don't play ambients

		if (Globals.cls.disable_screen != 0)
			return;

		if (frame.developer.value != 0)
			return;

		if (Globals.cls.state == Defines.ca_disconnected)
			return; // if at console, don't bring up the plaque

		if (Globals.cls.key_dest == Defines.key_console)
			return;

		if (Globals.cl.cinematictime > 0)
			SCR.scr_draw_loading = 2; // clear to balack first
		else
			SCR.scr_draw_loading = 1;

		SCR.UpdateScreen();
		Globals.cls.disable_screen = system.Sys_Milliseconds();
		Globals.cls.disable_servercount = Globals.cl.servercount;
	}

	/*
	 * ================ SCR_EndLoadingPlaque ================
	 */
	public static void EndLoadingPlaque()
	{
		Globals.cls.disable_screen = 0;
		QConsole.ClearNotify();
	}

	/*
	 * ================ SCR_Loading_f ================
	 */
	private static void Loading_f()
	{
		SCR.BeginLoadingPlaque();
	}

	/*
	 * ================ SCR_TimeRefresh_f ================
	 */
	private static void TimeRefresh_f()
	{
		int i;
		int start, stop;
		float time;

		if (Globals.cls.state != Defines.ca_active)
			return;

		start = system.Sys_Milliseconds();

		if (cmdparser.Cmd_Argc() == 2)
		{
			// run without page flipping
			Globals.re.BeginFrame(0);

			for (i = 0; i < 128; i++)
			{
				Globals.cl.refdef.viewangles[1] = i / 128.0f * 360.0f;
				Globals.re.RenderFrame(Globals.cl.refdef);
			}

			Globals.re.EndFrame();
		}
		else
		{
			for (i = 0; i < 128; i++)
			{
				Globals.cl.refdef.viewangles[1] = i / 128.0f * 360.0f;
				Globals.re.BeginFrame(0);
				Globals.re.RenderFrame(Globals.cl.refdef);
				Globals.re.EndFrame();
			}
		}

		stop = system.Sys_Milliseconds();
		time = (stop - start) / 1000.0f;
		clientserver.Com_Printf($"{time} seconds ({128.0f / time} fps)\n");
	}

	public static void DirtyScreen()
	{
		SCR.AddDirtyPoint(0, 0);
		SCR.AddDirtyPoint(Globals.viddef.getWidth() - 1, Globals.viddef.getHeight() - 1);
	}

	/*
	 * ============== SCR_TileClear
	 *
	 * Clear any parts of the tiled background that were drawn on last frame
	 * ==============
	 */
	private static readonly dirty_t clear = new();

	private static void TileClear()
	{
		int i;
		int top, bottom, left, right;
		SCR.clear.clear();

		if (SCR.scr_drawall.value != 0)
			SCR.DirtyScreen(); // for power vr or broken page flippers...

		if (SCR.scr_con_current == 1.0f)
			return; // full screen console

		if (SCR.scr_viewsize.value == 100)
			return; // full screen rendering

		if (Globals.cl.cinematictime > 0)
			return; // full screen cinematic

		// erase rect will be the union of the past three frames
		// so tripple buffering works properly
		SCR.clear.set(SCR.scr_dirty);

		for (i = 0; i < 2; i++)
		{
			if (SCR.scr_old_dirty[i].x1 < SCR.clear.x1)
				SCR.clear.x1 = SCR.scr_old_dirty[i].x1;

			if (SCR.scr_old_dirty[i].x2 > SCR.clear.x2)
				SCR.clear.x2 = SCR.scr_old_dirty[i].x2;

			if (SCR.scr_old_dirty[i].y1 < SCR.clear.y1)
				SCR.clear.y1 = SCR.scr_old_dirty[i].y1;

			if (SCR.scr_old_dirty[i].y2 > SCR.clear.y2)
				SCR.clear.y2 = SCR.scr_old_dirty[i].y2;
		}

		SCR.scr_old_dirty[1].set(SCR.scr_old_dirty[0]);
		SCR.scr_old_dirty[0].set(SCR.scr_dirty);
		SCR.scr_dirty.x1 = 9999;
		SCR.scr_dirty.x2 = -9999;
		SCR.scr_dirty.y1 = 9999;
		SCR.scr_dirty.y2 = -9999;

		// don't bother with anything convered by the console)
		top = (int)(SCR.scr_con_current * Globals.viddef.getHeight());

		if (top >= SCR.clear.y1)
			SCR.clear.y1 = top;

		if (SCR.clear.y2 <= SCR.clear.y1)
			return; // nothing disturbed

		top = Globals.scr_vrect.y;
		bottom = top + Globals.scr_vrect.height - 1;
		left = Globals.scr_vrect.x;
		right = left + Globals.scr_vrect.width - 1;

		if (SCR.clear.y1 < top)
		{
			// clear above view screen
			i = SCR.clear.y2 < top - 1 ? SCR.clear.y2 : top - 1;
			Globals.re.DrawTileClear(SCR.clear.x1, SCR.clear.y1, SCR.clear.x2 - SCR.clear.x1 + 1, i - SCR.clear.y1 + 1, "backtile");
			SCR.clear.y1 = top;
		}

		if (SCR.clear.y2 > bottom)
		{
			// clear below view screen
			i = SCR.clear.y1 > bottom + 1 ? SCR.clear.y1 : bottom + 1;
			Globals.re.DrawTileClear(SCR.clear.x1, i, SCR.clear.x2 - SCR.clear.x1 + 1, SCR.clear.y2 - i + 1, "backtile");
			SCR.clear.y2 = bottom;
		}

		if (SCR.clear.x1 < left)
		{
			// clear left of view screen
			i = SCR.clear.x2 < left - 1 ? SCR.clear.x2 : left - 1;
			Globals.re.DrawTileClear(SCR.clear.x1, SCR.clear.y1, i - SCR.clear.x1 + 1, SCR.clear.y2 - SCR.clear.y1 + 1, "backtile");
			SCR.clear.x1 = left;
		}

		if (SCR.clear.x2 > right)
		{
			// clear left of view screen
			i = SCR.clear.x1 > right + 1 ? SCR.clear.x1 : right + 1;
			Globals.re.DrawTileClear(i, SCR.clear.y1, SCR.clear.x2 - i + 1, SCR.clear.y2 - SCR.clear.y1 + 1, "backtile");
			SCR.clear.x2 = right;
		}
	}

	// ===============================================================
	private static readonly int STAT_MINUS = 10; // num frame for '-' stats digit
	private static readonly int CHAR_WIDTH = 16;

	/*
	 * ================ SizeHUDString
	 *
	 * Allow embedded \n in the string ================
	 */
	private static void SizeHUDString(string @string, ref Size dim)
	{
		int lines, width, current;
		lines = 1;
		width = 0;
		current = 0;

		for (var i = 0; i < @string.Length; i++)
		{
			if (@string[i] == '\n')
			{
				lines++;
				current = 0;
			}
			else
			{
				current++;

				if (current > width)
					width = current;
			}
		}

		dim.Width = width * 8;
		dim.Height = lines * 8;
	}

	private static void DrawHUDString(string @string, int x, int y, int centerwidth, int xor)
	{
		int margin;

		//char line[1024];
		StringBuilder line = new(1024);
		int i;
		margin = x;

		for (var l = 0; l < @string.Length;)
		{
			// scan out one line of text from the string
			line = new(1024);

			while (l < @string.Length && @string[l] != '\n')
			{
				line.Append(@string[l]);
				l++;
			}

			if (centerwidth != 0)
				x = margin + (centerwidth - line.Length * 8) / 2;
			else
				x = margin;

			for (i = 0; i < line.Length; i++)
			{
				Globals.re.DrawChar(x, y, line[i] ^ xor);
				x += 8;
			}

			if (l < @string.Length)
			{
				l++; // skip the \n
				x = margin;
				y += 8;
			}
		}
	}

	/*
	 * ============== SCR_DrawField ==============
	 */
	private static void DrawField(int x, int y, int color, int width, int value)
	{
		char ptr;
		string num;
		int l;
		int frame;

		if (width < 1)
			return;

		// draw number string
		if (width > 5)
			width = 5;

		SCR.AddDirtyPoint(x, y);
		SCR.AddDirtyPoint(x + width * SCR.CHAR_WIDTH + 2, y + 23);
		num = value.ToString();
		l = num.Length;

		if (l > width)
			l = width;

		x += 2 + SCR.CHAR_WIDTH * (width - l);
		ptr = num[0];

		for (var i = 0; i < l; i++)
		{
			ptr = num[i];

			if (ptr == '-')
				frame = SCR.STAT_MINUS;
			else
				frame = ptr - '0';

			Globals.re.DrawPic(x, y, SCR.sb_nums[color][frame]);
			x += SCR.CHAR_WIDTH;
		}
	}

	/*
	 * =============== SCR_TouchPics
	 *
	 * Allows rendering code to cache all needed sbar graphics ===============
	 */
	public static void TouchPics()
	{
		int i, j;

		for (i = 0; i < 2; i++)
		for (j = 0; j < 11; j++)
			Globals.re.RegisterPic(SCR.sb_nums[i][j]);

		if (Globals.crosshair.value != 0.0f)
		{
			if (Globals.crosshair.value > 3.0f || Globals.crosshair.value < 0.0f)
				Globals.crosshair.value = 3.0f;

			SCR.crosshair_pic = "ch" + (int)Globals.crosshair.value;
			var dim = new Size();
			Globals.re.DrawGetPicSize(ref dim, SCR.crosshair_pic);
			SCR.crosshair_width = dim.Width;
			SCR.crosshair_height = dim.Height;

			if (SCR.crosshair_width == 0)
				SCR.crosshair_pic = "";
		}
	}

	private static readonly LayoutParser layoutParser = new();

	/*
	 * ================ SCR_ExecuteLayoutString
	 *
	 * ================
	 */
	private static void ExecuteLayoutString(string s)
	{
		if (Globals.cls.state != Defines.ca_active || !Globals.cl.refresh_prepped)
			return;

		if (s == null || s.Length == 0)
			return;

		var x = 0;
		var y = 0;
		var width = 3;
		int value;
		var parser = SCR.layoutParser;
		parser.init(s);

		while (parser.hasNext())
		{
			parser.next();

			if (parser.tokenEquals("xl"))
			{
				parser.next();
				x = parser.tokenAsInt();

				continue;
			}

			if (parser.tokenEquals("xr"))
			{
				parser.next();
				x = Globals.viddef.getWidth() + parser.tokenAsInt();

				continue;
			}

			if (parser.tokenEquals("xv"))
			{
				parser.next();
				x = Globals.viddef.getWidth() / 2 - 160 + parser.tokenAsInt();

				continue;
			}

			if (parser.tokenEquals("yt"))
			{
				parser.next();
				y = parser.tokenAsInt();

				continue;
			}

			if (parser.tokenEquals("yb"))
			{
				parser.next();
				y = Globals.viddef.getHeight() + parser.tokenAsInt();

				continue;
			}

			if (parser.tokenEquals("yv"))
			{
				parser.next();
				y = Globals.viddef.getHeight() / 2 - 120 + parser.tokenAsInt();

				continue;
			}

			if (parser.tokenEquals("pic"))
			{
				// draw a pic from a stat number
				parser.next();
				value = Globals.cl.frame.playerstate.stats[parser.tokenAsInt()];

				if (value >= Defines.MAX_IMAGES)
					clientserver.Com_Error(Defines.ERR_DROP, "Pic >= MAX_IMAGES");

				if (Globals.cl.configstrings[Defines.CS_IMAGES + value] != null)
				{
					SCR.AddDirtyPoint(x, y);
					SCR.AddDirtyPoint(x + 23, y + 23);
					Globals.re.DrawPic(x, y, Globals.cl.configstrings[Defines.CS_IMAGES + value]);
				}

				continue;
			}

			if (parser.tokenEquals("client"))
			{
				// draw a deathmatch client block
				int score, ping, time;
				parser.next();
				x = Globals.viddef.getWidth() / 2 - 160 + parser.tokenAsInt();
				parser.next();
				y = Globals.viddef.getHeight() / 2 - 120 + parser.tokenAsInt();
				SCR.AddDirtyPoint(x, y);
				SCR.AddDirtyPoint(x + 159, y + 31);
				parser.next();
				value = parser.tokenAsInt();

				if (value >= Defines.MAX_CLIENTS || value < 0)
					clientserver.Com_Error(Defines.ERR_DROP, "client >= MAX_CLIENTS");

				var ci = Globals.cl.clientinfo[value];
				parser.next();
				score = parser.tokenAsInt();
				parser.next();
				ping = parser.tokenAsInt();
				parser.next();
				time = parser.tokenAsInt();
				QConsole.DrawAltString(x + 32, y, ci.name);
				QConsole.DrawString(x + 32, y + 8, "Score: ");
				QConsole.DrawAltString(x + 32 + 7 * 8, y + 8, "" + score);
				QConsole.DrawString(x + 32, y + 16, "Ping:  " + ping);
				QConsole.DrawString(x + 32, y + 24, "Time:  " + time);

				if (ci.icon == null)
					ci = Globals.cl.baseclientinfo;

				Globals.re.DrawPic(x, y, ci.iconname);

				continue;
			}

			if (parser.tokenEquals("ctf"))
			{
				// draw a ctf client block
				int score, ping;
				parser.next();
				x = Globals.viddef.getWidth() / 2 - 160 + parser.tokenAsInt();
				parser.next();
				y = Globals.viddef.getHeight() / 2 - 120 + parser.tokenAsInt();
				SCR.AddDirtyPoint(x, y);
				SCR.AddDirtyPoint(x + 159, y + 31);
				parser.next();
				value = parser.tokenAsInt();

				if (value >= Defines.MAX_CLIENTS || value < 0)
					clientserver.Com_Error(Defines.ERR_DROP, "client >= MAX_CLIENTS");

				var ci = Globals.cl.clientinfo[value];
				parser.next();
				score = parser.tokenAsInt();
				parser.next();
				ping = parser.tokenAsInt();

				if (ping > 999)
					ping = 999;

				// sprintf(block, "%3d %3d %-12.12s", score, ping, ci->name);
				var block = $"{score,3} {ping,3} {ci.name,-12}";

				if (value == Globals.cl.playernum)
					QConsole.DrawAltString(x, y, block);
				else
					QConsole.DrawString(x, y, block);

				continue;
			}

			if (parser.tokenEquals("picn"))
			{
				// draw a pic from a name
				parser.next();
				SCR.AddDirtyPoint(x, y);
				SCR.AddDirtyPoint(x + 23, y + 23);
				Globals.re.DrawPic(x, y, parser.token());

				continue;
			}

			if (parser.tokenEquals("num"))
			{
				// draw a number
				parser.next();
				width = parser.tokenAsInt();
				parser.next();
				value = Globals.cl.frame.playerstate.stats[parser.tokenAsInt()];
				SCR.DrawField(x, y, 0, width, value);

				continue;
			}

			if (parser.tokenEquals("hnum"))
			{
				// health number
				int color;
				width = 3;
				value = Globals.cl.frame.playerstate.stats[Defines.STAT_HEALTH];

				if (value > 25)
					color = 0; // green
				else if (value > 0)
					color = (Globals.cl.frame.serverframe >> 2) & 1; // flash
				else
					color = 1;

				if ((Globals.cl.frame.playerstate.stats[Defines.STAT_FLASHES] & 1) != 0)
					Globals.re.DrawPic(x, y, "field_3");

				SCR.DrawField(x, y, color, width, value);

				continue;
			}

			if (parser.tokenEquals("anum"))
			{
				// ammo number
				int color;
				width = 3;
				value = Globals.cl.frame.playerstate.stats[Defines.STAT_AMMO];

				if (value > 5)
					color = 0; // green
				else if (value >= 0)
					color = (Globals.cl.frame.serverframe >> 2) & 1; // flash
				else
					continue; // negative number = don't show

				if ((Globals.cl.frame.playerstate.stats[Defines.STAT_FLASHES] & 4) != 0)
					Globals.re.DrawPic(x, y, "field_3");

				SCR.DrawField(x, y, color, width, value);

				continue;
			}

			if (parser.tokenEquals("rnum"))
			{
				// armor number
				int color;
				width = 3;
				value = Globals.cl.frame.playerstate.stats[Defines.STAT_ARMOR];

				if (value < 1)
					continue;

				color = 0; // green

				if ((Globals.cl.frame.playerstate.stats[Defines.STAT_FLASHES] & 2) != 0)
					Globals.re.DrawPic(x, y, "field_3");

				SCR.DrawField(x, y, color, width, value);

				continue;
			}

			if (parser.tokenEquals("stat_string"))
			{
				parser.next();
				var index = parser.tokenAsInt();

				if (index < 0 || index >= Defines.MAX_CONFIGSTRINGS)
					clientserver.Com_Error(Defines.ERR_DROP, "Bad stat_string index");

				index = Globals.cl.frame.playerstate.stats[index];

				if (index < 0 || index >= Defines.MAX_CONFIGSTRINGS)
					clientserver.Com_Error(Defines.ERR_DROP, "Bad stat_string index");

				QConsole.DrawString(x, y, Globals.cl.configstrings[index]);

				continue;
			}

			if (parser.tokenEquals("cstring"))
			{
				parser.next();
				SCR.DrawHUDString(parser.token(), x, y, 320, 0);

				continue;
			}

			if (parser.tokenEquals("string"))
			{
				parser.next();
				QConsole.DrawString(x, y, parser.token());

				continue;
			}

			if (parser.tokenEquals("cstring2"))
			{
				parser.next();
				SCR.DrawHUDString(parser.token(), x, y, 320, 0x80);

				continue;
			}

			if (parser.tokenEquals("string2"))
			{
				parser.next();
				QConsole.DrawAltString(x, y, parser.token());

				continue;
			}

			if (parser.tokenEquals("if"))
			{
				// draw a number
				parser.next();
				value = Globals.cl.frame.playerstate.stats[parser.tokenAsInt()];

				if (value == 0)
				{
					parser.next();

					// skip to endif
					while (parser.hasNext() && !parser.tokenEquals("endif"))
						parser.next();
				}

				continue;
			}
		}
	}

	/*
	 * ================ SCR_DrawStats
	 *
	 * The status bar is a small layout program that is based on the stats array
	 * ================
	 */
	private static void DrawStats()
	{
		//TODO:
		SCR.ExecuteLayoutString(Globals.cl.configstrings[Defines.CS_STATUSBAR]);
	}

	/*
	 * ================ SCR_DrawLayout
	 *
	 * ================
	 */

	private static void DrawLayout()
	{
		if (Globals.cl.frame.playerstate.stats[Defines.STAT_LAYOUTS] != 0)
			SCR.ExecuteLayoutString(Globals.cl.layout);
	}

	// =======================================================

	/*
	 * ================== SCR_UpdateScreen
	 *
	 * This is called every frame, and can also be called explicitly to flush
	 * text to the screen. ==================
	 */
	private static readonly float[] separation = { 0, 0 };

	private static void UpdateScreen2()
	{
		int numframes;
		int i;

		// if the screen is disabled (loading plaque is up, or vid mode
		// changing)
		// do nothing at all
		if (Globals.cls.disable_screen != 0)
		{
			if (system.Sys_Milliseconds() - Globals.cls.disable_screen > 120000)
			{
				Globals.cls.disable_screen = 0;
				clientserver.Com_Printf("Loading plaque timed out.\n");
			}

			return;
		}

		if (!SCR.scr_initialized || !Globals.con.initialized)
			return; // not initialized yet

		/*
		 * * range check cl_camera_separation so we don't inadvertently fry
		 * someone's * brain
		 */
		if (Globals.cl_stereo_separation.value > 1.0)
			cvar.Cvar_SetValue("cl_stereo_separation", 1.0f);
		else if (Globals.cl_stereo_separation.value < 0)
			cvar.Cvar_SetValue("cl_stereo_separation", 0.0f);

		if (Globals.cl_stereo.value != 0)
		{
			numframes = 2;
			SCR.separation[0] = -Globals.cl_stereo_separation.value / 2;
			SCR.separation[1] = Globals.cl_stereo_separation.value / 2;
		}
		else
		{
			SCR.separation[0] = 0;
			SCR.separation[1] = 0;
			numframes = 1;
		}

		for (i = 0; i < numframes; i++)
		{
			Globals.re.BeginFrame(SCR.separation[i]);

			if (SCR.scr_draw_loading == 2)
			{
				//  loading plaque over black screen
				var dim = new Size();
				Globals.re.CinematicSetPalette(null);
				SCR.scr_draw_loading = 0; // false
				Globals.re.DrawGetPicSize(ref dim, "loading");
				Globals.re.DrawPic((Globals.viddef.getWidth() - dim.Width) / 2, (Globals.viddef.getHeight() - dim.Height) / 2, "loading");
			}

			// if a cinematic is supposed to be running, handle menus
			// and console specially
			else if (Globals.cl.cinematictime > 0)
			{
				if (Globals.cls.key_dest == Defines.key_menu)
				{
					if (Globals.cl.cinematicpalette_active)
					{
						Globals.re.CinematicSetPalette(null);
						Globals.cl.cinematicpalette_active = false;
					}

					Menu.Draw();
				}
				else if (Globals.cls.key_dest == Defines.key_console)
				{
					if (Globals.cl.cinematicpalette_active)
					{
						Globals.re.CinematicSetPalette(null);
						Globals.cl.cinematicpalette_active = false;
					}

					SCR.DrawConsole();
				}
				else
					SCR.DrawCinematic();
			}
			else
			{
				// make sure the game palette is active
				if (Globals.cl.cinematicpalette_active)
				{
					Globals.re.CinematicSetPalette(null);
					Globals.cl.cinematicpalette_active = false;
				}

				// do 3D refresh drawing, and then update the screen
				SCR.CalcVrect();

				// clear any dirty part of the background
				SCR.TileClear();
				V.RenderView(SCR.separation[i]);
				SCR.DrawStats();

				if ((Globals.cl.frame.playerstate.stats[Defines.STAT_LAYOUTS] & 1) != 0)
					SCR.DrawLayout();

				if ((Globals.cl.frame.playerstate.stats[Defines.STAT_LAYOUTS] & 2) != 0)
					CL_inv.DrawInventory();

				SCR.DrawNet();
				SCR.CheckDrawCenterString();
				SCR.DrawFPS();

				//
				//				if (scr_timegraph->value)
				//					SCR_DebugGraph (cls.frametime*300, 0);
				//
				//				if (scr_debuggraph->value || scr_timegraph->value ||
				// scr_netgraph->value)
				//					SCR_DrawDebugGraph ();
				//
				SCR.DrawPause();
				SCR.DrawConsole();
				Menu.Draw();
				SCR.DrawLoading();
			}
		}

		Globals.re.EndFrame();
	}

	/*
	 * ================= SCR_DrawCrosshair =================
	 */
	public static void DrawCrosshair()
	{
		if (Globals.crosshair.value == 0.0f)
			return;

		if (Globals.crosshair.modified)
		{
			Globals.crosshair.modified = false;
			SCR.TouchPics();
		}

		if (SCR.crosshair_pic.Length == 0)
			return;

		Globals.re.DrawPic(
			Globals.scr_vrect.x + ((Globals.scr_vrect.width - SCR.crosshair_width) >> 1),
			Globals.scr_vrect.y + ((Globals.scr_vrect.height - SCR.crosshair_height) >> 1),
			SCR.crosshair_pic
		);
	}

	private static readonly Action updateScreenCallback = () => { SCR.UpdateScreen2(); };

	// wird anstelle von der richtigen UpdateScreen benoetigt
	public static void UpdateScreen()
	{
		Globals.re.updateScreen(SCR.updateScreenCallback);
	}

	/*
	 * ================= SCR_AddDirtyPoint =================
	 */
	public static void AddDirtyPoint(int x, int y)
	{
		if (x < SCR.scr_dirty.x1)
			SCR.scr_dirty.x1 = x;

		if (x > SCR.scr_dirty.x2)
			SCR.scr_dirty.x2 = x;

		if (y < SCR.scr_dirty.y1)
			SCR.scr_dirty.y1 = y;

		if (y > SCR.scr_dirty.y2)
			SCR.scr_dirty.y2 = y;
	}

	private static int lastframes;
	private static int lasttime;
	private static string fpsvalue = "";

	private static void DrawFPS()
	{
		if (SCR.fps.value > 0.0f)
		{
			if (SCR.fps.modified)
			{
				SCR.fps.modified = false;
				cvar.Cvar_SetValue("cl_maxfps", 1000);
			}

			var diff = Globals.cls.realtime - SCR.lasttime;

			if (diff > (int)(SCR.fps.value * 1000))
			{
				SCR.fpsvalue = (Globals.cls.framecount - SCR.lastframes) * 100000 / diff / 100.0f + " fps";
				SCR.lastframes = Globals.cls.framecount;
				SCR.lasttime = Globals.cls.realtime;
			}

			var x = Globals.viddef.getWidth() - 8 * SCR.fpsvalue.Length - 2;

			for (var i = 0; i < SCR.fpsvalue.Length; i++)
			{
				Globals.re.DrawChar(x, 2, SCR.fpsvalue[i]);
				x += 8;
			}
		}
		else if (SCR.fps.modified)
		{
			SCR.fps.modified = false;
			cvar.Cvar_SetValue("cl_maxfps", 90);
		}
	}

	private static readonly cinematics_t cin = new();

	/**
     * LoadPCX
     */
	private static int LoadPCX(string filename, byte[] palette, cinematics_t cin)
	{
		pcx_t pcx;

		// load the file
		using var raw = new BinaryReader(filesystem.FS_FOpenFile(filename, false));

		if (raw == null)
		{
			VID.Printf(Defines.PRINT_DEVELOPER, "Bad pcx file " + filename + '\n');

			return 0;
		}

		// parse the PCX file
		pcx = new(raw);

		if (pcx.manufacturer != 0x0a || pcx.version != 5 || pcx.encoding != 1 || pcx.bits_per_pixel != 8 || pcx.xmax >= 640 || pcx.ymax >= 480)
		{
			VID.Printf(Defines.PRINT_ALL, "Bad pcx file " + filename + '\n');

			return 0;
		}

		var width = pcx.xmax - pcx.xmin + 1;
		var height = pcx.ymax - pcx.ymin + 1;
		var pix = new byte[width * height];

		if (palette != null)
		{
			// TODO this looks buggy!
			raw.BaseStream.Position = raw.BaseStream.Length;
			raw.Read(palette);
		}

		if (cin != null)
		{
			cin.pic = pix;
			cin.width = width;
			cin.height = height;
		}

		//
		// decode pcx
		//
		var count = 0;
		byte dataByte = 0;
		var runLength = 0;
		int x, y;

		// simple counter for buffer indexing
		var p = 0;

		for (y = 0; y < height; y++)
		{
			for (x = 0; x < width;)
			{
				dataByte = pcx.data[p++];

				if ((dataByte & 0xC0) == 0xC0)
				{
					runLength = dataByte & 0x3F;
					dataByte = pcx.data[p++];

					// write runLength pixel
					while (runLength-- > 0)
					{
						pix[count++] = dataByte;
						x++;
					}
				}
				else
				{
					// write one pixel
					pix[count++] = dataByte;
					x++;
				}
			}
		}

		return width * height;
	}

	/**
     * StopCinematic
     */
	public static void StopCinematic()
	{
		if (SCR.cin.restart_sound)
		{
			// done
			Globals.cl.cinematictime = 0;
			SCR.cin.pic = null;
			SCR.cin.pic_pending = null;

			if (Globals.cl.cinematicpalette_active)
			{
				Globals.re.CinematicSetPalette(null);
				Globals.cl.cinematicpalette_active = false;
			}

			// free the mapped byte buffer
			Globals.cl.cinematic_file?.Dispose();
			Globals.cl.cinematic_file = null;

			if (SCR.cin.hnodes1 != null)
				SCR.cin.hnodes1 = null;

			S.disableStreaming();
			SCR.cin.restart_sound = false;
		}
	}

	/**
     * FinishCinematic
     *
     * Called when either the cinematic completes, or it is aborted
     */
	public static void FinishCinematic()
	{
		// tell the server to advance to the next map / cinematic
		MSG.WriteByte(Globals.cls.netchan.message, Defines.clc_stringcmd);
		SZ.Print(Globals.cls.netchan.message, "nextserver " + Globals.cl.servercount + '\n');
	}

	// ==========================================================================

	/**
     * SmallestNode1
     *
     */
	private static int SmallestNode1(int numhnodes)
	{
		var best = 99999999;
		var bestnode = -1;

		for (var i = 0; i < numhnodes; i++)
		{
			if (SCR.cin.h_used[i] != 0)
				continue;

			if (SCR.cin.h_count[i] == 0)
				continue;

			if (SCR.cin.h_count[i] < best)
			{
				best = SCR.cin.h_count[i];
				bestnode = i;
			}
		}

		if (bestnode == -1)
			return -1;

		SCR.cin.h_used[bestnode] = 1; // true

		return bestnode;
	}

	/**
     * Huff1TableInit
     *
     * Reads the 64k counts table and initializes the node trees.
     *
     */
	private static void Huff1TableInit()
	{
		int[] node;
		var counts = new byte[256];
		int numhnodes;
		SCR.cin.hnodes1 = new int[256 * 256 * 2];
		Array.Fill(SCR.cin.hnodes1, 0);

		for (var prev = 0; prev < 256; prev++)
		{
			Array.Fill(SCR.cin.h_count, 0);
			Array.Fill(SCR.cin.h_used, 0);

			// read a row of counts
			Globals.cl.cinematic_file.Read(counts);

			for (var j = 0; j < 256; j++)
				SCR.cin.h_count[j] = counts[j] & 0xFF;

			// build the nodes
			numhnodes = 256;
			var nodebase = 0 + prev * 256 * 2;
			var index = 0;
			node = SCR.cin.hnodes1;

			while (numhnodes != 511)
			{
				index = nodebase + (numhnodes - 256) * 2;

				// pick two lowest counts
				node[index] = SCR.SmallestNode1(numhnodes);

				if (node[index] == -1)
					break; // no more

				node[index + 1] = SCR.SmallestNode1(numhnodes);

				if (node[index + 1] == -1)
					break;

				SCR.cin.h_count[numhnodes] = SCR.cin.h_count[node[index]] + SCR.cin.h_count[node[index + 1]];
				numhnodes++;
			}

			SCR.cin.numhnodes1[prev] = numhnodes - 1;
		}
	}

	/**
     * Huff1Decompress
     *
     */
	private static byte[] Huff1Decompress(byte[] @in, int size)
	{
		// get decompressed count
		var count = (@in[0] & 0xFF) | ((@in[1] & 0xFF) << 8) | ((@in[2] & 0xFF) << 16) | ((@in[3] & 0xFF) << 24);

		// used as index for in[];
		var input = 4;
		var @out = new byte[count];

		// used as index for out[];
		var out_p = 0;

		// read bits
		var hnodesbase = -256 * 2; // nodes 0-255 aren't stored
		var index = hnodesbase;
		var hnodes = SCR.cin.hnodes1;
		var nodenum = SCR.cin.numhnodes1[0];
		int inbyte;

		while (count != 0)
		{
			inbyte = @in[input++] & 0xFF;

			if (nodenum < 256)
			{
				index = hnodesbase + (nodenum << 9);
				@out[out_p++] = (byte)nodenum;

				if (--count == 0)
					break;

				nodenum = SCR.cin.numhnodes1[nodenum];
			}

			nodenum = hnodes[index + nodenum * 2 + (inbyte & 1)];
			inbyte >>= 1;

			if (nodenum < 256)
			{
				index = hnodesbase + (nodenum << 9);
				@out[out_p++] = (byte)nodenum;

				if (--count == 0)
					break;

				nodenum = SCR.cin.numhnodes1[nodenum];
			}

			nodenum = hnodes[index + nodenum * 2 + (inbyte & 1)];
			inbyte >>= 1;

			if (nodenum < 256)
			{
				index = hnodesbase + (nodenum << 9);
				@out[out_p++] = (byte)nodenum;

				if (--count == 0)
					break;

				nodenum = SCR.cin.numhnodes1[nodenum];
			}

			nodenum = hnodes[index + nodenum * 2 + (inbyte & 1)];
			inbyte >>= 1;

			if (nodenum < 256)
			{
				index = hnodesbase + (nodenum << 9);
				@out[out_p++] = (byte)nodenum;

				if (--count == 0)
					break;

				nodenum = SCR.cin.numhnodes1[nodenum];
			}

			nodenum = hnodes[index + nodenum * 2 + (inbyte & 1)];
			inbyte >>= 1;

			if (nodenum < 256)
			{
				index = hnodesbase + (nodenum << 9);
				@out[out_p++] = (byte)nodenum;

				if (--count == 0)
					break;

				nodenum = SCR.cin.numhnodes1[nodenum];
			}

			nodenum = hnodes[index + nodenum * 2 + (inbyte & 1)];
			inbyte >>= 1;

			if (nodenum < 256)
			{
				index = hnodesbase + (nodenum << 9);
				@out[out_p++] = (byte)nodenum;

				if (--count == 0)
					break;

				nodenum = SCR.cin.numhnodes1[nodenum];
			}

			nodenum = hnodes[index + nodenum * 2 + (inbyte & 1)];
			inbyte >>= 1;

			if (nodenum < 256)
			{
				index = hnodesbase + (nodenum << 9);
				@out[out_p++] = (byte)nodenum;

				if (--count == 0)
					break;

				nodenum = SCR.cin.numhnodes1[nodenum];
			}

			nodenum = hnodes[index + nodenum * 2 + (inbyte & 1)];
			inbyte >>= 1;

			if (nodenum < 256)
			{
				index = hnodesbase + (nodenum << 9);
				@out[out_p++] = (byte)nodenum;

				if (--count == 0)
					break;

				nodenum = SCR.cin.numhnodes1[nodenum];
			}

			nodenum = hnodes[index + nodenum * 2 + (inbyte & 1)];
			inbyte >>= 1;
		}

		if (input != size && input != size + 1)
			clientserver.Com_Printf("Decompression overread by " + (input - size));

		return @out;
	}

	private static readonly byte[] compressed = new byte[0x20000];

	/**
     * ReadNextFrame
     */
	private static byte[] ReadNextFrame()
	{
		var file = Globals.cl.cinematic_file;

		// read the next frame
		var command = file.ReadInt32();

		if (command == 2)
		{
			// last frame marker
			return null;
		}

		if (command == 1)
		{
			// read palette
			file.Read(Globals.cl.cinematicpalette);

			// dubious.... exposes an edge case
			Globals.cl.cinematicpalette_active = false;
		}

		// decompress the next frame
		var size = file.ReadInt32();

		if (size > SCR.compressed.Length || size < 1)
			clientserver.Com_Error(Defines.ERR_DROP, "Bad compressed frame size:" + size);

		file.Read(SCR.compressed, 0, size);

		// read sound
		var start = Globals.cl.cinematicframe * SCR.cin.s_rate / 14;
		var end = (Globals.cl.cinematicframe + 1) * SCR.cin.s_rate / 14;
		var count = end - start;
		var p = file.BaseStream.Position;
		S.RawSamples(count, SCR.cin.s_rate, SCR.cin.s_width, SCR.cin.s_channels, file);

		// skip the sound samples
		file.BaseStream.Position = p + count * SCR.cin.s_width * SCR.cin.s_channels;
		var pic = SCR.Huff1Decompress(SCR.compressed, size);
		Globals.cl.cinematicframe++;

		return pic;
	}

	/**
     * RunCinematic
     */
	public static void RunCinematic()
	{
		if (Globals.cl.cinematictime <= 0)
		{
			SCR.StopCinematic();

			return;
		}

		if (Globals.cl.cinematicframe == -1)
		{
			// static image
			return;
		}

		if (Globals.cls.key_dest != Defines.key_game)
		{
			// pause if menu or console is up
			Globals.cl.cinematictime = Globals.cls.realtime - Globals.cl.cinematicframe * 1000 / 14;

			return;
		}

		var frame = (int)((Globals.cls.realtime - Globals.cl.cinematictime) * 14.0f / 1000);

		if (frame <= Globals.cl.cinematicframe)
			return;

		if (frame > Globals.cl.cinematicframe + 1)
		{
			Com.Println("Dropped frame: " + frame + " > " + (Globals.cl.cinematicframe + 1));
			Globals.cl.cinematictime = Globals.cls.realtime - Globals.cl.cinematicframe * 1000 / 14;
		}

		SCR.cin.pic = SCR.cin.pic_pending;
		SCR.cin.pic_pending = SCR.ReadNextFrame();

		if (SCR.cin.pic_pending == null)
		{
			SCR.StopCinematic();
			SCR.FinishCinematic();

			// hack to get the black screen behind loading
			Globals.cl.cinematictime = 1;
			SCR.BeginLoadingPlaque();
			Globals.cl.cinematictime = 0;

			return;
		}
	}

	/**
     * DrawCinematic
     *
     * Returns true if a cinematic is active, meaning the view rendering should
     * be skipped.
     */
	private static bool DrawCinematic()
	{
		if (Globals.cl.cinematictime <= 0)
			return false;

		if (Globals.cls.key_dest == Defines.key_menu)
		{
			// blank screen and pause if menu is up
			Globals.re.CinematicSetPalette(null);
			Globals.cl.cinematicpalette_active = false;

			return true;
		}

		if (!Globals.cl.cinematicpalette_active)
		{
			Globals.re.CinematicSetPalette(Globals.cl.cinematicpalette);
			Globals.cl.cinematicpalette_active = true;
		}

		if (SCR.cin.pic == null)
			return true;

		Globals.re.DrawStretchRaw(0, 0, Globals.viddef.getWidth(), Globals.viddef.getHeight(), SCR.cin.width, SCR.cin.height, SCR.cin.pic);

		return true;
	}

	/**
     * PlayCinematic
     */
	public static void PlayCinematic(string arg)
	{
		// make sure CD isn't playing music
		//CDAudio.Stop();
		Globals.cl.cinematicframe = 0;

		if (arg.EndsWith(".pcx"))
		{
			// static pcx image
			var name1 = "pics/" + arg;
			var size = SCR.LoadPCX(name1, Globals.cl.cinematicpalette, SCR.cin);
			Globals.cl.cinematicframe = -1;
			Globals.cl.cinematictime = 1;
			SCR.EndLoadingPlaque();
			Globals.cls.state = Defines.ca_active;

			if (size == 0 || SCR.cin.pic == null)
			{
				Com.Println(name1 + " not found.");
				Globals.cl.cinematictime = 0;
			}

			return;
		}

		var name = "video/" + arg;
		Globals.cl.cinematic_file = new BinaryReader(filesystem.FS_FOpenFile(name, false));

		if (Globals.cl.cinematic_file == null)
		{
			//clientserver.Com_Error(ERR_DROP, "Cinematic " + name + " not found.\n");
			SCR.FinishCinematic();

			// done
			Globals.cl.cinematictime = 0;

			return;
		}

		SCR.EndLoadingPlaque();
		Globals.cls.state = Defines.ca_active;
		var file = Globals.cl.cinematic_file;
		SCR.cin.width = file.ReadInt32();
		SCR.cin.height = file.ReadInt32();
		SCR.cin.s_rate = file.ReadInt32();
		SCR.cin.s_width = file.ReadInt32();
		SCR.cin.s_channels = file.ReadInt32();
		SCR.Huff1TableInit();
		SCR.cin.restart_sound = true;
		Globals.cl.cinematicframe = 0;
		SCR.cin.pic = SCR.ReadNextFrame();
		Globals.cl.cinematictime = system.Sys_Milliseconds();
	}
}
