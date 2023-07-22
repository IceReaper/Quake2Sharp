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

using Quake2Sharp.game.types;
using Quake2Sharp.util;
using System.Text;

namespace Quake2Sharp.game;

public class PlayerHud
{
	/*
	 * ======================================================================
	 *
	 * INTERMISSION
	 *
	 * ======================================================================
	 */

	public static void MoveClientToIntermission(edict_t ent)
	{
		if (GameBase.deathmatch.value != 0 || GameBase.coop.value != 0)
			ent.client.showscores = true;

		Math3D.VectorCopy(GameBase.level.intermission_origin, ent.s.origin);
		ent.client.ps.pmove.origin[0] = (short)(GameBase.level.intermission_origin[0] * 8);
		ent.client.ps.pmove.origin[1] = (short)(GameBase.level.intermission_origin[1] * 8);
		ent.client.ps.pmove.origin[2] = (short)(GameBase.level.intermission_origin[2] * 8);
		Math3D.VectorCopy(GameBase.level.intermission_angle, ent.client.ps.viewangles);
		ent.client.ps.pmove.pm_type = Defines.PM_FREEZE;
		ent.client.ps.gunindex = 0;
		ent.client.ps.blend[3] = 0;
		ent.client.ps.rdflags &= ~Defines.RDF_UNDERWATER;

		// clean up powerup info
		ent.client.quad_framenum = 0;
		ent.client.invincible_framenum = 0;
		ent.client.breather_framenum = 0;
		ent.client.enviro_framenum = 0;
		ent.client.grenade_blew_up = false;
		ent.client.grenade_time = 0;

		ent.viewheight = 0;
		ent.s.modelindex = 0;
		ent.s.modelindex2 = 0;
		ent.s.modelindex3 = 0;
		ent.s.modelindex = 0;
		ent.s.effects = 0;
		ent.s.sound = 0;
		ent.solid = Defines.SOLID_NOT;

		// add the layout

		if (GameBase.deathmatch.value != 0 || GameBase.coop.value != 0)
		{
			PlayerHud.DeathmatchScoreboardMessage(ent, null);
			GameBase.gi.unicast(ent, true);
		}
	}

	public static void BeginIntermission(edict_t targ)
	{
		int i, n;
		edict_t ent, client;

		if (GameBase.level.intermissiontime != 0)
			return; // already activated

		GameBase.game.autosaved = false;

		// respawn any dead clients
		for (i = 0; i < GameBase.maxclients.value; i++)
		{
			client = GameBase.g_edicts[1 + i];

			if (!client.inuse)
				continue;

			if (client.health <= 0)
				PlayerClient.respawn(client);
		}

		GameBase.level.intermissiontime = GameBase.level.time;
		GameBase.level.changemap = targ.map;

		if (GameBase.level.changemap.IndexOf('*') > -1)
		{
			if (GameBase.coop.value != 0)
			{
				for (i = 0; i < GameBase.maxclients.value; i++)
				{
					client = GameBase.g_edicts[1 + i];

					if (!client.inuse)
						continue;

					// strip players of all keys between units
					for (n = 1; n < GameItemList.itemlist.Length; n++)
					{
						// null pointer exception fixed. (RST)
						if (GameItemList.itemlist[n] != null)
						{
							if ((GameItemList.itemlist[n].flags & Defines.IT_KEY) != 0)
								client.client.pers.inventory[n] = 0;
						}
					}
				}
			}
		}
		else
		{
			if (0 == GameBase.deathmatch.value)
			{
				GameBase.level.exitintermission = true; // go immediately to the

				// next level
				return;
			}
		}

		GameBase.level.exitintermission = false;

		// find an intermission spot
		ent = GameBase.G_FindEdict(null, GameBase.findByClass, "info_player_intermission");

		if (ent == null)
		{
			// the map creator forgot to put in an intermission
			// point...
			ent = GameBase.G_FindEdict(null, GameBase.findByClass, "info_player_start");

			if (ent == null)
				ent = GameBase.G_FindEdict(null, GameBase.findByClass, "info_player_deathmatch");
		}
		else
		{
			// chose one of four spots
			i = Lib.rand() & 3;
			EdictIterator es = null;

			while (i-- > 0)
			{
				es = GameBase.G_Find(es, GameBase.findByClass, "info_player_intermission");

				if (es == null) // wrap around the list
					continue;

				ent = es.o;
			}
		}

		Math3D.VectorCopy(ent.s.origin, GameBase.level.intermission_origin);
		Math3D.VectorCopy(ent.s.angles, GameBase.level.intermission_angle);

		// move all clients to the intermission point
		for (i = 0; i < GameBase.maxclients.value; i++)
		{
			client = GameBase.g_edicts[1 + i];

			if (!client.inuse)
				continue;

			PlayerHud.MoveClientToIntermission(client);
		}
	}

	/*
	 * ==================
	 * DeathmatchScoreboardMessage
	 * ==================
	 */
	public static void DeathmatchScoreboardMessage(edict_t ent, edict_t killer)
	{
		StringBuilder @string = new(1400);

		int i, j, k;
		var sorted = new int[Defines.MAX_CLIENTS];
		var sortedscores = new int[Defines.MAX_CLIENTS];
		int score, total;
		int picnum;
		int x, y;
		gclient_t cl;
		edict_t cl_ent;
		string tag;

		// sort the clients by score
		total = 0;

		for (i = 0; i < GameBase.game.maxclients; i++)
		{
			cl_ent = GameBase.g_edicts[1 + i];

			if (!cl_ent.inuse || GameBase.game.clients[i].resp.spectator)
				continue;

			score = GameBase.game.clients[i].resp.score;

			for (j = 0; j < total; j++)
			{
				if (score > sortedscores[j])
					break;
			}

			for (k = total; k > j; k--)
			{
				sorted[k] = sorted[k - 1];
				sortedscores[k] = sortedscores[k - 1];
			}

			sorted[j] = i;
			sortedscores[j] = score;
			total++;
		}

		// print level name and exit rules

		// add the clients in sorted order
		if (total > 12)
			total = 12;

		for (i = 0; i < total; i++)
		{
			cl = GameBase.game.clients[sorted[i]];
			cl_ent = GameBase.g_edicts[1 + sorted[i]];

			picnum = GameBase.gi.imageindex("i_fixme");
			x = i >= 6 ? 160 : 0;
			y = 32 + 32 * (i % 6);

			// add a dogtag
			if (cl_ent == ent)
				tag = "tag1";
			else if (cl_ent == killer)
				tag = "tag2";
			else
				tag = null;

			if (tag != null)
				@string.Append("xv ").Append(x + 32).Append(" yv ").Append(y).Append(" picn ").Append(tag);

			// send the layout
			@string.Append(" client ")
				.Append(x)
				.Append(" ")
				.Append(y)
				.Append(" ")
				.Append(sorted[i])
				.Append(" ")
				.Append(cl.resp.score)
				.Append(" ")
				.Append(cl.ping)
				.Append(" ")
				.Append((GameBase.level.framenum - cl.resp.enterframe) / 600);
		}

		GameBase.gi.WriteByte(Defines.svc_layout);
		GameBase.gi.WriteString(@string.ToString());
	}

	/*
	 * ==================
	 * DeathmatchScoreboard
	 *
	 * Draw instead of help message. Note that it isn't that hard to overflow
	 * the 1400 byte message limit!
	 * ==================
	 */
	public static void DeathmatchScoreboard(edict_t ent)
	{
		PlayerHud.DeathmatchScoreboardMessage(ent, ent.enemy);
		GameBase.gi.unicast(ent, true);
	}

	/*
	 * ==================
	 * Cmd_Score_f
	 *
	 * Display the scoreboard
	 * ==================
	 */
	public static void Cmd_Score_f(edict_t ent)
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

	//=======================================================================

	/*
	 * ===============
	 * G_SetStats
	 * ===============
	 */
	public static void G_SetStats(edict_t ent)
	{
		gitem_t item;
		int index, cells = 0;
		int power_armor_type;

		//
		// health
		//
		ent.client.ps.stats[Defines.STAT_HEALTH_ICON] = (short)GameBase.level.pic_health;
		ent.client.ps.stats[Defines.STAT_HEALTH] = (short)ent.health;

		//
		// ammo
		//
		if (0 == ent.client.ammo_index /*
                                        * ||
                                        * !ent.client.pers.inventory[ent.client.ammo_index]
                                        */
		   )
		{
			ent.client.ps.stats[Defines.STAT_AMMO_ICON] = 0;
			ent.client.ps.stats[Defines.STAT_AMMO] = 0;
		}
		else
		{
			item = GameItemList.itemlist[ent.client.ammo_index];
			ent.client.ps.stats[Defines.STAT_AMMO_ICON] = (short)GameBase.gi.imageindex(item.icon);
			ent.client.ps.stats[Defines.STAT_AMMO] = (short)ent.client.pers.inventory[ent.client.ammo_index];
		}

		//
		// armor
		//
		power_armor_type = GameItems.PowerArmorType(ent);

		if (power_armor_type != 0)
		{
			cells = ent.client.pers.inventory[GameItems.ITEM_INDEX(GameItems.FindItem("cells"))];

			if (cells == 0)
			{
				// ran out of cells for power armor
				ent.flags &= ~Defines.FL_POWER_ARMOR;
				GameBase.gi.sound(ent, Defines.CHAN_ITEM, GameBase.gi.soundindex("misc/power2.wav"), 1, Defines.ATTN_NORM, 0);
				power_armor_type = 0;
				;
			}
		}

		index = GameItems.ArmorIndex(ent);

		if (power_armor_type != 0 && (0 == index || 0 != (GameBase.level.framenum & 8)))
		{
			// flash
			// between
			// power
			// armor
			// and
			// other
			// armor
			// icon
			ent.client.ps.stats[Defines.STAT_ARMOR_ICON] = (short)GameBase.gi.imageindex("i_powershield");
			ent.client.ps.stats[Defines.STAT_ARMOR] = (short)cells;
		}
		else if (index != 0)
		{
			item = GameItems.GetItemByIndex(index);
			ent.client.ps.stats[Defines.STAT_ARMOR_ICON] = (short)GameBase.gi.imageindex(item.icon);
			ent.client.ps.stats[Defines.STAT_ARMOR] = (short)ent.client.pers.inventory[index];
		}
		else
		{
			ent.client.ps.stats[Defines.STAT_ARMOR_ICON] = 0;
			ent.client.ps.stats[Defines.STAT_ARMOR] = 0;
		}

		//
		// pickup message
		//
		if (GameBase.level.time > ent.client.pickup_msg_time)
		{
			ent.client.ps.stats[Defines.STAT_PICKUP_ICON] = 0;
			ent.client.ps.stats[Defines.STAT_PICKUP_STRING] = 0;
		}

		//
		// timers
		//
		if (ent.client.quad_framenum > GameBase.level.framenum)
		{
			ent.client.ps.stats[Defines.STAT_TIMER_ICON] = (short)GameBase.gi.imageindex("p_quad");
			ent.client.ps.stats[Defines.STAT_TIMER] = (short)((ent.client.quad_framenum - GameBase.level.framenum) / 10);
		}
		else if (ent.client.invincible_framenum > GameBase.level.framenum)
		{
			ent.client.ps.stats[Defines.STAT_TIMER_ICON] = (short)GameBase.gi.imageindex("p_invulnerability");
			ent.client.ps.stats[Defines.STAT_TIMER] = (short)((ent.client.invincible_framenum - GameBase.level.framenum) / 10);
		}
		else if (ent.client.enviro_framenum > GameBase.level.framenum)
		{
			ent.client.ps.stats[Defines.STAT_TIMER_ICON] = (short)GameBase.gi.imageindex("p_envirosuit");
			ent.client.ps.stats[Defines.STAT_TIMER] = (short)((ent.client.enviro_framenum - GameBase.level.framenum) / 10);
		}
		else if (ent.client.breather_framenum > GameBase.level.framenum)
		{
			ent.client.ps.stats[Defines.STAT_TIMER_ICON] = (short)GameBase.gi.imageindex("p_rebreather");
			ent.client.ps.stats[Defines.STAT_TIMER] = (short)((ent.client.breather_framenum - GameBase.level.framenum) / 10);
		}
		else
		{
			ent.client.ps.stats[Defines.STAT_TIMER_ICON] = 0;
			ent.client.ps.stats[Defines.STAT_TIMER] = 0;
		}

		//
		// selected item
		//
		// bugfix rst
		if (ent.client.pers.selected_item <= 0)
			ent.client.ps.stats[Defines.STAT_SELECTED_ICON] = 0;
		else
			ent.client.ps.stats[Defines.STAT_SELECTED_ICON] = (short)GameBase.gi.imageindex(GameItemList.itemlist[ent.client.pers.selected_item].icon);

		ent.client.ps.stats[Defines.STAT_SELECTED_ITEM] = (short)ent.client.pers.selected_item;

		//
		// layouts
		//
		ent.client.ps.stats[Defines.STAT_LAYOUTS] = 0;

		if (GameBase.deathmatch.value != 0)
		{
			if (ent.client.pers.health <= 0 || GameBase.level.intermissiontime != 0 || ent.client.showscores)
				ent.client.ps.stats[Defines.STAT_LAYOUTS] |= 1;

			if (ent.client.showinventory && ent.client.pers.health > 0)
				ent.client.ps.stats[Defines.STAT_LAYOUTS] |= 2;
		}
		else
		{
			if (ent.client.showscores || ent.client.showhelp)
				ent.client.ps.stats[Defines.STAT_LAYOUTS] |= 1;

			if (ent.client.showinventory && ent.client.pers.health > 0)
				ent.client.ps.stats[Defines.STAT_LAYOUTS] |= 2;
		}

		//
		// frags
		//
		ent.client.ps.stats[Defines.STAT_FRAGS] = (short)ent.client.resp.score;

		//
		// help icon / current weapon if not shown
		//
		if (ent.client.pers.helpchanged != 0 && (GameBase.level.framenum & 8) != 0)
			ent.client.ps.stats[Defines.STAT_HELPICON] = (short)GameBase.gi.imageindex("i_help");
		else if ((ent.client.pers.hand == Defines.CENTER_HANDED || ent.client.ps.fov > 91) && ent.client.pers.weapon != null)
			ent.client.ps.stats[Defines.STAT_HELPICON] = (short)GameBase.gi.imageindex(ent.client.pers.weapon.icon);
		else
			ent.client.ps.stats[Defines.STAT_HELPICON] = 0;

		ent.client.ps.stats[Defines.STAT_SPECTATOR] = 0;
	}

	/*
	 * ===============
	 * G_CheckChaseStats
	 * ===============
	 */
	public static void G_CheckChaseStats(edict_t ent)
	{
		int i;
		gclient_t cl;

		for (i = 1; i <= GameBase.maxclients.value; i++)
		{
			cl = GameBase.g_edicts[i].client;

			if (!GameBase.g_edicts[i].inuse || cl.chase_target != ent)
				continue;

			//memcpy(cl.ps.stats, ent.client.ps.stats, sizeof(cl.ps.stats));
			Array.Copy(ent.client.ps.stats, 0, cl.ps.stats, 0, Defines.MAX_STATS);

			PlayerHud.G_SetSpectatorStats(GameBase.g_edicts[i]);
		}
	}

	/*
	 * ===============
	 * G_SetSpectatorStats
	 * ===============
	 */
	public static void G_SetSpectatorStats(edict_t ent)
	{
		var cl = ent.client;

		if (null == cl.chase_target)
			PlayerHud.G_SetStats(ent);

		cl.ps.stats[Defines.STAT_SPECTATOR] = 1;

		// layouts are independant in spectator
		cl.ps.stats[Defines.STAT_LAYOUTS] = 0;

		if (cl.pers.health <= 0 || GameBase.level.intermissiontime != 0 || cl.showscores)
			cl.ps.stats[Defines.STAT_LAYOUTS] |= 1;

		if (cl.showinventory && cl.pers.health > 0)
			cl.ps.stats[Defines.STAT_LAYOUTS] |= 2;

		if (cl.chase_target != null && cl.chase_target.inuse)

			//cl.ps.stats[STAT_CHASE] = (short) (CS_PLAYERSKINS +
			// (cl.chase_target - g_edicts) - 1);
			cl.ps.stats[Defines.STAT_CHASE] = (short)(Defines.CS_PLAYERSKINS + cl.chase_target.index - 1);
		else
			cl.ps.stats[Defines.STAT_CHASE] = 0;
	}

	/**
     * HelpComputer. Draws the help computer.
     */
	public static void HelpComputer(edict_t ent)
	{
		StringBuilder sb = new(256);
		string sk;

		if (GameBase.skill.value == 0)
			sk = "easy";
		else if (GameBase.skill.value == 1)
			sk = "medium";
		else if (GameBase.skill.value == 2)
			sk = "hard";
		else
			sk = "hard+";

		// send the layout
		sb.Append("xv 32 yv 8 picn help "); // background
		sb.Append("xv 202 yv 12 string2 \"").Append(sk).Append("\" "); // skill
		sb.Append("xv 0 yv 24 cstring2 \"").Append(GameBase.level.level_name).Append("\" "); // level name
		sb.Append("xv 0 yv 54 cstring2 \"").Append(GameBase.game.helpmessage1).Append("\" "); // help 1
		sb.Append("xv 0 yv 110 cstring2 \"").Append(GameBase.game.helpmessage2).Append("\" "); // help 2
		sb.Append("xv 50 yv 164 string2 \" kills     goals    secrets\" ");
		sb.Append("xv 50 yv 172 string2 \"");

		sb.Append(
			$"{GameBase.level.killed_monsters,3}/{GameBase.level.total_monsters,3}     {GameBase.level.found_goals}/{GameBase.level.total_goals}       {GameBase.level.found_secrets}/{GameBase.level.total_secrets}\" "
		);

		GameBase.gi.WriteByte(Defines.svc_layout);
		GameBase.gi.WriteString(sb.ToString());
		GameBase.gi.unicast(ent, true);
	}
}
