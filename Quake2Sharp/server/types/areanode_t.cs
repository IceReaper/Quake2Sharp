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
namespace Quake2Sharp.server.types
{
	using game.types;

	public class areanode_t
	{
		public int axis; // -1 = leaf node
		public float dist;
		public areanode_t[] children = new areanode_t[2];
		public link_t trigger_edicts;
		public link_t solid_edicts;

		// used for debugging
		//	float mins_rst[] = {0,0,0};
		//	float maxs_rst[] = {0,0,0};
		public areanode_t()
		{
			this.trigger_edicts = new(this);
			this.solid_edicts = new(this);
		}
	}
}
