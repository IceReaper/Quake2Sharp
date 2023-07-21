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
namespace Quake2Sharp.client;

using qcommon;
using util;

/**
 * CL_inv
 */
public class CL_inv
{
	/*
	 * ================ CL_ParseInventory ================
	 */
	public static void ParseInventory()
	{
		int i;

		for (i = 0; i < Defines.MAX_ITEMS; i++)
			Globals.cl.inventory[i] = MSG.ReadShort(Globals.net_message);
	}

	/*
	 * ================ Inv_DrawString ================
	 */
	private static void Inv_DrawString(int x, int y, string @string)
	{
		for (var i = 0; i < @string.Length; i++)
		{
			Globals.re.DrawChar(x, y, @string[i]);
			x += 8;
		}
	}

	private static string getHighBitString(string s)
	{
		var b = Lib.stringToBytes(s);

		for (var i = 0; i < b.Length; i++)
			b[i] = (byte)(b[i] | 128);

		return Lib.bytesToString(b);
	}

	/*
	 * ================ CL_DrawInventory ================
	 */
	private static readonly int DISPLAY_ITEMS = 17;

	public static void DrawInventory()
	{
		int i, j;
		int num, selected_num, item;
		var index = new int[Defines.MAX_ITEMS];
		string @string;
		int x, y;
		string binding;
		string bind;
		int selected;
		int top;
		selected = Globals.cl.frame.playerstate.stats[Defines.STAT_SELECTED_ITEM];
		num = 0;
		selected_num = 0;

		for (i = 0; i < Defines.MAX_ITEMS; i++)
		{
			if (i == selected)
				selected_num = num;

			if (Globals.cl.inventory[i] != 0)
			{
				index[num] = i;
				num++;
			}
		}

		// determine scroll point
		top = selected_num - CL_inv.DISPLAY_ITEMS / 2;

		if (num - top < CL_inv.DISPLAY_ITEMS)
			top = num - CL_inv.DISPLAY_ITEMS;

		if (top < 0)
			top = 0;

		x = (Globals.viddef.getWidth() - 256) / 2;
		y = (Globals.viddef.getHeight() - 240) / 2;

		// repaint everything next frame
		SCR.DirtyScreen();
		Globals.re.DrawPic(x, y + 8, "inventory");
		y += 24;
		x += 24;
		CL_inv.Inv_DrawString(x, y, "hotkey ### item");
		CL_inv.Inv_DrawString(x, y + 8, "------ --- ----");
		y += 16;

		for (i = top; i < num && i < top + CL_inv.DISPLAY_ITEMS; i++)
		{
			item = index[i];

			// search for a binding
			//Com_sprintf (binding, sizeof(binding), "use %s",
			// cl.configstrings[CS_ITEMS+item]);
			binding = "use " + Globals.cl.configstrings[Defines.CS_ITEMS + item];
			bind = "";

			for (j = 0; j < 256; j++)
			{
				if (Globals.keybindings[j] != null && Globals.keybindings[j].Equals(binding))
				{
					bind = Key.KeynumToString(j);

					break;
				}
			}

			@string = $"{bind,6} {Globals.cl.inventory[item],3} {Globals.cl.configstrings[Defines.CS_ITEMS + item]}";

			if (item != selected)
				@string = CL_inv.getHighBitString(@string);
			else // draw a blinky cursor by the selected item
			{
				if (((int)(Globals.cls.realtime * 10) & 1) != 0)
					Globals.re.DrawChar(x - 8, y, 15);
			}

			CL_inv.Inv_DrawString(x, y, @string);
			y += 8;
		}
	}
}