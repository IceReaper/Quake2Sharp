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
using Quake2Sharp.game.types;
using Quake2Sharp.qcommon;
using Quake2Sharp.util;

namespace Quake2Sharp.client;

/**
 * CL_pred
 */
public class CL_pred
{
	/*
	 * =================== CL_CheckPredictionError ===================
	 */
	public static void CheckPredictionError()
	{
		int frame;
		var delta = new int[3];
		int i;
		int len;

		if (Globals.cl_predict.value == 0.0f || (Globals.cl.frame.playerstate.pmove.pm_flags & pmove_t.PMF_NO_PREDICTION) != 0)
			return;

		// calculate the last usercmd_t we sent that the server has processed
		frame = Globals.cls.netchan.incoming_acknowledged;
		frame &= Defines.CMD_BACKUP - 1;

		// compare what the server returned with what we had predicted it to be
		Math3D.VectorSubtract(Globals.cl.frame.playerstate.pmove.origin, Globals.cl.predicted_origins[frame], delta);

		// save the prediction error for interpolation
		len = Math.Abs(delta[0]) + Math.Abs(delta[1]) + Math.Abs(delta[2]);

		if (len > 640) // 80 world units
		{
			// a teleport or something
			Math3D.VectorClear(Globals.cl.prediction_error);
		}
		else
		{
			if (Globals.cl_showmiss.value != 0.0f && (delta[0] != 0 || delta[1] != 0 || delta[2] != 0))
				clientserver.Com_Printf("prediction miss on " + Globals.cl.frame.serverframe + ": " + (delta[0] + delta[1] + delta[2]) + "\n");

			Math3D.VectorCopy(Globals.cl.frame.playerstate.pmove.origin, Globals.cl.predicted_origins[frame]);

			// save for error itnerpolation
			for (i = 0; i < 3; i++)
				Globals.cl.prediction_error[i] = delta[i] * 0.125f;
		}
	}

	/*
	 * ==================== CL_ClipMoveToEntities
	 *
	 * ====================
	 */
	private static void ClipMoveToEntities(float[] start, float[] mins, float[] maxs, float[] end, trace_t tr)
	{
		int i, x, zd, zu;
		trace_t trace;
		int headnode;
		float[] angles;
		entity_state_t ent;
		int num;
		cmodel_t cmodel;
		var bmins = new float[3];
		var bmaxs = new float[3];

		for (i = 0; i < Globals.cl.frame.num_entities; i++)
		{
			num = (Globals.cl.frame.parse_entities + i) & (Defines.MAX_PARSE_ENTITIES - 1);
			ent = Globals.cl_parse_entities[num];

			if (ent.solid == 0)
				continue;

			if (ent.number == Globals.cl.playernum + 1)
				continue;

			if (ent.solid == 31)
			{
				// special value for bmodel
				cmodel = Globals.cl.model_clip[ent.modelindex];

				if (cmodel == null)
					continue;

				headnode = cmodel.headnode;
				angles = ent.angles;
			}
			else
			{
				// encoded bbox
				x = 8 * (ent.solid & 31);
				zd = (int)(8 * (((uint)ent.solid >> 5) & 31));
				zu = (int)(8 * (((uint)ent.solid >> 10) & 63) - 32);
				bmins[0] = bmins[1] = -x;
				bmaxs[0] = bmaxs[1] = x;
				bmins[2] = -zd;
				bmaxs[2] = zu;
				headnode = CM.HeadnodeForBox(bmins, bmaxs);
				angles = Globals.vec3_origin; // boxes don't rotate
			}

			if (tr.allsolid)
				return;

			trace = CM.TransformedBoxTrace(start, end, mins, maxs, headnode, Defines.MASK_PLAYERSOLID, ent.origin, angles);

			if (trace.allsolid || trace.startsolid || trace.fraction < tr.fraction)
			{
				trace.ent = ent.surrounding_ent;

				if (tr.startsolid)
				{
					tr.set(trace); // rst: solved the Z U P P E L - P R O B L E

					// M
					tr.startsolid = true;
				}
				else
					tr.set(trace); // rst: solved the Z U P P E L - P R O B L E

				// M
			}
			else if (trace.startsolid)
				tr.startsolid = true;
		}
	}

	/*
	 * ================ CL_PMTrace ================
	 */
	public static edict_t DUMMY_ENT = new(-1);

	private static trace_t PMTrace(float[] start, float[] mins, float[] maxs, float[] end)
	{
		trace_t t;

		// check against world
		t = CM.BoxTrace(start, end, mins, maxs, 0, Defines.MASK_PLAYERSOLID);

		if (t.fraction < 1.0f)
			t.ent = CL_pred.DUMMY_ENT;

		// check all other solid models
		CL_pred.ClipMoveToEntities(start, mins, maxs, end, t);

		return t;
	}

	/*
	 * ================= PMpointcontents
	 *
	 * Returns the content identificator of the point. =================
	 */
	private static int PMpointcontents(float[] point)
	{
		int i;
		entity_state_t ent;
		int num;
		cmodel_t cmodel;
		int contents;
		contents = CM.PointContents(point, 0);

		for (i = 0; i < Globals.cl.frame.num_entities; i++)
		{
			num = (Globals.cl.frame.parse_entities + i) & (Defines.MAX_PARSE_ENTITIES - 1);
			ent = Globals.cl_parse_entities[num];

			if (ent.solid != 31) // special value for bmodel
				continue;

			cmodel = Globals.cl.model_clip[ent.modelindex];

			if (cmodel == null)
				continue;

			contents |= CM.TransformedPointContents(point, cmodel.headnode, ent.origin, ent.angles);
		}

		return contents;
	}

	/*
	 * ================= CL_PredictMovement
	 *
	 * Sets cl.predicted_origin and cl.predicted_angles =================
	 */
	public static void PredictMovement()
	{
		if (Globals.cls.state != Defines.ca_active)
			return;

		if (Globals.cl_paused.value != 0.0f)
			return;

		if (Globals.cl_predict.value == 0.0f || (Globals.cl.frame.playerstate.pmove.pm_flags & pmove_t.PMF_NO_PREDICTION) != 0)
		{
			// just set angles
			for (var i = 0; i < 3; i++)
				Globals.cl.predicted_angles[i] = Globals.cl.viewangles[i] + Math3D.SHORT2ANGLE(Globals.cl.frame.playerstate.pmove.delta_angles[i]);

			return;
		}

		var ack = Globals.cls.netchan.incoming_acknowledged;
		var current = Globals.cls.netchan.outgoing_sequence;

		// if we are too far out of date, just freeze
		if (current - ack >= Defines.CMD_BACKUP)
		{
			if (Globals.cl_showmiss.value != 0.0f)
				clientserver.Com_Printf("exceeded CMD_BACKUP\n");

			return;
		}

		// copy current state to pmove
		//memset (pm, 0, sizeof(pm));
		pmove_t pm = new();
		pm.trace = (start, mins, maxs, end) => { return CL_pred.PMTrace(start, mins, maxs, end); };
		pm.pointcontents = point => { return CL_pred.PMpointcontents(point); };
		pmove.pm_airaccelerate = Lib.atof(Globals.cl.configstrings[Defines.CS_AIRACCEL]);

		// bugfix (rst) yeah !!!!!!!! found the solution to the B E W E G U N G
		// S P R O B L E M.
		pm.s.set(Globals.cl.frame.playerstate.pmove);

		// SCR_DebugGraph (current - ack - 1, 0);
		var frame = 0;

		// run frames
		usercmd_t cmd;

		while (++ack < current)
		{
			frame = ack & (Defines.CMD_BACKUP - 1);
			cmd = Globals.cl.cmds[frame];
			pm.cmd.set(cmd);
			pmove.Pmove(pm);

			// save for debug checking
			Math3D.VectorCopy(pm.s.origin, Globals.cl.predicted_origins[frame]);
		}

		var oldframe = (ack - 2) & (Defines.CMD_BACKUP - 1);
		int oldz = Globals.cl.predicted_origins[oldframe][2];
		var step = pm.s.origin[2] - oldz;

		if (step > 63 && step < 160 && (pm.s.pm_flags & pmove_t.PMF_ON_GROUND) != 0)
		{
			Globals.cl.predicted_step = step * 0.125f;
			Globals.cl.predicted_step_time = (int)(Globals.cls.realtime - Globals.cls.frametime * 500);
		}

		// copy results out for rendering
		Globals.cl.predicted_origin[0] = pm.s.origin[0] * 0.125f;
		Globals.cl.predicted_origin[1] = pm.s.origin[1] * 0.125f;
		Globals.cl.predicted_origin[2] = pm.s.origin[2] * 0.125f;
		Math3D.VectorCopy(pm.viewangles, Globals.cl.predicted_angles);
	}
}
