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
namespace Quake2Sharp.client;

using game;
using qcommon;
using System;
using System.IO;
using System.Text;
using util;

/**
 * Console
 */
public class Console
{
	public static Action ToggleConsole_f = () =>
	{
		SCR.EndLoadingPlaque(); // get rid of loading plaque

		if (Globals.cl.attractloop)
		{
			Cbuf.AddText("killserver\n");

			return;
		}

		if (Globals.cls.state == Defines.ca_disconnected)
		{
			// start the demo loop again
			Cbuf.AddText("d1\n");

			return;
		}

		Key.ClearTyping();
		Console.ClearNotify();

		if (Globals.cls.key_dest == Defines.key_console)
		{
			Menu.ForceMenuOff();
			Cvar.Set("paused", "0");
		}
		else
		{
			Menu.ForceMenuOff();
			Globals.cls.key_dest = Defines.key_console;

			if (Cvar.VariableValue("maxclients") == 1 && Globals.server_state != 0)
				Cvar.Set("paused", "1");
		}
	};

	public static Action Clear_f = () => { Array.Fill(Globals.con.text, (byte)' '); };

	public static Action Dump_f = () =>
	{
		int l, x;
		int line;
		Stream f;
		var buffer = new byte[1024];
		string name;

		if (Cmd.Argc() != 2)
		{
			Com.Printf("usage: condump <filename>\n");

			return;
		}

		// Com_sprintf (name, sizeof(name), "%s/%s.txt", FS_Gamedir(),
		// Cmd_Argv(1));
		name = FS.Gamedir() + "/" + Cmd.Argv(1) + ".txt";
		Com.Printf("Dumped console text to " + name + ".\n");
		FS.CreatePath(name);
		f = File.OpenWrite(name);

		if (f == null)
		{
			Com.Printf("ERROR: couldn't open.\n");

			return;
		}

		// skip empty lines
		for (l = Globals.con.current - Globals.con.totallines + 1; l <= Globals.con.current; l++)
		{
			line = l % Globals.con.totallines * Globals.con.linewidth;

			for (x = 0; x < Globals.con.linewidth; x++)
			{
				if (Globals.con.text[line + x] != ' ')
					break;
			}

			if (x != Globals.con.linewidth)
				break;
		}

		// write the remaining lines
		buffer[Globals.con.linewidth] = 0;

		for (; l <= Globals.con.current; l++)
		{
			line = l % Globals.con.totallines * Globals.con.linewidth;

			// strncpy (buffer, line, con.linewidth);
			Array.Copy(Globals.con.text, line, buffer, 0, Globals.con.linewidth);

			for (x = Globals.con.linewidth - 1; x >= 0; x--)
			{
				if (buffer[x] == ' ')
					buffer[x] = 0;
				else
					break;
			}

			for (x = 0; buffer[x] != 0; x++)
				buffer[x] &= 0x7f;

			buffer[x] = (byte)'\n';

			// fprintf (f, "%s\n", buffer);
			try
			{
				f.Write(buffer, 0, x + 1);
			}
			catch (Exception)
			{
			}
		}

		f.Close();
	};

	/**
     * 
     */
	public static void Init()
	{
		Globals.con.linewidth = -1;
		Console.CheckResize();
		Com.Printf("Console initialized.\n");

		//
		// register our commands
		//
		Globals.con_notifytime = Cvar.Get("con_notifytime", "3", 0);
		Cmd.AddCommand("toggleconsole", Console.ToggleConsole_f);
		Cmd.AddCommand("togglechat", Console.ToggleChat_f);
		Cmd.AddCommand("messagemode", Console.MessageMode_f);
		Cmd.AddCommand("messagemode2", Console.MessageMode2_f);
		Cmd.AddCommand("clear", Console.Clear_f);
		Cmd.AddCommand("condump", Console.Dump_f);
		Globals.con.initialized = true;
	}

	/**
     * If the line width has changed, reformat the buffer.
     */
	public static void CheckResize()
	{
		var width = (Globals.viddef.getWidth() >> 3) - 2;

		if (width > Defines.MAXCMDLINE)
			width = Defines.MAXCMDLINE;

		if (width == Globals.con.linewidth)
			return;

		if (width < 1)
		{
			// video hasn't been initialized yet
			width = 38;
			Globals.con.linewidth = width;
			Globals.con.totallines = Defines.CON_TEXTSIZE / Globals.con.linewidth;
			Array.Fill(Globals.con.text, (byte)' ');
		}
		else
		{
			var oldwidth = Globals.con.linewidth;
			Globals.con.linewidth = width;
			var oldtotallines = Globals.con.totallines;
			Globals.con.totallines = Defines.CON_TEXTSIZE / Globals.con.linewidth;
			var numlines = oldtotallines;

			if (Globals.con.totallines < numlines)
				numlines = Globals.con.totallines;

			var numchars = oldwidth;

			if (Globals.con.linewidth < numchars)
				numchars = Globals.con.linewidth;

			var tbuf = new byte[Defines.CON_TEXTSIZE];
			Array.Copy(Globals.con.text, 0, tbuf, 0, Defines.CON_TEXTSIZE);
			Array.Fill(Globals.con.text, (byte)' ');

			for (var i = 0; i < numlines; i++)
			{
				for (var j = 0; j < numchars; j++)
				{
					Globals.con.text[(Globals.con.totallines - 1 - i) * Globals.con.linewidth + j] =
						tbuf[(Globals.con.current - i + oldtotallines) % oldtotallines * oldwidth + j];
				}
			}

			Console.ClearNotify();
		}

		Globals.con.current = Globals.con.totallines - 1;
		Globals.con.display = Globals.con.current;
	}

	public static void ClearNotify()
	{
		int i;

		for (i = 0; i < Defines.NUM_CON_TIMES; i++)
			Globals.con.times[i] = 0;
	}

	public static void DrawString(int x, int y, string s)
	{
		for (var i = 0; i < s.Length; i++)
		{
			Globals.re.DrawChar(x, y, s[i]);
			x += 8;
		}
	}

	public static void DrawAltString(int x, int y, string s)
	{
		for (var i = 0; i < s.Length; i++)
		{
			Globals.re.DrawChar(x, y, s[i] ^ 0x80);
			x += 8;
		}
	}

	/*
	 * ================ Con_ToggleChat_f ================
	 */
	private static readonly Action ToggleChat_f = () =>
	{
		Key.ClearTyping();

		if (Globals.cls.key_dest == Defines.key_console)
		{
			if (Globals.cls.state == Defines.ca_active)
			{
				Menu.ForceMenuOff();
				Globals.cls.key_dest = Defines.key_game;
			}
		}
		else
			Globals.cls.key_dest = Defines.key_console;

		Console.ClearNotify();
	};

	/*
	 * ================ Con_MessageMode_f ================
	 */
	private static readonly Action MessageMode_f = () =>
	{
		Globals.chat_team = false;
		Globals.cls.key_dest = Defines.key_message;
	};

	/*
	 * ================ Con_MessageMode2_f ================
	 */
	private static readonly Action MessageMode2_f = () =>
	{
		Globals.chat_team = true;
		Globals.cls.key_dest = Defines.key_message;
	};

	/*
	 * =============== Con_Linefeed ===============
	 */
	private static void Linefeed()
	{
		Globals.con.x = 0;

		if (Globals.con.display == Globals.con.current)
			Globals.con.display++;

		Globals.con.current++;
		var i = Globals.con.current % Globals.con.totallines * Globals.con.linewidth;
		var e = i + Globals.con.linewidth;

		while (++i < e)
			Globals.con.text[i] = (byte)' ';
	}

	/*
	 * ================ Con_Print
	 * 
	 * Handles cursor positioning, line wrapping, etc All console printing must
	 * go through this in order to be logged to disk If no console is visible,
	 * the text will appear at the top of the game window ================
	 */
	private static int cr;

	public static void Print(string txt)
	{
		int y;
		int c, l;
		int mask;
		var txtpos = 0;

		if (!Globals.con.initialized)
			return;

		if (txt[0] == 1 || txt[0] == 2)
		{
			mask = 128; // go to colored text
			txtpos++;
		}
		else
			mask = 0;

		while (txtpos < txt.Length)
		{
			c = txt[txtpos];

			// count word length
			for (l = 0; l < Globals.con.linewidth && l < txt.Length - txtpos; l++)
			{
				if (txt[l + txtpos] <= ' ')
					break;
			}

			// word wrap
			if (l != Globals.con.linewidth && Globals.con.x + l > Globals.con.linewidth)
				Globals.con.x = 0;

			txtpos++;

			if (Console.cr != 0)
			{
				Globals.con.current--;
				Console.cr = 0;
			}

			if (Globals.con.x == 0)
			{
				Console.Linefeed();

				// mark time for transparent overlay
				if (Globals.con.current >= 0)
					Globals.con.times[Globals.con.current % Defines.NUM_CON_TIMES] = Globals.cls.realtime;
			}

			switch (c)
			{
				case '\n':
					Globals.con.x = 0;

					break;

				case '\r':
					Globals.con.x = 0;
					Console.cr = 1;

					break;

				default: // display character and advance
					y = Globals.con.current % Globals.con.totallines;
					Globals.con.text[y * Globals.con.linewidth + Globals.con.x] = (byte)(c | mask | Globals.con.ormask);
					Globals.con.x++;

					if (Globals.con.x >= Globals.con.linewidth)
						Globals.con.x = 0;

					break;
			}
		}
	}

	/*
	 * ============== Con_CenteredPrint ==============
	 */
	private static void CenteredPrint(string text)
	{
		var l = text.Length;
		l = (Globals.con.linewidth - l) / 2;

		if (l < 0)
			l = 0;

		StringBuilder sb = new(1024);

		for (var i = 0; i < l; i++)
			sb.Append(' ');

		sb.Append(text);
		sb.Append('\n');
		sb.Length = 1024;
		Console.Print(sb.ToString());
	}

	/*
	 * ==============================================================================
	 * 
	 * DRAWING
	 * 
	 * ==============================================================================
	 */

	/*
	 * ================ Con_DrawInput
	 * 
	 * The input line scrolls horizontally if typing goes beyond the right edge
	 * ================
	 */
	private static void DrawInput()
	{
		int i;
		byte[] text;
		var start = 0;

		if (Globals.cls.key_dest == Defines.key_menu)
			return;

		if (Globals.cls.key_dest != Defines.key_console && Globals.cls.state == Defines.ca_active)
			return; // don't draw anything (always draw if not active)

		text = Globals.key_lines[Globals.edit_line];

		// add the cursor frame
		text[Globals.key_linepos] = (byte)(10 + ((int)(Globals.cls.realtime >> 8) & 1));

		// fill out remainder with spaces
		for (i = Globals.key_linepos + 1; i < Globals.con.linewidth; i++)
			text[i] = (byte)' ';

		// prestep if horizontally scrolling
		if (Globals.key_linepos >= Globals.con.linewidth)
			start += 1 + Globals.key_linepos - Globals.con.linewidth;

		// draw it
		// y = con.vislines-16;

		for (i = 0; i < Globals.con.linewidth; i++)
			Globals.re.DrawChar((i + 1) << 3, Globals.con.vislines - 22, text[i]);

		// remove cursor
		Globals.key_lines[Globals.edit_line][Globals.key_linepos] = 0;
	}

	/*
	 * ================ Con_DrawNotify
	 * 
	 * Draws the last few lines of output transparently over the game top
	 * ================
	 */
	public static void DrawNotify()
	{
		int x, v;
		int text;
		int i;
		int time;
		string s;
		int skip;
		v = 0;

		for (i = Globals.con.current - Defines.NUM_CON_TIMES + 1; i <= Globals.con.current; i++)
		{
			if (i < 0)
				continue;

			time = (int)Globals.con.times[i % Defines.NUM_CON_TIMES];

			if (time == 0)
				continue;

			time = (int)(Globals.cls.realtime - time);

			if (time > Globals.con_notifytime.value * 1000)
				continue;

			text = i % Globals.con.totallines * Globals.con.linewidth;

			for (x = 0; x < Globals.con.linewidth; x++)
				Globals.re.DrawChar((x + 1) << 3, v, Globals.con.text[text + x]);

			v += 8;
		}

		if (Globals.cls.key_dest == Defines.key_message)
		{
			if (Globals.chat_team)
			{
				Console.DrawString(8, v, "say_team:");
				skip = 11;
			}
			else
			{
				Console.DrawString(8, v, "say:");
				skip = 5;
			}

			s = Globals.chat_buffer;

			if (Globals.chat_bufferlen > (Globals.viddef.getWidth() >> 3) - (skip + 1))
				s = s[(Globals.chat_bufferlen - ((Globals.viddef.getWidth() >> 3) - (skip + 1)))..];

			for (x = 0; x < s.Length; x++)
				Globals.re.DrawChar((x + skip) << 3, v, s[x]);

			Globals.re.DrawChar((x + skip) << 3, v, (int)(10 + ((Globals.cls.realtime >> 8) & 1)));
			v += 8;
		}

		if (v != 0)
		{
			SCR.AddDirtyPoint(0, 0);
			SCR.AddDirtyPoint(Globals.viddef.getWidth() - 1, v);
		}
	}

	/*
	 * ================ Con_DrawConsole
	 * 
	 * Draws the console with the solid background ================
	 */
	public static void DrawConsole(float frac)
	{
		var width = Globals.viddef.getWidth();
		var height = Globals.viddef.getHeight();
		var lines = (int)(height * frac);

		if (lines <= 0)
			return;

		if (lines > height)
			lines = height;

		// draw the background
		Globals.re.DrawStretchPic(0, -height + lines, width, height, "conback");
		SCR.AddDirtyPoint(0, 0);
		SCR.AddDirtyPoint(width - 1, lines - 1);
		var version = Com.sprintf("v%4.2f", Globals.VERSION);

		for (var i1 = 0; i1 < 5; i1++)
			Globals.re.DrawChar(width - 44 + i1 * 8, lines - 12, 128 + version[i1]);

		// draw the text
		Globals.con.vislines = lines;
		var rows = (lines - 22) >> 3; // rows of text to draw
		var y = lines - 30;

		// draw from the bottom up
		if (Globals.con.display != Globals.con.current)
		{
			// draw arrows to show the buffer is backscrolled
			for (var i1 = 0; i1 < Globals.con.linewidth; i1 += 4)
				Globals.re.DrawChar((i1 + 1) << 3, y, '^');

			y -= 8;
			rows--;
		}

		int i, j, x, n;
		var row = Globals.con.display;

		for (i = 0; i < rows; i++, y -= 8, row--)
		{
			if (row < 0)
				break;

			if (Globals.con.current - row >= Globals.con.totallines)
				break; // past scrollback wrap point

			var first = row % Globals.con.totallines * Globals.con.linewidth;

			for (x = 0; x < Globals.con.linewidth; x++)
				Globals.re.DrawChar((x + 1) << 3, y, Globals.con.text[x + first]);
		}

		// ZOID
		// draw the download bar
		// figure out width
		if (Globals.cls.download != null)
		{
			int text;

			if ((text = Globals.cls.downloadname.LastIndexOf('/')) != 0)
				text++;
			else
				text = 0;

			x = Globals.con.linewidth - Globals.con.linewidth * 7 / 40;
			y = x - (Globals.cls.downloadname.Length - text) - 8;
			i = Globals.con.linewidth / 3;
			StringBuilder dlbar = new(512);

			if (Globals.cls.downloadname.Length - text > i)
			{
				y = x - i - 11;
				var end = text + i - 1;
				;
				dlbar.Append(Globals.cls.downloadname.Substring(text, i - 1));
				dlbar.Append("...");
			}
			else
				dlbar.Append(Globals.cls.downloadname[text..]);

			dlbar.Append(": ");
			dlbar.Append((char)0x80);

			// where's the dot go?
			if (Globals.cls.downloadpercent == 0)
				n = 0;
			else
				n = y * Globals.cls.downloadpercent / 100;

			for (j = 0; j < y; j++)
			{
				if (j == n)
					dlbar.Append((char)0x83);
				else
					dlbar.Append((char)0x81);
			}

			dlbar.Append((char)0x82);
			dlbar.Append(Globals.cls.downloadpercent < 10 ? " 0" : " ");
			dlbar.Append(Globals.cls.downloadpercent).Append('%');

			// draw it
			y = Globals.con.vislines - 12;

			for (i = 0; i < dlbar.Length; i++)
				Globals.re.DrawChar((i + 1) << 3, y, dlbar[i]);
		}

		// ZOID

		// draw the input prompt, user text, and cursor if desired
		Console.DrawInput();
	}
}