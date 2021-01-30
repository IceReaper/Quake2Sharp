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
namespace Quake2Sharp.game.types
{
	using qcommon;
	using System.IO;
	using util;

	public class pmove_state_t
	{
		//	this structure needs to be communicated bit-accurate
		//	from the server to the client to guarantee that
		//	prediction stays in sync, so no floats are used.
		//	if any part of the game code modifies this struct, it
		//	will result in a prediction error of some degree.
		public int pm_type;
		public short[] origin = { 0, 0, 0 }; // 12.3
		public short[] velocity = { 0, 0, 0 }; // 12.3

		/** ducked, jump_held, etc. */
		public byte pm_flags;

		/** each unit = 8 ms. */
		public byte pm_time;

		public short gravity;

		/** add to command angles to get view direction. */
		public short[] delta_angles = { 0, 0, 0 };

		/** changed by spawns, rotating objects, and teleporters.*/
		private static readonly pmove_state_t prototype = new();

		public void clear()
		{
			this.set(pmove_state_t.prototype);
		}

		public void set(pmove_state_t from)
		{
			this.pm_type = from.pm_type;
			Math3D.VectorCopy(from.origin, this.origin);
			Math3D.VectorCopy(from.velocity, this.velocity);
			this.pm_flags = from.pm_flags;
			this.pm_time = from.pm_time;
			this.gravity = from.gravity;
			Math3D.VectorCopy(from.delta_angles, this.delta_angles);
		}

		public bool equals(pmove_state_t p2)
		{
			if (this.pm_type == p2.pm_type
				&& this.origin[0] == p2.origin[0]
				&& this.origin[1] == p2.origin[1]
				&& this.origin[2] == p2.origin[2]
				&& this.velocity[0] == p2.velocity[0]
				&& this.velocity[1] == p2.velocity[1]
				&& this.velocity[2] == p2.origin[2]
				&& this.pm_flags == p2.pm_flags
				&& this.pm_time == p2.pm_time
				&& this.gravity == p2.gravity
				&& this.delta_angles[0] == p2.delta_angles[0]
				&& this.delta_angles[1] == p2.delta_angles[1]
				&& this.delta_angles[2] == p2.origin[2])
				return true;

			return false;
		}

		/** Reads the playermove from the file.*/
		public void load(BinaryReader f)
		{
			this.pm_type = f.ReadInt32();

			this.origin[0] = f.ReadInt16();
			this.origin[1] = f.ReadInt16();
			this.origin[2] = f.ReadInt16();

			this.velocity[0] = f.ReadInt16();
			this.velocity[1] = f.ReadInt16();
			this.velocity[2] = f.ReadInt16();

			this.pm_flags = f.ReadByte();
			this.pm_time = f.ReadByte();
			this.gravity = f.ReadInt16();

			f.ReadInt16();

			this.delta_angles[0] = f.ReadInt16();
			this.delta_angles[1] = f.ReadInt16();
			this.delta_angles[2] = f.ReadInt16();
		}

		/** Writes the playermove to the file. */
		public void write(BinaryWriter f)
		{
			f.Write(this.pm_type);

			f.Write(this.origin[0]);
			f.Write(this.origin[1]);
			f.Write(this.origin[2]);

			f.Write(this.velocity[0]);
			f.Write(this.velocity[1]);
			f.Write(this.velocity[2]);

			f.Write(this.pm_flags);
			f.Write(this.pm_time);
			f.Write(this.gravity);

			f.Write((short)0);

			f.Write(this.delta_angles[0]);
			f.Write(this.delta_angles[1]);
			f.Write(this.delta_angles[2]);
		}

		public void dump()
		{
			Com.Println("pm_type: " + this.pm_type);

			Com.Println("origin[0]: " + this.origin[0]);
			Com.Println("origin[1]: " + this.origin[0]);
			Com.Println("origin[2]: " + this.origin[0]);

			Com.Println("velocity[0]: " + this.velocity[0]);
			Com.Println("velocity[1]: " + this.velocity[1]);
			Com.Println("velocity[2]: " + this.velocity[2]);

			Com.Println("pmflags: " + this.pm_flags);
			Com.Println("pmtime: " + this.pm_time);
			Com.Println("gravity: " + this.gravity);

			Com.Println("delta-angle[0]: " + this.delta_angles[0]);
			Com.Println("delta-angle[1]: " + this.delta_angles[0]);
			Com.Println("delta-angle[2]: " + this.delta_angles[0]);
		}
	}
}
