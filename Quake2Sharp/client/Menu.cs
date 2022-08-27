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
using game.types;
using qcommon;
using qcommon.types;
using sound;
using sys;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using types;
using util;

/**
 * Menu
 * 
 *  
 */
public class Menu
{
	private static int m_main_cursor;
	private static readonly int NUM_CURSOR_FRAMES = 15;
	private static readonly string menu_in_sound = "misc/menu1.wav";
	private static readonly string menu_move_sound = "misc/menu2.wav";
	private static readonly string menu_out_sound = "misc/menu3.wav";
	private static bool m_entersound; // play after drawing a frame, so caching

	// won't disrupt the sound
	private static Action m_drawfunc;
	private static Func<int, string> m_keyfunc;

	//	  =============================================================================
	/* Support Routines */
	public static readonly int MAX_MENU_DEPTH = 8;

	public class menulayer_t
	{
		public Action draw;
		public Func<int, string> key;
	}

	public class menuframework_s
	{
		public int x;
		public int y;
		public int cursor;
		public int nitems;
		public int nslots;
		public menucommon_s[] items = new menucommon_s[64];
		public string statusbar;

		//void (*cursordraw)( struct _tag_menuframework *m );
		public Action<object> cursordraw;
	}

	public class menucommon_s
	{
		public int type;
		public string name = "";
		public int x;
		public int y;
		public menuframework_s parent;
		public int cursor_offset;
		public int[] localdata = { 0, 0, 0, 0 };
		public int flags;
		public int n = -1; //position in an array.
		public string statusbar;
		public Action<object> callback;
		public Action<object> statusbarfunc;
		public Action<object> ownerdraw;
		public Action<object> cursordraw;
	}

	public class menufield_s : menucommon_s
	{
		//char buffer[80];
		public StringBuilder buffer; //allow deletion.
		public int cursor;
		public int length;
		public int visible_length;
		public int visible_offset;
	}

	public class menuslider_s : menucommon_s
	{
		public float minvalue;
		public float maxvalue;
		public float curvalue;
		public float range;
	}

	public class menulist_s : menucommon_s
	{
		public int curvalue;
		public string[] itemnames;
	}

	public class menuaction_s : menucommon_s
	{
	}

	public class menuseparator_s : menucommon_s
	{
	}

	public static menulayer_t[] m_layers = new menulayer_t[Menu.MAX_MENU_DEPTH];
	public static int m_menudepth;

	private static void Banner(string name)
	{
		var dim = new Size();
		Globals.re.DrawGetPicSize(ref dim, name);
		Globals.re.DrawPic(Globals.viddef.getWidth() / 2 - dim.Width / 2, Globals.viddef.getHeight() / 2 - 110, name);
	}

	private static void PushMenu(Action draw, Func<int, string> key)
	{
		//, string(*key)
		// (int k) ) {
		int i;

		if (Cvar.VariableValue("maxclients") == 1 && Globals.server_state != 0)
			Cvar.Set("paused", "1");

		// if this menu is already present, drop back to that level
		// to avoid stacking menus by hotkeys
		for (i = 0; i < Menu.m_menudepth; i++)
		{
			if (Menu.m_layers[i].draw == draw && Menu.m_layers[i].key == key)
				Menu.m_menudepth = i;
		}

		if (i == Menu.m_menudepth)
		{
			if (Menu.m_menudepth >= Menu.MAX_MENU_DEPTH)
				Com.Error(Defines.ERR_FATAL, "PushMenu: MAX_MENU_DEPTH");

			Menu.m_layers[Menu.m_menudepth].draw = draw; //m_drawfunc;
			Menu.m_layers[Menu.m_menudepth].key = key; //m_keyfunc;
		}

		Menu.m_menudepth++;
		Menu.m_drawfunc = draw;
		Menu.m_keyfunc = key;
		Menu.m_entersound = true;
		Globals.cls.key_dest = Defines.key_menu;
	}

	public static void ForceMenuOff()
	{
		Menu.m_drawfunc = null;
		Menu.m_keyfunc = null;
		Globals.cls.key_dest = Defines.key_game;
		Menu.m_menudepth = 0;
		Key.ClearStates();
		Cvar.Set("paused", "0");
	}

	public static void PopMenu()
	{
		S.StartLocalSound(Menu.menu_out_sound);
		Menu.m_menudepth--;

		if (Menu.m_menudepth < 0)
			Com.Error(Defines.ERR_FATAL, "PopMenu: depth < 1");

		if (0 < Menu.m_menudepth)
		{
			Menu.m_drawfunc = Menu.m_layers[Menu.m_menudepth - 1].draw;
			Menu.m_keyfunc = Menu.m_layers[Menu.m_menudepth - 1].key;
		}

		if (0 == Menu.m_menudepth)
			Menu.ForceMenuOff();
	}

	private static string Default_MenuKey(menuframework_s m, int key)
	{
		string sound = null;
		menucommon_s item;

		if (m != null)
		{
			if ((item = (menucommon_s)Menu.Menu_ItemAtCursor(m)) != null)
			{
				if (item.type == Defines.MTYPE_FIELD)
				{
					if (Menu.Field_Key((menufield_s)item, key))
						return null;
				}
			}
		}

		switch (key)
		{
			case Key.K_ESCAPE:
				Menu.PopMenu();

				return Menu.menu_out_sound;

			case Key.K_KP_UPARROW:
			case Key.K_UPARROW:
				if (m != null)
				{
					m.cursor--;
					Menu.Menu_AdjustCursor(m, -1);
					sound = Menu.menu_move_sound;
				}

				break;

			case Key.K_TAB:
				if (m != null)
				{
					m.cursor++;
					Menu.Menu_AdjustCursor(m, 1);
					sound = Menu.menu_move_sound;
				}

				break;

			case Key.K_KP_DOWNARROW:
			case Key.K_DOWNARROW:
				if (m != null)
				{
					m.cursor++;
					Menu.Menu_AdjustCursor(m, 1);
					sound = Menu.menu_move_sound;
				}

				break;

			case Key.K_KP_LEFTARROW:
			case Key.K_LEFTARROW:
				if (m != null)
				{
					Menu.Menu_SlideItem(m, -1);
					sound = Menu.menu_move_sound;
				}

				break;

			case Key.K_KP_RIGHTARROW:
			case Key.K_RIGHTARROW:
				if (m != null)
				{
					Menu.Menu_SlideItem(m, 1);
					sound = Menu.menu_move_sound;
				}

				break;

			case Key.K_MOUSE1:
			case Key.K_MOUSE2:
			case Key.K_MOUSE3:
			case Key.K_JOY1:
			case Key.K_JOY2:
			case Key.K_JOY3:
			case Key.K_JOY4:
			/*
			 * case K_AUX1 : case K_AUX2 : case K_AUX3 : case K_AUX4 : case K_AUX5 :
			 * case K_AUX6 : case K_AUX7 : case K_AUX8 : case K_AUX9 : case K_AUX10 :
			 * case K_AUX11 : case K_AUX12 : case K_AUX13 : case K_AUX14 : case
			 * K_AUX15 : case K_AUX16 : case K_AUX17 : case K_AUX18 : case K_AUX19 :
			 * case K_AUX20 : case K_AUX21 : case K_AUX22 : case K_AUX23 : case
			 * K_AUX24 : case K_AUX25 : case K_AUX26 : case K_AUX27 : case K_AUX28 :
			 * case K_AUX29 : case K_AUX30 : case K_AUX31 : case K_AUX32 :
			 */
			case Key.K_KP_ENTER:
			case Key.K_ENTER:
				if (m != null)
					Menu.Menu_SelectItem(m);

				sound = Menu.menu_move_sound;

				break;
		}

		return sound;
	}

	/*
	 * ================ DrawCharacter
	 * 
	 * Draws one solid graphics character cx and cy are in 320*240 coordinates,
	 * and will be centered on higher res screens. ================
	 */
	public static void DrawCharacter(int cx, int cy, int num)
	{
		Globals.re.DrawChar(cx + ((Globals.viddef.getWidth() - 320) >> 1), cy + ((Globals.viddef.getHeight() - 240) >> 1), num);
	}

	public static void Print(int cx, int cy, string str)
	{
		//while (*str)
		for (var n = 0; n < str.Length; n++)
		{
			Menu.DrawCharacter(cx, cy, str[n] + 128);

			//str++;
			cx += 8;
		}
	}

	public static void PrintWhite(int cx, int cy, string str)
	{
		for (var n = 0; n < str.Length; n++)
		{
			Menu.DrawCharacter(cx, cy, str[n]);

			//str++;
			cx += 8;
		}
	}

	public static void DrawPic(int x, int y, string pic)
	{
		Globals.re.DrawPic(x + ((Globals.viddef.getWidth() - 320) >> 1), y + ((Globals.viddef.getHeight() - 240) >> 1), pic);
	}

	/*
	 * ============= DrawCursor
	 * 
	 * Draws an animating cursor with the point at x,y. The pic will extend to
	 * the left of x, and both above and below y. =============
	 */
	private static bool cached;

	private static void DrawCursor(int x, int y, int f)
	{
		if (!(f >= 0))
			throw new("negative time and cursor bug");

		f = Math.Abs(f);

		if (!Menu.cached)
		{
			for (var i = 0; i < Menu.NUM_CURSOR_FRAMES; i++)
				Globals.re.RegisterPic("m_cursor" + i);

			Menu.cached = true;
		}

		Globals.re.DrawPic(x, y, "m_cursor" + f);
	}

	public static void DrawTextBox(int x, int y, int width, int lines)
	{
		int cx, cy;
		int n;

		// draw left side
		cx = x;
		cy = y;
		Menu.DrawCharacter(cx, cy, 1);

		for (n = 0; n < lines; n++)
		{
			cy += 8;
			Menu.DrawCharacter(cx, cy, 4);
		}

		Menu.DrawCharacter(cx, cy + 8, 7);

		// draw middle
		cx += 8;

		while (width > 0)
		{
			cy = y;
			Menu.DrawCharacter(cx, cy, 2);

			for (n = 0; n < lines; n++)
			{
				cy += 8;
				Menu.DrawCharacter(cx, cy, 5);
			}

			Menu.DrawCharacter(cx, cy + 8, 8);
			width -= 1;
			cx += 8;
		}

		// draw right side
		cy = y;
		Menu.DrawCharacter(cx, cy, 3);

		for (n = 0; n < lines; n++)
		{
			cy += 8;
			Menu.DrawCharacter(cx, cy, 6);
		}

		Menu.DrawCharacter(cx, cy + 8, 9);
	}

	/*
	 * =======================================================================
	 * 
	 * MAIN MENU
	 * 
	 * =======================================================================
	 */
	private static readonly int MAIN_ITEMS = 5;

	private static void Main_Draw()
	{
		int i;
		int w, h;
		int ystart;
		int xoffset;
		var widest = -1;
		var totalheight = 0;
		string litname;
		string[] names = { "m_main_game", "m_main_multiplayer", "m_main_options", "m_main_video", "m_main_quit" };
		var dim = new Size();

		for (i = 0; i < names.Length; i++)
		{
			Globals.re.DrawGetPicSize(ref dim, names[i]);
			w = dim.Width;
			h = dim.Height;

			if (w > widest)
				widest = w;

			totalheight += h + 12;
		}

		ystart = Globals.viddef.getHeight() / 2 - 110;
		xoffset = (Globals.viddef.getWidth() - widest + 70) / 2;

		for (i = 0; i < names.Length; i++)
		{
			if (i != Menu.m_main_cursor)
				Globals.re.DrawPic(xoffset, ystart + i * 40 + 13, names[i]);
		}

		//strcat(litname, "_sel");
		litname = names[Menu.m_main_cursor] + "_sel";
		Globals.re.DrawPic(xoffset, ystart + Menu.m_main_cursor * 40 + 13, litname);
		Menu.DrawCursor(xoffset - 25, ystart + Menu.m_main_cursor * 40 + 11, (int)(Globals.cls.realtime / 100) % Menu.NUM_CURSOR_FRAMES);
		Globals.re.DrawGetPicSize(ref dim, "m_main_plaque");
		w = dim.Width;
		h = dim.Height;
		Globals.re.DrawPic(xoffset - 30 - w, ystart, "m_main_plaque");
		Globals.re.DrawPic(xoffset - 30 - w, ystart + h + 5, "m_main_logo");
	}

	private static string Main_Key(int key)
	{
		var sound = Menu.menu_move_sound;

		switch (key)
		{
			case Key.K_ESCAPE:
				Menu.PopMenu();

				break;

			case Key.K_KP_DOWNARROW:
			case Key.K_DOWNARROW:
				if (++Menu.m_main_cursor >= Menu.MAIN_ITEMS)
					Menu.m_main_cursor = 0;

				return sound;

			case Key.K_KP_UPARROW:
			case Key.K_UPARROW:
				if (--Menu.m_main_cursor < 0)
					Menu.m_main_cursor = Menu.MAIN_ITEMS - 1;

				return sound;

			case Key.K_KP_ENTER:
			case Key.K_ENTER:
				Menu.m_entersound = true;

				switch (Menu.m_main_cursor)
				{
					case 0:
						Menu.Menu_Game_f();

						break;

					case 1:
						Menu.Menu_Multiplayer_f();

						break;

					case 2:
						Menu.Menu_Options_f();

						break;

					case 3:
						Menu.Menu_Video_f();

						break;

					case 4:
						Menu.Menu_Quit_f();

						break;
				}

				break;
		}

		return null;
	}

	private static readonly Action Menu_Main = () => { Menu.Menu_Main_f(); };

	public static void Menu_Main_f()
	{
		Menu.PushMenu(() => { Menu.Main_Draw(); }, key => { return Menu.Main_Key(key); });
	}

	/*
	 * =======================================================================
	 * 
	 * MULTIPLAYER MENU
	 * 
	 * =======================================================================
	 */
	private static readonly menuframework_s s_multiplayer_menu = new();
	private static readonly menuaction_s s_join_network_server_action = new();
	private static readonly menuaction_s s_start_network_server_action = new();
	private static readonly menuaction_s s_player_setup_action = new();

	private static void Multiplayer_MenuDraw()
	{
		Menu.Banner("m_banner_multiplayer");
		Menu.Menu_AdjustCursor(Menu.s_multiplayer_menu, 1);
		Menu.Menu_Draw(Menu.s_multiplayer_menu);
	}

	private static void PlayerSetupFunc(object unused)
	{
		Menu.Menu_PlayerConfig_f();
	}

	private static void JoinNetworkServerFunc(object unused)
	{
		Menu.Menu_JoinServer_f();
	}

	private static void StartNetworkServerFunc(object unused)
	{
		Menu.Menu_StartServer_f();
	}

	private static void Multiplayer_MenuInit()
	{
		Menu.s_multiplayer_menu.x = (int)(Globals.viddef.getWidth() * 0.50f - 64);
		Menu.s_multiplayer_menu.nitems = 0;
		Menu.s_join_network_server_action.type = Defines.MTYPE_ACTION;
		Menu.s_join_network_server_action.flags = Defines.QMF_LEFT_JUSTIFY;
		Menu.s_join_network_server_action.x = 0;
		Menu.s_join_network_server_action.y = 0;
		Menu.s_join_network_server_action.name = " join network server";
		Menu.s_join_network_server_action.callback = o => { Menu.JoinNetworkServerFunc(o); };
		Menu.s_start_network_server_action.type = Defines.MTYPE_ACTION;
		Menu.s_start_network_server_action.flags = Defines.QMF_LEFT_JUSTIFY;
		Menu.s_start_network_server_action.x = 0;
		Menu.s_start_network_server_action.y = 10;
		Menu.s_start_network_server_action.name = " start network server";
		Menu.s_start_network_server_action.callback = o => { Menu.StartNetworkServerFunc(o); };
		Menu.s_player_setup_action.type = Defines.MTYPE_ACTION;
		Menu.s_player_setup_action.flags = Defines.QMF_LEFT_JUSTIFY;
		Menu.s_player_setup_action.x = 0;
		Menu.s_player_setup_action.y = 20;
		Menu.s_player_setup_action.name = " player setup";
		Menu.s_player_setup_action.callback = o => { Menu.PlayerSetupFunc(o); };
		Menu.Menu_AddItem(Menu.s_multiplayer_menu, Menu.s_join_network_server_action);
		Menu.Menu_AddItem(Menu.s_multiplayer_menu, Menu.s_start_network_server_action);
		Menu.Menu_AddItem(Menu.s_multiplayer_menu, Menu.s_player_setup_action);
		Menu.Menu_SetStatusBar(Menu.s_multiplayer_menu, null);
		Menu.Menu_Center(Menu.s_multiplayer_menu);
	}

	private static string Multiplayer_MenuKey(int key)
	{
		return Menu.Default_MenuKey(Menu.s_multiplayer_menu, key);
	}

	private static readonly Action Menu_Multiplayer = () => { Menu.Menu_Multiplayer_f(); };

	private static void Menu_Multiplayer_f()
	{
		Menu.Multiplayer_MenuInit();
		Menu.PushMenu(() => { Menu.Multiplayer_MenuDraw(); }, key => { return Menu.Multiplayer_MenuKey(key); });
	}

	/*
	 * =======================================================================
	 * 
	 * KEYS MENU
	 * 
	 * =======================================================================
	 */
	private static readonly string[][] bindnames =
	{
		new[] { "+attack", "attack" },
		new[] { "weapnext", "next weapon" },
		new[] { "+forward", "walk forward" },
		new[] { "+back", "backpedal" },
		new[] { "+left", "turn left" },
		new[] { "+right", "turn right" },
		new[] { "+speed", "run" },
		new[] { "+moveleft", "step left" },
		new[] { "+moveright", "step right" },
		new[] { "+strafe", "sidestep" },
		new[] { "+lookup", "look up" },
		new[] { "+lookdown", "look down" },
		new[] { "centerview", "center view" },
		new[] { "+mlook", "mouse look" },
		new[] { "+klook", "keyboard look" },
		new[] { "+moveup", "up / jump" },
		new[] { "+movedown", "down / crouch" },
		new[] { "inven", "inventory" },
		new[] { "invuse", "use item" },
		new[] { "invdrop", "drop item" },
		new[] { "invprev", "prev item" },
		new[] { "invnext", "next item" },
		new[] { "cmd help", "help computer" },
		new string[] { null, null }
	};

	private static bool bind_grab;
	private static readonly menuframework_s s_keys_menu = new();
	private static readonly menuaction_s s_keys_attack_action = new();
	private static readonly menuaction_s s_keys_change_weapon_action = new();
	private static readonly menuaction_s s_keys_walk_forward_action = new();
	private static readonly menuaction_s s_keys_backpedal_action = new();
	private static readonly menuaction_s s_keys_turn_left_action = new();
	private static readonly menuaction_s s_keys_turn_right_action = new();
	private static readonly menuaction_s s_keys_run_action = new();
	private static readonly menuaction_s s_keys_step_left_action = new();
	private static readonly menuaction_s s_keys_step_right_action = new();
	private static readonly menuaction_s s_keys_sidestep_action = new();
	private static readonly menuaction_s s_keys_look_up_action = new();
	private static readonly menuaction_s s_keys_look_down_action = new();
	private static readonly menuaction_s s_keys_center_view_action = new();
	private static readonly menuaction_s s_keys_mouse_look_action = new();
	private static readonly menuaction_s s_keys_keyboard_look_action = new();
	private static readonly menuaction_s s_keys_move_up_action = new();
	private static readonly menuaction_s s_keys_move_down_action = new();
	private static readonly menuaction_s s_keys_inventory_action = new();
	private static readonly menuaction_s s_keys_inv_use_action = new();
	private static readonly menuaction_s s_keys_inv_drop_action = new();
	private static readonly menuaction_s s_keys_inv_prev_action = new();
	private static readonly menuaction_s s_keys_inv_next_action = new();
	private static readonly menuaction_s s_keys_help_computer_action = new();

	private static void UnbindCommand(string command)
	{
		int j;
		string b;

		for (j = 0; j < 256; j++)
		{
			b = Globals.keybindings[j];

			if (b == null)
				continue;

			if (b.Equals(command))
				Key.SetBinding(j, "");
		}
	}

	private static void FindKeysForCommand(string command, int[] twokeys)
	{
		int count;
		int j;
		string b;
		twokeys[0] = twokeys[1] = -1;
		count = 0;

		for (j = 0; j < 256; j++)
		{
			b = Globals.keybindings[j];

			if (b == null)
				continue;

			if (b.Equals(command))
			{
				twokeys[count] = j;
				count++;

				if (count == 2)
					break;
			}
		}
	}

	private static void KeyCursorDrawFunc(menuframework_s menu)
	{
		if (Menu.bind_grab)
			Globals.re.DrawChar(menu.x, menu.y + menu.cursor * 9, '=');
		else
			Globals.re.DrawChar(menu.x, menu.y + menu.cursor * 9, 12 + ((int)(Timer.Milliseconds() / 250) & 1));
	}

	private static void DrawKeyBindingFunc(object self)
	{
		int[] keys = { 0, 0 };
		var a = (menuaction_s)self;
		Menu.FindKeysForCommand(Menu.bindnames[a.localdata[0]][0], keys);

		if (keys[0] == -1)
			Menu.Menu_DrawString(a.x + a.parent.x + 16, a.y + a.parent.y, "???");
		else
		{
			int x;
			string name;
			name = Key.KeynumToString(keys[0]);
			Menu.Menu_DrawString(a.x + a.parent.x + 16, a.y + a.parent.y, name);
			x = name.Length * 8;

			if (keys[1] != -1)
			{
				Menu.Menu_DrawString(a.x + a.parent.x + 24 + x, a.y + a.parent.y, "or");
				Menu.Menu_DrawString(a.x + a.parent.x + 48 + x, a.y + a.parent.y, Key.KeynumToString(keys[1]));
			}
		}
	}

	private static void KeyBindingFunc(object self)
	{
		var a = (menuaction_s)self;
		int[] keys = { 0, 0 };
		Menu.FindKeysForCommand(Menu.bindnames[a.localdata[0]][0], keys);

		if (keys[1] != -1)
			Menu.UnbindCommand(Menu.bindnames[a.localdata[0]][0]);

		Menu.bind_grab = true;
		Menu.Menu_SetStatusBar(Menu.s_keys_menu, "press a key or button for this action");
	}

	private static void Keys_MenuInit()
	{
		var y = 0;
		var i = 0;
		Menu.s_keys_menu.x = (int)(Globals.viddef.getWidth() * 0.50);
		Menu.s_keys_menu.nitems = 0;
		Menu.s_keys_menu.cursordraw = o => { Menu.KeyCursorDrawFunc((menuframework_s)o); };
		Menu.s_keys_attack_action.type = Defines.MTYPE_ACTION;
		Menu.s_keys_attack_action.flags = Defines.QMF_GRAYED;
		Menu.s_keys_attack_action.x = 0;
		Menu.s_keys_attack_action.y = y;
		Menu.s_keys_attack_action.ownerdraw = o => { Menu.DrawKeyBindingFunc(o); };
		Menu.s_keys_attack_action.localdata[0] = i;
		Menu.s_keys_attack_action.name = Menu.bindnames[Menu.s_keys_attack_action.localdata[0]][1];
		Menu.s_keys_change_weapon_action.type = Defines.MTYPE_ACTION;
		Menu.s_keys_change_weapon_action.flags = Defines.QMF_GRAYED;
		Menu.s_keys_change_weapon_action.x = 0;
		Menu.s_keys_change_weapon_action.y = y += 9;
		Menu.s_keys_change_weapon_action.ownerdraw = o => { Menu.DrawKeyBindingFunc(o); };
		Menu.s_keys_change_weapon_action.localdata[0] = ++i;
		Menu.s_keys_change_weapon_action.name = Menu.bindnames[Menu.s_keys_change_weapon_action.localdata[0]][1];
		Menu.s_keys_walk_forward_action.type = Defines.MTYPE_ACTION;
		Menu.s_keys_walk_forward_action.flags = Defines.QMF_GRAYED;
		Menu.s_keys_walk_forward_action.x = 0;
		Menu.s_keys_walk_forward_action.y = y += 9;
		Menu.s_keys_walk_forward_action.ownerdraw = o => { Menu.DrawKeyBindingFunc(o); };
		Menu.s_keys_walk_forward_action.localdata[0] = ++i;
		Menu.s_keys_walk_forward_action.name = Menu.bindnames[Menu.s_keys_walk_forward_action.localdata[0]][1];
		Menu.s_keys_backpedal_action.type = Defines.MTYPE_ACTION;
		Menu.s_keys_backpedal_action.flags = Defines.QMF_GRAYED;
		Menu.s_keys_backpedal_action.x = 0;
		Menu.s_keys_backpedal_action.y = y += 9;
		Menu.s_keys_backpedal_action.ownerdraw = o => { Menu.DrawKeyBindingFunc(o); };
		Menu.s_keys_backpedal_action.localdata[0] = ++i;
		Menu.s_keys_backpedal_action.name = Menu.bindnames[Menu.s_keys_backpedal_action.localdata[0]][1];
		Menu.s_keys_turn_left_action.type = Defines.MTYPE_ACTION;
		Menu.s_keys_turn_left_action.flags = Defines.QMF_GRAYED;
		Menu.s_keys_turn_left_action.x = 0;
		Menu.s_keys_turn_left_action.y = y += 9;
		Menu.s_keys_turn_left_action.ownerdraw = o => { Menu.DrawKeyBindingFunc(o); };
		Menu.s_keys_turn_left_action.localdata[0] = ++i;
		Menu.s_keys_turn_left_action.name = Menu.bindnames[Menu.s_keys_turn_left_action.localdata[0]][1];
		Menu.s_keys_turn_right_action.type = Defines.MTYPE_ACTION;
		Menu.s_keys_turn_right_action.flags = Defines.QMF_GRAYED;
		Menu.s_keys_turn_right_action.x = 0;
		Menu.s_keys_turn_right_action.y = y += 9;
		Menu.s_keys_turn_right_action.ownerdraw = o => { Menu.DrawKeyBindingFunc(o); };
		Menu.s_keys_turn_right_action.localdata[0] = ++i;
		Menu.s_keys_turn_right_action.name = Menu.bindnames[Menu.s_keys_turn_right_action.localdata[0]][1];
		Menu.s_keys_run_action.type = Defines.MTYPE_ACTION;
		Menu.s_keys_run_action.flags = Defines.QMF_GRAYED;
		Menu.s_keys_run_action.x = 0;
		Menu.s_keys_run_action.y = y += 9;
		Menu.s_keys_run_action.ownerdraw = o => { Menu.DrawKeyBindingFunc(o); };
		Menu.s_keys_run_action.localdata[0] = ++i;
		Menu.s_keys_run_action.name = Menu.bindnames[Menu.s_keys_run_action.localdata[0]][1];
		Menu.s_keys_step_left_action.type = Defines.MTYPE_ACTION;
		Menu.s_keys_step_left_action.flags = Defines.QMF_GRAYED;
		Menu.s_keys_step_left_action.x = 0;
		Menu.s_keys_step_left_action.y = y += 9;
		Menu.s_keys_step_left_action.ownerdraw = o => { Menu.DrawKeyBindingFunc(o); };
		Menu.s_keys_step_left_action.localdata[0] = ++i;
		Menu.s_keys_step_left_action.name = Menu.bindnames[Menu.s_keys_step_left_action.localdata[0]][1];
		Menu.s_keys_step_right_action.type = Defines.MTYPE_ACTION;
		Menu.s_keys_step_right_action.flags = Defines.QMF_GRAYED;
		Menu.s_keys_step_right_action.x = 0;
		Menu.s_keys_step_right_action.y = y += 9;
		Menu.s_keys_step_right_action.ownerdraw = o => { Menu.DrawKeyBindingFunc(o); };
		Menu.s_keys_step_right_action.localdata[0] = ++i;
		Menu.s_keys_step_right_action.name = Menu.bindnames[Menu.s_keys_step_right_action.localdata[0]][1];
		Menu.s_keys_sidestep_action.type = Defines.MTYPE_ACTION;
		Menu.s_keys_sidestep_action.flags = Defines.QMF_GRAYED;
		Menu.s_keys_sidestep_action.x = 0;
		Menu.s_keys_sidestep_action.y = y += 9;
		Menu.s_keys_sidestep_action.ownerdraw = o => { Menu.DrawKeyBindingFunc(o); };
		Menu.s_keys_sidestep_action.localdata[0] = ++i;
		Menu.s_keys_sidestep_action.name = Menu.bindnames[Menu.s_keys_sidestep_action.localdata[0]][1];
		Menu.s_keys_look_up_action.type = Defines.MTYPE_ACTION;
		Menu.s_keys_look_up_action.flags = Defines.QMF_GRAYED;
		Menu.s_keys_look_up_action.x = 0;
		Menu.s_keys_look_up_action.y = y += 9;
		Menu.s_keys_look_up_action.ownerdraw = o => { Menu.DrawKeyBindingFunc(o); };
		Menu.s_keys_look_up_action.localdata[0] = ++i;
		Menu.s_keys_look_up_action.name = Menu.bindnames[Menu.s_keys_look_up_action.localdata[0]][1];
		Menu.s_keys_look_down_action.type = Defines.MTYPE_ACTION;
		Menu.s_keys_look_down_action.flags = Defines.QMF_GRAYED;
		Menu.s_keys_look_down_action.x = 0;
		Menu.s_keys_look_down_action.y = y += 9;
		Menu.s_keys_look_down_action.ownerdraw = o => { Menu.DrawKeyBindingFunc(o); };
		Menu.s_keys_look_down_action.localdata[0] = ++i;
		Menu.s_keys_look_down_action.name = Menu.bindnames[Menu.s_keys_look_down_action.localdata[0]][1];
		Menu.s_keys_center_view_action.type = Defines.MTYPE_ACTION;
		Menu.s_keys_center_view_action.flags = Defines.QMF_GRAYED;
		Menu.s_keys_center_view_action.x = 0;
		Menu.s_keys_center_view_action.y = y += 9;
		Menu.s_keys_center_view_action.ownerdraw = o => { Menu.DrawKeyBindingFunc(o); };
		Menu.s_keys_center_view_action.localdata[0] = ++i;
		Menu.s_keys_center_view_action.name = Menu.bindnames[Menu.s_keys_center_view_action.localdata[0]][1];
		Menu.s_keys_mouse_look_action.type = Defines.MTYPE_ACTION;
		Menu.s_keys_mouse_look_action.flags = Defines.QMF_GRAYED;
		Menu.s_keys_mouse_look_action.x = 0;
		Menu.s_keys_mouse_look_action.y = y += 9;
		Menu.s_keys_mouse_look_action.ownerdraw = o => { Menu.DrawKeyBindingFunc(o); };
		Menu.s_keys_mouse_look_action.localdata[0] = ++i;
		Menu.s_keys_mouse_look_action.name = Menu.bindnames[Menu.s_keys_mouse_look_action.localdata[0]][1];
		Menu.s_keys_keyboard_look_action.type = Defines.MTYPE_ACTION;
		Menu.s_keys_keyboard_look_action.flags = Defines.QMF_GRAYED;
		Menu.s_keys_keyboard_look_action.x = 0;
		Menu.s_keys_keyboard_look_action.y = y += 9;
		Menu.s_keys_keyboard_look_action.ownerdraw = o => { Menu.DrawKeyBindingFunc(o); };
		Menu.s_keys_keyboard_look_action.localdata[0] = ++i;
		Menu.s_keys_keyboard_look_action.name = Menu.bindnames[Menu.s_keys_keyboard_look_action.localdata[0]][1];
		Menu.s_keys_move_up_action.type = Defines.MTYPE_ACTION;
		Menu.s_keys_move_up_action.flags = Defines.QMF_GRAYED;
		Menu.s_keys_move_up_action.x = 0;
		Menu.s_keys_move_up_action.y = y += 9;
		Menu.s_keys_move_up_action.ownerdraw = o => { Menu.DrawKeyBindingFunc(o); };
		Menu.s_keys_move_up_action.localdata[0] = ++i;
		Menu.s_keys_move_up_action.name = Menu.bindnames[Menu.s_keys_move_up_action.localdata[0]][1];
		Menu.s_keys_move_down_action.type = Defines.MTYPE_ACTION;
		Menu.s_keys_move_down_action.flags = Defines.QMF_GRAYED;
		Menu.s_keys_move_down_action.x = 0;
		Menu.s_keys_move_down_action.y = y += 9;
		Menu.s_keys_move_down_action.ownerdraw = o => { Menu.DrawKeyBindingFunc(o); };
		Menu.s_keys_move_down_action.localdata[0] = ++i;
		Menu.s_keys_move_down_action.name = Menu.bindnames[Menu.s_keys_move_down_action.localdata[0]][1];
		Menu.s_keys_inventory_action.type = Defines.MTYPE_ACTION;
		Menu.s_keys_inventory_action.flags = Defines.QMF_GRAYED;
		Menu.s_keys_inventory_action.x = 0;
		Menu.s_keys_inventory_action.y = y += 9;
		Menu.s_keys_inventory_action.ownerdraw = o => { Menu.DrawKeyBindingFunc(o); };
		Menu.s_keys_inventory_action.localdata[0] = ++i;
		Menu.s_keys_inventory_action.name = Menu.bindnames[Menu.s_keys_inventory_action.localdata[0]][1];
		Menu.s_keys_inv_use_action.type = Defines.MTYPE_ACTION;
		Menu.s_keys_inv_use_action.flags = Defines.QMF_GRAYED;
		Menu.s_keys_inv_use_action.x = 0;
		Menu.s_keys_inv_use_action.y = y += 9;
		Menu.s_keys_inv_use_action.ownerdraw = o => { Menu.DrawKeyBindingFunc(o); };
		Menu.s_keys_inv_use_action.localdata[0] = ++i;
		Menu.s_keys_inv_use_action.name = Menu.bindnames[Menu.s_keys_inv_use_action.localdata[0]][1];
		Menu.s_keys_inv_drop_action.type = Defines.MTYPE_ACTION;
		Menu.s_keys_inv_drop_action.flags = Defines.QMF_GRAYED;
		Menu.s_keys_inv_drop_action.x = 0;
		Menu.s_keys_inv_drop_action.y = y += 9;
		Menu.s_keys_inv_drop_action.ownerdraw = o => { Menu.DrawKeyBindingFunc(o); };
		Menu.s_keys_inv_drop_action.localdata[0] = ++i;
		Menu.s_keys_inv_drop_action.name = Menu.bindnames[Menu.s_keys_inv_drop_action.localdata[0]][1];
		Menu.s_keys_inv_prev_action.type = Defines.MTYPE_ACTION;
		Menu.s_keys_inv_prev_action.flags = Defines.QMF_GRAYED;
		Menu.s_keys_inv_prev_action.x = 0;
		Menu.s_keys_inv_prev_action.y = y += 9;
		Menu.s_keys_inv_prev_action.ownerdraw = o => { Menu.DrawKeyBindingFunc(o); };
		Menu.s_keys_inv_prev_action.localdata[0] = ++i;
		Menu.s_keys_inv_prev_action.name = Menu.bindnames[Menu.s_keys_inv_prev_action.localdata[0]][1];
		Menu.s_keys_inv_next_action.type = Defines.MTYPE_ACTION;
		Menu.s_keys_inv_next_action.flags = Defines.QMF_GRAYED;
		Menu.s_keys_inv_next_action.x = 0;
		Menu.s_keys_inv_next_action.y = y += 9;
		Menu.s_keys_inv_next_action.ownerdraw = o => { Menu.DrawKeyBindingFunc(o); };
		Menu.s_keys_inv_next_action.localdata[0] = ++i;
		Menu.s_keys_inv_next_action.name = Menu.bindnames[Menu.s_keys_inv_next_action.localdata[0]][1];
		Menu.s_keys_help_computer_action.type = Defines.MTYPE_ACTION;
		Menu.s_keys_help_computer_action.flags = Defines.QMF_GRAYED;
		Menu.s_keys_help_computer_action.x = 0;
		Menu.s_keys_help_computer_action.y = y += 9;
		Menu.s_keys_help_computer_action.ownerdraw = o => { Menu.DrawKeyBindingFunc(o); };
		Menu.s_keys_help_computer_action.localdata[0] = ++i;
		Menu.s_keys_help_computer_action.name = Menu.bindnames[Menu.s_keys_help_computer_action.localdata[0]][1];
		Menu.Menu_AddItem(Menu.s_keys_menu, Menu.s_keys_attack_action);
		Menu.Menu_AddItem(Menu.s_keys_menu, Menu.s_keys_change_weapon_action);
		Menu.Menu_AddItem(Menu.s_keys_menu, Menu.s_keys_walk_forward_action);
		Menu.Menu_AddItem(Menu.s_keys_menu, Menu.s_keys_backpedal_action);
		Menu.Menu_AddItem(Menu.s_keys_menu, Menu.s_keys_turn_left_action);
		Menu.Menu_AddItem(Menu.s_keys_menu, Menu.s_keys_turn_right_action);
		Menu.Menu_AddItem(Menu.s_keys_menu, Menu.s_keys_run_action);
		Menu.Menu_AddItem(Menu.s_keys_menu, Menu.s_keys_step_left_action);
		Menu.Menu_AddItem(Menu.s_keys_menu, Menu.s_keys_step_right_action);
		Menu.Menu_AddItem(Menu.s_keys_menu, Menu.s_keys_sidestep_action);
		Menu.Menu_AddItem(Menu.s_keys_menu, Menu.s_keys_look_up_action);
		Menu.Menu_AddItem(Menu.s_keys_menu, Menu.s_keys_look_down_action);
		Menu.Menu_AddItem(Menu.s_keys_menu, Menu.s_keys_center_view_action);
		Menu.Menu_AddItem(Menu.s_keys_menu, Menu.s_keys_mouse_look_action);
		Menu.Menu_AddItem(Menu.s_keys_menu, Menu.s_keys_keyboard_look_action);
		Menu.Menu_AddItem(Menu.s_keys_menu, Menu.s_keys_move_up_action);
		Menu.Menu_AddItem(Menu.s_keys_menu, Menu.s_keys_move_down_action);
		Menu.Menu_AddItem(Menu.s_keys_menu, Menu.s_keys_inventory_action);
		Menu.Menu_AddItem(Menu.s_keys_menu, Menu.s_keys_inv_use_action);
		Menu.Menu_AddItem(Menu.s_keys_menu, Menu.s_keys_inv_drop_action);
		Menu.Menu_AddItem(Menu.s_keys_menu, Menu.s_keys_inv_prev_action);
		Menu.Menu_AddItem(Menu.s_keys_menu, Menu.s_keys_inv_next_action);
		Menu.Menu_AddItem(Menu.s_keys_menu, Menu.s_keys_help_computer_action);
		Menu.Menu_SetStatusBar(Menu.s_keys_menu, "enter to change, backspace to clear");
		Menu.Menu_Center(Menu.s_keys_menu);
	}

	private static Action Keys_MenuDraw = () => { Menu.Keys_MenuDraw_f(); };

	private static void Keys_MenuDraw_f()
	{
		Menu.Menu_AdjustCursor(Menu.s_keys_menu, 1);
		Menu.Menu_Draw(Menu.s_keys_menu);
	}

	private static Func<int, string> Keys_MenuKey = key => { return Menu.Keys_MenuKey_f(key); };

	private static string Keys_MenuKey_f(int key)
	{
		var item = (menuaction_s)Menu.Menu_ItemAtCursor(Menu.s_keys_menu);

		if (Menu.bind_grab)
		{
			if (key != Key.K_ESCAPE && key != '`')
			{
				//char cmd[1024];
				string cmd;

				//Com_sprintf(cmd, sizeof(cmd), "bind \"%s\" \"%s\"\n",
				// Key_KeynumToString(key), bindnames[item.localdata[0]][0]);
				cmd = "bind \"" + Key.KeynumToString(key) + "\" \"" + Menu.bindnames[item.localdata[0]][0] + "\"";
				Cbuf.InsertText(cmd);
			}

			Menu.Menu_SetStatusBar(Menu.s_keys_menu, "enter to change, backspace to clear");
			Menu.bind_grab = false;

			return Menu.menu_out_sound;
		}

		switch (key)
		{
			case Key.K_KP_ENTER:
			case Key.K_ENTER:
				Menu.KeyBindingFunc(item);

				return Menu.menu_in_sound;

			case Key.K_BACKSPACE: // delete bindings
			case Key.K_DEL: // delete bindings
			case Key.K_KP_DEL:
				Menu.UnbindCommand(Menu.bindnames[item.localdata[0]][0]);

				return Menu.menu_out_sound;

			default:
				return Menu.Default_MenuKey(Menu.s_keys_menu, key);
		}
	}

	private static readonly Action Menu_Keys = () => { Menu.Menu_Keys_f(); };

	private static void Menu_Keys_f()
	{
		Menu.Keys_MenuInit();
		Menu.PushMenu(() => { Menu.Keys_MenuDraw_f(); }, key => { return Menu.Keys_MenuKey_f(key); });
	}

	/*
	 * =======================================================================
	 * 
	 * CONTROLS MENU
	 * 
	 * =======================================================================
	 */
	private static cvar_t win_noalttab;
	private static readonly menuframework_s s_options_menu = new();
	private static readonly menuaction_s s_options_defaults_action = new();
	private static readonly menuaction_s s_options_customize_options_action = new();
	private static readonly menuslider_s s_options_sensitivity_slider = new();
	private static readonly menulist_s s_options_freelook_box = new();
	private static readonly menulist_s s_options_noalttab_box = new();
	private static readonly menulist_s s_options_alwaysrun_box = new();
	private static readonly menulist_s s_options_invertmouse_box = new();
	private static readonly menulist_s s_options_lookspring_box = new();
	private static readonly menulist_s s_options_lookstrafe_box = new();
	private static readonly menulist_s s_options_crosshair_box = new();
	private static readonly menuslider_s s_options_sfxvolume_slider = new();
	private static readonly menulist_s s_options_joystick_box = new();
	private static readonly menulist_s s_options_cdvolume_box = new();
	private static readonly menulist_s s_options_quality_list = new();

	//static menulist_s s_options_compatibility_list = new menulist_s();
	private static readonly menuaction_s s_options_console_action = new();

	private static void CrosshairFunc(object unused)
	{
		Cvar.SetValue("crosshair", Menu.s_options_crosshair_box.curvalue);
	}

	private static void JoystickFunc(object unused)
	{
		Cvar.SetValue("in_joystick", Menu.s_options_joystick_box.curvalue);
	}

	private static void CustomizeControlsFunc(object unused)
	{
		Menu.Menu_Keys_f();
	}

	private static void AlwaysRunFunc(object unused)
	{
		Cvar.SetValue("cl_run", Menu.s_options_alwaysrun_box.curvalue);
	}

	private static void FreeLookFunc(object unused)
	{
		Cvar.SetValue("freelook", Menu.s_options_freelook_box.curvalue);
	}

	private static void MouseSpeedFunc(object unused)
	{
		Cvar.SetValue("sensitivity", Menu.s_options_sensitivity_slider.curvalue / 2.0F);
	}

	private static void NoAltTabFunc(object unused)
	{
		Cvar.SetValue("win_noalttab", Menu.s_options_noalttab_box.curvalue);
	}

	private static float ClampCvar(float min, float max, float value)
	{
		if (value < min)
			return min;

		if (value > max)
			return max;

		return value;
	}

	private static void ControlsSetMenuItemValues()
	{
		Menu.s_options_sfxvolume_slider.curvalue = Cvar.VariableValue("s_volume") * 10;
		Menu.s_options_cdvolume_box.curvalue = 1 - (int)Cvar.VariableValue("cd_nocd");

		//s_options_quality_list.curvalue = 1 - ((int)
		// Cvar.VariableValue("s_loadas8bit"));
		var s = Cvar.VariableString("s_impl");

		for (var i = 0; i < Menu.s_drivers.Length; i++)
		{
			if (s.Equals(Menu.s_drivers[i]))
				Menu.s_options_quality_list.curvalue = i;
		}

		Menu.s_options_sensitivity_slider.curvalue = Globals.sensitivity.value * 2;
		Cvar.SetValue("cl_run", Menu.ClampCvar(0, 1, Globals.cl_run.value));
		Menu.s_options_alwaysrun_box.curvalue = (int)Globals.cl_run.value;
		Menu.s_options_invertmouse_box.curvalue = Globals.m_pitch.value < 0 ? 1 : 0;
		Cvar.SetValue("lookspring", Menu.ClampCvar(0, 1, Globals.lookspring.value));
		Menu.s_options_lookspring_box.curvalue = (int)Globals.lookspring.value;
		Cvar.SetValue("lookstrafe", Menu.ClampCvar(0, 1, Globals.lookstrafe.value));
		Menu.s_options_lookstrafe_box.curvalue = (int)Globals.lookstrafe.value;
		Cvar.SetValue("freelook", Menu.ClampCvar(0, 1, Globals.freelook.value));
		Menu.s_options_freelook_box.curvalue = (int)Globals.freelook.value;
		Cvar.SetValue("crosshair", Menu.ClampCvar(0, 3, Globals.crosshair.value));
		Menu.s_options_crosshair_box.curvalue = (int)Globals.crosshair.value;
		Cvar.SetValue("in_joystick", Menu.ClampCvar(0, 1, Globals.in_joystick.value));
		Menu.s_options_joystick_box.curvalue = (int)Globals.in_joystick.value;
		Menu.s_options_noalttab_box.curvalue = (int)Menu.win_noalttab.value;
	}

	private static void ControlsResetDefaultsFunc(object unused)
	{
		Cbuf.AddText("exec default.cfg\n");
		Cbuf.Execute();
		Menu.ControlsSetMenuItemValues();
	}

	private static void InvertMouseFunc(object unused)
	{
		Cvar.SetValue("m_pitch", -Globals.m_pitch.value);
	}

	private static void LookspringFunc(object unused)
	{
		Cvar.SetValue("lookspring", 1 - Globals.lookspring.value);
	}

	private static void LookstrafeFunc(object unused)
	{
		Cvar.SetValue("lookstrafe", 1 - Globals.lookstrafe.value);
	}

	private static void UpdateVolumeFunc(object unused)
	{
		Cvar.SetValue("s_volume", Menu.s_options_sfxvolume_slider.curvalue / 10);
	}

	private static void UpdateCDVolumeFunc(object unused)
	{
		Cvar.SetValue("cd_nocd", 1 - Menu.s_options_cdvolume_box.curvalue);
	}

	private static void ConsoleFunc(object unused)
	{
		/*
		 * * the proper way to do this is probably to have ToggleConsole_f
		 * accept a parameter
		 */

		if (Globals.cl.attractloop)
		{
			Cbuf.AddText("killserver\n");

			return;
		}

		Key.ClearTyping();
		Console.ClearNotify();
		Menu.ForceMenuOff();
		Globals.cls.key_dest = Defines.key_console;
	}

	private static void UpdateSoundQualityFunc(object unused)
	{
		var driverNotChanged = false;
		var current = Menu.s_drivers[Menu.s_options_quality_list.curvalue];
		driverNotChanged = S.getDriverName().Equals(current);

		//        if (s_options_quality_list.curvalue != 0) {
		//            //			Cvar.SetValue("s_khz", 22);
		//            //			Cvar.SetValue("s_loadas8bit", 0);
		//            driverNotChanged = S.getDriverName().equals("dummy");
		//            Cvar.Set("s_impl", "dummy");
		//        } else {
		//            //			Cvar.SetValue("s_khz", 11);
		//            //			Cvar.SetValue("s_loadas8bit", 1);
		//            driverNotChanged = S.getDriverName().equals("joal");
		//            Cvar.Set("s_impl", "joal");
		//        }

		//Cvar.SetValue("s_primary", s_options_compatibility_list.curvalue);

		if (driverNotChanged)
		{
			Globals.re.EndFrame();

			return;
		}

		Cvar.Set("s_impl", current);
		Menu.DrawTextBox(8, 120 - 48, 36, 3);
		Menu.Print(16 + 16, 120 - 48 + 8, "Restarting the sound system. This");
		Menu.Print(16 + 16, 120 - 48 + 16, "could take up to a minute, so");
		Menu.Print(16 + 16, 120 - 48 + 24, "please be patient.");

		// the text box won't show up unless we do a buffer swap
		Globals.re.EndFrame();
		Cl.SndRestartF();
	}

	private static readonly string[] cd_music_items = { "disabled", "enabled" };
	private static string[] compatibility_items = { "max compatibility", "max performance" };
	private static readonly string[] yesno_names = { "no", "yes" };
	private static readonly string[] crosshair_names = { "none", "cross", "dot", "angle" };
	private static string[] s_labels;
	private static string[] s_drivers;

	private static void Options_MenuInit()
	{
		Menu.s_drivers = S.getDriverNames();
		Menu.s_labels = new string[Menu.s_drivers.Length];

		for (var i = 0; i < Menu.s_drivers.Length; i++)
		{
			if ("dummy".Equals(Menu.s_drivers[i]))
				Menu.s_labels[i] = "off";
			else
				Menu.s_labels[i] = Menu.s_drivers[i];
		}

		Menu.win_noalttab = Cvar.Get("win_noalttab", "0", Defines.CVAR_ARCHIVE);
		/*
		 * * configure controls menu and menu items
		 */
		Menu.s_options_menu.x = Globals.viddef.getWidth() / 2;
		Menu.s_options_menu.y = Globals.viddef.getHeight() / 2 - 58;
		Menu.s_options_menu.nitems = 0;
		Menu.s_options_sfxvolume_slider.type = Defines.MTYPE_SLIDER;
		Menu.s_options_sfxvolume_slider.x = 0;
		Menu.s_options_sfxvolume_slider.y = 0;
		Menu.s_options_sfxvolume_slider.name = "effects volume";
		Menu.s_options_sfxvolume_slider.callback = o => { Menu.UpdateVolumeFunc(o); };
		Menu.s_options_sfxvolume_slider.minvalue = 0;
		Menu.s_options_sfxvolume_slider.maxvalue = 10;
		Menu.s_options_sfxvolume_slider.curvalue = Cvar.VariableValue("s_volume") * 10;
		Menu.s_options_cdvolume_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_options_cdvolume_box.x = 0;
		Menu.s_options_cdvolume_box.y = 10;
		Menu.s_options_cdvolume_box.name = "CD music";
		Menu.s_options_cdvolume_box.callback = o => { Menu.UpdateCDVolumeFunc(o); };
		Menu.s_options_cdvolume_box.itemnames = Menu.cd_music_items;
		Menu.s_options_cdvolume_box.curvalue = 1 - (int)Cvar.VariableValue("cd_nocd");
		Menu.s_options_quality_list.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_options_quality_list.x = 0;
		Menu.s_options_quality_list.y = 20;
		;
		Menu.s_options_quality_list.name = "sound";
		Menu.s_options_quality_list.callback = o => { Menu.UpdateSoundQualityFunc(o); };
		Menu.s_options_quality_list.itemnames = Menu.s_labels;
		Menu.s_options_sensitivity_slider.type = Defines.MTYPE_SLIDER;
		Menu.s_options_sensitivity_slider.x = 0;
		Menu.s_options_sensitivity_slider.y = 50;
		Menu.s_options_sensitivity_slider.name = "mouse speed";
		Menu.s_options_sensitivity_slider.callback = o => { Menu.MouseSpeedFunc(o); };
		Menu.s_options_sensitivity_slider.minvalue = 2;
		Menu.s_options_sensitivity_slider.maxvalue = 22;
		Menu.s_options_alwaysrun_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_options_alwaysrun_box.x = 0;
		Menu.s_options_alwaysrun_box.y = 60;
		Menu.s_options_alwaysrun_box.name = "always run";
		Menu.s_options_alwaysrun_box.callback = o => { Menu.AlwaysRunFunc(o); };
		Menu.s_options_alwaysrun_box.itemnames = Menu.yesno_names;
		Menu.s_options_invertmouse_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_options_invertmouse_box.x = 0;
		Menu.s_options_invertmouse_box.y = 70;
		Menu.s_options_invertmouse_box.name = "invert mouse";
		Menu.s_options_invertmouse_box.callback = o => { Menu.InvertMouseFunc(o); };
		Menu.s_options_invertmouse_box.itemnames = Menu.yesno_names;
		Menu.s_options_lookspring_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_options_lookspring_box.x = 0;
		Menu.s_options_lookspring_box.y = 80;
		Menu.s_options_lookspring_box.name = "lookspring";
		Menu.s_options_lookspring_box.callback = o => { Menu.LookspringFunc(o); };
		Menu.s_options_lookspring_box.itemnames = Menu.yesno_names;
		Menu.s_options_lookstrafe_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_options_lookstrafe_box.x = 0;
		Menu.s_options_lookstrafe_box.y = 90;
		Menu.s_options_lookstrafe_box.name = "lookstrafe";
		Menu.s_options_lookstrafe_box.callback = o => { Menu.LookstrafeFunc(o); };
		Menu.s_options_lookstrafe_box.itemnames = Menu.yesno_names;
		Menu.s_options_freelook_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_options_freelook_box.x = 0;
		Menu.s_options_freelook_box.y = 100;
		Menu.s_options_freelook_box.name = "free look";
		Menu.s_options_freelook_box.callback = o => { Menu.FreeLookFunc(o); };
		Menu.s_options_freelook_box.itemnames = Menu.yesno_names;
		Menu.s_options_crosshair_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_options_crosshair_box.x = 0;
		Menu.s_options_crosshair_box.y = 110;
		Menu.s_options_crosshair_box.name = "crosshair";
		Menu.s_options_crosshair_box.callback = o => { Menu.CrosshairFunc(o); };
		Menu.s_options_crosshair_box.itemnames = Menu.crosshair_names;
		/*
		 * s_options_noalttab_box.type = MTYPE_SPINCONTROL;
		 * s_options_noalttab_box.x = 0; s_options_noalttab_box.y = 110;
		 * s_options_noalttab_box.name = "disable alt-tab";
		 * s_options_noalttab_box.callback = NoAltTabFunc;
		 * s_options_noalttab_box.itemnames = yesno_names;
		 */
		Menu.s_options_joystick_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_options_joystick_box.x = 0;
		Menu.s_options_joystick_box.y = 120;
		Menu.s_options_joystick_box.name = "use joystick";
		Menu.s_options_joystick_box.callback = o => { Menu.JoystickFunc(o); };
		Menu.s_options_joystick_box.itemnames = Menu.yesno_names;
		Menu.s_options_customize_options_action.type = Defines.MTYPE_ACTION;
		Menu.s_options_customize_options_action.x = 0;
		Menu.s_options_customize_options_action.y = 140;
		Menu.s_options_customize_options_action.name = "customize controls";
		Menu.s_options_customize_options_action.callback = o => { Menu.CustomizeControlsFunc(o); };
		Menu.s_options_defaults_action.type = Defines.MTYPE_ACTION;
		Menu.s_options_defaults_action.x = 0;
		Menu.s_options_defaults_action.y = 150;
		Menu.s_options_defaults_action.name = "reset defaults";
		Menu.s_options_defaults_action.callback = o => { Menu.ControlsResetDefaultsFunc(o); };
		Menu.s_options_console_action.type = Defines.MTYPE_ACTION;
		Menu.s_options_console_action.x = 0;
		Menu.s_options_console_action.y = 160;
		Menu.s_options_console_action.name = "go to console";
		Menu.s_options_console_action.callback = o => { Menu.ConsoleFunc(o); };
		Menu.ControlsSetMenuItemValues();
		Menu.Menu_AddItem(Menu.s_options_menu, Menu.s_options_sfxvolume_slider);
		Menu.Menu_AddItem(Menu.s_options_menu, Menu.s_options_cdvolume_box);
		Menu.Menu_AddItem(Menu.s_options_menu, Menu.s_options_quality_list);

		//		Menu_AddItem(s_options_menu, s_options_compatibility_list);
		Menu.Menu_AddItem(Menu.s_options_menu, Menu.s_options_sensitivity_slider);
		Menu.Menu_AddItem(Menu.s_options_menu, Menu.s_options_alwaysrun_box);
		Menu.Menu_AddItem(Menu.s_options_menu, Menu.s_options_invertmouse_box);
		Menu.Menu_AddItem(Menu.s_options_menu, Menu.s_options_lookspring_box);
		Menu.Menu_AddItem(Menu.s_options_menu, Menu.s_options_lookstrafe_box);
		Menu.Menu_AddItem(Menu.s_options_menu, Menu.s_options_freelook_box);
		Menu.Menu_AddItem(Menu.s_options_menu, Menu.s_options_crosshair_box);

		//		Menu_AddItem(s_options_menu, s_options_joystick_box);
		Menu.Menu_AddItem(Menu.s_options_menu, Menu.s_options_customize_options_action);
		Menu.Menu_AddItem(Menu.s_options_menu, Menu.s_options_defaults_action);
		Menu.Menu_AddItem(Menu.s_options_menu, Menu.s_options_console_action);
	}

	private static void Options_MenuDraw()
	{
		Menu.Banner("m_banner_options");
		Menu.Menu_AdjustCursor(Menu.s_options_menu, 1);
		Menu.Menu_Draw(Menu.s_options_menu);
	}

	private static string Options_MenuKey(int key)
	{
		return Menu.Default_MenuKey(Menu.s_options_menu, key);
	}

	private static readonly Action Menu_Options = () => { Menu.Menu_Options_f(); };

	private static void Menu_Options_f()
	{
		Menu.Options_MenuInit();
		Menu.PushMenu(() => { Menu.Options_MenuDraw(); }, key => { return Menu.Options_MenuKey(key); });
	}

	/*
	 * =======================================================================
	 * 
	 * VIDEO MENU
	 * 
	 * =======================================================================
	 */
	private static readonly Action Menu_Video = () => { Menu.Menu_Video_f(); };

	private static void Menu_Video_f()
	{
		VID.MenuInit();
		Menu.PushMenu(() => { VID.MenuDraw(); }, key => { return VID.MenuKey(key); });
	}

	/*
	 * =============================================================================
	 * 
	 * END GAME MENU
	 * 
	 * =============================================================================
	 */
	private static int credits_start_time;
	private static readonly string[] creditsIndex = new string[256];
	private static string creditsBuffer;

	private static readonly string[] idcredits =
	{
		"+QUAKE II BY ID SOFTWARE",
		"",
		"+PROGRAMMING",
		"John Carmack",
		"John Cash",
		"Brian Hook",
		"",
		"+ART",
		"Adrian Carmack",
		"Kevin Cloud",
		"Paul Steed",
		"",
		"+LEVEL DESIGN",
		"Tim Willits",
		"American McGee",
		"Christian Antkow",
		"Paul Jaquays",
		"Brandon James",
		"",
		"+BIZ",
		"Todd Hollenshead",
		"Barrett (Bear) Alexander",
		"Donna Jackson",
		"",
		"",
		"+SPECIAL THANKS",
		"Ben Donges for beta testing",
		"",
		"",
		"",
		"",
		"",
		"",
		"+ADDITIONAL SUPPORT",
		"",
		"+LINUX PORT AND CTF",
		"Dave \"Zoid\" Kirsch",
		"",
		"+CINEMATIC SEQUENCES",
		"Ending Cinematic by Blur Studio - ",
		"Venice, CA",
		"",
		"Environment models for Introduction",
		"Cinematic by Karl Dolgener",
		"",
		"Assistance with environment design",
		"by Cliff Iwai",
		"",
		"+SOUND EFFECTS AND MUSIC",
		"Sound Design by Soundelux Media Labs.",
		"Music Composed and Produced by",
		"Soundelux Media Labs.  Special thanks",
		"to Bill Brown, Tom Ozanich, Brian",
		"Celano, Jeff Eisner, and The Soundelux",
		"Players.",
		"",
		"\"Level Music\" by Sonic Mayhem",
		"www.sonicmayhem.com",
		"",
		"\"Quake II Theme Song\"",
		"(C) 1997 Rob Zombie. All Rights",
		"Reserved.",
		"",
		"Track 10 (\"Climb\") by Jer Sypult",
		"",
		"Voice of computers by",
		"Carly Staehlin-Taylor",
		"",
		"+THANKS TO ACTIVISION",
		"+IN PARTICULAR:",
		"",
		"John Tam",
		"Steve Rosenthal",
		"Marty Stratton",
		"Henk Hartong",
		"",
		"Quake II(tm) (C)1997 Id Software, Inc.",
		"All Rights Reserved.  Distributed by",
		"Activision, Inc. under license.",
		"Quake II(tm), the Id Software name,",
		"the \"Q II\"(tm) logo and id(tm)",
		"logo are trademarks of Id Software,",
		"Inc. Activision(R) is a registered",
		"trademark of Activision, Inc. All",
		"other trademarks and trade names are",
		"properties of their respective owners.",
		null
	};

	private static string[] credits = Menu.idcredits;

	private static readonly string[] xatcredits =
	{
		"+QUAKE II MISSION PACK: THE RECKONING",
		"+BY",
		"+XATRIX ENTERTAINMENT, INC.",
		"",
		"+DESIGN AND DIRECTION",
		"Drew Markham",
		"",
		"+PRODUCED BY",
		"Greg Goodrich",
		"",
		"+PROGRAMMING",
		"Rafael Paiz",
		"",
		"+LEVEL DESIGN / ADDITIONAL GAME DESIGN",
		"Alex Mayberry",
		"",
		"+LEVEL DESIGN",
		"Mal Blackwell",
		"Dan Koppel",
		"",
		"+ART DIRECTION",
		"Michael \"Maxx\" Kaufman",
		"",
		"+COMPUTER GRAPHICS SUPERVISOR AND",
		"+CHARACTER ANIMATION DIRECTION",
		"Barry Dempsey",
		"",
		"+SENIOR ANIMATOR AND MODELER",
		"Jason Hoover",
		"",
		"+CHARACTER ANIMATION AND",
		"+MOTION CAPTURE SPECIALIST",
		"Amit Doron",
		"",
		"+ART",
		"Claire Praderie-Markham",
		"Viktor Antonov",
		"Corky Lehmkuhl",
		"",
		"+INTRODUCTION ANIMATION",
		"Dominique Drozdz",
		"",
		"+ADDITIONAL LEVEL DESIGN",
		"Aaron Barber",
		"Rhett Baldwin",
		"",
		"+3D CHARACTER ANIMATION TOOLS",
		"Gerry Tyra, SA Technology",
		"",
		"+ADDITIONAL EDITOR TOOL PROGRAMMING",
		"Robert Duffy",
		"",
		"+ADDITIONAL PROGRAMMING",
		"Ryan Feltrin",
		"",
		"+PRODUCTION COORDINATOR",
		"Victoria Sylvester",
		"",
		"+SOUND DESIGN",
		"Gary Bradfield",
		"",
		"+MUSIC BY",
		"Sonic Mayhem",
		"",
		"",
		"",
		"+SPECIAL THANKS",
		"+TO",
		"+OUR FRIENDS AT ID SOFTWARE",
		"",
		"John Carmack",
		"John Cash",
		"Brian Hook",
		"Adrian Carmack",
		"Kevin Cloud",
		"Paul Steed",
		"Tim Willits",
		"Christian Antkow",
		"Paul Jaquays",
		"Brandon James",
		"Todd Hollenshead",
		"Barrett (Bear) Alexander",
		"Dave \"Zoid\" Kirsch",
		"Donna Jackson",
		"",
		"",
		"",
		"+THANKS TO ACTIVISION",
		"+IN PARTICULAR:",
		"",
		"Marty Stratton",
		"Henk \"The Original Ripper\" Hartong",
		"Kevin Kraff",
		"Jamey Gottlieb",
		"Chris Hepburn",
		"",
		"+AND THE GAME TESTERS",
		"",
		"Tim Vanlaw",
		"Doug Jacobs",
		"Steven Rosenthal",
		"David Baker",
		"Chris Campbell",
		"Aaron Casillas",
		"Steve Elwell",
		"Derek Johnstone",
		"Igor Krinitskiy",
		"Samantha Lee",
		"Michael Spann",
		"Chris Toft",
		"Juan Valdes",
		"",
		"+THANKS TO INTERGRAPH COMPUTER SYTEMS",
		"+IN PARTICULAR:",
		"",
		"Michael T. Nicolaou",
		"",
		"",
		"Quake II Mission Pack: The Reckoning",
		"(tm) (C)1998 Id Software, Inc. All",
		"Rights Reserved. Developed by Xatrix",
		"Entertainment, Inc. for Id Software,",
		"Inc. Distributed by Activision Inc.",
		"under license. Quake(R) is a",
		"registered trademark of Id Software,",
		"Inc. Quake II Mission Pack: The",
		"Reckoning(tm), Quake II(tm), the Id",
		"Software name, the \"Q II\"(tm) logo",
		"and id(tm) logo are trademarks of Id",
		"Software, Inc. Activision(R) is a",
		"registered trademark of Activision,",
		"Inc. Xatrix(R) is a registered",
		"trademark of Xatrix Entertainment,",
		"Inc. All other trademarks and trade",
		"names are properties of their",
		"respective owners.",
		null
	};

	private static readonly string[] roguecredits =
	{
		"+QUAKE II MISSION PACK 2: GROUND ZERO",
		"+BY",
		"+ROGUE ENTERTAINMENT, INC.",
		"",
		"+PRODUCED BY",
		"Jim Molinets",
		"",
		"+PROGRAMMING",
		"Peter Mack",
		"Patrick Magruder",
		"",
		"+LEVEL DESIGN",
		"Jim Molinets",
		"Cameron Lamprecht",
		"Berenger Fish",
		"Robert Selitto",
		"Steve Tietze",
		"Steve Thoms",
		"",
		"+ART DIRECTION",
		"Rich Fleider",
		"",
		"+ART",
		"Rich Fleider",
		"Steve Maines",
		"Won Choi",
		"",
		"+ANIMATION SEQUENCES",
		"Creat Studios",
		"Steve Maines",
		"",
		"+ADDITIONAL LEVEL DESIGN",
		"Rich Fleider",
		"Steve Maines",
		"Peter Mack",
		"",
		"+SOUND",
		"James Grunke",
		"",
		"+GROUND ZERO THEME",
		"+AND",
		"+MUSIC BY",
		"Sonic Mayhem",
		"",
		"+VWEP MODELS",
		"Brent \"Hentai\" Dill",
		"",
		"",
		"",
		"+SPECIAL THANKS",
		"+TO",
		"+OUR FRIENDS AT ID SOFTWARE",
		"",
		"John Carmack",
		"John Cash",
		"Brian Hook",
		"Adrian Carmack",
		"Kevin Cloud",
		"Paul Steed",
		"Tim Willits",
		"Christian Antkow",
		"Paul Jaquays",
		"Brandon James",
		"Todd Hollenshead",
		"Barrett (Bear) Alexander",
		"Katherine Anna Kang",
		"Donna Jackson",
		"Dave \"Zoid\" Kirsch",
		"",
		"",
		"",
		"+THANKS TO ACTIVISION",
		"+IN PARTICULAR:",
		"",
		"Marty Stratton",
		"Henk Hartong",
		"Mitch Lasky",
		"Steve Rosenthal",
		"Steve Elwell",
		"",
		"+AND THE GAME TESTERS",
		"",
		"The Ranger Clan",
		"Dave \"Zoid\" Kirsch",
		"Nihilistic Software",
		"Robert Duffy",
		"",
		"And Countless Others",
		"",
		"",
		"",
		"Quake II Mission Pack 2: Ground Zero",
		"(tm) (C)1998 Id Software, Inc. All",
		"Rights Reserved. Developed by Rogue",
		"Entertainment, Inc. for Id Software,",
		"Inc. Distributed by Activision Inc.",
		"under license. Quake(R) is a",
		"registered trademark of Id Software,",
		"Inc. Quake II Mission Pack 2: Ground",
		"Zero(tm), Quake II(tm), the Id",
		"Software name, the \"Q II\"(tm) logo",
		"and id(tm) logo are trademarks of Id",
		"Software, Inc. Activision(R) is a",
		"registered trademark of Activision,",
		"Inc. Rogue(R) is a registered",
		"trademark of Rogue Entertainment,",
		"Inc. All other trademarks and trade",
		"names are properties of their",
		"respective owners.",
		null
	};

	public static void Credits_MenuDraw()
	{
		int i, y;

		/*
		 * * draw the credits
		 */
		for (i = 0, y = (int)(Globals.viddef.getHeight() - (Globals.cls.realtime - Menu.credits_start_time) / 40.0F);
		     Menu.credits[i] != null && y < Globals.viddef.getHeight();
		     y += 10, i++)
		{
			int j, stringoffset = 0;
			var bold = false;

			if (y <= -8)
				continue;

			if (Menu.credits[i].Length > 0 && Menu.credits[i][0] == '+')
			{
				bold = true;
				stringoffset = 1;
			}
			else
			{
				bold = false;
				stringoffset = 0;
			}

			for (j = 0; j + stringoffset < Menu.credits[i].Length; j++)
			{
				int x;
				x = (Globals.viddef.getWidth() - Menu.credits[i].Length * 8 - stringoffset * 8) / 2 + (j + stringoffset) * 8;

				if (bold)
					Globals.re.DrawChar(x, y, Menu.credits[i][j + stringoffset] + 128);
				else
					Globals.re.DrawChar(x, y, Menu.credits[i][j + stringoffset]);
			}
		}

		if (y < 0)
			Menu.credits_start_time = Globals.cls.realtime;
	}

	public static string Credits_Key(int key)
	{
		switch (key)
		{
			case Key.K_ESCAPE:
				Menu.PopMenu();

				break;
		}

		return Menu.menu_out_sound;
	}

	private static readonly Action Menu_Credits = () => { Menu.Menu_Credits_f(); };

	private static void Menu_Credits_f()
	{
		int n;
		var isdeveloper = 0;
		var b = FS.LoadFile("credits");

		if (b != null)
		{
			Menu.creditsBuffer = Encoding.ASCII.GetString(b).Split((char)0)[0];
			var line = Menu.creditsBuffer.Split("\r\n");

			for (n = 0; n < line.Length; n++)
				Menu.creditsIndex[n] = line[n];

			Menu.creditsIndex[n] = null;
			Menu.credits = Menu.creditsIndex;
		}
		else
		{
			isdeveloper = FS.Developer_searchpath(1);

			if (isdeveloper == 1) // xatrix
				Menu.credits = Menu.xatcredits;
			else if (isdeveloper == 2) // ROGUE
				Menu.credits = Menu.roguecredits;
			else
				Menu.credits = Menu.idcredits;
		}

		Menu.credits_start_time = Globals.cls.realtime;
		Menu.PushMenu(() => { Menu.Credits_MenuDraw(); }, key => { return Menu.Credits_Key(key); });
	}

	/*
	 * =============================================================================
	 * 
	 * GAME MENU
	 * 
	 * =============================================================================
	 */
	private static readonly menuframework_s s_game_menu = new();
	private static readonly menuaction_s s_easy_game_action = new();
	private static readonly menuaction_s s_medium_game_action = new();
	private static readonly menuaction_s s_hard_game_action = new();
	private static readonly menuaction_s s_load_game_action = new();
	private static readonly menuaction_s s_save_game_action = new();
	private static readonly menuaction_s s_credits_action = new();
	private static readonly menuseparator_s s_blankline = new();

	private static void StartGame()
	{
		// disable updates and start the cinematic going
		Globals.cl.servercount = -1;
		Menu.ForceMenuOff();
		Cvar.SetValue("deathmatch", 0);
		Cvar.SetValue("coop", 0);
		Cvar.SetValue("gamerules", 0); //PGM
		Cbuf.AddText("loading ; killserver ; wait ; newgame\n");
		Globals.cls.key_dest = Defines.key_game;
	}

	private static void EasyGameFunc(object data)
	{
		Cvar.ForceSet("skill", "0");
		Menu.StartGame();
	}

	private static void MediumGameFunc(object data)
	{
		Cvar.ForceSet("skill", "1");
		Menu.StartGame();
	}

	private static void HardGameFunc(object data)
	{
		Cvar.ForceSet("skill", "2");
		Menu.StartGame();
	}

	private static void LoadGameFunc(object unused)
	{
		Menu.Menu_LoadGame_f();
	}

	private static void SaveGameFunc(object unused)
	{
		Menu.Menu_SaveGame_f();
	}

	private static void CreditsFunc(object unused)
	{
		Menu.Menu_Credits_f();
	}

	private static string[] difficulty_names = { "easy", "medium", "fuckin shitty hard" };

	private static void Game_MenuInit()
	{
		Menu.s_game_menu.x = (int)(Globals.viddef.getWidth() * 0.50);
		Menu.s_game_menu.nitems = 0;
		Menu.s_easy_game_action.type = Defines.MTYPE_ACTION;
		Menu.s_easy_game_action.flags = Defines.QMF_LEFT_JUSTIFY;
		Menu.s_easy_game_action.x = 0;
		Menu.s_easy_game_action.y = 0;
		Menu.s_easy_game_action.name = "easy";
		Menu.s_easy_game_action.callback = o => { Menu.EasyGameFunc(o); };
		Menu.s_medium_game_action.type = Defines.MTYPE_ACTION;
		Menu.s_medium_game_action.flags = Defines.QMF_LEFT_JUSTIFY;
		Menu.s_medium_game_action.x = 0;
		Menu.s_medium_game_action.y = 10;
		Menu.s_medium_game_action.name = "medium";
		Menu.s_medium_game_action.callback = o => { Menu.MediumGameFunc(o); };
		Menu.s_hard_game_action.type = Defines.MTYPE_ACTION;
		Menu.s_hard_game_action.flags = Defines.QMF_LEFT_JUSTIFY;
		Menu.s_hard_game_action.x = 0;
		Menu.s_hard_game_action.y = 20;
		Menu.s_hard_game_action.name = "hard";
		Menu.s_hard_game_action.callback = o => { Menu.HardGameFunc(o); };
		Menu.s_blankline.type = Defines.MTYPE_SEPARATOR;
		Menu.s_load_game_action.type = Defines.MTYPE_ACTION;
		Menu.s_load_game_action.flags = Defines.QMF_LEFT_JUSTIFY;
		Menu.s_load_game_action.x = 0;
		Menu.s_load_game_action.y = 40;
		Menu.s_load_game_action.name = "load game";
		Menu.s_load_game_action.callback = o => { Menu.LoadGameFunc(o); };
		Menu.s_save_game_action.type = Defines.MTYPE_ACTION;
		Menu.s_save_game_action.flags = Defines.QMF_LEFT_JUSTIFY;
		Menu.s_save_game_action.x = 0;
		Menu.s_save_game_action.y = 50;
		Menu.s_save_game_action.name = "save game";
		Menu.s_save_game_action.callback = o => { Menu.SaveGameFunc(o); };
		Menu.s_credits_action.type = Defines.MTYPE_ACTION;
		Menu.s_credits_action.flags = Defines.QMF_LEFT_JUSTIFY;
		Menu.s_credits_action.x = 0;
		Menu.s_credits_action.y = 60;
		Menu.s_credits_action.name = "credits";
		Menu.s_credits_action.callback = o => { Menu.CreditsFunc(o); };
		Menu.Menu_AddItem(Menu.s_game_menu, Menu.s_easy_game_action);
		Menu.Menu_AddItem(Menu.s_game_menu, Menu.s_medium_game_action);
		Menu.Menu_AddItem(Menu.s_game_menu, Menu.s_hard_game_action);
		Menu.Menu_AddItem(Menu.s_game_menu, Menu.s_blankline);
		Menu.Menu_AddItem(Menu.s_game_menu, Menu.s_load_game_action);
		Menu.Menu_AddItem(Menu.s_game_menu, Menu.s_save_game_action);
		Menu.Menu_AddItem(Menu.s_game_menu, Menu.s_blankline);
		Menu.Menu_AddItem(Menu.s_game_menu, Menu.s_credits_action);
		Menu.Menu_Center(Menu.s_game_menu);
	}

	private static void Game_MenuDraw()
	{
		Menu.Banner("m_banner_game");
		Menu.Menu_AdjustCursor(Menu.s_game_menu, 1);
		Menu.Menu_Draw(Menu.s_game_menu);
	}

	private static string Game_MenuKey(int key)
	{
		return Menu.Default_MenuKey(Menu.s_game_menu, key);
	}

	private static readonly Action Menu_Game = () => { Menu.Menu_Game_f(); };

	private static void Menu_Game_f()
	{
		Menu.Game_MenuInit();
		Menu.PushMenu(() => { Menu.Game_MenuDraw(); }, key => { return Menu.Game_MenuKey(key); });
	}

	/*
	 * =============================================================================
	 * 
	 * LOADGAME MENU
	 * 
	 * =============================================================================
	 */
	public static readonly int MAX_SAVEGAMES = 15;
	private static readonly menuframework_s s_savegame_menu = new();
	private static readonly menuframework_s s_loadgame_menu = new();
	private static readonly menuaction_s[] s_loadgame_actions = new menuaction_s[Menu.MAX_SAVEGAMES];

	private static void Menu1()
	{
		for (var n = 0; n < Menu.MAX_SAVEGAMES; n++)
			Menu.s_loadgame_actions[n] = new();
	}

	//String m_savestrings[] = new String [MAX_SAVEGAMES][32];
	private static readonly string[] m_savestrings = new string[Menu.MAX_SAVEGAMES];

	private static void Menu2()
	{
		for (var n = 0; n < Menu.MAX_SAVEGAMES; n++)
			Menu.m_savestrings[n] = "";
	}

	private static readonly bool[] m_savevalid = new bool[Menu.MAX_SAVEGAMES];

	/** Search the save dir for saved games and their names. */
	private static void Create_Savestrings()
	{
		int i;
		BinaryReader f;
		string name;

		for (i = 0; i < Menu.MAX_SAVEGAMES; i++)
		{
			Menu.m_savestrings[i] = "<EMPTY>";
			name = FS.Gamedir() + "/save/save" + i + "/server.ssv";

			try
			{
				f = new(File.OpenRead(name));

				if (f == null)
				{
					Menu.m_savestrings[i] = "<EMPTY>";
					Menu.m_savevalid[i] = false;
				}
				else
				{
					var str = f.ReadStringQ();

					if (str != null)
						Menu.m_savestrings[i] = str;

					f.Close();
					Menu.m_savevalid[i] = true;
				}
			}
			catch (Exception)
			{
				Menu.m_savestrings[i] = "<EMPTY>";
				Menu.m_savevalid[i] = false;
			}

			;
		}
	}

	private static void LoadGameCallback(object self)
	{
		var a = (menuaction_s)self;

		if (Menu.m_savevalid[a.localdata[0]])
			Cbuf.AddText("load save" + a.localdata[0] + "\n");

		Menu.ForceMenuOff();
	}

	private static void LoadGame_MenuInit()
	{
		int i;
		Menu.s_loadgame_menu.x = Globals.viddef.getWidth() / 2 - 120;
		Menu.s_loadgame_menu.y = Globals.viddef.getHeight() / 2 - 58;
		Menu.s_loadgame_menu.nitems = 0;
		Menu.Create_Savestrings();

		for (i = 0; i < Menu.MAX_SAVEGAMES; i++)
		{
			Menu.s_loadgame_actions[i].name = Menu.m_savestrings[i];
			Menu.s_loadgame_actions[i].flags = Defines.QMF_LEFT_JUSTIFY;
			Menu.s_loadgame_actions[i].localdata[0] = i;
			Menu.s_loadgame_actions[i].callback = o => { Menu.LoadGameCallback(o); };
			Menu.s_loadgame_actions[i].x = 0;
			Menu.s_loadgame_actions[i].y = i * 10;

			if (i > 0) // separate from autosave
				Menu.s_loadgame_actions[i].y += 10;

			Menu.s_loadgame_actions[i].type = Defines.MTYPE_ACTION;
			Menu.Menu_AddItem(Menu.s_loadgame_menu, Menu.s_loadgame_actions[i]);
		}
	}

	private static void LoadGame_MenuDraw()
	{
		Menu.Banner("m_banner_load_game");

		//		Menu_AdjustCursor( &s_loadgame_menu, 1 );
		Menu.Menu_Draw(Menu.s_loadgame_menu);
	}

	private static string LoadGame_MenuKey(int key)
	{
		if (key == Key.K_ESCAPE || key == Key.K_ENTER)
		{
			Menu.s_savegame_menu.cursor = Menu.s_loadgame_menu.cursor - 1;

			if (Menu.s_savegame_menu.cursor < 0)
				Menu.s_savegame_menu.cursor = 0;
		}

		return Menu.Default_MenuKey(Menu.s_loadgame_menu, key);
	}

	private static readonly Action Menu_LoadGame = () => { Menu.Menu_LoadGame_f(); };

	private static void Menu_LoadGame_f()
	{
		Menu.LoadGame_MenuInit();
		Menu.PushMenu(() => { Menu.LoadGame_MenuDraw(); }, key => { return Menu.LoadGame_MenuKey(key); });
	}

	/*
	 * =============================================================================
	 * 
	 * SAVEGAME MENU
	 * 
	 * =============================================================================
	 */
	//static menuframework_s s_savegame_menu;
	private static readonly menuaction_s[] s_savegame_actions = new menuaction_s[Menu.MAX_SAVEGAMES];

	private static void Menu3()
	{
		for (var n = 0; n < Menu.MAX_SAVEGAMES; n++)
			Menu.s_savegame_actions[n] = new();
	}

	private static void SaveGameCallback(object self)
	{
		var a = (menuaction_s)self;
		Cbuf.AddText("save save" + a.localdata[0] + "\n");
		Menu.ForceMenuOff();
	}

	private static void SaveGame_MenuDraw()
	{
		Menu.Banner("m_banner_save_game");
		Menu.Menu_AdjustCursor(Menu.s_savegame_menu, 1);
		Menu.Menu_Draw(Menu.s_savegame_menu);
	}

	private static void SaveGame_MenuInit()
	{
		int i;
		Menu.s_savegame_menu.x = Globals.viddef.getWidth() / 2 - 120;
		Menu.s_savegame_menu.y = Globals.viddef.getHeight() / 2 - 58;
		Menu.s_savegame_menu.nitems = 0;
		Menu.Create_Savestrings();

		// don't include the autosave slot
		for (i = 0; i < Menu.MAX_SAVEGAMES - 1; i++)
		{
			Menu.s_savegame_actions[i].name = Menu.m_savestrings[i + 1];
			Menu.s_savegame_actions[i].localdata[0] = i + 1;
			Menu.s_savegame_actions[i].flags = Defines.QMF_LEFT_JUSTIFY;
			Menu.s_savegame_actions[i].callback = o => { Menu.SaveGameCallback(o); };
			Menu.s_savegame_actions[i].x = 0;
			Menu.s_savegame_actions[i].y = i * 10;
			Menu.s_savegame_actions[i].type = Defines.MTYPE_ACTION;
			Menu.Menu_AddItem(Menu.s_savegame_menu, Menu.s_savegame_actions[i]);
		}
	}

	private static string SaveGame_MenuKey(int key)
	{
		if (key == Key.K_ENTER || key == Key.K_ESCAPE)
		{
			Menu.s_loadgame_menu.cursor = Menu.s_savegame_menu.cursor - 1;

			if (Menu.s_loadgame_menu.cursor < 0)
				Menu.s_loadgame_menu.cursor = 0;
		}

		return Menu.Default_MenuKey(Menu.s_savegame_menu, key);
	}

	private static readonly Action Menu_SaveGame = () => { Menu.Menu_SaveGame_f(); };

	private static void Menu_SaveGame_f()
	{
		if (0 == Globals.server_state)
			return; // not playing a game

		Menu.SaveGame_MenuInit();
		Menu.PushMenu(() => { Menu.SaveGame_MenuDraw(); }, key => { return Menu.SaveGame_MenuKey(key); });
		Menu.Create_Savestrings();
	}

	/*
	 * =============================================================================
	 * 
	 * JOIN SERVER MENU
	 * 
	 * =============================================================================
	 */
	private static readonly menuframework_s s_joinserver_menu = new();
	private static readonly menuseparator_s s_joinserver_server_title = new();
	private static readonly menuaction_s s_joinserver_search_action = new();
	private static readonly menuaction_s s_joinserver_address_book_action = new();
	private static readonly netadr_t[] local_server_netadr = new netadr_t[Defines.MAX_LOCAL_SERVERS];
	private static readonly string[] local_server_names = new string[Defines.MAX_LOCAL_SERVERS]; //[80];
	private static readonly menuaction_s[] s_joinserver_server_actions = new menuaction_s[Defines.MAX_LOCAL_SERVERS];

	//	   user readable information
	//	   network address
	private static void Menu4()
	{
		for (var n = 0; n < Defines.MAX_LOCAL_SERVERS; n++)
		{
			Menu.local_server_netadr[n] = new();
			Menu.local_server_names[n] = "";
			Menu.s_joinserver_server_actions[n] = new();
			Menu.s_joinserver_server_actions[n].n = n;
		}
	}

	private static int m_num_servers;

	public static void AddToServerList(netadr_t adr, string info)
	{
		int i;

		if (Menu.m_num_servers == Defines.MAX_LOCAL_SERVERS)
			return;

		var x = info.Trim();

		// ignore if duplicated

		for (i = 0; i < Menu.m_num_servers; i++)
		{
			if (x.Equals(Menu.local_server_names[i]))
				return;
		}

		Menu.local_server_netadr[Menu.m_num_servers].set(adr);
		Menu.local_server_names[Menu.m_num_servers] = x;
		Menu.s_joinserver_server_actions[Menu.m_num_servers].name = x;
		Menu.m_num_servers++;
	}

	private static void JoinServerFunc(object self)
	{
		string buffer;
		int index;
		index = ((menucommon_s)self).n;

		if (Lib.Q_stricmp(Menu.local_server_names[index], Defines.NO_SERVER_STRING) == 0)
			return;

		if (index >= Menu.m_num_servers)
			return;

		buffer = "connect " + NET.AdrToString(Menu.local_server_netadr[index]) + "\n";
		Cbuf.AddText(buffer);
		Menu.ForceMenuOff();
	}

	private static void AddressBookFunc(object self)
	{
		Menu.Menu_AddressBook_f();
	}

	private static void NullCursorDraw(object self)
	{
	}

	private static void SearchLocalGames()
	{
		int i;
		Menu.m_num_servers = 0;

		for (i = 0; i < Defines.MAX_LOCAL_SERVERS; i++)
			Menu.local_server_names[i] = Defines.NO_SERVER_STRING;

		Menu.DrawTextBox(8, 120 - 48, 36, 3);
		Menu.Print(16 + 16, 120 - 48 + 8, "Searching for local servers, this");
		Menu.Print(16 + 16, 120 - 48 + 16, "could take up to a minute, so");
		Menu.Print(16 + 16, 120 - 48 + 24, "please be patient.");

		// the text box won't show up unless we do a buffer swap
		Globals.re.EndFrame();

		// send out info packets
		Cl.PingServersF();
	}

	private static void SearchLocalGamesFunc(object self)
	{
		Menu.SearchLocalGames();
	}

	private static void JoinServer_MenuInit()
	{
		int i;
		Menu.s_joinserver_menu.x = (int)(Globals.viddef.getWidth() * 0.50 - 120);
		Menu.s_joinserver_menu.nitems = 0;
		Menu.s_joinserver_address_book_action.type = Defines.MTYPE_ACTION;
		Menu.s_joinserver_address_book_action.name = "address book";
		Menu.s_joinserver_address_book_action.flags = Defines.QMF_LEFT_JUSTIFY;
		Menu.s_joinserver_address_book_action.x = 0;
		Menu.s_joinserver_address_book_action.y = 0;
		Menu.s_joinserver_address_book_action.callback = o => { Menu.AddressBookFunc(o); };
		Menu.s_joinserver_search_action.type = Defines.MTYPE_ACTION;
		Menu.s_joinserver_search_action.name = "refresh server list";
		Menu.s_joinserver_search_action.flags = Defines.QMF_LEFT_JUSTIFY;
		Menu.s_joinserver_search_action.x = 0;
		Menu.s_joinserver_search_action.y = 10;
		Menu.s_joinserver_search_action.callback = o => { Menu.SearchLocalGamesFunc(o); };
		Menu.s_joinserver_search_action.statusbar = "search for servers";
		Menu.s_joinserver_server_title.type = Defines.MTYPE_SEPARATOR;
		Menu.s_joinserver_server_title.name = "connect to...";
		Menu.s_joinserver_server_title.x = 80;
		Menu.s_joinserver_server_title.y = 30;

		for (i = 0; i < Defines.MAX_LOCAL_SERVERS; i++)
		{
			Menu.s_joinserver_server_actions[i].type = Defines.MTYPE_ACTION;
			Menu.local_server_names[i] = Defines.NO_SERVER_STRING;
			Menu.s_joinserver_server_actions[i].name = Menu.local_server_names[i];
			Menu.s_joinserver_server_actions[i].flags = Defines.QMF_LEFT_JUSTIFY;
			Menu.s_joinserver_server_actions[i].x = 0;
			Menu.s_joinserver_server_actions[i].y = 40 + i * 10;
			Menu.s_joinserver_server_actions[i].callback = o => { Menu.JoinServerFunc(o); };
			Menu.s_joinserver_server_actions[i].statusbar = "press ENTER to connect";
		}

		Menu.Menu_AddItem(Menu.s_joinserver_menu, Menu.s_joinserver_address_book_action);
		Menu.Menu_AddItem(Menu.s_joinserver_menu, Menu.s_joinserver_server_title);
		Menu.Menu_AddItem(Menu.s_joinserver_menu, Menu.s_joinserver_search_action);

		for (i = 0; i < 8; i++)
			Menu.Menu_AddItem(Menu.s_joinserver_menu, Menu.s_joinserver_server_actions[i]);

		Menu.Menu_Center(Menu.s_joinserver_menu);
		Menu.SearchLocalGames();
	}

	private static void JoinServer_MenuDraw()
	{
		Menu.Banner("m_banner_join_server");
		Menu.Menu_Draw(Menu.s_joinserver_menu);
	}

	private static string JoinServer_MenuKey(int key)
	{
		return Menu.Default_MenuKey(Menu.s_joinserver_menu, key);
	}

	private static readonly Action Menu_JoinServer = () => { Menu.Menu_JoinServer_f(); };

	private static void Menu_JoinServer_f()
	{
		Menu.JoinServer_MenuInit();
		Menu.PushMenu(() => { Menu.JoinServer_MenuDraw(); }, key => { return Menu.JoinServer_MenuKey(key); });
	}

	/*
	 * =============================================================================
	 * 
	 * START SERVER MENU
	 * 
	 * =============================================================================
	 */
	private static readonly menuframework_s s_startserver_menu = new();
	private static string[] mapnames;
	private static int nummaps;
	private static readonly menuaction_s s_startserver_start_action = new();
	private static readonly menuaction_s s_startserver_dmoptions_action = new();
	private static readonly menufield_s s_timelimit_field = new();
	private static readonly menufield_s s_fraglimit_field = new();
	private static readonly menufield_s s_maxclients_field = new();
	private static readonly menufield_s s_hostname_field = new();
	private static readonly menulist_s s_startmap_list = new();
	private static readonly menulist_s s_rules_box = new();

	private static void DMOptionsFunc(object self)
	{
		if (Menu.s_rules_box.curvalue == 1)
			return;

		Menu.Menu_DMOptions_f();
	}

	private static void RulesChangeFunc(object self)
	{
		// DM
		if (Menu.s_rules_box.curvalue == 0)
		{
			Menu.s_maxclients_field.statusbar = null;
			Menu.s_startserver_dmoptions_action.statusbar = null;
		}
		else if (Menu.s_rules_box.curvalue == 1)

			// coop // PGM
		{
			Menu.s_maxclients_field.statusbar = "4 maximum for cooperative";

			if (Lib.atoi(Menu.s_maxclients_field.buffer.ToString()) > 4)
				Menu.s_maxclients_field.buffer = new("4");

			Menu.s_startserver_dmoptions_action.statusbar = "N/A for cooperative";
		}

		//	  =====
		//	  PGM
		// ROGUE GAMES
		else if (FS.Developer_searchpath(2) == 2)
		{
			if (Menu.s_rules_box.curvalue == 2) // tag
			{
				Menu.s_maxclients_field.statusbar = null;
				Menu.s_startserver_dmoptions_action.statusbar = null;
			}

			/*
			 * else if(s_rules_box.curvalue == 3) // deathball {
			 * s_maxclients_field.statusbar = null;
			 * s_startserver_dmoptions_action.statusbar = null; }
			 */
		}

		//	  PGM
		//	  =====
	}

	private static void StartServerActionFunc(object self)
	{
		//char startmap[1024];
		string startmap;
		int timelimit;
		int fraglimit;
		int maxclients;
		string spot;

		//strcpy(startmap, strchr(mapnames[s_startmap_list.curvalue], '\n') +
		// 1);
		var x = Menu.mapnames[Menu.s_startmap_list.curvalue];
		var pos = x.IndexOf('\n');

		if (pos == -1)
			startmap = x;
		else
			startmap = x[(pos + 1)..];

		maxclients = Lib.atoi(Menu.s_maxclients_field.buffer.ToString());
		timelimit = Lib.atoi(Menu.s_timelimit_field.buffer.ToString());
		fraglimit = Lib.atoi(Menu.s_fraglimit_field.buffer.ToString());
		Cvar.SetValue("maxclients", Menu.ClampCvar(0, maxclients, maxclients));
		Cvar.SetValue("timelimit", Menu.ClampCvar(0, timelimit, timelimit));
		Cvar.SetValue("fraglimit", Menu.ClampCvar(0, fraglimit, fraglimit));
		Cvar.Set("hostname", Menu.s_hostname_field.buffer.ToString());

		//		Cvar.SetValue ("deathmatch", !s_rules_box.curvalue );
		//		Cvar.SetValue ("coop", s_rules_box.curvalue );

		//	  PGM
		if (Menu.s_rules_box.curvalue < 2 || FS.Developer_searchpath(2) != 2)
		{
			Cvar.SetValue("deathmatch", 1 - (int)Menu.s_rules_box.curvalue);
			Cvar.SetValue("coop", Menu.s_rules_box.curvalue);
			Cvar.SetValue("gamerules", 0);
		}
		else
		{
			Cvar.SetValue("deathmatch", 1);

			// deathmatch is always true for rogue games, right?
			Cvar.SetValue("coop", 0);

			// FIXME - this might need to depend on which game we're running
			Cvar.SetValue("gamerules", Menu.s_rules_box.curvalue);
		}

		//	  PGM
		spot = null;

		if (Menu.s_rules_box.curvalue == 1) // PGM
		{
			if (Lib.Q_stricmp(startmap, "bunk1") == 0)
				spot = "start";
			else if (Lib.Q_stricmp(startmap, "mintro") == 0)
				spot = "start";
			else if (Lib.Q_stricmp(startmap, "fact1") == 0)
				spot = "start";
			else if (Lib.Q_stricmp(startmap, "power1") == 0)
				spot = "pstart";
			else if (Lib.Q_stricmp(startmap, "biggun") == 0)
				spot = "bstart";
			else if (Lib.Q_stricmp(startmap, "hangar1") == 0)
				spot = "unitstart";
			else if (Lib.Q_stricmp(startmap, "city1") == 0)
				spot = "unitstart";
			else if (Lib.Q_stricmp(startmap, "boss1") == 0)
				spot = "bosstart";
		}

		if (spot != null)
		{
			if (Globals.server_state != 0)
				Cbuf.AddText("disconnect\n");

			Cbuf.AddText("gamemap \"*" + startmap + "$" + spot + "\"\n");
		}
		else
			Cbuf.AddText("map " + startmap + "\n");

		Menu.ForceMenuOff();
	}

	private static readonly string[] dm_coop_names = { "deathmatch", "cooperative" };
	private static readonly string[] dm_coop_names_rogue = { "deathmatch", "cooperative", "tag" };

	private static void StartServer_MenuInit()
	{
		//	  =======
		//	  PGM
		//	  =======
		byte[] buffer = null;
		string mapsname;
		string s;
		int i;
		Stream fp = null;
		/*
		 * * load the list of map names
		 */
		mapsname = FS.Gamedir() + "/maps.lst";

		if (File.Exists(mapsname))
			fp = File.OpenRead(mapsname);

		if (fp == null)
		{
			buffer = FS.LoadFile("maps.lst");

			if (buffer == null)

				//if ((length = FS_LoadFile("maps.lst", (Object *) & buffer))
				// == -1)
				Com.Error(Defines.ERR_DROP, "couldn't find maps.lst\n");
		}
		else
		{
			try
			{
				var len = (int)fp.Length;
				buffer = new byte[len];
				fp.Read(buffer);
			}
			catch (Exception)
			{
				Com.Error(Defines.ERR_DROP, "couldn't load maps.lst\n");
			}
		}

		s = Encoding.ASCII.GetString(buffer).Split((char)0)[0];
		var lines = s.Split("\r\n");
		Menu.nummaps = lines.Length;

		if (Menu.nummaps == 0)
			Com.Error(Defines.ERR_DROP, "no maps in maps.lst\n");

		Menu.mapnames = new string[Menu.nummaps];

		for (i = 0; i < Menu.nummaps; i++)
		{
			string shortname, longname, scratch;
			Com.ParseHelp ph = new(lines[i]);
			shortname = Com.Parse(ph).ToUpper();
			longname = Com.Parse(ph);
			scratch = longname + "\n" + shortname;
			Menu.mapnames[i] = scratch;
		}

		if (fp != null)
		{
			fp.Close();
			fp = null;
		}
		else
			FS.FreeFile(buffer);

		/*
		 * * initialize the menu stuff
		 */
		Menu.s_startserver_menu.x = (int)(Globals.viddef.getWidth() * 0.50);
		Menu.s_startserver_menu.nitems = 0;
		Menu.s_startmap_list.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_startmap_list.x = 0;
		Menu.s_startmap_list.y = 0;
		Menu.s_startmap_list.name = "initial map";
		Menu.s_startmap_list.itemnames = Menu.mapnames;
		Menu.s_rules_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_rules_box.x = 0;
		Menu.s_rules_box.y = 20;
		Menu.s_rules_box.name = "rules";

		//	  PGM - rogue games only available with rogue DLL.
		if (FS.Developer_searchpath(2) == 2)
			Menu.s_rules_box.itemnames = Menu.dm_coop_names_rogue;
		else
			Menu.s_rules_box.itemnames = Menu.dm_coop_names;

		//	  PGM

		if (Cvar.VariableValue("coop") != 0)
			Menu.s_rules_box.curvalue = 1;
		else
			Menu.s_rules_box.curvalue = 0;

		Menu.s_rules_box.callback = o => { Menu.RulesChangeFunc(o); };
		Menu.s_timelimit_field.type = Defines.MTYPE_FIELD;
		Menu.s_timelimit_field.name = "time limit";
		Menu.s_timelimit_field.flags = Defines.QMF_NUMBERSONLY;
		Menu.s_timelimit_field.x = 0;
		Menu.s_timelimit_field.y = 36;
		Menu.s_timelimit_field.statusbar = "0 = no limit";
		Menu.s_timelimit_field.length = 3;
		Menu.s_timelimit_field.visible_length = 3;
		Menu.s_timelimit_field.buffer = new(Cvar.VariableString("timelimit"));
		Menu.s_fraglimit_field.type = Defines.MTYPE_FIELD;
		Menu.s_fraglimit_field.name = "frag limit";
		Menu.s_fraglimit_field.flags = Defines.QMF_NUMBERSONLY;
		Menu.s_fraglimit_field.x = 0;
		Menu.s_fraglimit_field.y = 54;
		Menu.s_fraglimit_field.statusbar = "0 = no limit";
		Menu.s_fraglimit_field.length = 3;
		Menu.s_fraglimit_field.visible_length = 3;
		Menu.s_fraglimit_field.buffer = new(Cvar.VariableString("fraglimit"));
		/*
		 * * maxclients determines the maximum number of players that can join *
		 * the game. If maxclients is only "1" then we should default the menu *
		 * option to 8 players, otherwise use whatever its current value is. *
		 * Clamping will be done when the server is actually started.
		 */
		Menu.s_maxclients_field.type = Defines.MTYPE_FIELD;
		Menu.s_maxclients_field.name = "max players";
		Menu.s_maxclients_field.flags = Defines.QMF_NUMBERSONLY;
		Menu.s_maxclients_field.x = 0;
		Menu.s_maxclients_field.y = 72;
		Menu.s_maxclients_field.statusbar = null;
		Menu.s_maxclients_field.length = 3;
		Menu.s_maxclients_field.visible_length = 3;

		if (Cvar.VariableValue("maxclients") == 1)
			Menu.s_maxclients_field.buffer = new("8");
		else
			Menu.s_maxclients_field.buffer = new(Cvar.VariableString("maxclients"));

		Menu.s_hostname_field.type = Defines.MTYPE_FIELD;
		Menu.s_hostname_field.name = "hostname";
		Menu.s_hostname_field.flags = 0;
		Menu.s_hostname_field.x = 0;
		Menu.s_hostname_field.y = 90;
		Menu.s_hostname_field.statusbar = null;
		Menu.s_hostname_field.length = 12;
		Menu.s_hostname_field.visible_length = 12;
		Menu.s_hostname_field.buffer = new(Cvar.VariableString("hostname"));
		Menu.s_hostname_field.cursor = Menu.s_hostname_field.buffer.Length;
		Menu.s_startserver_dmoptions_action.type = Defines.MTYPE_ACTION;
		Menu.s_startserver_dmoptions_action.name = " deathmatch flags";
		Menu.s_startserver_dmoptions_action.flags = Defines.QMF_LEFT_JUSTIFY;
		Menu.s_startserver_dmoptions_action.x = 24;
		Menu.s_startserver_dmoptions_action.y = 108;
		Menu.s_startserver_dmoptions_action.statusbar = null;
		Menu.s_startserver_dmoptions_action.callback = o => { Menu.DMOptionsFunc(o); };
		Menu.s_startserver_start_action.type = Defines.MTYPE_ACTION;
		Menu.s_startserver_start_action.name = " begin";
		Menu.s_startserver_start_action.flags = Defines.QMF_LEFT_JUSTIFY;
		Menu.s_startserver_start_action.x = 24;
		Menu.s_startserver_start_action.y = 128;
		Menu.s_startserver_start_action.callback = o => { Menu.StartServerActionFunc(o); };
		Menu.Menu_AddItem(Menu.s_startserver_menu, Menu.s_startmap_list);
		Menu.Menu_AddItem(Menu.s_startserver_menu, Menu.s_rules_box);
		Menu.Menu_AddItem(Menu.s_startserver_menu, Menu.s_timelimit_field);
		Menu.Menu_AddItem(Menu.s_startserver_menu, Menu.s_fraglimit_field);
		Menu.Menu_AddItem(Menu.s_startserver_menu, Menu.s_maxclients_field);
		Menu.Menu_AddItem(Menu.s_startserver_menu, Menu.s_hostname_field);
		Menu.Menu_AddItem(Menu.s_startserver_menu, Menu.s_startserver_dmoptions_action);
		Menu.Menu_AddItem(Menu.s_startserver_menu, Menu.s_startserver_start_action);
		Menu.Menu_Center(Menu.s_startserver_menu);

		// call this now to set proper inital state
		Menu.RulesChangeFunc(null);
	}

	private static void StartServer_MenuDraw()
	{
		Menu.Menu_Draw(Menu.s_startserver_menu);
	}

	private static string StartServer_MenuKey(int key)
	{
		if (key == Key.K_ESCAPE)
		{
			if (Menu.mapnames != null)
			{
				int i;

				for (i = 0; i < Menu.nummaps; i++)
					Menu.mapnames[i] = null;
			}

			Menu.mapnames = null;
			Menu.nummaps = 0;
		}

		return Menu.Default_MenuKey(Menu.s_startserver_menu, key);
	}

	private static readonly Action Menu_StartServer = () => { Menu.Menu_StartServer_f(); };
	private static readonly Action startServer_MenuDraw = () => { Menu.StartServer_MenuDraw(); };
	private static readonly Func<int, string> startServer_MenuKey = key => { return Menu.StartServer_MenuKey(key); };

	private static void Menu_StartServer_f()
	{
		Menu.StartServer_MenuInit();
		Menu.PushMenu(Menu.startServer_MenuDraw, Menu.startServer_MenuKey);
	}

	/*
	 * =============================================================================
	 * 
	 * DMOPTIONS BOOK MENU
	 * 
	 * =============================================================================
	 */
	private static string dmoptions_statusbar; //[128];
	private static readonly menuframework_s s_dmoptions_menu = new();
	private static readonly menulist_s s_friendlyfire_box = new();
	private static readonly menulist_s s_falls_box = new();
	private static readonly menulist_s s_weapons_stay_box = new();
	private static readonly menulist_s s_instant_powerups_box = new();
	private static readonly menulist_s s_powerups_box = new();
	private static readonly menulist_s s_health_box = new();
	private static readonly menulist_s s_spawn_farthest_box = new();
	private static readonly menulist_s s_teamplay_box = new();
	private static readonly menulist_s s_samelevel_box = new();
	private static readonly menulist_s s_force_respawn_box = new();
	private static readonly menulist_s s_armor_box = new();
	private static readonly menulist_s s_allow_exit_box = new();
	private static readonly menulist_s s_infinite_ammo_box = new();
	private static readonly menulist_s s_fixed_fov_box = new();
	private static readonly menulist_s s_quad_drop_box = new();

	//	  ROGUE
	private static readonly menulist_s s_no_mines_box = new();
	private static readonly menulist_s s_no_nukes_box = new();
	private static readonly menulist_s s_stack_double_box = new();
	private static readonly menulist_s s_no_spheres_box = new();

	//	  ROGUE

	private static void setvalue(int flags)
	{
		Cvar.SetValue("dmflags", flags);
		Menu.dmoptions_statusbar = "dmflags = " + flags;
	}

	private static void DMFlagCallback(object self)
	{
		var f = (menulist_s)self;
		int flags;
		var bit = 0;
		flags = (int)Cvar.VariableValue("dmflags");

		if (f == Menu.s_friendlyfire_box)
		{
			if (f.curvalue != 0)
				flags &= ~Defines.DF_NO_FRIENDLY_FIRE;
			else
				flags |= Defines.DF_NO_FRIENDLY_FIRE;

			Menu.setvalue(flags);

			return;
		}

		if (f == Menu.s_falls_box)
		{
			if (f.curvalue != 0)
				flags &= ~Defines.DF_NO_FALLING;
			else
				flags |= Defines.DF_NO_FALLING;

			Menu.setvalue(flags);

			return;
		}

		if (f == Menu.s_weapons_stay_box)
			bit = Defines.DF_WEAPONS_STAY;
		else if (f == Menu.s_instant_powerups_box)
			bit = Defines.DF_INSTANT_ITEMS;
		else if (f == Menu.s_allow_exit_box)
			bit = Defines.DF_ALLOW_EXIT;
		else if (f == Menu.s_powerups_box)
		{
			if (f.curvalue != 0)
				flags &= ~Defines.DF_NO_ITEMS;
			else
				flags |= Defines.DF_NO_ITEMS;

			Menu.setvalue(flags);

			return;
		}
		else if (f == Menu.s_health_box)
		{
			if (f.curvalue != 0)
				flags &= ~Defines.DF_NO_HEALTH;
			else
				flags |= Defines.DF_NO_HEALTH;

			Menu.setvalue(flags);

			return;
		}
		else if (f == Menu.s_spawn_farthest_box)
			bit = Defines.DF_SPAWN_FARTHEST;
		else if (f == Menu.s_teamplay_box)
		{
			if (f.curvalue == 1)
			{
				flags |= Defines.DF_SKINTEAMS;
				flags &= ~Defines.DF_MODELTEAMS;
			}
			else if (f.curvalue == 2)
			{
				flags |= Defines.DF_MODELTEAMS;
				flags &= ~Defines.DF_SKINTEAMS;
			}
			else
				flags &= ~(Defines.DF_MODELTEAMS | Defines.DF_SKINTEAMS);

			Menu.setvalue(flags);

			return;
		}
		else if (f == Menu.s_samelevel_box)
			bit = Defines.DF_SAME_LEVEL;
		else if (f == Menu.s_force_respawn_box)
			bit = Defines.DF_FORCE_RESPAWN;
		else if (f == Menu.s_armor_box)
		{
			if (f.curvalue != 0)
				flags &= ~Defines.DF_NO_ARMOR;
			else
				flags |= Defines.DF_NO_ARMOR;

			Menu.setvalue(flags);

			return;
		}
		else if (f == Menu.s_infinite_ammo_box)
			bit = Defines.DF_INFINITE_AMMO;
		else if (f == Menu.s_fixed_fov_box)
			bit = Defines.DF_FIXED_FOV;
		else if (f == Menu.s_quad_drop_box)
			bit = Defines.DF_QUAD_DROP;

		//	  =======
		//	  ROGUE
		else if (FS.Developer_searchpath(2) == 2)
		{
			if (f == Menu.s_no_mines_box)
				bit = Defines.DF_NO_MINES;
			else if (f == Menu.s_no_nukes_box)
				bit = Defines.DF_NO_NUKES;
			else if (f == Menu.s_stack_double_box)
				bit = Defines.DF_NO_STACK_DOUBLE;
			else if (f == Menu.s_no_spheres_box)
				bit = Defines.DF_NO_SPHERES;
		}

		//	  ROGUE
		//	  =======

		if (f != null)
		{
			if (f.curvalue == 0)
				flags &= ~bit;
			else
				flags |= bit;
		}

		Cvar.SetValue("dmflags", flags);
		Menu.dmoptions_statusbar = "dmflags = " + flags;
	}

	//static String yes_no_names[] = { "no", "yes", 0 };
	private static readonly string[] teamplay_names = { "disabled", "by skin", "by model" };

	private static void DMOptions_MenuInit()
	{
		var dmflags = (int)Cvar.VariableValue("dmflags");
		var y = 0;
		Menu.s_dmoptions_menu.x = (int)(Globals.viddef.getWidth() * 0.50);
		Menu.s_dmoptions_menu.nitems = 0;
		Menu.s_falls_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_falls_box.x = 0;
		Menu.s_falls_box.y = y;
		Menu.s_falls_box.name = "falling damage";
		Menu.s_falls_box.callback = o => { Menu.DMFlagCallback(o); };
		Menu.s_falls_box.itemnames = Menu.yes_no_names;
		Menu.s_falls_box.curvalue = (dmflags & Defines.DF_NO_FALLING) == 0 ? 1 : 0;
		Menu.s_weapons_stay_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_weapons_stay_box.x = 0;
		Menu.s_weapons_stay_box.y = y += 10;
		Menu.s_weapons_stay_box.name = "weapons stay";
		Menu.s_weapons_stay_box.callback = o => { Menu.DMFlagCallback(o); };
		Menu.s_weapons_stay_box.itemnames = Menu.yes_no_names;
		Menu.s_weapons_stay_box.curvalue = (dmflags & Defines.DF_WEAPONS_STAY) != 0 ? 1 : 0;
		Menu.s_instant_powerups_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_instant_powerups_box.x = 0;
		Menu.s_instant_powerups_box.y = y += 10;
		Menu.s_instant_powerups_box.name = "instant powerups";
		Menu.s_instant_powerups_box.callback = o => { Menu.DMFlagCallback(o); };
		Menu.s_instant_powerups_box.itemnames = Menu.yes_no_names;
		Menu.s_instant_powerups_box.curvalue = (dmflags & Defines.DF_INSTANT_ITEMS) != 0 ? 1 : 0;
		Menu.s_powerups_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_powerups_box.x = 0;
		Menu.s_powerups_box.y = y += 10;
		Menu.s_powerups_box.name = "allow powerups";
		Menu.s_powerups_box.callback = o => { Menu.DMFlagCallback(o); };
		Menu.s_powerups_box.itemnames = Menu.yes_no_names;
		Menu.s_powerups_box.curvalue = (dmflags & Defines.DF_NO_ITEMS) == 0 ? 1 : 0;
		Menu.s_health_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_health_box.x = 0;
		Menu.s_health_box.y = y += 10;
		Menu.s_health_box.callback = o => { Menu.DMFlagCallback(o); };
		Menu.s_health_box.name = "allow health";
		Menu.s_health_box.itemnames = Menu.yes_no_names;
		Menu.s_health_box.curvalue = (dmflags & Defines.DF_NO_HEALTH) == 0 ? 1 : 0;
		Menu.s_armor_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_armor_box.x = 0;
		Menu.s_armor_box.y = y += 10;
		Menu.s_armor_box.name = "allow armor";
		Menu.s_armor_box.callback = o => { Menu.DMFlagCallback(o); };
		Menu.s_armor_box.itemnames = Menu.yes_no_names;
		Menu.s_armor_box.curvalue = (dmflags & Defines.DF_NO_ARMOR) == 0 ? 1 : 0;
		Menu.s_spawn_farthest_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_spawn_farthest_box.x = 0;
		Menu.s_spawn_farthest_box.y = y += 10;
		Menu.s_spawn_farthest_box.name = "spawn farthest";
		Menu.s_spawn_farthest_box.callback = o => { Menu.DMFlagCallback(o); };
		Menu.s_spawn_farthest_box.itemnames = Menu.yes_no_names;
		Menu.s_spawn_farthest_box.curvalue = (dmflags & Defines.DF_SPAWN_FARTHEST) != 0 ? 1 : 0;
		Menu.s_samelevel_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_samelevel_box.x = 0;
		Menu.s_samelevel_box.y = y += 10;
		Menu.s_samelevel_box.name = "same map";
		Menu.s_samelevel_box.callback = o => { Menu.DMFlagCallback(o); };
		Menu.s_samelevel_box.itemnames = Menu.yes_no_names;
		Menu.s_samelevel_box.curvalue = (dmflags & Defines.DF_SAME_LEVEL) != 0 ? 1 : 0;
		Menu.s_force_respawn_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_force_respawn_box.x = 0;
		Menu.s_force_respawn_box.y = y += 10;
		Menu.s_force_respawn_box.name = "force respawn";
		Menu.s_force_respawn_box.callback = o => { Menu.DMFlagCallback(o); };
		Menu.s_force_respawn_box.itemnames = Menu.yes_no_names;
		Menu.s_force_respawn_box.curvalue = (dmflags & Defines.DF_FORCE_RESPAWN) != 0 ? 1 : 0;
		Menu.s_teamplay_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_teamplay_box.x = 0;
		Menu.s_teamplay_box.y = y += 10;
		Menu.s_teamplay_box.name = "teamplay";
		Menu.s_teamplay_box.callback = o => { Menu.DMFlagCallback(o); };
		Menu.s_teamplay_box.itemnames = Menu.teamplay_names;
		Menu.s_allow_exit_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_allow_exit_box.x = 0;
		Menu.s_allow_exit_box.y = y += 10;
		Menu.s_allow_exit_box.name = "allow exit";
		Menu.s_allow_exit_box.callback = o => { Menu.DMFlagCallback(o); };
		Menu.s_allow_exit_box.itemnames = Menu.yes_no_names;
		Menu.s_allow_exit_box.curvalue = (dmflags & Defines.DF_ALLOW_EXIT) != 0 ? 1 : 0;
		Menu.s_infinite_ammo_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_infinite_ammo_box.x = 0;
		Menu.s_infinite_ammo_box.y = y += 10;
		Menu.s_infinite_ammo_box.name = "infinite ammo";
		Menu.s_infinite_ammo_box.callback = o => { Menu.DMFlagCallback(o); };
		Menu.s_infinite_ammo_box.itemnames = Menu.yes_no_names;
		Menu.s_infinite_ammo_box.curvalue = (dmflags & Defines.DF_INFINITE_AMMO) != 0 ? 1 : 0;
		Menu.s_fixed_fov_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_fixed_fov_box.x = 0;
		Menu.s_fixed_fov_box.y = y += 10;
		Menu.s_fixed_fov_box.name = "fixed FOV";
		Menu.s_fixed_fov_box.callback = o => { Menu.DMFlagCallback(o); };
		Menu.s_fixed_fov_box.itemnames = Menu.yes_no_names;
		Menu.s_fixed_fov_box.curvalue = (dmflags & Defines.DF_FIXED_FOV) != 0 ? 1 : 0;
		Menu.s_quad_drop_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_quad_drop_box.x = 0;
		Menu.s_quad_drop_box.y = y += 10;
		Menu.s_quad_drop_box.name = "quad drop";
		Menu.s_quad_drop_box.callback = o => { Menu.DMFlagCallback(o); };
		Menu.s_quad_drop_box.itemnames = Menu.yes_no_names;
		Menu.s_quad_drop_box.curvalue = (dmflags & Defines.DF_QUAD_DROP) != 0 ? 1 : 0;
		Menu.s_friendlyfire_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_friendlyfire_box.x = 0;
		Menu.s_friendlyfire_box.y = y += 10;
		Menu.s_friendlyfire_box.name = "friendly fire";
		Menu.s_friendlyfire_box.callback = o => { Menu.DMFlagCallback(o); };
		Menu.s_friendlyfire_box.itemnames = Menu.yes_no_names;
		Menu.s_friendlyfire_box.curvalue = (dmflags & Defines.DF_NO_FRIENDLY_FIRE) == 0 ? 1 : 0;

		//	  ============
		//	  ROGUE
		if (FS.Developer_searchpath(2) == 2)
		{
			Menu.s_no_mines_box.type = Defines.MTYPE_SPINCONTROL;
			Menu.s_no_mines_box.x = 0;
			Menu.s_no_mines_box.y = y += 10;
			Menu.s_no_mines_box.name = "remove mines";
			Menu.s_no_mines_box.callback = o => { Menu.DMFlagCallback(o); };
			Menu.s_no_mines_box.itemnames = Menu.yes_no_names;
			Menu.s_no_mines_box.curvalue = (dmflags & Defines.DF_NO_MINES) != 0 ? 1 : 0;
			Menu.s_no_nukes_box.type = Defines.MTYPE_SPINCONTROL;
			Menu.s_no_nukes_box.x = 0;
			Menu.s_no_nukes_box.y = y += 10;
			Menu.s_no_nukes_box.name = "remove nukes";
			Menu.s_no_nukes_box.callback = o => { Menu.DMFlagCallback(o); };
			Menu.s_no_nukes_box.itemnames = Menu.yes_no_names;
			Menu.s_no_nukes_box.curvalue = (dmflags & Defines.DF_NO_NUKES) != 0 ? 1 : 0;
			Menu.s_stack_double_box.type = Defines.MTYPE_SPINCONTROL;
			Menu.s_stack_double_box.x = 0;
			Menu.s_stack_double_box.y = y += 10;
			Menu.s_stack_double_box.name = "2x/4x stacking off";
			Menu.s_stack_double_box.callback = o => { Menu.DMFlagCallback(o); };
			Menu.s_stack_double_box.itemnames = Menu.yes_no_names;
			Menu.s_stack_double_box.curvalue = dmflags & Defines.DF_NO_STACK_DOUBLE;
			Menu.s_no_spheres_box.type = Defines.MTYPE_SPINCONTROL;
			Menu.s_no_spheres_box.x = 0;
			Menu.s_no_spheres_box.y = y += 10;
			Menu.s_no_spheres_box.name = "remove spheres";
			Menu.s_no_spheres_box.callback = o => { Menu.DMFlagCallback(o); };
			Menu.s_no_spheres_box.itemnames = Menu.yes_no_names;
			Menu.s_no_spheres_box.curvalue = (dmflags & Defines.DF_NO_SPHERES) != 0 ? 1 : 0;
		}

		//	  ROGUE
		//	  ============
		Menu.Menu_AddItem(Menu.s_dmoptions_menu, Menu.s_falls_box);
		Menu.Menu_AddItem(Menu.s_dmoptions_menu, Menu.s_weapons_stay_box);
		Menu.Menu_AddItem(Menu.s_dmoptions_menu, Menu.s_instant_powerups_box);
		Menu.Menu_AddItem(Menu.s_dmoptions_menu, Menu.s_powerups_box);
		Menu.Menu_AddItem(Menu.s_dmoptions_menu, Menu.s_health_box);
		Menu.Menu_AddItem(Menu.s_dmoptions_menu, Menu.s_armor_box);
		Menu.Menu_AddItem(Menu.s_dmoptions_menu, Menu.s_spawn_farthest_box);
		Menu.Menu_AddItem(Menu.s_dmoptions_menu, Menu.s_samelevel_box);
		Menu.Menu_AddItem(Menu.s_dmoptions_menu, Menu.s_force_respawn_box);
		Menu.Menu_AddItem(Menu.s_dmoptions_menu, Menu.s_teamplay_box);
		Menu.Menu_AddItem(Menu.s_dmoptions_menu, Menu.s_allow_exit_box);
		Menu.Menu_AddItem(Menu.s_dmoptions_menu, Menu.s_infinite_ammo_box);
		Menu.Menu_AddItem(Menu.s_dmoptions_menu, Menu.s_fixed_fov_box);
		Menu.Menu_AddItem(Menu.s_dmoptions_menu, Menu.s_quad_drop_box);
		Menu.Menu_AddItem(Menu.s_dmoptions_menu, Menu.s_friendlyfire_box);

		//	  =======
		//	  ROGUE
		if (FS.Developer_searchpath(2) == 2)
		{
			Menu.Menu_AddItem(Menu.s_dmoptions_menu, Menu.s_no_mines_box);
			Menu.Menu_AddItem(Menu.s_dmoptions_menu, Menu.s_no_nukes_box);
			Menu.Menu_AddItem(Menu.s_dmoptions_menu, Menu.s_stack_double_box);
			Menu.Menu_AddItem(Menu.s_dmoptions_menu, Menu.s_no_spheres_box);
		}

		//	  ROGUE
		//	  =======
		Menu.Menu_Center(Menu.s_dmoptions_menu);

		// set the original dmflags statusbar
		Menu.DMFlagCallback(null);
		Menu.Menu_SetStatusBar(Menu.s_dmoptions_menu, Menu.dmoptions_statusbar);
	}

	private static void DMOptions_MenuDraw()
	{
		Menu.Menu_Draw(Menu.s_dmoptions_menu);
	}

	private static string DMOptions_MenuKey(int key)
	{
		return Menu.Default_MenuKey(Menu.s_dmoptions_menu, key);
	}

	private static readonly Action Menu_DMOptions = () => { Menu.Menu_DMOptions_f(); };

	private static void Menu_DMOptions_f()
	{
		Menu.DMOptions_MenuInit();
		Menu.PushMenu(() => { Menu.DMOptions_MenuDraw(); }, key => { return Menu.DMOptions_MenuKey(key); });
	}

	/*
	 * =============================================================================
	 * 
	 * DOWNLOADOPTIONS BOOK MENU
	 * 
	 * =============================================================================
	 */
	private static readonly menuframework_s s_downloadoptions_menu = new();
	private static readonly menuseparator_s s_download_title = new();
	private static readonly menulist_s s_allow_download_box = new();
	private static readonly menulist_s s_allow_download_maps_box = new();
	private static readonly menulist_s s_allow_download_models_box = new();
	private static readonly menulist_s s_allow_download_players_box = new();
	private static readonly menulist_s s_allow_download_sounds_box = new();

	private static void DownloadCallback(object self)
	{
		var f = (menulist_s)self;

		if (f == Menu.s_allow_download_box)
			Cvar.SetValue("allow_download", f.curvalue);
		else if (f == Menu.s_allow_download_maps_box)
			Cvar.SetValue("allow_download_maps", f.curvalue);
		else if (f == Menu.s_allow_download_models_box)
			Cvar.SetValue("allow_download_models", f.curvalue);
		else if (f == Menu.s_allow_download_players_box)
			Cvar.SetValue("allow_download_players", f.curvalue);
		else if (f == Menu.s_allow_download_sounds_box)
			Cvar.SetValue("allow_download_sounds", f.curvalue);
	}

	private static readonly string[] yes_no_names = { "no", "yes" };

	private static void DownloadOptions_MenuInit()
	{
		var y = 0;
		Menu.s_downloadoptions_menu.x = (int)(Globals.viddef.getWidth() * 0.50);
		Menu.s_downloadoptions_menu.nitems = 0;
		Menu.s_download_title.type = Defines.MTYPE_SEPARATOR;
		Menu.s_download_title.name = "Download Options";
		Menu.s_download_title.x = 48;
		Menu.s_download_title.y = y;
		Menu.s_allow_download_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_allow_download_box.x = 0;
		Menu.s_allow_download_box.y = y += 20;
		Menu.s_allow_download_box.name = "allow downloading";
		Menu.s_allow_download_box.callback = o => { Menu.DownloadCallback(o); };
		Menu.s_allow_download_box.itemnames = Menu.yes_no_names;
		Menu.s_allow_download_box.curvalue = Cvar.VariableValue("allow_download") != 0 ? 1 : 0;
		Menu.s_allow_download_maps_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_allow_download_maps_box.x = 0;
		Menu.s_allow_download_maps_box.y = y += 20;
		Menu.s_allow_download_maps_box.name = "maps";
		Menu.s_allow_download_maps_box.callback = o => { Menu.DownloadCallback(o); };
		Menu.s_allow_download_maps_box.itemnames = Menu.yes_no_names;
		Menu.s_allow_download_maps_box.curvalue = Cvar.VariableValue("allow_download_maps") != 0 ? 1 : 0;
		Menu.s_allow_download_players_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_allow_download_players_box.x = 0;
		Menu.s_allow_download_players_box.y = y += 10;
		Menu.s_allow_download_players_box.name = "player models/skins";
		Menu.s_allow_download_players_box.callback = o => { Menu.DownloadCallback(o); };
		Menu.s_allow_download_players_box.itemnames = Menu.yes_no_names;
		Menu.s_allow_download_players_box.curvalue = Cvar.VariableValue("allow_download_players") != 0 ? 1 : 0;
		Menu.s_allow_download_models_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_allow_download_models_box.x = 0;
		Menu.s_allow_download_models_box.y = y += 10;
		Menu.s_allow_download_models_box.name = "models";
		Menu.s_allow_download_models_box.callback = o => { Menu.DownloadCallback(o); };
		Menu.s_allow_download_models_box.itemnames = Menu.yes_no_names;
		Menu.s_allow_download_models_box.curvalue = Cvar.VariableValue("allow_download_models") != 0 ? 1 : 0;
		Menu.s_allow_download_sounds_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_allow_download_sounds_box.x = 0;
		Menu.s_allow_download_sounds_box.y = y += 10;
		Menu.s_allow_download_sounds_box.name = "sounds";
		Menu.s_allow_download_sounds_box.callback = o => { Menu.DownloadCallback(o); };
		Menu.s_allow_download_sounds_box.itemnames = Menu.yes_no_names;
		Menu.s_allow_download_sounds_box.curvalue = Cvar.VariableValue("allow_download_sounds") != 0 ? 1 : 0;
		Menu.Menu_AddItem(Menu.s_downloadoptions_menu, Menu.s_download_title);
		Menu.Menu_AddItem(Menu.s_downloadoptions_menu, Menu.s_allow_download_box);
		Menu.Menu_AddItem(Menu.s_downloadoptions_menu, Menu.s_allow_download_maps_box);
		Menu.Menu_AddItem(Menu.s_downloadoptions_menu, Menu.s_allow_download_players_box);
		Menu.Menu_AddItem(Menu.s_downloadoptions_menu, Menu.s_allow_download_models_box);
		Menu.Menu_AddItem(Menu.s_downloadoptions_menu, Menu.s_allow_download_sounds_box);
		Menu.Menu_Center(Menu.s_downloadoptions_menu);

		// skip over title
		if (Menu.s_downloadoptions_menu.cursor == 0)
			Menu.s_downloadoptions_menu.cursor = 1;
	}

	private static void DownloadOptions_MenuDraw()
	{
		Menu.Menu_Draw(Menu.s_downloadoptions_menu);
	}

	private static string DownloadOptions_MenuKey(int key)
	{
		return Menu.Default_MenuKey(Menu.s_downloadoptions_menu, key);
	}

	private static readonly Action Menu_DownloadOptions = () => { Menu.Menu_DownloadOptions_f(); };

	private static void Menu_DownloadOptions_f()
	{
		Menu.DownloadOptions_MenuInit();
		Menu.PushMenu(() => { Menu.DownloadOptions_MenuDraw(); }, key => { return Menu.DownloadOptions_MenuKey(key); });
	}

	/*
	 * =============================================================================
	 * 
	 * ADDRESS BOOK MENU
	 * 
	 * =============================================================================
	 */
	private static readonly menuframework_s s_addressbook_menu = new();
	private static readonly menufield_s[] s_addressbook_fields = new menufield_s[Defines.NUM_ADDRESSBOOK_ENTRIES];

	private static void Menu5()
	{
		for (var n = 0; n < Defines.NUM_ADDRESSBOOK_ENTRIES; n++)
			Menu.s_addressbook_fields[n] = new();
	}

	private static void AddressBook_MenuInit()
	{
		Menu.s_addressbook_menu.x = Globals.viddef.getWidth() / 2 - 142;
		Menu.s_addressbook_menu.y = Globals.viddef.getHeight() / 2 - 58;
		Menu.s_addressbook_menu.nitems = 0;

		for (var i = 0; i < Defines.NUM_ADDRESSBOOK_ENTRIES; i++)
		{
			var adr = Cvar.Get("adr" + i, "", Defines.CVAR_ARCHIVE);
			Menu.s_addressbook_fields[i].type = Defines.MTYPE_FIELD;
			Menu.s_addressbook_fields[i].name = null;
			Menu.s_addressbook_fields[i].callback = null;
			Menu.s_addressbook_fields[i].x = 0;
			Menu.s_addressbook_fields[i].y = i * 18 + 0;
			Menu.s_addressbook_fields[i].localdata[0] = i;

			// put the cursor to the end of text for editing
			Menu.s_addressbook_fields[i].cursor = adr.@string.Length;
			Menu.s_addressbook_fields[i].length = 60;
			Menu.s_addressbook_fields[i].visible_length = 30;
			Menu.s_addressbook_fields[i].buffer = new(adr.@string);
			Menu.Menu_AddItem(Menu.s_addressbook_menu, Menu.s_addressbook_fields[i]);
		}
	}

	private static Func<int, string> AddressBook_MenuKey = key => { return Menu.AddressBook_MenuKey_f(key); };

	private static string AddressBook_MenuKey_f(int key)
	{
		if (key == Key.K_ESCAPE)
		{
			for (var index = 0; index < Defines.NUM_ADDRESSBOOK_ENTRIES; index++)
				Cvar.Set("adr" + index, Menu.s_addressbook_fields[index].buffer.ToString());
		}

		return Menu.Default_MenuKey(Menu.s_addressbook_menu, key);
	}

	private static Action AddressBook_MenuDraw = () => { Menu.AddressBook_MenuDraw_f(); };

	private static void AddressBook_MenuDraw_f()
	{
		Menu.Banner("m_banner_addressbook");
		Menu.Menu_Draw(Menu.s_addressbook_menu);
	}

	private static readonly Action Menu_AddressBook = () => { Menu.Menu_AddressBook_f(); };

	private static void Menu_AddressBook_f()
	{
		Menu.AddressBook_MenuInit();
		Menu.PushMenu(() => { Menu.AddressBook_MenuDraw_f(); }, key => { return Menu.AddressBook_MenuKey_f(key); });
	}

	/*
	 * =============================================================================
	 * 
	 * PLAYER CONFIG MENU
	 * 
	 * =============================================================================
	 */
	private static readonly menuframework_s s_player_config_menu = new();
	private static readonly menufield_s s_player_name_field = new();
	private static readonly menulist_s s_player_model_box = new();
	private static readonly menulist_s s_player_skin_box = new();
	private static readonly menulist_s s_player_handedness_box = new();
	private static readonly menulist_s s_player_rate_box = new();
	private static readonly menuseparator_s s_player_skin_title = new();
	private static readonly menuseparator_s s_player_model_title = new();
	private static readonly menuseparator_s s_player_hand_title = new();
	private static readonly menuseparator_s s_player_rate_title = new();
	private static readonly menuaction_s s_player_download_action = new();
	private static readonly playermodelinfo_t[] s_pmi = new playermodelinfo_t[Defines.MAX_PLAYERMODELS];
	private static string[] s_pmnames = new string[Defines.MAX_PLAYERMODELS];
	private static int s_numplayermodels;
	private static readonly int[] rate_tbl = { 2500, 3200, 5000, 10000, 25000, 0 };
	private static readonly string[] rate_names = { "28.8 Modem", "33.6 Modem", "Single ISDN", "Dual ISDN/Cable", "T1/LAN", "User defined" };

	private static void DownloadOptionsFunc(object self)
	{
		Menu.Menu_DownloadOptions_f();
	}

	private static void HandednessCallback(object unused)
	{
		Cvar.SetValue("hand", Menu.s_player_handedness_box.curvalue);
	}

	private static void RateCallback(object unused)
	{
		if (Menu.s_player_rate_box.curvalue != Menu.rate_tbl.Length - 1) //sizeof(rate_tbl)
			// / sizeof(*
			// rate_tbl) - 1)
			Cvar.SetValue("rate", Menu.rate_tbl[Menu.s_player_rate_box.curvalue]);
	}

	private static void ModelCallback(object unused)
	{
		Menu.s_player_skin_box.itemnames = Menu.s_pmi[Menu.s_player_model_box.curvalue].skindisplaynames;
		Menu.s_player_skin_box.curvalue = 0;
	}

	private static bool IconOfSkinExists(string skin, string[] pcxfiles, int npcxfiles)
	{
		string scratch;

		//strcpy(scratch, skin);
		scratch = skin;
		var pos = scratch.LastIndexOf('.');

		if (pos != -1)
			scratch = scratch[..pos] + "_i.pcx";
		else
			scratch += "_i.pcx";

		for (var i = 0; i < npcxfiles; i++)
		{
			if (pcxfiles[i].Equals(scratch))
				return true;
		}

		return false;
	}

	private static bool PlayerConfig_ScanDirectories()
	{
		//char findname[1024];
		string findname;

		//char scratch[1024];
		string scratch;
		int ndirs = 0, npms = 0;
		int a, b, c;
		string[] dirnames;
		string path = null;
		int i;

		//extern String * FS_ListFiles(String , int *, unsigned, unsigned);
		Menu.s_numplayermodels = 0;

		/*
		 * * get a list of directories
		 */
		do
		{
			path = FS.NextPath(path);
			findname = path + "/players/*.*";

			if ((dirnames = FS.ListFiles(findname, 0, Defines.SFF_SUBDIR)) != null)
			{
				ndirs = dirnames.Length;

				break;
			}
		}
		while (path != null);

		if (dirnames == null)
			return false;

		/*
		 * * go through the subdirectories
		 */
		npms = ndirs;

		if (npms > Defines.MAX_PLAYERMODELS)
			npms = Defines.MAX_PLAYERMODELS;

		for (i = 0; i < npms; i++)
		{
			int k, s;

			//String a, b, c;
			string[] pcxnames;
			string[] skinnames;
			int npcxfiles;
			var nskins = 0;

			if (dirnames[i] == null)
				continue;

			// verify the existence of tris.md2
			scratch = dirnames[i];
			scratch += "/tris.md2";

			if (Sys.FindFirst(scratch, 0, Defines.SFF_SUBDIR | Defines.SFF_HIDDEN | Defines.SFF_SYSTEM) == null)
			{
				//free(dirnames[i]);
				dirnames[i] = null;
				Sys.FindClose();

				continue;
			}

			Sys.FindClose();

			// verify the existence of at least one pcx skin
			scratch = dirnames[i] + "/*.pcx";
			pcxnames = FS.ListFiles(scratch, 0, 0);
			npcxfiles = pcxnames.Length;

			if (pcxnames == null)
			{
				dirnames[i] = null;

				continue;
			}

			// count valid skins, which consist of a skin with a matching "_i"
			// icon
			for (k = 0; k < npcxfiles - 1; k++)
			{
				if (!pcxnames[k].EndsWith("_i.pcx"))
				{
					//if (!strstr(pcxnames[k], "_i.pcx")) {
					if (Menu.IconOfSkinExists(pcxnames[k], pcxnames, npcxfiles - 1))
						nskins++;
				}
			}

			if (nskins == 0)
				continue;

			skinnames = new string[nskins + 1]; //malloc(sizeof(String) *

			// (nskins + 1));
			//memset(skinnames, 0, sizeof(String) * (nskins + 1));

			// copy the valid skins
			for (s = 0, k = 0; k < npcxfiles - 1; k++)
			{
				if (pcxnames[k].IndexOf("_i.pcx") < 0)
				{
					if (Menu.IconOfSkinExists(pcxnames[k], pcxnames, npcxfiles - 1))
					{
						a = pcxnames[k].LastIndexOf('/');
						b = pcxnames[k].LastIndexOf('\\');

						if (a > b)
							c = a;
						else
							c = b;

						scratch = pcxnames[k][(c + 1)..];
						var pos = scratch.LastIndexOf('.');

						if (pos != -1)
							scratch = scratch[..pos];

						skinnames[s] = scratch;
						s++;
					}
				}
			}

			// at this point we have a valid player model
			if (Menu.s_pmi[Menu.s_numplayermodels] == null)
				Menu.s_pmi[Menu.s_numplayermodels] = new();

			Menu.s_pmi[Menu.s_numplayermodels].nskins = nskins;
			Menu.s_pmi[Menu.s_numplayermodels].skindisplaynames = skinnames;

			// make short name for the model
			a = dirnames[i].LastIndexOf('/');
			b = dirnames[i].LastIndexOf('\\');

			if (a > b)
				c = a;
			else
				c = b;

			Menu.s_pmi[Menu.s_numplayermodels].displayname = dirnames[i][(c + 1)..];
			Menu.s_pmi[Menu.s_numplayermodels].directory = dirnames[i][(c + 1)..];
			Menu.s_numplayermodels++;
		}

		return true;
	}

	private static int pmicmpfnc(object _a, object _b)
	{
		var a = (playermodelinfo_t)_a;
		var b = (playermodelinfo_t)_b;

		/*
		 * * sort by male, female, then alphabetical
		 */
		if (a.directory.Equals("male"))
			return -1;

		if (b.directory.Equals("male"))
			return 1;

		if (a.directory.Equals("female"))
			return -1;

		if (b.directory.Equals("female"))
			return 1;

		return string.Compare(a.directory, b.directory);
	}

	private static readonly string[] handedness = { "right", "left", "center" };

	private static bool PlayerConfig_MenuInit()
	{
		/*
		 * extern cvar_t * name; extern cvar_t * team; extern cvar_t * skin;
		 */
		//har currentdirectory[1024];
		string currentdirectory;

		//char currentskin[1024];
		string currentskin;
		var i = 0;
		var currentdirectoryindex = 0;
		var currentskinindex = 0;
		var hand = Cvar.Get("hand", "0", Defines.CVAR_USERINFO | Defines.CVAR_ARCHIVE);
		Menu.PlayerConfig_ScanDirectories();

		if (Menu.s_numplayermodels == 0)
			return false;

		if (hand.value < 0 || hand.value > 2)
			Cvar.SetValue("hand", 0);

		currentdirectory = Globals.skin.@string;

		if (currentdirectory.LastIndexOf('/') != -1)
		{
			currentskin = Lib.rightFrom(currentdirectory, '/');
			currentdirectory = Lib.leftFrom(currentdirectory, '/');
		}
		else if (currentdirectory.LastIndexOf('\\') != -1)
		{
			currentskin = Lib.rightFrom(currentdirectory, '\\');
			currentdirectory = Lib.leftFrom(currentdirectory, '\\');
		}
		else
		{
			currentdirectory = "male";
			currentskin = "grunt";
		}

		//qsort(s_pmi, s_numplayermodels, sizeof(s_pmi[0]), pmicmpfnc);
		Array.Sort(Menu.s_pmi, 0, Menu.s_numplayermodels, Comparer<object>.Create((o1, o2) => { return Menu.pmicmpfnc(o1, o2); }));

		//memset(s_pmnames, 0, sizeof(s_pmnames));
		Menu.s_pmnames = new string[Defines.MAX_PLAYERMODELS];

		for (i = 0; i < Menu.s_numplayermodels; i++)
		{
			Menu.s_pmnames[i] = Menu.s_pmi[i].displayname;

			if (Lib.Q_stricmp(Menu.s_pmi[i].directory, currentdirectory) == 0)
			{
				int j;
				currentdirectoryindex = i;

				for (j = 0; j < Menu.s_pmi[i].nskins; j++)
				{
					if (Lib.Q_stricmp(Menu.s_pmi[i].skindisplaynames[j], currentskin) == 0)
					{
						currentskinindex = j;

						break;
					}
				}
			}
		}

		Menu.s_player_config_menu.x = Globals.viddef.getWidth() / 2 - 95;
		Menu.s_player_config_menu.y = Globals.viddef.getHeight() / 2 - 97;
		Menu.s_player_config_menu.nitems = 0;
		Menu.s_player_name_field.type = Defines.MTYPE_FIELD;
		Menu.s_player_name_field.name = "name";
		Menu.s_player_name_field.callback = null;
		Menu.s_player_name_field.x = 0;
		Menu.s_player_name_field.y = 0;
		Menu.s_player_name_field.length = 20;
		Menu.s_player_name_field.visible_length = 20;
		Menu.s_player_name_field.buffer = new(Globals.name.@string);
		Menu.s_player_name_field.cursor = Globals.name.@string.Length;
		Menu.s_player_model_title.type = Defines.MTYPE_SEPARATOR;
		Menu.s_player_model_title.name = "model";
		Menu.s_player_model_title.x = -8;
		Menu.s_player_model_title.y = 60;
		Menu.s_player_model_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_player_model_box.x = -56;
		Menu.s_player_model_box.y = 70;
		Menu.s_player_model_box.callback = o => { Menu.ModelCallback(o); };
		Menu.s_player_model_box.cursor_offset = -48;
		Menu.s_player_model_box.curvalue = currentdirectoryindex;
		Menu.s_player_model_box.itemnames = Menu.s_pmnames;
		Menu.s_player_skin_title.type = Defines.MTYPE_SEPARATOR;
		Menu.s_player_skin_title.name = "skin";
		Menu.s_player_skin_title.x = -16;
		Menu.s_player_skin_title.y = 84;
		Menu.s_player_skin_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_player_skin_box.x = -56;
		Menu.s_player_skin_box.y = 94;
		Menu.s_player_skin_box.name = null;
		Menu.s_player_skin_box.callback = null;
		Menu.s_player_skin_box.cursor_offset = -48;
		Menu.s_player_skin_box.curvalue = currentskinindex;
		Menu.s_player_skin_box.itemnames = Menu.s_pmi[currentdirectoryindex].skindisplaynames;
		Menu.s_player_hand_title.type = Defines.MTYPE_SEPARATOR;
		Menu.s_player_hand_title.name = "handedness";
		Menu.s_player_hand_title.x = 32;
		Menu.s_player_hand_title.y = 108;
		Menu.s_player_handedness_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_player_handedness_box.x = -56;
		Menu.s_player_handedness_box.y = 118;
		Menu.s_player_handedness_box.name = null;
		Menu.s_player_handedness_box.cursor_offset = -48;
		Menu.s_player_handedness_box.callback = o => { Menu.HandednessCallback(o); };
		Menu.s_player_handedness_box.curvalue = (int)Cvar.VariableValue("hand");
		Menu.s_player_handedness_box.itemnames = Menu.handedness;

		for (i = 0; i < Menu.rate_tbl.Length - 1; i++)
		{
			if (Cvar.VariableValue("rate") == Menu.rate_tbl[i])
				break;
		}

		Menu.s_player_rate_title.type = Defines.MTYPE_SEPARATOR;
		Menu.s_player_rate_title.name = "connect speed";
		Menu.s_player_rate_title.x = 56;
		Menu.s_player_rate_title.y = 156;
		Menu.s_player_rate_box.type = Defines.MTYPE_SPINCONTROL;
		Menu.s_player_rate_box.x = -56;
		Menu.s_player_rate_box.y = 166;
		Menu.s_player_rate_box.name = null;
		Menu.s_player_rate_box.cursor_offset = -48;
		Menu.s_player_rate_box.callback = o => { Menu.RateCallback(o); };
		Menu.s_player_rate_box.curvalue = i;
		Menu.s_player_rate_box.itemnames = Menu.rate_names;
		Menu.s_player_download_action.type = Defines.MTYPE_ACTION;
		Menu.s_player_download_action.name = "download options";
		Menu.s_player_download_action.flags = Defines.QMF_LEFT_JUSTIFY;
		Menu.s_player_download_action.x = -24;
		Menu.s_player_download_action.y = 186;
		Menu.s_player_download_action.statusbar = null;
		Menu.s_player_download_action.callback = o => { Menu.DownloadOptionsFunc(o); };
		Menu.Menu_AddItem(Menu.s_player_config_menu, Menu.s_player_name_field);
		Menu.Menu_AddItem(Menu.s_player_config_menu, Menu.s_player_model_title);
		Menu.Menu_AddItem(Menu.s_player_config_menu, Menu.s_player_model_box);

		if (Menu.s_player_skin_box.itemnames != null)
		{
			Menu.Menu_AddItem(Menu.s_player_config_menu, Menu.s_player_skin_title);
			Menu.Menu_AddItem(Menu.s_player_config_menu, Menu.s_player_skin_box);
		}

		Menu.Menu_AddItem(Menu.s_player_config_menu, Menu.s_player_hand_title);
		Menu.Menu_AddItem(Menu.s_player_config_menu, Menu.s_player_handedness_box);
		Menu.Menu_AddItem(Menu.s_player_config_menu, Menu.s_player_rate_title);
		Menu.Menu_AddItem(Menu.s_player_config_menu, Menu.s_player_rate_box);
		Menu.Menu_AddItem(Menu.s_player_config_menu, Menu.s_player_download_action);

		return true;
	}

	private static int yaw;
	private static readonly entity_t entity = new();

	private static void PlayerConfig_MenuDraw()
	{
		refdef_t refdef = new();

		//char scratch[MAX_QPATH];
		string scratch;

		//memset(refdef, 0, sizeof(refdef));
		refdef.x = Globals.viddef.getWidth() / 2;
		refdef.y = Globals.viddef.getHeight() / 2 - 72;
		refdef.width = 144;
		refdef.height = 168;
		refdef.fov_x = 40;
		refdef.fov_y = Math3D.CalcFov(refdef.fov_x, refdef.width, refdef.height);
		refdef.time = Globals.cls.realtime * 0.001f;

		if (Menu.s_pmi[Menu.s_player_model_box.curvalue].skindisplaynames != null)
		{
			Menu.entity.clear();
			scratch = "players/" + Menu.s_pmi[Menu.s_player_model_box.curvalue].directory + "/tris.md2";
			Menu.entity.model = Globals.re.RegisterModel(scratch);

			scratch = "players/"
				+ Menu.s_pmi[Menu.s_player_model_box.curvalue].directory
				+ "/"
				+ Menu.s_pmi[Menu.s_player_model_box.curvalue].skindisplaynames[Menu.s_player_skin_box.curvalue]
				+ ".pcx";

			Menu.entity.skin = Globals.re.RegisterSkin(scratch);
			Menu.entity.flags = Defines.RF_FULLBRIGHT;
			Menu.entity.origin[0] = 80;
			Menu.entity.origin[1] = 0;
			Menu.entity.origin[2] = 0;
			Math3D.VectorCopy(Menu.entity.origin, Menu.entity.oldorigin);
			Menu.entity.frame = 0;
			Menu.entity.oldframe = 0;
			Menu.entity.backlerp = 0.0f;
			Menu.entity.angles[1] = Menu.yaw++;

			if (++Menu.yaw > 360)
				Menu.yaw -= 360;

			refdef.areabits = null;
			refdef.num_entities = 1;
			refdef.entities = new entity_t[] { Menu.entity };
			refdef.lightstyles = null;
			refdef.rdflags = Defines.RDF_NOWORLDMODEL;
			Menu.Menu_Draw(Menu.s_player_config_menu);

			Menu.DrawTextBox(
				(int)(refdef.x * (320.0F / Globals.viddef.getWidth()) - 8),
				(int)(Globals.viddef.getHeight() / 2 * (240.0F / Globals.viddef.getHeight()) - 77),
				refdef.width / 8,
				refdef.height / 8
			);

			refdef.height += 4;
			Globals.re.RenderFrame(refdef);

			scratch = "/players/"
				+ Menu.s_pmi[Menu.s_player_model_box.curvalue].directory
				+ "/"
				+ Menu.s_pmi[Menu.s_player_model_box.curvalue].skindisplaynames[Menu.s_player_skin_box.curvalue]
				+ "_i.pcx";

			Globals.re.DrawPic(Menu.s_player_config_menu.x - 40, refdef.y, scratch);
		}
	}

	private static string PlayerConfig_MenuKey(int key)
	{
		int i;

		if (key == Key.K_ESCAPE)
		{
			string scratch;
			Cvar.Set("name", Menu.s_player_name_field.buffer.ToString());

			scratch = Menu.s_pmi[Menu.s_player_model_box.curvalue].directory
				+ "/"
				+ Menu.s_pmi[Menu.s_player_model_box.curvalue].skindisplaynames[Menu.s_player_skin_box.curvalue];

			Cvar.Set("skin", scratch);

			for (i = 0; i < Menu.s_numplayermodels; i++)
			{
				int j;

				for (j = 0; j < Menu.s_pmi[i].nskins; j++)
				{
					if (Menu.s_pmi[i].skindisplaynames[j] != null)
						Menu.s_pmi[i].skindisplaynames[j] = null;
				}

				Menu.s_pmi[i].skindisplaynames = null;
				Menu.s_pmi[i].nskins = 0;
			}
		}

		return Menu.Default_MenuKey(Menu.s_player_config_menu, key);
	}

	private static readonly Action Menu_PlayerConfig = () => { Menu.Menu_PlayerConfig_f(); };

	private static void Menu_PlayerConfig_f()
	{
		if (!Menu.PlayerConfig_MenuInit())
		{
			Menu.Menu_SetStatusBar(Menu.s_multiplayer_menu, "No valid player models found");

			return;
		}

		Menu.Menu_SetStatusBar(Menu.s_multiplayer_menu, null);
		Menu.PushMenu(() => { Menu.PlayerConfig_MenuDraw(); }, key => { return Menu.PlayerConfig_MenuKey(key); });
	}

	/*
	 * =======================================================================
	 * 
	 * QUIT MENU
	 * 
	 * =======================================================================
	 */

	private static string Quit_Key(int key)
	{
		switch (key)
		{
			case Key.K_ESCAPE:
			case 'n':
			case 'N':
				Menu.PopMenu();

				break;

			case 'Y':
			case 'y':
				Globals.cls.key_dest = Defines.key_console;
				Cl.QuitF();

				break;

			default:
				break;
		}

		return null;
	}

	private static void Quit_Draw()
	{
		int w, h;
		var d = new Size();
		Globals.re.DrawGetPicSize(ref d, "quit");
		w = d.Width;
		h = d.Height;
		Globals.re.DrawPic((Globals.viddef.getWidth() - w) / 2, (Globals.viddef.getHeight() - h) / 2, "quit");
	}

	private static readonly Action Menu_Quit = () => { Menu.Menu_Quit_f(); };

	private static void Menu_Quit_f()
	{
		Menu.PushMenu(() => { Menu.Quit_Draw(); }, key => { return Menu.Quit_Key(key); });
	}

	//	  =============================================================================
	/* Menu Subsystem */

	/**
     * Init
     */
	public static void Init()
	{
		Cmd.AddCommand("menu_main", Menu.Menu_Main);
		Cmd.AddCommand("menu_game", Menu.Menu_Game);
		Cmd.AddCommand("menu_loadgame", Menu.Menu_LoadGame);
		Cmd.AddCommand("menu_savegame", Menu.Menu_SaveGame);
		Cmd.AddCommand("menu_joinserver", Menu.Menu_JoinServer);
		Cmd.AddCommand("menu_addressbook", Menu.Menu_AddressBook);
		Cmd.AddCommand("menu_startserver", Menu.Menu_StartServer);
		Cmd.AddCommand("menu_dmoptions", Menu.Menu_DMOptions);
		Cmd.AddCommand("menu_playerconfig", Menu.Menu_PlayerConfig);
		Cmd.AddCommand("menu_downloadoptions", Menu.Menu_DownloadOptions);
		Cmd.AddCommand("menu_credits", Menu.Menu_Credits);
		Cmd.AddCommand("menu_multiplayer", Menu.Menu_Multiplayer);
		Cmd.AddCommand("menu_video", Menu.Menu_Video);
		Cmd.AddCommand("menu_options", Menu.Menu_Options);
		Cmd.AddCommand("menu_keys", Menu.Menu_Keys);
		Cmd.AddCommand("menu_quit", Menu.Menu_Quit);

		for (var i = 0; i < Menu.m_layers.Length; i++)
			Menu.m_layers[i] = new();
	}

	/*
	 * ================= Draw =================
	 */
	public static void Draw()
	{
		if (Globals.cls.key_dest != Defines.key_menu)
			return;

		// repaint everything next frame
		SCR.DirtyScreen();

		// dim everything behind it down
		if (Globals.cl.cinematictime > 0)
			Globals.re.DrawFill(0, 0, Globals.viddef.getWidth(), Globals.viddef.getHeight(), 0);
		else
			Globals.re.DrawFadeScreen();

		Menu.m_drawfunc();

		// delay playing the enter sound until after the
		// menu has been drawn, to avoid delay while
		// caching images
		if (Menu.m_entersound)
		{
			S.StartLocalSound(Menu.menu_in_sound);
			Menu.m_entersound = false;
		}
	}

	/*
	 * ================= Keydown =================
	 */
	public static void Keydown(int key)
	{
		string s;

		if (Menu.m_keyfunc != null)
		{
			if ((s = Menu.m_keyfunc(key)) != null)
				S.StartLocalSound(s);
		}
	}

	public static void Action_DoEnter(menuaction_s a)
	{
		if (a.callback != null)
			a.callback(a);
	}

	public static void Action_Draw(menuaction_s a)
	{
		if ((a.flags & Defines.QMF_LEFT_JUSTIFY) != 0)
		{
			if ((a.flags & Defines.QMF_GRAYED) != 0)
				Menu.Menu_DrawStringDark(a.x + a.parent.x + Defines.LCOLUMN_OFFSET, a.y + a.parent.y, a.name);
			else
				Menu.Menu_DrawString(a.x + a.parent.x + Defines.LCOLUMN_OFFSET, a.y + a.parent.y, a.name);
		}
		else
		{
			if ((a.flags & Defines.QMF_GRAYED) != 0)
				Menu.Menu_DrawStringR2LDark(a.x + a.parent.x + Defines.LCOLUMN_OFFSET, a.y + a.parent.y, a.name);
			else
				Menu.Menu_DrawStringR2L(a.x + a.parent.x + Defines.LCOLUMN_OFFSET, a.y + a.parent.y, a.name);
		}

		if (a.ownerdraw != null)
			a.ownerdraw(a);
	}

	public static bool Field_DoEnter(menufield_s f)
	{
		if (f.callback != null)
		{
			f.callback(f);

			return true;
		}

		return false;
	}

	public static void Field_Draw(menufield_s f)
	{
		int i;
		string tempbuffer;

		//[128] = "";

		if (f.name != null)
			Menu.Menu_DrawStringR2LDark(f.x + f.parent.x + Defines.LCOLUMN_OFFSET, f.y + f.parent.y, f.name);

		//strncpy(tempbuffer, f.buffer + f.visible_offset, f.visible_length);
		var s = f.buffer.ToString();
		tempbuffer = s[f.visible_offset..];
		Globals.re.DrawChar(f.x + f.parent.x + 16, f.y + f.parent.y - 4, 18);
		Globals.re.DrawChar(f.x + f.parent.x + 16, f.y + f.parent.y + 4, 24);
		Globals.re.DrawChar(f.x + f.parent.x + 24 + f.visible_length * 8, f.y + f.parent.y - 4, 20);
		Globals.re.DrawChar(f.x + f.parent.x + 24 + f.visible_length * 8, f.y + f.parent.y + 4, 26);

		for (i = 0; i < f.visible_length; i++)
		{
			Globals.re.DrawChar(f.x + f.parent.x + 24 + i * 8, f.y + f.parent.y - 4, 19);
			Globals.re.DrawChar(f.x + f.parent.x + 24 + i * 8, f.y + f.parent.y + 4, 25);
		}

		Menu.Menu_DrawString(f.x + f.parent.x + 24, f.y + f.parent.y, tempbuffer);

		if (Menu.Menu_ItemAtCursor(f.parent) == f)
		{
			int offset;

			if (f.visible_offset != 0)
				offset = f.visible_length;
			else
				offset = f.cursor;

			if (((int)(Timer.Milliseconds() / 250) & 1) != 0)
				Globals.re.DrawChar(f.x + f.parent.x + (offset + 2) * 8 + 8, f.y + f.parent.y, 11);
			else
				Globals.re.DrawChar(f.x + f.parent.x + (offset + 2) * 8 + 8, f.y + f.parent.y, ' ');
		}
	}

	public static bool Field_Key(menufield_s f, int k)
	{
		var key = (char)k;

		switch (k)
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

		if (key > 127)
		{
			switch (k)
			{
				case Key.K_DEL:
				default:
					return false;
			}
		}

		/*
		 * * support pasting from the clipboard
		 */
		if ((char.ToUpper(key) == 'V' && Globals.keydown[Key.K_CTRL]) || ((key == Key.K_INS || key == Key.K_KP_INS) && Globals.keydown[Key.K_SHIFT]))
		{
			string cbd;

			if ((cbd = Sys.GetClipboardData()) != null)
			{
				//strtok(cbd, "\n\r\b");
				var lines = cbd.Split("\r\n");

				if (lines.Length > 0 && lines[0].Length != 0)
				{
					//strncpy(f.buffer, cbd, f.length - 1);
					f.buffer = new(lines[0]);
					f.cursor = f.buffer.Length;
					f.visible_offset = f.cursor - f.visible_length;

					if (f.visible_offset < 0)
						f.visible_offset = 0;
				}
			}

			return true;
		}

		switch (k)
		{
			case Key.K_KP_LEFTARROW:
			case Key.K_LEFTARROW:
			case Key.K_BACKSPACE:
				if (f.cursor > 0)
				{
					f.buffer.Remove(f.cursor - 1, 1);

					//memmove(f.buffer[f.cursor - 1], f.buffer[f.cursor], strlen(&
					// f.buffer[f.cursor]) + 1);
					f.cursor--;

					if (f.visible_offset != 0)
						f.visible_offset--;
				}

				break;

			case Key.K_KP_DEL:
			case Key.K_DEL:
				//memmove(& f.buffer[f.cursor], & f.buffer[f.cursor + 1], strlen(&
				// f.buffer[f.cursor + 1]) + 1);
				f.buffer.Remove(f.cursor, 1);

				break;

			case Key.K_KP_ENTER:
			case Key.K_ENTER:
			case Key.K_ESCAPE:
			case Key.K_TAB:
				return false;

			case Key.K_SPACE:
			default:
				if (!char.IsDigit(key) && (f.flags & Defines.QMF_NUMBERSONLY) != 0)
					return false;

				if (f.cursor < f.length)
				{
					f.buffer.Append(key);
					f.cursor++;

					if (f.cursor > f.visible_length)
						f.visible_offset++;
				}

				break;
		}

		return true;
	}

	public static void Menu_AddItem(menuframework_s menu, menucommon_s item)
	{
		if (menu.nitems == 0)
			menu.nslots = 0;

		if (menu.nitems < Defines.MAXMENUITEMS)
		{
			menu.items[menu.nitems] = item;
			((menucommon_s)menu.items[menu.nitems]).parent = menu;
			menu.nitems++;
		}

		menu.nslots = Menu.Menu_TallySlots(menu);
	}

	/*
	 * * Menu_AdjustCursor * * This function takes the given menu, the
	 * direction, and attempts * to adjust the menu's cursor so that it's at the
	 * next available * slot.
	 */
	public static void Menu_AdjustCursor(menuframework_s m, int dir)
	{
		menucommon_s citem;

		/*
		 * * see if it's in a valid spot
		 */
		if (m.cursor >= 0 && m.cursor < m.nitems)
		{
			if ((citem = Menu.Menu_ItemAtCursor(m)) != null)
			{
				if (citem.type != Defines.MTYPE_SEPARATOR)
					return;
			}
		}

		/*
		 * * it's not in a valid spot, so crawl in the direction indicated until
		 * we * find a valid spot
		 */
		if (dir == 1)
		{
			while (true)
			{
				citem = Menu.Menu_ItemAtCursor(m);

				if (citem != null)
				{
					if (citem.type != Defines.MTYPE_SEPARATOR)
						break;
				}

				m.cursor += dir;

				if (m.cursor >= m.nitems)
					m.cursor = 0;
			}
		}
		else
		{
			while (true)
			{
				citem = Menu.Menu_ItemAtCursor(m);

				if (citem != null)
				{
					if (citem.type != Defines.MTYPE_SEPARATOR)
						break;
				}

				m.cursor += dir;

				if (m.cursor < 0)
					m.cursor = m.nitems - 1;
			}
		}
	}

	public static void Menu_Center(menuframework_s menu)
	{
		int height;
		height = ((menucommon_s)menu.items[menu.nitems - 1]).y;
		height += 10;
		menu.y = (Globals.viddef.getHeight() - height) / 2;
	}

	public static void Menu_Draw(menuframework_s menu)
	{
		int i;
		menucommon_s item;

		/*
		 * * draw contents
		 */
		for (i = 0; i < menu.nitems; i++)
		{
			switch (((menucommon_s)menu.items[i]).type)
			{
				case Defines.MTYPE_FIELD:
					Menu.Field_Draw((menufield_s)menu.items[i]);

					break;

				case Defines.MTYPE_SLIDER:
					Menu.Slider_Draw((menuslider_s)menu.items[i]);

					break;

				case Defines.MTYPE_LIST:
					Menu.MenuList_Draw((menulist_s)menu.items[i]);

					break;

				case Defines.MTYPE_SPINCONTROL:
					Menu.SpinControl_Draw((menulist_s)menu.items[i]);

					break;

				case Defines.MTYPE_ACTION:
					Menu.Action_Draw((menuaction_s)menu.items[i]);

					break;

				case Defines.MTYPE_SEPARATOR:
					Menu.Separator_Draw((menuseparator_s)menu.items[i]);

					break;
			}
		}

		item = Menu.Menu_ItemAtCursor(menu);

		if (item != null && item.cursordraw != null)
			item.cursordraw(item);
		else if (menu.cursordraw != null)
			menu.cursordraw(menu);
		else if (item != null && item.type != Defines.MTYPE_FIELD)
		{
			if ((item.flags & Defines.QMF_LEFT_JUSTIFY) != 0)
				Globals.re.DrawChar(menu.x + item.x - 24 + item.cursor_offset, menu.y + item.y, 12 + ((int)(Timer.Milliseconds() / 250) & 1));
			else
				Globals.re.DrawChar(menu.x + item.cursor_offset, menu.y + item.y, 12 + ((int)(Timer.Milliseconds() / 250) & 1));
		}

		if (item != null)
		{
			if (item.statusbarfunc != null)
				item.statusbarfunc(item);
			else if (item.statusbar != null)
				Menu.Menu_DrawStatusBar(item.statusbar);
			else
				Menu.Menu_DrawStatusBar(menu.statusbar);
		}
		else
			Menu.Menu_DrawStatusBar(menu.statusbar);
	}

	public static void Menu_DrawStatusBar(string @string)
	{
		if (@string != null)
		{
			var l = @string.Length;
			var maxrow = Globals.viddef.getHeight() / 8;
			var maxcol = Globals.viddef.getWidth() / 8;
			var col = maxcol / 2 - l / 2;
			Globals.re.DrawFill(0, Globals.viddef.getHeight() - 8, Globals.viddef.getWidth(), 8, 4);
			Menu.Menu_DrawString(col * 8, Globals.viddef.getHeight() - 8, @string);
		}
		else
			Globals.re.DrawFill(0, Globals.viddef.getHeight() - 8, Globals.viddef.getWidth(), 8, 0);
	}

	public static void Menu_DrawString(int x, int y, string @string)
	{
		int i;

		for (i = 0; i < @string.Length; i++)
			Globals.re.DrawChar(x + i * 8, y, @string[i]);
	}

	public static void Menu_DrawStringDark(int x, int y, string @string)
	{
		int i;

		for (i = 0; i < @string.Length; i++)
			Globals.re.DrawChar(x + i * 8, y, @string[i] + 128);
	}

	public static void Menu_DrawStringR2L(int x, int y, string @string)
	{
		int i;
		var l = @string.Length;

		for (i = 0; i < l; i++)
			Globals.re.DrawChar(x - i * 8, y, @string[l - i - 1]);
	}

	public static void Menu_DrawStringR2LDark(int x, int y, string @string)
	{
		int i;
		var l = @string.Length;

		for (i = 0; i < l; i++)
			Globals.re.DrawChar(x - i * 8, y, @string[l - i - 1] + 128);
	}

	public static menucommon_s Menu_ItemAtCursor(menuframework_s m)
	{
		if (m.cursor < 0 || m.cursor >= m.nitems)
			return null;

		return (menucommon_s)m.items[m.cursor];
	}

	public static bool Menu_SelectItem(menuframework_s s)
	{
		var item = Menu.Menu_ItemAtCursor(s);

		if (item != null)
		{
			switch (item.type)
			{
				case Defines.MTYPE_FIELD:
					return Menu.Field_DoEnter((menufield_s)item);

				case Defines.MTYPE_ACTION:
					Menu.Action_DoEnter((menuaction_s)item);

					return true;

				case Defines.MTYPE_LIST:
					//			Menulist_DoEnter( ( menulist_s ) item );
					return false;

				case Defines.MTYPE_SPINCONTROL:
					//			SpinControl_DoEnter( ( menulist_s ) item );
					return false;
			}
		}

		return false;
	}

	public static void Menu_SetStatusBar(menuframework_s m, string @string)
	{
		m.statusbar = @string;
	}

	public static void Menu_SlideItem(menuframework_s s, int dir)
	{
		var item = (menucommon_s)Menu.Menu_ItemAtCursor(s);

		if (item != null)
		{
			switch (item.type)
			{
				case Defines.MTYPE_SLIDER:
					Menu.Slider_DoSlide((menuslider_s)item, dir);

					break;

				case Defines.MTYPE_SPINCONTROL:
					Menu.SpinControl_DoSlide((menulist_s)item, dir);

					break;
			}
		}
	}

	public static int Menu_TallySlots(menuframework_s menu)
	{
		int i;
		var total = 0;

		for (i = 0; i < menu.nitems; i++)
		{
			if (((menucommon_s)menu.items[i]).type == Defines.MTYPE_LIST)
			{
				var nitems = 0;
				var n = ((menulist_s)menu.items[i]).itemnames;

				while (n[nitems] != null)
					nitems++;

				total += nitems;
			}
			else
				total++;
		}

		return total;
	}

	public static void Menulist_DoEnter(menulist_s l)
	{
		int start;
		start = l.y / 10 + 1;
		l.curvalue = l.parent.cursor - start;

		if (l.callback != null)
			l.callback(l);
	}

	public static void MenuList_Draw(menulist_s l)
	{
		string[] n;
		var y = 0;
		Menu.Menu_DrawStringR2LDark(l.x + l.parent.x + Defines.LCOLUMN_OFFSET, l.y + l.parent.y, l.name);
		n = l.itemnames;
		Globals.re.DrawFill(l.x - 112 + l.parent.x, l.parent.y + l.y + l.curvalue * 10 + 10, 128, 10, 16);
		var i = 0;

		while (n[i] != null)
		{
			Menu.Menu_DrawStringR2LDark(l.x + l.parent.x + Defines.LCOLUMN_OFFSET, l.y + l.parent.y + y + 10, n[i]);
			i++;
			y += 10;
		}
	}

	public static void Separator_Draw(menuseparator_s s)
	{
		if (s.name != null)
			Menu.Menu_DrawStringR2LDark(s.x + s.parent.x, s.y + s.parent.y, s.name);
	}

	public static void Slider_DoSlide(menuslider_s s, int dir)
	{
		s.curvalue += dir;

		if (s.curvalue > s.maxvalue)
			s.curvalue = s.maxvalue;
		else if (s.curvalue < s.minvalue)
			s.curvalue = s.minvalue;

		if (s.callback != null)
			s.callback(s);
	}

	public static readonly int SLIDER_RANGE = 10;

	public static void Slider_Draw(menuslider_s s)
	{
		int i;
		Menu.Menu_DrawStringR2LDark(s.x + s.parent.x + Defines.LCOLUMN_OFFSET, s.y + s.parent.y, s.name);
		s.range = (s.curvalue - s.minvalue) / (float)(s.maxvalue - s.minvalue);

		if (s.range < 0)
			s.range = 0;

		if (s.range > 1)
			s.range = 1;

		Globals.re.DrawChar(s.x + s.parent.x + Defines.RCOLUMN_OFFSET, s.y + s.parent.y, 128);

		for (i = 0; i < Menu.SLIDER_RANGE; i++)
			Globals.re.DrawChar(Defines.RCOLUMN_OFFSET + s.x + i * 8 + s.parent.x + 8, s.y + s.parent.y, 129);

		Globals.re.DrawChar(Defines.RCOLUMN_OFFSET + s.x + i * 8 + s.parent.x + 8, s.y + s.parent.y, 130);
		Globals.re.DrawChar((int)(8 + Defines.RCOLUMN_OFFSET + s.parent.x + s.x + (Menu.SLIDER_RANGE - 1) * 8 * s.range), s.y + s.parent.y, 131);
	}

	public static void SpinControl_DoEnter(menulist_s s)
	{
		s.curvalue++;

		if (s.itemnames[s.curvalue] == null)
			s.curvalue = 0;

		if (s.callback != null)
			s.callback(s);
	}

	public static void SpinControl_DoSlide(menulist_s s, int dir)
	{
		s.curvalue += dir;

		if (s.curvalue < 0)
			s.curvalue = 0;
		else if (s.curvalue >= s.itemnames.Length || s.itemnames[s.curvalue] == null)
			s.curvalue--;

		if (s.callback != null)
			s.callback(s);
	}

	public static void SpinControl_Draw(menulist_s s)
	{
		if (s.name != null)
			Menu.Menu_DrawStringR2LDark(s.x + s.parent.x + Defines.LCOLUMN_OFFSET, s.y + s.parent.y, s.name);

		if (s.itemnames[s.curvalue].IndexOf('\n') == -1)
			Menu.Menu_DrawString(Defines.RCOLUMN_OFFSET + s.x + s.parent.x, s.y + s.parent.y, s.itemnames[s.curvalue]);
		else
		{
			string line1, line2;
			line1 = Lib.leftFrom(s.itemnames[s.curvalue], '\n');
			Menu.Menu_DrawString(Defines.RCOLUMN_OFFSET + s.x + s.parent.x, s.y + s.parent.y, line1);
			line2 = Lib.rightFrom(s.itemnames[s.curvalue], '\n');
			var pos = line2.IndexOf('\n');

			if (pos != -1)
				line2 = line2[..pos];

			Menu.Menu_DrawString(Defines.RCOLUMN_OFFSET + s.x + s.parent.x, s.y + s.parent.y + 10, line2);
		}
	}

	static Menu()
	{
		Menu.Menu1();
		Menu.Menu2();
		Menu.Menu3();
		Menu.Menu4();
		Menu.Menu5();
	}
}