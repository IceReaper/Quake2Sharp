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

public class dface_t
{
	public static readonly int SIZE = 4 * Defines.SIZE_OF_SHORT + 2 * Defines.SIZE_OF_INT + Defines.MAXLIGHTMAPS;

	//unsigned short	planenum;
	public int planenum;
	public short side;
	public int firstedge; // we must support > 64k edges
	public short numedges;
	public short texinfo;

	// lighting info
	public byte[] styles = new byte[Defines.MAXLIGHTMAPS];
	public int lightofs; // start of [numstyles*surfsize] samples

	public dface_t(BinaryReader b)
	{
		this.planenum = b.ReadUInt16();
		this.side = b.ReadInt16();
		this.firstedge = b.ReadInt32();
		this.numedges = b.ReadInt16();
		this.texinfo = b.ReadInt16();
		b.Read(this.styles);
		this.lightofs = b.ReadInt32();
	}
}