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
namespace Quake2Sharp.render.opengl;

using client;
using client.types;
using game;
using game.types;
using opentk;
using qcommon;
using qcommon.types;
using render.types;
using System.Drawing;
using types;
using util;

public partial class OpenGLRenderApi : RenderAPI
{
	public static int[] d_8to24table = new int[256];
	protected int registration_sequence;

	// this a hack for function pointer test
	// default disabled
	protected bool qglColorTableEXT;
	protected bool qglSelectTextureSGIS;
	protected bool qglActiveTextureARB;
	protected bool qglPointParameterfEXT;
	protected bool qglLockArraysEXT;

	protected bool qglMTexCoord2fSGIS;
	/*
	====================================================================
	
	from gl_rmain.c
	
	====================================================================
	*/

	protected model_t r_worldmodel;
	protected float gldepthmin;
	protected float gldepthmax;
	protected glconfig_t gl_config = new();
	protected glstate_t gl_state = new();
	protected image_t r_notexture; // use for bad textures
	protected image_t r_particletexture; // little dot for particles
	protected entity_t currententity;
	protected model_t currentmodel;
	protected cplane_t[] frustum = { new(), new(), new(), new() };
	protected int r_visframecount; // bumped when going to a new PVS
	protected int r_framecount; // used for dlight push checking
	protected int c_brush_polys;
	protected int c_alias_polys;
	private readonly float[] v_blend = { 0, 0, 0, 0 }; // final blending color

	//
	//	   view origin
	//
	protected float[] vup = { 0, 0, 0 };
	protected float[] vpn = { 0, 0, 0 };
	protected float[] vright = { 0, 0, 0 };
	protected float[] r_origin = { 0, 0, 0 };
	protected float[] r_world_matrix = new float[16];
	private float[] r_base_world_matrix = new float[16];

	//
	//	   screen size info
	//
	protected refdef_t r_newrefdef = new();
	protected int r_viewcluster;
	protected int r_viewcluster2;
	protected int r_oldviewcluster;
	protected int r_oldviewcluster2;
	private cvar_t r_norefresh;
	private cvar_t r_drawentities;
	protected cvar_t r_drawworld;
	private cvar_t r_speeds;
	protected cvar_t r_fullbright;
	protected cvar_t r_novis;
	private cvar_t r_nocull;
	protected cvar_t r_lerpmodels;
	protected cvar_t r_lefthand;
	protected cvar_t r_lightlevel;

	// FIXME: This is a HACK to get the client's light level
	private cvar_t gl_nosubimage;
	private cvar_t gl_allow_software;
	protected cvar_t gl_vertex_arrays;
	protected cvar_t gl_particle_min_size;
	protected cvar_t gl_particle_max_size;
	private cvar_t gl_particle_size;
	protected cvar_t gl_particle_att_a;
	protected cvar_t gl_particle_att_b;
	protected cvar_t gl_particle_att_c;
	private cvar_t gl_ext_swapinterval;
	protected cvar_t gl_ext_palettedtexture;
	private cvar_t gl_ext_multitexture;
	private cvar_t gl_ext_pointparameters;
	private cvar_t gl_ext_compiled_vertex_array;
	private cvar_t gl_log;
	private cvar_t gl_bitdepth;
	private cvar_t gl_drawbuffer;
	private cvar_t gl_driver;
	protected cvar_t gl_lightmap;
	protected cvar_t gl_shadows;
	private cvar_t gl_mode;
	protected cvar_t gl_dynamic;
	protected cvar_t gl_monolightmap;
	protected cvar_t gl_modulate;
	protected cvar_t gl_nobind;
	protected cvar_t gl_round_down;
	protected cvar_t gl_picmip;
	protected cvar_t gl_skymip;
	protected cvar_t gl_showtris;
	private cvar_t gl_ztrick;
	private cvar_t gl_finish;
	private cvar_t gl_clear;
	private cvar_t gl_cull;
	private cvar_t gl_polyblend;
	protected cvar_t gl_flashblend;
	private cvar_t gl_playermip;
	protected cvar_t gl_saturatelighting;
	protected cvar_t gl_swapinterval;
	protected cvar_t gl_texturemode;
	protected cvar_t gl_texturealphamode;
	protected cvar_t gl_texturesolidmode;
	protected cvar_t gl_lockpvs;
	private cvar_t gl_3dlabs_broken;
	protected cvar_t vid_gamma;
	private cvar_t vid_ref;

	// ============================================================================
	// to port from gl_rmain.c, ...
	// ============================================================================

	/*
	=================
	R_CullBox
	
	Returns true if the box is completely outside the frustom
	=================
	*/
	protected bool R_CullBox(float[] mins, float[] maxs)
	{
		if (!(mins.Length == 3 && maxs.Length == 3))
			throw new("vec3_t bug");

		if (this.r_nocull.value != 0)
			return false;

		for (var i = 0; i < 4; i++)
		{
			if (Math3D.BoxOnPlaneSide(mins, maxs, this.frustum[i]) == 2)
				return true;
		}

		return false;
	}

	protected void R_RotateForEntity(entity_t e)
	{
		this.gl.glTranslatef(e.origin[0], e.origin[1], e.origin[2]);

		this.gl.glRotatef(e.angles[1], 0, 0, 1);
		this.gl.glRotatef(-e.angles[0], 0, 1, 0);
		this.gl.glRotatef(-e.angles[2], 1, 0, 0);
	}

	/*
	=============================================================
	
	   SPRITE MODELS
	
	=============================================================
	*/

	/*
	=================
	R_DrawSpriteModel
	
	=================
	*/
	private void R_DrawSpriteModel(entity_t e)
	{
		var alpha = 1.0F;
		float[] point = { 0, 0, 0 };

		dsprframe_t frame;
		dsprite_t psprite;

		// don't even bother culling, because it's just a single
		// polygon without a surface cache

		psprite = (dsprite_t)this.currentmodel.extradata;

		e.frame %= psprite.numframes;

		frame = psprite.frames[e.frame];

		if ((e.flags & Defines.RF_TRANSLUCENT) != 0)
			alpha = e.alpha;

		if (alpha != 1.0F)
			this.gl.glEnable(OpenGL.GL_BLEND);

		this.gl.glColor4f(1, 1, 1, alpha);

		this.GL_Bind(this.currentmodel.skins[e.frame].texnum);

		this.GL_TexEnv(OpenGL.GL_MODULATE);

		if (alpha == 1.0)
			this.gl.glEnable(OpenGL.GL_ALPHA_TEST);
		else
			this.gl.glDisable(OpenGL.GL_ALPHA_TEST);

		this.gl.glBegin(OpenGL.GL_QUADS);

		this.gl.glTexCoord2f(0, 1);
		Math3D.VectorMA(e.origin, -frame.origin_y, this.vup, point);
		Math3D.VectorMA(point, -frame.origin_x, this.vright, point);
		this.gl.glVertex3f(point[0], point[1], point[2]);

		this.gl.glTexCoord2f(0, 0);
		Math3D.VectorMA(e.origin, frame.height - frame.origin_y, this.vup, point);
		Math3D.VectorMA(point, -frame.origin_x, this.vright, point);
		this.gl.glVertex3f(point[0], point[1], point[2]);

		this.gl.glTexCoord2f(1, 0);
		Math3D.VectorMA(e.origin, frame.height - frame.origin_y, this.vup, point);
		Math3D.VectorMA(point, frame.width - frame.origin_x, this.vright, point);
		this.gl.glVertex3f(point[0], point[1], point[2]);

		this.gl.glTexCoord2f(1, 1);
		Math3D.VectorMA(e.origin, -frame.origin_y, this.vup, point);
		Math3D.VectorMA(point, frame.width - frame.origin_x, this.vright, point);
		this.gl.glVertex3f(point[0], point[1], point[2]);

		this.gl.glEnd();

		this.gl.glDisable(OpenGL.GL_ALPHA_TEST);
		this.GL_TexEnv(OpenGL.GL_REPLACE);

		if (alpha != 1.0F)
			this.gl.glDisable(OpenGL.GL_BLEND);

		this.gl.glColor4f(1, 1, 1, 1);
	}

	// ==================================================================================

	/*
	=============
	R_DrawNullModel
	=============
	*/
	private void R_DrawNullModel()
	{
		float[] shadelight = { 0, 0, 0 };

		if ((this.currententity.flags & Defines.RF_FULLBRIGHT) != 0)
		{
			shadelight[0] = shadelight[1] = shadelight[2] = 0.0F;
			shadelight[2] = 0.8F;
		}
		else
			this.R_LightPoint(this.currententity.origin, shadelight);

		this.gl.glPushMatrix();
		this.R_RotateForEntity(this.currententity);

		this.gl.glDisable(OpenGL.GL_TEXTURE_2D);
		this.gl.glColor3f(shadelight[0], shadelight[1], shadelight[2]);

		//		// this replaces the TRIANGLE_FAN
		//		glut.glutWireCube(gl, 20);

		this.gl.glBegin(OpenGL.GL_TRIANGLE_FAN);
		this.gl.glVertex3f(0, 0, -16);
		int i;

		for (i = 0; i <= 4; i++)
			this.gl.glVertex3f((float)(16.0f * Math.Cos(i * Math.PI / 2)), (float)(16.0f * Math.Sin(i * Math.PI / 2)), 0.0f);

		this.gl.glEnd();

		this.gl.glBegin(OpenGL.GL_TRIANGLE_FAN);
		this.gl.glVertex3f(0, 0, 16);

		for (i = 4; i >= 0; i--)
			this.gl.glVertex3f((float)(16.0f * Math.Cos(i * Math.PI / 2)), (float)(16.0f * Math.Sin(i * Math.PI / 2)), 0.0f);

		this.gl.glEnd();

		this.gl.glColor3f(1, 1, 1);
		this.gl.glPopMatrix();
		this.gl.glEnable(OpenGL.GL_TEXTURE_2D);
	}

	/*
	=============
	R_DrawEntitiesOnList
	=============
	*/
	private void R_DrawEntitiesOnList()
	{
		int i;

		if (this.r_drawentities.value == 0.0f)
			return;

		// draw non-transparent first
		for (i = 0; i < this.r_newrefdef.num_entities; i++)
		{
			this.currententity = this.r_newrefdef.entities[i];

			if ((this.currententity.flags & Defines.RF_TRANSLUCENT) != 0)
				continue; // solid

			if ((this.currententity.flags & Defines.RF_BEAM) != 0)
				this.R_DrawBeam(this.currententity);
			else
			{
				this.currentmodel = this.currententity.model;

				if (this.currentmodel == null)
				{
					this.R_DrawNullModel();

					continue;
				}

				switch (this.currentmodel.type)
				{
					case Base.mod_alias:
						this.R_DrawAliasModel(this.currententity);

						break;

					case Base.mod_brush:
						this.R_DrawBrushModel(this.currententity);

						break;

					case Base.mod_sprite:
						this.R_DrawSpriteModel(this.currententity);

						break;

					default:
						Com.Error(Defines.ERR_DROP, "Bad modeltype");

						break;
				}
			}
		}

		// draw transparent entities
		// we could sort these if it ever becomes a problem...
		this.gl.glDepthMask(false); // no z writes

		for (i = 0; i < this.r_newrefdef.num_entities; i++)
		{
			this.currententity = this.r_newrefdef.entities[i];

			if ((this.currententity.flags & Defines.RF_TRANSLUCENT) == 0)
				continue; // solid

			if ((this.currententity.flags & Defines.RF_BEAM) != 0)
				this.R_DrawBeam(this.currententity);
			else
			{
				this.currentmodel = this.currententity.model;

				if (this.currentmodel == null)
				{
					this.R_DrawNullModel();

					continue;
				}

				switch (this.currentmodel.type)
				{
					case Base.mod_alias:
						this.R_DrawAliasModel(this.currententity);

						break;

					case Base.mod_brush:
						this.R_DrawBrushModel(this.currententity);

						break;

					case Base.mod_sprite:
						this.R_DrawSpriteModel(this.currententity);

						break;

					default:
						Com.Error(Defines.ERR_DROP, "Bad modeltype");

						break;
				}
			}
		}

		this.gl.glDepthMask(true); // back to writing
	}

	/*
	** GL_DrawParticles
	**
	*/
	private void GL_DrawParticles(int num_particles)
	{
		float[] up = { 0, 0, 0 };
		float[] right = { 0, 0, 0 };
		float scale;
		int color;

		float origin_x, origin_y, origin_z;

		Math3D.VectorScale(this.vup, 1.5f, up);
		Math3D.VectorScale(this.vright, 1.5f, right);

		this.GL_Bind(this.r_particletexture.texnum);
		this.gl.glDepthMask(false); // no z buffering
		this.gl.glEnable(OpenGL.GL_BLEND);
		this.GL_TexEnv(OpenGL.GL_MODULATE);

		this.gl.glBegin(OpenGL.GL_TRIANGLES);

		var sourceVertices = particle_t.vertexArray;
		var sourceColors = particle_t.colorArray;

		for (int j = 0, i = 0; i < num_particles; i++)
		{
			origin_x = sourceVertices[j++];
			origin_y = sourceVertices[j++];
			origin_z = sourceVertices[j++];

			// hack a scale up to keep particles from disapearing
			scale = (origin_x - this.r_origin[0]) * this.vpn[0] + (origin_y - this.r_origin[1]) * this.vpn[1] + (origin_z - this.r_origin[2]) * this.vpn[2];

			scale = scale < 20 ? 1 : 1 + scale * 0.004f;

			color = sourceColors[i];
			this.gl.glColor4ub((byte)((color >> 0) & 0xFF), (byte)((color >> 8) & 0xFF), (byte)((color >> 16) & 0xFF), (byte)((color >> 24) & 0xFF));

			// first vertex
			this.gl.glTexCoord2f(0.0625f, 0.0625f);
			this.gl.glVertex3f(origin_x, origin_y, origin_z);

			// second vertex
			this.gl.glTexCoord2f(1.0625f, 0.0625f);
			this.gl.glVertex3f(origin_x + up[0] * scale, origin_y + up[1] * scale, origin_z + up[2] * scale);

			// third vertex
			this.gl.glTexCoord2f(0.0625f, 1.0625f);
			this.gl.glVertex3f(origin_x + right[0] * scale, origin_y + right[1] * scale, origin_z + right[2] * scale);
		}

		this.gl.glEnd();

		this.gl.glDisable(OpenGL.GL_BLEND);
		this.gl.glColor4f(1, 1, 1, 1);
		this.gl.glDepthMask(true); // back to normal Z buffering
		this.GL_TexEnv(OpenGL.GL_REPLACE);
	}

	/*
	===============
	R_DrawParticles
	===============
	*/
	private void R_DrawParticles()
	{
		if (this.gl_ext_pointparameters.value != 0.0f && this.qglPointParameterfEXT)
		{
			this.gl.glEnableClientState(OpenGL.GL_VERTEX_ARRAY);
			this.gl.glVertexPointer(3, 0, particle_t.vertexArray);
			this.gl.glEnableClientState(OpenGL.GL_COLOR_ARRAY);
			this.gl.glColorPointer(4, true, 0, particle_t.colorArray);

			this.gl.glDepthMask(false);
			this.gl.glEnable(OpenGL.GL_BLEND);
			this.gl.glDisable(OpenGL.GL_TEXTURE_2D);
			this.gl.glPointSize(this.gl_particle_size.value);

			this.gl.glDrawArrays(OpenGL.GL_POINTS, 0, this.r_newrefdef.num_particles);

			this.gl.glDisableClientState(OpenGL.GL_COLOR_ARRAY);
			this.gl.glDisableClientState(OpenGL.GL_VERTEX_ARRAY);

			this.gl.glDisable(OpenGL.GL_BLEND);
			this.gl.glColor4f(1.0F, 1.0F, 1.0F, 1.0F);
			this.gl.glDepthMask(true);
			this.gl.glEnable(OpenGL.GL_TEXTURE_2D);
		}
		else
			this.GL_DrawParticles(this.r_newrefdef.num_particles);
	}

	/*
	============
	R_PolyBlend
	============
	*/
	private void R_PolyBlend()
	{
		if (this.gl_polyblend.value == 0.0f)
			return;

		if (this.v_blend[3] == 0.0f)
			return;

		this.gl.glDisable(OpenGL.GL_ALPHA_TEST);
		this.gl.glEnable(OpenGL.GL_BLEND);
		this.gl.glDisable(OpenGL.GL_DEPTH_TEST);
		this.gl.glDisable(OpenGL.GL_TEXTURE_2D);

		this.gl.glLoadIdentity();

		// FIXME: get rid of these
		this.gl.glRotatef(-90, 1, 0, 0); // put Z going up
		this.gl.glRotatef(90, 0, 0, 1); // put Z going up

		this.gl.glColor4f(this.v_blend[0], this.v_blend[1], this.v_blend[2], this.v_blend[3]);

		this.gl.glBegin(OpenGL.GL_QUADS);

		this.gl.glVertex3f(10, 100, 100);
		this.gl.glVertex3f(10, -100, 100);
		this.gl.glVertex3f(10, -100, -100);
		this.gl.glVertex3f(10, 100, -100);
		this.gl.glEnd();

		this.gl.glDisable(OpenGL.GL_BLEND);
		this.gl.glEnable(OpenGL.GL_TEXTURE_2D);
		this.gl.glEnable(OpenGL.GL_ALPHA_TEST);

		this.gl.glColor4f(1, 1, 1, 1);
	}

	// =======================================================================

	private int SignbitsForPlane(cplane_t @out)
	{
		// for fast box on planeside test
		var bits = 0;

		for (var j = 0; j < 3; j++)
		{
			if (@out.normal[j] < 0)
				bits |= 1 << j;
		}

		return bits;
	}

	private void R_SetFrustum()
	{
		// rotate VPN right by FOV_X/2 degrees
		Math3D.RotatePointAroundVector(this.frustum[0].normal, this.vup, this.vpn, -(90f - this.r_newrefdef.fov_x / 2f));

		// rotate VPN left by FOV_X/2 degrees
		Math3D.RotatePointAroundVector(this.frustum[1].normal, this.vup, this.vpn, 90f - this.r_newrefdef.fov_x / 2f);

		// rotate VPN up by FOV_X/2 degrees
		Math3D.RotatePointAroundVector(this.frustum[2].normal, this.vright, this.vpn, 90f - this.r_newrefdef.fov_y / 2f);

		// rotate VPN down by FOV_X/2 degrees
		Math3D.RotatePointAroundVector(this.frustum[3].normal, this.vright, this.vpn, -(90f - this.r_newrefdef.fov_y / 2f));

		for (var i = 0; i < 4; i++)
		{
			this.frustum[i].type = (byte)Defines.PLANE_ANYZ;
			this.frustum[i].dist = Math3D.DotProduct(this.r_origin, this.frustum[i].normal);
			this.frustum[i].signbits = (byte)this.SignbitsForPlane(this.frustum[i]);
		}
	}

	// =======================================================================

	/*
	===============
	R_SetupFrame
	===============
	*/
	private void R_SetupFrame()
	{
		int i;
		mleaf_t leaf;

		this.r_framecount++;

		//	build the transformation matrix for the given view angles
		Math3D.VectorCopy(this.r_newrefdef.vieworg, this.r_origin);

		Math3D.AngleVectors(this.r_newrefdef.viewangles, this.vpn, this.vright, this.vup);

		//	current viewcluster
		if ((this.r_newrefdef.rdflags & Defines.RDF_NOWORLDMODEL) == 0)
		{
			this.r_oldviewcluster = this.r_viewcluster;
			this.r_oldviewcluster2 = this.r_viewcluster2;
			leaf = this.Mod_PointInLeaf(this.r_origin, this.r_worldmodel);
			this.r_viewcluster = this.r_viewcluster2 = leaf.cluster;

			// check above and below so crossing solid water doesn't draw wrong
			if (leaf.contents == 0)
			{
				// look down a bit
				float[] temp = { 0, 0, 0 };

				Math3D.VectorCopy(this.r_origin, temp);
				temp[2] -= 16;
				leaf = this.Mod_PointInLeaf(temp, this.r_worldmodel);

				if ((leaf.contents & Defines.CONTENTS_SOLID) == 0 && leaf.cluster != this.r_viewcluster2)
					this.r_viewcluster2 = leaf.cluster;
			}
			else
			{
				// look up a bit
				float[] temp = { 0, 0, 0 };

				Math3D.VectorCopy(this.r_origin, temp);
				temp[2] += 16;
				leaf = this.Mod_PointInLeaf(temp, this.r_worldmodel);

				if ((leaf.contents & Defines.CONTENTS_SOLID) == 0 && leaf.cluster != this.r_viewcluster2)
					this.r_viewcluster2 = leaf.cluster;
			}
		}

		for (i = 0; i < 4; i++)
			this.v_blend[i] = this.r_newrefdef.blend[i];

		this.c_brush_polys = 0;
		this.c_alias_polys = 0;

		// clear out the portion of the screen that the NOWORLDMODEL defines
		if ((this.r_newrefdef.rdflags & Defines.RDF_NOWORLDMODEL) != 0)
		{
			this.gl.glEnable(OpenGL.GL_SCISSOR_TEST);
			this.gl.glClearColor(0.3f, 0.3f, 0.3f, 1.0f);

			this.gl.glScissor(
				this.r_newrefdef.x,
				Base.vid.getHeight() - this.r_newrefdef.height - this.r_newrefdef.y,
				this.r_newrefdef.width,
				this.r_newrefdef.height
			);

			this.gl.glClear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
			this.gl.glClearColor(1.0f, 0.0f, 0.5f, 0.5f);
			this.gl.glDisable(OpenGL.GL_SCISSOR_TEST);
		}
	}

	protected void MYgluPerspective(double fovy, double aspect, double zNear, double zFar)
	{
		double xmin, xmax, ymin, ymax;

		ymax = zNear * Math.Tan(fovy * Math.PI / 360.0);
		ymin = -ymax;

		xmin = ymin * aspect;
		xmax = ymax * aspect;

		xmin += -(2 * this.gl_state.camera_separation) / zNear;
		xmax += -(2 * this.gl_state.camera_separation) / zNear;

		this.gl.glFrustum(xmin, xmax, ymin, ymax, zNear, zFar);
	}

	/*
	=============
	R_SetupGL
	=============
	*/
	private void R_SetupGL()
	{
		float screenaspect;
		int x, x2, y2, y, w, h;

		//
		// set up viewport
		//
		x = (int)Math.Floor((float)this.r_newrefdef.x * Base.vid.getWidth() / Base.vid.getWidth());
		x2 = (int)Math.Ceiling(((float)this.r_newrefdef.x + this.r_newrefdef.width) * Base.vid.getWidth() / Base.vid.getWidth());
		y = (int)Math.Floor(Base.vid.getHeight() - (float)this.r_newrefdef.y * Base.vid.getHeight() / Base.vid.getHeight());

		y2 = (int)Math.Ceiling(Base.vid.getHeight() - ((float)this.r_newrefdef.y + this.r_newrefdef.height) * Base.vid.getHeight() / Base.vid.getHeight());

		w = x2 - x;
		h = y - y2;

		this.gl.glViewport(x, y2, w, h);

		//
		// set up projection matrix
		//
		screenaspect = (float)this.r_newrefdef.width / this.r_newrefdef.height;
		this.gl.glMatrixMode(OpenGL.GL_PROJECTION);
		this.gl.glLoadIdentity();
		this.MYgluPerspective(this.r_newrefdef.fov_y, screenaspect, 4, 4096);

		this.gl.glCullFace(OpenGL.GL_FRONT);

		this.gl.glMatrixMode(OpenGL.GL_MODELVIEW);
		this.gl.glLoadIdentity();

		this.gl.glRotatef(-90, 1, 0, 0); // put Z going up
		this.gl.glRotatef(90, 0, 0, 1); // put Z going up
		this.gl.glRotatef(-this.r_newrefdef.viewangles[2], 1, 0, 0);
		this.gl.glRotatef(-this.r_newrefdef.viewangles[0], 0, 1, 0);
		this.gl.glRotatef(-this.r_newrefdef.viewangles[1], 0, 0, 1);
		this.gl.glTranslatef(-this.r_newrefdef.vieworg[0], -this.r_newrefdef.vieworg[1], -this.r_newrefdef.vieworg[2]);

		this.gl.glGetFloat(OpenGL.GL_MODELVIEW_MATRIX, this.r_world_matrix);

		//
		// set drawing parms
		//
		if (this.gl_cull.value != 0.0f)
			this.gl.glEnable(OpenGL.GL_CULL_FACE);
		else
			this.gl.glDisable(OpenGL.GL_CULL_FACE);

		this.gl.glDisable(OpenGL.GL_BLEND);
		this.gl.glDisable(OpenGL.GL_ALPHA_TEST);
		this.gl.glEnable(OpenGL.GL_DEPTH_TEST);
	}

	/*
	=============
	R_Clear
	=============
	*/
	private int trickframe;

	private void R_Clear()
	{
		if (this.gl_ztrick.value != 0.0f)
		{
			if (this.gl_clear.value != 0.0f)
				this.gl.glClear(OpenGL.GL_COLOR_BUFFER_BIT);

			this.trickframe++;

			if ((this.trickframe & 1) != 0)
			{
				this.gldepthmin = 0;
				this.gldepthmax = 0.49999f;
				this.gl.glDepthFunc(OpenGL.GL_LEQUAL);
			}
			else
			{
				this.gldepthmin = 1;
				this.gldepthmax = 0.5f;
				this.gl.glDepthFunc(OpenGL.GL_GEQUAL);
			}
		}
		else
		{
			if (this.gl_clear.value != 0.0f)
				this.gl.glClear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
			else
				this.gl.glClear(OpenGL.GL_DEPTH_BUFFER_BIT);

			this.gldepthmin = 0;
			this.gldepthmax = 1;
			this.gl.glDepthFunc(OpenGL.GL_LEQUAL);
		}

		this.gl.glDepthRange(this.gldepthmin, this.gldepthmax);
	}

	private void R_Flash()
	{
		this.R_PolyBlend();
	}

	/*
	================
	R_RenderView
	
	r_newrefdef must be set before the first call
	================
	*/
	private void R_RenderView(refdef_t fd)
	{
		if (this.r_norefresh.value != 0.0f)
			return;

		this.r_newrefdef = fd;

		if (this.r_newrefdef == null)
			Com.Error(Defines.ERR_DROP, "R_RenderView: refdef_t fd is null");

		if (this.r_worldmodel == null && (this.r_newrefdef.rdflags & Defines.RDF_NOWORLDMODEL) == 0)
			Com.Error(Defines.ERR_DROP, "R_RenderView: NULL worldmodel");

		if (this.r_speeds.value != 0.0f)
		{
			this.c_brush_polys = 0;
			this.c_alias_polys = 0;
		}

		this.R_PushDlights();

		if (this.gl_finish.value != 0.0f)
			this.gl.glFinish();

		this.R_SetupFrame();

		this.R_SetFrustum();

		this.R_SetupGL();

		this.R_MarkLeaves(); // done here so we know if we're in water

		this.R_DrawWorld();

		this.R_DrawEntitiesOnList();

		this.R_RenderDlights();

		this.R_DrawParticles();

		this.R_DrawAlphaSurfaces();

		this.R_Flash();

		if (this.r_speeds.value != 0.0f)
		{
			VID.Printf(
				Defines.PRINT_ALL,
				"%4i wpoly %4i epoly %i tex %i lmaps\n",
				this.c_brush_polys, this.c_alias_polys, this.c_visible_textures, this.c_visible_lightmaps
			);
		}
	}

	private void R_SetGL2D()
	{
		// set 2D virtual screen size
		this.gl.glViewport(0, 0, Base.vid.getWidth(), Base.vid.getHeight());
		this.gl.glMatrixMode(OpenGL.GL_PROJECTION);
		this.gl.glLoadIdentity();
		this.gl.glOrtho(0, Base.vid.getWidth(), Base.vid.getHeight(), 0, -99999, 99999);
		this.gl.glMatrixMode(OpenGL.GL_MODELVIEW);
		this.gl.glLoadIdentity();
		this.gl.glDisable(OpenGL.GL_DEPTH_TEST);
		this.gl.glDisable(OpenGL.GL_CULL_FACE);
		this.gl.glDisable(OpenGL.GL_BLEND);
		this.gl.glEnable(OpenGL.GL_ALPHA_TEST);
		this.gl.glColor4f(1, 1, 1, 1);
	}

	/*
	====================
	R_SetLightLevel
	
	====================
	*/
	private void R_SetLightLevel()
	{
		float[] shadelight = { 0, 0, 0 };

		if ((this.r_newrefdef.rdflags & Defines.RDF_NOWORLDMODEL) != 0)
			return;

		// save off light value for server to look at (BIG HACK!)

		this.R_LightPoint(this.r_newrefdef.vieworg, shadelight);

		// pick the greatest component, which should be the same
		// as the mono value returned by software
		if (shadelight[0] > shadelight[1])
		{
			if (shadelight[0] > shadelight[2])
				this.r_lightlevel.value = 150 * shadelight[0];
			else
				this.r_lightlevel.value = 150 * shadelight[2];
		}
		else
		{
			if (shadelight[1] > shadelight[2])
				this.r_lightlevel.value = 150 * shadelight[1];
			else
				this.r_lightlevel.value = 150 * shadelight[2];
		}
	}

	/*
	@@@@@@@@@@@@@@@@@@@@@
	R_RenderFrame
	
	@@@@@@@@@@@@@@@@@@@@@
	*/
	public override void R_RenderFrame(refdef_t fd)
	{
		this.R_RenderView(fd);
		this.R_SetLightLevel();
		this.R_SetGL2D();
	}

	protected void R_Register()
	{
		this.r_lefthand = Cvar.Get("hand", "0", Defines.CVAR_USERINFO | Defines.CVAR_ARCHIVE);
		this.r_norefresh = Cvar.Get("r_norefresh", "0", 0);
		this.r_fullbright = Cvar.Get("r_fullbright", "0", 0);
		this.r_drawentities = Cvar.Get("r_drawentities", "1", 0);
		this.r_drawworld = Cvar.Get("r_drawworld", "1", 0);
		this.r_novis = Cvar.Get("r_novis", "0", 0);
		this.r_nocull = Cvar.Get("r_nocull", "0", 0);
		this.r_lerpmodels = Cvar.Get("r_lerpmodels", "1", 0);
		this.r_speeds = Cvar.Get("r_speeds", "0", 0);

		this.r_lightlevel = Cvar.Get("r_lightlevel", "1", 0);

		this.gl_nosubimage = Cvar.Get("gl_nosubimage", "0", 0);
		this.gl_allow_software = Cvar.Get("gl_allow_software", "0", 0);

		this.gl_particle_min_size = Cvar.Get("gl_particle_min_size", "2", Defines.CVAR_ARCHIVE);
		this.gl_particle_max_size = Cvar.Get("gl_particle_max_size", "40", Defines.CVAR_ARCHIVE);
		this.gl_particle_size = Cvar.Get("gl_particle_size", "40", Defines.CVAR_ARCHIVE);
		this.gl_particle_att_a = Cvar.Get("gl_particle_att_a", "0.01", Defines.CVAR_ARCHIVE);
		this.gl_particle_att_b = Cvar.Get("gl_particle_att_b", "0.0", Defines.CVAR_ARCHIVE);
		this.gl_particle_att_c = Cvar.Get("gl_particle_att_c", "0.01", Defines.CVAR_ARCHIVE);

		this.gl_modulate = Cvar.Get("gl_modulate", "1.5", Defines.CVAR_ARCHIVE);
		this.gl_log = Cvar.Get("gl_log", "0", 0);
		this.gl_bitdepth = Cvar.Get("gl_bitdepth", "0", 0);
		this.gl_mode = Cvar.Get("gl_mode", "1", Defines.CVAR_ARCHIVE);
		this.gl_lightmap = Cvar.Get("gl_lightmap", "0", 0);
		this.gl_shadows = Cvar.Get("gl_shadows", "0", Defines.CVAR_ARCHIVE);
		this.gl_dynamic = Cvar.Get("gl_dynamic", "1", 0);
		this.gl_nobind = Cvar.Get("gl_nobind", "0", 0);
		this.gl_round_down = Cvar.Get("gl_round_down", "1", 0);
		this.gl_picmip = Cvar.Get("gl_picmip", "0", 0);
		this.gl_skymip = Cvar.Get("gl_skymip", "0", 0);
		this.gl_showtris = Cvar.Get("gl_showtris", "0", 0);
		this.gl_ztrick = Cvar.Get("gl_ztrick", "0", 0);
		this.gl_finish = Cvar.Get("gl_finish", "0", Defines.CVAR_ARCHIVE);
		this.gl_clear = Cvar.Get("gl_clear", "0", 0);
		this.gl_cull = Cvar.Get("gl_cull", "1", 0);
		this.gl_polyblend = Cvar.Get("gl_polyblend", "1", 0);
		this.gl_flashblend = Cvar.Get("gl_flashblend", "0", 0);
		this.gl_playermip = Cvar.Get("gl_playermip", "0", 0);
		this.gl_monolightmap = Cvar.Get("gl_monolightmap", "0", 0);
		this.gl_driver = Cvar.Get("gl_driver", "opengl32", Defines.CVAR_ARCHIVE);
		this.gl_texturemode = Cvar.Get("gl_texturemode", "GL_LINEAR_MIPMAP_NEAREST", Defines.CVAR_ARCHIVE);
		this.gl_texturealphamode = Cvar.Get("gl_texturealphamode", "default", Defines.CVAR_ARCHIVE);
		this.gl_texturesolidmode = Cvar.Get("gl_texturesolidmode", "default", Defines.CVAR_ARCHIVE);
		this.gl_lockpvs = Cvar.Get("gl_lockpvs", "0", 0);

		this.gl_vertex_arrays = Cvar.Get("gl_vertex_arrays", "0", Defines.CVAR_ARCHIVE);

		this.gl_ext_swapinterval = Cvar.Get("gl_ext_swapinterval", "1", Defines.CVAR_ARCHIVE);
		this.gl_ext_palettedtexture = Cvar.Get("gl_ext_palettedtexture", "0", Defines.CVAR_ARCHIVE);
		this.gl_ext_multitexture = Cvar.Get("gl_ext_multitexture", "1", Defines.CVAR_ARCHIVE);
		this.gl_ext_pointparameters = Cvar.Get("gl_ext_pointparameters", "1", Defines.CVAR_ARCHIVE);
		this.gl_ext_compiled_vertex_array = Cvar.Get("gl_ext_compiled_vertex_array", "1", Defines.CVAR_ARCHIVE);

		this.gl_drawbuffer = Cvar.Get("gl_drawbuffer", "GL_BACK", 0);
		this.gl_swapinterval = Cvar.Get("gl_swapinterval", "1", Defines.CVAR_ARCHIVE);

		this.gl_saturatelighting = Cvar.Get("gl_saturatelighting", "0", 0);

		this.gl_3dlabs_broken = Cvar.Get("gl_3dlabs_broken", "1", Defines.CVAR_ARCHIVE);

		Base.vid_fullscreen = Cvar.Get("vid_fullscreen", "0", Defines.CVAR_ARCHIVE);
		this.vid_gamma = Cvar.Get("vid_gamma", "1.0", Defines.CVAR_ARCHIVE);
		this.vid_ref = Cvar.Get("vid_ref", "Dummy", Defines.CVAR_ARCHIVE);

		Cmd.AddCommand("imagelist", () => { this.GL_ImageList_f(); });

		Cmd.AddCommand("screenshot", () => { Base.glImpl.screenshot(); });

		Cmd.AddCommand("modellist", () => { this.Mod_Modellist_f(); });

		Cmd.AddCommand("gl_strings", () => { this.GL_Strings_f(); });
	}

	/*
	==================
	R_SetMode
	==================
	*/
	protected bool R_SetMode()
	{
		int err; //  enum rserr_t
		bool fullscreen;

		//		if (vid_fullscreen.modified && !gl_config.allow_cds) {
		//			VID.Printf(Defines.PRINT_ALL, "R_SetMode() - CDS not allowed with this driver\n");
		//			Cvar.SetValue("vid_fullscreen", (vid_fullscreen.value > 0.0f) ? 0.0f : 1.0f);
		//			vid_fullscreen.modified = false;
		//		}

		fullscreen = Base.vid_fullscreen.value > 0.0f;

		Base.vid_fullscreen.modified = false;
		this.gl_mode.modified = false;

		var dim = new Size(Base.vid.getWidth(), Base.vid.getHeight());

		if ((err = Base.glImpl.setMode(dim, (int)this.gl_mode.value, fullscreen)) == Base.rserr_ok)
			this.gl_state.prev_mode = (int)this.gl_mode.value;
		else
		{
			if (err == Base.rserr_invalid_fullscreen)
			{
				Cvar.SetValue("vid_fullscreen", 0);
				Base.vid_fullscreen.modified = false;
				VID.Printf(Defines.PRINT_ALL, "ref_gl::R_SetMode() - fullscreen unavailable in this mode\n");

				if ((err = Base.glImpl.setMode(dim, (int)this.gl_mode.value, false)) == Base.rserr_ok)
					return true;
			}
			else if (err == Base.rserr_invalid_mode)
			{
				Cvar.SetValue("gl_mode", this.gl_state.prev_mode);
				this.gl_mode.modified = false;
				VID.Printf(Defines.PRINT_ALL, "ref_gl::R_SetMode() - invalid mode\n");
			}

			// try setting it back to something safe
			if ((err = Base.glImpl.setMode(dim, this.gl_state.prev_mode, false)) != Base.rserr_ok)
			{
				VID.Printf(Defines.PRINT_ALL, "ref_gl::R_SetMode() - could not revert to safe mode\n");

				return false;
			}
		}

		return true;
	}

	/*
	===============
	R_Init
	===============
	*/
	private readonly float[] r_turbsin = new float[256];

	protected bool R_Init()
	{
		return this.R_Init(0, 0);
	}

	public override bool R_Init(int vid_xpos, int vid_ypos)
	{
		if (!(OpenGLRenderApi.SIN.Length == 256))
			throw new("warpsin table bug");

		// fill r_turbsin
		for (var j = 0; j < 256; j++)
			this.r_turbsin[j] = OpenGLRenderApi.SIN[j] * 0.5f;

		this.Draw_GetPalette();

		this.R_Register();

		// set our "safe" modes
		this.gl_state.prev_mode = 3;

		// create the window and set up the context
		if (!this.R_SetMode())
		{
			VID.Printf(Defines.PRINT_ALL, "ref_gl::R_Init() - could not R_SetMode()\n");

			return false;
		}

		return true;
	}

	public override bool R_Init2()
	{
		VID.MenuInit();

		/*
		** get our various GL strings
		*/
		this.gl_config.vendor_string = this.gl.glGetString(OpenGL.GL_VENDOR);
		VID.Printf(Defines.PRINT_ALL, "GL_VENDOR: " + this.gl_config.vendor_string + '\n');
		this.gl_config.renderer_string = this.gl.glGetString(OpenGL.GL_RENDERER);
		VID.Printf(Defines.PRINT_ALL, "GL_RENDERER: " + this.gl_config.renderer_string + '\n');
		this.gl_config.version_string = this.gl.glGetString(OpenGL.GL_VERSION);
		VID.Printf(Defines.PRINT_ALL, "GL_VERSION: " + this.gl_config.version_string + '\n');
		this.gl_config.extensions_string = this.gl.glGetString(OpenGL.GL_EXTENSIONS);
		VID.Printf(Defines.PRINT_ALL, "GL_EXTENSIONS: " + this.gl_config.extensions_string + '\n');

		this.gl_config.parseOpenGLVersion();

		var renderer_buffer = this.gl_config.renderer_string.ToLower();
		var vendor_buffer = this.gl_config.vendor_string.ToLower();

		if (renderer_buffer.IndexOf("voodoo") >= 0)
		{
			if (renderer_buffer.IndexOf("rush") < 0)
				this.gl_config.renderer = Base.GL_RENDERER_VOODOO;
			else
				this.gl_config.renderer = Base.GL_RENDERER_VOODOO_RUSH;
		}
		else if (vendor_buffer.IndexOf("sgi") >= 0)
			this.gl_config.renderer = Base.GL_RENDERER_SGI;
		else if (renderer_buffer.IndexOf("permedia") >= 0)
			this.gl_config.renderer = Base.GL_RENDERER_PERMEDIA2;
		else if (renderer_buffer.IndexOf("glint") >= 0)
			this.gl_config.renderer = Base.GL_RENDERER_GLINT_MX;
		else if (renderer_buffer.IndexOf("glzicd") >= 0)
			this.gl_config.renderer = Base.GL_RENDERER_REALIZM;
		else if (renderer_buffer.IndexOf("gdi") >= 0)
			this.gl_config.renderer = Base.GL_RENDERER_MCD;
		else if (renderer_buffer.IndexOf("pcx2") >= 0)
			this.gl_config.renderer = Base.GL_RENDERER_PCX2;
		else if (renderer_buffer.IndexOf("verite") >= 0)
			this.gl_config.renderer = Base.GL_RENDERER_RENDITION;
		else
			this.gl_config.renderer = Base.GL_RENDERER_OTHER;

		var monolightmap = this.gl_monolightmap.@string.ToUpper();

		if (monolightmap.Length < 2 || monolightmap[1] != 'F')
		{
			if (this.gl_config.renderer == Base.GL_RENDERER_PERMEDIA2)
			{
				Cvar.Set("gl_monolightmap", "A");
				VID.Printf(Defines.PRINT_ALL, "...using gl_monolightmap 'a'\n");
			}
			else if ((this.gl_config.renderer & Base.GL_RENDERER_POWERVR) != 0)
				Cvar.Set("gl_monolightmap", "0");
			else
				Cvar.Set("gl_monolightmap", "0");
		}

		// power vr can't have anything stay in the framebuffer, so
		// the screen needs to redraw the tiled background every frame
		if ((this.gl_config.renderer & Base.GL_RENDERER_POWERVR) != 0)
			Cvar.Set("scr_drawall", "1");
		else
			Cvar.Set("scr_drawall", "0");

		// #ifdef __linux__
		Cvar.SetValue("gl_finish", 1);

		// #endif

		// MCD has buffering issues
		if (this.gl_config.renderer == Base.GL_RENDERER_MCD)
			Cvar.SetValue("gl_finish", 1);

		if ((this.gl_config.renderer & Base.GL_RENDERER_3DLABS) != 0)
		{
			if (this.gl_3dlabs_broken.value != 0.0f)
				this.gl_config.allow_cds = false;
			else
				this.gl_config.allow_cds = true;
		}
		else
			this.gl_config.allow_cds = true;

		if (this.gl_config.allow_cds)
			VID.Printf(Defines.PRINT_ALL, "...allowing CDS\n");
		else
			VID.Printf(Defines.PRINT_ALL, "...disabling CDS\n");

		/*
		** grab extensions
		*/
		if (this.gl_config.extensions_string.IndexOf("GL_EXT_compiled_vertex_array") >= 0
		    || this.gl_config.extensions_string.IndexOf("GL_SGI_compiled_vertex_array") >= 0)
		{
			VID.Printf(Defines.PRINT_ALL, "...enabling GL_EXT_compiled_vertex_array\n");

			//		 qglLockArraysEXT = ( void * ) qwglGetProcAddress( "glLockArraysEXT" );
			if (this.gl_ext_compiled_vertex_array.value != 0.0f)
				this.qglLockArraysEXT = true;
			else
				this.qglLockArraysEXT = false;

			//		 qglUnlockArraysEXT = ( void * ) qwglGetProcAddress( "glUnlockArraysEXT" );
			//qglUnlockArraysEXT = true;
		}
		else
			VID.Printf(Defines.PRINT_ALL, "...GL_EXT_compiled_vertex_array not found\n");

		if (this.gl_config.extensions_string.IndexOf("GL_EXT_point_parameters") >= 0)
		{
			if (this.gl_ext_pointparameters.value != 0.0f)
			{
				//			 qglPointParameterfEXT = ( void (APIENTRY *)( GLenum, GLfloat ) ) qwglGetProcAddress( "glPointParameterfEXT" );
				this.qglPointParameterfEXT = true;

				//			 qglPointParameterfvEXT = ( void (APIENTRY *)( GLenum, const GLfloat * ) ) qwglGetProcAddress( "glPointParameterfvEXT" );
				VID.Printf(Defines.PRINT_ALL, "...using GL_EXT_point_parameters\n");
			}
			else
				VID.Printf(Defines.PRINT_ALL, "...ignoring GL_EXT_point_parameters\n");
		}
		else
			VID.Printf(Defines.PRINT_ALL, "...GL_EXT_point_parameters not found\n");

		// #ifdef __linux__
		//	 if ( strstr( gl_config.extensions_string, "3DFX_set_global_palette" ))
		//	 {
		//		 if ( gl_ext_palettedtexture->value )
		//		 {
		//			 VID.Printf( Defines.PRINT_ALL, "...using 3DFX_set_global_palette\n" );
		//			 qgl3DfxSetPaletteEXT = ( void ( APIENTRY * ) (GLuint *) )qwglGetProcAddress( "gl3DfxSetPaletteEXT" );
		////			 qglColorTableEXT = Fake_glColorTableEXT;
		//		 }
		//		 else
		//		 {
		//			 VID.Printf( Defines.PRINT_ALL, "...ignoring 3DFX_set_global_palette\n" );
		//		 }
		//	 }
		//	 else
		//	 {
		//		 VID.Printf( Defines.PRINT_ALL, "...3DFX_set_global_palette not found\n" );
		//	 }
		// #endif

		if (!this.qglColorTableEXT
		    && this.gl_config.extensions_string.IndexOf("GL_EXT_paletted_texture") >= 0
		    && this.gl_config.extensions_string.IndexOf("GL_EXT_shared_texture_palette") >= 0)
		{
			if (this.gl_ext_palettedtexture.value != 0.0f)
			{
				VID.Printf(Defines.PRINT_ALL, "...using GL_EXT_shared_texture_palette\n");
				this.qglColorTableEXT = false; // true;
			}
			else
			{
				VID.Printf(Defines.PRINT_ALL, "...ignoring GL_EXT_shared_texture_palette\n");
				this.qglColorTableEXT = false;
			}
		}
		else
			VID.Printf(Defines.PRINT_ALL, "...GL_EXT_shared_texture_palette not found\n");

		if (this.gl_config.extensions_string.IndexOf("GL_ARB_multitexture") >= 0)
		{
			if (this.gl_ext_multitexture.value != 0.0f)
			{
				VID.Printf(Defines.PRINT_ALL, "...using GL_ARB_multitexture\n");

				//			 qglMTexCoord2fSGIS = ( void * ) qwglGetProcAddress( "glMultiTexCoord2fARB" );
				//			 qglActiveTextureARB = ( void * ) qwglGetProcAddress( "glActiveTextureARB" );
				//			 qglClientActiveTextureARB = ( void * ) qwglGetProcAddress( "glClientActiveTextureARB" );
				this.qglActiveTextureARB = true;
				this.qglMTexCoord2fSGIS = true;

				//				GL_TEXTURE0 = GL_TEXTURE0_ARB;
				//				GL_TEXTURE1 = GL_TEXTURE1_ARB;
				Cvar.SetValue("r_fullbright", 1);
			}
			else
			{
				VID.Printf(Defines.PRINT_ALL, "...ignoring GL_ARB_multitexture\n");
				Cvar.SetValue("r_fullbright", 0);
			}
		}
		else
		{
			VID.Printf(Defines.PRINT_ALL, "...GL_ARB_multitexture not found\n");
			Cvar.SetValue("r_fullbright", 0);
		}

		if (this.gl_config.extensions_string.IndexOf("GL_SGIS_multitexture") >= 0)
		{
			if (this.qglActiveTextureARB)
			{
				VID.Printf(Defines.PRINT_ALL, "...GL_SGIS_multitexture deprecated in favor of ARB_multitexture\n");
				Cvar.SetValue("r_fullbright", 1);
			}
			else if (this.gl_ext_multitexture.value != 0.0f)
			{
				VID.Printf(Defines.PRINT_ALL, "...using GL_SGIS_multitexture\n");

				//			 qglMTexCoord2fSGIS = ( void * ) qwglGetProcAddress( "glMTexCoord2fSGIS" );
				//			 qglSelectTextureSGIS = ( void * ) qwglGetProcAddress( "glSelectTextureSGIS" );
				this.qglSelectTextureSGIS = true;
				this.qglMTexCoord2fSGIS = true;
				Cvar.SetValue("r_fullbright", 1);

				//			 //GL_TEXTURE0 = GL_TEXTURE0_SGIS;
				//			 //GL_TEXTURE1 = GL_TEXTURE1_SGIS;
			}
			else
			{
				VID.Printf(Defines.PRINT_ALL, "...ignoring GL_SGIS_multitexture\n");
				Cvar.SetValue("r_fullbright", 0);
			}
		}
		else
		{
			VID.Printf(Defines.PRINT_ALL, "...GL_SGIS_multitexture not found\n");

			if (!this.qglActiveTextureARB)
				Cvar.SetValue("r_fullbright", 0);
		}

		this.GL_SetDefaultState();

		this.GL_InitImages();
		this.Mod_Init();
		this.R_InitParticleTexture();
		this.Draw_InitLocal();

		var err = this.gl.glGetError();

		if (err != OpenGL.GL_NO_ERROR)
			VID.Printf(Defines.PRINT_ALL, "glGetError() = 0x%x\n\t%s\n", err, "" + this.gl.glGetString(err));

		return true;
	}

	/*
	===============
	R_Shutdown
	===============
	*/
	public override void R_Shutdown()
	{
		Cmd.RemoveCommand("modellist");
		Cmd.RemoveCommand("screenshot");
		Cmd.RemoveCommand("imagelist");
		Cmd.RemoveCommand("gl_strings");

		this.Mod_FreeAll();

		this.GL_ShutdownImages();

		/*
		 * shut down OS specific OpenGL stuff like contexts, etc.
		 */
		Base.glImpl.shutdown();
	}

	/*
	@@@@@@@@@@@@@@@@@@@@@
	R_BeginFrame
	@@@@@@@@@@@@@@@@@@@@@
	*/
	public override void R_BeginFrame(float camera_separation)
	{
		Base.vid.update();

		this.gl_state.camera_separation = camera_separation;

		/*
		** change modes if necessary
		*/
		if (this.gl_log.modified)
		{
			Base.glImpl.enableLogging(this.gl_log.value != 0.0f);
			this.gl_log.modified = false;
		}

		if (this.gl_log.value != 0.0f)
			Base.glImpl.logNewFrame();

		/*
		** update 3Dfx gamma -- it is expected that a user will do a vid_restart
		** after tweaking this value
		*/
		if (this.vid_gamma.modified)
		{
			this.vid_gamma.modified = false;

			if ((this.gl_config.renderer & Base.GL_RENDERER_VOODOO) != 0)
			{
				// wird erstmal nicht gebraucht

				/* 
				char envbuffer[1024];
				float g;
				
				g = 2.00 * ( 0.8 - ( vid_gamma->value - 0.5 ) ) + 1.0F;
				Com_sprintf( envbuffer, sizeof(envbuffer), "SSTV2_GAMMA=%f", g );
				putenv( envbuffer );
				Com_sprintf( envbuffer, sizeof(envbuffer), "SST_GAMMA=%f", g );
				putenv( envbuffer );
				*/
				VID.Printf(Defines.PRINT_DEVELOPER, "gamma anpassung fuer VOODOO nicht gesetzt");
			}
		}

		Base.glImpl.beginFrame(camera_separation);

		/*
		** go into 2D mode
		*/
		this.gl.glViewport(0, 0, Base.vid.getWidth(), Base.vid.getHeight());
		this.gl.glMatrixMode(OpenGL.GL_PROJECTION);
		this.gl.glLoadIdentity();
		this.gl.glOrtho(0, Base.vid.getWidth(), Base.vid.getHeight(), 0, -99999, 99999);
		this.gl.glMatrixMode(OpenGL.GL_MODELVIEW);
		this.gl.glLoadIdentity();
		this.gl.glDisable(OpenGL.GL_DEPTH_TEST);
		this.gl.glDisable(OpenGL.GL_CULL_FACE);
		this.gl.glDisable(OpenGL.GL_BLEND);
		this.gl.glEnable(OpenGL.GL_ALPHA_TEST);
		this.gl.glColor4f(1, 1, 1, 1);

		/*
		** draw buffer stuff
		*/
		if (this.gl_drawbuffer.modified)
		{
			this.gl_drawbuffer.modified = false;

			if (this.gl_state.camera_separation == 0 || !this.gl_state.stereo_enabled)
			{
				if (this.gl_drawbuffer.@string.Equals("GL_FRONT", StringComparison.OrdinalIgnoreCase))
					this.gl.glDrawBuffer(OpenGL.GL_FRONT);
				else
					this.gl.glDrawBuffer(OpenGL.GL_BACK);
			}
		}

		/*
		** texturemode stuff
		*/
		if (this.gl_texturemode.modified)
		{
			this.GL_TextureMode(this.gl_texturemode.@string);
			this.gl_texturemode.modified = false;
		}

		if (this.gl_texturealphamode.modified)
		{
			this.GL_TextureAlphaMode(this.gl_texturealphamode.@string);
			this.gl_texturealphamode.modified = false;
		}

		if (this.gl_texturesolidmode.modified)
		{
			this.GL_TextureSolidMode(this.gl_texturesolidmode.@string);
			this.gl_texturesolidmode.modified = false;
		}

		/*
		** swapinterval stuff
		*/
		this.GL_UpdateSwapInterval();

		//
		// clear screen if desired
		//
		this.R_Clear();
	}

	protected int[] r_rawpalette = new int[256];

	/*
	=============
	R_SetPalette
	=============
	*/
	public override void R_SetPalette(byte[] palette)
	{
		// 256 RGB values (768 bytes)
		// or null
		int i;
		var color = 0;

		if (palette != null)
		{
			var j = 0;

			for (i = 0; i < 256; i++)
			{
				color = (palette[j++] & 0xFF) << 0;
				color |= (palette[j++] & 0xFF) << 8;
				color |= (palette[j++] & 0xFF) << 16;
				color |= unchecked((int)0xFF000000);
				this.r_rawpalette[i] = color;
			}
		}
		else
		{
			for (i = 0; i < 256; i++)
				this.r_rawpalette[i] = OpenGLRenderApi.d_8to24table[i] | unchecked((int)0xff000000);
		}

		this.gl.glClearColor(0, 0, 0, 0);
		this.gl.glClear(OpenGL.GL_COLOR_BUFFER_BIT);
		this.gl.glClearColor(1f, 0f, 0.5f, 0.5f);
	}

	private static readonly int NUM_BEAM_SEGS = 6;
	private readonly float[][] start_points = new float[OpenGLRenderApi.NUM_BEAM_SEGS][];

	// array of vec3_t
	private readonly float[][] end_points = new float[OpenGLRenderApi.NUM_BEAM_SEGS][]; // array of vec3_t

	/*
	** R_DrawBeam
	*/
	private void R_DrawBeam(entity_t e)
	{
		int i;
		float r, g, b;

		float[] perpvec = { 0, 0, 0 }; // vec3_t
		float[] direction = { 0, 0, 0 }; // vec3_t
		float[] normalized_direction = { 0, 0, 0 }; // vec3_t

		float[] oldorigin = { 0, 0, 0 }; // vec3_t
		float[] origin = { 0, 0, 0 }; // vec3_t

		oldorigin[0] = e.oldorigin[0];
		oldorigin[1] = e.oldorigin[1];
		oldorigin[2] = e.oldorigin[2];

		origin[0] = e.origin[0];
		origin[1] = e.origin[1];
		origin[2] = e.origin[2];

		normalized_direction[0] = direction[0] = oldorigin[0] - origin[0];
		normalized_direction[1] = direction[1] = oldorigin[1] - origin[1];
		normalized_direction[2] = direction[2] = oldorigin[2] - origin[2];

		if (Math3D.VectorNormalize(normalized_direction) == 0.0f)
			return;

		Math3D.PerpendicularVector(perpvec, normalized_direction);
		Math3D.VectorScale(perpvec, e.frame / 2, perpvec);

		for (i = 0; i < 6; i++)
		{
			Math3D.RotatePointAroundVector(this.start_points[i], normalized_direction, perpvec, 360.0f / OpenGLRenderApi.NUM_BEAM_SEGS * i);

			Math3D.VectorAdd(this.start_points[i], origin, this.start_points[i]);
			Math3D.VectorAdd(this.start_points[i], direction, this.end_points[i]);
		}

		this.gl.glDisable(OpenGL.GL_TEXTURE_2D);
		this.gl.glEnable(OpenGL.GL_BLEND);
		this.gl.glDepthMask(false);

		r = OpenGLRenderApi.d_8to24table[e.skinnum & 0xFF] & 0xFF;
		g = (OpenGLRenderApi.d_8to24table[e.skinnum & 0xFF] >> 8) & 0xFF;
		b = (OpenGLRenderApi.d_8to24table[e.skinnum & 0xFF] >> 16) & 0xFF;

		r *= 1 / 255.0f;
		g *= 1 / 255.0f;
		b *= 1 / 255.0f;

		this.gl.glColor4f(r, g, b, e.alpha);

		this.gl.glBegin(OpenGL.GL_TRIANGLE_STRIP);

		float[] v;

		for (i = 0; i < OpenGLRenderApi.NUM_BEAM_SEGS; i++)
		{
			v = this.start_points[i];
			this.gl.glVertex3f(v[0], v[1], v[2]);
			v = this.end_points[i];
			this.gl.glVertex3f(v[0], v[1], v[2]);
			v = this.start_points[(i + 1) % OpenGLRenderApi.NUM_BEAM_SEGS];
			this.gl.glVertex3f(v[0], v[1], v[2]);
			v = this.end_points[(i + 1) % OpenGLRenderApi.NUM_BEAM_SEGS];
			this.gl.glVertex3f(v[0], v[1], v[2]);
		}

		this.gl.glEnd();

		this.gl.glEnable(OpenGL.GL_TEXTURE_2D);
		this.gl.glDisable(OpenGL.GL_BLEND);
		this.gl.glDepthMask(true);
	}

	private IOpenGL gl;

	public OpenGLRenderApi()
	{
		this.gl = new OpenTkGL();

		for (var i = 0; i < this.start_points.Length; i++)
			this.start_points[i] = new float[3];

		for (var i = 0; i < this.end_points.Length; i++)
			this.end_points[i] = new float[3];

		this.ImageConstructor();
		this.MeshConstructor();
		this.WarpConstructor();
	}
}