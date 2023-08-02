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
namespace QClient.util;

using game;
using game.types;
using qcommon;
using System.Text;

public static class QuakeFile
{
	/** Writes a Vector to a RandomAccessFile. */
	public static void Write(this BinaryWriter binaryWriter, float[] v)
	{
		for (var n = 0; n < 3; n++)
			binaryWriter.Write(v[n]);
	}

	/** Writes a Vector to a RandomAccessFile. */
	public static float[] ReadVector(this BinaryReader binaryReader)
	{
		float[] res = { 0, 0, 0 };

		for (var n = 0; n < 3; n++)
			res[n] = binaryReader.ReadSingle();

		return res;
	}

	/** Reads a length specified string from a file. */
	public static string ReadStringQ(this BinaryReader binaryReader)
	{
		var len = binaryReader.ReadInt32();

		if (len == -1)
			return null;

		if (len == 0)
			return "";

		var bb = new byte[len];

		binaryReader.Read(bb, 0, len);

		return Encoding.ASCII.GetString(bb, 0, len);
	}

	/** Writes a length specified string to a file. */
	public static void WriteQ(this BinaryWriter binaryWriter, string s)
	{
		if (s == null)
		{
			binaryWriter.Write(-1);

			return;
		}

		binaryWriter.Write(s.Length);

		if (s.Length != 0)
			binaryWriter.Write(Encoding.ASCII.GetBytes(s));
	}

	/** Writes the edict reference. */
	public static void Write(this BinaryWriter binaryWriter, edict_t ent)
	{
		if (ent == null)
			binaryWriter.Write(-1);
		else
			binaryWriter.Write(ent.s.number);
	}

	/**
     * Reads an edict index from a file and returns the edict.
     */
	public static edict_t ReadEdictRef(this BinaryReader binaryReader)
	{
		var i = binaryReader.ReadInt32();

		// handle -1
		if (i < 0)
			return null;

		if (i > GameBase.g_edicts.Length)
		{
			Com.DPrintf("illegal edict num:" + i + "\n");

			return null;
		}

		// valid edict.
		return GameBase.g_edicts[i];
	}

	/** Writes the Adapter-ID to the file. */
	public static void Write(this BinaryWriter binaryWriter, SuperAdapter a)
	{
		binaryWriter.Write(3988);

		if (a == null)
			binaryWriter.WriteQ(null);
		else
		{
			var str = a.getID();

			if (a == null)
				Com.DPrintf("writeAdapter: invalid Adapter id for " + a + "\n");

			binaryWriter.WriteQ(str);
		}
	}

	/** Reads the adapter id and returns the adapter. */
	public static SuperAdapter ReadAdapter(this BinaryReader binaryReader)
	{
		if (binaryReader.ReadInt32() != 3988)
			Com.DPrintf("wrong read position: readadapter 3988 \n");

		var id = binaryReader.ReadStringQ();

		if (id == null)
		{
			// null adapter. :-)
			return null;
		}

		return SuperAdapter.getFromID(id);
	}

	/** Writes an item reference. */
	public static void Write(this BinaryWriter binaryWriter, gitem_t item)
	{
		if (item == null)
			binaryWriter.Write(-1);
		else
			binaryWriter.Write(item.index);
	}

	/** Reads the item index and returns the game item. */
	public static gitem_t ReadItem(this BinaryReader binaryReader)
	{
		var ndx = binaryReader.ReadInt32();

		if (ndx == -1)
			return null;

		return GameItemList.itemlist[ndx];
	}
}