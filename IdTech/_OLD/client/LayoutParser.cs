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

using IdTech.common;

namespace Quake2Sharp.client;

public class LayoutParser
{
	private int tokenPos;
	private int tokenLength;
	private int index;
	private int length;
	private string data;

	public LayoutParser()
	{
		this.init(null);
	}

	public void init(string layout)
	{
		this.tokenPos = 0;
		this.tokenLength = 0;
		this.index = 0;
		this.data = layout ?? "";
		this.length = layout?.Length ?? 0;
	}

	public bool hasNext()
	{
		return !this.isEof();
	}

	public void next()
	{
		if (this.data == null)
		{
			this.tokenLength = 0;

			return;
		}

		while (true)
		{
			// skip whitespace
			this.skipwhites();

			if (this.isEof())
			{
				this.tokenLength = 0;

				return;
			}

			// skip // comments
			if (this.getchar() == '/')
			{
				if (this.nextchar() == '/')
				{
					this.skiptoeol();

					// goto skip whitespace
					continue;
				}

				this.prevchar();

				break;
			}

			break;
		}

		int c;
		var len = 0;

		// handle quoted strings specially
		if (this.getchar() == '\"')
		{
			this.nextchar();
			this.tokenPos = this.index;

			while (true)
			{
				c = this.getchar();
				this.nextchar();

				if (c == '\"' || c == 0)
				{
					this.tokenLength = len;

					return;
				}

				if (len < Defines.MAX_TOKEN_CHARS)
					++len;
			}
		}

		// parse a regular word
		c = this.getchar();
		this.tokenPos = this.index;

		do
		{
			if (len < Defines.MAX_TOKEN_CHARS)
				++len;

			c = this.nextchar();
		}
		while (c > 32);

		if (len == Defines.MAX_TOKEN_CHARS)
		{
			clientserver.Com_Printf("Token exceeded " + Defines.MAX_TOKEN_CHARS + " chars, discarded.\n");
			len = 0;
		}

		this.tokenLength = len;

		return;
	}

	public bool tokenEquals(string other)
	{
		if (this.tokenLength != other.Length)
			return false;

		return this.data.Substring(this.tokenPos, this.tokenLength) == other[..this.tokenLength];
	}

	public int tokenAsInt()
	{
		if (this.tokenLength == 0)
			return 0;

		return this.atoi();
	}

	public string token()
	{
		if (this.tokenLength == 0)
			return "";

		return this.data.Substring(this.tokenPos, this.tokenLength);
	}

	private int atoi()
	{
		var result = 0;
		var negative = false;
		int i = 0, max = this.tokenLength;
		var s = this.data;
		int limit;
		int multmin;
		int digit;

		if (max > 0)
		{
			if (s[this.tokenPos] == '-')
			{
				negative = true;
				limit = int.MinValue;
				i++;
			}
			else
				limit = -int.MaxValue;

			multmin = limit / 10;

			if (i < max)
			{
				digit = s[this.tokenPos + i++] - '0';

				if (digit < 0)
					return 0; // wrong format

				result = -digit;
			}

			while (i < max)
			{
				// Accumulating negatively avoids surprises near MAX_VALUE
				digit = s[this.tokenPos + i++] - '0';

				if (digit < 0)
					return 0; // wrong format

				if (result < multmin)
					return 0; // wrong format

				result *= 10;

				if (result < limit + digit)
					return 0; // wrong format

				result -= digit;
			}
		}
		else
			return 0; // wrong format

		if (negative)
		{
			if (i > 1)
				return result;

			/* Only got "-" */
			return 0; // wrong format
		}

		return -result;
	}

	private char getchar()
	{
		if (this.index < this.length)
			return this.data[this.index];

		return (char)0;
	}

	private char nextchar()
	{
		++this.index;

		if (this.index < this.length)
			return this.data[this.index];

		return (char)0;
	}

	private char prevchar()
	{
		if (this.index > 0)
		{
			--this.index;

			return this.data[this.index];
		}

		return (char)0;
	}

	private bool isEof()
	{
		return this.index >= this.length;
	}

	private char skipwhites()
	{
		var c = (char)0;

		while (this.index < this.length && (c = this.data[this.index]) <= ' ' && c != 0)
			++this.index;

		return c;
	}

	private char skiptoeol()
	{
		var c = (char)0;

		while (this.index < this.length && (c = this.data[this.index]) != '\n' && c != 0)
			++this.index;

		return c;
	}
}
