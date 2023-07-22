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

using Quake2Sharp.game.adapters;
using Quake2Sharp.util;

namespace Quake2Sharp.game.types;

public class moveinfo_t
{
	// fixed data
	public float[] start_origin = { 0, 0, 0 };
	public float[] start_angles = { 0, 0, 0 };
	public float[] end_origin = { 0, 0, 0 };
	public float[] end_angles = { 0, 0, 0 };
	public int sound_start;
	public int sound_middle;
	public int sound_end;
	public float accel;
	public float speed;
	public float decel;
	public float distance;
	public float wait;

	// state data
	public int state;
	public float[] dir = { 0, 0, 0 };
	public float current_speed;
	public float move_speed;
	public float next_speed;
	public float remaining_distance;
	public float decel_distance;
	public EntThinkAdapter endfunc;

	/** saves the moveinfo to the file.*/
	public void write(BinaryWriter f)
	{
		f.Write(this.start_origin);
		f.Write(this.start_angles);
		f.Write(this.end_origin);
		f.Write(this.end_angles);

		f.Write(this.sound_start);
		f.Write(this.sound_middle);
		f.Write(this.sound_end);

		f.Write(this.accel);
		f.Write(this.speed);
		f.Write(this.decel);
		f.Write(this.distance);

		f.Write(this.wait);

		f.Write(this.state);
		f.Write(this.dir);

		f.Write(this.current_speed);
		f.Write(this.move_speed);
		f.Write(this.next_speed);
		f.Write(this.remaining_distance);
		f.Write(this.decel_distance);
		f.Write(this.endfunc);
	}

	/** Reads the moveinfo from a file. */
	public void read(BinaryReader f)
	{
		this.start_origin = f.ReadVector();
		this.start_angles = f.ReadVector();
		this.end_origin = f.ReadVector();
		this.end_angles = f.ReadVector();

		this.sound_start = f.ReadInt32();
		this.sound_middle = f.ReadInt32();
		this.sound_end = f.ReadInt32();

		this.accel = f.ReadSingle();
		this.speed = f.ReadSingle();
		this.decel = f.ReadSingle();
		this.distance = f.ReadSingle();

		this.wait = f.ReadSingle();

		this.state = f.ReadInt32();
		this.dir = f.ReadVector();

		this.current_speed = f.ReadSingle();
		this.move_speed = f.ReadSingle();
		this.next_speed = f.ReadSingle();
		this.remaining_distance = f.ReadSingle();
		this.decel_distance = f.ReadSingle();
		this.endfunc = (EntThinkAdapter)f.ReadAdapter();
	}
}