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
namespace Quake2Sharp.qcommon.types;

using System.IO;

public class dmdl_t
{
	public int ident;
	public int version;
	public int skinwidth;
	public int skinheight;
	public int framesize; // byte size of each frame
	public int num_skins;
	public int num_xyz;
	public int num_st; // greater than num_xyz for seams
	public int num_tris;
	public int num_glcmds; // dwords in strip/fan command list
	public int num_frames;
	public int ofs_skins; // each skin is a MAX_SKINNAME string
	public int ofs_st; // byte offset from start for stverts
	public int ofs_tris; // offset for dtriangles
	public int ofs_frames; // offset for first frame
	public int ofs_glcmds;
	public int ofs_end; // end of file
	public string[] skinNames;
	public dstvert_t[] stVerts;
	public dtriangle_t[] triAngles;
	public int[] glCmds;
	public daliasframe_t[] aliasFrames;

	public dmdl_t(BinaryReader b)
	{
		this.ident = b.ReadInt32();
		this.version = b.ReadInt32();
		this.skinwidth = b.ReadInt32();
		this.skinheight = b.ReadInt32();
		this.framesize = b.ReadInt32(); // byte size of each frame
		this.num_skins = b.ReadInt32();
		this.num_xyz = b.ReadInt32();
		this.num_st = b.ReadInt32(); // greater than num_xyz for seams
		this.num_tris = b.ReadInt32();
		this.num_glcmds = b.ReadInt32(); // dwords in strip/fan command list
		this.num_frames = b.ReadInt32();
		this.ofs_skins = b.ReadInt32(); // each skin is a MAX_SKINNAME string
		this.ofs_st = b.ReadInt32(); // byte offset from start for stverts
		this.ofs_tris = b.ReadInt32(); // offset for dtriangles
		this.ofs_frames = b.ReadInt32(); // offset for first frame
		this.ofs_glcmds = b.ReadInt32();
		this.ofs_end = b.ReadInt32(); // end of file
	}

	/*
	 * new members for vertex array handling
	 */
	public float[] textureCoordBuf = null;
	public int[] vertexIndexBuf = null;
	public int[] counts = null;
	public int[][] indexElements = null;
}