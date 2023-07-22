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

using IdTech.backend;
using IdTech.common;
using Quake2Sharp.client.types;
using Quake2Sharp.game.types;
using Quake2Sharp.util;
using System.Text;

namespace Quake2Sharp.client;

/**
 * V
 */
public class V
{
	private static cvar_t cl_testblend;
	private static cvar_t cl_testparticles;
	private static cvar_t cl_testentities;
	private static cvar_t cl_testlights;
	private static cvar_t cl_stats;
	private static int r_numdlights;
	private static readonly dlight_t[] r_dlights = new dlight_t[Defines.MAX_DLIGHTS];
	private static int r_numentities;
	private static readonly entity_t[] r_entities = new entity_t[Defines.MAX_ENTITIES];
	private static int r_numparticles;

	//static particle_t[] r_particles = new particle_t[MAX_PARTICLES];
	private static readonly lightstyle_t[] r_lightstyles = new lightstyle_t[Defines.MAX_LIGHTSTYLES];

	static V()
	{
		for (var i = 0; i < V.r_dlights.Length; i++)
			V.r_dlights[i] = new();

		for (var i = 0; i < V.r_entities.Length; i++)
			V.r_entities[i] = new();

		for (var i = 0; i < V.r_lightstyles.Length; i++)
			V.r_lightstyles[i] = new();
	}

	/*
	 * ==================== V_ClearScene
	 *
	 * Specifies the model that will be used as the world ====================
	 */
	private static void ClearScene()
	{
		V.r_numdlights = 0;
		V.r_numentities = 0;
		V.r_numparticles = 0;
	}

	/*
	 * ===================== V_AddEntity
	 *
	 * =====================
	 */
	public static void AddEntity(entity_t ent)
	{
		if (V.r_numentities >= Defines.MAX_ENTITIES)
			return;

		V.r_entities[V.r_numentities++].set(ent);
	}

	/*
	 * ===================== V_AddParticle
	 *
	 * =====================
	 */
	public static void AddParticle(float[] org, int color, float alpha)
	{
		if (V.r_numparticles >= Defines.MAX_PARTICLES)
			return;

		var i = V.r_numparticles++;
		var c = particle_t.colorTable[color];
		c |= (int)(alpha * 255) << 24;
		particle_t.colorArray[i] = c;
		i *= 3;
		particle_t.vertexArray[i++] = org[0];
		particle_t.vertexArray[i++] = org[1];
		particle_t.vertexArray[i++] = org[2];
	}

	/*
	 * ===================== V_AddLight
	 *
	 * =====================
	 */
	public static void AddLight(float[] org, float intensity, float r, float g, float b)
	{
		dlight_t dl;

		if (V.r_numdlights >= Defines.MAX_DLIGHTS)
			return;

		dl = V.r_dlights[V.r_numdlights++];
		Math3D.VectorCopy(org, dl.origin);
		dl.intensity = intensity;
		dl.color[0] = r;
		dl.color[1] = g;
		dl.color[2] = b;
	}

	/*
	 * ===================== V_AddLightStyle
	 *
	 * =====================
	 */
	public static void AddLightStyle(int style, float r, float g, float b)
	{
		lightstyle_t ls;

		if (style < 0 || style > Defines.MAX_LIGHTSTYLES)
			clientserver.Com_Error(Defines.ERR_DROP, "Bad light style " + style);

		ls = V.r_lightstyles[style];
		ls.white = r + g + b;
		ls.rgb[0] = r;
		ls.rgb[1] = g;
		ls.rgb[2] = b;
	}

	// stack variable
	private static readonly float[] origin = { 0, 0, 0 };

	/*
	 * ================ V_TestParticles
	 *
	 * If cl_testparticles is set, create 4096 particles in the view
	 * ================
	 */
	private static void TestParticles()
	{
		int i, j;
		float d, r, u;
		V.r_numparticles = 0;

		for (i = 0; i < Defines.MAX_PARTICLES; i++)
		{
			d = i * 0.25f;
			r = 4 * ((i & 7) - 3.5f);
			u = 4 * (((i >> 3) & 7) - 3.5f);

			for (j = 0; j < 3; j++)
				V.origin[j] = Globals.cl.refdef.vieworg[j] + Globals.cl.v_forward[j] * d + Globals.cl.v_right[j] * r + Globals.cl.v_up[j] * u;

			V.AddParticle(V.origin, 8, V.cl_testparticles.value);
		}
	}

	/*
	 * ================ V_TestEntities
	 *
	 * If cl_testentities is set, create 32 player models ================
	 */
	private static void TestEntities()
	{
		int i, j;
		float f, r;
		entity_t ent;
		V.r_numentities = 32;

		//memset (r_entities, 0, sizeof(r_entities));
		for (i = 0; i < V.r_entities.Length; i++)
			V.r_entities[i].clear();

		for (i = 0; i < V.r_numentities; i++)
		{
			ent = V.r_entities[i];
			r = 64 * (i % 4 - 1.5f);
			f = 64 * (i / 4) + 128;

			for (j = 0; j < 3; j++)
				ent.origin[j] = Globals.cl.refdef.vieworg[j] + Globals.cl.v_forward[j] * f + Globals.cl.v_right[j] * r;

			ent.model = Globals.cl.baseclientinfo.model;
			ent.skin = Globals.cl.baseclientinfo.skin;
		}
	}

	/*
	 * ================ V_TestLights
	 *
	 * If cl_testlights is set, create 32 lights models ================
	 */
	private static void TestLights()
	{
		int i, j;
		float f, r;
		dlight_t dl;
		V.r_numdlights = 32;

		//memset (r_dlights, 0, sizeof(r_dlights));
		for (i = 0; i < V.r_dlights.Length; i++)
			V.r_dlights[i] = new();

		for (i = 0; i < V.r_numdlights; i++)
		{
			dl = V.r_dlights[i];
			r = 64 * (i % 4 - 1.5f);
			f = 64 * (i / 4) + 128;

			for (j = 0; j < 3; j++)
				dl.origin[j] = Globals.cl.refdef.vieworg[j] + Globals.cl.v_forward[j] * f + Globals.cl.v_right[j] * r;

			dl.color[0] = (i % 6 + 1) & 1;
			dl.color[1] = ((i % 6 + 1) & 2) >> 1;
			dl.color[2] = ((i % 6 + 1) & 4) >> 2;
			dl.intensity = 200;
		}
	}

	private static readonly Action Gun_Next_f = () =>
	{
		Globals.gun_frame++;
		clientserver.Com_Printf("frame " + Globals.gun_frame + "\n");
	};

	private static readonly Action Gun_Prev_f = () =>
	{
		Globals.gun_frame--;

		if (Globals.gun_frame < 0)
			Globals.gun_frame = 0;

		clientserver.Com_Printf("frame " + Globals.gun_frame + "\n");
	};

	private static readonly Action Gun_Model_f = () =>
	{
		if (cmdparser.Cmd_Argc() != 2)
		{
			Globals.gun_model = null;

			return;
		}

		var name = "models/" + cmdparser.Cmd_Argv(1) + "/tris.md2";
		Globals.gun_model = Globals.re.RegisterModel(name);
	};

	/*
	 * ================== V_RenderView
	 *
	 * ==================
	 */
	public static void RenderView(float stereo_separation)
	{
		//		extern int entitycmpfnc( const entity_t *, const entity_t * );
		//
		if (Globals.cls.state != Defines.ca_active)
			return;

		if (!Globals.cl.refresh_prepped)
			return; // still loading

		if (frame.cl_timedemo.value != 0.0f)
		{
			if (Globals.cl.timedemo_start == 0)
				Globals.cl.timedemo_start = system.Sys_Milliseconds();

			Globals.cl.timedemo_frames++;
		}

		// an invalid frame will just use the exact previous refdef
		// we can't use the old frame if the video mode has changed, though...
		if (Globals.cl.frame.valid && (Globals.cl.force_refdef || Globals.cl_paused.value == 0.0f))
		{
			Globals.cl.force_refdef = false;
			V.ClearScene();

			// build a refresh entity list and calc cl.sim*
			// this also calls CL_CalcViewValues which loads
			// v_forward, etc.
			CL_ents.AddEntities();

			if (V.cl_testparticles.value != 0.0f)
				V.TestParticles();

			if (V.cl_testentities.value != 0.0f)
				V.TestEntities();

			if (V.cl_testlights.value != 0.0f)
				V.TestLights();

			if (V.cl_testblend.value != 0.0f)
			{
				Globals.cl.refdef.blend[0] = 1.0f;
				Globals.cl.refdef.blend[1] = 0.5f;
				Globals.cl.refdef.blend[2] = 0.25f;
				Globals.cl.refdef.blend[3] = 0.5f;
			}

			// offset vieworg appropriately if we're doing stereo separation
			if (stereo_separation != 0)
			{
				var tmp = new float[3];
				Math3D.VectorScale(Globals.cl.v_right, stereo_separation, tmp);
				Math3D.VectorAdd(Globals.cl.refdef.vieworg, tmp, Globals.cl.refdef.vieworg);
			}

			// never let it sit exactly on a node line, because a water plane
			// can
			// dissapear when viewed with the eye exactly on it.
			// the server protocol only specifies to 1/8 pixel, so add 1/16 in
			// each axis
			Globals.cl.refdef.vieworg[0] += 1.0f / 16;
			Globals.cl.refdef.vieworg[1] += 1.0f / 16;
			Globals.cl.refdef.vieworg[2] += 1.0f / 16;
			Globals.cl.refdef.x = Globals.scr_vrect.x;
			Globals.cl.refdef.y = Globals.scr_vrect.y;
			Globals.cl.refdef.width = Globals.scr_vrect.width;
			Globals.cl.refdef.height = Globals.scr_vrect.height;
			Globals.cl.refdef.fov_y = Math3D.CalcFov(Globals.cl.refdef.fov_x, Globals.cl.refdef.width, Globals.cl.refdef.height);
			Globals.cl.refdef.time = Globals.cl.time * 0.001f;
			Globals.cl.refdef.areabits = Globals.cl.frame.areabits;

			if (Globals.cl_add_entities.value == 0.0f)
				V.r_numentities = 0;

			if (Globals.cl_add_particles.value == 0.0f)
				V.r_numparticles = 0;

			if (Globals.cl_add_lights.value == 0.0f)
				V.r_numdlights = 0;

			if (Globals.cl_add_blend.value == 0)
				Math3D.VectorClear(Globals.cl.refdef.blend);

			Globals.cl.refdef.num_entities = V.r_numentities;
			Globals.cl.refdef.entities = V.r_entities;
			Globals.cl.refdef.num_particles = V.r_numparticles;
			Globals.cl.refdef.num_dlights = V.r_numdlights;
			Globals.cl.refdef.dlights = V.r_dlights;
			Globals.cl.refdef.lightstyles = V.r_lightstyles;
			Globals.cl.refdef.rdflags = Globals.cl.frame.playerstate.rdflags;
		}

		Globals.re.RenderFrame(Globals.cl.refdef);

		if (V.cl_stats.value != 0.0f)
			clientserver.Com_Printf($"ent:{V.r_numentities}  lt:{V.r_numdlights}  part:{V.r_numparticles}\n");

		if (frame.log_stats.value != 0.0f && frame.log_stats_file != null)
		{
			try
			{
				frame.log_stats_file.Write(Encoding.UTF8.GetBytes(V.r_numentities + "," + V.r_numdlights + "," + V.r_numparticles));
			}
			catch (Exception)
			{
			}
		}

		SCR.AddDirtyPoint(Globals.scr_vrect.x, Globals.scr_vrect.y);
		SCR.AddDirtyPoint(Globals.scr_vrect.x + Globals.scr_vrect.width - 1, Globals.scr_vrect.y + Globals.scr_vrect.height - 1);
		SCR.DrawCrosshair();
	}

	/*
	 * ============= V_Viewpos_f =============
	 */
	private static readonly Action Viewpos_f = () =>
	{
		clientserver.Com_Printf(
			$"({(int)Globals.cl.refdef.vieworg[0]} {(int)Globals.cl.refdef.vieworg[1]} {(int)Globals.cl.refdef.vieworg[2]}) : {(int)Globals.cl.refdef.viewangles[Defines.YAW]}\n"
		);
	};

	public static void Init()
	{
		cmdparser.Cmd_AddCommand("gun_next", V.Gun_Next_f);
		cmdparser.Cmd_AddCommand("gun_prev", V.Gun_Prev_f);
		cmdparser.Cmd_AddCommand("gun_model", V.Gun_Model_f);
		cmdparser.Cmd_AddCommand("viewpos", V.Viewpos_f);
		Globals.crosshair = cvar.Cvar_Get("crosshair", "0", Defines.CVAR_ARCHIVE);
		V.cl_testblend = cvar.Cvar_Get("cl_testblend", "0", 0);
		V.cl_testparticles = cvar.Cvar_Get("cl_testparticles", "0", 0);
		V.cl_testentities = cvar.Cvar_Get("cl_testentities", "0", 0);
		V.cl_testlights = cvar.Cvar_Get("cl_testlights", "0", 0);
		V.cl_stats = cvar.Cvar_Get("cl_stats", "0", 0);
	}
}
