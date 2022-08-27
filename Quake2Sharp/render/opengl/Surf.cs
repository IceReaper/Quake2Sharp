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

using client.types;
using game.types;
using qcommon;
using render.types;
using System;
using types;
using util;

public partial class OpenGLRenderApi
{
	// GL_RSURF.C: surface-related refresh code
	private readonly float[] modelorg = { 0, 0, 0 }; // relative to viewpoint
	private msurface_t r_alpha_surfaces;
	public static readonly int BLOCK_WIDTH = 128;
	public static readonly int BLOCK_HEIGHT = 128;
	public static readonly int MAX_LIGHTMAPS = 128;
	private int c_visible_lightmaps;
	private int c_visible_textures;
	private static readonly int GL_LIGHTMAP_FORMAT = OpenGL.GL_RGBA;

	private readonly gllightmapstate_t gl_lms = new();
	/*
	=============================================================

		BRUSH MODELS

	=============================================================
	*/

	/*
	===============
	R_TextureAnimation

	Returns the proper texture for a given time and base texture
	===============
	*/
	private image_t R_TextureAnimation(mtexinfo_t tex)
	{
		int c;

		if (tex.next == null)
			return tex.image;

		c = this.currententity.frame % tex.numframes;

		while (c != 0)
		{
			tex = tex.next;
			c--;
		}

		return tex.image;
	}

	/*
	================
	DrawGLPoly
	================
	*/
	private void DrawGLPoly(glpoly_t p)
	{
		this.gl.glBegin(OpenGL.GL_POLYGON);

		for (var i = 0; i < p.numverts; i++)
		{
			this.gl.glTexCoord2f(p.s1(i), p.t1(i));
			this.gl.glVertex3f(p.x(i), p.y(i), p.z(i));
		}

		this.gl.glEnd();
	}

	//	  ============
	//	  PGM
	/*
	================
	DrawGLFlowingPoly -- version of DrawGLPoly that handles scrolling texture
	================
	*/
	private void DrawGLFlowingPoly(msurface_t fa)
	{
		var scroll = -64 * (this.r_newrefdef.time / 40.0f - (int)(this.r_newrefdef.time / 40.0f));

		if (scroll == 0.0f)
			scroll = -64.0f;

		this.gl.glBegin(OpenGL.GL_POLYGON);
		var p = fa.polys;

		for (var i = 0; i < p.numverts; i++)
		{
			this.gl.glTexCoord2f(p.s1(i) + scroll, p.t1(i));
			this.gl.glVertex3f(p.x(i), p.y(i), p.z(i));
		}

		this.gl.glEnd();
	}

	//	  PGM
	//	  ============

	/*
	** R_DrawTriangleOutlines
	*/
	private void R_DrawTriangleOutlines()
	{
		if (this.gl_showtris.value == 0)
			return;

		this.gl.glDisable(OpenGL.GL_TEXTURE_2D);
		this.gl.glDisable(OpenGL.GL_DEPTH_TEST);
		this.gl.glColor4f(1, 1, 1, 1);

		for (var i = 0; i < OpenGLRenderApi.MAX_LIGHTMAPS; i++)
		{
			msurface_t surf;

			for (surf = this.gl_lms.lightmap_surfaces[i]; surf != null; surf = surf.lightmapchain)
			{
				var p = surf.polys;

				for (; p != null; p = p.chain)
				{
					for (var j = 2; j < p.numverts; j++)
					{
						this.gl.glBegin(OpenGL.GL_LINE_STRIP);
						this.gl.glVertex3f(p.x(0), p.y(0), p.z(0));
						this.gl.glVertex3f(p.x(j - 1), p.y(j - 1), p.z(j - 1));
						this.gl.glVertex3f(p.x(j), p.y(j), p.z(j));
						this.gl.glVertex3f(p.x(0), p.y(0), p.z(0));
						this.gl.glEnd();
					}
				}
			}
		}

		this.gl.glEnable(OpenGL.GL_DEPTH_TEST);
		this.gl.glEnable(OpenGL.GL_TEXTURE_2D);
	}

	/*
	** DrawGLPolyChain
	*/
	private void DrawGLPolyChain(glpoly_t p, float soffset, float toffset)
	{
		if (soffset == 0 && toffset == 0)
		{
			for (; p != null; p = p.chain)
			{
				this.gl.glBegin(OpenGL.GL_POLYGON);

				for (var j = 0; j < p.numverts; j++)
				{
					this.gl.glTexCoord2f(p.s2(j), p.t2(j));
					this.gl.glVertex3f(p.x(j), p.y(j), p.z(j));
				}

				this.gl.glEnd();
			}
		}
		else
		{
			for (; p != null; p = p.chain)
			{
				this.gl.glBegin(OpenGL.GL_POLYGON);

				for (var j = 0; j < p.numverts; j++)
				{
					this.gl.glTexCoord2f(p.s2(j) - soffset, p.t2(j) - toffset);
					this.gl.glVertex3f(p.x(j), p.y(j), p.z(j));
				}

				this.gl.glEnd();
			}
		}
	}

	/*
	** R_BlendLightMaps
	**
	** This routine takes all the given light mapped surfaces in the world and
	** blends them into the framebuffer.
	*/
	private void R_BlendLightmaps()
	{
		int i;
		msurface_t surf;
		msurface_t newdrawsurf = null;

		// don't bother if we're set to fullbright
		if (this.r_fullbright.value != 0)
			return;

		if (this.r_worldmodel.lightdata == null)
			return;

		// don't bother writing Z
		this.gl.glDepthMask(false);

		/*
		** set the appropriate blending mode unless we're only looking at the
		** lightmaps.
		*/
		if (this.gl_lightmap.value == 0)
		{
			this.gl.glEnable(OpenGL.GL_BLEND);

			if (this.gl_saturatelighting.value != 0)
				this.gl.glBlendFunc(OpenGL.GL_ONE, OpenGL.GL_ONE);
			else
			{
				var format = this.gl_monolightmap.@string.ToUpper()[0];

				if (format != '0')
				{
					switch (format)
					{
						case 'I':
							this.gl.glBlendFunc(OpenGL.GL_ZERO, OpenGL.GL_SRC_COLOR);

							break;

						case 'L':
							this.gl.glBlendFunc(OpenGL.GL_ZERO, OpenGL.GL_SRC_COLOR);

							break;

						case 'A':
						default:
							this.gl.glBlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA);

							break;
					}
				}
				else
					this.gl.glBlendFunc(OpenGL.GL_ZERO, OpenGL.GL_SRC_COLOR);
			}
		}

		if (this.currentmodel == this.r_worldmodel)
			this.c_visible_lightmaps = 0;

		/*
		** render static lightmaps first
		*/
		for (i = 1; i < OpenGLRenderApi.MAX_LIGHTMAPS; i++)
		{
			if (this.gl_lms.lightmap_surfaces[i] != null)
			{
				if (this.currentmodel == this.r_worldmodel)
					this.c_visible_lightmaps++;

				this.GL_Bind(this.gl_state.lightmap_textures + i);

				for (surf = this.gl_lms.lightmap_surfaces[i]; surf != null; surf = surf.lightmapchain)
				{
					if (surf.polys != null)
						this.DrawGLPolyChain(surf.polys, 0, 0);
				}
			}
		}

		/*
		** render dynamic lightmaps
		*/
		if (this.gl_dynamic.value != 0)
		{
			this.LM_InitBlock();

			this.GL_Bind(this.gl_state.lightmap_textures + 0);

			if (this.currentmodel == this.r_worldmodel)
				this.c_visible_lightmaps++;

			newdrawsurf = this.gl_lms.lightmap_surfaces[0];

			for (surf = this.gl_lms.lightmap_surfaces[0]; surf != null; surf = surf.lightmapchain)
			{
				int smax, tmax;
				int[] @base;

				smax = (surf.extents[0] >> 4) + 1;
				tmax = (surf.extents[1] >> 4) + 1;

				pos_t lightPos = new(surf.dlight_s, surf.dlight_t);

				if (this.LM_AllocBlock(smax, tmax, lightPos))
				{
					// kopiere die koordinaten zurueck
					surf.dlight_s = lightPos.x;
					surf.dlight_t = lightPos.y;

					@base = this.gl_lms.lightmap_buffer;
					var position = surf.dlight_t * OpenGLRenderApi.BLOCK_WIDTH + surf.dlight_s;

					this.R_BuildLightMap(surf, @base, position, OpenGLRenderApi.BLOCK_WIDTH);
				}
				else
				{
					msurface_t drawsurf;

					// upload what we have so far
					this.LM_UploadBlock(true);

					// draw all surfaces that use this lightmap
					for (drawsurf = newdrawsurf; drawsurf != surf; drawsurf = drawsurf.lightmapchain)
					{
						if (drawsurf.polys != null)
						{
							this.DrawGLPolyChain(
								drawsurf.polys,
								(drawsurf.light_s - drawsurf.dlight_s) * (1.0f / 128.0f),
								(drawsurf.light_t - drawsurf.dlight_t) * (1.0f / 128.0f)
							);
						}
					}

					newdrawsurf = drawsurf;

					// clear the block
					this.LM_InitBlock();

					// try uploading the block now
					if (!this.LM_AllocBlock(smax, tmax, lightPos))
						Com.Error(Defines.ERR_FATAL, "Consecutive calls to LM_AllocBlock(" + smax + "," + tmax + ") failed (dynamic)\n");

					// kopiere die koordinaten zurueck
					surf.dlight_s = lightPos.x;
					surf.dlight_t = lightPos.y;

					@base = this.gl_lms.lightmap_buffer;
					var position = surf.dlight_t * OpenGLRenderApi.BLOCK_WIDTH + surf.dlight_s;

					this.R_BuildLightMap(surf, @base, position, OpenGLRenderApi.BLOCK_WIDTH);
				}
			}

			/*
			** draw remainder of dynamic lightmaps that haven't been uploaded yet
			*/
			if (newdrawsurf != null)
				this.LM_UploadBlock(true);

			for (surf = newdrawsurf; surf != null; surf = surf.lightmapchain)
			{
				if (surf.polys != null)
					this.DrawGLPolyChain(surf.polys, (surf.light_s - surf.dlight_s) * (1.0f / 128.0f), (surf.light_t - surf.dlight_t) * (1.0f / 128.0f));
			}
		}

		/*
		** restore state
		*/
		this.gl.glDisable(OpenGL.GL_BLEND);
		this.gl.glBlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA);
		this.gl.glDepthMask(true);
	}

	private readonly int[] temp2 = new int[34 * 34];

	/*
	================
	R_RenderBrushPoly
	================
	*/
	private void R_RenderBrushPoly(msurface_t fa)
	{
		int maps;
		image_t image;
		var is_dynamic = false;

		this.c_brush_polys++;

		image = this.R_TextureAnimation(fa.texinfo);

		if ((fa.flags & Defines.SURF_DRAWTURB) != 0)
		{
			this.GL_Bind(image.texnum);

			// warp texture, no lightmaps
			this.GL_TexEnv(OpenGL.GL_MODULATE);
			this.gl.glColor4f(this.gl_state.inverse_intensity, this.gl_state.inverse_intensity, this.gl_state.inverse_intensity, 1.0F);
			this.EmitWaterPolys(fa);
			this.GL_TexEnv(OpenGL.GL_REPLACE);

			return;
		}

		this.GL_Bind(image.texnum);
		this.GL_TexEnv(OpenGL.GL_REPLACE);

		//	  ======
		//	  PGM
		if ((fa.texinfo.flags & Defines.SURF_FLOWING) != 0)
			this.DrawGLFlowingPoly(fa);
		else
			this.DrawGLPoly(fa.polys);

		//	  PGM
		//	  ======

		// ersetzt goto
		var gotoDynamic = false;

		/*
		** check for lightmap modification
		*/
		for (maps = 0; maps < Defines.MAXLIGHTMAPS && fa.styles[maps] != (byte)255; maps++)
		{
			if (this.r_newrefdef.lightstyles[fa.styles[maps] & 0xFF].white != fa.cached_light[maps])
			{
				gotoDynamic = true;

				break;
			}
		}

		if (maps == 4)
			maps--;

		// dynamic this frame or dynamic previously
		if (gotoDynamic || fa.dlightframe == this.r_framecount)
		{
			//	label dynamic:
			if (this.gl_dynamic.value != 0)
			{
				if ((fa.texinfo.flags & (Defines.SURF_SKY | Defines.SURF_TRANS33 | Defines.SURF_TRANS66 | Defines.SURF_WARP)) == 0)
					is_dynamic = true;
			}
		}

		if (is_dynamic)
		{
			if (((fa.styles[maps] & 0xFF) >= 32 || fa.styles[maps] == 0) && fa.dlightframe != this.r_framecount)
			{
				// ist ersetzt durch temp2:	unsigned	temp[34*34];
				int smax, tmax;

				smax = (fa.extents[0] >> 4) + 1;
				tmax = (fa.extents[1] >> 4) + 1;

				this.R_BuildLightMap(fa, this.temp2, 0, smax);
				this.R_SetCacheState(fa);

				this.GL_Bind(this.gl_state.lightmap_textures + fa.lightmaptexturenum);

				this.gl.glTexSubImage2D(
					OpenGL.GL_TEXTURE_2D,
					0,
					fa.light_s,
					fa.light_t,
					smax,
					tmax,
					OpenGLRenderApi.GL_LIGHTMAP_FORMAT,
					OpenGL.GL_UNSIGNED_BYTE,
					this.temp2
				);

				fa.lightmapchain = this.gl_lms.lightmap_surfaces[fa.lightmaptexturenum];
				this.gl_lms.lightmap_surfaces[fa.lightmaptexturenum] = fa;
			}
			else
			{
				fa.lightmapchain = this.gl_lms.lightmap_surfaces[0];
				this.gl_lms.lightmap_surfaces[0] = fa;
			}
		}
		else
		{
			fa.lightmapchain = this.gl_lms.lightmap_surfaces[fa.lightmaptexturenum];
			this.gl_lms.lightmap_surfaces[fa.lightmaptexturenum] = fa;
		}
	}

	/*
	================
	R_DrawAlphaSurfaces

	Draw water surfaces and windows.
	The BSP tree is waled front to back, so unwinding the chain
	of alpha_surfaces will draw back to front, giving proper ordering.
	================
	*/
	protected void R_DrawAlphaSurfaces()
	{
		msurface_t s;
		float intens;

		//
		// go back to the world matrix
		//
		this.gl.glLoadMatrix(this.r_world_matrix);

		this.gl.glEnable(OpenGL.GL_BLEND);
		this.GL_TexEnv(OpenGL.GL_MODULATE);

		// the textures are prescaled up for a better lighting range,
		// so scale it back down
		intens = this.gl_state.inverse_intensity;

		for (s = this.r_alpha_surfaces; s != null; s = s.texturechain)
		{
			this.GL_Bind(s.texinfo.image.texnum);
			this.c_brush_polys++;

			if ((s.texinfo.flags & Defines.SURF_TRANS33) != 0)
				this.gl.glColor4f(intens, intens, intens, 0.33f);
			else if ((s.texinfo.flags & Defines.SURF_TRANS66) != 0)
				this.gl.glColor4f(intens, intens, intens, 0.66f);
			else
				this.gl.glColor4f(intens, intens, intens, 1);

			if ((s.flags & Defines.SURF_DRAWTURB) != 0)
				this.EmitWaterPolys(s);
			else if ((s.texinfo.flags & Defines.SURF_FLOWING) != 0) // PGM	9/16/98
				this.DrawGLFlowingPoly(s); // PGM
			else
				this.DrawGLPoly(s.polys);
		}

		this.GL_TexEnv(OpenGL.GL_REPLACE);
		this.gl.glColor4f(1, 1, 1, 1);
		this.gl.glDisable(OpenGL.GL_BLEND);

		this.r_alpha_surfaces = null;
	}

	/*
	================
	DrawTextureChains
	================
	*/
	private void DrawTextureChains()
	{
		int i;
		msurface_t s;
		image_t image;

		this.c_visible_textures = 0;

		if (!this.qglSelectTextureSGIS && !this.qglActiveTextureARB)
		{
			for (i = 0; i < this.numgltextures; i++)
			{
				image = this.gltextures[i];

				if (image.registration_sequence == 0)
					continue;

				s = image.texturechain;

				if (s == null)
					continue;

				this.c_visible_textures++;

				for (; s != null; s = s.texturechain)
					this.R_RenderBrushPoly(s);

				image.texturechain = null;
			}
		}
		else
		{
			for (i = 0; i < this.numgltextures; i++)
			{
				image = this.gltextures[i];

				if (image.registration_sequence == 0)
					continue;

				if (image.texturechain == null)
					continue;

				this.c_visible_textures++;

				for (s = image.texturechain; s != null; s = s.texturechain)
				{
					if ((s.flags & Defines.SURF_DRAWTURB) == 0)
						this.R_RenderBrushPoly(s);
				}
			}

			this.GL_EnableMultitexture(false);

			for (i = 0; i < this.numgltextures; i++)
			{
				image = this.gltextures[i];

				if (image.registration_sequence == 0)
					continue;

				s = image.texturechain;

				if (s == null)
					continue;

				for (; s != null; s = s.texturechain)
				{
					if ((s.flags & Defines.SURF_DRAWTURB) != 0)
						this.R_RenderBrushPoly(s);
				}

				image.texturechain = null;
			}
		}

		this.GL_TexEnv(OpenGL.GL_REPLACE);
	}

	// direct buffer
	private readonly int[] temp = new int[128 * 128];

	private void GL_RenderLightmappedPoly(msurface_t surf)
	{
		var nv = surf.polys.numverts;
		int map;
		var image = this.R_TextureAnimation(surf.texinfo);
		var is_dynamic = false;
		var lmtex = surf.lightmaptexturenum;
		glpoly_t p;

		// ersetzt goto
		var gotoDynamic = false;

		for (map = 0; map < Defines.MAXLIGHTMAPS && surf.styles[map] != (byte)255; map++)
		{
			if (this.r_newrefdef.lightstyles[surf.styles[map] & 0xFF].white != surf.cached_light[map])
			{
				gotoDynamic = true;

				break;
			}
		}

		if (map == 4)
			map--;

		// dynamic this frame or dynamic previously
		if (gotoDynamic || surf.dlightframe == this.r_framecount)
		{
			//	label dynamic:
			if (this.gl_dynamic.value != 0)
			{
				if ((surf.texinfo.flags & (Defines.SURF_SKY | Defines.SURF_TRANS33 | Defines.SURF_TRANS66 | Defines.SURF_WARP)) == 0)
					is_dynamic = true;
			}
		}

		if (is_dynamic)
		{
			// ist raus gezogen worden int[] temp = new int[128*128];
			int smax, tmax;

			if (((surf.styles[map] & 0xFF) >= 32 || surf.styles[map] == 0) && surf.dlightframe != this.r_framecount)
			{
				smax = (surf.extents[0] >> 4) + 1;
				tmax = (surf.extents[1] >> 4) + 1;

				this.R_BuildLightMap(surf, this.temp, 0, smax);
				this.R_SetCacheState(surf);

				this.GL_MBind(OpenGL.GL_TEXTURE1, this.gl_state.lightmap_textures + surf.lightmaptexturenum);

				lmtex = surf.lightmaptexturenum;

				this.gl.glTexSubImage2D(
					OpenGL.GL_TEXTURE_2D,
					0,
					surf.light_s,
					surf.light_t,
					smax,
					tmax,
					OpenGLRenderApi.GL_LIGHTMAP_FORMAT,
					OpenGL.GL_UNSIGNED_BYTE,
					this.temp
				);
			}
			else
			{
				smax = (surf.extents[0] >> 4) + 1;
				tmax = (surf.extents[1] >> 4) + 1;

				this.R_BuildLightMap(surf, this.temp, 0, smax);

				this.GL_MBind(OpenGL.GL_TEXTURE1, this.gl_state.lightmap_textures + 0);

				lmtex = 0;

				this.gl.glTexSubImage2D(
					OpenGL.GL_TEXTURE_2D,
					0,
					surf.light_s,
					surf.light_t,
					smax,
					tmax,
					OpenGLRenderApi.GL_LIGHTMAP_FORMAT,
					OpenGL.GL_UNSIGNED_BYTE,
					this.temp
				);
			}

			this.c_brush_polys++;

			this.GL_MBind(OpenGL.GL_TEXTURE0, image.texnum);
			this.GL_MBind(OpenGL.GL_TEXTURE1, this.gl_state.lightmap_textures + lmtex);

			// ==========
			//	  PGM
			if ((surf.texinfo.flags & Defines.SURF_FLOWING) != 0)
			{
				float scroll;

				scroll = -64 * (this.r_newrefdef.time / 40.0f - (int)(this.r_newrefdef.time / 40.0f));

				if (scroll == 0.0f)
					scroll = -64.0f;

				for (p = surf.polys; p != null; p = p.chain)
				{
					this.gl.glBegin(OpenGL.GL_POLYGON);

					for (var i = 0; i < nv; i++)
					{
						this.gl.glMultiTexCoord2f(OpenGL.GL_TEXTURE0, p.s1(i) + scroll, p.t1(i));
						this.gl.glMultiTexCoord2f(OpenGL.GL_TEXTURE1, p.s2(i), p.t2(i));

						//gglMTexCoord2fSGIS( GL_TEXTURE0, v[3] + scroll, v[4]);
						//gglMTexCoord2fSGIS( GL_TEXTURE1, v[5], v[6]);
						this.gl.glVertex3f(p.x(i), p.y(i), p.z(i));
					}

					this.gl.glEnd();
				}
			}
			else
			{
				for (p = surf.polys; p != null; p = p.chain)
				{
					this.gl.glBegin(OpenGL.GL_POLYGON);

					for (var i = 0; i < nv; i++)
					{
						this.gl.glMultiTexCoord2f(OpenGL.GL_TEXTURE0, p.s1(i), p.t1(i));
						this.gl.glMultiTexCoord2f(OpenGL.GL_TEXTURE1, p.s2(i), p.t2(i));

						//gglMTexCoord2fSGIS( GL_TEXTURE0, v[3], v[4]);
						//gglMTexCoord2fSGIS( GL_TEXTURE1, v[5], v[6]);
						this.gl.glVertex3f(p.x(i), p.y(i), p.z(i));
					}

					this.gl.glEnd();
				}
			}

			// PGM
			// ==========
		}
		else
		{
			this.c_brush_polys++;

			this.GL_MBind(OpenGL.GL_TEXTURE0, image.texnum);
			this.GL_MBind(OpenGL.GL_TEXTURE1, this.gl_state.lightmap_textures + lmtex);

			// ==========
			//	  PGM
			if ((surf.texinfo.flags & Defines.SURF_FLOWING) != 0)
			{
				float scroll;

				scroll = -64 * (this.r_newrefdef.time / 40.0f - (int)(this.r_newrefdef.time / 40.0f));

				if (scroll == 0.0)
					scroll = -64.0f;

				for (p = surf.polys; p != null; p = p.chain)
				{
					this.gl.glBegin(OpenGL.GL_POLYGON);

					for (var i = 0; i < nv; i++)
					{
						this.gl.glMultiTexCoord2f(OpenGL.GL_TEXTURE0, p.s1(i) + scroll, p.t1(i));
						this.gl.glMultiTexCoord2f(OpenGL.GL_TEXTURE1, p.s2(i), p.t2(i));

						//gglMTexCoord2fSGIS( GL_TEXTURE0, v[3] + scroll, v[4]);
						//gglMTexCoord2fSGIS( GL_TEXTURE1, v[5], v[6]);
						this.gl.glVertex3f(p.x(i), p.y(i), p.z(i));
					}

					this.gl.glEnd();
				}
			}
			else
			{
				// PGM
				//  ==========
				for (p = surf.polys; p != null; p = p.chain)
				{
					this.gl.glBegin(OpenGL.GL_POLYGON);

					for (var i = 0; i < nv; i++)
					{
						this.gl.glMultiTexCoord2f(OpenGL.GL_TEXTURE0, p.s1(i), p.t1(i));
						this.gl.glMultiTexCoord2f(OpenGL.GL_TEXTURE1, p.s2(i), p.t2(i));

						//gglMTexCoord2fSGIS( GL_TEXTURE0, v[3], v[4]);
						//gglMTexCoord2fSGIS( GL_TEXTURE1, v[5], v[6]);
						this.gl.glVertex3f(p.x(i), p.y(i), p.z(i));
					}

					this.gl.glEnd();
				}

				// ==========
				// PGM
			}

			// PGM
			// ==========
		}
	}

	/*
	=================
	R_DrawInlineBModel
	=================
	*/
	private void R_DrawInlineBModel()
	{
		int i, k;
		cplane_t pplane;
		float dot;
		msurface_t psurf;
		dlight_t lt;

		// calculate dynamic lighting for bmodel
		if (this.gl_flashblend.value == 0)
		{
			for (k = 0; k < this.r_newrefdef.num_dlights; k++)
			{
				lt = this.r_newrefdef.dlights[k];
				this.R_MarkLights(lt, 1 << k, this.currentmodel.nodes[this.currentmodel.firstnode]);
			}
		}

		// psurf = &currentmodel->surfaces[currentmodel->firstmodelsurface];
		var psurfp = this.currentmodel.firstmodelsurface;
		msurface_t[] surfaces;
		surfaces = this.currentmodel.surfaces;

		//psurf = surfaces[psurfp];

		if ((this.currententity.flags & Defines.RF_TRANSLUCENT) != 0)
		{
			this.gl.glEnable(OpenGL.GL_BLEND);
			this.gl.glColor4f(1, 1, 1, 0.25f);
			this.GL_TexEnv(OpenGL.GL_MODULATE);
		}

		//
		// draw texture
		//
		for (i = 0; i < this.currentmodel.nummodelsurfaces; i++)
		{
			psurf = surfaces[psurfp++];

			// find which side of the node we are on
			pplane = psurf.plane;

			dot = Math3D.DotProduct(this.modelorg, pplane.normal) - pplane.dist;

			// draw the polygon
			if (((psurf.flags & Defines.SURF_PLANEBACK) != 0 && dot < -Base.BACKFACE_EPSILON)
			    || ((psurf.flags & Defines.SURF_PLANEBACK) == 0 && dot > Base.BACKFACE_EPSILON))
			{
				if ((psurf.texinfo.flags & (Defines.SURF_TRANS33 | Defines.SURF_TRANS66)) != 0)
				{
					// add to the translucent chain
					psurf.texturechain = this.r_alpha_surfaces;
					this.r_alpha_surfaces = psurf;
				}
				else if (this.qglMTexCoord2fSGIS && (psurf.flags & Defines.SURF_DRAWTURB) == 0)
					this.GL_RenderLightmappedPoly(psurf);
				else
				{
					this.GL_EnableMultitexture(false);
					this.R_RenderBrushPoly(psurf);
					this.GL_EnableMultitexture(true);
				}
			}
		}

		if ((this.currententity.flags & Defines.RF_TRANSLUCENT) == 0)
		{
			if (!this.qglMTexCoord2fSGIS)
				this.R_BlendLightmaps();
		}
		else
		{
			this.gl.glDisable(OpenGL.GL_BLEND);
			this.gl.glColor4f(1, 1, 1, 1);
			this.GL_TexEnv(OpenGL.GL_REPLACE);
		}
	}

	/*
	=================
	R_DrawBrushModel
	=================
	*/
	protected void R_DrawBrushModel(entity_t e)
	{
		float[] mins = { 0, 0, 0 };
		float[] maxs = { 0, 0, 0 };
		int i;
		bool rotated;

		if (this.currentmodel.nummodelsurfaces == 0)
			return;

		this.currententity = e;
		this.gl_state.currenttextures[0] = this.gl_state.currenttextures[1] = -1;

		if (e.angles[0] != 0 || e.angles[1] != 0 || e.angles[2] != 0)
		{
			rotated = true;

			for (i = 0; i < 3; i++)
			{
				mins[i] = e.origin[i] - this.currentmodel.radius;
				maxs[i] = e.origin[i] + this.currentmodel.radius;

				//System.out.println("rotate: " + Lib.vtos(mins) + " " + Lib.vtos(maxs));
			}
		}
		else
		{
			rotated = false;
			Math3D.VectorAdd(e.origin, this.currentmodel.mins, mins);
			Math3D.VectorAdd(e.origin, this.currentmodel.maxs, maxs);

			//System.out.println("       "  + Lib.vtos(mins) + " " + Lib.vtos(maxs));
		}

		if (this.R_CullBox(mins, maxs))
		{
			//System.out.println("origin "  + Lib.vtos(e.origin) + " +++ " + Lib.vtos(currentmodel.mins));
			return;
		}

		this.gl.glColor3f(1, 1, 1);

		// memset (gl_lms.lightmap_surfaces, 0, sizeof(gl_lms.lightmap_surfaces));
		this.gl_lms.clearLightmapSurfaces();

		Math3D.VectorSubtract(this.r_newrefdef.vieworg, e.origin, this.modelorg);

		if (rotated)
		{
			float[] temp = { 0, 0, 0 };
			float[] forward = { 0, 0, 0 };
			float[] right = { 0, 0, 0 };
			float[] up = { 0, 0, 0 };

			Math3D.VectorCopy(this.modelorg, temp);
			Math3D.AngleVectors(e.angles, forward, right, up);
			this.modelorg[0] = Math3D.DotProduct(temp, forward);
			this.modelorg[1] = -Math3D.DotProduct(temp, right);
			this.modelorg[2] = Math3D.DotProduct(temp, up);
		}

		this.gl.glPushMatrix();

		e.angles[0] = -e.angles[0]; // stupid quake bug
		e.angles[2] = -e.angles[2]; // stupid quake bug
		this.R_RotateForEntity(e);
		e.angles[0] = -e.angles[0]; // stupid quake bug
		e.angles[2] = -e.angles[2]; // stupid quake bug

		this.GL_EnableMultitexture(true);
		this.GL_SelectTexture(OpenGL.GL_TEXTURE0);
		this.GL_TexEnv(OpenGL.GL_REPLACE);
		this.GL_SelectTexture(OpenGL.GL_TEXTURE1);
		this.GL_TexEnv(OpenGL.GL_MODULATE);

		this.R_DrawInlineBModel();
		this.GL_EnableMultitexture(false);

		this.gl.glPopMatrix();
	}

	/*
	=============================================================

		WORLD MODEL

	=============================================================
	*/

	/*
	================
	R_RecursiveWorldNode
	================
	*/
	private void R_RecursiveWorldNode(mnode_t node)
	{
		int c, side, sidebit;
		cplane_t plane;
		msurface_t surf;
		msurface_t mark;
		mleaf_t pleaf;
		float dot = 0;
		image_t image;

		if (node.contents == Defines.CONTENTS_SOLID)
			return; // solid

		if (node.visframe != this.r_visframecount)
			return;

		if (this.R_CullBox(node.mins, node.maxs))
			return;

		// if a leaf node, draw stuff
		if (node.contents != -1)
		{
			pleaf = (mleaf_t)node;

			// check for door connected areas
			if (this.r_newrefdef.areabits != null)
			{
				if ((this.r_newrefdef.areabits[pleaf.area >> 3] & 0xFF & (1 << (pleaf.area & 7))) == 0)
					return; // not visible
			}

			var markp = 0;

			mark = pleaf.getMarkSurface(markp); // first marked surface
			c = pleaf.nummarksurfaces;

			if (c != 0)
			{
				do
				{
					mark.visframe = this.r_framecount;
					mark = pleaf.getMarkSurface(++markp); // next surface
				}
				while (--c != 0);
			}

			return;
		}

		// node is just a decision point, so go down the apropriate sides

		// find which side of the node we are on
		plane = node.plane;

		switch (plane.type)
		{
			case Defines.PLANE_X:
				dot = this.modelorg[0] - plane.dist;

				break;

			case Defines.PLANE_Y:
				dot = this.modelorg[1] - plane.dist;

				break;

			case Defines.PLANE_Z:
				dot = this.modelorg[2] - plane.dist;

				break;

			default:
				dot = Math3D.DotProduct(this.modelorg, plane.normal) - plane.dist;

				break;
		}

		if (dot >= 0.0f)
		{
			side = 0;
			sidebit = 0;
		}
		else
		{
			side = 1;
			sidebit = Defines.SURF_PLANEBACK;
		}

		// recurse down the children, front side first
		this.R_RecursiveWorldNode(node.children[side]);

		// draw stuff
		//for ( c = node.numsurfaces, surf = r_worldmodel.surfaces[node.firstsurface]; c != 0 ; c--, surf++)
		for (c = 0; c < node.numsurfaces; c++)
		{
			surf = this.r_worldmodel.surfaces[node.firstsurface + c];

			if (surf.visframe != this.r_framecount)
				continue;

			if ((surf.flags & Defines.SURF_PLANEBACK) != sidebit)
				continue; // wrong side

			if ((surf.texinfo.flags & Defines.SURF_SKY) != 0)
			{
				// just adds to visible sky bounds
				this.R_AddSkySurface(surf);
			}
			else if ((surf.texinfo.flags & (Defines.SURF_TRANS33 | Defines.SURF_TRANS66)) != 0)
			{
				// add to the translucent chain
				surf.texturechain = this.r_alpha_surfaces;
				this.r_alpha_surfaces = surf;
			}
			else
			{
				if (this.qglMTexCoord2fSGIS && (surf.flags & Defines.SURF_DRAWTURB) == 0)
					this.GL_RenderLightmappedPoly(surf);
				else
				{
					// the polygon is visible, so add it to the texture
					// sorted chain
					// FIXME: this is a hack for animation
					image = this.R_TextureAnimation(surf.texinfo);
					surf.texturechain = image.texturechain;
					image.texturechain = surf;
				}
			}
		}

		// recurse down the back side
		this.R_RecursiveWorldNode(node.children[1 - side]);
	}

	/*
	=============
	R_DrawWorld
	=============
	*/
	protected void R_DrawWorld()
	{
		entity_t ent = new();

		// auto cycle the world frame for texture animation
		ent.frame = (int)(this.r_newrefdef.time * 2);
		this.currententity = ent;

		if (this.r_drawworld.value == 0)
			return;

		if ((this.r_newrefdef.rdflags & Defines.RDF_NOWORLDMODEL) != 0)
			return;

		this.currentmodel = this.r_worldmodel;

		Math3D.VectorCopy(this.r_newrefdef.vieworg, this.modelorg);

		this.gl_state.currenttextures[0] = this.gl_state.currenttextures[1] = -1;

		this.gl.glColor3f(1, 1, 1);

		// memset (gl_lms.lightmap_surfaces, 0, sizeof(gl_lms.lightmap_surfaces));
		this.gl_lms.clearLightmapSurfaces();

		this.R_ClearSkyBox();

		if (this.qglMTexCoord2fSGIS)
		{
			this.GL_EnableMultitexture(true);

			this.GL_SelectTexture(OpenGL.GL_TEXTURE0);
			this.GL_TexEnv(OpenGL.GL_REPLACE);
			this.GL_SelectTexture(OpenGL.GL_TEXTURE1);

			if (this.gl_lightmap.value != 0)
				this.GL_TexEnv(OpenGL.GL_REPLACE);
			else
				this.GL_TexEnv(OpenGL.GL_MODULATE);

			this.R_RecursiveWorldNode(this.r_worldmodel.nodes[0]); // root node

			this.GL_EnableMultitexture(false);
		}
		else
			this.R_RecursiveWorldNode(this.r_worldmodel.nodes[0]); // root node

		/*
		** theoretically nothing should happen in the next two functions
		** if multitexture is enabled
		*/
		this.DrawTextureChains();
		this.R_BlendLightmaps();

		this.R_DrawSkyBox();

		this.R_DrawTriangleOutlines();
	}

	private readonly byte[] fatvis = new byte[Defines.MAX_MAP_LEAFS / 8];

	/*
	===============
	R_MarkLeaves

	Mark the leaves and nodes that are in the PVS for the current
	cluster
	===============
	*/
	protected void R_MarkLeaves()
	{
		byte[] vis;

		//byte[] fatvis = new byte[Defines.MAX_MAP_LEAFS / 8];

		Array.Fill(this.fatvis, (byte)0);

		mnode_t node;
		int i, c;
		mleaf_t leaf;
		int cluster;

		if (this.r_oldviewcluster == this.r_viewcluster
		    && this.r_oldviewcluster2 == this.r_viewcluster2
		    && this.r_novis.value == 0
		    && this.r_viewcluster != -1)
			return;

		// development aid to let you run around and see exactly where
		// the pvs ends
		if (this.gl_lockpvs.value != 0)
			return;

		this.r_visframecount++;
		this.r_oldviewcluster = this.r_viewcluster;
		this.r_oldviewcluster2 = this.r_viewcluster2;

		if (this.r_novis.value != 0 || this.r_viewcluster == -1 || this.r_worldmodel.vis == null)
		{
			// mark everything
			for (i = 0; i < this.r_worldmodel.numleafs; i++)
				this.r_worldmodel.leafs[i].visframe = this.r_visframecount;

			for (i = 0; i < this.r_worldmodel.numnodes; i++)
				this.r_worldmodel.nodes[i].visframe = this.r_visframecount;

			return;
		}

		vis = this.Mod_ClusterPVS(this.r_viewcluster, this.r_worldmodel);

		// may have to combine two clusters because of solid water boundaries
		if (this.r_viewcluster2 != this.r_viewcluster)
		{
			// memcpy (fatvis, vis, (r_worldmodel.numleafs+7)/8);
			Array.Copy(vis, 0, this.fatvis, 0, (this.r_worldmodel.numleafs + 7) / 8);
			vis = this.Mod_ClusterPVS(this.r_viewcluster2, this.r_worldmodel);
			c = (this.r_worldmodel.numleafs + 31) / 32;
			var k = 0;

			for (i = 0; i < c; i++)
			{
				this.fatvis[k] |= vis[k++];
				this.fatvis[k] |= vis[k++];
				this.fatvis[k] |= vis[k++];
				this.fatvis[k] |= vis[k++];
			}

			vis = this.fatvis;
		}

		for (i = 0; i < this.r_worldmodel.numleafs; i++)
		{
			leaf = this.r_worldmodel.leafs[i];
			cluster = leaf.cluster;

			if (cluster == -1)
				continue;

			if ((vis[cluster >> 3] & 0xFF & (1 << (cluster & 7))) != 0)
			{
				node = (mnode_t)leaf;

				do
				{
					if (node.visframe == this.r_visframecount)
						break;

					node.visframe = this.r_visframecount;
					node = node.parent;
				}
				while (node != null);
			}
		}
	}

	/*
	=============================================================================

	  LIGHTMAP ALLOCATION

	=============================================================================
	*/

	private void LM_InitBlock()
	{
		Array.Fill(this.gl_lms.allocated, 0);
	}

	private void LM_UploadBlock(bool dynamic)
	{
		int texture;
		var height = 0;

		if (dynamic)
			texture = 0;
		else
			texture = this.gl_lms.current_lightmap_texture;

		this.GL_Bind(this.gl_state.lightmap_textures + texture);
		this.gl.glTexParameterf(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_LINEAR);
		this.gl.glTexParameterf(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_LINEAR);

		if (dynamic)
		{
			int i;

			for (i = 0; i < OpenGLRenderApi.BLOCK_WIDTH; i++)
			{
				if (this.gl_lms.allocated[i] > height)
					height = this.gl_lms.allocated[i];
			}

			this.gl.glTexSubImage2D(
				OpenGL.GL_TEXTURE_2D,
				0,
				0,
				0,
				OpenGLRenderApi.BLOCK_WIDTH,
				height,
				OpenGLRenderApi.GL_LIGHTMAP_FORMAT,
				OpenGL.GL_UNSIGNED_BYTE,
				this.gl_lms.lightmap_buffer
			);
		}
		else
		{
			this.gl.glTexImage2D(
				OpenGL.GL_TEXTURE_2D,
				0,
				this.gl_lms.internal_format,
				OpenGLRenderApi.BLOCK_WIDTH,
				OpenGLRenderApi.BLOCK_HEIGHT,
				0,
				OpenGLRenderApi.GL_LIGHTMAP_FORMAT,
				OpenGL.GL_UNSIGNED_BYTE,
				this.gl_lms.lightmap_buffer
			);

			if (++this.gl_lms.current_lightmap_texture == OpenGLRenderApi.MAX_LIGHTMAPS)
				Com.Error(Defines.ERR_DROP, "LM_UploadBlock() - MAX_LIGHTMAPS exceeded\n");

			//debugLightmap(gl_lms.lightmap_buffer, 128, 128, 4);
		}
	}

	// returns a texture number and the position inside it
	private bool LM_AllocBlock(int w, int h, pos_t pos)
	{
		var x = pos.x;
		int i, j;
		int best, best2;

		best = OpenGLRenderApi.BLOCK_HEIGHT;

		for (i = 0; i < OpenGLRenderApi.BLOCK_WIDTH - w; i++)
		{
			best2 = 0;

			for (j = 0; j < w; j++)
			{
				if (this.gl_lms.allocated[i + j] >= best)
					break;

				if (this.gl_lms.allocated[i + j] > best2)
					best2 = this.gl_lms.allocated[i + j];
			}

			if (j == w)
			{
				// this is a valid spot
				pos.x = x = i;
				pos.y = best = best2;
			}
		}

		if (best + h > OpenGLRenderApi.BLOCK_HEIGHT)
			return false;

		for (i = 0; i < w; i++)
			this.gl_lms.allocated[x + i] = best + h;

		return true;
	}

	/*
	================
	GL_BuildPolygonFromSurface
	================
	*/
	protected void GL_BuildPolygonFromSurface(msurface_t fa)
	{
		int lindex, lnumverts;
		medge_t[] pedges;
		medge_t r_pedge;
		float[] vec;
		float s, t;
		glpoly_t poly;
		float[] total = { 0, 0, 0 };

		// reconstruct the polygon
		pedges = this.currentmodel.edges;
		lnumverts = fa.numedges;
		Math3D.VectorClear(total);

		//
		// draw texture
		//
		// poly = Hunk_Alloc (sizeof(glpoly_t) + (lnumverts-4) * VERTEXSIZE*sizeof(float));
		poly = Polygon.create(lnumverts);

		poly.next = fa.polys;
		poly.flags = fa.flags;
		fa.polys = poly;

		for (var i = 0; i < lnumverts; i++)
		{
			lindex = this.currentmodel.surfedges[fa.firstedge + i];

			if (lindex > 0)
			{
				r_pedge = pedges[lindex];
				vec = this.currentmodel.vertexes[r_pedge.v[0]].position;
			}
			else
			{
				r_pedge = pedges[-lindex];
				vec = this.currentmodel.vertexes[r_pedge.v[1]].position;
			}

			s = Math3D.DotProduct(vec, fa.texinfo.vecs[0]) + fa.texinfo.vecs[0][3];
			s /= fa.texinfo.image.width;

			t = Math3D.DotProduct(vec, fa.texinfo.vecs[1]) + fa.texinfo.vecs[1][3];
			t /= fa.texinfo.image.height;

			Math3D.VectorAdd(total, vec, total);

			//Math3D.VectorCopy (vec, poly.verts[i]);
			poly.x(i, vec[0]);
			poly.y(i, vec[1]);
			poly.z(i, vec[2]);

			//poly.verts[i][3] = s;
			//poly.verts[i][4] = t;
			poly.s1(i, s);
			poly.t1(i, t);

			//
			// lightmap texture coordinates
			//
			s = Math3D.DotProduct(vec, fa.texinfo.vecs[0]) + fa.texinfo.vecs[0][3];
			s -= fa.texturemins[0];
			s += fa.light_s * 16;
			s += 8;
			s /= OpenGLRenderApi.BLOCK_WIDTH * 16; //fa.texinfo.texture.width;

			t = Math3D.DotProduct(vec, fa.texinfo.vecs[1]) + fa.texinfo.vecs[1][3];
			t -= fa.texturemins[1];
			t += fa.light_t * 16;
			t += 8;
			t /= OpenGLRenderApi.BLOCK_HEIGHT * 16; //fa.texinfo.texture.height;

			//poly.verts[i][5] = s;
			//poly.verts[i][6] = t;
			poly.s2(i, s);
			poly.t2(i, t);
		}
	}

	/*
	========================
	GL_CreateSurfaceLightmap
	========================
	*/
	protected void GL_CreateSurfaceLightmap(msurface_t surf)
	{
		int smax, tmax;
		int[] @base;

		if ((surf.flags & (Defines.SURF_DRAWSKY | Defines.SURF_DRAWTURB)) != 0)
			return;

		smax = (surf.extents[0] >> 4) + 1;
		tmax = (surf.extents[1] >> 4) + 1;

		pos_t lightPos = new(surf.light_s, surf.light_t);

		if (!this.LM_AllocBlock(smax, tmax, lightPos))
		{
			this.LM_UploadBlock(false);
			this.LM_InitBlock();
			lightPos = new(surf.light_s, surf.light_t);

			if (!this.LM_AllocBlock(smax, tmax, lightPos))
				Com.Error(Defines.ERR_FATAL, "Consecutive calls to LM_AllocBlock(" + smax + "," + tmax + ") failed\n");
		}

		// kopiere die koordinaten zurueck
		surf.light_s = lightPos.x;
		surf.light_t = lightPos.y;

		surf.lightmaptexturenum = this.gl_lms.current_lightmap_texture;

		var basep = surf.light_t * OpenGLRenderApi.BLOCK_WIDTH + surf.light_s;
		@base = this.gl_lms.lightmap_buffer;
		var position = basep;

		this.R_SetCacheState(surf);
		this.R_BuildLightMap(surf, @base, position, OpenGLRenderApi.BLOCK_WIDTH);
	}

	private lightstyle_t[] lightstyles;
	private readonly int[] dummy = new int[128 * 128];

	/*
	==================
	GL_BeginBuildingLightmaps

	==================
	*/
	protected void GL_BeginBuildingLightmaps(model_t m)
	{
		// static lightstyle_t	lightstyles[MAX_LIGHTSTYLES];
		int i;

		// init lightstyles
		if (this.lightstyles == null)
		{
			this.lightstyles = new lightstyle_t[Defines.MAX_LIGHTSTYLES];

			for (i = 0; i < this.lightstyles.Length; i++)
				this.lightstyles[i] = new();
		}

		// memset( gl_lms.allocated, 0, sizeof(gl_lms.allocated) );
		Array.Fill(this.gl_lms.allocated, 0);

		this.r_framecount = 1; // no dlightcache

		this.GL_EnableMultitexture(true);
		this.GL_SelectTexture(OpenGL.GL_TEXTURE1);

		/*
		** setup the base lightstyles so the lightmaps won't have to be regenerated
		** the first time they're seen
		*/
		for (i = 0; i < Defines.MAX_LIGHTSTYLES; i++)
		{
			this.lightstyles[i].rgb[0] = 1;
			this.lightstyles[i].rgb[1] = 1;
			this.lightstyles[i].rgb[2] = 1;
			this.lightstyles[i].white = 3;
		}

		this.r_newrefdef.lightstyles = this.lightstyles;

		if (this.gl_state.lightmap_textures == 0)
			this.gl_state.lightmap_textures = Base.TEXNUM_LIGHTMAPS;

		this.gl_lms.current_lightmap_texture = 1;

		/*
		** if mono lightmaps are enabled and we want to use alpha
		** blending (a,1-a) then we're likely running on a 3DLabs
		** Permedia2.  In a perfect world we'd use a GL_ALPHA lightmap
		** in order to conserve space and maximize bandwidth, however 
		** this isn't a perfect world.
		**
		** So we have to use alpha lightmaps, but stored in GL_RGBA format,
		** which means we only get 1/16th the color resolution we should when
		** using alpha lightmaps.  If we find another board that supports
		** only alpha lightmaps but that can at least support the GL_ALPHA
		** format then we should change this code to use real alpha maps.
		*/

		var format = this.gl_monolightmap.@string.ToUpper()[0];

		if (format == 'A')
			this.gl_lms.internal_format = this.gl_tex_alpha_format;
		/*
		** try to do hacked colored lighting with a blended texture
		*/
		else if (format == 'C')
			this.gl_lms.internal_format = this.gl_tex_alpha_format;
		else if (format == 'I')
			this.gl_lms.internal_format = OpenGL.GL_INTENSITY8;
		else if (format == 'L')
			this.gl_lms.internal_format = OpenGL.GL_LUMINANCE8;
		else
			this.gl_lms.internal_format = this.gl_tex_solid_format;

		/*
		** initialize the dynamic lightmap texture
		*/
		this.GL_Bind(this.gl_state.lightmap_textures + 0);
		this.gl.glTexParameterf(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_LINEAR);
		this.gl.glTexParameterf(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_LINEAR);

		this.gl.glTexImage2D(
			OpenGL.GL_TEXTURE_2D,
			0,
			this.gl_lms.internal_format,
			OpenGLRenderApi.BLOCK_WIDTH,
			OpenGLRenderApi.BLOCK_HEIGHT,
			0,
			OpenGLRenderApi.GL_LIGHTMAP_FORMAT,
			OpenGL.GL_UNSIGNED_BYTE,
			this.dummy
		);
	}

	/*
	=======================
	GL_EndBuildingLightmaps
	=======================
	*/
	protected void GL_EndBuildingLightmaps()
	{
		this.LM_UploadBlock(false);
		this.GL_EnableMultitexture(false);
	}

	//ImageFrame frame;

	//	void debugLightmap(byte[] buf, int w, int h, float scale) {
	//		IntBuffer pix = ByteBuffer.wrap(buf).order(ByteOrder.LITTLE_ENDIAN).asIntBuffer();
	//		
	//		int[] pixel = new int[w * h];
	//		
	//		pix.get(pixel);
	//		
	//		BufferedImage image = new BufferedImage(w, h, BufferedImage.TYPE_4BYTE_ABGR);
	//		image.setRGB(0,  0, w, h, pixel, 0, w);
	//		AffineTransformOp op = new AffineTransformOp(AffineTransform.getScaleInstance(scale, scale), AffineTransformOp.TYPE_NEAREST_NEIGHBOR);
	//		BufferedImage tmp = op.filter(image, null);
	//		
	//		if (frame == null) {
	//			frame = new ImageFrame(null);
	//			frame.show();
	//		} 
	//		frame.showImage(tmp);
	//		
	//	}
}