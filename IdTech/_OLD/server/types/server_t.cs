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
using Quake2Sharp.qcommon.types;

namespace Quake2Sharp.server.types;

public class server_t
{
	public server_t()
	{
		this.models = new cmodel_t[Defines.MAX_MODELS];

		for (var n = 0; n < Defines.MAX_MODELS; n++)
			this.models[n] = new();

		for (var n = 0; n < Defines.MAX_EDICTS; n++)
			this.baselines[n] = new(null);
	}

	public int state; // precache commands are only valid during load
	public bool attractloop; // running cinematics and demos for the local system

	// only
	public bool loadgame; // client begins should reuse existing entity
	public int time; // always sv.framenum * 100 msec
	public int framenum;
	public string name = ""; // map name, or cinematic name
	public cmodel_t[] models;
	public string[] configstrings = new string[Defines.MAX_CONFIGSTRINGS];
	public entity_state_t[] baselines = new entity_state_t[Defines.MAX_EDICTS];

	// the multicast buffer is used to send a message to a set of clients
	// it is only used to marshall data until SV_Multicast is called
	public sizebuf_t multicast = new();
	public byte[] multicast_buf = new byte[Defines.MAX_MSGLEN];

	// demo server information
	public BinaryReader demofile;
}