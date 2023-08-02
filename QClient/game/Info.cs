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
namespace QClient.game;

using qcommon;
using System.Text;

public class Info
{
	/**
	 * Returns a value for a key from an info string. 
	 */
	public static string Info_ValueForKey(string s, string key)
	{
		var tk = s.Trim('\\').Split('\\');

		if (tk.Length == 1 && tk[0] == "")
			return "";

		for (var i = 0; i < tk.Length; i++)
		{
			var key1 = tk[i];

			if (i + 1 == tk.Length)
			{
				Com.Printf("MISSING VALUE\n");

				return s;
			}

			var value1 = tk[++i];

			if (key.Equals(key1))
				return value1;
		}

		return "";
	}

	/**
     * Sets a value for a key in the user info string.
     */
	public static string Info_SetValueForKey(string s, string key, string value)
	{
		if (value == null || value.Length == 0)
			return s;

		if (key.IndexOf('\\') != -1 || value.IndexOf('\\') != -1)
		{
			Com.Printf("Can't use keys or values with a \\\n");

			return s;
		}

		if (key.IndexOf(';') != -1)
		{
			Com.Printf("Can't use keys or values with a semicolon\n");

			return s;
		}

		if (key.IndexOf('"') != -1 || value.IndexOf('"') != -1)
		{
			Com.Printf("Can't use keys or values with a \"\n");

			return s;
		}

		if (key.Length > Defines.MAX_INFO_KEY - 1 || value.Length > Defines.MAX_INFO_KEY - 1)
		{
			Com.Printf("Keys and values must be < 64 characters.\n");

			return s;
		}

		StringBuilder sb = new(Info.Info_RemoveKey(s, key));

		if (sb.Length + 2 + key.Length + value.Length > Defines.MAX_INFO_STRING)
		{
			Com.Printf("Info string length exceeded\n");

			return s;
		}

		sb.Append('\\').Append(key).Append('\\').Append(value);

		return sb.ToString();
	}

	/** 
     * Removes a key and value from an info string. 
     */
	public static string Info_RemoveKey(string s, string key)
	{
		StringBuilder sb = new(512);

		if (key.IndexOf('\\') != -1)
		{
			Com.Printf("Can't use a key with a \\\n");

			return s;
		}

		var tk = s.Trim('\\').Split('\\');

		if (tk.Length == 1 && tk[0] == "")
			return sb.ToString();

		for (var i = 0; i < tk.Length; i++)
		{
			var key1 = tk[i];

			if (i + 1 == tk.Length)
			{
				Com.Printf("MISSING VALUE\n");

				return s;
			}

			var value1 = tk[++i];

			if (!key.Equals(key1))
				sb.Append('\\').Append(key1).Append('\\').Append(value1);
		}

		return sb.ToString();
	}

	/**
     * Some characters are illegal in info strings because they can mess up the
     * server's parsing.
     */
	public static bool Info_Validate(string s)
	{
		return !(s.IndexOf('"') != -1 || s.IndexOf(';') != -1);
	}

	private static readonly string fillspaces = "                     ";

	public static void Print(string s)
	{
		StringBuilder sb = new(512);
		var tk = s.Trim('\\').Split('\\');

		if (tk.Length == 1 && tk[0] == "")
		{
			Com.Printf(sb.ToString());

			return;
		}

		for (var i = 0; i < tk.Length; i++)
		{
			var key1 = tk[i];

			if (i + 1 == tk.Length)
			{
				Com.Printf("MISSING VALUE\n");

				return;
			}

			var value1 = tk[++i];

			sb.Append(key1);

			var len = key1.Length;

			if (len < 20)
				sb.Append(Info.fillspaces[len..]);

			sb.Append('=').Append(value1).Append('\n');
		}

		Com.Printf(sb.ToString());
	}
}