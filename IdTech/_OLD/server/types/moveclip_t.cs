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

using Quake2Sharp.game.types;
using Quake2Sharp.util;

namespace Quake2Sharp.server.types;

public class moveclip_t
{
	public float[] boxmins = { 0, 0, 0 }; // enclose the test object along entire move
	public float[] boxmaxs = { 0, 0, 0 }; // enclose the test object along entire move
	public float[] mins; // size of the moving object
	public float[] maxs; // size of the moving object
	public float[] mins2 = { 0, 0, 0 }; // size when clipping against mosnters
	public float[] maxs2 = { 0, 0, 0 }; // size when clipping against mosnters
	public float[] start;
	public float[] end;

	// mem
	public trace_t trace = new();
	public edict_t passedict;
	public int contentmask;

	public void clear()
	{
		Math3D.VectorClear(this.boxmins);
		Math3D.VectorClear(this.boxmaxs);
		Math3D.VectorClear(this.mins);
		Math3D.VectorClear(this.maxs);
		Math3D.VectorClear(this.mins2);
		Math3D.VectorClear(this.maxs2);
		this.start = this.end = null;
		this.trace.clear();
		this.passedict = null;
		this.contentmask = 0;
	}
}