/*
 * Copyright (C) 1997-2001 Id Software, Inc.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or (at
 * your option) any later version.
 *
 * This program is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 *
 * See the GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA
 * 02111-1307, USA.
 */

using Quake2Sharp;
using Quake2Sharp.game;
using Quake2Sharp.qcommon;
using System.Text;
using Timer = Quake2Sharp.sys.Timer;

namespace IdTech.common;

/*
 * =======================================================================
 *
 * This file implements the Quake II command processor. Every command
 * which is send via the command line at startup, via the console and
 * via rcon is processed here and send to the appropriate subsystem.
 *
 * =======================================================================
 */
public static class cmdparser
{
	private const int AliasLoopCount = 16;

	private static readonly Dictionary<string, Action?> cmd_functions = new(); /* possible commands to execute */
	private static int alias_count; /* for detecting runaway loops */
	private static readonly Dictionary<string, string> cmd_alias = new();
	private static int cmd_wait;
	private static readonly List<string> cmd_argv = new();
	private static string cmd_text_buf = string.Empty;
	private static string defer_text_buf = string.Empty;

	/*
	 * Causes execution of the remainder of the command buffer to be delayed
	 * until next frame.  This allows commands like: bind g "impulse 5 ;
	 * +attack ; wait ; -attack ; impulse 2"
	 */
	public static void Cmd_Wait_f()
	{
		cmd_wait = Timer.Sys_Milliseconds();
	}

	public static void Cbuf_Init()
	{
		cmd_text_buf = string.Empty;
	}

	/*
	 * Adds command text at the end of the buffer
	 */
	public static void Cbuf_AddText(string text)
	{
		cmd_text_buf += text;
	}

	/*
	 * Adds command text immediately after the current command
	 * Adds a \n to the text
	 */
	public static void Cbuf_InsertText(string text)
	{
		cmd_text_buf = text + cmd_text_buf;
	}

	public static void Cbuf_CopyToDefer()
	{
		defer_text_buf = cmd_text_buf;
		cmd_text_buf = string.Empty;
	}

	public static void Cbuf_InsertFromDefer()
	{
		cmd_text_buf += defer_text_buf;
		defer_text_buf = string.Empty;
	}

	public static void Cbuf_ExecuteText(int exec_when, string text)
	{
		switch (exec_when)
		{
			case Defines.EXEC_NOW:
				Cmd_ExecuteString(text);
				break;

			case Defines.EXEC_INSERT:
				Cbuf_InsertText(text);
				break;

			case Defines.EXEC_APPEND:
				Cbuf_AddText(text);
				break;

			default:
				clientserver.Com_Error(Defines.ERR_FATAL, "Cbuf_ExecuteText: bad exec_when");
				break;
		}
	}

	public static void Cbuf_Execute()
	{
		if (cmd_wait > 0)
		{
			// make sure that "wait" in scripts waits for ~16.66ms (1 frame at 60fps)
			// regardless of framerate
			if (Timer.Sys_Milliseconds() - cmd_wait <= 16)
				return;

			cmd_wait = 0;
		}

		alias_count = 0; /* don't allow infinite alias loops */

		while (cmd_text_buf.Length > 0)
		{
			/* find a \n or ; line break */
			var quotes = false;
			var i = 0;

			for (; i < cmd_text_buf.Length; i++)
			{
				if (cmd_text_buf[i] == '"')
					quotes = !quotes;

				if (!quotes && (cmd_text_buf[i] == ';' || cmd_text_buf[i] == '\n'))
					break; /* don't break if inside a quoted string */
			}

			/* delete the text from the command buffer and move remaining
			   commands down this is necessary because commands (exec,
			   alias) can insert data at the beginning of the text buffer */
			var line = cmd_text_buf[..i];
			cmd_text_buf = i == cmd_text_buf.Length ? string.Empty : cmd_text_buf[(i + 1)..];

			/* execute the command line */
			Cmd_ExecuteString(line);

			/* skip out while text still remains in buffer,
			   leaving it for after we're done waiting */
			if (cmd_wait > 0)
				break;
		}
	}

	/*
	 * Adds command line parameters as script statements Commands lead with
	 * a +, and continue until another +
	 *
	 * Set commands are added early, so they are guaranteed to be set before
	 * the client and server initialize for the first time.
	 *
	 * Other commands are added late, after all initialization is complete.
	 */
	public static void Cbuf_AddEarlyCommands(bool clear)
	{
		for (var i = 0; i < argproc.COM_Argc(); i++)
		{
			var argument = argproc.COM_Argv(i);

			if (argument != "+set")
				continue;

			Cbuf_AddText($"set {argproc.COM_Argv(++i)} {argproc.COM_Argv(++i)}\n");

			if (!clear)
				continue;

			argproc.COM_ClearArgv(i--);
			argproc.COM_ClearArgv(i--);
			argproc.COM_ClearArgv(i--);
		}
	}

	/*
	 * Adds command line parameters as script statements
	 * Commands lead with a + and continue until another + or -
	 * quake +developer 1 +map amlev1
	 *
	 * Returns true if any late commands were added, which
	 * will keep the demoloop from immediately starting
	 */
	public static bool Cbuf_AddLateCommands()
	{
		/* build the combined string to parse from */
		var text = string.Empty;

		for (var i = 1; i < argproc.COM_Argc(); i++)
		{
			if (i > 1)
				text += " ";

			text += argproc.COM_Argv(i);
		}

		/* pull out the commands */
		var found = false;

		for (var i = 0; i < text.Length; i++)
		{
			if (text[i] != '+')
				continue;

			i++;

			var j = i;

			while (j < text.Length && text[j] != '+' && !(text[j] == '-' && text[j - 1] == ' '))
				j++;

			Cbuf_AddText($"{text.Substring(i, j - i)}\n");

			found = true;

			i = j - 1;
		}

		return found;
	}

	/*
	 * Execute a script file
	 */
	private static void Cmd_Exec_f()
	{
		if (Cmd_Argc() != 2)
		{
			clientserver.Com_Printf("exec <filename> : execute a script file\n");
			return;
		}

		var data = filesystem.FS_LoadFile(Cmd_Argv(1));

		if (data == null)
		{
			clientserver.Com_Printf($"couldn't exec {Cmd_Argv(1)}\n");
			return;
		}

		// make sure last line has a newline
		clientserver.Com_Printf($"execing {Cmd_Argv(1)}.\n");

		Cbuf_InsertText($"{Encoding.UTF8.GetString(data)}\n");
	}

	/*
	 * Inserts the current value of a variable as command text
	 */
	private static void Cmd_Vstr_f()
	{
		if (Cmd_Argc() != 2)
		{
			clientserver.Com_Printf("vstr <variablename> : execute a variable command\n");
			return;
		}

		Cbuf_InsertText($"{cvar.Cvar_VariableString(Cmd_Argv(1))}\n");
	}

	/*
	 * Just prints the rest of the line to the console
	 */
	private static void Cmd_Echo_f()
	{
		clientserver.Com_Printf($"{Cmd_Args()}\n");
	}

	/*
	 * Creates a new command that executes
	 * a command string (possibly ; seperated)
	 */
	private static void Cmd_Alias_f()
	{
		if (Cmd_Argc() == 1)
		{
			clientserver.Com_Printf("Current alias commands:\n");

			foreach (var alias in cmd_alias)
				clientserver.Com_Printf($"{alias.Key} : {alias.Value}\n");

			return;
		}

		/* copy the rest of the command line */
		var value = string.Empty;

		for (var i = 2; i < Cmd_Argc(); i++)
		{
			if (i > 2)
				value += " ";

			value += Cmd_Argv(i);
		}

		/* if the alias already exists, reuse it */
		cmd_alias[Cmd_Argv(1)] = $"{value}\n";
	}

	public static int Cmd_Argc()
	{
		return cmd_argv.Count;
	}

	public static string Cmd_Argv(int argument)
	{
		return argument >= cmd_argv.Count ? string.Empty : cmd_argv[argument];
	}

	/*
	 * Returns a single string containing argv(1) to argv(argc()-1)
	 */
	public static string Cmd_Args()
	{
		return string.Join(' ', cmd_argv.Skip(1));
	}

	private static string Cmd_MacroExpandString(string text)
	{
		var inquote = false;
		var count = 0;

		for (var i = 0; i < text.Length; i++)
		{
			if (text[i] == '"')
				inquote = !inquote;

			/* don't expand inside quotes */
			if (inquote)
				continue;

			if (text[i] != '$')
				continue;

			/* scan out the complete macro */
			var remaining = text[(i + 1)..];
			var ph = new Com.ParseHelp(remaining);
			var token = Com.Parse(ph);
			remaining = ph.data == null ? string.Empty : new string(ph.data)[ph.index..];

			text = text[..i] + cvar.Cvar_VariableString(token) + remaining;
			i--;

			if (++count != 100)
				continue;

			clientserver.Com_Printf("Macro expansion loop, discarded.\n");
			return string.Empty;
		}

		if (!inquote)
			return text;

		clientserver.Com_Printf("Line has unmatched quote, discarded.\n");
		return string.Empty;
	}

	/*
	 * Parses the given string into command line tokens.
	 * $Cvars will be expanded unless they are in a quoted token
	 */
	public static void Cmd_TokenizeString(string text, bool macroExpand)
	{
		/* clear the args from the last string */
		cmd_argv.Clear();

		/* macro expand the text */
		if (macroExpand)
			text = Cmd_MacroExpandString(text);

		while (true)
		{
			var i = 0;

			/* skip whitespace up to a \n */
			while (i < text.Length && text[i] <= ' ' && text[i] != '\n')
				i++;

			/* a newline separates commands in the buffer */
			if (i < text.Length && text[i] == '\n')
			{
				text = text[(i + 1)..];
				break;
			}

			if (i == text.Length)
			{
				text = string.Empty;
				break;
			}

			text = text[i..];

			var ph = new Com.ParseHelp(text);
			var com_token = Com.Parse(ph);
			text = ph.data == null ? string.Empty : new string(ph.data)[ph.index..];

			if (com_token.Length == 0)
				return;

			cmd_argv.Add(com_token);
		}
	}

	public static void Cmd_AddCommand(string cmd_name, Action function)
	{
		/* fail if the command is a variable name */
		if (cvar.Cvar_VariableString(cmd_name).Length > 0)
			Cmd_RemoveCommand(cmd_name);

		/* fail if the command already exists */
		if (cmd_functions.ContainsKey(cmd_name))
		{
			clientserver.Com_Printf($"Cmd_AddCommand: {cmd_name} already defined\n");
			return;
		}

		/* link the command in */
		cmd_functions.Add(cmd_name, function);
	}

	public static void Cmd_RemoveCommand(string cmd_name)
	{
		if (!cmd_functions.ContainsKey(cmd_name))
			clientserver.Com_Printf($"Cmd_RemoveCommand: {cmd_name} not removed\n");

		cmd_functions.Remove(cmd_name);
	}

	private static bool Cmd_Exists(string cmd_name)
	{
		return cmd_functions.ContainsKey(cmd_name);
	}

	public static string? Cmd_CompleteCommand(string partial)
	{
		if (partial.Length == 0)
			return null;

		var matches = new List<string>();

		foreach (var command in cmd_functions.Keys)
		{
			/* check for exact match */
			if (command == partial)
				return command;

			/* check for partial match */
			if (command.StartsWith(partial))
				matches.Add(command);
		}

		foreach (var a in cmd_alias.Keys)
		{
			/* check for exact match */
			if (a == partial)
				return a;

			/* check for partial match */
			if (a.StartsWith(partial))
				matches.Add(a);
		}

		foreach (var cvar in cvar.cvar_vars)
		{
			/* check for exact match */
			if (cvar.name == partial)
				return cvar.name;

			/* check for partial match */
			if (cvar.name.StartsWith(partial))
				matches.Add(cvar.name);
		}

		if (matches.Count == 0)
			return null;

		if (matches.Count == 1)
			return matches[0];

		/* Sort it */
		matches.Sort();

		clientserver.Com_Printf("\n\n");

		foreach (var match in matches)
			clientserver.Com_Printf($"  {match}\n");

		var result = partial;

		while (true)
		{
			foreach (var match in matches)
			{
				if (matches.Count == result.Length)
					return result;

				if (match[result.Length] != matches[0][result.Length])
					return result;
			}

			result += matches[0][result.Length];
		}
	}

	private static string? Cmd_CompleteMapCommand(string partial)
	{
		var maps = filesystem.FS_ListFiles2("maps/*.bsp");

		if (maps.Length == 0)
			return null;

		var matches = new List<string>();

		foreach (var map in maps)
		{
			var mapName = map;

			if (mapName.Contains('/'))
				mapName = mapName[(mapName.LastIndexOf('/') + 1)..];

			mapName = mapName[..mapName.IndexOf('.')];

			/* check for exact match */
			if (partial == mapName)
				return partial;

			/* check for partial match */
			if (mapName.StartsWith(partial))
				matches.Add(mapName);
		}

		if (matches.Count == 0)
			return null;

		if (matches.Count == 1)
			return matches[0];

		matches.Sort();

		clientserver.Com_Printf("\n=================\n\n");

		foreach (var match in matches)
			clientserver.Com_Printf($"{match}\n");

		var result = partial;

		//partial complete
		while (true)
		{
			foreach (var match in matches)
			{
				if (matches.Count == result.Length)
					return result;

				if (match[result.Length] != matches[0][result.Length])
					return result;
			}

			result += matches[0][result.Length];
		}
	}

	private static bool Cmd_IsComplete(string command)
	{
		/* check for exact match */
		if (cmd_functions.ContainsKey(command))
			return true;

		if (cmd_alias.ContainsKey(command))
			return true;

		foreach (var cvar in cvar.cvar_vars)
		{
			if (cvar.name == command)
				return true;
		}

		return false;
	}

	/* ugly hack to suppress warnings from default.cfg in Key_Bind_f() */
	private static bool doneWithDefaultCfg;

	/*
	 * A complete command line has been parsed, so try to execute it
	 */
	public static void Cmd_ExecuteString(string text)
	{
		Cmd_TokenizeString(text, true);

		/* execute the command line */
		if (Cmd_Argc() == 0)
			return; /* no tokens */

		/* exec yq2.cfg is done directly after exec default.cfg, see Qcommon_Init() */
		if (Cmd_Argc() > 1 && cmd_argv[0] == "exec" && cmd_argv[1] == "yq2.cfg")
			doneWithDefaultCfg = true;

		/* check functions */
		if (cmd_functions.TryGetValue(cmd_argv[0], out var cmd))
		{
			/* forward to server command */
			if (cmd == null)
				Cmd_ExecuteString($"cmd {text}");
			else
				cmd();

			return;
		}

		/* check alias */
		if (cmd_alias.TryGetValue(cmd_argv[0], out var a))
		{
			if (++alias_count == AliasLoopCount)
			{
				clientserver.Com_Printf("ALIAS_LOOP_COUNT\n");
				return;
			}

			Cbuf_InsertText(a);

			return;
		}

		/* check cvars */
		if (cvar.Cvar_Command())
			return;

		/* send it as a server command if we are connected */
		if (!Globals.DEDICATED_ONLY)
			Cmd.ForwardToServer();
	}

	private static void Cmd_List_f()
	{
		foreach (var cmd in cmd_functions.Keys)
			clientserver.Com_Printf($"{cmd}\n");

		clientserver.Com_Printf($"{cmd_functions.Count} commands\n");
	}

	public static void Cmd_Init()
	{
		/* register our commands */
		Cmd_AddCommand("cmdlist", Cmd_List_f);
		Cmd_AddCommand("exec", Cmd_Exec_f);
		Cmd_AddCommand("vstr", Cmd_Vstr_f);
		Cmd_AddCommand("echo", Cmd_Echo_f);
		Cmd_AddCommand("alias", Cmd_Alias_f);
		Cmd_AddCommand("wait", Cmd_Wait_f);
	}

	public static void Cmd_Shutdown()
	{
		cmd_alias.Clear();
	}
}
