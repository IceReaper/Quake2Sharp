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
namespace Quake2Sharp.qcommon.types
{
	using System.IO;

	public class dleaf_t
	{
		public dleaf_t(byte[] cmod_base, int i, int j)
			: this(new(new MemoryStream(cmod_base, i, j)))
		{
		}

		public dleaf_t(BinaryReader bb)
		{
			this.contents = bb.ReadInt32();
			this.cluster = bb.ReadInt16();
			this.area = bb.ReadInt16();

			this.mins[0] = bb.ReadInt16();
			this.mins[1] = bb.ReadInt16();
			this.mins[2] = bb.ReadInt16();

			this.maxs[0] = bb.ReadInt16();
			this.maxs[1] = bb.ReadInt16();
			this.maxs[2] = bb.ReadInt16();

			this.firstleafface = bb.ReadUInt16();
			this.numleaffaces = bb.ReadUInt16();

			this.firstleafbrush = bb.ReadUInt16();
			this.numleafbrushes = bb.ReadUInt16();
		}

		public static readonly int SIZE = 4 + 8 * 2 + 4 * 2;
		public int contents; // OR of all brushes (not needed?)
		public short cluster;
		public short area;
		public short[] mins = { 0, 0, 0 }; // for frustum culling
		public short[] maxs = { 0, 0, 0 };
		public int firstleafface; // unsigned short
		public int numleaffaces; // unsigned short
		public int firstleafbrush; // unsigned short
		public int numleafbrushes; // unsigned short
	}
}
