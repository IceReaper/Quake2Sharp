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

using monsters;
using qcommon;
using qcommon.types;
using server;
using System.Text;
using types;
using util;

/**
 * Cmd
 */
public class Cmd
{
	private static readonly Action List_f = () =>
	{
		var cmd = Cmd.cmd_functions;
		var i = 0;

		while (cmd != null)
		{
			Com.Printf(cmd.name + '\n');
			i++;
			cmd = cmd.next;
		}

		Com.Printf(i + " commands\n");
	};

	private static readonly Action Exec_f = () =>
	{
		if (Cmd.Argc() != 2)
		{
			Com.Printf("exec <filename> : execute a script file\n");

			return;
		}

		byte[] f = null;
		f = FS.LoadFile(Cmd.Argv(1));

		if (f == null)
		{
			Com.Printf("couldn't exec " + Cmd.Argv(1) + "\n");

			return;
		}

		Com.Printf("execing " + Cmd.Argv(1) + "\n");

		Cbuf.InsertText(Encoding.ASCII.GetString(f).Split((char)0)[0]);

		FS.FreeFile(f);
	};

	private static readonly Action Echo_f = () =>
	{
		for (var i = 1; i < Cmd.Argc(); i++)
			Com.Printf(Cmd.Argv(i) + " ");

		Com.Printf("'\n");
	};

	private static readonly Action Alias_f = () =>
	{
		cmdalias_t a = null;

		if (Cmd.Argc() == 1)
		{
			Com.Printf("Current alias commands:\n");

			for (a = Globals.cmd_alias; a != null; a = a.next)
				Com.Printf(a.name + " : " + a.value);

			return;
		}

		var s = Cmd.Argv(1);

		if (s.Length > Defines.MAX_ALIAS_NAME)
		{
			Com.Printf("Alias name is too long\n");

			return;
		}

		// if the alias already exists, reuse it
		for (a = Globals.cmd_alias; a != null; a = a.next)
		{
			if (s.Equals(a.name, StringComparison.OrdinalIgnoreCase))
			{
				a.value = null;

				break;
			}
		}

		if (a == null)
		{
			a = new();
			a.next = Globals.cmd_alias;
			Globals.cmd_alias = a;
		}

		a.name = s;

		// copy the rest of the command line
		var cmd = "";
		var c = Cmd.Argc();

		for (var i = 2; i < c; i++)
		{
			cmd = cmd + Cmd.Argv(i);

			if (i != c - 1)
				cmd = cmd + " ";
		}

		cmd = cmd + "\n";

		a.value = cmd;
	};

	public static Action Wait_f = () => { Globals.cmd_wait = true; };
	public static cmd_function_t cmd_functions;
	public static int cmd_argc;
	public static string[] cmd_argv = new string[Defines.MAX_STRING_TOKENS];
	public static string cmd_args;
	public static readonly int ALIAS_LOOP_COUNT = 16;

	/**
     * Register our commands.
     */
	public static void Init()
	{
		Cmd.AddCommand("exec", Cmd.Exec_f);
		Cmd.AddCommand("echo", Cmd.Echo_f);
		Cmd.AddCommand("cmdlist", Cmd.List_f);
		Cmd.AddCommand("alias", Cmd.Alias_f);
		Cmd.AddCommand("wait", Cmd.Wait_f);
	}

	private static readonly char[] expanded = new char[Defines.MAX_STRING_CHARS];
	private static readonly char[] temporary = new char[Defines.MAX_STRING_CHARS];

	public static Comparer<object> PlayerSort = Comparer<object>.Create(
		(o1, o2) =>
		{
			var anum = (int)o1;
			var bnum = (int)o2;

			int anum1 = GameBase.game.clients[anum].ps.stats[Defines.STAT_FRAGS];
			int bnum1 = GameBase.game.clients[bnum].ps.stats[Defines.STAT_FRAGS];

			if (anum1 < bnum1)
				return -1;

			if (anum1 > bnum1)
				return 1;

			return 0;
		}
	);

	/** 
     * Cmd_MacroExpandString.
     */
	public static char[] MacroExpandString(char[] text, int len)
	{
		int i, j, count;
		bool inquote;

		char[] scan;

		string token;
		inquote = false;

		scan = text;

		if (len >= Defines.MAX_STRING_CHARS)
		{
			Com.Printf("Line exceeded " + Defines.MAX_STRING_CHARS + " chars, discarded.\n");

			return null;
		}

		count = 0;

		for (i = 0; i < len; i++)
		{
			if (scan[i] == '"')
				inquote = !inquote;

			if (inquote)
				continue; // don't expand inside quotes

			if (scan[i] != '$')
				continue;

			// scan out the complete macro, without $
			Com.ParseHelp ph = new(text, i + 1);
			token = Com.Parse(ph);

			if (ph.data == null)
				continue;

			token = Cvar.VariableString(token);

			j = token.Length;

			len += j;

			if (len >= Defines.MAX_STRING_CHARS)
			{
				Com.Printf("Expanded line exceeded " + Defines.MAX_STRING_CHARS + " chars, discarded.\n");

				return null;
			}

			Array.Copy(scan, 0, Cmd.temporary, 0, i);
			Array.Copy(token.ToCharArray(), 0, Cmd.temporary, i, token.Length);
			Array.Copy(ph.data, ph.index, Cmd.temporary, i + j, len - ph.index - j);

			Array.Copy(Cmd.temporary, 0, Cmd.expanded, 0, 0);
			scan = Cmd.expanded;
			i--;

			if (++count == 100)
			{
				Com.Printf("Macro expansion loop, discarded.\n");

				return null;
			}
		}

		if (inquote)
		{
			Com.Printf("Line has unmatched quote, discarded.\n");

			return null;
		}

		return scan;
	}

	/**
     * Cmd_TokenizeString
     * 
     * Parses the given string into command line tokens. $Cvars will be expanded
     * unless they are in a quoted token.
     */
	public static void TokenizeString(char[] text, bool macroExpand)
	{
		string com_token;

		Cmd.cmd_argc = 0;
		Cmd.cmd_args = "";

		var len = Lib.strlen(text);

		// macro expand the text
		if (macroExpand)
			text = Cmd.MacroExpandString(text, len);

		if (text == null)
			return;

		len = Lib.strlen(text);

		Com.ParseHelp ph = new(text);

		while (true)
		{
			// skip whitespace up to a /n
			var c = ph.skipwhitestoeol();

			if (c == '\n')
			{
				// a newline seperates commands in the buffer
				c = ph.nextchar();

				break;
			}

			if (c == 0)
				return;

			// set cmd_args to everything after the first arg
			if (Cmd.cmd_argc == 1)
			{
				Cmd.cmd_args = new(text, ph.index, len - ph.index);
				Cmd.cmd_args.Trim();
			}

			com_token = Com.Parse(ph);

			if (ph.data == null)
				return;

			if (Cmd.cmd_argc < Defines.MAX_STRING_TOKENS)
			{
				Cmd.cmd_argv[Cmd.cmd_argc] = com_token;
				Cmd.cmd_argc++;
			}
		}
	}

	public static void AddCommand(string cmd_name, Action function)
	{
		cmd_function_t cmd;

		//Com.DPrintf("Cmd_AddCommand: " + cmd_name + "\n");
		// fail if the command is a variable name
		if (Cvar.VariableString(cmd_name).Length > 0)
		{
			Com.Printf("Cmd_AddCommand: " + cmd_name + " already defined as a var\n");

			return;
		}

		// fail if the command already exists
		for (cmd = Cmd.cmd_functions; cmd != null; cmd = cmd.next)
		{
			if (cmd_name.Equals(cmd.name))
			{
				Com.Printf("Cmd_AddCommand: " + cmd_name + " already defined\n");

				return;
			}
		}

		cmd = new();
		cmd.name = cmd_name;

		cmd.function = function;
		cmd.next = Cmd.cmd_functions;
		Cmd.cmd_functions = cmd;
	}

	/**
     * Cmd_RemoveCommand 
     */
	public static void RemoveCommand(string cmd_name)
	{
		cmd_function_t cmd, back = null;

		back = cmd = Cmd.cmd_functions;

		while (true)
		{
			if (cmd == null)
			{
				Com.Printf("Cmd_RemoveCommand: " + cmd_name + " not added\n");

				return;
			}

			if (0 == Lib.strcmp(cmd_name, cmd.name))
			{
				if (cmd == Cmd.cmd_functions)
					Cmd.cmd_functions = cmd.next;
				else
					back.next = cmd.next;

				return;
			}

			back = cmd;
			cmd = cmd.next;
		}
	}

	/** 
     * Cmd_Exists 
     */
	public static bool Exists(string cmd_name)
	{
		cmd_function_t cmd;

		for (cmd = Cmd.cmd_functions; cmd != null; cmd = cmd.next)
		{
			if (cmd.name.Equals(cmd_name))
				return true;
		}

		return false;
	}

	public static int Argc()
	{
		return Cmd.cmd_argc;
	}

	public static string Argv(int i)
	{
		if (i < 0 || i >= Cmd.cmd_argc)
			return "";

		return Cmd.cmd_argv[i];
	}

	public static string Args()
	{
		return new(Cmd.cmd_args);
	}

	/**
     * Cmd_ExecuteString
     * 
     * A complete command line has been parsed, so try to execute it 
     * FIXME: lookupnoadd the token to speed search? 
     */
	public static void ExecuteString(string text)
	{
		cmd_function_t cmd;
		cmdalias_t a;

		Cmd.TokenizeString(text.ToCharArray(), true);

		// execute the command line
		if (Cmd.Argc() == 0)
			return; // no tokens

		// check functions
		for (cmd = Cmd.cmd_functions; cmd != null; cmd = cmd.next)
		{
			if (Cmd.cmd_argv[0].Equals(cmd.name, StringComparison.OrdinalIgnoreCase))
			{
				if (null == cmd.function)
				{
					// forward to server command
					Cmd.ExecuteString("cmd " + text);
				}
				else
					cmd.function();

				return;
			}
		}

		// check alias
		for (a = Globals.cmd_alias; a != null; a = a.next)
		{
			if (Cmd.cmd_argv[0].Equals(a.name, StringComparison.OrdinalIgnoreCase))
			{
				if (++Globals.alias_count == Cmd.ALIAS_LOOP_COUNT)
				{
					Com.Printf("ALIAS_LOOP_COUNT\n");

					return;
				}

				Cbuf.InsertText(a.value);

				return;
			}
		}

		// check cvars
		if (Cvar.Command())
			return;

		// send it as a server command if we are connected
		Cmd.ForwardToServer();
	}

	/**
     * Cmd_Give_f
     * 
     * Give items to a client.
     */
	public static void Give_f(edict_t ent)
	{
		string name;
		gitem_t it;
		int index;
		int i;
		bool give_all;
		edict_t it_ent;

		if (GameBase.deathmatch.value != 0 && GameBase.sv_cheats.value == 0)
		{
			SV_GAME.PF_cprintfhigh(ent, "You must run the server with '+set cheats 1' to enable this command.\n");

			return;
		}

		name = Cmd.Args();

		if (0 == Lib.Q_stricmp(name, "all"))
			give_all = true;
		else
			give_all = false;

		if (give_all || 0 == Lib.Q_stricmp(Cmd.Argv(1), "health"))
		{
			if (Cmd.Argc() == 3)
				ent.health = Lib.atoi(Cmd.Argv(2));
			else
				ent.health = ent.max_health;

			if (!give_all)
				return;
		}

		if (give_all || 0 == Lib.Q_stricmp(name, "weapons"))
		{
			for (i = 1; i < GameBase.game.num_items; i++)
			{
				it = GameItemList.itemlist[i];

				if (null == it.pickup)
					continue;

				if (0 == (it.flags & Defines.IT_WEAPON))
					continue;

				ent.client.pers.inventory[i] += 1;
			}

			if (!give_all)
				return;
		}

		if (give_all || 0 == Lib.Q_stricmp(name, "ammo"))
		{
			for (i = 1; i < GameBase.game.num_items; i++)
			{
				it = GameItemList.itemlist[i];

				if (null == it.pickup)
					continue;

				if (0 == (it.flags & Defines.IT_AMMO))
					continue;

				GameItems.Add_Ammo(ent, it, 1000);
			}

			if (!give_all)
				return;
		}

		if (give_all || Lib.Q_stricmp(name, "armor") == 0)
		{
			gitem_armor_t info;

			it = GameItems.FindItem("Jacket Armor");
			ent.client.pers.inventory[GameItems.ITEM_INDEX(it)] = 0;

			it = GameItems.FindItem("Combat Armor");
			ent.client.pers.inventory[GameItems.ITEM_INDEX(it)] = 0;

			it = GameItems.FindItem("Body Armor");
			info = (gitem_armor_t)it.info;
			ent.client.pers.inventory[GameItems.ITEM_INDEX(it)] = info.max_count;

			if (!give_all)
				return;
		}

		if (give_all || Lib.Q_stricmp(name, "Power Shield") == 0)
		{
			it = GameItems.FindItem("Power Shield");
			it_ent = GameUtil.G_Spawn();
			it_ent.classname = it.classname;
			GameItems.SpawnItem(it_ent, it);
			GameItems.Touch_Item.touch(it_ent, ent, GameBase.dummyplane, null);

			if (it_ent.inuse)
				GameUtil.G_FreeEdict(it_ent);

			if (!give_all)
				return;
		}

		if (give_all)
		{
			for (i = 1; i < GameBase.game.num_items; i++)
			{
				it = GameItemList.itemlist[i];

				if (it.pickup != null)
					continue;

				if ((it.flags & (Defines.IT_ARMOR | Defines.IT_WEAPON | Defines.IT_AMMO)) != 0)
					continue;

				ent.client.pers.inventory[i] = 1;
			}

			return;
		}

		it = GameItems.FindItem(name);

		if (it == null)
		{
			name = Cmd.Argv(1);
			it = GameItems.FindItem(name);

			if (it == null)
			{
				SV_GAME.PF_cprintf(ent, Defines.PRINT_HIGH, "unknown item\n");

				return;
			}
		}

		if (it.pickup == null)
		{
			SV_GAME.PF_cprintf(ent, Defines.PRINT_HIGH, "non-pickup item\n");

			return;
		}

		index = GameItems.ITEM_INDEX(it);

		if ((it.flags & Defines.IT_AMMO) != 0)
		{
			if (Cmd.Argc() == 3)
				ent.client.pers.inventory[index] = Lib.atoi(Cmd.Argv(2));
			else
				ent.client.pers.inventory[index] += it.quantity;
		}
		else
		{
			it_ent = GameUtil.G_Spawn();
			it_ent.classname = it.classname;
			GameItems.SpawnItem(it_ent, it);
			GameItems.Touch_Item.touch(it_ent, ent, GameBase.dummyplane, null);

			if (it_ent.inuse)
				GameUtil.G_FreeEdict(it_ent);
		}
	}

	/** 
     * Cmd_God_f
     * 
     * Sets client to godmode
     * 
     * argv(0) god
     */
	public static void God_f(edict_t ent)
	{
		string msg;

		if (GameBase.deathmatch.value != 0 && GameBase.sv_cheats.value == 0)
		{
			SV_GAME.PF_cprintfhigh(ent, "You must run the server with '+set cheats 1' to enable this command.\n");

			return;
		}

		ent.flags ^= Defines.FL_GODMODE;

		if (0 == (ent.flags & Defines.FL_GODMODE))
			msg = "godmode OFF\n";
		else
			msg = "godmode ON\n";

		SV_GAME.PF_cprintf(ent, Defines.PRINT_HIGH, msg);
	}

	/** 
     * Cmd_Notarget_f
     * 
     * Sets client to notarget
     * 
     * argv(0) notarget.
     */
	public static void Notarget_f(edict_t ent)
	{
		string msg;

		if (GameBase.deathmatch.value != 0 && GameBase.sv_cheats.value == 0)
		{
			SV_GAME.PF_cprintfhigh(ent, "You must run the server with '+set cheats 1' to enable this command.\n");

			return;
		}

		ent.flags ^= Defines.FL_NOTARGET;

		if (0 == (ent.flags & Defines.FL_NOTARGET))
			msg = "notarget OFF\n";
		else
			msg = "notarget ON\n";

		SV_GAME.PF_cprintfhigh(ent, msg);
	}

	/**
     * Cmd_Noclip_f
     * 
     * argv(0) noclip.
     */
	public static void Noclip_f(edict_t ent)
	{
		string msg;

		if (GameBase.deathmatch.value != 0 && GameBase.sv_cheats.value == 0)
		{
			SV_GAME.PF_cprintfhigh(ent, "You must run the server with '+set cheats 1' to enable this command.\n");

			return;
		}

		if (ent.movetype == Defines.MOVETYPE_NOCLIP)
		{
			ent.movetype = Defines.MOVETYPE_WALK;
			msg = "noclip OFF\n";
		}
		else
		{
			ent.movetype = Defines.MOVETYPE_NOCLIP;
			msg = "noclip ON\n";
		}

		SV_GAME.PF_cprintfhigh(ent, msg);
	}

	/**
     * Cmd_Use_f
     * 
     * Use an inventory item.
     */
	public static void Use_f(edict_t ent)
	{
		int index;
		gitem_t it;
		string s;

		s = Cmd.Args();

		it = GameItems.FindItem(s);
		Com.dprintln("using:" + s);

		if (it == null)
		{
			SV_GAME.PF_cprintfhigh(ent, "unknown item: " + s + "\n");

			return;
		}

		if (it.use == null)
		{
			SV_GAME.PF_cprintfhigh(ent, "Item is not usable.\n");

			return;
		}

		index = GameItems.ITEM_INDEX(it);

		if (0 == ent.client.pers.inventory[index])
		{
			SV_GAME.PF_cprintfhigh(ent, "Out of item: " + s + "\n");

			return;
		}

		it.use.use(ent, it);
	}

	/**
     * Cmd_Drop_f
     * 
     * Drop an inventory item.
     */
	public static void Drop_f(edict_t ent)
	{
		int index;
		gitem_t it;
		string s;

		s = Cmd.Args();
		it = GameItems.FindItem(s);

		if (it == null)
		{
			SV_GAME.PF_cprintfhigh(ent, "unknown item: " + s + "\n");

			return;
		}

		if (it.drop == null)
		{
			SV_GAME.PF_cprintf(ent, Defines.PRINT_HIGH, "Item is not dropable.\n");

			return;
		}

		index = GameItems.ITEM_INDEX(it);

		if (0 == ent.client.pers.inventory[index])
		{
			SV_GAME.PF_cprintfhigh(ent, "Out of item: " + s + "\n");

			return;
		}

		it.drop.drop(ent, it);
	}

	/**
     * Cmd_Inven_f.
     */
	public static void Inven_f(edict_t ent)
	{
		int i;
		gclient_t cl;

		cl = ent.client;

		cl.showscores = false;
		cl.showhelp = false;

		if (cl.showinventory)
		{
			cl.showinventory = false;

			return;
		}

		cl.showinventory = true;

		GameBase.gi.WriteByte(Defines.svc_inventory);

		for (i = 0; i < Defines.MAX_ITEMS; i++)
			GameBase.gi.WriteShort(cl.pers.inventory[i]);

		GameBase.gi.unicast(ent, true);
	}

	/**
     * Cmd_InvUse_f.
     */
	public static void InvUse_f(edict_t ent)
	{
		gitem_t it;

		Cmd.ValidateSelectedItem(ent);

		if (ent.client.pers.selected_item == -1)
		{
			SV_GAME.PF_cprintfhigh(ent, "No item to use.\n");

			return;
		}

		it = GameItemList.itemlist[ent.client.pers.selected_item];

		if (it.use == null)
		{
			SV_GAME.PF_cprintfhigh(ent, "Item is not usable.\n");

			return;
		}

		it.use.use(ent, it);
	}

	/**
     * Cmd_WeapPrev_f.
     */
	public static void WeapPrev_f(edict_t ent)
	{
		gclient_t cl;
		int i, index;
		gitem_t it;
		int selected_weapon;

		cl = ent.client;

		if (cl.pers.weapon == null)
			return;

		selected_weapon = GameItems.ITEM_INDEX(cl.pers.weapon);

		// scan for the next valid one
		for (i = 1; i <= Defines.MAX_ITEMS; i++)
		{
			index = (selected_weapon + i) % Defines.MAX_ITEMS;

			if (0 == cl.pers.inventory[index])
				continue;

			it = GameItemList.itemlist[index];

			if (it.use == null)
				continue;

			if (0 == (it.flags & Defines.IT_WEAPON))
				continue;

			it.use.use(ent, it);

			if (cl.pers.weapon == it)
				return; // successful
		}
	}

	/**
     * Cmd_WeapNext_f.
     */
	public static void WeapNext_f(edict_t ent)
	{
		gclient_t cl;
		int i, index;
		gitem_t it;
		int selected_weapon;

		cl = ent.client;

		if (null == cl.pers.weapon)
			return;

		selected_weapon = GameItems.ITEM_INDEX(cl.pers.weapon);

		// scan for the next valid one
		for (i = 1; i <= Defines.MAX_ITEMS; i++)
		{
			index = (selected_weapon + Defines.MAX_ITEMS - i) % Defines.MAX_ITEMS;

			//bugfix rst
			if (index == 0)
				index++;

			if (0 == cl.pers.inventory[index])
				continue;

			it = GameItemList.itemlist[index];

			if (null == it.use)
				continue;

			if (0 == (it.flags & Defines.IT_WEAPON))
				continue;

			it.use.use(ent, it);

			if (cl.pers.weapon == it)
				return; // successful
		}
	}

	/** 
     * Cmd_WeapLast_f.
     */
	public static void WeapLast_f(edict_t ent)
	{
		gclient_t cl;
		int index;
		gitem_t it;

		cl = ent.client;

		if (null == cl.pers.weapon || null == cl.pers.lastweapon)
			return;

		index = GameItems.ITEM_INDEX(cl.pers.lastweapon);

		if (0 == cl.pers.inventory[index])
			return;

		it = GameItemList.itemlist[index];

		if (null == it.use)
			return;

		if (0 == (it.flags & Defines.IT_WEAPON))
			return;

		it.use.use(ent, it);
	}

	/**
     * Cmd_InvDrop_f 
     */
	public static void InvDrop_f(edict_t ent)
	{
		gitem_t it;

		Cmd.ValidateSelectedItem(ent);

		if (ent.client.pers.selected_item == -1)
		{
			SV_GAME.PF_cprintfhigh(ent, "No item to drop.\n");

			return;
		}

		it = GameItemList.itemlist[ent.client.pers.selected_item];

		if (it.drop == null)
		{
			SV_GAME.PF_cprintfhigh(ent, "Item is not dropable.\n");

			return;
		}

		it.drop.drop(ent, it);
	}

	/** 
     * Cmd_Score_f
     * 
     * Display the scoreboard.
     * 
     */
	public static void Score_f(edict_t ent)
	{
		ent.client.showinventory = false;
		ent.client.showhelp = false;

		if (0 == GameBase.deathmatch.value && 0 == GameBase.coop.value)
			return;

		if (ent.client.showscores)
		{
			ent.client.showscores = false;

			return;
		}

		ent.client.showscores = true;
		PlayerHud.DeathmatchScoreboard(ent);
	}

	/**
     * Cmd_Help_f
     * 
     * Display the current help message. 
     *
     */
	public static void Help_f(edict_t ent)
	{
		// this is for backwards compatability
		if (GameBase.deathmatch.value != 0)
		{
			Cmd.Score_f(ent);

			return;
		}

		ent.client.showinventory = false;
		ent.client.showscores = false;

		if (ent.client.showhelp && ent.client.pers.game_helpchanged == GameBase.game.helpchanged)
		{
			ent.client.showhelp = false;

			return;
		}

		ent.client.showhelp = true;
		ent.client.pers.helpchanged = 0;
		PlayerHud.HelpComputer(ent);
	}

	/**
     * Cmd_Kill_f
     */
	public static void Kill_f(edict_t ent)
	{
		if (GameBase.level.time - ent.client.respawn_time < 5)
			return;

		ent.flags &= ~Defines.FL_GODMODE;
		ent.health = 0;
		GameBase.meansOfDeath = Defines.MOD_SUICIDE;
		PlayerClient.player_die.die(ent, ent, ent, 100000, Globals.vec3_origin);
	}

	/**
     * Cmd_PutAway_f
     */
	public static void PutAway_f(edict_t ent)
	{
		ent.client.showscores = false;
		ent.client.showhelp = false;
		ent.client.showinventory = false;
	}

	/**
     * Cmd_Players_f
     */
	public static void Players_f(edict_t ent)
	{
		int i;
		int count;
		string small;
		string large;

		var index = new int[256];

		count = 0;

		for (i = 0; i < GameBase.maxclients.value; i++)
		{
			if (GameBase.game.clients[i].pers.connected)
			{
				index[count] = i;
				count++;
			}
		}

		// sort by frags
		Array.Sort(index, 0, count - 1, Cmd.PlayerSort);

		// print information
		large = "";

		for (i = 0; i < count; i++)
		{
			small = GameBase.game.clients[index[i]].ps.stats[Defines.STAT_FRAGS] + " " + GameBase.game.clients[index[i]].pers.netname + "\n";

			if (small.Length + large.Length > 1024 - 100)
			{
				// can't print all of them in one packet
				large += "...\n";

				break;
			}

			large += small;
		}

		SV_GAME.PF_cprintfhigh(ent, large + "\n" + count + " players\n");
	}

	/**
     * Cmd_Wave_f
     */
	public static void Wave_f(edict_t ent)
	{
		int i;

		i = Lib.atoi(Cmd.Argv(1));

		// can't wave when ducked
		if ((ent.client.ps.pmove.pm_flags & pmove_t.PMF_DUCKED) != 0)
			return;

		if (ent.client.anim_priority > Defines.ANIM_WAVE)
			return;

		ent.client.anim_priority = Defines.ANIM_WAVE;

		switch (i)
		{
			case 0:
				SV_GAME.PF_cprintfhigh(ent, "flipoff\n");
				ent.s.frame = M_Player.FRAME_flip01 - 1;
				ent.client.anim_end = M_Player.FRAME_flip12;

				break;

			case 1:
				SV_GAME.PF_cprintfhigh(ent, "salute\n");
				ent.s.frame = M_Player.FRAME_salute01 - 1;
				ent.client.anim_end = M_Player.FRAME_salute11;

				break;

			case 2:
				SV_GAME.PF_cprintfhigh(ent, "taunt\n");
				ent.s.frame = M_Player.FRAME_taunt01 - 1;
				ent.client.anim_end = M_Player.FRAME_taunt17;

				break;

			case 3:
				SV_GAME.PF_cprintfhigh(ent, "wave\n");
				ent.s.frame = M_Player.FRAME_wave01 - 1;
				ent.client.anim_end = M_Player.FRAME_wave11;

				break;

			case 4:
			default:
				SV_GAME.PF_cprintfhigh(ent, "point\n");
				ent.s.frame = M_Player.FRAME_point01 - 1;
				ent.client.anim_end = M_Player.FRAME_point12;

				break;
		}
	}

	/**
     * Command to print the players own position.
     */
	public static void ShowPosition_f(edict_t ent)
	{
		SV_GAME.PF_cprintfhigh(ent, "pos=" + Lib.vtofsbeaty(ent.s.origin) + "\n");
	}

	/**
     * Cmd_Say_f
     */
	public static void Say_f(edict_t ent, bool team, bool arg0)
	{
		int i, j;
		edict_t other;
		string text;
		gclient_t cl;

		if (Cmd.Argc() < 2 && !arg0)
			return;

		if (0 == ((int)GameBase.dmflags.value & (Defines.DF_MODELTEAMS | Defines.DF_SKINTEAMS)))
			team = false;

		if (team)
			text = "(" + ent.client.pers.netname + "): ";
		else
			text = "" + ent.client.pers.netname + ": ";

		if (arg0)
		{
			text += Cmd.Argv(0);
			text += " ";
			text += Cmd.Args();
		}
		else
		{
			if (Cmd.Args().StartsWith("\""))
				text += Cmd.Args()[1..];
			else
				text += Cmd.Args();
		}

		// don't let text be too long for malicious reasons
		if (text.Length > 150)

			//text[150] = 0;
			text = text[..150];

		text += "\n";

		if (GameBase.flood_msgs.value != 0)
		{
			cl = ent.client;

			if (GameBase.level.time < cl.flood_locktill)
			{
				SV_GAME.PF_cprintfhigh(ent, "You can't talk for " + (int)(cl.flood_locktill - GameBase.level.time) + " more seconds\n");

				return;
			}

			i = (int)(cl.flood_whenhead - GameBase.flood_msgs.value + 1);

			if (i < 0)
				i = 10 + i;

			if (cl.flood_when[i] != 0 && GameBase.level.time - cl.flood_when[i] < GameBase.flood_persecond.value)
			{
				cl.flood_locktill = GameBase.level.time + GameBase.flood_waitdelay.value;

				SV_GAME.PF_cprintf(ent, Defines.PRINT_CHAT, "Flood protection:  You can't talk for " + (int)GameBase.flood_waitdelay.value + " seconds.\n");

				return;
			}

			cl.flood_whenhead = (cl.flood_whenhead + 1) % 10;
			cl.flood_when[cl.flood_whenhead] = GameBase.level.time;
		}

		if (Globals.dedicated.value != 0)
			SV_GAME.PF_cprintf(null, Defines.PRINT_CHAT, "" + text + "");

		for (j = 1; j <= GameBase.game.maxclients; j++)
		{
			other = GameBase.g_edicts[j];

			if (!other.inuse)
				continue;

			if (other.client == null)
				continue;

			if (team)
			{
				if (!GameUtil.OnSameTeam(ent, other))
					continue;
			}

			SV_GAME.PF_cprintf(other, Defines.PRINT_CHAT, "" + text + "");
		}
	}

	/**
     * Returns the playerlist. TODO: The list is badly formatted at the moment.
     */
	public static void PlayerList_f(edict_t ent)
	{
		int i;
		string st;
		string text;
		edict_t e2;

		// connect time, ping, score, name
		text = "";

		for (i = 0; i < GameBase.maxclients.value; i++)
		{
			e2 = GameBase.g_edicts[1 + i];

			if (!e2.inuse)
				continue;

			st = ""
				+ (GameBase.level.framenum - e2.client.resp.enterframe) / 600
				+ ":"
				+ (GameBase.level.framenum - e2.client.resp.enterframe) % 600 / 10
				+ " "
				+ e2.client.ping
				+ " "
				+ e2.client.resp.score
				+ " "
				+ e2.client.pers.netname
				+ " "
				+ (e2.client.resp.spectator ? " (spectator)" : "")
				+ "\n";

			if (text.Length + st.Length > 1024 - 50)
			{
				text += "And more...\n";
				SV_GAME.PF_cprintfhigh(ent, "" + text + "");

				return;
			}

			text += st;
		}

		SV_GAME.PF_cprintfhigh(ent, text);
	}

	/**
     * Adds the current command line as a clc_stringcmd to the client message.
     * things like godmode, noclip, etc, are commands directed to the server, so
     * when they are typed in at the console, they will need to be forwarded.
     */
	public static void ForwardToServer()
	{
		string cmd;

		cmd = Cmd.Argv(0);

		if (Globals.cls.state <= Defines.ca_connected || cmd[0] == '-' || cmd[0] == '+')
		{
			Com.Printf("Unknown command \"" + cmd + "\"\n");

			return;
		}

		MSG.WriteByte(Globals.cls.netchan.message, Defines.clc_stringcmd);
		SZ.Print(Globals.cls.netchan.message, cmd);

		if (Cmd.Argc() > 1)
		{
			SZ.Print(Globals.cls.netchan.message, " ");
			SZ.Print(Globals.cls.netchan.message, Cmd.Args());
		}
	}

	/**
     * Cmd_CompleteCommand.
     */
	public static List<string> CompleteCommand(string partial)
	{
		List<string> cmds = new();

		// check for match
		for (var cmd = Cmd.cmd_functions; cmd != null; cmd = cmd.next)
		{
			if (cmd.name.StartsWith(partial))
				cmds.Add(cmd.name);
		}

		for (var a = Globals.cmd_alias; a != null; a = a.next)
		{
			if (a.name.StartsWith(partial))
				cmds.Add(a.name);
		}

		return cmds;
	}

	/**
     * Processes the commands the player enters in the quake console.
     */
	public static void ClientCommand(edict_t ent)
	{
		string cmd;

		if (ent.client == null)
			return; // not fully in game yet

		cmd = GameBase.gi.argv(0).ToLower();

		if (cmd.Equals("players"))
		{
			Cmd.Players_f(ent);

			return;
		}

		if (cmd.Equals("say"))
		{
			Cmd.Say_f(ent, false, false);

			return;
		}

		if (cmd.Equals("say_team"))
		{
			Cmd.Say_f(ent, true, false);

			return;
		}

		if (cmd.Equals("score"))
		{
			Cmd.Score_f(ent);

			return;
		}

		if (cmd.Equals("help"))
		{
			Cmd.Help_f(ent);

			return;
		}

		if (GameBase.level.intermissiontime != 0)
			return;

		if (cmd.Equals("use"))
			Cmd.Use_f(ent);
		else if (cmd.Equals("drop"))
			Cmd.Drop_f(ent);
		else if (cmd.Equals("give"))
			Cmd.Give_f(ent);
		else if (cmd.Equals("god"))
			Cmd.God_f(ent);
		else if (cmd.Equals("notarget"))
			Cmd.Notarget_f(ent);
		else if (cmd.Equals("noclip"))
			Cmd.Noclip_f(ent);
		else if (cmd.Equals("inven"))
			Cmd.Inven_f(ent);
		else if (cmd.Equals("invnext"))
			GameItems.SelectNextItem(ent, -1);
		else if (cmd.Equals("invprev"))
			GameItems.SelectPrevItem(ent, -1);
		else if (cmd.Equals("invnextw"))
			GameItems.SelectNextItem(ent, Defines.IT_WEAPON);
		else if (cmd.Equals("invprevw"))
			GameItems.SelectPrevItem(ent, Defines.IT_WEAPON);
		else if (cmd.Equals("invnextp"))
			GameItems.SelectNextItem(ent, Defines.IT_POWERUP);
		else if (cmd.Equals("invprevp"))
			GameItems.SelectPrevItem(ent, Defines.IT_POWERUP);
		else if (cmd.Equals("invuse"))
			Cmd.InvUse_f(ent);
		else if (cmd.Equals("invdrop"))
			Cmd.InvDrop_f(ent);
		else if (cmd.Equals("weapprev"))
			Cmd.WeapPrev_f(ent);
		else if (cmd.Equals("weapnext"))
			Cmd.WeapNext_f(ent);
		else if (cmd.Equals("weaplast"))
			Cmd.WeapLast_f(ent);
		else if (cmd.Equals("kill"))
			Cmd.Kill_f(ent);
		else if (cmd.Equals("putaway"))
			Cmd.PutAway_f(ent);
		else if (cmd.Equals("wave"))
			Cmd.Wave_f(ent);
		else if (cmd.Equals("playerlist"))
			Cmd.PlayerList_f(ent);
		else if (cmd.Equals("showposition"))
			Cmd.ShowPosition_f(ent);
		else

			// anything that doesn't match a command will be a chat
			Cmd.Say_f(ent, false, true);
	}

	public static void ValidateSelectedItem(edict_t ent)
	{
		var cl = ent.client;

		if (cl.pers.inventory[cl.pers.selected_item] != 0)
			return; // valid

		GameItems.SelectNextItem(ent, -1);
	}
}