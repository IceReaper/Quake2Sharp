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
namespace Quake2Sharp.game
{
	using System;

	public abstract class EndianHandler
	{
		private static readonly int mask = 0xFF;
		public abstract float BigFloat(float f);
		public abstract short BigShort(short s);
		public abstract int BigLong(int i);
		public abstract float LittleFloat(float f);
		public abstract short LittleShort(short s);
		public abstract int LittleLong(int i);

		public static float swapFloat(float f)
		{
			var i = BitConverter.ToInt32(BitConverter.GetBytes(f), 0);
			i = EndianHandler.swapInt(i);
			f = BitConverter.ToSingle(BitConverter.GetBytes(i), 0);

			return f;
		}

		public static int swapInt(int i)
		{
			var a = i & EndianHandler.mask;
			i >>= 8;

			a <<= 24;

			var b = i & EndianHandler.mask;

			i >>= 8;
			b <<= 16;

			var c = i & EndianHandler.mask;
			i >>= 8;
			c <<= 8;

			return i | c | b | a;
		}

		public static short swapShort(short s)
		{
			var a = s & EndianHandler.mask;
			a <<= 8;
			var b = (s >> 8) & EndianHandler.mask;

			return (short)(b | a);
		}
	}
}
