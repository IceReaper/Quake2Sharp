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
namespace Quake2Sharp.sys;

/**
 * KBD
 */
public abstract class KBD
{
	protected static int win_x = 0;
	protected static int win_y = 0;

	// motion values
	public static int mx = 0;
	public static int my = 0;
	public abstract void Init();
	public abstract void Update();
	public abstract void Close();
	public abstract void Do_Key_Event(int key, bool down, string? text);
	public abstract void installGrabs();
	public abstract void uninstallGrabs();

	//abstract public void centerMouse();
}