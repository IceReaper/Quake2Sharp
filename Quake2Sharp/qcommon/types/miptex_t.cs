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

using System.Text;

/*
==============================================================================

  .WAL texture file format

==============================================================================
*/
public class miptex_t
{
	private static readonly int MIPLEVELS = 4;
	private static readonly int NAME_SIZE = 32;
	public string name; // char name[32];
	public int width, height;
	public int[] offsets = new int[miptex_t.MIPLEVELS]; // 4 mip maps stored

	// next frame in animation chain
	public string animname; //	char	animname[32];
	public int flags;
	public int contents;
	public int value;

	public miptex_t(byte[] dataBytes)
		: this(new BinaryReader(new MemoryStream(dataBytes)))
	{
	}

	public miptex_t(BinaryReader b)
	{
		var nameBuf = new byte[miptex_t.NAME_SIZE];

		// fill header
		b.Read(nameBuf);
		this.name = Encoding.ASCII.GetString(nameBuf).Split((char)0)[0];
		this.width = b.ReadInt32();
		this.height = b.ReadInt32();
		this.offsets[0] = b.ReadInt32();
		this.offsets[1] = b.ReadInt32();
		this.offsets[2] = b.ReadInt32();
		this.offsets[3] = b.ReadInt32();
		b.Read(nameBuf);
		this.animname = Encoding.ASCII.GetString(nameBuf).Split((char)0)[0];
		this.flags = b.ReadInt32();
		this.contents = b.ReadInt32();
		this.value = b.ReadInt32();
	}
}