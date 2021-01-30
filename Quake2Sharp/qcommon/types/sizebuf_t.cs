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
namespace Quake2Sharp.qcommon.types
{
	using System;

	/**
 * sizebuf_t
 */
	public class sizebuf_t
	{
		public bool allowoverflow = false;
		public bool overflowed;
		public byte[] data = null;
		public int maxsize = 0;
		public int cursize;
		public int readcount = 0;

		public void clear()
		{
			if (this.data != null)
				Array.Fill(this.data, (byte)0);

			this.cursize = 0;
			this.overflowed = false;
		}
	}
}
