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

using System.IO;

public class dvertex_t
{
	public static readonly int SIZE = 3 * 4; // 3 mal 32 bit float
	public float[] point = { 0, 0, 0 };

	public dvertex_t(BinaryReader b)
	{
		this.point[0] = b.ReadSingle();
		this.point[1] = b.ReadSingle();
		this.point[2] = b.ReadSingle();
	}
}