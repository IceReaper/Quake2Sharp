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
using qcommon;
using qcommon.types;
using render.types;
using util;

public partial class OpenGLRenderApi
{
	// g_mesh.c: triangle model functions
	/*
	 * =============================================================
	 * 
	 * ALIAS MODELS
	 * 
	 * =============================================================
	 */
	private readonly float[][] r_avertexnormals = Anorms.VERTEXNORMALS;
	private readonly float[][] s_lerped = new float[qfiles.MAX_VERTS][];
	private readonly float[] shadevector = { 0, 0, 0 };
	private readonly float[] shadelight = { 0, 0, 0 };

	// precalculated dot products for quantized angles
	private static readonly int SHADEDOT_QUANT = 16;
	private readonly float[][] r_avertexnormal_dots = Anorms.VERTEXNORMAL_DOTS;
	private float[] shadedots;

	private void GL_LerpVerts(int nverts, int[] ov, int[] v, float[][] lerp, float[] move, float[] frontv, float[] backv)
	{
		int ovv, vv;

		//PMM -- added RF_SHELL_DOUBLE, RF_SHELL_HALF_DAM
		if ((this.currententity.flags
			    & (Defines.RF_SHELL_RED | Defines.RF_SHELL_GREEN | Defines.RF_SHELL_BLUE | Defines.RF_SHELL_DOUBLE | Defines.RF_SHELL_HALF_DAM))
		    != 0)
		{
			float[] normal;

			for (var i = 0;
			     i < nverts;
			     i++ /* , v++, ov++, lerp+=4 */
			    )
			{
				vv = v[i];
				normal = this.r_avertexnormals[((uint)vv >> 24) & 0xFF];
				ovv = ov[i];

				lerp[i][0] = move[0] + (ovv & 0xFF) * backv[0] + (vv & 0xFF) * frontv[0] + normal[0] * Defines.POWERSUIT_SCALE;
				lerp[i][1] = move[1] + (((uint)ovv >> 8) & 0xFF) * backv[1] + (((uint)vv >> 8) & 0xFF) * frontv[1] + normal[1] * Defines.POWERSUIT_SCALE;

				lerp[i][2] = move[2] + (((uint)ovv >> 16) & 0xFF) * backv[2] + (((uint)vv >> 16) & 0xFF) * frontv[2] + normal[2] * Defines.POWERSUIT_SCALE;
			}
		}
		else
		{
			for (var i = 0;
			     i < nverts;
			     i++ /* , v++, ov++, lerp+=4 */
			    )
			{
				vv = v[i];
				ovv = ov[i];
				lerp[i][0] = move[0] + (ovv & 0xFF) * backv[0] + (vv & 0xFF) * frontv[0];
				lerp[i][1] = move[1] + (((uint)ovv >> 8) & 0xFF) * backv[1] + (((uint)vv >> 8) & 0xFF) * frontv[1];
				lerp[i][2] = move[2] + (((uint)ovv >> 16) & 0xFF) * backv[2] + (((uint)vv >> 16) & 0xFF) * frontv[2];
			}
		}
	}

	private void GL_LerpVerts(int nverts, int[] ov, int[] v, float[] move, float[] frontv, float[] backv)
	{
		int ovv, vv;
		var lerp = this.vertexArrayBuf;

		//PMM -- added RF_SHELL_DOUBLE, RF_SHELL_HALF_DAM
		if ((this.currententity.flags
			    & (Defines.RF_SHELL_RED | Defines.RF_SHELL_GREEN | Defines.RF_SHELL_BLUE | Defines.RF_SHELL_DOUBLE | Defines.RF_SHELL_HALF_DAM))
		    != 0)
		{
			float[] normal;
			var j = 0;

			for (var i = 0;
			     i < nverts;
			     i++ /* , v++, ov++, lerp+=4 */
			    )
			{
				vv = v[i];
				normal = this.r_avertexnormals[((uint)v[i] >> 24) & 0xFF];
				ovv = ov[i];

				lerp[j] = move[0] + (ovv & 0xFF) * backv[0] + (vv & 0xFF) * frontv[0] + normal[0] * Defines.POWERSUIT_SCALE;
				lerp[j + 1] = move[1] + (((uint)ovv >> 8) & 0xFF) * backv[1] + (((uint)vv >> 8) & 0xFF) * frontv[1] + normal[1] * Defines.POWERSUIT_SCALE;

				lerp[j + 2] = move[2] + (((uint)ovv >> 16) & 0xFF) * backv[2] + (((uint)vv >> 16) & 0xFF) * frontv[2] + normal[2] * Defines.POWERSUIT_SCALE;

				j += 3;
			}
		}
		else
		{
			var j = 0;

			for (var i = 0;
			     i < nverts;
			     i++ /* , v++, ov++, lerp+=4 */
			    )
			{
				ovv = ov[i];
				vv = v[i];

				lerp[j] = move[0] + (ovv & 0xFF) * backv[0] + (vv & 0xFF) * frontv[0];
				lerp[j + 1] = move[1] + (((uint)ovv >> 8) & 0xFF) * backv[1] + (((uint)vv >> 8) & 0xFF) * frontv[1];
				lerp[j + 2] = move[2] + (((uint)ovv >> 16) & 0xFF) * backv[2] + (((uint)vv >> 16) & 0xFF) * frontv[2];
				j += 3;
			}
		}
	}

	private readonly float[] colorArrayBuf = new float[qfiles.MAX_VERTS * 4];
	private readonly float[] vertexArrayBuf = new float[qfiles.MAX_VERTS * 3];
	private float[] tmpVec = { 0, 0, 0 };

	/*
	 * ============= GL_DrawAliasFrameLerp
	 * 
	 * interpolates between two frames and origins FIXME: batch lerp all
	 * vertexes =============
	 */
	private void GL_DrawAliasFrameLerp(dmdl_t paliashdr, float backlerp)
	{
		float l;
		daliasframe_t frame, oldframe;
		int[] v, ov;

		int[] order;
		var orderIndex = 0;
		int count;

		float frontlerp;
		float alpha;

		float[] move = { 0, 0, 0 }; // vec3_t

		float[][] vectors =
		{
			new float[] { 0, 0, 0 }, new float[] { 0, 0, 0 }, new float[] { 0, 0, 0 } // 3 mal
			// vec3_t
		};

		float[] frontv = { 0, 0, 0 }; // vec3_t
		float[] backv = { 0, 0, 0 }; // vec3_t

		int i;
		int index_xyz;

		//float[][] lerp;

		frame = paliashdr.aliasFrames[this.currententity.frame];

		v = frame.verts;

		oldframe = paliashdr.aliasFrames[this.currententity.oldframe];

		ov = oldframe.verts;

		order = paliashdr.glCmds;

		if ((this.currententity.flags & Defines.RF_TRANSLUCENT) != 0)
			alpha = this.currententity.alpha;
		else
			alpha = 1.0f;

		// PMM - added double shell
		if ((this.currententity.flags
			    & (Defines.RF_SHELL_RED | Defines.RF_SHELL_GREEN | Defines.RF_SHELL_BLUE | Defines.RF_SHELL_DOUBLE | Defines.RF_SHELL_HALF_DAM))
		    != 0)
			this.gl.glDisable(OpenGL.GL_TEXTURE_2D);

		frontlerp = 1.0f - backlerp;

		// move should be the delta back to the previous frame * backlerp
		Math3D.VectorSubtract(this.currententity.oldorigin, this.currententity.origin, frontv);
		Math3D.AngleVectors(this.currententity.angles, vectors[0], vectors[1], vectors[2]);

		move[0] = Math3D.DotProduct(frontv, vectors[0]); // forward
		move[1] = -Math3D.DotProduct(frontv, vectors[1]); // left
		move[2] = Math3D.DotProduct(frontv, vectors[2]); // up

		Math3D.VectorAdd(move, oldframe.translate, move);

		for (i = 0; i < 3; i++)
		{
			move[i] = backlerp * move[i] + frontlerp * frame.translate[i];
			frontv[i] = frontlerp * frame.scale[i];
			backv[i] = backlerp * oldframe.scale[i];
		}

		if (this.gl_vertex_arrays.value != 0.0f)
		{
			this.GL_LerpVerts(paliashdr.num_xyz, ov, v, move, frontv, backv);

			this.gl.glEnableClientState(OpenGL.GL_VERTEX_ARRAY);
			this.gl.glVertexPointer(3, 0, this.vertexArrayBuf);

			// PMM - added double damage shell
			if ((this.currententity.flags
				    & (Defines.RF_SHELL_RED | Defines.RF_SHELL_GREEN | Defines.RF_SHELL_BLUE | Defines.RF_SHELL_DOUBLE | Defines.RF_SHELL_HALF_DAM))
			    != 0)
			{
				this.gl.glDisableClientState(OpenGL.GL_COLOR_ARRAY);
				this.gl.glColor4f(this.shadelight[0], this.shadelight[1], this.shadelight[2], alpha);
			}
			else
			{
				this.gl.glEnableClientState(OpenGL.GL_COLOR_ARRAY);
				this.gl.glColorPointer(4, 0, this.colorArrayBuf);

				//
				// pre light everything
				//
				var color = this.colorArrayBuf;
				var j = 0;

				for (i = 0; i < paliashdr.num_xyz; i++)
				{
					// light normal index
					l = this.shadedots[((uint)v[i] >> 24) & 0xFF];
					color[j++] = l * this.shadelight[0];
					color[j++] = l * this.shadelight[1];
					color[j++] = l * this.shadelight[2];
					color[j++] = alpha;
				}
			}

			if (this.qglLockArraysEXT)
				this.gl.glLockArraysEXT(0, paliashdr.num_xyz);

			while (true)
			{
				// get the vertex count and primitive type
				count = order[orderIndex++];

				if (count == 0)
					break; // done

				if (count < 0)
				{
					count = -count;
					this.gl.glBegin(OpenGL.GL_TRIANGLE_FAN);
				}
				else
					this.gl.glBegin(OpenGL.GL_TRIANGLE_STRIP);

				// PMM - added double damage shell
				if ((this.currententity.flags
					    & (Defines.RF_SHELL_RED | Defines.RF_SHELL_GREEN | Defines.RF_SHELL_BLUE | Defines.RF_SHELL_DOUBLE | Defines.RF_SHELL_HALF_DAM))
				    != 0)
				{
					do
					{
						index_xyz = order[orderIndex + 2];
						orderIndex += 3;
						/*
						 * vertexArrayBuf.position(4 * index_xyz);
						 * vertexArrayBuf.get(tmpVec); gl.glVertex3fv( tmpVec );
						 */
						this.gl.glArrayElement(index_xyz);
					}
					while (--count != 0);
				}
				else
				{
					do
					{
						// texture coordinates come from the draw list
						this.gl.glTexCoord2f(
							BitConverter.ToSingle(BitConverter.GetBytes(order[orderIndex + 0]), 0),
							BitConverter.ToSingle(BitConverter.GetBytes(order[orderIndex + 1]), 0)
						);

						index_xyz = order[orderIndex + 2];
						orderIndex += 3;

						// normals and vertexes come from the frame list
						this.gl.glArrayElement(index_xyz);
					}
					while (--count != 0);
				}

				this.gl.glEnd();
			}

			if (this.qglLockArraysEXT)
				this.gl.glUnlockArraysEXT();
		}
		else
		{
			this.GL_LerpVerts(paliashdr.num_xyz, ov, v, this.s_lerped, move, frontv, backv);

			float[] tmp;

			while (true)
			{
				// get the vertex count and primitive type
				count = order[orderIndex++];

				if (count == 0)
					break; // done

				if (count < 0)
				{
					count = -count;
					this.gl.glBegin(OpenGL.GL_TRIANGLE_FAN);
				}
				else
					this.gl.glBegin(OpenGL.GL_TRIANGLE_STRIP);

				if ((this.currententity.flags & (Defines.RF_SHELL_RED | Defines.RF_SHELL_GREEN | Defines.RF_SHELL_BLUE)) != 0)
				{
					do
					{
						index_xyz = order[orderIndex + 2];
						orderIndex += 3;

						this.gl.glColor4f(this.shadelight[0], this.shadelight[1], this.shadelight[2], alpha);
						tmp = this.s_lerped[index_xyz];
						this.gl.glVertex3f(tmp[0], tmp[1], tmp[2]);
					}
					while (--count != 0);
				}
				else
				{
					do
					{
						// texture coordinates come from the draw list
						// gl.glTexCoord2f (((float *)order)[0], ((float
						// *)order)[1]);

						this.gl.glTexCoord2f(
							BitConverter.ToSingle(BitConverter.GetBytes(order[orderIndex + 0]), 0),
							BitConverter.ToSingle(BitConverter.GetBytes(order[orderIndex + 1]), 0)
						);

						index_xyz = order[orderIndex + 2];
						orderIndex += 3;

						// normals and vertexes come from the frame list
						l = this.shadedots[((uint)v[index_xyz] >> 24) & 0xFF];

						this.gl.glColor4f(l * this.shadelight[0], l * this.shadelight[1], l * this.shadelight[2], alpha);
						tmp = this.s_lerped[index_xyz];
						this.gl.glVertex3f(tmp[0], tmp[1], tmp[2]);
					}
					while (--count != 0);
				}

				this.gl.glEnd();
			}
		}

		// PMM - added double damage shell
		if ((this.currententity.flags
			    & (Defines.RF_SHELL_RED | Defines.RF_SHELL_GREEN | Defines.RF_SHELL_BLUE | Defines.RF_SHELL_DOUBLE | Defines.RF_SHELL_HALF_DAM))
		    != 0)
			this.gl.glEnable(OpenGL.GL_TEXTURE_2D);
	}

	/*
	 * ============= GL_DrawAliasShadow =============
	 */
	private void GL_DrawAliasShadow(dmdl_t paliashdr, int posenum)
	{
		float[] point = { 0, 0, 0 };
		int count;
		var lheight = this.currententity.origin[2] - this.lightspot[2];

		var order = paliashdr.glCmds;

		var height = -lheight + 1.0f;

		var orderIndex = 0;
		var index = 0;

		while (true)
		{
			// get the vertex count and primitive type
			count = order[orderIndex++];

			if (count == 0)
				break; // done

			if (count < 0)
			{
				count = -count;
				this.gl.glBegin(OpenGL.GL_TRIANGLE_FAN);
			}
			else
				this.gl.glBegin(OpenGL.GL_TRIANGLE_STRIP);

			do
			{
				// normals and vertexes come from the frame list
				/*
				 * point[0] = verts[order[2]].v[0] * frame.scale[0] +
				 * frame.translate[0]; point[1] = verts[order[2]].v[1] *
				 * frame.scale[1] + frame.translate[1]; point[2] =
				 * verts[order[2]].v[2] * frame.scale[2] + frame.translate[2];
				 */

				if (this.gl_vertex_arrays.value != 0.0f)
				{
					index = order[orderIndex + 2] * 3;
					point[0] = this.vertexArrayBuf[index];
					point[1] = this.vertexArrayBuf[index + 1];
					point[2] = this.vertexArrayBuf[index + 2];
				}
				else
					Math3D.VectorCopy(this.s_lerped[order[orderIndex + 2]], point);

				point[0] -= this.shadevector[0] * (point[2] + lheight);
				point[1] -= this.shadevector[1] * (point[2] + lheight);
				point[2] = height;
				this.gl.glVertex3f(point[0], point[1], point[2]);

				orderIndex += 3;
			}
			while (--count != 0);

			this.gl.glEnd();
		}
	}

	/*
	 * * R_CullAliasModel
	 */
	private bool R_CullAliasModel(float[][] bbox, entity_t e)
	{
		int i;
		float[] mins = { 0, 0, 0 };
		float[] maxs = { 0, 0, 0 };

		dmdl_t paliashdr;

		float[][] vectors = { new float[] { 0, 0, 0 }, new float[] { 0, 0, 0 }, new float[] { 0, 0, 0 } };

		float[] thismins = { 0, 0, 0 };
		float[] oldmins = { 0, 0, 0 };
		float[] thismaxs = { 0, 0, 0 };
		float[] oldmaxs = { 0, 0, 0 };
		daliasframe_t pframe, poldframe;
		float[] angles = { 0, 0, 0 };

		paliashdr = (dmdl_t)this.currentmodel.extradata;

		if (e.frame >= paliashdr.num_frames || e.frame < 0)
		{
			VID.Printf(Defines.PRINT_ALL, "R_CullAliasModel " + this.currentmodel.name + ": no such frame " + e.frame + '\n');
			e.frame = 0;
		}

		if (e.oldframe >= paliashdr.num_frames || e.oldframe < 0)
		{
			VID.Printf(Defines.PRINT_ALL, "R_CullAliasModel " + this.currentmodel.name + ": no such oldframe " + e.oldframe + '\n');
			e.oldframe = 0;
		}

		pframe = paliashdr.aliasFrames[e.frame];
		poldframe = paliashdr.aliasFrames[e.oldframe];

		/*
		 * * compute axially aligned mins and maxs
		 */
		if (pframe == poldframe)
		{
			for (i = 0; i < 3; i++)
			{
				mins[i] = pframe.translate[i];
				maxs[i] = mins[i] + pframe.scale[i] * 255;
			}
		}
		else
		{
			for (i = 0; i < 3; i++)
			{
				thismins[i] = pframe.translate[i];
				thismaxs[i] = thismins[i] + pframe.scale[i] * 255;

				oldmins[i] = poldframe.translate[i];
				oldmaxs[i] = oldmins[i] + poldframe.scale[i] * 255;

				if (thismins[i] < oldmins[i])
					mins[i] = thismins[i];
				else
					mins[i] = oldmins[i];

				if (thismaxs[i] > oldmaxs[i])
					maxs[i] = thismaxs[i];
				else
					maxs[i] = oldmaxs[i];
			}
		}

		/*
		 * * compute a full bounding box
		 */
		for (i = 0; i < 8; i++)
		{
			float[] tmp = { 0, 0, 0 };

			if ((i & 1) != 0)
				tmp[0] = mins[0];
			else
				tmp[0] = maxs[0];

			if ((i & 2) != 0)
				tmp[1] = mins[1];
			else
				tmp[1] = maxs[1];

			if ((i & 4) != 0)
				tmp[2] = mins[2];
			else
				tmp[2] = maxs[2];

			Math3D.VectorCopy(tmp, bbox[i]);
		}

		/*
		 * * rotate the bounding box
		 */
		Math3D.VectorCopy(e.angles, angles);
		angles[Base.YAW] = -angles[Base.YAW];
		Math3D.AngleVectors(angles, vectors[0], vectors[1], vectors[2]);

		for (i = 0; i < 8; i++)
		{
			float[] tmp = { 0, 0, 0 };

			Math3D.VectorCopy(bbox[i], tmp);

			bbox[i][0] = Math3D.DotProduct(vectors[0], tmp);
			bbox[i][1] = -Math3D.DotProduct(vectors[1], tmp);
			bbox[i][2] = Math3D.DotProduct(vectors[2], tmp);

			Math3D.VectorAdd(e.origin, bbox[i], bbox[i]);
		}

		{
			int p, f;
			var aggregatemask = ~0; // 0xFFFFFFFF

			for (p = 0; p < 8; p++)
			{
				var mask = 0;

				for (f = 0; f < 4; f++)
				{
					var dp = Math3D.DotProduct(this.frustum[f].normal, bbox[p]);

					if (dp - this.frustum[f].dist < 0)
						mask |= 1 << f;
				}

				aggregatemask &= mask;
			}

			if (aggregatemask != 0)
				return true;

			return false;
		}
	}

	/*
	 * ================= R_DrawAliasModel
	 * 
	 * =================
	 */
	protected void R_DrawAliasModel(entity_t e)
	{
		int i;
		dmdl_t paliashdr;
		float an;

		// bounding box
		float[][] bbox =
		{
			new float[] { 0, 0, 0 },
			new float[] { 0, 0, 0 },
			new float[] { 0, 0, 0 },
			new float[] { 0, 0, 0 },
			new float[] { 0, 0, 0 },
			new float[] { 0, 0, 0 },
			new float[] { 0, 0, 0 },
			new float[] { 0, 0, 0 }
		};

		image_t skin;

		if ((e.flags & Defines.RF_WEAPONMODEL) == 0)
		{
			if (this.R_CullAliasModel(bbox, e))
				return;
		}

		if ((e.flags & Defines.RF_WEAPONMODEL) != 0)
		{
			if (this.r_lefthand.value == 2.0f)
				return;
		}

		paliashdr = (dmdl_t)this.currentmodel.extradata;

		//
		// get lighting information
		//
		// PMM - rewrote, reordered to handle new shells & mixing
		// PMM - 3.20 code .. replaced with original way of doing it to keep mod
		// authors happy
		//
		if ((this.currententity.flags
			    & (Defines.RF_SHELL_HALF_DAM | Defines.RF_SHELL_GREEN | Defines.RF_SHELL_RED | Defines.RF_SHELL_BLUE | Defines.RF_SHELL_DOUBLE))
		    != 0)
		{
			Math3D.VectorClear(this.shadelight);

			if ((this.currententity.flags & Defines.RF_SHELL_HALF_DAM) != 0)
			{
				this.shadelight[0] = 0.56f;
				this.shadelight[1] = 0.59f;
				this.shadelight[2] = 0.45f;
			}

			if ((this.currententity.flags & Defines.RF_SHELL_DOUBLE) != 0)
			{
				this.shadelight[0] = 0.9f;
				this.shadelight[1] = 0.7f;
			}

			if ((this.currententity.flags & Defines.RF_SHELL_RED) != 0)
				this.shadelight[0] = 1.0f;

			if ((this.currententity.flags & Defines.RF_SHELL_GREEN) != 0)
				this.shadelight[1] = 1.0f;

			if ((this.currententity.flags & Defines.RF_SHELL_BLUE) != 0)
				this.shadelight[2] = 1.0f;
		}

		else if ((this.currententity.flags & Defines.RF_FULLBRIGHT) != 0)
		{
			for (i = 0; i < 3; i++)
				this.shadelight[i] = 1.0f;
		}
		else
		{
			this.R_LightPoint(this.currententity.origin, this.shadelight);

			// player lighting hack for communication back to server
			// big hack!
			if ((this.currententity.flags & Defines.RF_WEAPONMODEL) != 0)
			{
				// pick the greatest component, which should be the same
				// as the mono value returned by software
				if (this.shadelight[0] > this.shadelight[1])
				{
					if (this.shadelight[0] > this.shadelight[2])
						this.r_lightlevel.value = 150 * this.shadelight[0];
					else
						this.r_lightlevel.value = 150 * this.shadelight[2];
				}
				else
				{
					if (this.shadelight[1] > this.shadelight[2])
						this.r_lightlevel.value = 150 * this.shadelight[1];
					else
						this.r_lightlevel.value = 150 * this.shadelight[2];
				}
			}

			if (this.gl_monolightmap.@string[0] != '0')
			{
				var s = this.shadelight[0];

				if (s < this.shadelight[1])
					s = this.shadelight[1];

				if (s < this.shadelight[2])
					s = this.shadelight[2];

				this.shadelight[0] = s;
				this.shadelight[1] = s;
				this.shadelight[2] = s;
			}
		}

		if ((this.currententity.flags & Defines.RF_MINLIGHT) != 0)
		{
			for (i = 0; i < 3; i++)
			{
				if (this.shadelight[i] > 0.1f)
					break;
			}

			if (i == 3)
			{
				this.shadelight[0] = 0.1f;
				this.shadelight[1] = 0.1f;
				this.shadelight[2] = 0.1f;
			}
		}

		if ((this.currententity.flags & Defines.RF_GLOW) != 0)
		{
			// bonus items will
			// pulse with time
			float scale;
			float min;

			scale = (float)(0.1f * Math.Sin(this.r_newrefdef.time * 7));

			for (i = 0; i < 3; i++)
			{
				min = this.shadelight[i] * 0.8f;
				this.shadelight[i] += scale;

				if (this.shadelight[i] < min)
					this.shadelight[i] = min;
			}
		}

		// =================
		// PGM ir goggles color override
		if ((this.r_newrefdef.rdflags & Defines.RDF_IRGOGGLES) != 0 && (this.currententity.flags & Defines.RF_IR_VISIBLE) != 0)
		{
			this.shadelight[0] = 1.0f;
			this.shadelight[1] = 0.0f;
			this.shadelight[2] = 0.0f;
		}

		// PGM
		// =================

		this.shadedots = this.r_avertexnormal_dots[(int)(this.currententity.angles[1] * (OpenGLRenderApi.SHADEDOT_QUANT / 360.0))
			& (OpenGLRenderApi.SHADEDOT_QUANT - 1)];

		an = (float)(this.currententity.angles[1] / 180 * Math.PI);
		this.shadevector[0] = (float)Math.Cos(-an);
		this.shadevector[1] = (float)Math.Sin(-an);
		this.shadevector[2] = 1;
		Math3D.VectorNormalize(this.shadevector);

		//
		// locate the proper data
		//

		this.c_alias_polys += paliashdr.num_tris;

		//
		// draw all the triangles
		//
		if ((this.currententity.flags & Defines.RF_DEPTHHACK) != 0)

			// hack the depth range to prevent view model from poking into walls
			this.gl.glDepthRange(this.gldepthmin, this.gldepthmin + 0.3 * (this.gldepthmax - this.gldepthmin));

		if ((this.currententity.flags & Defines.RF_WEAPONMODEL) != 0 && this.r_lefthand.value == 1.0f)
		{
			this.gl.glMatrixMode(OpenGL.GL_PROJECTION);
			this.gl.glPushMatrix();
			this.gl.glLoadIdentity();
			this.gl.glScalef(-1, 1, 1);
			this.MYgluPerspective(this.r_newrefdef.fov_y, (float)this.r_newrefdef.width / this.r_newrefdef.height, 4, 4096);
			this.gl.glMatrixMode(OpenGL.GL_MODELVIEW);

			this.gl.glCullFace(OpenGL.GL_BACK);
		}

		this.gl.glPushMatrix();
		e.angles[Base.PITCH] = -e.angles[Base.PITCH]; // sigh.
		this.R_RotateForEntity(e);
		e.angles[Base.PITCH] = -e.angles[Base.PITCH]; // sigh.

		// select skin
		if (this.currententity.skin != null)
			skin = this.currententity.skin; // custom player skin
		else
		{
			if (this.currententity.skinnum >= qfiles.MAX_MD2SKINS)
				skin = this.currentmodel.skins[0];
			else
			{
				skin = this.currentmodel.skins[this.currententity.skinnum];

				if (skin == null)
					skin = this.currentmodel.skins[0];
			}
		}

		if (skin == null)
			skin = this.r_notexture; // fallback...

		this.GL_Bind(skin.texnum);

		// draw it

		this.gl.glShadeModel(OpenGL.GL_SMOOTH);

		this.GL_TexEnv(OpenGL.GL_MODULATE);

		if ((this.currententity.flags & Defines.RF_TRANSLUCENT) != 0)
			this.gl.glEnable(OpenGL.GL_BLEND);

		if (this.currententity.frame >= paliashdr.num_frames || this.currententity.frame < 0)
		{
			VID.Printf(Defines.PRINT_ALL, "R_DrawAliasModel " + this.currentmodel.name + ": no such frame " + this.currententity.frame + '\n');
			this.currententity.frame = 0;
			this.currententity.oldframe = 0;
		}

		if (this.currententity.oldframe >= paliashdr.num_frames || this.currententity.oldframe < 0)
		{
			VID.Printf(Defines.PRINT_ALL, "R_DrawAliasModel " + this.currentmodel.name + ": no such oldframe " + this.currententity.oldframe + '\n');
			this.currententity.frame = 0;
			this.currententity.oldframe = 0;
		}

		if (this.r_lerpmodels.value == 0.0f)
			this.currententity.backlerp = 0;

		this.GL_DrawAliasFrameLerp(paliashdr, this.currententity.backlerp);

		this.GL_TexEnv(OpenGL.GL_REPLACE);
		this.gl.glShadeModel(OpenGL.GL_FLAT);

		this.gl.glPopMatrix();

		if ((this.currententity.flags & Defines.RF_WEAPONMODEL) != 0 && this.r_lefthand.value == 1.0F)
		{
			this.gl.glMatrixMode(OpenGL.GL_PROJECTION);
			this.gl.glPopMatrix();
			this.gl.glMatrixMode(OpenGL.GL_MODELVIEW);
			this.gl.glCullFace(OpenGL.GL_FRONT);
		}

		if ((this.currententity.flags & Defines.RF_TRANSLUCENT) != 0)
			this.gl.glDisable(OpenGL.GL_BLEND);

		if ((this.currententity.flags & Defines.RF_DEPTHHACK) != 0)
			this.gl.glDepthRange(this.gldepthmin, this.gldepthmax);

		if (this.gl_shadows.value != 0.0f && (this.currententity.flags & (Defines.RF_TRANSLUCENT | Defines.RF_WEAPONMODEL)) == 0)
		{
			this.gl.glPushMatrix();
			this.R_RotateForEntity(e);
			this.gl.glDisable(OpenGL.GL_TEXTURE_2D);
			this.gl.glEnable(OpenGL.GL_BLEND);
			this.gl.glColor4f(0, 0, 0, 0.5f);
			this.GL_DrawAliasShadow(paliashdr, this.currententity.frame);
			this.gl.glEnable(OpenGL.GL_TEXTURE_2D);
			this.gl.glDisable(OpenGL.GL_BLEND);
			this.gl.glPopMatrix();
		}

		this.gl.glColor4f(1, 1, 1, 1);
	}

	protected void MeshConstructor()
	{
		for (var i = 0; i < this.s_lerped.Length; i++)
			this.s_lerped[i] = new float[4];

		this.shadedots = this.r_avertexnormal_dots[0];
	}
}