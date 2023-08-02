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
namespace QClient.qcommon.types;

using System.Text;
using util;

public class texinfo_t
{
	// works fine.
	public texinfo_t(byte[] cmod_base, int o, int len)
		: this(new(new MemoryStream(cmod_base, o, len)))
	{
	}

	public texinfo_t(BinaryReader bb)
	{
		var str = new byte[32];

		this.vecs[0] = new float[] { bb.ReadSingle(), bb.ReadSingle(), bb.ReadSingle(), bb.ReadSingle() };
		this.vecs[1] = new float[] { bb.ReadSingle(), bb.ReadSingle(), bb.ReadSingle(), bb.ReadSingle() };

		this.flags = bb.ReadInt32();
		this.value = bb.ReadInt32();

		bb.Read(str);
		this.texture = Encoding.ASCII.GetString(str, 0, Lib.strlen(str));
		this.nexttexinfo = bb.ReadInt32();
	}

	public static readonly int SIZE = 32 + 4 + 4 + 32 + 4;

	//float			vecs[2][4];		// [s/t][xyz offset]
	public float[][] vecs = { new float[] { 0, 0, 0, 0 }, new float[] { 0, 0, 0, 0 } };
	public int flags; // miptex flags + overrides
	public int value; // light emission, etc

	//char			texture[32];	// texture name (textures/*.wal)
	public string texture = "";
	public int nexttexinfo; // for animations, -1 = end of chain
}