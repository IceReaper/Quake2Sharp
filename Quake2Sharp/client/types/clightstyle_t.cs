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

public class clightstyle_t
{
	public int length;
	public readonly float[] value = new float[3];
	public readonly float[] map = new float[Defines.MAX_QPATH];

	public void clear()
	{
		this.value[0] = this.value[1] = this.value[2] = this.length = 0;

		for (var i = 0; i < this.map.Length; i++)
			this.map[i] = 0.0f;
	}
}