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
namespace Quake2Sharp.render.opengl.types;

using render.types;

public class gllightmapstate_t
{
	public int internal_format;
	public int current_lightmap_texture;
	public readonly msurface_t[] lightmap_surfaces = new msurface_t[OpenGLRenderApi.MAX_LIGHTMAPS];
	public readonly int[] allocated = new int[OpenGLRenderApi.BLOCK_WIDTH];

	// the lightmap texture data needs to be kept in
	// main memory so texsubimage can update properly
	public readonly int[] lightmap_buffer = new int[OpenGLRenderApi.BLOCK_WIDTH * OpenGLRenderApi.BLOCK_HEIGHT];

	public gllightmapstate_t()
	{
		for (var i = 0; i < OpenGLRenderApi.MAX_LIGHTMAPS; i++)
			this.lightmap_surfaces[i] = new();
	}

	public void clearLightmapSurfaces()
	{
		for (var i = 0; i < OpenGLRenderApi.MAX_LIGHTMAPS; i++)
			this.lightmap_surfaces[i] = new();
	}
}