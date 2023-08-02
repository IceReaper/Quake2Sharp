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

public class daliasframe_t
{
	public float[] scale = { 0, 0, 0 }; // multiply byte verts by this
	public float[] translate = { 0, 0, 0 }; // then add this
	public string name; // frame name from grabbing (size 16)
	public int[] verts; // variable sized

	public daliasframe_t(BinaryReader b)
	{
		this.scale[0] = b.ReadSingle();
		this.scale[1] = b.ReadSingle();
		this.scale[2] = b.ReadSingle();
		this.translate[0] = b.ReadSingle();
		this.translate[1] = b.ReadSingle();
		this.translate[2] = b.ReadSingle();
		var nameBuf = new byte[16];
		b.Read(nameBuf);
		this.name = Encoding.ASCII.GetString(nameBuf).Split((char)0)[0];
	}
}