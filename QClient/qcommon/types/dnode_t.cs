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
namespace QClient.qcommon.types;

public class dnode_t
{
	public dnode_t(BinaryReader bb)
	{
		this.planenum = bb.ReadInt32();

		this.children[0] = bb.ReadInt32();
		this.children[1] = bb.ReadInt32();

		for (var j = 0; j < 3; j++)
			this.mins[j] = bb.ReadInt16();

		for (var j = 0; j < 3; j++)
			this.maxs[j] = bb.ReadInt16();

		this.firstface = bb.ReadUInt16();
		this.numfaces = bb.ReadUInt16();
	}

	public int planenum;
	public int[] children = { 0, 0 };

	// negative numbers are -(leafs+1), not nodes
	public short[] mins = { 0, 0, 0 }; // for frustom culling

	public short[] maxs = { 0, 0, 0 };
	/*
	unsigned short	firstface;
	unsigned short	numfaces;	// counting both sides
	*/

	public int firstface;
	public int numfaces;
	public static int SIZE = 4 + 8 + 6 + 6 + 2 + 2; // counting both sides
}