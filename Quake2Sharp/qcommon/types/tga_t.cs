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

	/*
	========================================================================

	TGA files are used for sky planes

	========================================================================
	*/
	public class tga_t
	{
		// targa header
		public int id_length, colormap_type, image_type; // unsigned char
		public int colormap_index, colormap_length; // unsigned short
		public int colormap_size; // unsigned char
		public int x_origin, y_origin, width, height; // unsigned short
		public int pixel_size, attributes; // unsigned char
		public byte[] data; // (un)compressed data

		public tga_t(byte[] dataBytes)
			: this(new BinaryReader(new MemoryStream(dataBytes)))
		{
		}

		public tga_t(BinaryReader b)
		{
			// fill header
			this.id_length = b.ReadByte();
			this.colormap_type = b.ReadByte();
			this.image_type = b.ReadByte();
			this.colormap_index = b.ReadUInt16();
			this.colormap_length = b.ReadUInt16();
			this.colormap_size = b.ReadByte();
			this.x_origin = b.ReadUInt16();
			this.y_origin = b.ReadUInt16();
			this.width = b.ReadUInt16();
			this.height = b.ReadUInt16();
			this.pixel_size = b.ReadByte();

			// fill data
			this.data = b.ReadBytes((int)(b.BaseStream.Length - b.BaseStream.Position));
		}
	}
}
