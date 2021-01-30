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
namespace Quake2Sharp.render.opengl
{
	using qcommon;
	using render.types;
	using System;
	using util;

	public partial class OpenGLRenderApi
	{
		// warpsin.h
		public static readonly float[] SIN =
		{
			0f,
			0.19633f,
			0.392541f,
			0.588517f,
			0.784137f,
			0.979285f,
			1.17384f,
			1.3677f,
			1.56072f,
			1.75281f,
			1.94384f,
			2.1337f,
			2.32228f,
			2.50945f,
			2.69512f,
			2.87916f,
			3.06147f,
			3.24193f,
			3.42044f,
			3.59689f,
			3.77117f,
			3.94319f,
			4.11282f,
			4.27998f,
			4.44456f,
			4.60647f,
			4.76559f,
			4.92185f,
			5.07515f,
			5.22538f,
			5.37247f,
			5.51632f,
			5.65685f,
			5.79398f,
			5.92761f,
			6.05767f,
			6.18408f,
			6.30677f,
			6.42566f,
			6.54068f,
			6.65176f,
			6.75883f,
			6.86183f,
			6.9607f,
			7.05537f,
			7.14579f,
			7.23191f,
			7.31368f,
			7.39104f,
			7.46394f,
			7.53235f,
			7.59623f,
			7.65552f,
			7.71021f,
			7.76025f,
			7.80562f,
			7.84628f,
			7.88222f,
			7.91341f,
			7.93984f,
			7.96148f,
			7.97832f,
			7.99036f,
			7.99759f,
			8f,
			7.99759f,
			7.99036f,
			7.97832f,
			7.96148f,
			7.93984f,
			7.91341f,
			7.88222f,
			7.84628f,
			7.80562f,
			7.76025f,
			7.71021f,
			7.65552f,
			7.59623f,
			7.53235f,
			7.46394f,
			7.39104f,
			7.31368f,
			7.23191f,
			7.14579f,
			7.05537f,
			6.9607f,
			6.86183f,
			6.75883f,
			6.65176f,
			6.54068f,
			6.42566f,
			6.30677f,
			6.18408f,
			6.05767f,
			5.92761f,
			5.79398f,
			5.65685f,
			5.51632f,
			5.37247f,
			5.22538f,
			5.07515f,
			4.92185f,
			4.76559f,
			4.60647f,
			4.44456f,
			4.27998f,
			4.11282f,
			3.94319f,
			3.77117f,
			3.59689f,
			3.42044f,
			3.24193f,
			3.06147f,
			2.87916f,
			2.69512f,
			2.50945f,
			2.32228f,
			2.1337f,
			1.94384f,
			1.75281f,
			1.56072f,
			1.3677f,
			1.17384f,
			0.979285f,
			0.784137f,
			0.588517f,
			0.392541f,
			0.19633f,
			9.79717e-16f,
			-0.19633f,
			-0.392541f,
			-0.588517f,
			-0.784137f,
			-0.979285f,
			-1.17384f,
			-1.3677f,
			-1.56072f,
			-1.75281f,
			-1.94384f,
			-2.1337f,
			-2.32228f,
			-2.50945f,
			-2.69512f,
			-2.87916f,
			-3.06147f,
			-3.24193f,
			-3.42044f,
			-3.59689f,
			-3.77117f,
			-3.94319f,
			-4.11282f,
			-4.27998f,
			-4.44456f,
			-4.60647f,
			-4.76559f,
			-4.92185f,
			-5.07515f,
			-5.22538f,
			-5.37247f,
			-5.51632f,
			-5.65685f,
			-5.79398f,
			-5.92761f,
			-6.05767f,
			-6.18408f,
			-6.30677f,
			-6.42566f,
			-6.54068f,
			-6.65176f,
			-6.75883f,
			-6.86183f,
			-6.9607f,
			-7.05537f,
			-7.14579f,
			-7.23191f,
			-7.31368f,
			-7.39104f,
			-7.46394f,
			-7.53235f,
			-7.59623f,
			-7.65552f,
			-7.71021f,
			-7.76025f,
			-7.80562f,
			-7.84628f,
			-7.88222f,
			-7.91341f,
			-7.93984f,
			-7.96148f,
			-7.97832f,
			-7.99036f,
			-7.99759f,
			-8f,
			-7.99759f,
			-7.99036f,
			-7.97832f,
			-7.96148f,
			-7.93984f,
			-7.91341f,
			-7.88222f,
			-7.84628f,
			-7.80562f,
			-7.76025f,
			-7.71021f,
			-7.65552f,
			-7.59623f,
			-7.53235f,
			-7.46394f,
			-7.39104f,
			-7.31368f,
			-7.23191f,
			-7.14579f,
			-7.05537f,
			-6.9607f,
			-6.86183f,
			-6.75883f,
			-6.65176f,
			-6.54068f,
			-6.42566f,
			-6.30677f,
			-6.18408f,
			-6.05767f,
			-5.92761f,
			-5.79398f,
			-5.65685f,
			-5.51632f,
			-5.37247f,
			-5.22538f,
			-5.07515f,
			-4.92185f,
			-4.76559f,
			-4.60647f,
			-4.44456f,
			-4.27998f,
			-4.11282f,
			-3.94319f,
			-3.77117f,
			-3.59689f,
			-3.42044f,
			-3.24193f,
			-3.06147f,
			-2.87916f,
			-2.69512f,
			-2.50945f,
			-2.32228f,
			-2.1337f,
			-1.94384f,
			-1.75281f,
			-1.56072f,
			-1.3677f,
			-1.17384f,
			-0.979285f,
			-0.784137f,
			-0.588517f,
			-0.392541f,
			-0.19633f
		};

		// gl_warp.c -- sky and water polygons
		//extern model_t *loadmodel;
		private string skyname;
		private float skyrotate;
		private readonly float[] skyaxis = { 0, 0, 0 };
		private readonly image_t[] sky_images = new image_t[6];
		private msurface_t warpface;
		private static readonly int SUBDIVIDE_SIZE = 64;

		private void BoundPoly(int numverts, float[][] verts, float[] mins, float[] maxs)
		{
			int i, j;
			float[] v;

			mins[0] = mins[1] = mins[2] = 9999;
			maxs[0] = maxs[1] = maxs[2] = -9999;

			for (i = 0; i < numverts; i++)
			{
				v = verts[i];

				for (j = 0; j < 3; j++)
				{
					if (v[j] < mins[j])
						mins[j] = v[j];

					if (v[j] > maxs[j])
						maxs[j] = v[j];
				}
			}
		}

		private void SubdividePolygon(int numverts, float[][] verts)
		{
			int i, j, k;
			float[] mins = { 0, 0, 0 };
			float[] maxs = { 0, 0, 0 };
			float m;
			float[] v = { 0, 0, 0 };
			var front = new float[64][];
			var back = new float[64][];
			int f, b;
			var dist = new float[64];
			float frac;
			float s, t;
			float[] total = { 0, 0, 0 };
			float total_s, total_t;

			for (var i1 = 0; i1 < front.Length; i1++)
				front[i1] = new float[3];

			for (var i1 = 0; i1 < back.Length; i1++)
				back[i1] = new float[3];

			if (numverts > 60)
				Com.Error(Defines.ERR_DROP, "numverts = " + numverts);

			this.BoundPoly(numverts, verts, mins, maxs);

			// x,y und z
			for (i = 0; i < 3; i++)
			{
				m = (mins[i] + maxs[i]) * 0.5f;
				m = OpenGLRenderApi.SUBDIVIDE_SIZE * (float)Math.Floor(m / OpenGLRenderApi.SUBDIVIDE_SIZE + 0.5f);

				if (maxs[i] - m < 8)
					continue;

				if (m - mins[i] < 8)
					continue;

				// cut it
				for (j = 0; j < numverts; j++)
					dist[j] = verts[j][i] - m;

				// wrap cases
				dist[j] = dist[0];

				Math3D.VectorCopy(verts[0], verts[numverts]);

				f = b = 0;

				for (j = 0; j < numverts; j++)
				{
					v = verts[j];

					if (dist[j] >= 0)
					{
						Math3D.VectorCopy(v, front[f]);
						f++;
					}

					if (dist[j] <= 0)
					{
						Math3D.VectorCopy(v, back[b]);
						b++;
					}

					if (dist[j] == 0 || dist[j + 1] == 0)
						continue;

					if (dist[j] > 0 != dist[j + 1] > 0)
					{
						// clip point
						frac = dist[j] / (dist[j] - dist[j + 1]);

						for (k = 0; k < 3; k++)
							front[f][k] = back[b][k] = v[k] + frac * (verts[j + 1][k] - v[k]);

						f++;
						b++;
					}
				}

				this.SubdividePolygon(f, front);
				this.SubdividePolygon(b, back);

				return;
			}

			// add a point in the center to help keep warp valid

			// wird im Konstruktor erschlagen
			// poly = Hunk_Alloc (sizeof(glpoly_t) + ((numverts-4)+2) *
			// VERTEXSIZE*sizeof(float));

			// init polys
			var poly = Polygon.create(numverts + 2);

			poly.next = this.warpface.polys;
			this.warpface.polys = poly;
			Math3D.VectorClear(total);
			total_s = 0;
			total_t = 0;

			for (i = 0; i < numverts; i++)
			{
				poly.x(i + 1, verts[i][0]);
				poly.y(i + 1, verts[i][1]);
				poly.z(i + 1, verts[i][2]);
				s = Math3D.DotProduct(verts[i], this.warpface.texinfo.vecs[0]);
				t = Math3D.DotProduct(verts[i], this.warpface.texinfo.vecs[1]);

				total_s += s;
				total_t += t;
				Math3D.VectorAdd(total, verts[i], total);

				poly.s1(i + 1, s);
				poly.t1(i + 1, t);
			}

			var scale = 1.0f / numverts;
			poly.x(0, total[0] * scale);
			poly.y(0, total[1] * scale);
			poly.z(0, total[2] * scale);
			poly.s1(0, total_s * scale);
			poly.t1(0, total_t * scale);

			poly.x(i + 1, poly.x(1));
			poly.y(i + 1, poly.y(1));
			poly.z(i + 1, poly.z(1));
			poly.s1(i + 1, poly.s1(1));
			poly.t1(i + 1, poly.t1(1));
			poly.s2(i + 1, poly.s2(1));
			poly.t2(i + 1, poly.t2(1));
		}

		/*
		 * GL_SubdivideSurface
		 * 
		 * Breaks a polygon up along axial 64 unit boundaries so that turbulent and
		 * sky warps can be done reasonably.
		 */
		private readonly float[][] tmpVerts = new float[64][];

		protected void GL_SubdivideSurface(msurface_t fa)
		{
			var verts = this.tmpVerts;
			float[] vec;
			this.warpface = fa;

			//
			// convert edges back to a normal polygon
			//
			var numverts = 0;

			for (var i = 0; i < fa.numedges; i++)
			{
				var lindex = this.loadmodel.surfedges[fa.firstedge + i];

				if (lindex > 0)
					vec = this.loadmodel.vertexes[this.loadmodel.edges[lindex].v[0]].position;
				else
					vec = this.loadmodel.vertexes[this.loadmodel.edges[-lindex].v[1]].position;

				Math3D.VectorCopy(vec, verts[numverts]);
				numverts++;
			}

			this.SubdividePolygon(numverts, verts);
		}

		//	  =========================================================

		//// speed up sin calculations - Ed
		//	float r_turbsin[] =
		//	{
		//		#include "warpsin.h"
		//	};
		private static readonly float TURBSCALE = (float)(256.0f / (2 * Math.PI));

		/*
		 * ============= EmitWaterPolys
		 * 
		 * Does a water warp on the pre-fragmented glpoly_t chain =============
		 */
		protected void EmitWaterPolys(msurface_t fa)
		{
			glpoly_t p, bp;
			int i;
			float s = 0;
			float t = 0;
			float os, ot;
			float scroll;
			var rdt = this.r_newrefdef.time;

			if ((fa.texinfo.flags & Defines.SURF_FLOWING) != 0)
				scroll = -64 * (this.r_newrefdef.time * 0.5f - (int)(this.r_newrefdef.time * 0.5f));
			else
				scroll = 0;

			for (bp = fa.polys; bp != null; bp = bp.next)
			{
				p = bp;

				this.gl.glBegin(OpenGL.GL_TRIANGLE_FAN);

				for (i = 0; i < p.numverts; i++)
				{
					os = p.s1(i);
					ot = p.t1(i);

					s = os + OpenGLRenderApi.SIN[(int)((ot * 0.125f + this.r_newrefdef.time) * OpenGLRenderApi.TURBSCALE) & 255];
					s += scroll;
					s *= 1.0f / 64;

					t = ot + OpenGLRenderApi.SIN[(int)((os * 0.125f + rdt) * OpenGLRenderApi.TURBSCALE) & 255];
					t *= 1.0f / 64;

					this.gl.glTexCoord2f(s, t);
					this.gl.glVertex3f(p.x(i), p.y(i), p.z(i));
				}

				this.gl.glEnd();
			}
		}

		//	  ===================================================================

		private readonly float[][] skyclip =
		{
			new float[] { 1, 1, 0 },
			new float[] { 1, -1, 0 },
			new float[] { 0, -1, 1 },
			new float[] { 0, 1, 1 },
			new float[] { 1, 0, 1 },
			new float[] { -1, 0, 1 }
		};

		private int c_sky;

		// 1 = s, 2 = t, 3 = 2048
		private readonly int[][] st_to_vec =
		{
			new[] { 3, -1, 2 },
			new[] { -3, 1, 2 },
			new[] { 1, 3, 2 },
			new[] { -1, -3, 2 },
			new[] { -2, -1, 3 }, // 0 degrees yaw, look straight up
			new[] { 2, -1, -3 } // look straight down
		};

		private readonly int[][] vec_to_st =
		{
			new[] { -2, 3, 1 }, new[] { 2, 3, -1 }, new[] { 1, 3, 2 }, new[] { -1, 3, -2 }, new[] { -2, -1, 3 }, new[] { -2, 1, -3 }
		};

		private readonly float[][] skymins = new float[2][];
		private readonly float[][] skymaxs = new float[2][];
		private float sky_min, sky_max;

		private void DrawSkyPolygon(int nump, float[][] vecs)
		{
			int i, j;
			float[] v = { 0, 0, 0 };
			float[] av = { 0, 0, 0 };
			float s, t, dv;
			int axis;

			this.c_sky++;

			// decide which face it maps to
			Math3D.VectorCopy(Globals.vec3_origin, v);

			for (i = 0; i < nump; i++)
				Math3D.VectorAdd(vecs[i], v, v);

			av[0] = Math.Abs(v[0]);
			av[1] = Math.Abs(v[1]);
			av[2] = Math.Abs(v[2]);

			if (av[0] > av[1] && av[0] > av[2])
			{
				if (v[0] < 0)
					axis = 1;
				else
					axis = 0;
			}
			else if (av[1] > av[2] && av[1] > av[0])
			{
				if (v[1] < 0)
					axis = 3;
				else
					axis = 2;
			}
			else
			{
				if (v[2] < 0)
					axis = 5;
				else
					axis = 4;
			}

			// project new texture coords
			for (i = 0; i < nump; i++)
			{
				j = this.vec_to_st[axis][2];

				if (j > 0)
					dv = vecs[i][j - 1];
				else
					dv = -vecs[i][-j - 1];

				if (dv < 0.001f)
					continue; // don't divide by zero

				j = this.vec_to_st[axis][0];

				if (j < 0)
					s = -vecs[i][-j - 1] / dv;
				else
					s = vecs[i][j - 1] / dv;

				j = this.vec_to_st[axis][1];

				if (j < 0)
					t = -vecs[i][-j - 1] / dv;
				else
					t = vecs[i][j - 1] / dv;

				if (s < this.skymins[0][axis])
					this.skymins[0][axis] = s;

				if (t < this.skymins[1][axis])
					this.skymins[1][axis] = t;

				if (s > this.skymaxs[0][axis])
					this.skymaxs[0][axis] = s;

				if (t > this.skymaxs[1][axis])
					this.skymaxs[1][axis] = t;
			}
		}

		private static readonly float ON_EPSILON = 0.1f; // point on plane side epsilon
		private static readonly int MAX_CLIP_VERTS = 64;
		private const int SIDE_BACK = 1;
		private const int SIDE_FRONT = 0;
		private const int SIDE_ON = 2;
		private readonly float[] dists = new float[OpenGLRenderApi.MAX_CLIP_VERTS];
		private readonly int[] sides = new int[OpenGLRenderApi.MAX_CLIP_VERTS];
		private readonly float[][][][] newv = new float[6][][][];

		private void ClipSkyPolygon(int nump, float[][] vecs, int stage)
		{
			float[] norm;
			float[] v;
			bool front, back;
			float d, e;
			int[] newc = { 0, 0 };
			int i, j;

			if (nump > OpenGLRenderApi.MAX_CLIP_VERTS - 2)
				Com.Error(Defines.ERR_DROP, "ClipSkyPolygon: MAX_CLIP_VERTS");

			if (stage == 6)
			{
				// fully clipped, so draw it
				this.DrawSkyPolygon(nump, vecs);

				return;
			}

			front = back = false;
			norm = this.skyclip[stage];

			for (i = 0; i < nump; i++)
			{
				d = Math3D.DotProduct(vecs[i], norm);

				if (d > OpenGLRenderApi.ON_EPSILON)
				{
					front = true;
					this.sides[i] = OpenGLRenderApi.SIDE_FRONT;
				}
				else if (d < -OpenGLRenderApi.ON_EPSILON)
				{
					back = true;
					this.sides[i] = OpenGLRenderApi.SIDE_BACK;
				}
				else
					this.sides[i] = OpenGLRenderApi.SIDE_ON;

				this.dists[i] = d;
			}

			if (!front || !back)
			{
				// not clipped
				this.ClipSkyPolygon(nump, vecs, stage + 1);

				return;
			}

			// clip it
			this.sides[i] = this.sides[0];
			this.dists[i] = this.dists[0];
			Math3D.VectorCopy(vecs[0], vecs[i]);
			newc[0] = newc[1] = 0;

			for (i = 0; i < nump; i++)
			{
				v = vecs[i];

				switch (this.sides[i])
				{
					case OpenGLRenderApi.SIDE_FRONT:
						Math3D.VectorCopy(v, this.newv[stage][0][newc[0]]);
						newc[0]++;

						break;

					case OpenGLRenderApi.SIDE_BACK:
						Math3D.VectorCopy(v, this.newv[stage][1][newc[1]]);
						newc[1]++;

						break;

					case OpenGLRenderApi.SIDE_ON:
						Math3D.VectorCopy(v, this.newv[stage][0][newc[0]]);
						newc[0]++;
						Math3D.VectorCopy(v, this.newv[stage][1][newc[1]]);
						newc[1]++;

						break;
				}

				if (this.sides[i] == OpenGLRenderApi.SIDE_ON || this.sides[i + 1] == OpenGLRenderApi.SIDE_ON || this.sides[i + 1] == this.sides[i])
					continue;

				d = this.dists[i] / (this.dists[i] - this.dists[i + 1]);

				for (j = 0; j < 3; j++)
				{
					e = v[j] + d * (vecs[i + 1][j] - v[j]);
					this.newv[stage][0][newc[0]][j] = e;
					this.newv[stage][1][newc[1]][j] = e;
				}

				newc[0]++;
				newc[1]++;
			}

			// continue
			this.ClipSkyPolygon(newc[0], this.newv[stage][0], stage + 1);
			this.ClipSkyPolygon(newc[1], this.newv[stage][1], stage + 1);
		}

		private readonly float[][] verts = new float[OpenGLRenderApi.MAX_CLIP_VERTS][];

		/*
		 * R_AddSkySurface
		 */
		protected void R_AddSkySurface(msurface_t fa)
		{
			// calculate vertex values for sky box
			for (var p = fa.polys; p != null; p = p.next)
			{
				for (var i = 0; i < p.numverts; i++)
				{
					this.verts[i][0] = p.x(i) - this.r_origin[0];
					this.verts[i][1] = p.y(i) - this.r_origin[1];
					this.verts[i][2] = p.z(i) - this.r_origin[2];
				}

				this.ClipSkyPolygon(p.numverts, this.verts, 0);
			}
		}

		/*
		 * ============== R_ClearSkyBox ==============
		 */
		protected void R_ClearSkyBox()
		{
			int i;

			for (i = 0; i < 6; i++)
			{
				this.skymins[0][i] = this.skymins[1][i] = 9999;
				this.skymaxs[0][i] = this.skymaxs[1][i] = -9999;
			}
		}

		private void MakeSkyVec(float s, float t, int axis)
		{
			float[] v = { 0, 0, 0 };
			float[] b = { 0, 0, 0 };
			int j, k;

			b[0] = s * 2300;
			b[1] = t * 2300;
			b[2] = 2300;

			for (j = 0; j < 3; j++)
			{
				k = this.st_to_vec[axis][j];

				if (k < 0)
					v[j] = -b[-k - 1];
				else
					v[j] = b[k - 1];
			}

			// avoid bilerp seam
			s = (s + 1) * 0.5f;
			t = (t + 1) * 0.5f;

			if (s < this.sky_min)
				s = this.sky_min;
			else if (s > this.sky_max)
				s = this.sky_max;

			if (t < this.sky_min)
				t = this.sky_min;
			else if (t > this.sky_max)
				t = this.sky_max;

			t = 1.0f - t;
			this.gl.glTexCoord2f(s, t);
			this.gl.glVertex3f(v[0], v[1], v[2]);
		}

		/*
		 * ============== R_DrawSkyBox ==============
		 */
		private readonly int[] skytexorder = { 0, 2, 1, 3, 4, 5 };

		protected void R_DrawSkyBox()
		{
			int i;

			if (this.skyrotate != 0)
			{
				// check for no sky at all
				for (i = 0; i < 6; i++)
				{
					if (this.skymins[0][i] < this.skymaxs[0][i] && this.skymins[1][i] < this.skymaxs[1][i])
						break;
				}

				if (i == 6)
					return; // nothing visible
			}

			this.gl.glPushMatrix();
			this.gl.glTranslatef(this.r_origin[0], this.r_origin[1], this.r_origin[2]);
			this.gl.glRotatef(this.r_newrefdef.time * this.skyrotate, this.skyaxis[0], this.skyaxis[1], this.skyaxis[2]);

			for (i = 0; i < 6; i++)
			{
				if (this.skyrotate != 0)
				{
					// hack, forces full sky to draw when rotating
					this.skymins[0][i] = -1;
					this.skymins[1][i] = -1;
					this.skymaxs[0][i] = 1;
					this.skymaxs[1][i] = 1;
				}

				if (this.skymins[0][i] >= this.skymaxs[0][i] || this.skymins[1][i] >= this.skymaxs[1][i])
					continue;

				this.GL_Bind(this.sky_images[this.skytexorder[i]].texnum);

				this.gl.glBegin(OpenGL.GL_QUADS);
				this.MakeSkyVec(this.skymins[0][i], this.skymins[1][i], i);
				this.MakeSkyVec(this.skymins[0][i], this.skymaxs[1][i], i);
				this.MakeSkyVec(this.skymaxs[0][i], this.skymaxs[1][i], i);
				this.MakeSkyVec(this.skymaxs[0][i], this.skymins[1][i], i);
				this.gl.glEnd();
			}

			this.gl.glPopMatrix();
		}

		/*
		 * ============ R_SetSky ============
		 */
		// 3dstudio environment map names
		private readonly string[] suf = { "rt", "bk", "lf", "ft", "up", "dn" };

		public override void R_SetSky(string name, float rotate, float[] axis)
		{
			if (!(axis.Length == 3))
				throw new("vec3_t bug");

			int i;
			string pathname;

			//		strncpy (skyname, name, sizeof(skyname)-1);
			this.skyname = name;

			this.skyrotate = rotate;
			Math3D.VectorCopy(axis, this.skyaxis);

			for (i = 0; i < 6; i++)
			{
				// chop down rotating skies for less memory
				if (this.gl_skymip.value != 0 || this.skyrotate != 0)
					this.gl_picmip.value++;

				if (this.qglColorTableEXT && this.gl_ext_palettedtexture.value != 0)
				{
					//	Com_sprintf (pathname, sizeof(pathname), "env/%s%s.pcx",
					// skyname, suf[i]);
					pathname = "env/" + this.skyname + this.suf[i] + ".pcx";
				}
				else
				{
					// Com_sprintf (pathname, sizeof(pathname), "env/%s%s.tga",
					// skyname, suf[i]);
					pathname = "env/" + this.skyname + this.suf[i] + ".tga";
				}

				this.sky_images[i] = this.GL_FindImage(pathname, Base.it_sky);

				if (this.sky_images[i] == null)
					this.sky_images[i] = this.r_notexture;

				if (this.gl_skymip.value != 0 || this.skyrotate != 0)
				{
					// take less memory
					this.gl_picmip.value--;
					this.sky_min = 1.0f / 256;
					this.sky_max = 255.0f / 256;
				}
				else
				{
					this.sky_min = 1.0f / 512;
					this.sky_max = 511.0f / 512;
				}
			}
		}

		protected void WarpConstructor()
		{
			for (var i = 0; i < this.tmpVerts.Length; i++)
				this.tmpVerts[i] = new float[3];

			for (var i = 0; i < this.skymins.Length; i++)
				this.skymins[i] = new float[6];

			for (var i = 0; i < this.skymaxs.Length; i++)
				this.skymaxs[i] = new float[6];

			for (var i = 0; i < this.newv.Length; i++)
			{
				this.newv[i] = new float[2][][];

				for (var j = 0; j < this.newv[i].Length; j++)
				{
					this.newv[i][j] = new float[OpenGLRenderApi.MAX_CLIP_VERTS][];

					for (var k = 0; k < this.newv[i][j].Length; k++)
						this.newv[i][j][k] = new float[3];
				}
			}

			for (var i = 0; i < this.verts.Length; i++)
				this.verts[i] = new float[3];
		}
	}
}
