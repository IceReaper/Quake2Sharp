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
	using System;
	using System.IO;
	using util;

	/** 
 	Player_state_t is the information needed in addition to pmove_state_t
	to rendered a view.  There will only be 10 player_state_t sent each second,
	but the number of pmove_state_t changes will be relative to client
	frame rates.
*/
	public class player_state_t
	{
		public pmove_state_t pmove = new(); // for prediction

		// these fields do not need to be communicated bit-precise
		public float[] viewangles = { 0, 0, 0 }; // for fixed views
		public float[] viewoffset = { 0, 0, 0 }; // add to pmovestate->origin
		public float[] kick_angles = { 0, 0, 0 }; // add to view direction to get render angles

		// set by weapon kicks, pain effects, etc
		public float[] gunangles = { 0, 0, 0 };
		public float[] gunoffset = { 0, 0, 0 };
		public int gunindex;
		public int gunframe;
		public float[] blend = new float[4]; // rgba full screen effect
		public float fov; // horizontal field of view
		public int rdflags; // refdef flags
		public short[] stats = new short[Defines.MAX_STATS];

		/** Lets cleverly reset the structure. */
		private static readonly player_state_t prototype = new();

		/** Clears the player_state.*/
		public void clear()
		{
			this.set(player_state_t.prototype);
		}

		/** Clones the object.*/
		public player_state_t getClone()
		{
			return new player_state_t().set(this);
		}

		/** Copies the player state data. */
		public player_state_t set(player_state_t from)
		{
			this.pmove.set(from.pmove);
			Math3D.VectorCopy(from.viewangles, this.viewangles);
			Math3D.VectorCopy(from.viewoffset, this.viewoffset);
			Math3D.VectorCopy(from.kick_angles, this.kick_angles);

			Math3D.VectorCopy(from.gunangles, this.gunangles);
			Math3D.VectorCopy(from.gunoffset, this.gunoffset);

			this.gunindex = from.gunindex;
			this.gunframe = from.gunframe;

			this.blend[0] = from.blend[0];
			this.blend[1] = from.blend[1];
			this.blend[2] = from.blend[2];
			this.blend[3] = from.blend[3];

			this.fov = from.fov;
			this.rdflags = from.rdflags;

			Array.Copy(from.stats, 0, this.stats, 0, Defines.MAX_STATS);

			return this;
		}

		/** Reads a player_state from a file.*/
		public void load(BinaryReader f)
		{
			this.pmove.load(f);

			this.viewangles[0] = f.ReadSingle();
			this.viewangles[1] = f.ReadSingle();
			this.viewangles[2] = f.ReadSingle();

			this.viewoffset[0] = f.ReadSingle();
			this.viewoffset[1] = f.ReadSingle();
			this.viewoffset[2] = f.ReadSingle();

			this.kick_angles[0] = f.ReadSingle();
			this.kick_angles[1] = f.ReadSingle();
			this.kick_angles[2] = f.ReadSingle();

			this.gunangles[0] = f.ReadSingle();
			this.gunangles[1] = f.ReadSingle();
			this.gunangles[2] = f.ReadSingle();

			this.gunoffset[0] = f.ReadSingle();
			this.gunoffset[1] = f.ReadSingle();
			this.gunoffset[2] = f.ReadSingle();

			this.gunindex = f.ReadInt32();
			this.gunframe = f.ReadInt32();

			this.blend[0] = f.ReadSingle();
			this.blend[1] = f.ReadSingle();
			this.blend[2] = f.ReadSingle();
			this.blend[3] = f.ReadSingle();

			this.fov = f.ReadSingle();

			this.rdflags = f.ReadInt32();

			for (var n = 0; n < Defines.MAX_STATS; n++)
				this.stats[n] = f.ReadInt16();
		}

		/** Writes a player_state to a file.*/
		public void write(BinaryWriter f)
		{
			this.pmove.write(f);

			f.Write(this.viewangles[0]);
			f.Write(this.viewangles[1]);
			f.Write(this.viewangles[2]);

			f.Write(this.viewoffset[0]);
			f.Write(this.viewoffset[1]);
			f.Write(this.viewoffset[2]);

			f.Write(this.kick_angles[0]);
			f.Write(this.kick_angles[1]);
			f.Write(this.kick_angles[2]);

			f.Write(this.gunangles[0]);
			f.Write(this.gunangles[1]);
			f.Write(this.gunangles[2]);

			f.Write(this.gunoffset[0]);
			f.Write(this.gunoffset[1]);
			f.Write(this.gunoffset[2]);

			f.Write(this.gunindex);
			f.Write(this.gunframe);

			f.Write(this.blend[0]);
			f.Write(this.blend[1]);
			f.Write(this.blend[2]);
			f.Write(this.blend[3]);

			f.Write(this.fov);

			f.Write(this.rdflags);

			for (var n = 0; n < Defines.MAX_STATS; n++)
				f.Write(this.stats[n]);
		}

		/** Prints the player state. */
		public void dump()
		{
			this.pmove.dump();

			Lib.printv("viewangles", this.viewangles);
			Lib.printv("viewoffset", this.viewoffset);
			Lib.printv("kick_angles", this.kick_angles);
			Lib.printv("gunangles", this.gunangles);
			Lib.printv("gunoffset", this.gunoffset);

			Com.Println("gunindex: " + this.gunindex);
			Com.Println("gunframe: " + this.gunframe);

			Lib.printv("blend", this.blend);

			Com.Println("fov: " + this.fov);

			Com.Println("rdflags: " + this.rdflags);

			for (var n = 0; n < Defines.MAX_STATS; n++)
				Console.WriteLine("stats[" + n + "]: " + this.stats[n]);
		}
	}
}
