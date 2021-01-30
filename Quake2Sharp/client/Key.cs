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
namespace Quake2Sharp.client
{
	using game;
	using qcommon;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;
	using util;

	/**
 * Key
 */
	public class Key
	{
		//
		// these are the key numbers that should be passed to Key_Event
		//
		public const int K_TAB = 9;
		public const int K_ENTER = 13;
		public const int K_ESCAPE = 27;
		public const int K_SPACE = 32;

		// normal keys should be passed as lowercased ascii
		public const int K_BACKSPACE = 127;
		public const int K_UPARROW = 128;
		public const int K_DOWNARROW = 129;
		public const int K_LEFTARROW = 130;
		public const int K_RIGHTARROW = 131;
		public const int K_ALT = 132;
		public const int K_CTRL = 133;
		public const int K_SHIFT = 134;
		public const int K_F1 = 135;
		public const int K_F2 = 136;
		public const int K_F3 = 137;
		public const int K_F4 = 138;
		public const int K_F5 = 139;
		public const int K_F6 = 140;
		public const int K_F7 = 141;
		public const int K_F8 = 142;
		public const int K_F9 = 143;
		public const int K_F10 = 144;
		public const int K_F11 = 145;
		public const int K_F12 = 146;
		public const int K_INS = 147;
		public const int K_DEL = 148;
		public const int K_PGDN = 149;
		public const int K_PGUP = 150;
		public const int K_HOME = 151;
		public const int K_END = 152;
		public const int K_KP_HOME = 160;
		public const int K_KP_UPARROW = 161;
		public const int K_KP_PGUP = 162;
		public const int K_KP_LEFTARROW = 163;
		public const int K_KP_5 = 164;
		public const int K_KP_RIGHTARROW = 165;
		public const int K_KP_END = 166;
		public const int K_KP_DOWNARROW = 167;
		public const int K_KP_PGDN = 168;
		public const int K_KP_ENTER = 169;
		public const int K_KP_INS = 170;
		public const int K_KP_DEL = 171;
		public const int K_KP_SLASH = 172;
		public const int K_KP_MINUS = 173;
		public const int K_KP_PLUS = 174;
		public static readonly int K_PAUSE = 255;

		//
		// mouse buttons generate virtual keys
		//
		public const int K_MOUSE1 = 200;
		public const int K_MOUSE2 = 201;
		public const int K_MOUSE3 = 202;

		//
		// joystick buttons
		//
		public const int K_JOY1 = 203;
		public const int K_JOY2 = 204;
		public const int K_JOY3 = 205;
		public const int K_JOY4 = 206;
		public const int K_MWHEELDOWN = 239;
		public const int K_MWHEELUP = 240;
		public static int anykeydown;
		private static int key_waiting;
		private static int history_line;
		private static bool shift_down;
		private static readonly int[] key_repeats = new int[256];

		//static int[] keyshift = new int[256];
		private static readonly bool[] menubound = new bool[256];
		private static readonly bool[] consolekeys = new bool[256];
		private static readonly string[] keynames = new string[256];

		static Key()
		{
			Key.keynames[Key.K_TAB] = "TAB";
			Key.keynames[Key.K_ENTER] = "ENTER";
			Key.keynames[Key.K_ESCAPE] = "ESCAPE";
			Key.keynames[Key.K_SPACE] = "SPACE";
			Key.keynames[Key.K_BACKSPACE] = "BACKSPACE";
			Key.keynames[Key.K_UPARROW] = "UPARROW";
			Key.keynames[Key.K_DOWNARROW] = "DOWNARROW";
			Key.keynames[Key.K_LEFTARROW] = "LEFTARROW";
			Key.keynames[Key.K_RIGHTARROW] = "RIGHTARROW";
			Key.keynames[Key.K_ALT] = "ALT";
			Key.keynames[Key.K_CTRL] = "CTRL";
			Key.keynames[Key.K_SHIFT] = "SHIFT";
			Key.keynames[Key.K_F1] = "F1";
			Key.keynames[Key.K_F2] = "F2";
			Key.keynames[Key.K_F3] = "F3";
			Key.keynames[Key.K_F4] = "F4";
			Key.keynames[Key.K_F5] = "F5";
			Key.keynames[Key.K_F6] = "F6";
			Key.keynames[Key.K_F7] = "F7";
			Key.keynames[Key.K_F8] = "F8";
			Key.keynames[Key.K_F9] = "F9";
			Key.keynames[Key.K_F10] = "F10";
			Key.keynames[Key.K_F11] = "F11";
			Key.keynames[Key.K_F12] = "F12";
			Key.keynames[Key.K_INS] = "INS";
			Key.keynames[Key.K_DEL] = "DEL";
			Key.keynames[Key.K_PGDN] = "PGDN";
			Key.keynames[Key.K_PGUP] = "PGUP";
			Key.keynames[Key.K_HOME] = "HOME";
			Key.keynames[Key.K_END] = "END";
			Key.keynames[Key.K_MOUSE1] = "MOUSE1";
			Key.keynames[Key.K_MOUSE2] = "MOUSE2";
			Key.keynames[Key.K_MOUSE3] = "MOUSE3";

			//	00092         {"JOY1", K_JOY1},
			//	00093         {"JOY2", K_JOY2},
			//	00094         {"JOY3", K_JOY3},
			//	00095         {"JOY4", K_JOY4},
			Key.keynames[Key.K_KP_HOME] = "KP_HOME";
			Key.keynames[Key.K_KP_UPARROW] = "KP_UPARROW";
			Key.keynames[Key.K_KP_PGUP] = "KP_PGUP";
			Key.keynames[Key.K_KP_LEFTARROW] = "KP_LEFTARROW";
			Key.keynames[Key.K_KP_5] = "KP_5";
			Key.keynames[Key.K_KP_RIGHTARROW] = "KP_RIGHTARROW";
			Key.keynames[Key.K_KP_END] = "KP_END";
			Key.keynames[Key.K_KP_DOWNARROW] = "KP_DOWNARROW";
			Key.keynames[Key.K_KP_PGDN] = "KP_PGDN";
			Key.keynames[Key.K_KP_ENTER] = "KP_ENTER";
			Key.keynames[Key.K_KP_INS] = "KP_INS";
			Key.keynames[Key.K_KP_DEL] = "KP_DEL";
			Key.keynames[Key.K_KP_SLASH] = "KP_SLASH";
			Key.keynames[Key.K_KP_PLUS] = "KP_PLUS";
			Key.keynames[Key.K_KP_MINUS] = "KP_MINUS";
			Key.keynames[Key.K_MWHEELUP] = "MWHEELUP";
			Key.keynames[Key.K_MWHEELDOWN] = "MWHEELDOWN";
			Key.keynames[Key.K_PAUSE] = "PAUSE";
			Key.keynames[';'] = "SEMICOLON"; // because a raw semicolon seperates commands
			Key.keynames[0] = "NULL";
		}

		/**
	 * 
	 */
		public static void Init()
		{
			for (var i = 0; i < 32; i++)
			{
				Globals.key_lines[i][0] = (byte)']';
				Globals.key_lines[i][1] = 0;
			}

			Globals.key_linepos = 1;

			//
			// init ascii characters in console mode
			//
			for (var i = 32; i < 128; i++)
				Key.consolekeys[i] = true;

			Key.consolekeys[Key.K_ENTER] = true;
			Key.consolekeys[Key.K_KP_ENTER] = true;
			Key.consolekeys[Key.K_TAB] = true;
			Key.consolekeys[Key.K_LEFTARROW] = true;
			Key.consolekeys[Key.K_KP_LEFTARROW] = true;
			Key.consolekeys[Key.K_RIGHTARROW] = true;
			Key.consolekeys[Key.K_KP_RIGHTARROW] = true;
			Key.consolekeys[Key.K_UPARROW] = true;
			Key.consolekeys[Key.K_KP_UPARROW] = true;
			Key.consolekeys[Key.K_DOWNARROW] = true;
			Key.consolekeys[Key.K_KP_DOWNARROW] = true;
			Key.consolekeys[Key.K_BACKSPACE] = true;
			Key.consolekeys[Key.K_HOME] = true;
			Key.consolekeys[Key.K_KP_HOME] = true;
			Key.consolekeys[Key.K_END] = true;
			Key.consolekeys[Key.K_KP_END] = true;
			Key.consolekeys[Key.K_PGUP] = true;
			Key.consolekeys[Key.K_KP_PGUP] = true;
			Key.consolekeys[Key.K_PGDN] = true;
			Key.consolekeys[Key.K_KP_PGDN] = true;
			Key.consolekeys[Key.K_SHIFT] = true;
			Key.consolekeys[Key.K_INS] = true;
			Key.consolekeys[Key.K_KP_INS] = true;
			Key.consolekeys[Key.K_KP_DEL] = true;
			Key.consolekeys[Key.K_KP_SLASH] = true;
			Key.consolekeys[Key.K_KP_PLUS] = true;
			Key.consolekeys[Key.K_KP_MINUS] = true;
			Key.consolekeys[Key.K_KP_5] = true;
			Key.consolekeys['`'] = false;
			Key.consolekeys['~'] = false;

			//		for (int i = 0; i < 256; i++)
			//			keyshift[i] = i;
			//		for (int i = 'a'; i <= 'z'; i++)
			//			keyshift[i] = i - 'a' + 'A';
			//		keyshift['1'] = '!';
			//		keyshift['2'] = '@';
			//		keyshift['3'] = '#';
			//		keyshift['4'] = '$';
			//		keyshift['5'] = '%';
			//		keyshift['6'] = '^';
			//		keyshift['7'] = '&';
			//		keyshift['8'] = '*';
			//		keyshift['9'] = '(';
			//		keyshift['0'] = ')';
			//		keyshift['-'] = '_';
			//		keyshift['='] = '+';
			//		keyshift[','] = '<';
			//		keyshift['.'] = '>';
			//		keyshift['/'] = '?';
			//		keyshift[';'] = ':';
			//		keyshift['\''] = '"';
			//		keyshift['['] = '{';
			//		keyshift[']'] = '}';
			//		keyshift['`'] = '~';
			//		keyshift['\\'] = '|';
			Key.menubound[Key.K_ESCAPE] = true;

			for (var i = 0; i < 12; i++)
				Key.menubound[Key.K_F1 + i] = true;

			//
			// register our functions
			//
			Cmd.AddCommand("bind", Key.Bind_f);
			Cmd.AddCommand("unbind", Key.Unbind_f);
			Cmd.AddCommand("unbindall", Key.Unbindall_f);
			Cmd.AddCommand("bindlist", Key.Bindlist_f);
		}

		public static void ClearTyping()
		{
			Globals.key_lines[Globals.edit_line][1] = 0; // clear any typing
			Globals.key_linepos = 1;
		}

		/**
	 * Called by the system between frames for both key up and key down events.
	 */
		public static void Event(int key, bool down, int time)
		{
			string kb;
			string cmd;

			// hack for modal presses
			if (Key.key_waiting == -1)
			{
				if (down)
					Key.key_waiting = key;

				return;
			}

			// update auto-repeat status
			if (down)
			{
				Key.key_repeats[key]++;

				if (Key.key_repeats[key] > 1 && Globals.cls.key_dest == Defines.key_game && !(Globals.cls.state == Defines.ca_disconnected))
					return; // ignore most autorepeats

				if (key >= 200 && Globals.keybindings[key] == null)
					Com.Printf(Key.KeynumToString(key) + " is unbound, hit F4 to set.\n");
			}
			else
				Key.key_repeats[key] = 0;

			if (key == Key.K_SHIFT)
				Key.shift_down = down;

			// console key is hardcoded, so the user can never unbind it
			if (key == '`' || key == '~')
			{
				if (!down)
					return;

				client.Console.ToggleConsole_f();

				return;
			}

			// any key during the attract mode will bring up the menu
			if (Globals.cl.attractloop && Globals.cls.key_dest != Defines.key_menu && !(key >= Key.K_F1 && key <= Key.K_F12))
				key = Key.K_ESCAPE;

			// menu key is hardcoded, so the user can never unbind it
			if (key == Key.K_ESCAPE)
			{
				if (!down)
					return;

				if (Globals.cl.frame.playerstate.stats[Defines.STAT_LAYOUTS] != 0 && Globals.cls.key_dest == Defines.key_game)
				{
					// put away help computer / inventory
					Cbuf.AddText("cmd putaway\n");

					return;
				}

				switch (Globals.cls.key_dest)
				{
					case Defines.key_message:
						Key.Message(key);

						break;

					case Defines.key_menu:
						Menu.Keydown(key);

						break;

					case Defines.key_game:
					case Defines.key_console:
						Menu.Menu_Main_f();

						break;

					default:
						Com.Error(Defines.ERR_FATAL, "Bad cls.key_dest");

						break;
				}

				return;
			}

			// track if any key is down for BUTTON_ANY
			Globals.keydown[key] = down;

			if (down)
			{
				if (Key.key_repeats[key] == 1)
					Key.anykeydown++;
			}
			else
			{
				Key.anykeydown--;

				if (Key.anykeydown < 0)
					Key.anykeydown = 0;
			}

			//
			// key up events only generate commands if the game key binding is
			// a button command (leading + sign).  These will occur even in console mode,
			// to keep the character from continuing an action started before a console
			// switch.  Button commands include the kenum as a parameter, so multiple
			// downs can be matched with ups
			//
			if (!down)
			{
				kb = Globals.keybindings[key];

				if (kb != null && kb.Length > 0 && kb[0] == '+')
				{
					cmd = "-" + kb[1..] + " " + key + " " + time + "\n";
					Cbuf.AddText(cmd);
				}

				//			if (keyshift[key] != key) {
				//				kb = Globals.keybindings[keyshift[key]];
				//				if (kb != null && kb.length()>0 && kb.charAt(0) == '+') {
				//					cmd = "-" + kb.substring(1) + " " + key + " " + time + "\n";
				//					Cbuf.AddText(cmd);
				//				}
				//			}
				return;
			}

			//
			// if not a consolekey, send to the interpreter no matter what mode is
			//
			if ((Globals.cls.key_dest == Defines.key_menu && Key.menubound[key])
				|| (Globals.cls.key_dest == Defines.key_console && !Key.consolekeys[key])
				|| (Globals.cls.key_dest == Defines.key_game && (Globals.cls.state == Defines.ca_active || !Key.consolekeys[key])))
			{
				kb = Globals.keybindings[key];

				if (kb != null)
				{
					if (kb.Length > 0 && kb[0] == '+')
					{
						// button commands add keynum and time as a parm
						cmd = kb + " " + key + " " + time + "\n";
						Cbuf.AddText(cmd);
					}
					else
						Cbuf.AddText(kb + "\n");
				}

				return;
			}

			if (!down)
				return; // other systems only care about key down events

			//		if (shift_down)
			//			key = keyshift[key];

			switch (Globals.cls.key_dest)
			{
				case Defines.key_message:
					Key.Message(key);

					break;

				case Defines.key_menu:
					Menu.Keydown(key);

					break;

				case Defines.key_game:
				case Defines.key_console:
					Key.Console(key);

					break;

				default:
					Com.Error(Defines.ERR_FATAL, "Bad cls.key_dest");

					break;
			}
		}

		/**
	 * Returns a string (either a single ascii char, or a K_* name) for the 
	 * given keynum.
	 */
		public static string KeynumToString(int keynum)
		{
			if (keynum < 0 || keynum > 255)
				return "<KEY NOT FOUND>";

			if (keynum > 32 && keynum < 127)
				return char.ToString((char)keynum);

			if (Key.keynames[keynum] != null)
				return Key.keynames[keynum];

			return "<UNKNOWN KEYNUM>";
		}

		/**
	 * Returns a key number to be used to index keybindings[] by looking at
	 * the given string. Single ascii characters return themselves, while
	 * the K_* names are matched up.
	 */
		private static int StringToKeynum(string str)
		{
			if (str == null)
				return -1;

			if (str.Length == 1)
				return str[0];

			for (var i = 0; i < Key.keynames.Length; i++)
			{
				if (str.Equals(Key.keynames[i], StringComparison.OrdinalIgnoreCase))
					return i;
			}

			return -1;
		}

		public static void Message(int key)
		{
			if (key == Key.K_ENTER || key == Key.K_KP_ENTER)
			{
				if (Globals.chat_team)
					Cbuf.AddText("say_team \"");
				else
					Cbuf.AddText("say \"");

				Cbuf.AddText(Globals.chat_buffer);
				Cbuf.AddText("\"\n");
				Globals.cls.key_dest = Defines.key_game;
				Globals.chat_buffer = "";

				return;
			}

			if (key == Key.K_ESCAPE)
			{
				Globals.cls.key_dest = Defines.key_game;
				Globals.chat_buffer = "";

				return;
			}

			if (key < 32 || key > 127)
				return; // non printable

			if (key == Key.K_BACKSPACE)
			{
				if (Globals.chat_buffer.Length > 2)
					Globals.chat_buffer = Globals.chat_buffer[..(Globals.chat_buffer.Length - 2)];
				else
					Globals.chat_buffer = "";

				return;
			}

			if (Globals.chat_buffer.Length > Defines.MAXCMDLINE)
				return; // all full

			Globals.chat_buffer += (char)key;
		}

		/**
	 * Interactive line editing and console scrollback.
	 */
		public static void Console(int key)
		{
			switch (key)
			{
				case Key.K_KP_SLASH:
					key = '/';

					break;

				case Key.K_KP_MINUS:
					key = '-';

					break;

				case Key.K_KP_PLUS:
					key = '+';

					break;

				case Key.K_KP_HOME:
					key = '7';

					break;

				case Key.K_KP_UPARROW:
					key = '8';

					break;

				case Key.K_KP_PGUP:
					key = '9';

					break;

				case Key.K_KP_LEFTARROW:
					key = '4';

					break;

				case Key.K_KP_5:
					key = '5';

					break;

				case Key.K_KP_RIGHTARROW:
					key = '6';

					break;

				case Key.K_KP_END:
					key = '1';

					break;

				case Key.K_KP_DOWNARROW:
					key = '2';

					break;

				case Key.K_KP_PGDN:
					key = '3';

					break;

				case Key.K_KP_INS:
					key = '0';

					break;

				case Key.K_KP_DEL:
					key = '.';

					break;
			}

			if (key == 'l')
			{
				if (Globals.keydown[Key.K_CTRL])
				{
					Cbuf.AddText("clear\n");

					return;
				}
			}

			if (key == Key.K_ENTER || key == Key.K_KP_ENTER)
			{
				// backslash text are commands, else chat
				if (Globals.key_lines[Globals.edit_line][1] == '\\' || Globals.key_lines[Globals.edit_line][1] == '/')
					Cbuf.AddText(Encoding.ASCII.GetString(Globals.key_lines[Globals.edit_line], 2, Lib.strlen(Globals.key_lines[Globals.edit_line]) - 2));
				else
					Cbuf.AddText(Encoding.ASCII.GetString(Globals.key_lines[Globals.edit_line], 1, Lib.strlen(Globals.key_lines[Globals.edit_line]) - 1));

				Cbuf.AddText("\n");
				Com.Printf(Encoding.ASCII.GetString(Globals.key_lines[Globals.edit_line], 0, Lib.strlen(Globals.key_lines[Globals.edit_line])) + "\n");
				Globals.edit_line = (Globals.edit_line + 1) & 31;
				Key.history_line = Globals.edit_line;
				Globals.key_lines[Globals.edit_line][0] = (byte)']';
				Globals.key_linepos = 1;

				if (Globals.cls.state == Defines.ca_disconnected)
					SCR.UpdateScreen(); // force an update, because the command may take some time

				return;
			}

			if (key == Key.K_TAB)
			{
				// command completion
				Key.CompleteCommand();

				return;
			}

			if (key == Key.K_BACKSPACE || key == Key.K_LEFTARROW || key == Key.K_KP_LEFTARROW || (key == 'h' && Globals.keydown[Key.K_CTRL]))
			{
				if (Globals.key_linepos > 1)
					Globals.key_linepos--;

				return;
			}

			if (key == Key.K_UPARROW || key == Key.K_KP_UPARROW || (key == 'p' && Globals.keydown[Key.K_CTRL]))
			{
				do
					Key.history_line = (Key.history_line - 1) & 31;
				while (Key.history_line != Globals.edit_line && Globals.key_lines[Key.history_line][1] == 0);

				if (Key.history_line == Globals.edit_line)
					Key.history_line = (Globals.edit_line + 1) & 31;

				//Lib.strcpy(Globals.key_lines[Globals.edit_line], Globals.key_lines[history_line]);
				Array.Copy(Globals.key_lines[Key.history_line], 0, Globals.key_lines[Globals.edit_line], 0, Globals.key_lines[Globals.edit_line].Length);
				Globals.key_linepos = Lib.strlen(Globals.key_lines[Globals.edit_line]);

				return;
			}

			if (key == Key.K_DOWNARROW || key == Key.K_KP_DOWNARROW || (key == 'n' && Globals.keydown[Key.K_CTRL]))
			{
				if (Key.history_line == Globals.edit_line)
					return;

				do
					Key.history_line = (Key.history_line + 1) & 31;
				while (Key.history_line != Globals.edit_line && Globals.key_lines[Key.history_line][1] == 0);

				if (Key.history_line == Globals.edit_line)
				{
					Globals.key_lines[Globals.edit_line][0] = (byte)']';
					Globals.key_linepos = 1;
				}
				else
				{
					//Lib.strcpy(Globals.key_lines[Globals.edit_line], Globals.key_lines[history_line]);
					Array.Copy(Globals.key_lines[Key.history_line], 0, Globals.key_lines[Globals.edit_line], 0, Globals.key_lines[Globals.edit_line].Length);
					Globals.key_linepos = Lib.strlen(Globals.key_lines[Globals.edit_line]);
				}

				return;
			}

			if (key == Key.K_PGUP || key == Key.K_KP_PGUP)
			{
				Globals.con.display -= 2;

				return;
			}

			if (key == Key.K_PGDN || key == Key.K_KP_PGDN)
			{
				Globals.con.display += 2;

				if (Globals.con.display > Globals.con.current)
					Globals.con.display = Globals.con.current;

				return;
			}

			if (key == Key.K_HOME || key == Key.K_KP_HOME)
			{
				Globals.con.display = Globals.con.current - Globals.con.totallines + 10;

				return;
			}

			if (key == Key.K_END || key == Key.K_KP_END)
			{
				Globals.con.display = Globals.con.current;

				return;
			}

			if (key < 32 || key > 127)
				return; // non printable

			if (Globals.key_linepos < Defines.MAXCMDLINE - 1)
			{
				Globals.key_lines[Globals.edit_line][Globals.key_linepos] = (byte)key;
				Globals.key_linepos++;
				Globals.key_lines[Globals.edit_line][Globals.key_linepos] = 0;
			}
		}

		private static void printCompletions(string type, List<string> compl)
		{
			Com.Printf(type);

			for (var i = 0; i < compl.Count; i++)
				Com.Printf((string)compl[i] + " ");

			Com.Printf("\n");
		}

		private static void CompleteCommand()
		{
			var start = 1;

			if (Globals.key_lines[Globals.edit_line][start] == '\\' || Globals.key_lines[Globals.edit_line][start] == '/')
				start++;

			var end = start;

			while (Globals.key_lines[Globals.edit_line][end] != 0)
				end++;

			var s = Encoding.ASCII.GetString(Globals.key_lines[Globals.edit_line], start, end - start);
			var cmds = Cmd.CompleteCommand(s);
			var vars = Cvar.CompleteVariable(s);
			var c = cmds.Count;
			var v = vars.Count;

			if (c + v > 1)
			{
				if (c > 0)
					Key.printCompletions("\nCommands:\n", cmds);

				if (v > 0)
					Key.printCompletions("\nVariables:\n", vars);

				return;
			}

			if (c == 1)
				s = (string)cmds[0];
			else if (v == 1)
				s = (string)vars[0];
			else
				return;

			Globals.key_lines[Globals.edit_line][1] = (byte)'/';
			var bytes = Lib.stringToBytes(s);
			Array.Copy(bytes, 0, Globals.key_lines[Globals.edit_line], 2, bytes.Length);
			Globals.key_linepos = bytes.Length + 2;
			Globals.key_lines[Globals.edit_line][Globals.key_linepos++] = (byte)' ';
			Globals.key_lines[Globals.edit_line][Globals.key_linepos] = 0;

			return;
		}

		public static Action Bind_f = () => { Key.Key_Bind_f(); };

		private static void Key_Bind_f()
		{
			var c = Cmd.Argc();

			if (c < 2)
			{
				Com.Printf("bind <key> [command] : attach a command to a key\n");

				return;
			}

			var b = Key.StringToKeynum(Cmd.Argv(1));

			if (b == -1)
			{
				Com.Printf("\"" + Cmd.Argv(1) + "\" isn't a valid key\n");

				return;
			}

			if (c == 2)
			{
				if (Globals.keybindings[b] != null)
					Com.Printf("\"" + Cmd.Argv(1) + "\" = \"" + Globals.keybindings[b] + "\"\n");
				else
					Com.Printf("\"" + Cmd.Argv(1) + "\" is not bound\n");

				return;
			}

			// copy the rest of the command line
			var cmd = ""; // start out with a null string

			for (var i = 2; i < c; i++)
			{
				cmd += Cmd.Argv(i);

				if (i != c - 1)
					cmd += " ";
			}

			Key.SetBinding(b, cmd);
		}

		public static void SetBinding(int keynum, string binding)
		{
			if (keynum == -1)
				return;

			// free old bindings
			Globals.keybindings[keynum] = null;
			Globals.keybindings[keynum] = binding;
		}

		private static readonly Action Unbind_f = () => { Key.Key_Unbind_f(); };

		private static void Key_Unbind_f()
		{
			if (Cmd.Argc() != 2)
			{
				Com.Printf("unbind <key> : remove commands from a key\n");

				return;
			}

			var b = Key.StringToKeynum(Cmd.Argv(1));

			if (b == -1)
			{
				Com.Printf("\"" + Cmd.Argv(1) + "\" isn't a valid key\n");

				return;
			}

			Key.SetBinding(b, null);
		}

		private static readonly Action Unbindall_f = () => { Key.Key_Unbindall_f(); };

		private static void Key_Unbindall_f()
		{
			for (var i = 0; i < 256; i++)
				Key.SetBinding(i, null);
		}

		private static readonly Action Bindlist_f = () => { Key.Key_Bindlist_f(); };

		private static void Key_Bindlist_f()
		{
			for (var i = 0; i < 256; i++)
			{
				if (Globals.keybindings[i] != null && Globals.keybindings[i].Length != 0)
					Com.Printf(Key.KeynumToString(i) + " \"" + Globals.keybindings[i] + "\"\n");
			}
		}

		public static void ClearStates()
		{
			int i;
			Key.anykeydown = 0;

			for (i = 0; i < 256; i++)
			{
				if (Globals.keydown[i] || Key.key_repeats[i] != 0)
					Key.Event(i, false, 0);

				Globals.keydown[i] = false;
				Key.key_repeats[i] = 0;
			}
		}

		public static void WriteBindings(StreamWriter f)
		{
			for (var i = 0; i < 256; i++)
			{
				if (Globals.keybindings[i] != null && Globals.keybindings[i].Length > 0)
				{
					try
					{
						f.Write("bind " + Key.KeynumToString(i) + " \"" + Globals.keybindings[i] + "\"\n");
					}
					catch (Exception)
					{
					}
				}
			}
		}
	}
}
