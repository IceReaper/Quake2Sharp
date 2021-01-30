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
namespace Quake2Sharp.game
{
	using qcommon;
	using System;
	using System.IO;
	using types;
	using util;

	public class GameSVCmds
	{
		/**
     * 
     * PACKET FILTERING
     * 
     * 
     * You can add or remove addresses from the filter list with:
     * 
     * addip <ip> removeip <ip>
     * 
     * The ip address is specified in dot format, and any unspecified digits
     * will match any value, so you can specify an entire class C network with
     * "addip 192.246.40".
     * 
     * Removeip will only remove an address specified exactly the same way. You
     * cannot addip a subnet, then removeip a single host.
     * 
     * listip Prints the current list of filters.
     * 
     * writeip Dumps "addip <ip>" commands to listip.cfg so it can be execed at
     * a later date. The filter lists are not saved and restored by default,
     * because I beleive it would cause too much confusion.
     * 
     * filterban <0 or 1>
     * 
     * If 1 (the default), then ip addresses matching the current list will be
     * prohibited from entering the game. This is the default setting.
     * 
     * If 0, then only addresses matching the list will be allowed. This lets
     * you easily set up a private game, or a game that only allows players from
     * your local network.
     * 
     */
		public class ipfilter_t
		{
			public int mask;
			public int compare;
		}

		public static void Svcmd_Test_f()
		{
			GameBase.gi.cprintf(null, Defines.PRINT_HIGH, "Svcmd_Test_f()\n");
		}

		public static readonly int MAX_IPFILTERS = 1024;
		private static readonly ipfilter_t[] ipfilters = new ipfilter_t[GameSVCmds.MAX_IPFILTERS];
		private static int numipfilters;

		static GameSVCmds()
		{
			for (var n = 0; n < GameSVCmds.MAX_IPFILTERS; n++)
				GameSVCmds.ipfilters[n] = new();
		}

		/**
     * StringToFilter.
     */
		private static bool StringToFilter(string s, ipfilter_t f)
		{
			byte[] b = { 0, 0, 0, 0 };
			byte[] m = { 0, 0, 0, 0 };

			try
			{
				var tk = s.Split(". ");

				for (var n = 0; n < 4; n++)
				{
					b[n] = (byte)Lib.atoi(tk[n]);

					if (b[n] != 0)
						m[n] = 0xff;
				}

				f.mask = new BinaryReader(new MemoryStream(m)).ReadInt32();
				f.compare = new BinaryReader(new MemoryStream(b)).ReadInt32();
			}
			catch (Exception)
			{
				GameBase.gi.cprintf(null, Defines.PRINT_HIGH, "Bad filter address: " + s + "\n");

				return false;
			}

			return true;
		}

		/**
     * SV_FilterPacket.
     */
		public static bool SV_FilterPacket(string from)
		{
			int i;
			int @in;
			int[] m = { 0, 0, 0, 0 };

			var p = 0;
			char c;

			i = 0;

			while (p < from.Length && i < 4)
			{
				m[i] = 0;

				c = from[p];

				while (c >= '0' && c <= '9')
				{
					m[i] = m[i] * 10 + (c - '0');
					c = from[p++];
				}

				if (p == from.Length || c == ':')
					break;

				i++;
				p++;
			}

			@in = (m[0] & 0xff) | ((m[1] & 0xff) << 8) | ((m[2] & 0xff) << 16) | ((m[3] & 0xff) << 24);

			for (i = 0; i < GameSVCmds.numipfilters; i++)
			{
				if ((@in & GameSVCmds.ipfilters[i].mask) == GameSVCmds.ipfilters[i].compare)
					return (int)GameBase.filterban.value != 0;
			}

			return (int)1 - GameBase.filterban.value != 0;
		}

		/**
     * SV_AddIP_f.
     */
		private static void SVCmd_AddIP_f()
		{
			int i;

			if (GameBase.gi.argc() < 3)
			{
				GameBase.gi.cprintf(null, Defines.PRINT_HIGH, "Usage:  addip <ip-mask>\n");

				return;
			}

			for (i = 0; i < GameSVCmds.numipfilters; i++)
			{
				if ((uint)GameSVCmds.ipfilters[i].compare == 0xffffffff)
					break; // free spot
			}

			if (i == GameSVCmds.numipfilters)
			{
				if (GameSVCmds.numipfilters == GameSVCmds.MAX_IPFILTERS)
				{
					GameBase.gi.cprintf(null, Defines.PRINT_HIGH, "IP filter list is full\n");

					return;
				}

				GameSVCmds.numipfilters++;
			}

			if (!GameSVCmds.StringToFilter(GameBase.gi.argv(2), GameSVCmds.ipfilters[i]))
				GameSVCmds.ipfilters[i].compare = unchecked((int)0xffffffff);
		}

		/**
     * SV_RemoveIP_f.
     */
		private static void SVCmd_RemoveIP_f()
		{
			ipfilter_t f = new();
			int i, j;

			if (GameBase.gi.argc() < 3)
			{
				GameBase.gi.cprintf(null, Defines.PRINT_HIGH, "Usage:  sv removeip <ip-mask>\n");

				return;
			}

			if (!GameSVCmds.StringToFilter(GameBase.gi.argv(2), f))
				return;

			for (i = 0; i < GameSVCmds.numipfilters; i++)
			{
				if (GameSVCmds.ipfilters[i].mask == f.mask && GameSVCmds.ipfilters[i].compare == f.compare)
				{
					for (j = i + 1; j < GameSVCmds.numipfilters; j++)
						GameSVCmds.ipfilters[j - 1] = GameSVCmds.ipfilters[j];

					GameSVCmds.numipfilters--;
					GameBase.gi.cprintf(null, Defines.PRINT_HIGH, "Removed.\n");

					return;
				}
			}

			GameBase.gi.cprintf(null, Defines.PRINT_HIGH, "Didn't find " + GameBase.gi.argv(2) + ".\n");
		}

		/**
     * SV_ListIP_f.
     */
		private static void SVCmd_ListIP_f()
		{
			int i;
			byte[] b;

			GameBase.gi.cprintf(null, Defines.PRINT_HIGH, "Filter list:\n");

			for (i = 0; i < GameSVCmds.numipfilters; i++)
			{
				b = Lib.getIntBytes(GameSVCmds.ipfilters[i].compare);
				GameBase.gi.cprintf(null, Defines.PRINT_HIGH, (b[0] & 0xff) + "." + (b[1] & 0xff) + "." + (b[2] & 0xff) + "." + (b[3] & 0xff));
			}
		}

		/**
     * SV_WriteIP_f.
     */
		private static void SVCmd_WriteIP_f()
		{
			StreamWriter f;

			//char name[MAX_OSPATH];
			string name;
			byte[] b;

			int i;
			cvar_t game;

			game = GameBase.gi.cvar("game", "", 0);

			if (game.@string == null)
				name = Defines.GAMEVERSION + "/listip.cfg";
			else
				name = game.@string + "/listip.cfg";

			GameBase.gi.cprintf(null, Defines.PRINT_HIGH, "Writing " + name + ".\n");

			f = File.CreateText(name);

			if (f == null)
			{
				GameBase.gi.cprintf(null, Defines.PRINT_HIGH, "Couldn't open " + name + "\n");

				return;
			}

			try
			{
				f.Write("set filterban " + (int)GameBase.filterban.value + "\n");

				for (i = 0; i < GameSVCmds.numipfilters; i++)
				{
					b = Lib.getIntBytes(GameSVCmds.ipfilters[i].compare);
					f.Write("sv addip " + (b[0] & 0xff) + "." + (b[1] & 0xff) + "." + (b[2] & 0xff) + "." + (b[3] & 0xff) + "\n");
				}
			}
			catch (Exception e)
			{
				Com.Printf("IOError in SVCmd_WriteIP_f:" + e);
			}

			f.Close();
		}

		/**
     * ServerCommand
     * 
     * ServerCommand will be called when an "sv" command is issued. The game can
     * issue gi.argc() / gi.argv() commands to get the rest of the parameters
     */
		public static void ServerCommand()
		{
			string cmd;

			cmd = GameBase.gi.argv(1);

			if (Lib.Q_stricmp(cmd, "test") == 0)
				GameSVCmds.Svcmd_Test_f();
			else if (Lib.Q_stricmp(cmd, "addip") == 0)
				GameSVCmds.SVCmd_AddIP_f();
			else if (Lib.Q_stricmp(cmd, "removeip") == 0)
				GameSVCmds.SVCmd_RemoveIP_f();
			else if (Lib.Q_stricmp(cmd, "listip") == 0)
				GameSVCmds.SVCmd_ListIP_f();
			else if (Lib.Q_stricmp(cmd, "writeip") == 0)
				GameSVCmds.SVCmd_WriteIP_f();
			else
				GameBase.gi.cprintf(null, Defines.PRINT_HIGH, "Unknown server command \"" + cmd + "\"\n");
		}
	}
}
