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
namespace Quake2Sharp.client.types;

using game.types;

public class frame_t
{
	public static readonly int MAX_MAP_AREAS = 256;
	public bool valid; // cleared if delta parsing was invalid
	public int serverframe;
	public int servertime; // server time the message is valid for (in msec)
	public int deltaframe;
	public byte[] areabits = new byte [frame_t.MAX_MAP_AREAS / 8]; // portalarea visibility bits
	public player_state_t playerstate = new(); // mem
	public int num_entities;
	public int parse_entities; // non-masked index into cl_parse_entities array

	public void set(frame_t from)
	{
		this.valid = from.valid;
		this.serverframe = from.serverframe;
		this.deltaframe = from.deltaframe;
		this.num_entities = from.num_entities;
		this.parse_entities = from.parse_entities;
		Array.Copy(from.areabits, 0, this.areabits, 0, this.areabits.Length);
		this.playerstate.set(from.playerstate);
	}

	public void reset()
	{
		this.valid = false;
		this.serverframe = this.servertime = this.deltaframe = 0;
		Array.Fill(this.areabits, (byte)0);
		this.playerstate.clear();
		this.num_entities = this.parse_entities = 0;
	}
}