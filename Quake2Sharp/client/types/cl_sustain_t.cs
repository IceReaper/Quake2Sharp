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
namespace Quake2Sharp.client.types
{
	using System;

	/**
 * cl_sustain_t
 */
	public class cl_sustain_t
	{
		public int id;
		private int type;
		public int endtime;
		public int nextthink;
		public int thinkinterval;
		public float[] org = new float[3];
		public float[] dir = new float[3];
		public int color;
		public int count;
		public int magnitude;
		public Action<cl_sustain_t> think;

		public void clear()
		{
			this.org[0] = this.org[1] = this.org[2] = this.dir[0] = this.dir[1] = this.dir[2] =
				this.id = this.type = this.endtime = this.nextthink = this.thinkinterval = this.color = this.count = this.magnitude = 0;

			this.think = null;
		}
	}
}
