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
namespace Quake2Sharp.util;

using game.types;
using qcommon;

public class Math3D
{
	private static readonly float shortratio = 360.0f / 65536.0f;
	private static readonly float piratio = (float)(Math.PI / 360.0);

	public static void set(float[] v1, float[] v2)
	{
		v1[0] = v2[0];
		v1[1] = v2[1];
		v1[2] = v2[2];
	}

	public static void VectorSubtract(float[] a, float[] b, float[] c)
	{
		c[0] = a[0] - b[0];
		c[1] = a[1] - b[1];
		c[2] = a[2] - b[2];
	}

	public static void VectorSubtract(short[] a, short[] b, int[] c)
	{
		c[0] = a[0] - b[0];
		c[1] = a[1] - b[1];
		c[2] = a[2] - b[2];
	}

	public static void VectorAdd(float[] a, float[] b, float[] to)
	{
		to[0] = a[0] + b[0];
		to[1] = a[1] + b[1];
		to[2] = a[2] + b[2];
	}

	public static void VectorCopy(float[] from, float[] to)
	{
		to[0] = from[0];
		to[1] = from[1];
		to[2] = from[2];
	}

	public static void VectorCopy(short[] from, short[] to)
	{
		to[0] = from[0];
		to[1] = from[1];
		to[2] = from[2];
	}

	public static void VectorCopy(short[] from, float[] to)
	{
		to[0] = from[0];
		to[1] = from[1];
		to[2] = from[2];
	}

	public static void VectorCopy(float[] from, short[] to)
	{
		to[0] = (short)from[0];
		to[1] = (short)from[1];
		to[2] = (short)from[2];
	}

	public static void VectorClear(float[] a)
	{
		a[0] = a[1] = a[2] = 0;
	}

	public static bool VectorEquals(float[] v1, float[] v2)
	{
		if (v1[0] != v2[0] || v1[1] != v2[1] || v1[2] != v2[2])
			return false;

		return true;
	}

	public static void VectorNegate(float[] from, float[] to)
	{
		to[0] = -from[0];
		to[1] = -from[1];
		to[2] = -from[2];
	}

	public static void VectorSet(float[] v, float x, float y, float z)
	{
		v[0] = x;
		v[1] = y;
		v[2] = z;
	}

	public static void VectorMA(float[] veca, float scale, float[] vecb, float[] to)
	{
		to[0] = veca[0] + scale * vecb[0];
		to[1] = veca[1] + scale * vecb[1];
		to[2] = veca[2] + scale * vecb[2];
	}

	public static float VectorNormalize(float[] v)
	{
		var length = Math3D.VectorLength(v);

		if (length != 0.0f)
		{
			var ilength = 1.0f / length;
			v[0] *= ilength;
			v[1] *= ilength;
			v[2] *= ilength;
		}

		return length;
	}

	public static float VectorLength(float[] v)
	{
		return (float)Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
	}

	public static void VectorInverse(float[] v)
	{
		v[0] = -v[0];
		v[1] = -v[1];
		v[2] = -v[2];
	}

	public static void VectorScale(float[] @in, float scale, float[] @out)
	{
		@out[0] = @in[0] * scale;
		@out[1] = @in[1] * scale;
		@out[2] = @in[2] * scale;
	}

	public static float vectoyaw(float[] vec)
	{
		float yaw;

		if ( /*vec[YAW] == 0 &&*/
		    vec[Defines.PITCH] == 0)
		{
			yaw = 0;

			if (vec[Defines.YAW] > 0)
				yaw = 90;
			else if (vec[Defines.YAW] < 0)
				yaw = -90;
		}
		else
		{
			yaw = (int)(Math.Atan2(vec[Defines.YAW], vec[Defines.PITCH]) * 180 / Math.PI);

			if (yaw < 0)
				yaw += 360;
		}

		return yaw;
	}

	public static void vectoangles(float[] value1, float[] angles)
	{
		float yaw, pitch;

		if (value1[1] == 0 && value1[0] == 0)
		{
			yaw = 0;

			if (value1[2] > 0)
				pitch = 90;
			else
				pitch = 270;
		}
		else
		{
			if (value1[0] != 0)
				yaw = (int)(Math.Atan2(value1[1], value1[0]) * 180 / Math.PI);
			else if (value1[1] > 0)
				yaw = 90;
			else
				yaw = -90;

			if (yaw < 0)
				yaw += 360;

			var forward = (float)Math.Sqrt(value1[0] * value1[0] + value1[1] * value1[1]);
			pitch = (int)(Math.Atan2(value1[2], forward) * 180 / Math.PI);

			if (pitch < 0)
				pitch += 360;
		}

		angles[Defines.PITCH] = -pitch;
		angles[Defines.YAW] = yaw;
		angles[Defines.ROLL] = 0;
	}

	private static readonly float[][] m = new float[3][];
	private static readonly float[][] im = new float[3][];
	private static readonly float[][] tmpmat = new float[3][];
	private static readonly float[][] zrot = new float[3][];

	// to reduce garbage
	private static readonly float[] vr = { 0, 0, 0 };
	private static readonly float[] vup = { 0, 0, 0 };
	private static readonly float[] vf = { 0, 0, 0 };

	public static void RotatePointAroundVector(float[] dst, float[] dir, float[] point, float degrees)
	{
		Math3D.vf[0] = dir[0];
		Math3D.vf[1] = dir[1];
		Math3D.vf[2] = dir[2];

		Math3D.PerpendicularVector(Math3D.vr, dir);
		Math3D.CrossProduct(Math3D.vr, Math3D.vf, Math3D.vup);

		Math3D.m[0][0] = Math3D.vr[0];
		Math3D.m[1][0] = Math3D.vr[1];
		Math3D.m[2][0] = Math3D.vr[2];

		Math3D.m[0][1] = Math3D.vup[0];
		Math3D.m[1][1] = Math3D.vup[1];
		Math3D.m[2][1] = Math3D.vup[2];

		Math3D.m[0][2] = Math3D.vf[0];
		Math3D.m[1][2] = Math3D.vf[1];
		Math3D.m[2][2] = Math3D.vf[2];

		Math3D.im[0][0] = Math3D.m[0][0];
		Math3D.im[0][1] = Math3D.m[1][0];
		Math3D.im[0][2] = Math3D.m[2][0];
		Math3D.im[1][0] = Math3D.m[0][1];
		Math3D.im[1][1] = Math3D.m[1][1];
		Math3D.im[1][2] = Math3D.m[2][1];
		Math3D.im[2][0] = Math3D.m[0][2];
		Math3D.im[2][1] = Math3D.m[1][2];
		Math3D.im[2][2] = Math3D.m[2][2];

		Math3D.zrot[0][2] = Math3D.zrot[1][2] = Math3D.zrot[2][0] = Math3D.zrot[2][1] = 0.0f;

		Math3D.zrot[2][2] = 1.0F;

		Math3D.zrot[0][0] = Math3D.zrot[1][1] = (float)Math.Cos(Math3D.DEG2RAD(degrees));
		Math3D.zrot[0][1] = (float)Math.Sin(Math3D.DEG2RAD(degrees));
		Math3D.zrot[1][0] = -Math3D.zrot[0][1];

		Math3D.R_ConcatRotations(Math3D.m, Math3D.zrot, Math3D.tmpmat);
		Math3D.R_ConcatRotations(Math3D.tmpmat, Math3D.im, Math3D.zrot);

		for (var i = 0; i < 3; i++)
			dst[i] = Math3D.zrot[i][0] * point[0] + Math3D.zrot[i][1] * point[1] + Math3D.zrot[i][2] * point[2];
	}

	public static void MakeNormalVectors(float[] forward, float[] right, float[] up)
	{
		// this rotate and negat guarantees a vector
		// not colinear with the original
		right[1] = -forward[0];
		right[2] = forward[1];
		right[0] = forward[2];

		var d = Math3D.DotProduct(right, forward);
		Math3D.VectorMA(right, -d, forward, right);
		Math3D.VectorNormalize(right);
		Math3D.CrossProduct(right, forward, up);
	}

	public static float SHORT2ANGLE(int x)
	{
		return x * Math3D.shortratio;
	}

	/*
	================
	R_ConcatTransforms
	================
	*/
	public static void R_ConcatTransforms(float[][] in1, float[][] in2, float[][] @out)
	{
		@out[0][0] = in1[0][0] * in2[0][0] + in1[0][1] * in2[1][0] + in1[0][2] * in2[2][0];
		@out[0][1] = in1[0][0] * in2[0][1] + in1[0][1] * in2[1][1] + in1[0][2] * in2[2][1];
		@out[0][2] = in1[0][0] * in2[0][2] + in1[0][1] * in2[1][2] + in1[0][2] * in2[2][2];
		@out[0][3] = in1[0][0] * in2[0][3] + in1[0][1] * in2[1][3] + in1[0][2] * in2[2][3] + in1[0][3];
		@out[1][0] = in1[1][0] * in2[0][0] + in1[1][1] * in2[1][0] + in1[1][2] * in2[2][0];
		@out[1][1] = in1[1][0] * in2[0][1] + in1[1][1] * in2[1][1] + in1[1][2] * in2[2][1];
		@out[1][2] = in1[1][0] * in2[0][2] + in1[1][1] * in2[1][2] + in1[1][2] * in2[2][2];
		@out[1][3] = in1[1][0] * in2[0][3] + in1[1][1] * in2[1][3] + in1[1][2] * in2[2][3] + in1[1][3];
		@out[2][0] = in1[2][0] * in2[0][0] + in1[2][1] * in2[1][0] + in1[2][2] * in2[2][0];
		@out[2][1] = in1[2][0] * in2[0][1] + in1[2][1] * in2[1][1] + in1[2][2] * in2[2][1];
		@out[2][2] = in1[2][0] * in2[0][2] + in1[2][1] * in2[1][2] + in1[2][2] * in2[2][2];
		@out[2][3] = in1[2][0] * in2[0][3] + in1[2][1] * in2[1][3] + in1[2][2] * in2[2][3] + in1[2][3];
	}

	/**
	 * concatenates 2 matrices each [3][3].
	 */
	public static void R_ConcatRotations(float[][] in1, float[][] in2, float[][] @out)
	{
		@out[0][0] = in1[0][0] * in2[0][0] + in1[0][1] * in2[1][0] + in1[0][2] * in2[2][0];
		@out[0][1] = in1[0][0] * in2[0][1] + in1[0][1] * in2[1][1] + in1[0][2] * in2[2][1];
		@out[0][2] = in1[0][0] * in2[0][2] + in1[0][1] * in2[1][2] + in1[0][2] * in2[2][2];
		@out[1][0] = in1[1][0] * in2[0][0] + in1[1][1] * in2[1][0] + in1[1][2] * in2[2][0];
		@out[1][1] = in1[1][0] * in2[0][1] + in1[1][1] * in2[1][1] + in1[1][2] * in2[2][1];
		@out[1][2] = in1[1][0] * in2[0][2] + in1[1][1] * in2[1][2] + in1[1][2] * in2[2][2];
		@out[2][0] = in1[2][0] * in2[0][0] + in1[2][1] * in2[1][0] + in1[2][2] * in2[2][0];
		@out[2][1] = in1[2][0] * in2[0][1] + in1[2][1] * in2[1][1] + in1[2][2] * in2[2][1];
		@out[2][2] = in1[2][0] * in2[0][2] + in1[2][1] * in2[1][2] + in1[2][2] * in2[2][2];
	}

	public static void ProjectPointOnPlane(float[] dst, float[] p, float[] normal)
	{
		var inv_denom = 1.0F / Math3D.DotProduct(normal, normal);

		var d = Math3D.DotProduct(normal, p) * inv_denom;

		dst[0] = normal[0] * inv_denom;
		dst[1] = normal[1] * inv_denom;
		dst[2] = normal[2] * inv_denom;

		dst[0] = p[0] - d * dst[0];
		dst[1] = p[1] - d * dst[1];
		dst[2] = p[2] - d * dst[2];
	}

	private static readonly float[][] PLANE_XYZ = { new float[] { 1, 0, 0 }, new float[] { 0, 1, 0 }, new float[] { 0, 0, 1 } };

	/** assumes "src" is normalized */
	public static void PerpendicularVector(float[] dst, float[] src)
	{
		int pos;
		int i;
		var minelem = 1.0F;

		// find the smallest magnitude axially aligned vector
		for (pos = 0, i = 0; i < 3; i++)
		{
			if (Math.Abs(src[i]) < minelem)
			{
				pos = i;
				minelem = Math.Abs(src[i]);
			}
		}

		// project the point onto the plane defined by src
		Math3D.ProjectPointOnPlane(dst, Math3D.PLANE_XYZ[pos], src);

		//normalize the result
		Math3D.VectorNormalize(dst);
	}

	//=====================================================================
	/** 
	 stellt fest, auf welcher Seite sich die Kiste befindet, wenn die Ebene 
	 durch Entfernung und Senkrechten-Normale gegeben ist.    
	 erste Version mit vec3_t... */
	public static int BoxOnPlaneSide(float[] emins, float[] emaxs, cplane_t p)
	{
		if (!(emins.Length == 3 && emaxs.Length == 3))
			throw new("vec3_t bug");

		float dist1, dist2;
		int sides;

		//	   fast axial cases
		if (p.type < 3)
		{
			if (p.dist <= emins[p.type])
				return 1;

			if (p.dist >= emaxs[p.type])
				return 2;

			return 3;
		}

		//	   general case
		switch (p.signbits)
		{
			case 0:
				dist1 = p.normal[0] * emaxs[0] + p.normal[1] * emaxs[1] + p.normal[2] * emaxs[2];
				dist2 = p.normal[0] * emins[0] + p.normal[1] * emins[1] + p.normal[2] * emins[2];

				break;

			case 1:
				dist1 = p.normal[0] * emins[0] + p.normal[1] * emaxs[1] + p.normal[2] * emaxs[2];
				dist2 = p.normal[0] * emaxs[0] + p.normal[1] * emins[1] + p.normal[2] * emins[2];

				break;

			case 2:
				dist1 = p.normal[0] * emaxs[0] + p.normal[1] * emins[1] + p.normal[2] * emaxs[2];
				dist2 = p.normal[0] * emins[0] + p.normal[1] * emaxs[1] + p.normal[2] * emins[2];

				break;

			case 3:
				dist1 = p.normal[0] * emins[0] + p.normal[1] * emins[1] + p.normal[2] * emaxs[2];
				dist2 = p.normal[0] * emaxs[0] + p.normal[1] * emaxs[1] + p.normal[2] * emins[2];

				break;

			case 4:
				dist1 = p.normal[0] * emaxs[0] + p.normal[1] * emaxs[1] + p.normal[2] * emins[2];
				dist2 = p.normal[0] * emins[0] + p.normal[1] * emins[1] + p.normal[2] * emaxs[2];

				break;

			case 5:
				dist1 = p.normal[0] * emins[0] + p.normal[1] * emaxs[1] + p.normal[2] * emins[2];
				dist2 = p.normal[0] * emaxs[0] + p.normal[1] * emins[1] + p.normal[2] * emaxs[2];

				break;

			case 6:
				dist1 = p.normal[0] * emaxs[0] + p.normal[1] * emins[1] + p.normal[2] * emins[2];
				dist2 = p.normal[0] * emins[0] + p.normal[1] * emaxs[1] + p.normal[2] * emaxs[2];

				break;

			case 7:
				dist1 = p.normal[0] * emins[0] + p.normal[1] * emins[1] + p.normal[2] * emins[2];
				dist2 = p.normal[0] * emaxs[0] + p.normal[1] * emaxs[1] + p.normal[2] * emaxs[2];

				break;

			default:
				dist1 = dist2 = 0;

				throw new("BoxOnPlaneSide bug");
		}

		sides = 0;

		if (dist1 >= p.dist)
			sides = 1;

		if (dist2 < p.dist)
			sides |= 2;

		if (!(sides != 0))
			throw new("BoxOnPlaneSide(): sides == 0 bug");

		return sides;
	}

	//	this is the slow, general version
	private static readonly float[][] corners = new float[2][];

	public static int BoxOnPlaneSide2(float[] emins, float[] emaxs, cplane_t p)
	{
		for (var i = 0; i < 3; i++)
		{
			if (p.normal[i] < 0)
			{
				Math3D.corners[0][i] = emins[i];
				Math3D.corners[1][i] = emaxs[i];
			}
			else
			{
				Math3D.corners[1][i] = emins[i];
				Math3D.corners[0][i] = emaxs[i];
			}
		}

		var dist1 = Math3D.DotProduct(p.normal, Math3D.corners[0]) - p.dist;
		var dist2 = Math3D.DotProduct(p.normal, Math3D.corners[1]) - p.dist;
		var sides = 0;

		if (dist1 >= 0)
			sides = 1;

		if (dist2 < 0)
			sides |= 2;

		return sides;
	}

	public static void AngleVectors(float[] angles, float[] forward, float[] right, float[] up)
	{
		var cr = 2.0f * Math3D.piratio;
		var angle = (float)(angles[Defines.YAW] * cr);
		var sy = (float)Math.Sin(angle);
		var cy = (float)Math.Cos(angle);
		angle = (float)(angles[Defines.PITCH] * cr);
		var sp = (float)Math.Sin(angle);
		var cp = (float)Math.Cos(angle);

		if (forward != null)
		{
			forward[0] = cp * cy;
			forward[1] = cp * sy;
			forward[2] = -sp;
		}

		if (right != null || up != null)
		{
			angle = (float)(angles[Defines.ROLL] * cr);
			var sr = (float)Math.Sin(angle);
			cr = (float)Math.Cos(angle);

			if (right != null)
			{
				right[0] = -sr * sp * cy + cr * sy;
				right[1] = -sr * sp * sy + -cr * cy;
				right[2] = -sr * cp;
			}

			if (up != null)
			{
				up[0] = cr * sp * cy + sr * sy;
				up[1] = cr * sp * sy + -sr * cy;
				up[2] = cr * cp;
			}
		}
	}

	public static void G_ProjectSource(float[] point, float[] distance, float[] forward, float[] right, float[] result)
	{
		result[0] = point[0] + forward[0] * distance[0] + right[0] * distance[1];
		result[1] = point[1] + forward[1] * distance[0] + right[1] * distance[1];
		result[2] = point[2] + forward[2] * distance[0] + right[2] * distance[1] + distance[2];
	}

	public static float DotProduct(float[] x, float[] y)
	{
		return x[0] * y[0] + x[1] * y[1] + x[2] * y[2];
	}

	public static void CrossProduct(float[] v1, float[] v2, float[] cross)
	{
		cross[0] = v1[1] * v2[2] - v1[2] * v2[1];
		cross[1] = v1[2] * v2[0] - v1[0] * v2[2];
		cross[2] = v1[0] * v2[1] - v1[1] * v2[0];
	}

	public static int Q_log2(int val)
	{
		var answer = 0;

		while ((val >>= 1) > 0)
			answer++;

		return answer;
	}

	public static float DEG2RAD(float @in)
	{
		return @in * (float)Math.PI / 180.0f;
	}

	public static float anglemod(float a)
	{
		return (float)Math3D.shortratio * ((int)(a / Math3D.shortratio) & 65535);
	}

	public static int ANGLE2SHORT(float x)
	{
		return (int)(x / Math3D.shortratio) & 65535;
	}

	public static float LerpAngle(float a2, float a1, float frac)
	{
		if (a1 - a2 > 180)
			a1 -= 360;

		if (a1 - a2 < -180)
			a1 += 360;

		return a2 + frac * (a1 - a2);
	}

	public static float CalcFov(float fov_x, float width, float height)
	{
		double a = 0.0f;
		double x;

		if (fov_x < 1.0f || fov_x > 179.0f)
			Com.Error(Defines.ERR_DROP, "Bad fov: " + fov_x);

		x = width / Math.Tan(fov_x * Math3D.piratio);

		a = Math.Atan(height / x);

		a = a / Math3D.piratio;

		return (float)a;
	}

	static Math3D()
	{
		for (var n = 0; n < Math3D.m.Length; n++)
			Math3D.m[n] = new float[3];

		for (var n = 0; n < Math3D.im.Length; n++)
			Math3D.im[n] = new float[3];

		for (var n = 0; n < Math3D.tmpmat.Length; n++)
			Math3D.tmpmat[n] = new float[3];

		for (var n = 0; n < Math3D.zrot.Length; n++)
			Math3D.zrot[n] = new float[3];

		for (var n = 0; n < Math3D.corners.Length; n++)
			Math3D.corners[n] = new float[3];
	}
}