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

public class explosion_t
{
	public int type;
	public readonly entity_t ent = new();
	public int frames;
	public float light;
	public readonly float[] lightcolor = new float[3];
	public float start;
	public int baseframe;

	public void clear()
	{
		this.lightcolor[0] = this.lightcolor[1] = this.lightcolor[2] = this.light = this.start = this.type = this.frames = this.baseframe = 0;
		this.ent.clear();
	}
}