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
namespace Quake2Sharp.render.opengl
{
	using render.types;

	public class Polygon : glpoly_t
	{
		private static readonly int MAXPOLYS = 20000;
		private static readonly int MAX_BUFFER_VERTICES = 120000;
		private static readonly float[] buffer = new float[Polygon.MAX_BUFFER_VERTICES * glpoly_t.STRIDE];
		private static int bufferIndex;
		private static int polyCount;
		private static readonly Polygon[] polyCache = new Polygon[Polygon.MAXPOLYS];

		static Polygon()
		{
			for (var i = 0; i < Polygon.polyCache.Length; i++)
				Polygon.polyCache[i] = new();
		}

		public static glpoly_t create(int numverts)
		{
			var poly = Polygon.polyCache[Polygon.polyCount++];
			poly.clear();
			poly.numverts = numverts;
			poly.pos = Polygon.bufferIndex;
			Polygon.bufferIndex += numverts;

			return poly;
		}

		public static void reset()
		{
			Polygon.polyCount = 0;
			Polygon.bufferIndex = 0;
		}

		private Polygon()
		{
		}

		private void clear()
		{
			this.next = null;
			this.chain = null;
			this.numverts = 0;
			this.flags = 0;
		}

		public override float x(int index)
		{
			return Polygon.buffer[this.offset(index)];
		}

		public override void x(int index, float value)
		{
			Polygon.buffer[this.offset(index)] = value;
		}

		public override float y(int index)
		{
			return Polygon.buffer[this.offset(index) + 1];
		}

		public override void y(int index, float value)
		{
			Polygon.buffer[this.offset(index) + 1] = value;
		}

		public override float z(int index)
		{
			return Polygon.buffer[this.offset(index) + 2];
		}

		public override void z(int index, float value)
		{
			Polygon.buffer[this.offset(index) + 2] = value;
		}

		public override float s1(int index)
		{
			return Polygon.buffer[this.offset(index) + 3];
		}

		public override void s1(int index, float value)
		{
			Polygon.buffer[this.offset(index) + 3] = value;
		}

		public override float t1(int index)
		{
			return Polygon.buffer[this.offset(index) + 4];
		}

		public override void t1(int index, float value)
		{
			Polygon.buffer[this.offset(index) + 4] = value;
		}

		public override float s2(int index)
		{
			return Polygon.buffer[this.offset(index) + 5];
		}

		public override void s2(int index, float value)
		{
			Polygon.buffer[this.offset(index) + 5] = value;
		}

		public override float t2(int index)
		{
			return Polygon.buffer[this.offset(index) + 6];
		}

		public override void t2(int index, float value)
		{
			Polygon.buffer[this.offset(index) + 6] = value;
		}

		public override void beginScrolling(float value)
		{
			// not in use
		}

		public override void endScrolling()
		{
			// not in use
		}

		private int offset(int index)
		{
			return (index + this.pos) * glpoly_t.STRIDE;
		}
	}
}
