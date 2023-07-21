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

public class cinematics_t
{
	public bool restart_sound;
	public int s_rate;
	public int s_width;
	public int s_channels;
	public int width;
	public int height;
	public byte[] pic;
	public byte[] pic_pending;

	// order 1 huffman stuff
	public int[] hnodes1; // [256][256][2];
	public readonly int[] numhnodes1 = new int[256];
	public readonly int[] h_used = new int[512];
	public readonly int[] h_count = new int[512];
}