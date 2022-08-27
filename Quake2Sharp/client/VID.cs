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
using render;
using sound;
using sys;
using System;
using System.Drawing;
using System.Text;
using types;
using util;

public class VID
{
	//	   Main windowed and fullscreen graphics interface module. This module
	//	   is used for both the software and OpenGL rendering versions of the
	//	   Quake refresh engine.

	// Global variables used internally by this module
	// Globals.viddef
	// global video state; used by other modules

	// Structure containing functions exported from refresh DLL
	// Globals.re;

	// Console variables that we need to access from this module
	private static cvar_t vid_gamma;
	private static cvar_t vid_ref; // Name of Refresh DLL loaded
	private static cvar_t vid_xpos; // X coordinate of window position
	private static cvar_t vid_ypos; // Y coordinate of window position
	private static cvar_t vid_width;
	private static cvar_t vid_height;
	private static cvar_t vid_fullscreen;

	// Global variables used internally by this module
	// void *reflib_library;		// Handle to refresh DLL
	private static bool reflib_active;

	// const char so_file[] = "/etc/quake2.conf";
	/*
	==========================================================================

	DLL GLUE

	==========================================================================
	*/

	public static void Printf(int print_level, string fmt, params object[] vargs)
	{
		// static qboolean inupdate;
		if (print_level == Defines.PRINT_ALL)
			Com.Printf(fmt, vargs);
		else
			Com.DPrintf(fmt, vargs);
	}

	// ==========================================================================

	/*
	============
	VID_Restart_f

	Console command to re-start the video mode and refresh DLL. We do this
	simply by setting the modified flag for the vid_ref variable, which will
	cause the entire video mode and refresh DLL to be reset on the next frame.
	============
	*/
	private static void Restart_f()
	{
		VID.vid_modes[11].width = (int)VID.vid_width.value;
		VID.vid_modes[11].height = (int)VID.vid_height.value;
		VID.vid_ref.modified = true;
	}

	/*
	** VID_GetModeInfo
	*/
	private static readonly vidmode_t[] vid_modes =
	{
		new("Mode 0: 320x240", 320, 240, 0),
		new("Mode 1: 400x300", 400, 300, 1),
		new("Mode 2: 512x384", 512, 384, 2),
		new("Mode 3: 640x480", 640, 480, 3),
		new("Mode 4: 800x600", 800, 600, 4),
		new("Mode 5: 960x720", 960, 720, 5),
		new("Mode 6: 1024x768", 1024, 768, 6),
		new("Mode 7: 1152x864", 1152, 864, 7),
		new("Mode 8: 1280x1024", 1280, 1024, 8),
		new("Mode 9: 1600x1200", 1600, 1200, 9),
		new("Mode 10: 2048x1536", 2048, 1536, 10),
		new("Mode 11: user", 640, 480, 11)
	};

	private static vidmode_t[] fs_modes;

	public static bool GetModeInfo(ref Size dim, int mode)
	{
		if (VID.fs_modes == null)
			VID.initModeList();

		var modes = VID.vid_modes;

		if (VID.vid_fullscreen.value != 0.0f)
			modes = VID.fs_modes;

		if (mode < 0 || mode >= modes.Length)
			return false;

		dim.Width = modes[mode].width;
		dim.Height = modes[mode].height;

		return true;
	}

	/*
	** VID_NewWindow
	*/
	public static void NewWindow(int width, int height)
	{
		Globals.viddef.setSize(width, height);
	}

	private static void FreeReflib()
	{
		if (Globals.re != null)
		{
			Globals.re.getKeyboardHandler().Close();
			IN.Shutdown();
		}

		Globals.re = null;
		VID.reflib_active = false;
	}

	/*
	==============
	VID_LoadRefresh
	==============
	*/
	private static bool LoadRefresh(string name)
	{
		if (VID.reflib_active)
		{
			Globals.re.getKeyboardHandler().Close();
			IN.Shutdown();
			Globals.re.Shutdown();
			VID.FreeReflib();
		}

		Com.Printf("------- Loading " + name + " -------\n");
		var found = false;
		var driverNames = Renderer.getDriverNames();

		for (var i = 0; i < driverNames.Length; i++)
		{
			if (driverNames[i].Equals(name))
			{
				found = true;

				break;
			}
		}

		if (!found)
		{
			Com.Printf("LoadLibrary(\"" + name + "\") failed\n");

			return false;
		}

		Com.Printf("LoadLibrary(\"" + name + "\")\n");
		Globals.re = Renderer.getDriver(name);

		if (Globals.re == null)
			Com.Error(Defines.ERR_FATAL, name + " can't load but registered");

		if (Globals.re.apiVersion() != Defines.API_VERSION)
		{
			VID.FreeReflib();
			Com.Error(Defines.ERR_FATAL, name + " has incompatible api_version");
		}

		IN.Real_IN_Init();

		if (!Globals.re.Init((int)VID.vid_xpos.value, (int)VID.vid_ypos.value))
		{
			Globals.re.Shutdown();
			VID.FreeReflib();

			return false;
		}

		/* Init KBD */
		Globals.re.getKeyboardHandler().Init();
		Com.Printf("------------------------------------\n");
		VID.reflib_active = true;

		return true;
	}

	/*
	============
	VID_CheckChanges

	This function gets called once just before drawing each frame, and it's sole purpose in life
	is to check to see if any of the video mode parameters have changed, and if they have to 
	update the rendering DLL and/or video mode to match.
	============
	*/
	public static void CheckChanges()
	{
		Globals.viddef.update();

		if (VID.vid_ref.modified)
			S.StopAllSounds();

		while (VID.vid_ref.modified)
		{
			/*
			** refresh has changed
			*/
			VID.vid_ref.modified = false;
			VID.vid_fullscreen.modified = true;
			Globals.cl.refresh_prepped = false;
			Globals.cls.disable_screen = 1.0f; // true;

			if (!VID.LoadRefresh(VID.vid_ref.@string))
			{
				string renderer;

				if (VID.vid_ref.@string.Equals(Renderer.getPreferedName()))
				{
					// try the default renderer as fallback after prefered
					renderer = Renderer.getDefaultName();
				}
				else
				{
					// try the prefered renderer as first fallback
					renderer = Renderer.getPreferedName();
				}

				if (VID.vid_ref.@string.Equals(Renderer.getDefaultName()))
				{
					renderer = VID.vid_ref.@string;
					Com.Printf("Refresh failed\n");
					VID.gl_mode = Cvar.Get("gl_mode", "0", 0);

					if (VID.gl_mode.value != 0.0f)
					{
						Com.Printf("Trying mode 0\n");
						Cvar.SetValue("gl_mode", 0);

						if (!VID.LoadRefresh(VID.vid_ref.@string))
							Com.Error(Defines.ERR_FATAL, "Couldn't fall back to " + renderer + " refresh!");
					}
					else
						Com.Error(Defines.ERR_FATAL, "Couldn't fall back to " + renderer + " refresh!");
				}

				Cvar.Set("vid_ref", renderer);

				/*
				 * drop the console if we fail to load a refresh
				 */
				if (Globals.cls.key_dest != Defines.key_console)
				{
					try
					{
						Console.ToggleConsole_f();
					}
					catch (Exception)
					{
					}
				}
			}

			Globals.cls.disable_screen = 0.0f; //false;
		}
	}

	/*
	============
	VID_Init
	============
	*/
	public static void Init()
	{
		/* Create the video variables so we know how to start the graphics drivers */
		VID.vid_ref = Cvar.Get("vid_ref", Renderer.getPreferedName(), Defines.CVAR_ARCHIVE);
		VID.vid_xpos = Cvar.Get("vid_xpos", "3", Defines.CVAR_ARCHIVE);
		VID.vid_ypos = Cvar.Get("vid_ypos", "22", Defines.CVAR_ARCHIVE);
		VID.vid_width = Cvar.Get("vid_width", "640", Defines.CVAR_ARCHIVE);
		VID.vid_height = Cvar.Get("vid_height", "480", Defines.CVAR_ARCHIVE);
		VID.vid_fullscreen = Cvar.Get("vid_fullscreen", "0", Defines.CVAR_ARCHIVE);
		VID.vid_gamma = Cvar.Get("vid_gamma", "1", Defines.CVAR_ARCHIVE);
		VID.vid_modes[11].width = (int)VID.vid_width.value;
		VID.vid_modes[11].height = (int)VID.vid_height.value;
		/* Add some console commands that we want to handle */
		Cmd.AddCommand("vid_restart", () => { VID.Restart_f(); });
		/* Disable the 3Dfx splash screen */
		// putenv("FX_GLIDE_NO_SPLASH=0");
		/* Start the graphics mode and load refresh DLL */
		VID.CheckChanges();
	}

	/*
	============
	VID_Shutdown
	============
	*/
	public static void Shutdown()
	{
		if (VID.reflib_active)
		{
			Globals.re.getKeyboardHandler().Close();
			IN.Shutdown();
			Globals.re.Shutdown();
			VID.FreeReflib();
		}
	}

	// ==========================================================================
	// 
	//	vid_menu.c
	//
	// ==========================================================================
	private static cvar_t gl_mode;
	private static cvar_t gl_driver;
	private static cvar_t gl_picmip;
	private static cvar_t gl_ext_palettedtexture;

	private static cvar_t gl_swapinterval;
	/*
	====================================================================

	MENU INTERACTION

	====================================================================
	*/

	private static readonly Menu.menuframework_s s_opengl_menu = new();
	private static Menu.menuframework_s s_current_menu; // referenz
	private static readonly Menu.menulist_s s_mode_list = new();
	private static readonly Menu.menulist_s s_ref_list = new();
	private static readonly Menu.menuslider_s s_tq_slider = new();
	private static readonly Menu.menuslider_s s_screensize_slider = new();
	private static readonly Menu.menuslider_s s_brightness_slider = new();
	private static readonly Menu.menulist_s s_fs_box = new();
	private static Menu.menulist_s s_stipple_box = new();
	private static readonly Menu.menulist_s s_paletted_texture_box = new();
	private static readonly Menu.menulist_s s_vsync_box = new();
	private static Menu.menulist_s s_windowed_mouse = new();
	private static readonly Menu.menuaction_s s_apply_action = new();
	private static readonly Menu.menuaction_s s_defaults_action = new();

	private static void DriverCallback(object unused)
	{
		VID.s_current_menu = VID.s_opengl_menu; // s_software_menu;
	}

	private static void ScreenSizeCallback(object s)
	{
		var slider = (Menu.menuslider_s)s;
		Cvar.SetValue("viewsize", slider.curvalue * 10);
	}

	private static void BrightnessCallback(object s)
	{
		var slider = (Menu.menuslider_s)s;

		// if ( stricmp( vid_ref.string, "soft" ) == 0 ||
		//	stricmp( vid_ref.string, "softx" ) == 0 )
		if (VID.vid_ref.@string.Equals("soft", StringComparison.OrdinalIgnoreCase)
		    || VID.vid_ref.@string.Equals("softx", StringComparison.OrdinalIgnoreCase))
		{
			var gamma = 0.8f - (slider.curvalue / 10.0f - 0.5f) + 0.5f;
			Cvar.SetValue("vid_gamma", gamma);
		}
	}

	private static void ResetDefaults(object unused)
	{
		VID.MenuInit();
	}

	private static void ApplyChanges(object unused)
	{
		/*
		** invert sense so greater = brighter, and scale to a range of 0.5 to 1.3
		*/
		// the original was modified, because on CRTs it was too dark.
		// the slider range is [5; 13]
		// gamma: [1.1; 0.7]
		var gamma = 0.4f - (VID.s_brightness_slider.curvalue / 20.0f - 0.25f) + 0.7f;

		// modulate:  [1.0; 2.6]
		var modulate = VID.s_brightness_slider.curvalue * 0.2f;
		Cvar.SetValue("vid_gamma", gamma);
		Cvar.SetValue("gl_modulate", modulate);
		Cvar.SetValue("gl_picmip", 3 - VID.s_tq_slider.curvalue);
		Cvar.SetValue("vid_fullscreen", VID.s_fs_box.curvalue);
		Cvar.SetValue("gl_swapinterval", (int)VID.s_vsync_box.curvalue);

		// set always true because of vid_ref or mode changes
		VID.gl_swapinterval.modified = true;
		Cvar.SetValue("gl_ext_palettedtexture", VID.s_paletted_texture_box.curvalue);
		Cvar.SetValue("gl_mode", VID.s_mode_list.curvalue);
		Cvar.Set("vid_ref", VID.drivers[VID.s_ref_list.curvalue]);
		Cvar.Set("gl_driver", VID.drivers[VID.s_ref_list.curvalue]);

		if (VID.gl_driver.modified)
			VID.vid_ref.modified = true;

		Menu.ForceMenuOff();
	}

	private static readonly string[] resolutions =
	{
		"[320 240  ]",
		"[400 300  ]",
		"[512 384  ]",
		"[640 480  ]",
		"[800 600  ]",
		"[960 720  ]",
		"[1024 768 ]",
		"[1152 864 ]",
		"[1280 1024]",
		"[1600 1200]",
		"[2048 1536]",
		"user mode"
	};

	private static string[] fs_resolutions;
	private static int mode_x;
	private static string[] refs;
	private static string[] drivers;
	private static readonly string[] yesno_names = { "no", "yes" };

	private static void initModeList()
	{
		var modes = Globals.re.getModeList();
		VID.fs_resolutions = new string[modes.Length];
		VID.fs_modes = new vidmode_t[modes.Length];

		for (var i = 0; i < modes.Length; i++)
		{
			var m = modes[i];
			StringBuilder sb = new(18);
			sb.Append('[');
			sb.Append(m.Width);
			sb.Append(' ');
			sb.Append(m.Height);

			while (sb.Length < 10)
				sb.Append(' ');

			sb.Append(']');
			VID.fs_resolutions[i] = sb.ToString();
			sb.Length = 0;
			sb.Append("Mode ");
			sb.Append(i);
			sb.Append(':');
			sb.Append(m.Width);
			sb.Append('x');
			sb.Append(m.Height);
			VID.fs_modes[i] = new(sb.ToString(), m.Width, m.Height, i);
		}
	}

	private static void initRefs()
	{
		VID.drivers = Renderer.getDriverNames();
		VID.refs = new string[VID.drivers.Length];
		StringBuilder sb = new();

		for (var i = 0; i < VID.drivers.Length; i++)
		{
			sb.Length = 0;
			sb.Append("[OpenGL ").Append(VID.drivers[i]);

			while (sb.Length < 16)
				sb.Append(" ");

			sb.Append("]");
			VID.refs[i] = sb.ToString();
		}
	}

	/*
	** VID_MenuInit
	*/
	public static void MenuInit()
	{
		VID.initRefs();

		if (VID.gl_driver == null)
			VID.gl_driver = Cvar.Get("gl_driver", Renderer.getPreferedName(), 0);

		if (VID.gl_picmip == null)
			VID.gl_picmip = Cvar.Get("gl_picmip", "0", 0);

		if (VID.gl_mode == null)
			VID.gl_mode = Cvar.Get("gl_mode", "3", 0);

		if (VID.gl_ext_palettedtexture == null)
			VID.gl_ext_palettedtexture = Cvar.Get("gl_ext_palettedtexture", "1", Defines.CVAR_ARCHIVE);

		if (VID.gl_swapinterval == null)
			VID.gl_swapinterval = Cvar.Get("gl_swapinterval", "0", Defines.CVAR_ARCHIVE);

		VID.s_mode_list.curvalue = (int)VID.gl_mode.value;

		if (VID.vid_fullscreen.value != 0.0f)
		{
			VID.s_mode_list.itemnames = VID.fs_resolutions;

			if (VID.s_mode_list.curvalue >= VID.fs_resolutions.Length - 1)
				VID.s_mode_list.curvalue = 0;

			VID.mode_x = VID.fs_modes[VID.s_mode_list.curvalue].width;
		}
		else
		{
			VID.s_mode_list.itemnames = VID.resolutions;

			if (VID.s_mode_list.curvalue >= VID.resolutions.Length - 1)
				VID.s_mode_list.curvalue = 0;

			VID.mode_x = VID.vid_modes[VID.s_mode_list.curvalue].width;
		}

		if (SCR.scr_viewsize == null)
			SCR.scr_viewsize = Cvar.Get("viewsize", "100", Defines.CVAR_ARCHIVE);

		VID.s_screensize_slider.curvalue = (int)(SCR.scr_viewsize.value / 10);

		for (var i = 0; i < VID.drivers.Length; i++)
		{
			if (VID.vid_ref.@string.Equals(VID.drivers[i]))
				VID.s_ref_list.curvalue = i;
		}

		VID.s_opengl_menu.x = (int)(Globals.viddef.getWidth() * 0.50f);
		VID.s_opengl_menu.nitems = 0;
		VID.s_ref_list.type = Defines.MTYPE_SPINCONTROL;
		VID.s_ref_list.name = "driver";
		VID.s_ref_list.x = 0;
		VID.s_ref_list.y = 0;
		VID.s_ref_list.callback = self => { VID.DriverCallback(self); };
		VID.s_ref_list.itemnames = VID.refs;
		VID.s_mode_list.type = Defines.MTYPE_SPINCONTROL;
		VID.s_mode_list.name = "video mode";
		VID.s_mode_list.x = 0;
		VID.s_mode_list.y = 10;
		VID.s_screensize_slider.type = Defines.MTYPE_SLIDER;
		VID.s_screensize_slider.x = 0;
		VID.s_screensize_slider.y = 20;
		VID.s_screensize_slider.name = "screen size";
		VID.s_screensize_slider.minvalue = 3;
		VID.s_screensize_slider.maxvalue = 12;
		VID.s_screensize_slider.callback = self => { VID.ScreenSizeCallback(self); };
		VID.s_brightness_slider.type = Defines.MTYPE_SLIDER;
		VID.s_brightness_slider.x = 0;
		VID.s_brightness_slider.y = 30;
		VID.s_brightness_slider.name = "brightness";
		VID.s_brightness_slider.callback = self => { VID.BrightnessCallback(self); };
		VID.s_brightness_slider.minvalue = 5;
		VID.s_brightness_slider.maxvalue = 13;
		VID.s_brightness_slider.curvalue = (1.3f - VID.vid_gamma.value + 0.5f) * 10;
		VID.s_fs_box.type = Defines.MTYPE_SPINCONTROL;
		VID.s_fs_box.x = 0;
		VID.s_fs_box.y = 40;
		VID.s_fs_box.name = "fullscreen";
		VID.s_fs_box.itemnames = VID.yesno_names;
		VID.s_fs_box.curvalue = (int)VID.vid_fullscreen.value;

		VID.s_fs_box.callback = o =>
		{
			var fs = ((Menu.menulist_s)o).curvalue;

			if (fs == 0)
			{
				VID.s_mode_list.itemnames = VID.resolutions;
				var i = VID.vid_modes.Length - 2;

				while (i > 0 && VID.vid_modes[i].width > VID.mode_x)
					i--;

				VID.s_mode_list.curvalue = i;
			}
			else
			{
				VID.s_mode_list.itemnames = VID.fs_resolutions;
				var i = VID.fs_modes.Length - 1;

				while (i > 0 && VID.fs_modes[i].width > VID.mode_x)
					i--;

				VID.s_mode_list.curvalue = i;
			}
		};

		VID.s_tq_slider.type = Defines.MTYPE_SLIDER;
		VID.s_tq_slider.x = 0;
		VID.s_tq_slider.y = 60;
		VID.s_tq_slider.name = "texture quality";
		VID.s_tq_slider.minvalue = 0;
		VID.s_tq_slider.maxvalue = 3;
		VID.s_tq_slider.curvalue = 3 - VID.gl_picmip.value;
		VID.s_paletted_texture_box.type = Defines.MTYPE_SPINCONTROL;
		VID.s_paletted_texture_box.x = 0;
		VID.s_paletted_texture_box.y = 70;
		VID.s_paletted_texture_box.name = "8-bit textures";
		VID.s_paletted_texture_box.itemnames = VID.yesno_names;
		VID.s_paletted_texture_box.curvalue = (int)VID.gl_ext_palettedtexture.value;
		VID.s_vsync_box.type = Defines.MTYPE_SPINCONTROL;
		VID.s_vsync_box.x = 0;
		VID.s_vsync_box.y = 80;
		VID.s_vsync_box.name = "sync every frame";
		VID.s_vsync_box.itemnames = VID.yesno_names;
		VID.s_vsync_box.curvalue = (int)VID.gl_swapinterval.value;
		VID.s_defaults_action.type = Defines.MTYPE_ACTION;
		VID.s_defaults_action.name = "reset to default";
		VID.s_defaults_action.x = 0;
		VID.s_defaults_action.y = 100;
		VID.s_defaults_action.callback = self => { VID.ResetDefaults(self); };
		VID.s_apply_action.type = Defines.MTYPE_ACTION;
		VID.s_apply_action.name = "apply";
		VID.s_apply_action.x = 0;
		VID.s_apply_action.y = 110;
		VID.s_apply_action.callback = self => { VID.ApplyChanges(self); };
		Menu.Menu_AddItem(VID.s_opengl_menu, VID.s_ref_list);
		Menu.Menu_AddItem(VID.s_opengl_menu, VID.s_mode_list);
		Menu.Menu_AddItem(VID.s_opengl_menu, VID.s_screensize_slider);
		Menu.Menu_AddItem(VID.s_opengl_menu, VID.s_brightness_slider);
		Menu.Menu_AddItem(VID.s_opengl_menu, VID.s_fs_box);
		Menu.Menu_AddItem(VID.s_opengl_menu, VID.s_tq_slider);
		Menu.Menu_AddItem(VID.s_opengl_menu, VID.s_paletted_texture_box);
		Menu.Menu_AddItem(VID.s_opengl_menu, VID.s_vsync_box);
		Menu.Menu_AddItem(VID.s_opengl_menu, VID.s_defaults_action);
		Menu.Menu_AddItem(VID.s_opengl_menu, VID.s_apply_action);
		Menu.Menu_Center(VID.s_opengl_menu);
		VID.s_opengl_menu.x -= 8;
	}

	/*
	================
	VID_MenuDraw
	================
	*/
	public static void MenuDraw()
	{
		VID.s_current_menu = VID.s_opengl_menu;
		/*
		** draw the banner
		*/
		var dim = new Size();
		Globals.re.DrawGetPicSize(ref dim, "m_banner_video");
		Globals.re.DrawPic(Globals.viddef.getWidth() / 2 - dim.Width / 2, Globals.viddef.getHeight() / 2 - 110, "m_banner_video");
		/*
		** move cursor to a reasonable starting position
		*/
		Menu.Menu_AdjustCursor(VID.s_current_menu, 1);
		/*
		** draw the menu
		*/
		Menu.Menu_Draw(VID.s_current_menu);
	}

	/*
	================
	VID_MenuKey
	================
	*/
	public static string MenuKey(int key)
	{
		var m = VID.s_current_menu;
		var sound = "misc/menu1.wav";

		switch (key)
		{
			case Key.K_ESCAPE:
				Menu.PopMenu();

				return null;

			case Key.K_UPARROW:
				m.cursor--;
				Menu.Menu_AdjustCursor(m, -1);

				break;

			case Key.K_DOWNARROW:
				m.cursor++;
				Menu.Menu_AdjustCursor(m, 1);

				break;

			case Key.K_LEFTARROW:
				Menu.Menu_SlideItem(m, -1);

				break;

			case Key.K_RIGHTARROW:
				Menu.Menu_SlideItem(m, 1);

				break;

			case Key.K_ENTER:
				Menu.Menu_SelectItem(m);

				break;
		}

		return sound;
	}
}