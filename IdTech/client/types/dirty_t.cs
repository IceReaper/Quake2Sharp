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

public class dirty_t
{
	public int x1;
	public int x2;
	public int y1;
	public int y2;

	public void set(dirty_t src)
	{
		this.x1 = src.x1;
		this.x2 = src.x2;
		this.y1 = src.y1;
		this.y2 = src.y2;
	}

	public void clear()
	{
		this.x1 = this.x2 = this.y1 = this.y2 = 0;
	}
}