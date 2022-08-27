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
namespace Quake2Sharp.game.types;

using adapters;
using System.IO;
using util;

public class mmove_t
{
	public mmove_t(int firstframe, int lastframe, mframe_t[] frame, EntThinkAdapter endfunc)
	{
		this.firstframe = firstframe;
		this.lastframe = lastframe;
		this.frame = frame;
		this.endfunc = endfunc;
	}

	public mmove_t()
	{
	}

	public int firstframe;
	public int lastframe;
	public mframe_t[] frame; //ptr
	public EntThinkAdapter endfunc;

	/** Writes the structure to a random acccess file. */
	public void write(BinaryWriter f)
	{
		f.Write(this.firstframe);
		f.Write(this.lastframe);

		if (this.frame == null)
			f.Write(-1);
		else
		{
			f.Write(this.frame.Length);

			for (var n = 0; n < this.frame.Length; n++)
				this.frame[n].write(f);
		}

		f.Write(this.endfunc);
	}

	/** Read the mmove_t from the RandomAccessFile. */
	public void read(BinaryReader f)
	{
		this.firstframe = f.ReadInt32();
		this.lastframe = f.ReadInt32();

		var len = f.ReadInt32();

		this.frame = new mframe_t[len];

		for (var n = 0; n < len; n++)
		{
			this.frame[n] = new();
			this.frame[n].read(f);
		}

		this.endfunc = (EntThinkAdapter)f.ReadAdapter();
	}
}