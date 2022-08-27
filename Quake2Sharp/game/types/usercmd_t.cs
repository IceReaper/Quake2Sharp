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
namespace Quake2Sharp.game.types;

public class usercmd_t
{
	public byte msec;
	public byte buttons;
	public short[] angles = new short[3];
	public short forwardmove, sidemove, upmove;
	public byte impulse; // remove?
	public byte lightlevel; // light level the player is standing on

	public void clear()
	{
		this.forwardmove = this.sidemove = this.upmove = this.msec = this.buttons = this.impulse = this.lightlevel = 0;
		this.angles[0] = this.angles[1] = this.angles[2] = 0;
	}

	public usercmd_t()
	{
	}

	public usercmd_t(usercmd_t from)
	{
		this.msec = from.msec;
		this.buttons = from.buttons;
		this.angles[0] = from.angles[0];
		this.angles[1] = from.angles[1];
		this.angles[2] = from.angles[2];
		this.forwardmove = from.forwardmove;
		this.sidemove = from.sidemove;
		this.upmove = from.upmove;
		this.impulse = from.impulse;
		this.lightlevel = from.lightlevel;
	}

	public usercmd_t set(usercmd_t from)
	{
		this.msec = from.msec;
		this.buttons = from.buttons;
		this.angles[0] = from.angles[0];
		this.angles[1] = from.angles[1];
		this.angles[2] = from.angles[2];
		this.forwardmove = from.forwardmove;
		this.sidemove = from.sidemove;
		this.upmove = from.upmove;
		this.impulse = from.impulse;
		this.lightlevel = from.lightlevel;

		return this;
	}
}