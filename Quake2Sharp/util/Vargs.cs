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
	using System.Collections.Generic;
	using System.Linq;

	/**
 * Vargs is a helper class to encapsulate printf arguments.
 * 
 * @author cwei
 */
	public class Vargs
	{
		// initial capacity
		private static readonly int SIZE = 5;

		private readonly List<object> v;

		public Vargs()
			: this(Vargs.SIZE)
		{
		}

		public Vargs(int initialSize)
		{
			if (this.v != null)
				this.v.Clear(); // clear previous list for GC

			this.v = new List<object>();
		}

		public Vargs add(bool value)
		{
			this.v.Add(value);

			return this;
		}

		public Vargs add(byte value)
		{
			this.v.Add(value);

			return this;
		}

		public Vargs add(char value)
		{
			this.v.Add(value);

			return this;
		}

		public Vargs add(short value)
		{
			this.v.Add(value);

			return this;
		}

		public Vargs add(int value)
		{
			this.v.Add(value);

			return this;
		}

		public Vargs add(long value)
		{
			this.v.Add(value);

			return this;
		}

		public Vargs add(float value)
		{
			this.v.Add(value);

			return this;
		}

		public Vargs add(double value)
		{
			this.v.Add(value);

			return this;
		}

		public Vargs add(string value)
		{
			this.v.Add(value);

			return this;
		}

		public Vargs add(object value)
		{
			this.v.Add(value);

			return this;
		}

		public Vargs clear()
		{
			this.v.Clear();

			return this;
		}

		public List<object> toVector()
		{
			//		Vector tmp = v;
			//		v = null;
			//		return tmp;
			return this.v.ToList();
		}

		public object[] toArray()
		{
			return this.v.ToArray();
		}

		public int size()
		{
			return this.v.Count;
		}
	}
}
