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

public class dmodel_t
{
	public dmodel_t(BinaryReader bb)
	{
		for (var j = 0; j < 3; j++)
			this.mins[j] = bb.ReadSingle();

		for (var j = 0; j < 3; j++)
			this.maxs[j] = bb.ReadSingle();

		for (var j = 0; j < 3; j++)
			this.origin[j] = bb.ReadSingle();

		this.headnode = bb.ReadInt32();
		this.firstface = bb.ReadInt32();
		this.numfaces = bb.ReadInt32();
	}

	public float[] mins = { 0, 0, 0 };
	public float[] maxs = { 0, 0, 0 };
	public float[] origin = { 0, 0, 0 }; // for sounds or lights
	public int headnode;
	public int firstface, numfaces; // submodels just draw faces

	// without walking the bsp tree
	public static int SIZE = 3 * 4 + 3 * 4 + 3 * 4 + 4 + 8;
}