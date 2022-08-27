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

/*
========================================================================

PCX files are used for as many images as possible

========================================================================
*/
public class pcx_t
{
	// size of byte arrays
	private static readonly int PALETTE_SIZE = 48;
	private static readonly int FILLER_SIZE = 58;
	public byte manufacturer;
	public byte version;
	public byte encoding;
	public byte bits_per_pixel;
	public int xmin, ymin, xmax, ymax; // unsigned short
	public int hres, vres; // unsigned short
	public byte[] palette; //unsigned byte; size 48
	public byte reserved;
	public byte color_planes;
	public int bytes_per_line; // unsigned short
	public int palette_type; // unsigned short
	public byte[] filler; // size 58
	public byte[] data; //unbounded data

	public pcx_t(byte[] dataBytes)
		: this(new BinaryReader(new MemoryStream(dataBytes)))
	{
	}

	public pcx_t(BinaryReader b)
	{
		// fill header
		this.manufacturer = b.ReadByte();
		this.version = b.ReadByte();
		this.encoding = b.ReadByte();
		this.bits_per_pixel = b.ReadByte();
		this.xmin = b.ReadUInt16();
		this.ymin = b.ReadUInt16();
		this.xmax = b.ReadUInt16();
		this.ymax = b.ReadUInt16();
		this.hres = b.ReadUInt16();
		this.vres = b.ReadUInt16();
		b.Read(this.palette = new byte[pcx_t.PALETTE_SIZE]);
		this.reserved = b.ReadByte();
		this.color_planes = b.ReadByte();
		this.bytes_per_line = b.ReadUInt16();
		this.palette_type = b.ReadUInt16();
		b.Read(this.filler = new byte[pcx_t.FILLER_SIZE]);

		// fill data
		this.data = b.ReadBytes((int)(b.BaseStream.Length - b.BaseStream.Position));
	}
}