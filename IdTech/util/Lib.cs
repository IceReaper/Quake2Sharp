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
namespace Quake2Sharp.util;

using qcommon;
using System.Globalization;
using System.Text;

public class Lib
{
	/** Converts a vector to a string. */
	public static string vtos(float[] v)
	{
		return (int)v[0] + " " + (int)v[1] + " " + (int)v[2];
	}

	/** Converts a vector to a beatiful string. */
	public static string vtofsbeaty(float[] v)
	{
		return $"{v[0]:0.00,8} {v[1]:0.00,8} {v[2]:0.00,8}";
	}

	/** Like in  libc. */
	public static short rand()
	{
		return (short)Globals.rnd.Next(short.MaxValue + 1);
	}

	/** Like in libc. */
	public static float crandom()
	{
		return ((float)Globals.rnd.NextDouble() - 0.5f) * 2.0f;
	}

	/** Like in libc. */
	public static float random()
	{
		return (float)Globals.rnd.NextDouble();
	}

	/** Like in libc. */
	public static float crand()
	{
		return ((float)Globals.rnd.NextDouble() - 0.5f) * 2.0f;
	}

	/** Like in libc. */
	public static int strcmp(string in1, string in2)
	{
		return in1.CompareTo(in2);
	}

	/** Like in libc. */
	public static float atof(string @in)
	{
		if (@in == null || @in.Length == 0)
			return 0;

		try
		{
			return (float)double.Parse(@in, CultureInfo.InvariantCulture);
		}
		catch (Exception)
		{
			return 0;
		}
	}

	/** Like in quake2. */
	public static int Q_stricmp(string in1, string in2)
	{
		return StringComparer.OrdinalIgnoreCase.Compare(in1, in2);
	}

	/** Like in libc. */
	public static int atoi(string @in)
	{
		if (@in == null || @in.Length == 0)
			return 0;

		try
		{
			return (int)double.Parse(@in, CultureInfo.InvariantCulture);
		}
		catch (Exception)
		{
			return 0;
		}
	}

	/** Converts a string to a vector. Needs improvement. */
	public static float[] atov(string v)
	{
		float[] res = { 0, 0, 0 };
		var strres = v.Split(" ");

		for (var n = 0; n < 3 && n < strres.Length; n++)
			res[n] = Lib.atof(strres[n]);

		return res;
	}

	/** Like in libc. */
	public static int strlen(char[] @in)
	{
		for (var i = 0; i < @in.Length; i++)
		{
			if (@in[i] == 0)
				return i;
		}

		return @in.Length;
	}

	/** Like in libc. */
	public static int strlen(byte[] @in)
	{
		for (var i = 0; i < @in.Length; i++)
		{
			if (@in[i] == 0)
				return i;
		}

		return @in.Length;
	}

	/** Converts memory to a memory dump string. */
	public static string hexDump(byte[] data1, int len, bool showAddress)
	{
		StringBuilder result = new();
		StringBuilder charfield = new();
		var i = 0;

		while (i < len)
		{
			if ((i & 0xf) == 0)
			{
				if (showAddress)
				{
					var address = i.ToString("x");
					address = ("0000"[..(4 - address.Length)] + address).ToUpper();
					result.Append(address + ": ");
				}
			}

			int v = data1[i];

			result.Append(Lib.hex2(v));
			result.Append(" ");

			charfield.Append(Lib.readableChar(v));
			i++;

			// nach dem letzten, newline einfuegen
			if ((i & 0xf) == 0)
			{
				result.Append(charfield);
				result.Append("\n");
				charfield.Length = 0;
			}

			//	in der Mitte ein Luecke einfuegen ?
			else if ((i & 0xf) == 8)
				result.Append(" ");
		}

		return result.ToString();
	}

	/** Formats an hex byte. */
	public static string hex2(int i)
	{
		var val = (i & 0xff).ToString("x");

		return ("00"[..(2 - val.Length)] + val).ToUpper();
	}

	/** Returns true if the char is alphanumeric. */
	public static char readableChar(int i)
	{
		if (i < 0x20 || i > 0x7f)
			return '.';

		return (char)i;
	}

	/** Prints a vector to the quake console. */
	public static void printv(string @in, float[] arr)
	{
		for (var n = 0; n < arr.Length; n++)
			Com.Println(@in + "[" + n + "]: " + arr[n]);
	}

	/** Returns the right part of the string from the last occruence of c. */
	public static string rightFrom(string @in, char c)
	{
		var pos = @in.LastIndexOf(c);

		if (pos == -1)
			return "";

		if (pos < @in.Length)
			return @in[(pos + 1)..];

		return "";
	}

	/** Returns the left part of the string from the last occruence of c. */
	public static string leftFrom(string @in, char c)
	{
		var pos = @in.LastIndexOf(c);

		if (pos == -1)
			return "";

		if (pos < @in.Length)
			return @in[..pos];

		return "";
	}

	/** Converts an int to 4 bytes representation. */
	public static byte[] getIntBytes(int c)
	{
		var b = new byte[4];
		b[0] = (byte)(c & 0xff);
		b[1] = (byte)(((uint)c >> 8) & 0xff);
		b[2] = (byte)(((uint)c >> 16) & 0xff);
		b[3] = (byte)(((uint)c >> 24) & 0xff);

		return b;
	}

	/** Duplicates a float array. */
	public static float[] clone(float[] @in)
	{
		var @out = new float[@in.Length];

		if (@in.Length != 0)
			Array.Copy(@in, 0, @out, 0, @in.Length);

		return @out;
	}

	/** 
     * convert a string to byte[] with 8bit latin 1
     * 
     * avoid String.getBytes() because it is using system specific character encoding.
     */
	public static byte[] stringToBytes(string value)
	{
		try
		{
			return Encoding.Latin1.GetBytes(value);
		}
		catch (Exception)
		{
			// can't happen: Latin 1 is a standard encoding
			return null;
		}
	}

	/** 
     * convert a byte[] with 8bit latin 1 to string
     * 
     * avoid new String(bytes) because it is using system specific character encoding.
     */
	public static string bytesToString(byte[] value)
	{
		try
		{
			return Encoding.Latin1.GetString(value).Split((char)0)[0];
		}
		catch (Exception)
		{
			// can't happen: Latin 1 is a standard encoding
			return null;
		}
	}

	/** Helper method that savely handles the null termination of old C String data. */
	public static string CtoJava(byte[] old)
	{
		return Lib.CtoJava(old, 0, old.Length);
	}

	/** Helper method that savely handles the null termination of old C String data. */
	public static string CtoJava(byte[] old, int offset, int maxLenght)
	{
		if (old.Length == 0 || old[0] == 0)
			return "";

		int i;

		for (i = offset; i - offset < maxLenght && old[i] != 0; i++)
			;

		return Encoding.ASCII.GetString(old, offset, i - offset);
	}
}