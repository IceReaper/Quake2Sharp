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

public class server_static_t
{
	public server_static_t()
	{
		for (var n = 0; n < Defines.MAX_CHALLENGES; n++)
			this.challenges[n] = new();
	}

	public bool initialized; // sv_init has completed
	public int realtime; // always increasing, no clamping, etc
	public string mapcmd = ""; // ie: *intro.cin+base
	public int spawncount; // incremented each server start

	// used to check late spawns
	public client_t[] clients; // [maxclients->value];
	public int num_client_entities; // maxclients->value*UPDATE_BACKUP*MAX_PACKET_ENTITIES
	public int next_client_entities; // next client_entity to use
	public entity_state_t[] client_entities; // [num_client_entities]
	public int last_heartbeat;
	public challenge_t[] challenges = new challenge_t[Defines.MAX_CHALLENGES]; // to

	// prevent
	// invalid
	// IPs
	// from
	// connecting

	// serverrecord values
	public BinaryWriter demofile;
	public sizebuf_t demo_multicast = new();
	public byte[] demo_multicast_buf = new byte[Defines.MAX_MSGLEN];
}