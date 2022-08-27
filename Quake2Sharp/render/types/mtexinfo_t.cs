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
namespace Quake2Sharp.render.types;

using System;

public class mtexinfo_t
{
	// [s/t][xyz offset]
	public float[][] vecs = { new float[] { 0, 0, 0, 0 }, new float[] { 0, 0, 0, 0 } };
	public int flags;
	public int numframes;
	public mtexinfo_t next; // animation chain
	public image_t image;

	public void clear()
	{
		Array.Fill(this.vecs[0], 0);
		Array.Fill(this.vecs[1], 0);

		this.flags = 0;
		this.numframes = 0;
		this.next = null;
		this.image = null;
	}
}