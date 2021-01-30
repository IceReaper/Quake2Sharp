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
	using System;
	using util;

	public class pmove_t
	{
		// state (in / out)
		public pmove_state_t s = new();

		// command (in)
		public usercmd_t cmd = new();
		public bool snapinitial; // if s has been changed outside pmove

		// results (out)
		public int numtouch;
		public edict_t[] touchents = new edict_t[Defines.MAXTOUCH];
		public float[] viewangles = { 0, 0, 0 }; // clamped
		public float viewheight;
		public float[] mins = { 0, 0, 0 }, maxs = { 0, 0, 0 }; // bounding box size
		public edict_t groundentity;
		public int watertype;
		public int waterlevel;
		public Func<float[], float[], float[], float[], trace_t> trace;
		public Func<float[], int> pointcontents;

		// pmove->pm_flags
		public static readonly int PMF_DUCKED = 1;
		public static readonly int PMF_JUMP_HELD = 2;
		public static readonly int PMF_ON_GROUND = 4;
		public static readonly int PMF_TIME_WATERJUMP = 8; // pm_time is waterjump
		public static readonly int PMF_TIME_LAND = 16; // pm_time is time before rejump
		public static readonly int PMF_TIME_TELEPORT = 32; // pm_time is non-moving

		// time
		public static readonly int PMF_NO_PREDICTION = 64; // temporarily disables

		// prediction (used for
		// grappling hook)

		public void clear()
		{
			this.groundentity = null;
			this.waterlevel = this.watertype = 0;
			this.trace = null;
			this.pointcontents = null;
			Math3D.VectorClear(this.mins);
			Math3D.VectorClear(this.maxs);
			this.viewheight = 0;
			Math3D.VectorClear(this.viewangles);
			Array.Fill(this.touchents, null);
			this.numtouch = 0;
			this.snapinitial = false;
			this.cmd.clear();
			this.s.clear();
		}
	}
}
