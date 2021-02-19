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

namespace Quake2Sharp.client
{
	using game;

	public class CentityT
	{
		public readonly entity_state_t Baseline = new(null); // delta from this if not from a previous frame
		public readonly entity_state_t Current = new(null);
		public readonly entity_state_t Prev = new(null); // will always be valid, but might just be a copy of current

		public int Serverframe; // if not current, this ent isn't in the frame

		public int Trailcount; // for diminishing grenade trails
		public readonly float[] LerpOrigin = {0, 0, 0}; // for trails (variable hz)

		public int FlyStoptime;
	}
}
