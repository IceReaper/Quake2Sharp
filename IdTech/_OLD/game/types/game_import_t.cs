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
using Quake2Sharp.qcommon;
using Quake2Sharp.server;

namespace Quake2Sharp.game.types;

//
//	collection of functions provided by the main engine
//
public class game_import_t
{
	// special messages
	public void bprintf(int printlevel, string s)
	{
		SV_SEND.SV_BroadcastPrintf(printlevel, s);
	}

	public void dprintf(string s)
	{
		SV_GAME.PF_dprintf(s);
	}

	public void cprintf(edict_t ent, int printlevel, string s)
	{
		SV_GAME.PF_cprintf(ent, printlevel, s);
	}

	public void centerprintf(edict_t ent, string s)
	{
		SV_GAME.PF_centerprintf(ent, s);
	}

	public void sound(edict_t ent, int channel, int soundindex, float volume, float attenuation, float timeofs)
	{
		SV_GAME.PF_StartSound(ent, channel, soundindex, volume, attenuation, timeofs);
	}

	public void positioned_sound(float[] origin, edict_t ent, int channel, int soundinedex, float volume, float attenuation, float timeofs)
	{
		SV_SEND.SV_StartSound(origin, ent, channel, soundinedex, volume, attenuation, timeofs);
	}

	// config strings hold all the index strings, the lightstyles,
	// and misc data like the sky definition and cdtrack.
	// All of the current configstrings are sent to clients when
	// they connect, and changes are sent to all connected clients.
	public void configstring(int num, string @string)
	{
		SV_GAME.PF_Configstring(num, @string);
	}

	public void error(string err)
	{
		clientserver.Com_Error(Defines.ERR_FATAL, err);
	}

	public void error(int level, string err)
	{
		SV_GAME.PF_error(level, err);
	}

	// the *index functions create configstrings and some internal server state
	public int modelindex(string name)
	{
		return SV_INIT.SV_ModelIndex(name);
	}

	public int soundindex(string name)
	{
		return SV_INIT.SV_SoundIndex(name);
	}

	public int imageindex(string name)
	{
		return SV_INIT.SV_ImageIndex(name);
	}

	public void setmodel(edict_t ent, string name)
	{
		SV_GAME.PF_setmodel(ent, name);
	}

	// collision detection
	public trace_t trace(float[] start, float[] mins, float[] maxs, float[] end, edict_t passent, int contentmask)
	{
		return SV_WORLD.SV_Trace(start, mins, maxs, end, passent, contentmask);
	}

	public Func<float[], int> pointcontents = o => { return 0; };

	public bool inPHS(float[] p1, float[] p2)
	{
		return SV_GAME.PF_inPHS(p1, p2);
	}

	public void SetAreaPortalState(int portalnum, bool open)
	{
		CM.CM_SetAreaPortalState(portalnum, open);
	}

	public bool AreasConnected(int area1, int area2)
	{
		return CM.CM_AreasConnected(area1, area2);
	}

	// an entity will never be sent to a client or used for collision
	// if it is not passed to linkentity. If the size, position, or
	// solidity changes, it must be relinked.
	public void linkentity(edict_t ent)
	{
		SV_WORLD.SV_LinkEdict(ent);
	}

	public void unlinkentity(edict_t ent)
	{
		SV_WORLD.SV_UnlinkEdict(ent);
	}

	// call before removing an interactive edict
	public int BoxEdicts(float[] mins, float[] maxs, edict_t[] list, int maxcount, int areatype)
	{
		return SV_WORLD.SV_AreaEdicts(mins, maxs, list, maxcount, areatype);
	}

	public void Pmove(pmove_t pmove)
	{
		IdTech.common.pmove.Pmove(pmove);
	}

	// player movement code common with client prediction
	// network messaging
	public void multicast(float[] origin, int to)
	{
		SV_SEND.SV_Multicast(origin, to);
	}

	public void unicast(edict_t ent, bool reliable)
	{
		SV_GAME.PF_Unicast(ent, reliable);
	}

	public void WriteByte(int c)
	{
		SV_GAME.PF_WriteByte(c);
	}

	public void WriteShort(int c)
	{
		SV_GAME.PF_WriteShort(c);
	}

	public void WriteString(string s)
	{
		SV_GAME.PF_WriteString(s);
	}

	public void WritePosition(float[] pos)
	{
		SV_GAME.PF_WritePos(pos);
	}

	// some fractional bits
	public void WriteDir(float[] pos)
	{
		SV_GAME.PF_WriteDir(pos);
	}

	// console variable interaction
	public cvar_t cvar(string var_name, string value, int flags)
	{
		return IdTech.common.cvar.Cvar_Get(var_name, value, flags);
	}

	// console variable interaction
	public cvar_t cvar_set(string var_name, string value)
	{
		return IdTech.common.cvar.Cvar_Set(var_name, value);
	}

	// console variable interaction
	public cvar_t cvar_forceset(string var_name, string value)
	{
		return IdTech.common.cvar.Cvar_ForceSet(var_name, value);
	}

	// ClientCommand and ServerCommand parameter access
	public int argc()
	{
		return cmdparser.Cmd_Argc();
	}

	public string argv(int n)
	{
		return cmdparser.Cmd_Argv(n);
	}

	// concatenation of all argv >= 1
	public string args()
	{
		return cmdparser.Cmd_Args();
	}

	// add commands to the server console as if they were typed in
	// for map changing, etc
	public void AddCommandString(string text)
	{
		cmdparser.Cbuf_AddText(text);
	}
}
