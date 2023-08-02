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
namespace QClient.render.opengl;

using client;
using game.types;
using qcommon;
using qcommon.types;
using render.types;
using System.Text;
using util;

public partial class OpenGLRenderApi
{
	// models.c -- model loading and caching
	protected model_t loadmodel;
	private int modfilelen;
	private readonly byte[] mod_novis = new byte[Defines.MAX_MAP_LEAFS / 8];
	private static readonly int MAX_MOD_KNOWN = 512;
	private readonly model_t[] mod_known = new model_t[OpenGLRenderApi.MAX_MOD_KNOWN];
	private int mod_numknown;

	// the inline * models from the current map are kept seperate
	private readonly model_t[] mod_inline = new model_t[OpenGLRenderApi.MAX_MOD_KNOWN];

	/*
	===============
	Mod_PointInLeaf
	===============
	*/
	protected mleaf_t Mod_PointInLeaf(float[] p, model_t model)
	{
		mnode_t node;
		float d;
		cplane_t plane;

		if (model == null || model.nodes == null)
			Com.Error(Defines.ERR_DROP, "Mod_PointInLeaf: bad model");

		node = model.nodes[0]; // root node

		while (true)
		{
			if (node.contents != -1)
				return (mleaf_t)node;

			plane = node.plane;
			d = Math3D.DotProduct(p, plane.normal) - plane.dist;

			if (d > 0)
				node = node.children[0];
			else
				node = node.children[1];
		}

		// never reached
	}

	private readonly byte[] decompressed = new byte[Defines.MAX_MAP_LEAFS / 8];
	private readonly byte[] model_visibility = new byte[Defines.MAX_MAP_VISIBILITY];

	/*
	===================
	Mod_DecompressVis
	===================
	*/
	private byte[] Mod_DecompressVis(byte[] @in, int offset, model_t model)
	{
		int c;
		byte[] @out;
		int outp, inp;
		int row;

		row = (model.vis.numclusters + 7) >> 3;
		@out = this.decompressed;
		outp = 0;
		inp = offset;

		if (@in == null)
		{
			// no vis info, so make all visible
			while (row != 0)
			{
				@out[outp++] = (byte)0xFF;
				row--;
			}

			return this.decompressed;
		}

		do
		{
			if (@in[inp] != 0)
			{
				@out[outp++] = @in[inp++];

				continue;
			}

			c = @in[inp + 1] & 0xFF;
			inp += 2;

			while (c != 0)
			{
				@out[outp++] = 0;
				c--;
			}
		}
		while (outp < row);

		return this.decompressed;
	}

	/*
	==============
	Mod_ClusterPVS
	==============
	*/
	protected byte[] Mod_ClusterPVS(int cluster, model_t model)
	{
		if (cluster == -1 || model.vis == null)
			return this.mod_novis;

		//return Mod_DecompressVis( (byte *)model.vis + model.vis.bitofs[cluster][Defines.DVIS_PVS], model);
		return this.Mod_DecompressVis(this.model_visibility, model.vis.bitofs[cluster][Defines.DVIS_PVS], model);
	}

	//	  ===============================================================================

	/*
	================
	Mod_Modellist_f
	================
	*/
	protected void Mod_Modellist_f()
	{
		int i;
		model_t mod;
		int total;

		total = 0;
		VID.Printf(Defines.PRINT_ALL, "Loaded models:\n");

		for (i = 0; i < this.mod_numknown; i++)
		{
			mod = this.mod_known[i];

			if (mod.name.Length == 0)
				continue;

			VID.Printf(Defines.PRINT_ALL, $"{mod.extradatasize,8} : {mod.name}\n");
			total += mod.extradatasize;
		}

		VID.Printf(Defines.PRINT_ALL, "Total resident: " + total + '\n');
	}

	/*
	===============
	Mod_Init
	===============
	*/
	protected void Mod_Init()
	{
		// init mod_known
		for (var i = 0; i < OpenGLRenderApi.MAX_MOD_KNOWN; i++)
			this.mod_known[i] = new();

		Array.Fill(this.mod_novis, (byte)0xff);
	}

	private byte[] fileBuffer;

	/*
	==================
	Mod_ForName

	Loads in a model for the given name
	==================
	*/
	private model_t Mod_ForName(string name, bool crash)
	{
		model_t mod = null;
		int i;

		if (name == null || name.Length == 0)
			Com.Error(Defines.ERR_DROP, "Mod_ForName: NULL name");

		//
		// inline models are grabbed only from worldmodel
		//
		if (name[0] == '*')
		{
			i = int.Parse(name[1..]);

			if (i < 1 || this.r_worldmodel == null || i >= this.r_worldmodel.numsubmodels)
				Com.Error(Defines.ERR_DROP, "bad inline model number");

			return this.mod_inline[i];
		}

		//
		// search the currently loaded models
		//
		for (i = 0; i < this.mod_numknown; i++)
		{
			mod = this.mod_known[i];

			if (mod.name.Length == 0)
				continue;

			if (mod.name.Equals(name))
				return mod;
		}

		//
		// find a free model slot spot
		//
		for (i = 0; i < this.mod_numknown; i++)
		{
			mod = this.mod_known[i];

			if (mod.name.Length == 0)
				break; // free spot
		}

		if (i == this.mod_numknown)
		{
			if (this.mod_numknown == OpenGLRenderApi.MAX_MOD_KNOWN)
				Com.Error(Defines.ERR_DROP, "mod_numknown == MAX_MOD_KNOWN");

			this.mod_numknown++;
			mod = this.mod_known[i];
		}

		mod.name = name;

		//
		// load the file
		//
		this.fileBuffer = FS.LoadFile(name);

		if (this.fileBuffer == null)
		{
			if (crash)
				Com.Error(Defines.ERR_DROP, "Mod_NumForName: " + mod.name + " not found");

			mod.name = "";

			return null;
		}

		this.modfilelen = this.fileBuffer.Length;

		this.loadmodel = mod;

		//
		// fill it in
		//
		BinaryReader bb = new(new MemoryStream(this.fileBuffer));

		// call the apropriate loader

		var ident = bb.ReadInt32();
		bb.BaseStream.Position = 0;

		switch (ident)
		{
			case qfiles.IDALIASHEADER:
				this.Mod_LoadAliasModel(mod, bb);

				break;

			case qfiles.IDSPRITEHEADER:
				this.Mod_LoadSpriteModel(mod, bb);

				break;

			case qfiles.IDBSPHEADER:
				this.Mod_LoadBrushModel(mod, bb);

				break;

			default:
				Com.Error(Defines.ERR_DROP, "Mod_NumForName: unknown fileid for " + mod.name);

				break;
		}

		this.fileBuffer = null; // free it for garbage collection

		return mod;
	}

	/*
	===============================================================================

						BRUSHMODEL LOADING

	===============================================================================
	*/
	private byte[] mod_base;

	/*
	=================
	Mod_LoadLighting
	=================
	*/
	private void Mod_LoadLighting(lump_t l)
	{
		if (l.filelen == 0)
		{
			this.loadmodel.lightdata = null;

			return;
		}

		// memcpy (loadmodel.lightdata, mod_base + l.fileofs, l.filelen);
		this.loadmodel.lightdata = new byte[l.filelen];
		Array.Copy(this.mod_base, l.fileofs, this.loadmodel.lightdata, 0, l.filelen);
	}

	/*
	=================
	Mod_LoadVisibility
	=================
	*/
	private void Mod_LoadVisibility(lump_t l)
	{
		if (l.filelen == 0)
		{
			this.loadmodel.vis = null;

			return;
		}

		Array.Copy(this.mod_base, l.fileofs, this.model_visibility, 0, l.filelen);

		BinaryReader bb = new(new MemoryStream(this.model_visibility, 0, l.filelen));

		this.loadmodel.vis = new(bb);

		/* done:
		memcpy (loadmodel.vis, mod_base + l.fileofs, l.filelen);

		loadmodel.vis.numclusters = LittleLong (loadmodel.vis.numclusters);
		for (i=0 ; i<loadmodel.vis.numclusters ; i++)
		{
			loadmodel.vis.bitofs[i][0] = LittleLong (loadmodel.vis.bitofs[i][0]);
			loadmodel.vis.bitofs[i][1] = LittleLong (loadmodel.vis.bitofs[i][1]);
		}
		*/
	}

	/*
	=================
	Mod_LoadVertexes
	=================
	*/
	private void Mod_LoadVertexes(lump_t l)
	{
		mvertex_t[] vertexes;
		int i, count;

		if (l.filelen % mvertex_t.DISK_SIZE != 0)
			Com.Error(Defines.ERR_DROP, "MOD_LoadBmodel: funny lump size in " + this.loadmodel.name);

		count = l.filelen / mvertex_t.DISK_SIZE;

		vertexes = new mvertex_t[count];

		this.loadmodel.vertexes = vertexes;
		this.loadmodel.numvertexes = count;

		BinaryReader bb = new(new MemoryStream(this.mod_base, l.fileofs, l.filelen));

		for (i = 0; i < count; i++)
			vertexes[i] = new(bb);
	}

	/*
	=================
	RadiusFromBounds
	=================
	*/
	private float RadiusFromBounds(float[] mins, float[] maxs)
	{
		float[] corner = { 0, 0, 0 };

		for (var i = 0; i < 3; i++)
			corner[i] = Math.Abs(mins[i]) > Math.Abs(maxs[i]) ? Math.Abs(mins[i]) : Math.Abs(maxs[i]);

		return Math3D.VectorLength(corner);
	}

	/*
	=================
	Mod_LoadSubmodels
	=================
	*/
	private void Mod_LoadSubmodels(lump_t l)
	{
		dmodel_t @in;
		mmodel_t[] @out;
		int i, j, count;

		if (l.filelen % dmodel_t.SIZE != 0)
			Com.Error(Defines.ERR_DROP, "MOD_LoadBmodel: funny lump size in " + this.loadmodel.name);

		count = l.filelen / dmodel_t.SIZE;

		// out = Hunk_Alloc ( count*sizeof(*out));
		@out = new mmodel_t[count];

		this.loadmodel.submodels = @out;
		this.loadmodel.numsubmodels = count;

		BinaryReader bb = new(new MemoryStream(this.mod_base, l.fileofs, l.filelen));

		for (i = 0; i < count; i++)
		{
			@in = new(bb);
			@out[i] = new();

			for (j = 0; j < 3; j++)
			{
				// spread the mins / maxs by a pixel
				@out[i].mins[j] = @in.mins[j] - 1;
				@out[i].maxs[j] = @in.maxs[j] + 1;
				@out[i].origin[j] = @in.origin[j];
			}

			@out[i].radius = this.RadiusFromBounds(@out[i].mins, @out[i].maxs);
			@out[i].headnode = @in.headnode;
			@out[i].firstface = @in.firstface;
			@out[i].numfaces = @in.numfaces;
		}
	}

	/*
	=================
	Mod_LoadEdges
	=================
	*/
	private void Mod_LoadEdges(lump_t l)
	{
		medge_t[] edges;
		int i, count;

		if (l.filelen % medge_t.DISK_SIZE != 0)
			Com.Error(Defines.ERR_DROP, "MOD_LoadBmodel: funny lump size in " + this.loadmodel.name);

		count = l.filelen / medge_t.DISK_SIZE;

		// out = Hunk_Alloc ( (count + 1) * sizeof(*out));
		edges = new medge_t[count + 1];

		this.loadmodel.edges = edges;
		this.loadmodel.numedges = count;

		BinaryReader bb = new(new MemoryStream(this.mod_base, l.fileofs, l.filelen));

		for (i = 0; i < count; i++)
			edges[i] = new(bb);
	}

	/*
	=================
	Mod_LoadTexinfo
	=================
	*/
	private void Mod_LoadTexinfo(lump_t l)
	{
		texinfo_t @in;
		mtexinfo_t[] @out;
		mtexinfo_t step;
		int i, count;
		int next;
		string name;

		if (l.filelen % texinfo_t.SIZE != 0)
			Com.Error(Defines.ERR_DROP, "MOD_LoadBmodel: funny lump size in " + this.loadmodel.name);

		count = l.filelen / texinfo_t.SIZE;

		// out = Hunk_Alloc ( count*sizeof(*out));
		@out = new mtexinfo_t[count];

		for (i = 0; i < count; i++)
			@out[i] = new();

		this.loadmodel.texinfo = @out;
		this.loadmodel.numtexinfo = count;

		BinaryReader bb = new(new MemoryStream(this.mod_base, l.fileofs, l.filelen));

		for (i = 0; i < count; i++)
		{
			@in = new(bb);
			@out[i].vecs = @in.vecs;
			@out[i].flags = @in.flags;
			next = @in.nexttexinfo;

			if (next > 0)
				@out[i].next = this.loadmodel.texinfo[next];
			else
				@out[i].next = null;

			name = "textures/" + @in.texture + ".wal";

			@out[i].image = this.GL_FindImage(name, Base.it_wall);

			if (@out[i].image == null)
			{
				VID.Printf(Defines.PRINT_ALL, "Couldn't load " + name + '\n');
				@out[i].image = this.r_notexture;
			}
		}

		// count animation frames
		for (i = 0; i < count; i++)
		{
			@out[i].numframes = 1;

			for (step = @out[i].next; step != null && step != @out[i]; step = step.next)
				@out[i].numframes++;
		}
	}

	/*
	================
	CalcSurfaceExtents

	Fills in s.texturemins[] and s.extents[]
	================
	*/
	private void CalcSurfaceExtents(msurface_t s)
	{
		float[] mins = { 0, 0 };
		float[] maxs = { 0, 0 };
		float val;

		int j, e;
		mvertex_t v;
		int[] bmins = { 0, 0 };
		int[] bmaxs = { 0, 0 };

		mins[0] = mins[1] = 999999;
		maxs[0] = maxs[1] = -99999;

		var tex = s.texinfo;

		for (var i = 0; i < s.numedges; i++)
		{
			e = this.loadmodel.surfedges[s.firstedge + i];

			if (e >= 0)
				v = this.loadmodel.vertexes[this.loadmodel.edges[e].v[0]];
			else
				v = this.loadmodel.vertexes[this.loadmodel.edges[-e].v[1]];

			for (j = 0; j < 2; j++)
			{
				val = v.position[0] * tex.vecs[j][0] + v.position[1] * tex.vecs[j][1] + v.position[2] * tex.vecs[j][2] + tex.vecs[j][3];

				if (val < mins[j])
					mins[j] = val;

				if (val > maxs[j])
					maxs[j] = val;
			}
		}

		for (var i = 0; i < 2; i++)
		{
			bmins[i] = (int)Math.Floor(mins[i] / 16);
			bmaxs[i] = (int)Math.Ceiling(maxs[i] / 16);

			s.texturemins[i] = (short)(bmins[i] * 16);
			s.extents[i] = (short)((bmaxs[i] - bmins[i]) * 16);
		}
	}

	/*
	=================
	Mod_LoadFaces
	=================
	*/
	private void Mod_LoadFaces(lump_t l)
	{
		dface_t @in;
		msurface_t[] @out;
		int i, count, surfnum;
		int planenum, side;
		int ti;

		if (l.filelen % dface_t.SIZE != 0)
			Com.Error(Defines.ERR_DROP, "MOD_LoadBmodel: funny lump size in " + this.loadmodel.name);

		count = l.filelen / dface_t.SIZE;

		// out = Hunk_Alloc ( count*sizeof(*out));
		@out = new msurface_t[count];

		this.loadmodel.surfaces = @out;
		this.loadmodel.numsurfaces = count;

		BinaryReader bb = new(new MemoryStream(this.mod_base, l.fileofs, l.filelen));

		this.currentmodel = this.loadmodel;

		this.GL_BeginBuildingLightmaps(this.loadmodel);

		for (surfnum = 0; surfnum < count; surfnum++)
		{
			@in = new(bb);
			@out[surfnum] = new();
			@out[surfnum].firstedge = @in.firstedge;
			@out[surfnum].numedges = @in.numedges;
			@out[surfnum].flags = 0;
			@out[surfnum].polys = null;

			planenum = @in.planenum;
			side = @in.side;

			if (side != 0)
				@out[surfnum].flags |= Defines.SURF_PLANEBACK;

			@out[surfnum].plane = this.loadmodel.planes[planenum];

			ti = @in.texinfo;

			if (ti < 0 || ti >= this.loadmodel.numtexinfo)
				Com.Error(Defines.ERR_DROP, "MOD_LoadBmodel: bad texinfo number");

			@out[surfnum].texinfo = this.loadmodel.texinfo[ti];

			this.CalcSurfaceExtents(@out[surfnum]);

			// lighting info

			for (i = 0; i < Defines.MAXLIGHTMAPS; i++)
				@out[surfnum].styles[i] = @in.styles[i];

			i = @in.lightofs;

			if (i == -1)
			{
				@out[surfnum].samples = null;
				@out[surfnum].samplesOffset = 0;
			}
			else
			{
				@out[surfnum].samples = this.loadmodel.lightdata;
				@out[surfnum].samplesOffset = i;
			}

			// set the drawing flags

			if ((@out[surfnum].texinfo.flags & Defines.SURF_WARP) != 0)
			{
				@out[surfnum].flags |= Defines.SURF_DRAWTURB;

				for (i = 0; i < 2; i++)
				{
					@out[surfnum].extents[i] = 16384;
					@out[surfnum].texturemins[i] = -8192;
				}

				this.GL_SubdivideSurface(@out[surfnum]); // cut up polygon for warps
			}

			// create lightmaps and polygons
			if ((@out[surfnum].texinfo.flags & (Defines.SURF_SKY | Defines.SURF_TRANS33 | Defines.SURF_TRANS66 | Defines.SURF_WARP)) == 0)
				this.GL_CreateSurfaceLightmap(@out[surfnum]);

			if ((@out[surfnum].texinfo.flags & Defines.SURF_WARP) == 0)
				this.GL_BuildPolygonFromSurface(@out[surfnum]);
		}

		this.GL_EndBuildingLightmaps();
	}

	/*
	=================
	Mod_SetParent
	=================
	*/
	private void Mod_SetParent(mnode_t node, mnode_t parent)
	{
		node.parent = parent;

		if (node.contents != -1)
			return;

		this.Mod_SetParent(node.children[0], node);
		this.Mod_SetParent(node.children[1], node);
	}

	/*
	=================
	Mod_LoadNodes
	=================
	*/
	private void Mod_LoadNodes(lump_t l)
	{
		int i, j, count, p;
		dnode_t @in;
		mnode_t[] @out;

		if (l.filelen % dnode_t.SIZE != 0)
			Com.Error(Defines.ERR_DROP, "MOD_LoadBmodel: funny lump size in " + this.loadmodel.name);

		count = l.filelen / dnode_t.SIZE;

		// out = Hunk_Alloc ( count*sizeof(*out));
		@out = new mnode_t[count];

		this.loadmodel.nodes = @out;
		this.loadmodel.numnodes = count;

		BinaryReader bb = new(new MemoryStream(this.mod_base, l.fileofs, l.filelen));

		// initialize the tree array
		for (i = 0; i < count; i++)
			@out[i] = new(); // do first before linking

		// fill and link the nodes
		for (i = 0; i < count; i++)
		{
			@in = new(bb);

			for (j = 0; j < 3; j++)
			{
				@out[i].mins[j] = @in.mins[j];
				@out[i].maxs[j] = @in.maxs[j];
			}

			p = @in.planenum;
			@out[i].plane = this.loadmodel.planes[p];

			@out[i].firstsurface = @in.firstface;
			@out[i].numsurfaces = @in.numfaces;
			@out[i].contents = -1; // differentiate from leafs

			for (j = 0; j < 2; j++)
			{
				p = @in.children[j];

				if (p >= 0)
					@out[i].children[j] = this.loadmodel.nodes[p];
				else
					@out[i].children[j] = this.loadmodel.leafs[-1 - p]; // mleaf_t extends mnode_t
			}
		}

		this.Mod_SetParent(this.loadmodel.nodes[0], null); // sets nodes and leafs
	}

	/*
	=================
	Mod_LoadLeafs
	=================
	*/
	private void Mod_LoadLeafs(lump_t l)
	{
		dleaf_t @in;
		mleaf_t[] @out;
		int i, j, count;

		if (l.filelen % dleaf_t.SIZE != 0)
			Com.Error(Defines.ERR_DROP, "MOD_LoadBmodel: funny lump size in " + this.loadmodel.name);

		count = l.filelen / dleaf_t.SIZE;

		// out = Hunk_Alloc ( count*sizeof(*out));
		@out = new mleaf_t[count];

		this.loadmodel.leafs = @out;
		this.loadmodel.numleafs = count;

		BinaryReader bb = new(new MemoryStream(this.mod_base, l.fileofs, l.filelen));

		for (i = 0; i < count; i++)
		{
			@in = new(bb);
			@out[i] = new();

			for (j = 0; j < 3; j++)
			{
				@out[i].mins[j] = @in.mins[j];
				@out[i].maxs[j] = @in.maxs[j];
			}

			@out[i].contents = @in.contents;
			@out[i].cluster = @in.cluster;
			@out[i].area = @in.area;

			@out[i].setMarkSurface(@in.firstleafface, this.loadmodel.marksurfaces);
			@out[i].nummarksurfaces = @in.numleaffaces;
		}
	}

	/*
	=================
	Mod_LoadMarksurfaces
	=================
	*/
	private void Mod_LoadMarksurfaces(lump_t l)
	{
		int i, j, count;

		msurface_t[] @out;

		if (l.filelen % Defines.SIZE_OF_SHORT != 0)
			Com.Error(Defines.ERR_DROP, "MOD_LoadBmodel: funny lump size in " + this.loadmodel.name);

		count = l.filelen / Defines.SIZE_OF_SHORT;

		// out = Hunk_Alloc ( count*sizeof(*out));
		@out = new msurface_t[count];

		this.loadmodel.marksurfaces = @out;
		this.loadmodel.nummarksurfaces = count;

		BinaryReader bb = new(new MemoryStream(this.mod_base, l.fileofs, l.filelen));

		for (i = 0; i < count; i++)
		{
			j = bb.ReadInt16();

			if (j < 0 || j >= this.loadmodel.numsurfaces)
				Com.Error(Defines.ERR_DROP, "Mod_ParseMarksurfaces: bad surface number");

			@out[i] = this.loadmodel.surfaces[j];
		}
	}

	/*
	=================
	Mod_LoadSurfedges
	=================
	*/
	private void Mod_LoadSurfedges(lump_t l)
	{
		int i, count;
		int[] offsets;

		if (l.filelen % Defines.SIZE_OF_INT != 0)
			Com.Error(Defines.ERR_DROP, "MOD_LoadBmodel: funny lump size in " + this.loadmodel.name);

		count = l.filelen / Defines.SIZE_OF_INT;

		if (count < 1 || count >= Defines.MAX_MAP_SURFEDGES)
			Com.Error(Defines.ERR_DROP, "MOD_LoadBmodel: bad surfedges count in " + this.loadmodel.name + ": " + count);

		offsets = new int[count];

		this.loadmodel.surfedges = offsets;
		this.loadmodel.numsurfedges = count;

		BinaryReader bb = new(new MemoryStream(this.mod_base, l.fileofs, l.filelen));

		for (i = 0; i < count; i++)
			offsets[i] = bb.ReadInt32();
	}

	/*
	=================
	Mod_LoadPlanes
	=================
	*/
	private void Mod_LoadPlanes(lump_t l)
	{
		int i, j;
		cplane_t[] @out;
		dplane_t @in;
		int count;
		int bits;

		if (l.filelen % dplane_t.SIZE != 0)
			Com.Error(Defines.ERR_DROP, "MOD_LoadBmodel: funny lump size in " + this.loadmodel.name);

		count = l.filelen / dplane_t.SIZE;

		// out = Hunk_Alloc ( count*2*sizeof(*out));
		@out = new cplane_t[count * 2];

		this.loadmodel.planes = @out;
		this.loadmodel.numplanes = count;

		BinaryReader bb = new(new MemoryStream(this.mod_base, l.fileofs, l.filelen));

		for (i = 0; i < count; i++)
		{
			bits = 0;
			@in = new(bb);
			@out[i] = new();

			for (j = 0; j < 3; j++)
			{
				@out[i].normal[j] = @in.normal[j];

				if (@out[i].normal[j] < 0)
					bits |= 1 << j;
			}

			@out[i].dist = @in.dist;
			@out[i].type = (byte)@in.type;
			@out[i].signbits = (byte)bits;
		}
	}

	/*
	=================
	Mod_LoadBrushModel
	=================
	*/
	private void Mod_LoadBrushModel(model_t mod, BinaryReader buffer)
	{
		int i;
		dheader_t header;
		mmodel_t bm;

		this.loadmodel.type = Base.mod_brush;

		if (this.loadmodel != this.mod_known[0])
			Com.Error(Defines.ERR_DROP, "Loaded a brush model after the world");

		header = new(buffer);

		i = header.version;

		if (i != Defines.BSPVERSION)
			Com.Error(Defines.ERR_DROP, "Mod_LoadBrushModel: " + mod.name + " has wrong version number (" + i + " should be " + Defines.BSPVERSION + ")");

		this.mod_base = this.fileBuffer; //(byte *)header;

		// load into heap
		this.Mod_LoadVertexes(header.lumps[Defines.LUMP_VERTEXES]); // ok
		this.Mod_LoadEdges(header.lumps[Defines.LUMP_EDGES]); // ok
		this.Mod_LoadSurfedges(header.lumps[Defines.LUMP_SURFEDGES]); // ok
		this.Mod_LoadLighting(header.lumps[Defines.LUMP_LIGHTING]); // ok
		this.Mod_LoadPlanes(header.lumps[Defines.LUMP_PLANES]); // ok
		this.Mod_LoadTexinfo(header.lumps[Defines.LUMP_TEXINFO]); // ok
		this.Mod_LoadFaces(header.lumps[Defines.LUMP_FACES]); // ok
		this.Mod_LoadMarksurfaces(header.lumps[Defines.LUMP_LEAFFACES]);
		this.Mod_LoadVisibility(header.lumps[Defines.LUMP_VISIBILITY]); // ok
		this.Mod_LoadLeafs(header.lumps[Defines.LUMP_LEAFS]); // ok
		this.Mod_LoadNodes(header.lumps[Defines.LUMP_NODES]); // ok
		this.Mod_LoadSubmodels(header.lumps[Defines.LUMP_MODELS]);
		mod.numframes = 2; // regular and alternate animation

		//
		// set up the submodels
		//
		model_t starmod;

		for (i = 0; i < mod.numsubmodels; i++)
		{
			bm = mod.submodels[i];
			starmod = this.mod_inline[i] = this.loadmodel.copy();

			starmod.firstmodelsurface = bm.firstface;
			starmod.nummodelsurfaces = bm.numfaces;
			starmod.firstnode = bm.headnode;

			if (starmod.firstnode >= this.loadmodel.numnodes)
				Com.Error(Defines.ERR_DROP, "Inline model " + i + " has bad firstnode");

			Math3D.VectorCopy(bm.maxs, starmod.maxs);
			Math3D.VectorCopy(bm.mins, starmod.mins);
			starmod.radius = bm.radius;

			if (i == 0)
				this.loadmodel = starmod.copy();

			starmod.numleafs = bm.visleafs;
		}
	}

	/*
	==============================================================================

	ALIAS MODELS

	==============================================================================
	*/

	/*
	=================
	Mod_LoadAliasModel
	=================
	*/
	private void Mod_LoadAliasModel(model_t mod, BinaryReader buffer)
	{
		int i;
		dmdl_t pheader;
		dstvert_t[] poutst;
		dtriangle_t[] pouttri;
		daliasframe_t[] poutframe;
		int[] poutcmd;

		pheader = new(buffer);

		if (pheader.version != qfiles.ALIAS_VERSION)
		{
			Com.Error(
				Defines.ERR_DROP,
				$"{mod.name} has wrong version number ({pheader.version} should be {qfiles.ALIAS_VERSION})"
			);
		}

		if (pheader.skinheight > Base.MAX_LBM_HEIGHT)
			Com.Error(Defines.ERR_DROP, "model " + mod.name + " has a skin taller than " + Base.MAX_LBM_HEIGHT);

		if (pheader.num_xyz <= 0)
			Com.Error(Defines.ERR_DROP, "model " + mod.name + " has no vertices");

		if (pheader.num_xyz > qfiles.MAX_VERTS)
			Com.Error(Defines.ERR_DROP, "model " + mod.name + " has too many vertices");

		if (pheader.num_st <= 0)
			Com.Error(Defines.ERR_DROP, "model " + mod.name + " has no st vertices");

		if (pheader.num_tris <= 0)
			Com.Error(Defines.ERR_DROP, "model " + mod.name + " has no triangles");

		if (pheader.num_frames <= 0)
			Com.Error(Defines.ERR_DROP, "model " + mod.name + " has no frames");

		//
		// load base s and t vertices (not used in gl version)
		//
		poutst = new dstvert_t[pheader.num_st];
		buffer.BaseStream.Position = pheader.ofs_st;

		for (i = 0; i < pheader.num_st; i++)
			poutst[i] = new(buffer);

		//
		//	   load triangle lists
		//
		pouttri = new dtriangle_t[pheader.num_tris];
		buffer.BaseStream.Position = pheader.ofs_tris;

		for (i = 0; i < pheader.num_tris; i++)
			pouttri[i] = new(buffer);

		//
		//	   load the frames
		//
		poutframe = new daliasframe_t[pheader.num_frames];
		buffer.BaseStream.Position = pheader.ofs_frames;

		for (i = 0; i < pheader.num_frames; i++)
		{
			poutframe[i] = new(buffer);

			// verts are all 8 bit, so no swapping needed
			poutframe[i].verts = new int[pheader.num_xyz];

			for (var k = 0; k < pheader.num_xyz; k++)
				poutframe[i].verts[k] = buffer.ReadInt32();
		}

		mod.type = Base.mod_alias;

		//
		// load the glcmds
		//
		poutcmd = new int[pheader.num_glcmds];
		buffer.BaseStream.Position = pheader.ofs_glcmds;

		for (i = 0; i < pheader.num_glcmds; i++)
			poutcmd[i] = buffer.ReadInt32(); // LittleLong (pincmd[i]);

		// register all skins
		var skinNames = new string[pheader.num_skins];
		var nameBuf = new byte[qfiles.MAX_SKINNAME];
		buffer.BaseStream.Position = pheader.ofs_skins;

		for (i = 0; i < pheader.num_skins; i++)
		{
			buffer.Read(nameBuf);
			skinNames[i] = Encoding.ASCII.GetString(nameBuf).Split((char)0)[0];
			mod.skins[i] = this.GL_FindImage(skinNames[i], Base.it_skin);
		}

		// set the model arrays
		pheader.skinNames = skinNames; // skin names
		pheader.stVerts = poutst; // textur koordinaten
		pheader.triAngles = pouttri; // dreiecke
		pheader.glCmds = poutcmd; // STRIP or FAN
		pheader.aliasFrames = poutframe; // frames mit vertex array

		mod.extradata = pheader;

		mod.mins[0] = -32;
		mod.mins[1] = -32;
		mod.mins[2] = -32;
		mod.maxs[0] = 32;
		mod.maxs[1] = 32;
		mod.maxs[2] = 32;
	}

	/*
	==============================================================================

	SPRITE MODELS

	==============================================================================
	*/

	/*
	=================
	Mod_LoadSpriteModel
	=================
	*/
	private void Mod_LoadSpriteModel(model_t mod, BinaryReader buffer)
	{
		dsprite_t sprout = new(buffer);

		if (sprout.version != qfiles.SPRITE_VERSION)
		{
			Com.Error(
				Defines.ERR_DROP,
				$"{mod.name} has wrong version number ({sprout.version} should be {qfiles.SPRITE_VERSION})"
			);
		}

		if (sprout.numframes > qfiles.MAX_MD2SKINS)
			Com.Error(Defines.ERR_DROP, $"{mod.name} has too many frames ({sprout.numframes} > {qfiles.MAX_MD2SKINS})");

		for (var i = 0; i < sprout.numframes; i++)
			mod.skins[i] = this.GL_FindImage(sprout.frames[i].name, Base.it_sprite);

		mod.type = Base.mod_sprite;
		mod.extradata = sprout;
	}

	//	  =============================================================================

	/*
	@@@@@@@@@@@@@@@@@@@@@
	R_BeginRegistration

	Specifies the model that will be used as the world
	@@@@@@@@@@@@@@@@@@@@@
	*/
	public override void R_BeginRegistration(string model)
	{
		cvar_t flushmap;

		Polygon.reset();

		this.registration_sequence++;
		this.r_oldviewcluster = -1; // force markleafs

		var fullname = "maps/" + model + ".bsp";

		// explicitly free the old map if different
		// this guarantees that mod_known[0] is the world map
		flushmap = Cvar.Get("flushmap", "0", 0);

		if (!this.mod_known[0].name.Equals(fullname) || flushmap.value != 0.0f)
			this.Mod_Free(this.mod_known[0]);

		this.r_worldmodel = this.Mod_ForName(fullname, true);

		this.r_viewcluster = -1;
	}

	/*
	@@@@@@@@@@@@@@@@@@@@@
	R_RegisterModel

	@@@@@@@@@@@@@@@@@@@@@
	*/
	public override model_t R_RegisterModel(string name)
	{
		model_t mod = null;
		int i;
		dsprite_t sprout;
		dmdl_t pheader;

		mod = this.Mod_ForName(name, false);

		if (mod != null)
		{
			mod.registration_sequence = this.registration_sequence;

			// register any images used by the models
			if (mod.type == Base.mod_sprite)
			{
				sprout = (dsprite_t)mod.extradata;

				for (i = 0; i < sprout.numframes; i++)
					mod.skins[i] = this.GL_FindImage(sprout.frames[i].name, Base.it_sprite);
			}
			else if (mod.type == Base.mod_alias)
			{
				pheader = (dmdl_t)mod.extradata;

				for (i = 0; i < pheader.num_skins; i++)
					mod.skins[i] = this.GL_FindImage(pheader.skinNames[i], Base.it_skin);

				// PGM
				mod.numframes = pheader.num_frames;

				// PGM
			}
			else if (mod.type == Base.mod_brush)
			{
				for (i = 0; i < mod.numtexinfo; i++)
					mod.texinfo[i].image.registration_sequence = this.registration_sequence;
			}
		}

		return mod;
	}

	/*
	@@@@@@@@@@@@@@@@@@@@@
	R_EndRegistration

	@@@@@@@@@@@@@@@@@@@@@
	*/
	public override void R_EndRegistration()
	{
		model_t mod;

		for (var i = 0; i < this.mod_numknown; i++)
		{
			mod = this.mod_known[i];

			if (mod.name.Length == 0)
				continue;

			if (mod.registration_sequence != this.registration_sequence)
			{
				// don't need this model
				this.Mod_Free(mod);
			}
		}

		this.GL_FreeUnusedImages();
	}

	//	  =============================================================================

	/*
	================
	Mod_Free
	================
	*/
	private void Mod_Free(model_t mod)
	{
		mod.clear();
	}

	/*
	================
	Mod_FreeAll
	================
	*/
	protected void Mod_FreeAll()
	{
		for (var i = 0; i < this.mod_numknown; i++)
		{
			if (this.mod_known[i].extradata != null)
				this.Mod_Free(this.mod_known[i]);
		}
	}
}