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
using qcommon.types;
using System;
using util;

public class model_t
{
	public string name = "";
	public int registration_sequence;

	// was enum modtype_t
	public int type;
	public int numframes;
	public int flags;

	//
	// volume occupied by the model graphics
	//		
	public float[] mins = { 0, 0, 0 }, maxs = { 0, 0, 0 };
	public float radius;

	//
	// solid volume for clipping 
	//
	public bool clipbox;
	public float[] clipmins = { 0, 0, 0 }, clipmaxs = { 0, 0, 0 };

	//
	// brush model
	//
	public int firstmodelsurface, nummodelsurfaces;
	public int lightmap; // only for submodels
	public int numsubmodels;
	public mmodel_t[] submodels;
	public int numplanes;
	public cplane_t[] planes;
	public int numleafs; // number of visible leafs, not counting 0
	public mleaf_t[] leafs;
	public int numvertexes;
	public mvertex_t[] vertexes;
	public int numedges;
	public medge_t[] edges;
	public int numnodes;
	public int firstnode;
	public mnode_t[] nodes;
	public int numtexinfo;
	public mtexinfo_t[] texinfo;
	public int numsurfaces;
	public msurface_t[] surfaces;
	public int numsurfedges;
	public int[] surfedges;
	public int nummarksurfaces;
	public msurface_t[] marksurfaces;
	public dvis_t vis;
	public byte[] lightdata;

	// for alias models and skins
	// was image_t *skins[]; (array of pointers)
	public image_t[] skins = new image_t[Defines.MAX_MD2SKINS];
	public int extradatasize;

	// or whatever
	public object extradata;

	public void clear()
	{
		this.name = "";
		this.registration_sequence = 0;

		// was enum modtype_t
		this.type = 0;
		this.numframes = 0;
		this.flags = 0;

		//
		// volume occupied by the model graphics
		//		
		Math3D.VectorClear(this.mins);
		Math3D.VectorClear(this.maxs);
		this.radius = 0;

		//
		// solid volume for clipping 
		//
		this.clipbox = false;
		Math3D.VectorClear(this.clipmins);
		Math3D.VectorClear(this.clipmaxs);

		//
		// brush model
		//
		this.firstmodelsurface = this.nummodelsurfaces = 0;
		this.lightmap = 0; // only for submodels

		this.numsubmodels = 0;
		this.submodels = null;

		this.numplanes = 0;
		this.planes = null;

		this.numleafs = 0; // number of visible leafs, not counting 0
		this.leafs = null;

		this.numvertexes = 0;
		this.vertexes = null;

		this.numedges = 0;
		this.edges = null;

		this.numnodes = 0;
		this.firstnode = 0;
		this.nodes = null;

		this.numtexinfo = 0;
		this.texinfo = null;

		this.numsurfaces = 0;
		this.surfaces = null;

		this.numsurfedges = 0;
		this.surfedges = null;

		this.nummarksurfaces = 0;
		this.marksurfaces = null;

		this.vis = null;

		this.lightdata = null;

		// for alias models and skins
		// was image_t *skins[]; (array of pointers)
		Array.Fill(this.skins, null);

		this.extradatasize = 0;

		// or whatever
		this.extradata = null;
	}

	public model_t copy()
	{
		model_t theClone = null;

		try
		{
			theClone = (model_t)this.MemberwiseClone();
			theClone.mins = Lib.clone(this.mins);
			theClone.maxs = Lib.clone(this.maxs);
			theClone.clipmins = Lib.clone(this.clipmins);
			theClone.clipmaxs = Lib.clone(this.clipmaxs);
		}
		catch (Exception)
		{
		}

		return theClone;
	}
}