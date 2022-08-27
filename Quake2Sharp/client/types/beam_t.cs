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
namespace Quake2Sharp.client.types;

using render.types;

public class beam_t
{
	public int entity;
	public int dest_entity;
	public model_t model;
	public int endtime;
	public readonly float[] offset = new float[3];
	public readonly float[] start = new float[3];
	public readonly float[] end = new float[3];

	public void clear()
	{
		this.offset[0] = this.offset[1] = this.offset[2] = this.start[0] =
			this.start[1] = this.start[2] = this.end[0] = this.end[1] = this.end[2] = this.entity = this.dest_entity = this.endtime = 0;

		this.model = null;
	}
}