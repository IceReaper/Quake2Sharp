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
using Quake2Sharp.qcommon.types;
using Quake2Sharp.util;
using System.Numerics;
using System.Text;

namespace Quake2Sharp.qcommon;

public class MSG
{
	//
	// writing functions
	//

	//ok.
	public static void WriteChar(sizebuf_t sb, int c)
	{
		sb.data[SZ.GetSpace(sb, 1)] = (byte)(c & 0xFF);
	}

	//ok.
	public static void WriteChar(sizebuf_t sb, float c)
	{
		MSG.WriteChar(sb, (int)c);
	}

	//ok.
	public static void WriteByte(sizebuf_t sb, int c)
	{
		sb.data[SZ.GetSpace(sb, 1)] = (byte)(c & 0xFF);
	}

	//ok.
	public static void WriteByte(sizebuf_t sb, float c)
	{
		MSG.WriteByte(sb, (int)c);
	}

	public static void WriteShort(sizebuf_t sb, int c)
	{
		var i = SZ.GetSpace(sb, 2);
		sb.data[i++] = (byte)(c & 0xff);
		sb.data[i] = (byte)(((uint)c >> 8) & 0xFF);
	}

	//ok.
	public static void WriteInt(sizebuf_t sb, int c)
	{
		var i = SZ.GetSpace(sb, 4);
		sb.data[i++] = (byte)(c & 0xff);
		sb.data[i++] = (byte)(((uint)c >> 8) & 0xff);
		sb.data[i++] = (byte)(((uint)c >> 16) & 0xff);
		sb.data[i++] = (byte)(((uint)c >> 24) & 0xff);
	}

	//ok.
	public static void WriteLong(sizebuf_t sb, int c)
	{
		MSG.WriteInt(sb, c);
	}

	//ok.
	public static void WriteFloat(sizebuf_t sb, float f)
	{
		MSG.WriteInt(sb, BitConverter.ToInt32(BitConverter.GetBytes(f), 0));
	}

	// had a bug, now its ok.
	public static void WriteString(sizebuf_t sb, string s)
	{
		var x = s;

		if (s == null)
			x = "";

		SZ.Write(sb, Lib.stringToBytes(x));
		MSG.WriteByte(sb, 0);

		//Com.dprintln("MSG.WriteString:" + s.replace('\0', '@'));
	}

	//ok.
	public static void WriteString(sizebuf_t sb, byte[] s)
	{
		MSG.WriteString(sb, Encoding.ASCII.GetString(s).Split((char)0)[0]);
	}

	public static void WriteCoord(sizebuf_t sb, float f)
	{
		MSG.WriteShort(sb, (int)(f * 8));
	}

	public static void WritePos(sizebuf_t sb, float[] pos)
	{
		if (!(pos.Length == 3))
			throw new("vec3_t bug");

		MSG.WriteShort(sb, (int)(pos[0] * 8));
		MSG.WriteShort(sb, (int)(pos[1] * 8));
		MSG.WriteShort(sb, (int)(pos[2] * 8));
	}

	public static void WriteAngle(sizebuf_t sb, float f)
	{
		MSG.WriteByte(sb, (int)(f * 256 / 360) & 255);
	}

	public static void WriteAngle16(sizebuf_t sb, float f)
	{
		MSG.WriteShort(sb, Math3D.ANGLE2SHORT(f));
	}

	public static void WriteDeltaUsercmd(sizebuf_t buf, usercmd_t from, usercmd_t cmd)
	{
		int bits;

		//
		// send the movement message
		//
		bits = 0;

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

		MSG.WriteByte(buf, bits);

		if ((bits & Defines.CM_ANGLE1) != 0)
			MSG.WriteShort(buf, cmd.angles[0]);

		if ((bits & Defines.CM_ANGLE2) != 0)
			MSG.WriteShort(buf, cmd.angles[1]);

		if ((bits & Defines.CM_ANGLE3) != 0)
			MSG.WriteShort(buf, cmd.angles[2]);

		if ((bits & Defines.CM_FORWARD) != 0)
			MSG.WriteShort(buf, cmd.forwardmove);

		if ((bits & Defines.CM_SIDE) != 0)
			MSG.WriteShort(buf, cmd.sidemove);

		if ((bits & Defines.CM_UP) != 0)
			MSG.WriteShort(buf, cmd.upmove);

		if ((bits & Defines.CM_BUTTONS) != 0)
			MSG.WriteByte(buf, cmd.buttons);

		if ((bits & Defines.CM_IMPULSE) != 0)
			MSG.WriteByte(buf, cmd.impulse);

		MSG.WriteByte(buf, cmd.msec);
		MSG.WriteByte(buf, cmd.lightlevel);
	}

	//should be ok.
	public static void WriteDir(sizebuf_t sb, float[] dir)
	{
		int i, best;
		float d, bestd;

		if (dir == null)
		{
			MSG.WriteByte(sb, 0);

			return;
		}

		bestd = 0;
		best = 0;

		for (i = 0; i < Defines.NUMVERTEXNORMALS; i++)
		{
			d = Math3D.DotProduct(dir, Globals.bytedirs[i]);

			if (d > bestd)
			{
				bestd = d;
				best = i;
			}
		}

		MSG.WriteByte(sb, best);
	}

	//should be ok.
	public static Vector3 ReadDir(sizebuf_t sb)
	{
		var dir = new float[3];
		MSG.ReadDir(sb, dir);

		return new(dir[0], dir[1], dir[2]);
	}

	public static void ReadDir(sizebuf_t sb, float[] dir)
	{
		int b;

		b = MSG.ReadByte(sb);

		if (b >= Defines.NUMVERTEXNORMALS)
			clientserver.Com_Error(Defines.ERR_DROP, "MSF_ReadDir: out of range");

		Math3D.VectorCopy(Globals.bytedirs[b], dir);
	}

	/*
	 * ================== WriteDeltaEntity
	 *
	 * Writes part of a packetentities message. Can delta from either a baseline
	 * or a previous packet_entity ==================
	 */
	public static void WriteDeltaEntity(entity_state_t from, entity_state_t to, sizebuf_t msg, bool force, bool newentity)
	{
		int bits;

		if (0 == to.number)
			clientserver.Com_Error(Defines.ERR_FATAL, "Unset entity number");

		if (to.number >= Defines.MAX_EDICTS)
			clientserver.Com_Error(Defines.ERR_FATAL, "Entity number >= MAX_EDICTS");

		// send an update
		bits = 0;

		if (to.number >= 256)
			bits |= Defines.U_NUMBER16; // number8 is implicit otherwise

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
			if (to.skinnum < 256)
				bits |= Defines.U_SKIN8;
			else if (to.skinnum < 0x10000)
				bits |= Defines.U_SKIN16;
			else
				bits |= Defines.U_SKIN8 | Defines.U_SKIN16;
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

		// event is not delta compressed, just 0 compressed
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

		//
		// write the message
		//
		if (bits == 0 && !force)
			return; // nothing to send!

		//----------

		if ((bits & 0xff000000) != 0)
			bits |= Defines.U_MOREBITS3 | Defines.U_MOREBITS2 | Defines.U_MOREBITS1;
		else if ((bits & 0x00ff0000) != 0)
			bits |= Defines.U_MOREBITS2 | Defines.U_MOREBITS1;
		else if ((bits & 0x0000ff00) != 0)
			bits |= Defines.U_MOREBITS1;

		MSG.WriteByte(msg, bits & 255);

		if ((bits & 0xff000000) != 0)
		{
			MSG.WriteByte(msg, ((uint)bits >> 8) & 255);
			MSG.WriteByte(msg, ((uint)bits >> 16) & 255);
			MSG.WriteByte(msg, ((uint)bits >> 24) & 255);
		}
		else if ((bits & 0x00ff0000) != 0)
		{
			MSG.WriteByte(msg, ((uint)bits >> 8) & 255);
			MSG.WriteByte(msg, ((uint)bits >> 16) & 255);
		}
		else if ((bits & 0x0000ff00) != 0)
			MSG.WriteByte(msg, ((uint)bits >> 8) & 255);

		//----------

		if ((bits & Defines.U_NUMBER16) != 0)
			MSG.WriteShort(msg, to.number);
		else
			MSG.WriteByte(msg, to.number);

		if ((bits & Defines.U_MODEL) != 0)
			MSG.WriteByte(msg, to.modelindex);

		if ((bits & Defines.U_MODEL2) != 0)
			MSG.WriteByte(msg, to.modelindex2);

		if ((bits & Defines.U_MODEL3) != 0)
			MSG.WriteByte(msg, to.modelindex3);

		if ((bits & Defines.U_MODEL4) != 0)
			MSG.WriteByte(msg, to.modelindex4);

		if ((bits & Defines.U_FRAME8) != 0)
			MSG.WriteByte(msg, to.frame);

		if ((bits & Defines.U_FRAME16) != 0)
			MSG.WriteShort(msg, to.frame);

		if ((bits & Defines.U_SKIN8) != 0 && (bits & Defines.U_SKIN16) != 0) //used for laser
			// colors
			MSG.WriteInt(msg, to.skinnum);
		else if ((bits & Defines.U_SKIN8) != 0)
			MSG.WriteByte(msg, to.skinnum);
		else if ((bits & Defines.U_SKIN16) != 0)
			MSG.WriteShort(msg, to.skinnum);

		if ((bits & (Defines.U_EFFECTS8 | Defines.U_EFFECTS16)) == (Defines.U_EFFECTS8 | Defines.U_EFFECTS16))
			MSG.WriteInt(msg, to.effects);
		else if ((bits & Defines.U_EFFECTS8) != 0)
			MSG.WriteByte(msg, to.effects);
		else if ((bits & Defines.U_EFFECTS16) != 0)
			MSG.WriteShort(msg, to.effects);

		if ((bits & (Defines.U_RENDERFX8 | Defines.U_RENDERFX16)) == (Defines.U_RENDERFX8 | Defines.U_RENDERFX16))
			MSG.WriteInt(msg, to.renderfx);
		else if ((bits & Defines.U_RENDERFX8) != 0)
			MSG.WriteByte(msg, to.renderfx);
		else if ((bits & Defines.U_RENDERFX16) != 0)
			MSG.WriteShort(msg, to.renderfx);

		if ((bits & Defines.U_ORIGIN1) != 0)
			MSG.WriteCoord(msg, to.origin[0]);

		if ((bits & Defines.U_ORIGIN2) != 0)
			MSG.WriteCoord(msg, to.origin[1]);

		if ((bits & Defines.U_ORIGIN3) != 0)
			MSG.WriteCoord(msg, to.origin[2]);

		if ((bits & Defines.U_ANGLE1) != 0)
			MSG.WriteAngle(msg, to.angles[0]);

		if ((bits & Defines.U_ANGLE2) != 0)
			MSG.WriteAngle(msg, to.angles[1]);

		if ((bits & Defines.U_ANGLE3) != 0)
			MSG.WriteAngle(msg, to.angles[2]);

		if ((bits & Defines.U_OLDORIGIN) != 0)
		{
			MSG.WriteCoord(msg, to.old_origin[0]);
			MSG.WriteCoord(msg, to.old_origin[1]);
			MSG.WriteCoord(msg, to.old_origin[2]);
		}

		if ((bits & Defines.U_SOUND) != 0)
			MSG.WriteByte(msg, to.sound);

		if ((bits & Defines.U_EVENT) != 0)
			MSG.WriteByte(msg, to.@event);

		if ((bits & Defines.U_SOLID) != 0)
			MSG.WriteShort(msg, to.solid);
	}

	//============================================================

	//
	// reading functions
	//

	public static void BeginReading(sizebuf_t msg)
	{
		msg.readcount = 0;
	}

	// returns -1 if no more characters are available, but also [-128 , 127]
	public static int ReadChar(sizebuf_t msg_read)
	{
		int c;

		if (msg_read.readcount + 1 > msg_read.cursize)
			c = -1;
		else
			c = (sbyte)msg_read.data[msg_read.readcount];

		msg_read.readcount++;

		// kickangles bugfix (rst)
		return c;
	}

	public static int ReadByte(sizebuf_t msg_read)
	{
		int c;

		if (msg_read.readcount + 1 > msg_read.cursize)
			c = -1;
		else
			c = msg_read.data[msg_read.readcount] & 0xff;

		msg_read.readcount++;

		return c;
	}

	public static short ReadShort(sizebuf_t msg_read)
	{
		int c;

		if (msg_read.readcount + 2 > msg_read.cursize)
			c = -1;
		else
			c = (short)((msg_read.data[msg_read.readcount] & 0xff) + (msg_read.data[msg_read.readcount + 1] << 8));

		msg_read.readcount += 2;

		return (short)c;
	}

	public static int ReadLong(sizebuf_t msg_read)
	{
		int c;

		if (msg_read.readcount + 4 > msg_read.cursize)
		{
			clientserver.Com_Printf("buffer underrun in ReadLong!");
			c = -1;
		}

		else
		{
			c = (msg_read.data[msg_read.readcount] & 0xff)
				| ((msg_read.data[msg_read.readcount + 1] & 0xff) << 8)
				| ((msg_read.data[msg_read.readcount + 2] & 0xff) << 16)
				| ((msg_read.data[msg_read.readcount + 3] & 0xff) << 24);
		}

		msg_read.readcount += 4;

		return c;
	}

	public static float ReadFloat(sizebuf_t msg_read)
	{
		var n = MSG.ReadLong(msg_read);

		return BitConverter.ToSingle(BitConverter.GetBytes(n), 0);
	}

	// 2k read buffer.
	public static byte[] readbuf = new byte[2048];

	public static string ReadString(sizebuf_t msg_read)
	{
		int c;
		var l = 0;

		do
		{
			c = MSG.ReadByte(msg_read);

			if (c == -1 || c == 0)
				break;

			MSG.readbuf[l] = (byte)c;
			l++;
		}
		while (l < 2047);

		var ret = Encoding.ASCII.GetString(MSG.readbuf, 0, l);

		// Com.dprintln("MSG.ReadString:[" + ret + "]");
		return ret;
	}

	public static string ReadStringLine(sizebuf_t msg_read)
	{
		int l;
		byte c;

		l = 0;

		do
		{
			c = (byte)MSG.ReadChar(msg_read);

			if (c == 0xff || c == 0x00 || c == 0x0a)
				break;

			MSG.readbuf[l] = c;
			l++;
		}
		while (l < 2047);

		var ret = Encoding.ASCII.GetString(MSG.readbuf, 0, l).Trim();
		Com.dprintln("MSG.ReadStringLine:[" + ret.Replace('\0', '@') + "]");

		return ret;
	}

	public static float ReadCoord(sizebuf_t msg_read)
	{
		return MSG.ReadShort(msg_read) * (1.0f / 8);
	}

	public static Vector3 ReadPos(sizebuf_t msg_read)
	{
		var pos = new float[3];
		MSG.ReadPos(msg_read, pos);

		return new(pos[0], pos[1], pos[2]);
	}

	public static void ReadPos(sizebuf_t msg_read, float[] pos)
	{
		if (!(pos.Length == 3))
			throw new("vec3_t bug");

		pos[0] = MSG.ReadShort(msg_read) * (1.0f / 8);
		pos[1] = MSG.ReadShort(msg_read) * (1.0f / 8);
		pos[2] = MSG.ReadShort(msg_read) * (1.0f / 8);
	}

	public static float ReadAngle(sizebuf_t msg_read)
	{
		return MSG.ReadChar(msg_read) * (360.0f / 256);
	}

	public static float ReadAngle16(sizebuf_t msg_read)
	{
		return Math3D.SHORT2ANGLE(MSG.ReadShort(msg_read));
	}

	public static void ReadDeltaUsercmd(sizebuf_t msg_read, usercmd_t from, usercmd_t move)
	{
		int bits;

		//memcpy(move, from, sizeof(* move));
		// IMPORTANT!! copy without new
		move.set(from);
		bits = MSG.ReadByte(msg_read);

		// read current angles
		if ((bits & Defines.CM_ANGLE1) != 0)
			move.angles[0] = MSG.ReadShort(msg_read);

		if ((bits & Defines.CM_ANGLE2) != 0)
			move.angles[1] = MSG.ReadShort(msg_read);

		if ((bits & Defines.CM_ANGLE3) != 0)
			move.angles[2] = MSG.ReadShort(msg_read);

		// read movement
		if ((bits & Defines.CM_FORWARD) != 0)
			move.forwardmove = MSG.ReadShort(msg_read);

		if ((bits & Defines.CM_SIDE) != 0)
			move.sidemove = MSG.ReadShort(msg_read);

		if ((bits & Defines.CM_UP) != 0)
			move.upmove = MSG.ReadShort(msg_read);

		// read buttons
		if ((bits & Defines.CM_BUTTONS) != 0)
			move.buttons = (byte)MSG.ReadByte(msg_read);

		if ((bits & Defines.CM_IMPULSE) != 0)
			move.impulse = (byte)MSG.ReadByte(msg_read);

		// read time to run command
		move.msec = (byte)MSG.ReadByte(msg_read);

		// read the light level
		move.lightlevel = (byte)MSG.ReadByte(msg_read);
	}

	public static void ReadData(sizebuf_t msg_read, byte[] data, int len)
	{
		for (var i = 0; i < len; i++)
			data[i] = (byte)MSG.ReadByte(msg_read);
	}
}
