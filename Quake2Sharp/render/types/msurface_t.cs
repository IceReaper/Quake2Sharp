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
namespace Quake2Sharp.render.types;

using game.types;

public class msurface_t
{
	public int visframe; // should be drawn when node is crossed
	public cplane_t plane;
	public int flags;
	public int firstedge; // look up in model->surfedges[], negative numbers
	public int numedges; // are backwards edges
	public short[] texturemins = { 0, 0 };
	public short[] extents = { 0, 0 };
	public int light_s, light_t; // gl lightmap coordinates
	public int dlight_s, dlight_t;

	// gl lightmap coordinates for dynamic lightmaps
	public glpoly_t polys; // multiple if warped
	public msurface_t texturechain;
	public msurface_t lightmapchain;
	public mtexinfo_t texinfo = new();

	// lighting info
	public int dlightframe;
	public int dlightbits;
	public int lightmaptexturenum;
	public byte[] styles = new byte[Defines.MAXLIGHTMAPS];
	public float[] cached_light = new float[Defines.MAXLIGHTMAPS];

	// values currently used in lightmap
	//public byte samples[]; // [numstyles*surfsize]
	public byte[] samples; // [numstyles*surfsize]
	public int samplesOffset;

	public void clear()
	{
		this.visframe = 0;
		this.plane.clear();
		this.flags = 0;

		this.firstedge = 0;
		this.numedges = 0;

		this.texturemins[0] = this.texturemins[1] = -1;
		this.extents[0] = this.extents[1] = 0;

		this.light_s = this.light_t = 0;
		this.dlight_s = this.dlight_t = 0;

		this.polys = null;
		this.texturechain = null;
		this.lightmapchain = null;

		//texinfo = new mtexinfo_t();
		this.texinfo.clear();

		this.dlightframe = 0;
		this.dlightbits = 0;

		this.lightmaptexturenum = 0;

		for (var i = 0; i < this.styles.Length; i++)
			this.styles[i] = 0;

		for (var i = 0; i < this.cached_light.Length; i++)
			this.cached_light[i] = 0;

		this.samples = null;
		this.samplesOffset = 0;
	}
}