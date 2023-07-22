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
using Quake2Sharp.qcommon.types;
using Quake2Sharp.util;

namespace Quake2Sharp.qcommon;

/**
 * SZ
 */
public class SZ
{
	public static void Clear(sizebuf_t buf)
	{
		buf.clear();
	}

	//===========================================================================

	public static void Init(sizebuf_t buf, byte[] data, int length)
	{
		buf.readcount = 0;

		buf.data = data;
		buf.maxsize = length;
		buf.cursize = 0;
		buf.allowoverflow = buf.overflowed = false;
	}

	/** Ask for the pointer using sizebuf_t.cursize (RST) */
	public static int GetSpace(sizebuf_t buf, int length)
	{
		int oldsize;

		if (buf.cursize + length > buf.maxsize)
		{
			if (!buf.allowoverflow)
				clientserver.Com_Error(Defines.ERR_FATAL, "SZ_GetSpace: overflow without allowoverflow set");

			if (length > buf.maxsize)
				clientserver.Com_Error(Defines.ERR_FATAL, "SZ_GetSpace: " + length + " is > full buffer size");

			clientserver.Com_Printf("SZ_GetSpace: overflow\n");
			SZ.Clear(buf);
			buf.overflowed = true;
		}

		oldsize = buf.cursize;
		buf.cursize += length;

		return oldsize;
	}

	public static void Write(sizebuf_t buf, byte[] data, int length)
	{
		//memcpy(SZ_GetSpace(buf, length), data, length);
		Array.Copy(data, 0, buf.data, SZ.GetSpace(buf, length), length);
	}

	public static void Write(sizebuf_t buf, byte[] data, int offset, int length)
	{
		Array.Copy(data, offset, buf.data, SZ.GetSpace(buf, length), length);
	}

	public static void Write(sizebuf_t buf, byte[] data)
	{
		var length = data.Length;

		//memcpy(SZ_GetSpace(buf, length), data, length);
		Array.Copy(data, 0, buf.data, SZ.GetSpace(buf, length), length);
	}

	//
	public static void Print(sizebuf_t buf, string data)
	{
		Com.dprintln("SZ.print():<" + data + ">");
		var length = data.Length;
		var str = Lib.stringToBytes(data);

		if (buf.cursize != 0)
		{
			if (buf.data[buf.cursize - 1] != 0)
			{
				//memcpy( SZ_GetSpace(buf, len), data, len); // no trailing 0
				Array.Copy(str, 0, buf.data, SZ.GetSpace(buf, length + 1), length);
			}
			else
			{
				Array.Copy(str, 0, buf.data, SZ.GetSpace(buf, length) - 1, length);

				//memcpy(SZ_GetSpace(buf, len - 1) - 1, data, len); // write over trailing 0
			}
		}
		else

			// first print.
			Array.Copy(str, 0, buf.data, SZ.GetSpace(buf, length), length);

		//memcpy(SZ_GetSpace(buf, len), data, len);

		buf.data[buf.cursize - 1] = 0;
	}
}
