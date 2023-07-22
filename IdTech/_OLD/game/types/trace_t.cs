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

using Quake2Sharp.util;

namespace Quake2Sharp.game.types;

//a trace is returned when a box is swept through the world
public class trace_t
{
	public bool allsolid; // if true, plane is not valid
	public bool startsolid; // if true, the initial point was in a solid area
	public float fraction; // time completed, 1.0 = didn't hit anything
	public float[] endpos = { 0, 0, 0 }; // final position

	// memory
	public cplane_t plane = new(); // surface normal at impact

	// pointer
	public csurface_t surface; // surface hit
	public int contents; // contents on other side of surface hit

	// pointer
	public edict_t ent; // not set by CM_*() functions

	public void set(trace_t from)
	{
		this.allsolid = from.allsolid;
		this.startsolid = from.allsolid;
		this.fraction = from.fraction;
		Math3D.VectorCopy(from.endpos, this.endpos);
		this.plane.set(from.plane);
		this.surface = from.surface;
		this.contents = from.contents;
		this.ent = from.ent;
	}

	public void clear()
	{
		this.allsolid = false;
		this.startsolid = false;
		this.fraction = 0;
		Math3D.VectorClear(this.endpos);
		this.plane.clear();
		this.surface = null;
		this.contents = 0;
		this.ent = null;
	}
}