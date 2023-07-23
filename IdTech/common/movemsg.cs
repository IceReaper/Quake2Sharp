/*
 * Copyright (C) 1997-2001 Id Software, Inc.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or (at
 * your option) any later version.
 *
 * This program is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 *
 * See the GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA
 * 02111-1307, USA.
 */

using Quake2Sharp;
using Quake2Sharp.game.types;
using Quake2Sharp.util;
using System.Text;

namespace IdTech.common;

/*
 * =======================================================================
 *
 * Movement message (forward, backward, left, right, etc) handling.
 *
 * =======================================================================
 */
public static class movemsg
{
	private static float[][] bytedirs =
	{
		new[] { -0.525731f, 0.000000f, 0.850651f },
		new[] { -0.442863f, 0.238856f, 0.864188f },
		new[] { -0.295242f, 0.000000f, 0.955423f },
		new[] { -0.309017f, 0.500000f, 0.809017f },
		new[] { -0.162460f, 0.262866f, 0.951056f },
		new[] { 0.000000f, 0.000000f, 1.000000f },
		new[] { 0.000000f, 0.850651f, 0.525731f },
		new[] { -0.147621f, 0.716567f, 0.681718f },
		new[] { 0.147621f, 0.716567f, 0.681718f },
		new[] { 0.000000f, 0.525731f, 0.850651f },
		new[] { 0.309017f, 0.500000f, 0.809017f },
		new[] { 0.525731f, 0.000000f, 0.850651f },
		new[] { 0.295242f, 0.000000f, 0.955423f },
		new[] { 0.442863f, 0.238856f, 0.864188f },
		new[] { 0.162460f, 0.262866f, 0.951056f },
		new[] { -0.681718f, 0.147621f, 0.716567f },
		new[] { -0.809017f, 0.309017f, 0.500000f },
		new[] { -0.587785f, 0.425325f, 0.688191f },
		new[] { -0.850651f, 0.525731f, 0.000000f },
		new[] { -0.864188f, 0.442863f, 0.238856f },
		new[] { -0.716567f, 0.681718f, 0.147621f },
		new[] { -0.688191f, 0.587785f, 0.425325f },
		new[] { -0.500000f, 0.809017f, 0.309017f },
		new[] { -0.238856f, 0.864188f, 0.442863f },
		new[] { -0.425325f, 0.688191f, 0.587785f },
		new[] { -0.716567f, 0.681718f, -0.147621f },
		new[] { -0.500000f, 0.809017f, -0.309017f },
		new[] { -0.525731f, 0.850651f, 0.000000f },
		new[] { 0.000000f, 0.850651f, -0.525731f },
		new[] { -0.238856f, 0.864188f, -0.442863f },
		new[] { 0.000000f, 0.955423f, -0.295242f },
		new[] { -0.262866f, 0.951056f, -0.162460f },
		new[] { 0.000000f, 1.000000f, 0.000000f },
		new[] { 0.000000f, 0.955423f, 0.295242f },
		new[] { -0.262866f, 0.951056f, 0.162460f },
		new[] { 0.238856f, 0.864188f, 0.442863f },
		new[] { 0.262866f, 0.951056f, 0.162460f },
		new[] { 0.500000f, 0.809017f, 0.309017f },
		new[] { 0.238856f, 0.864188f, -0.442863f },
		new[] { 0.262866f, 0.951056f, -0.162460f },
		new[] { 0.500000f, 0.809017f, -0.309017f },
		new[] { 0.850651f, 0.525731f, 0.000000f },
		new[] { 0.716567f, 0.681718f, 0.147621f },
		new[] { 0.716567f, 0.681718f, -0.147621f },
		new[] { 0.525731f, 0.850651f, 0.000000f },
		new[] { 0.425325f, 0.688191f, 0.587785f },
		new[] { 0.864188f, 0.442863f, 0.238856f },
		new[] { 0.688191f, 0.587785f, 0.425325f },
		new[] { 0.809017f, 0.309017f, 0.500000f },
		new[] { 0.681718f, 0.147621f, 0.716567f },
		new[] { 0.587785f, 0.425325f, 0.688191f },
		new[] { 0.955423f, 0.295242f, 0.000000f },
		new[] { 1.000000f, 0.000000f, 0.000000f },
		new[] { 0.951056f, 0.162460f, 0.262866f },
		new[] { 0.850651f, -0.525731f, 0.000000f },
		new[] { 0.955423f, -0.295242f, 0.000000f },
		new[] { 0.864188f, -0.442863f, 0.238856f },
		new[] { 0.951056f, -0.162460f, 0.262866f },
		new[] { 0.809017f, -0.309017f, 0.500000f },
		new[] { 0.681718f, -0.147621f, 0.716567f },
		new[] { 0.850651f, 0.000000f, 0.525731f },
		new[] { 0.864188f, 0.442863f, -0.238856f },
		new[] { 0.809017f, 0.309017f, -0.500000f },
		new[] { 0.951056f, 0.162460f, -0.262866f },
		new[] { 0.525731f, 0.000000f, -0.850651f },
		new[] { 0.681718f, 0.147621f, -0.716567f },
		new[] { 0.681718f, -0.147621f, -0.716567f },
		new[] { 0.850651f, 0.000000f, -0.525731f },
		new[] { 0.809017f, -0.309017f, -0.500000f },
		new[] { 0.864188f, -0.442863f, -0.238856f },
		new[] { 0.951056f, -0.162460f, -0.262866f },
		new[] { 0.147621f, 0.716567f, -0.681718f },
		new[] { 0.309017f, 0.500000f, -0.809017f },
		new[] { 0.425325f, 0.688191f, -0.587785f },
		new[] { 0.442863f, 0.238856f, -0.864188f },
		new[] { 0.587785f, 0.425325f, -0.688191f },
		new[] { 0.688191f, 0.587785f, -0.425325f },
		new[] { -0.147621f, 0.716567f, -0.681718f },
		new[] { -0.309017f, 0.500000f, -0.809017f },
		new[] { 0.000000f, 0.525731f, -0.850651f },
		new[] { -0.525731f, 0.000000f, -0.850651f },
		new[] { -0.442863f, 0.238856f, -0.864188f },
		new[] { -0.295242f, 0.000000f, -0.955423f },
		new[] { -0.162460f, 0.262866f, -0.951056f },
		new[] { 0.000000f, 0.000000f, -1.000000f },
		new[] { 0.295242f, 0.000000f, -0.955423f },
		new[] { 0.162460f, 0.262866f, -0.951056f },
		new[] { -0.442863f, -0.238856f, -0.864188f },
		new[] { -0.309017f, -0.500000f, -0.809017f },
		new[] { -0.162460f, -0.262866f, -0.951056f },
		new[] { 0.000000f, -0.850651f, -0.525731f },
		new[] { -0.147621f, -0.716567f, -0.681718f },
		new[] { 0.147621f, -0.716567f, -0.681718f },
		new[] { 0.000000f, -0.525731f, -0.850651f },
		new[] { 0.309017f, -0.500000f, -0.809017f },
		new[] { 0.442863f, -0.238856f, -0.864188f },
		new[] { 0.162460f, -0.262866f, -0.951056f },
		new[] { 0.238856f, -0.864188f, -0.442863f },
		new[] { 0.500000f, -0.809017f, -0.309017f },
		new[] { 0.425325f, -0.688191f, -0.587785f },
		new[] { 0.716567f, -0.681718f, -0.147621f },
		new[] { 0.688191f, -0.587785f, -0.425325f },
		new[] { 0.587785f, -0.425325f, -0.688191f },
		new[] { 0.000000f, -0.955423f, -0.295242f },
		new[] { 0.000000f, -1.000000f, 0.000000f },
		new[] { 0.262866f, -0.951056f, -0.162460f },
		new[] { 0.000000f, -0.850651f, 0.525731f },
		new[] { 0.000000f, -0.955423f, 0.295242f },
		new[] { 0.238856f, -0.864188f, 0.442863f },
		new[] { 0.262866f, -0.951056f, 0.162460f },
		new[] { 0.500000f, -0.809017f, 0.309017f },
		new[] { 0.716567f, -0.681718f, 0.147621f },
		new[] { 0.525731f, -0.850651f, 0.000000f },
		new[] { -0.238856f, -0.864188f, -0.442863f },
		new[] { -0.500000f, -0.809017f, -0.309017f },
		new[] { -0.262866f, -0.951056f, -0.162460f },
		new[] { -0.850651f, -0.525731f, 0.000000f },
		new[] { -0.716567f, -0.681718f, -0.147621f },
		new[] { -0.716567f, -0.681718f, 0.147621f },
		new[] { -0.525731f, -0.850651f, 0.000000f },
		new[] { -0.500000f, -0.809017f, 0.309017f },
		new[] { -0.238856f, -0.864188f, 0.442863f },
		new[] { -0.262866f, -0.951056f, 0.162460f },
		new[] { -0.864188f, -0.442863f, 0.238856f },
		new[] { -0.809017f, -0.309017f, 0.500000f },
		new[] { -0.688191f, -0.587785f, 0.425325f },
		new[] { -0.681718f, -0.147621f, 0.716567f },
		new[] { -0.442863f, -0.238856f, 0.864188f },
		new[] { -0.587785f, -0.425325f, 0.688191f },
		new[] { -0.309017f, -0.500000f, 0.809017f },
		new[] { -0.147621f, -0.716567f, 0.681718f },
		new[] { -0.425325f, -0.688191f, 0.587785f },
		new[] { -0.162460f, -0.262866f, 0.951056f },
		new[] { 0.442863f, -0.238856f, 0.864188f },
		new[] { 0.162460f, -0.262866f, 0.951056f },
		new[] { 0.309017f, -0.500000f, 0.809017f },
		new[] { 0.147621f, -0.716567f, 0.681718f },
		new[] { 0.000000f, -0.525731f, 0.850651f },
		new[] { 0.425325f, -0.688191f, 0.587785f },
		new[] { 0.587785f, -0.425325f, 0.688191f },
		new[] { 0.688191f, -0.587785f, 0.425325f },
		new[] { -0.955423f, 0.295242f, 0.000000f },
		new[] { -0.951056f, 0.162460f, 0.262866f },
		new[] { -1.000000f, 0.000000f, 0.000000f },
		new[] { -0.850651f, 0.000000f, 0.525731f },
		new[] { -0.955423f, -0.295242f, 0.000000f },
		new[] { -0.951056f, -0.162460f, 0.262866f },
		new[] { -0.864188f, 0.442863f, -0.238856f },
		new[] { -0.951056f, 0.162460f, -0.262866f },
		new[] { -0.809017f, 0.309017f, -0.500000f },
		new[] { -0.864188f, -0.442863f, -0.238856f },
		new[] { -0.951056f, -0.162460f, -0.262866f },
		new[] { -0.809017f, -0.309017f, -0.500000f },
		new[] { -0.681718f, 0.147621f, -0.716567f },
		new[] { -0.681718f, -0.147621f, -0.716567f },
		new[] { -0.850651f, 0.000000f, -0.525731f },
		new[] { -0.688191f, 0.587785f, -0.425325f },
		new[] { -0.587785f, 0.425325f, -0.688191f },
		new[] { -0.425325f, 0.688191f, -0.587785f },
		new[] { -0.425325f, -0.688191f, -0.587785f },
		new[] { -0.587785f, -0.425325f, -0.688191f },
		new[] { -0.688191f, -0.587785f, -0.425325f }
	};

	public static void MSG_WriteChar(Stream sb, char c)
	{
		new BinaryWriter(sb).Write(c);
	}

	public static void MSG_WriteByte(Stream sb, byte b)
	{
		new BinaryWriter(sb).Write(b);
	}

	public static void MSG_WriteShort(Stream sb, short s)
	{
		new BinaryWriter(sb).Write(s);
	}

	public static void MSG_WriteLong(Stream sb, int l)
	{
		new BinaryWriter(sb).Write(l);
	}

	public static void MSG_WriteFloat(Stream sb, float f)
	{
		new BinaryWriter(sb).Write(f);
	}

	public static void MSG_WriteString(Stream sb, string? s)
	{
		var writer = new BinaryWriter(sb);

		if (s != null)
			writer.Write(Encoding.UTF8.GetBytes(s));

		writer.Write((byte)0);
	}

	public static void MSG_WriteCoord(Stream sb, float c)
	{
		MSG_WriteShort(sb, (short)(c * 8));
	}

	public static void MSG_WritePos(Stream sb, float[] pos)
	{
		MSG_WriteShort(sb, (short)(pos[0] * 8));
		MSG_WriteShort(sb, (short)(pos[1] * 8));
		MSG_WriteShort(sb, (short)(pos[2] * 8));
	}

	public static void MSG_WriteAngle(Stream sb, float f)
	{
		MSG_WriteByte(sb, (byte)(f * 256 / 360));
	}

	public static void MSG_WriteAngle16(Stream sb, float f)
	{
		MSG_WriteShort(sb, (short)Math3D.ANGLE2SHORT(f));
	}

	public static void MSG_WriteDeltaUsercmd(Stream buf, usercmd_t from, usercmd_t cmd)
	{
		/* Movement messages */
		var bits = (byte)0;

		if (cmd.angles[0] != from.angles[0])
			bits |= Defines.CM_ANGLE1;

		if (cmd.angles[1] != from.angles[1])
			bits |= Defines.CM_ANGLE2;

		if (cmd.angles[2] != from.angles[2])
			bits |= Defines.CM_ANGLE3;

		if (cmd.forwardmove != from.forwardmove)
			bits |= Defines.CM_FORWARD;

		if (cmd.sidemove != from.sidemove)
			bits |= Defines.CM_SIDE;

		if (cmd.upmove != from.upmove)
			bits |= Defines.CM_UP;

		if (cmd.buttons != from.buttons)
			bits |= Defines.CM_BUTTONS;

		if (cmd.impulse != from.impulse)
			bits |= Defines.CM_IMPULSE;

		MSG_WriteByte(buf, bits);

		if ((bits & Defines.CM_ANGLE1) != 0)
			MSG_WriteShort(buf, cmd.angles[0]);

		if ((bits & Defines.CM_ANGLE2) != 0)
			MSG_WriteShort(buf, cmd.angles[1]);

		if ((bits & Defines.CM_ANGLE3) != 0)
			MSG_WriteShort(buf, cmd.angles[2]);

		if ((bits & Defines.CM_FORWARD) != 0)
			MSG_WriteShort(buf, cmd.forwardmove);

		if ((bits & Defines.CM_SIDE) != 0)
			MSG_WriteShort(buf, cmd.sidemove);

		if ((bits & Defines.CM_UP) != 0)
			MSG_WriteShort(buf, cmd.upmove);

		if ((bits & Defines.CM_BUTTONS) != 0)
			MSG_WriteByte(buf, cmd.buttons);

		if ((bits & Defines.CM_IMPULSE) != 0)
			MSG_WriteByte(buf, cmd.impulse);

		MSG_WriteByte(buf, cmd.msec);
		MSG_WriteByte(buf, cmd.lightlevel);
	}

	public static void MSG_WriteDir(Stream sb, float[]? dir)
	{
		if (dir == null)
		{
			MSG_WriteByte(sb, 0);
			return;
		}

		var bestd = 0f;
		var best = (byte)0;

		for (byte i = 0; i < movemsg.bytedirs.Length; i++)
		{
			var d = Math3D.DotProduct(dir, movemsg.bytedirs[i]);

			if (d > bestd)
			{
				bestd = d;
				best = i;
			}
		}

		MSG_WriteByte(sb, best);
	}

	public static void MSG_ReadDir(Stream sb, float[] dir)
	{
		var b = MSG_ReadByte(sb);

		if (b >= bytedirs.Length)
			clientserver.Com_Error(Defines.ERR_DROP, "MSF_ReadDir: out of range");

		Math3D.VectorCopy(bytedirs[b], dir);
	}

	/*
	 * Writes part of a packetentities message.
	 * Can delta from either a baseline or a previous packet_entity
	 */
	public static void MSG_WriteDeltaEntity(entity_state_t from, entity_state_t to, Stream msg, bool force, bool newentity)
	{
		if (to.number == 0)
			clientserver.Com_Error(Defines.ERR_FATAL, "Unset entity number");

		if (to.number >= Defines.MAX_EDICTS)
			clientserver.Com_Error(Defines.ERR_FATAL, "Entity number >= MAX_EDICTS");

		/* send an update */
		var bits = 0;

		if (to.number >= 256)
			bits |= Defines.U_NUMBER16; /* number8 is implicit otherwise */

		if (to.origin[0] != from.origin[0])
			bits |= Defines.U_ORIGIN1;

		if (to.origin[1] != from.origin[1])
			bits |= Defines.U_ORIGIN2;

		if (to.origin[2] != from.origin[2])
			bits |= Defines.U_ORIGIN3;

		if (to.angles[0] != from.angles[0])
			bits |= Defines.U_ANGLE1;

		if (to.angles[1] != from.angles[1])
			bits |= Defines.U_ANGLE2;

		if (to.angles[2] != from.angles[2])
			bits |= Defines.U_ANGLE3;

		if (to.skinnum != from.skinnum)
		{
			if ((uint)to.skinnum < 256)
				bits |= Defines.U_SKIN8;
			else if ((uint)to.skinnum < 0x10000)
				bits |= Defines.U_SKIN16;
			else
				bits |= (Defines.U_SKIN8 | Defines.U_SKIN16);
		}

		if (to.frame != from.frame)
		{
			if (to.frame < 256)
				bits |= Defines.U_FRAME8;
			else
				bits |= Defines.U_FRAME16;
		}

		if (to.effects != from.effects)
		{
			if (to.effects < 256)
				bits |= Defines.U_EFFECTS8;
			else if (to.effects < 0x8000)
				bits |= Defines.U_EFFECTS16;
			else
				bits |= Defines.U_EFFECTS8 | Defines.U_EFFECTS16;
		}

		if (to.renderfx != from.renderfx)
		{
			if (to.renderfx < 256)
				bits |= Defines.U_RENDERFX8;
			else if (to.renderfx < 0x8000)
				bits |= Defines.U_RENDERFX16;
			else
				bits |= Defines.U_RENDERFX8 | Defines.U_RENDERFX16;
		}

		if (to.solid != from.solid)
			bits |= Defines.U_SOLID;

		/* event is not delta compressed, just 0 compressed */
		if (to.@event != 0)
			bits |= Defines.U_EVENT;

		if (to.modelindex != from.modelindex)
			bits |= Defines.U_MODEL;

		if (to.modelindex2 != from.modelindex2)
			bits |= Defines.U_MODEL2;

		if (to.modelindex3 != from.modelindex3)
			bits |= Defines.U_MODEL3;

		if (to.modelindex4 != from.modelindex4)
			bits |= Defines.U_MODEL4;

		if (to.sound != from.sound)
			bits |= Defines.U_SOUND;

		if (newentity || (to.renderfx & Defines.RF_BEAM) != 0)
			bits |= Defines.U_OLDORIGIN;

		/* write the message */
		if (bits == 0 && !force)
			return; /* nothing to send! */

		if ((bits & 0xff000000) != 0)
			bits |= Defines.U_MOREBITS3 | Defines.U_MOREBITS2 | Defines.U_MOREBITS1;
		else if ((bits & 0x00ff0000) != 0)
			bits |= Defines.U_MOREBITS2 | Defines.U_MOREBITS1;
		else if ((bits & 0x0000ff00) != 0)
			bits |= Defines.U_MOREBITS1;

		MSG_WriteByte(msg, (byte)(bits & 255));

		if ((bits & 0xff000000) != 0)
		{
			MSG_WriteByte(msg, (byte)(bits >> 8));
			MSG_WriteByte(msg, (byte)(bits >> 16));
			MSG_WriteByte(msg, (byte)(bits >> 24));
		}
		else if ((bits & 0x00ff0000) != 0)
		{
			MSG_WriteByte(msg, (byte)(bits >> 8));
			MSG_WriteByte(msg, (byte)(bits >> 16));
		}
		else if ((bits & 0x0000ff00) != 0)
			MSG_WriteByte(msg, (byte)(bits >> 8));

		if ((bits & Defines.U_NUMBER16) != 0)
			MSG_WriteShort(msg, (short)to.number);
		else
			MSG_WriteByte(msg, (byte)to.number);

		if ((bits & Defines.U_MODEL) != 0)
			MSG_WriteByte(msg, (byte)to.modelindex);

		if ((bits & Defines.U_MODEL2) != 0)
			MSG_WriteByte(msg, (byte)to.modelindex2);

		if ((bits & Defines.U_MODEL3) != 0)
			MSG_WriteByte(msg, (byte)to.modelindex3);

		if ((bits & Defines.U_MODEL4) != 0)
			MSG_WriteByte(msg, (byte)to.modelindex4);

		if ((bits & Defines.U_FRAME8) != 0)
			MSG_WriteByte(msg, (byte)to.frame);

		if ((bits & Defines.U_FRAME16) != 0)
			MSG_WriteShort(msg, (short)to.frame);

		if ((bits & Defines.U_SKIN8) != 0 && (bits & Defines.U_SKIN16) != 0) /*used for laser colors */
			MSG_WriteLong(msg, to.skinnum);
		else if ((bits & Defines.U_SKIN8) != 0)
			MSG_WriteByte(msg, (byte)to.skinnum);
		else if ((bits & Defines.U_SKIN16) != 0)
			MSG_WriteShort(msg, (short)to.skinnum);

		if ((bits & (Defines.U_EFFECTS8 | Defines.U_EFFECTS16)) == (Defines.U_EFFECTS8 | Defines.U_EFFECTS16))
			MSG_WriteLong(msg, to.effects);
		else if ((bits & Defines.U_EFFECTS8) != 0)
			MSG_WriteByte(msg, (byte)to.effects);
		else if ((bits & Defines.U_EFFECTS16) != 0)
			MSG_WriteShort(msg, (short)to.effects);

		if ((bits & (Defines.U_RENDERFX8 | Defines.U_RENDERFX16)) == (Defines.U_RENDERFX8 | Defines.U_RENDERFX16))
			MSG_WriteLong(msg, to.renderfx);
		else if ((bits & Defines.U_RENDERFX8) != 0)
			MSG_WriteByte(msg, (byte)to.renderfx);
		else if ((bits & Defines.U_RENDERFX16) != 0)
			MSG_WriteShort(msg, (short)to.renderfx);

		if ((bits & Defines.U_ORIGIN1) != 0)
			MSG_WriteCoord(msg, to.origin[0]);

		if ((bits & Defines.U_ORIGIN2) != 0)
			MSG_WriteCoord(msg, to.origin[1]);

		if ((bits & Defines.U_ORIGIN3) != 0)
			MSG_WriteCoord(msg, to.origin[2]);

		if ((bits & Defines.U_ANGLE1) != 0)
			MSG_WriteAngle(msg, to.angles[0]);

		if ((bits & Defines.U_ANGLE2) != 0)
			MSG_WriteAngle(msg, to.angles[1]);

		if ((bits & Defines.U_ANGLE3) != 0)
			MSG_WriteAngle(msg, to.angles[2]);

		if ((bits & Defines.U_OLDORIGIN) != 0)
		{
			MSG_WriteCoord(msg, to.old_origin[0]);
			MSG_WriteCoord(msg, to.old_origin[1]);
			MSG_WriteCoord(msg, to.old_origin[2]);
		}

		if ((bits & Defines.U_SOUND) != 0)
			MSG_WriteByte(msg, (byte)to.sound);

		if ((bits & Defines.U_EVENT) != 0)
			MSG_WriteByte(msg, (byte)to.@event);

		if ((bits & Defines.U_SOLID) != 0)
			MSG_WriteShort(msg, (short)to.solid);
	}

	public static void MSG_BeginReading(Stream msg)
	{
		msg.Position = 0;
	}

	public static char MSG_ReadChar(Stream msg_read)
	{
		return new BinaryReader(msg_read).ReadChar();
	}

	public static byte MSG_ReadByte(Stream msg_read)
	{
		return new BinaryReader(msg_read).ReadByte();
	}

	public static short MSG_ReadShort(Stream msg_read)
	{
		return new BinaryReader(msg_read).ReadInt16();
	}

	public static int MSG_ReadLong(Stream msg_read)
	{
		return new BinaryReader(msg_read).ReadInt32();
	}

	public static float MSG_ReadFloat(Stream msg_read)
	{
		return new BinaryReader(msg_read).ReadSingle();
	}

	public static string MSG_ReadString(Stream msg_read)
	{
		var reader = new BinaryReader(msg_read);
		var @string = new StringBuilder(2048);

		while (true)
		{
			var character = reader.ReadChar();

			if (character == '\0')
				break;

			@string.Append(character);
		}

		return @string.ToString();
	}

	public static string MSG_ReadStringLine(Stream msg_read)
	{
		var reader = new BinaryReader(msg_read);
		var @string = new StringBuilder(2048);

		while (true)
		{
			var character = reader.ReadChar();

			if (character == '\0' || character == '\n')
				break;

			@string.Append(character);
		}

		return @string.ToString();
	}

	public static float MSG_ReadCoord(Stream msg_read)
	{
		return MSG_ReadShort(msg_read) * 0.125f;
	}

	public static void MSG_ReadPos(Stream msg_read, float[] pos)
	{
		pos[0] = MSG_ReadShort(msg_read) * (0.125f);
		pos[1] = MSG_ReadShort(msg_read) * (0.125f);
		pos[2] = MSG_ReadShort(msg_read) * (0.125f);
	}

	public static float MSG_ReadAngle(Stream msg_read)
	{
		return MSG_ReadByte(msg_read) * 1.40625f;
	}

	public static float MSG_ReadAngle16(Stream msg_read)
	{
		return Math3D.SHORT2ANGLE(MSG_ReadShort(msg_read));
	}

	public static void MSG_ReadDeltaUsercmd(Stream msg_read, usercmd_t from, usercmd_t move)
	{
		var bits = movemsg.MSG_ReadByte(msg_read);

		/* read current angles */
		move.angles[0] = (bits & Defines.CM_ANGLE1) != 0 ? movemsg.MSG_ReadShort(msg_read) : from.angles[0];
		move.angles[1] = (bits & Defines.CM_ANGLE2) != 0 ? movemsg.MSG_ReadShort(msg_read) : from.angles[1];
		move.angles[2] = (bits & Defines.CM_ANGLE3) != 0 ? movemsg.MSG_ReadShort(msg_read) : from.angles[2];

		/* read movement */
		move.forwardmove = (bits & Defines.CM_FORWARD) != 0 ? movemsg.MSG_ReadShort(msg_read) : from.forwardmove;
		move.sidemove = (bits & Defines.CM_SIDE) != 0 ? movemsg.MSG_ReadShort(msg_read) : from.sidemove;
		move.upmove = (bits & Defines.CM_UP) != 0 ? movemsg.MSG_ReadShort(msg_read) : from.upmove;

		/* read buttons */
		move.buttons = (bits & Defines.CM_BUTTONS) != 0 ? movemsg.MSG_ReadByte(msg_read) : from.buttons;
		move.impulse = (bits & Defines.CM_IMPULSE) != 0 ? movemsg.MSG_ReadByte(msg_read) : from.impulse;

		/* read time to run command */
		move.msec = MSG_ReadByte(msg_read);

		/* read the light level */
		move.lightlevel = MSG_ReadByte(msg_read);
	}

	public static void MSG_ReadData(Stream msg_read, byte[] data, int len)
	{
		msg_read.Read(data, 0, len);
	}
}
