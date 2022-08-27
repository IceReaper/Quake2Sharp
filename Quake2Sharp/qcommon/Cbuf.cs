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
namespace Quake2Sharp.qcommon;

using game;
using System.Text;
using util;

/**
 * Cbuf
 */
public class Cbuf
{
	private static readonly byte[] line = new byte[1024];
	private static readonly byte[] tmp = new byte[8192];

	/**
     *  
     */
	public static void Init()
	{
		SZ.Init(Globals.cmd_text, Globals.cmd_text_buf, Globals.cmd_text_buf.Length);
	}

	public static void InsertText(string text)
	{
		var templen = 0;

		// copy off any commands still remaining in the exec buffer
		templen = Globals.cmd_text.cursize;

		if (templen != 0)
		{
			Array.Copy(Globals.cmd_text.data, 0, Cbuf.tmp, 0, templen);
			SZ.Clear(Globals.cmd_text);
		}

		// add the entire text of the file
		Cbuf.AddText(text);

		// add the copied off data
		if (templen != 0)
			SZ.Write(Globals.cmd_text, Cbuf.tmp, templen);
	}

	/**
     * @param clear
     */
	public static void AddEarlyCommands(bool clear)
	{
		for (var i = 0; i < Com.Argc(); i++)
		{
			var s = Com.Argv(i);

			if (!s.Equals("+set"))
				continue;

			Cbuf.AddText("set " + Com.Argv(i + 1) + " " + Com.Argv(i + 2) + "\n");

			if (clear)
			{
				Com.ClearArgv(i);
				Com.ClearArgv(i + 1);
				Com.ClearArgv(i + 2);
			}

			i += 2;
		}
	}

	/**
     * @return
     */
	public static bool AddLateCommands()
	{
		int i;
		int j;
		var ret = false;

		// build the combined string to parse from
		var s = 0;
		var argc = Com.Argc();

		for (i = 1; i < argc; i++)
			s += Com.Argv(i).Length;

		if (s == 0)
			return false;

		var text = "";

		for (i = 1; i < argc; i++)
		{
			text += Com.Argv(i);

			if (i != argc - 1)
				text += " ";
		}

		// pull out the commands
		var build = "";

		for (i = 0; i < text.Length; i++)
		{
			if (text[i] == '+')
			{
				i++;

				for (j = i; j < text.Length && text[j] != '+' && text[j] != '-'; j++)
					;

				build += text.Substring(i, j - i);
				build += "\n";

				i = j - 1;
			}
		}

		ret = build.Length != 0;

		if (ret)
			Cbuf.AddText(build);

		text = null;
		build = null;

		return ret;
	}

	/**
     * @param text
     */
	public static void AddText(string text)
	{
		var l = text.Length;

		if (Globals.cmd_text.cursize + l >= Globals.cmd_text.maxsize)
		{
			Com.Printf("Cbuf_AddText: overflow\n");

			return;
		}

		SZ.Write(Globals.cmd_text, Lib.stringToBytes(text), l);
	}

	/**
     *  
     */
	public static void Execute()
	{
		byte[] text = null;

		Globals.alias_count = 0; // don't allow infinite alias loops

		while (Globals.cmd_text.cursize != 0)
		{
			// find a \n or ; line break
			text = Globals.cmd_text.data;

			var quotes = 0;
			int i;

			for (i = 0; i < Globals.cmd_text.cursize; i++)
			{
				if (text[i] == '"')
					quotes++;

				if (!(quotes % 2 != 0) && text[i] == ';')
					break; // don't break if inside a quoted string

				if (text[i] == '\n')
					break;
			}

			Array.Copy(text, 0, Cbuf.line, 0, i);
			Cbuf.line[i] = 0;

			// delete the text from the command buffer and move remaining
			// commands down
			// this is necessary because commands (exec, alias) can insert data
			// at the
			// beginning of the text buffer

			if (i == Globals.cmd_text.cursize)
				Globals.cmd_text.cursize = 0;
			else
			{
				i++;
				Globals.cmd_text.cursize -= i;

				//byte[] tmp = new byte[Globals.cmd_text.cursize];

				Array.Copy(text, i, Cbuf.tmp, 0, Globals.cmd_text.cursize);
				Array.Copy(Cbuf.tmp, 0, text, 0, Globals.cmd_text.cursize);
				text[Globals.cmd_text.cursize] = (byte)'\0';
			}

			// execute the command line
			var len = Lib.strlen(Cbuf.line);

			var cmd = Encoding.ASCII.GetString(Cbuf.line, 0, len);
			Cmd.ExecuteString(cmd);

			if (Globals.cmd_wait)
			{
				// skip out while text still remains in buffer, leaving it
				// for next frame
				Globals.cmd_wait = false;

				break;
			}
		}
	}

	public static void ExecuteText(int exec_when, string text)
	{
		switch (exec_when)
		{
			case Defines.EXEC_NOW:
				Cmd.ExecuteString(text);

				break;

			case Defines.EXEC_INSERT:
				Cbuf.InsertText(text);

				break;

			case Defines.EXEC_APPEND:
				Cbuf.AddText(text);

				break;

			default:
				Com.Error(Defines.ERR_FATAL, "Cbuf_ExecuteText: bad exec_when");

				break;
		}
	}

	/*
	 * ============ Cbuf_CopyToDefer ============
	 */
	public static void CopyToDefer()
	{
		Array.Copy(Globals.cmd_text_buf, 0, Globals.defer_text_buf, 0, Globals.cmd_text.cursize);
		Globals.defer_text_buf[Globals.cmd_text.cursize] = 0;
		Globals.cmd_text.cursize = 0;
	}

	/*
	 * ============ Cbuf_InsertFromDefer ============
	 */
	public static void InsertFromDefer()
	{
		Cbuf.InsertText(Encoding.ASCII.GetString(Globals.defer_text_buf).Split((char)0)[0]);
		Globals.defer_text_buf[0] = 0;
	}
}