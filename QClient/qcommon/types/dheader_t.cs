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

public class dheader_t
{
	public dheader_t(BinaryReader bb)
	{
		this.ident = bb.ReadInt32();
		this.version = bb.ReadInt32();

		for (var n = 0; n < Defines.HEADER_LUMPS; n++)
			this.lumps[n] = new(bb.ReadInt32(), bb.ReadInt32());
	}

	public int ident;
	public int version;
	public lump_t[] lumps = new lump_t[Defines.HEADER_LUMPS];
}