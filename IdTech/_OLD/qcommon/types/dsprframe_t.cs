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

using System.Text;

namespace Quake2Sharp.qcommon.types;

public class dsprframe_t
{
	public int width, height;
	public int origin_x, origin_y; // raster coordinates inside pic
	public string name; // name of pcx file (MAX_SKINNAME)

	public dsprframe_t(BinaryReader b)
	{
		this.width = b.ReadInt32();
		this.height = b.ReadInt32();
		this.origin_x = b.ReadInt32();
		this.origin_y = b.ReadInt32();
		var nameBuf = new byte[qfiles.MAX_SKINNAME];
		b.Read(nameBuf);
		this.name = Encoding.ASCII.GetString(nameBuf).Split((char)0)[0];
	}
}