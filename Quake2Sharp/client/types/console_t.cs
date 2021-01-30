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
	/**
 * console_t
 */
	public class console_t
	{
		public bool initialized;
		public byte[] text = new byte[Defines.CON_TEXTSIZE];
		public int current; // line where next message will be printed
		public int x; // offset in current line for next print
		public int display; // bottom of console displays this line
		public int ormask; // high bit mask for colored characters
		public int linewidth; // characters across screen
		public int totallines; // total lines in console scrollback
		public int vislines;
		public float[] times = new float[Defines.NUM_CON_TIMES]; // cls.realtime time the line was generated

		// for transparent notify lines	
	}
}
