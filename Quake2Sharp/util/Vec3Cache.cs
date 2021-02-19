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

namespace Quake2Sharp.util
{
	/**
 * Vec3Cache contains float[3] for temporary usage.
 * The usage can reduce the garbage at runtime.
 *
 * @author cwei
 */
	public sealed class Vec3Cache
	{
		private static readonly float[][] cache = new float[64][];
		private static int index;
		private static readonly int max = 0;

		static Vec3Cache()
		{
			for (var n = 0; n < Vec3Cache.cache.Length; n++)
				Vec3Cache.cache[n] = new float[3];
		}

		public static float[] get()
		{
			//max = Math.max(index, max);
			return Vec3Cache.cache[Vec3Cache.index++];
		}

		public static void release()
		{
			Vec3Cache.index--;
		}

		public static void release(int count)
		{
			Vec3Cache.index -= count;
		}

		public static void debug()
		{
			System.Console.Error.WriteLine("Vec3Cache: max. " + (Vec3Cache.max + 1) + " vectors used.");
		}
	}
}
