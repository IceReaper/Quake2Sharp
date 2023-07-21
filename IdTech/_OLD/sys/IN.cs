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
using game;
using game.types;
using qcommon;
using util;

/**
 * IN
 */
public class IN
{
	private static bool mouse_avail = true;
	public static bool mouse_active;
	private static int old_mouse_x;
	private static int old_mouse_y;
	private static bool mlooking;

	public static void ActivateMouse()
	{
		if (!IN.mouse_avail)
			return;

		if (!IN.mouse_active)
		{
			KBD.mx = KBD.my = 0; // don't spazz
			IN.install_grabs();
			IN.mouse_active = true;
		}
	}

	public static void DeactivateMouse()
	{
		// if (!mouse_avail || c == null) return;
		if (IN.mouse_active)
		{
			IN.uninstall_grabs();
			IN.mouse_active = false;
		}
	}

	private static void install_grabs()
	{
		Globals.re.getKeyboardHandler().installGrabs();
	}

	private static void uninstall_grabs()
	{
		Globals.re.getKeyboardHandler().uninstallGrabs();
	}

	public static void toggleMouse()
	{
		if (IN.mouse_avail)
		{
			IN.mouse_avail = false;
			IN.DeactivateMouse();
		}
		else
		{
			IN.mouse_avail = true;
			IN.ActivateMouse();
		}
	}

	public static void Init()
	{
		Globals.in_mouse = cvar.Cvar_Get("in_mouse", "1", Defines.CVAR_ARCHIVE);
		Globals.in_joystick = cvar.Cvar_Get("in_joystick", "0", Defines.CVAR_ARCHIVE);
	}

	public static void Shutdown()
	{
		IN.mouse_avail = false;
	}

	public static void Real_IN_Init()
	{
		// mouse variables
		Globals.m_filter = cvar.Cvar_Get("m_filter", "0", 0);
		Globals.in_mouse = cvar.Cvar_Get("in_mouse", "1", Defines.CVAR_ARCHIVE);
		Globals.freelook = cvar.Cvar_Get("freelook", "1", 0);
		Globals.lookstrafe = cvar.Cvar_Get("lookstrafe", "0", 0);
		Globals.sensitivity = cvar.Cvar_Get("sensitivity", "3", 0);
		Globals.m_pitch = cvar.Cvar_Get("m_pitch", "0.022", 0);
		Globals.m_yaw = cvar.Cvar_Get("m_yaw", "0.022", 0);
		Globals.m_forward = cvar.Cvar_Get("m_forward", "1", 0);
		Globals.m_side = cvar.Cvar_Get("m_side", "0.8", 0);

		cmdparser.Cmd_AddCommand("+mlook", () => { IN.MLookDown(); });

		cmdparser.Cmd_AddCommand("-mlook", () => { IN.MLookUp(); });

		cmdparser.Cmd_AddCommand("force_centerview", () => { IN.Force_CenterView_f(); });

		cmdparser.Cmd_AddCommand("togglemouse", () => { IN.toggleMouse(); });

		IN.mouse_avail = true;
	}

	public static void Frame()
	{
		if (!Globals.cl.cinematicpalette_active
		    && (!Globals.cl.refresh_prepped || Globals.cls.key_dest == Defines.key_console || Globals.cls.key_dest == Defines.key_menu))
			IN.DeactivateMouse();
		else
			IN.ActivateMouse();
	}

	public static void CenterView()
	{
		Globals.cl.viewangles[Defines.PITCH] = -Math3D.SHORT2ANGLE(Globals.cl.frame.playerstate.pmove.delta_angles[Defines.PITCH]);
	}

	public static void Move(usercmd_t cmd)
	{
		if (!IN.mouse_avail)
			return;

		if (Globals.m_filter.value != 0.0f)
		{
			KBD.mx = (KBD.mx + IN.old_mouse_x) / 2;
			KBD.my = (KBD.my + IN.old_mouse_y) / 2;
		}

		IN.old_mouse_x = KBD.mx;
		IN.old_mouse_y = KBD.my;

		KBD.mx = (int)(KBD.mx * Globals.sensitivity.value);
		KBD.my = (int)(KBD.my * Globals.sensitivity.value);

		// add mouse X/Y movement to cmd
		if ((CL_input.in_strafe.state & 1) != 0 || (Globals.lookstrafe.value != 0 && IN.mlooking))
			cmd.sidemove += (short)(Globals.m_side.value * KBD.mx);
		else
			Globals.cl.viewangles[Defines.YAW] -= Globals.m_yaw.value * KBD.mx;

		if ((IN.mlooking || Globals.freelook.value != 0.0f) && (CL_input.in_strafe.state & 1) == 0)
			Globals.cl.viewangles[Defines.PITCH] += Globals.m_pitch.value * KBD.my;
		else
			cmd.forwardmove -= (short)(Globals.m_forward.value * KBD.my);

		KBD.mx = KBD.my = 0;
	}

	private static void MLookDown()
	{
		IN.mlooking = true;
	}

	private static void MLookUp()
	{
		IN.mlooking = false;
		IN.CenterView();
	}

	private static void Force_CenterView_f()
	{
		Globals.cl.viewangles[Defines.PITCH] = 0;
	}
}
