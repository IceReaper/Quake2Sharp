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
namespace Quake2Sharp.qcommon.types;

public class dareaportal_t
{
	public dareaportal_t()
	{
	}

	public dareaportal_t(BinaryReader bb)
	{
		this.portalnum = bb.ReadInt32();
		this.otherarea = bb.ReadInt32();
	}

	public int portalnum;
	public int otherarea;
	public static int SIZE = 8;
}