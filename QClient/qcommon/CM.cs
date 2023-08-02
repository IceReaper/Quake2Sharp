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
namespace QClient.qcommon;

using game.types;
using OpenTK.Core;
using QClient;
using System.Text;
using types;
using util;

public class CM
{
	public class cnode_t
	{
		public cplane_t plane; // ptr
		public int[] children = { 0, 0 }; // negative numbers are leafs
	}

	public class cbrushside_t
	{
		public cplane_t plane; // ptr
		public mapsurface_t surface; // ptr
	}

	public class cleaf_t
	{
		public int contents;
		public int cluster;
		public int area;

		// was unsigned short, but is ok (rst)
		public short firstleafbrush;

		// was unsigned short, but is ok (rst)
		public short numleafbrushes;
	}

	public class cbrush_t
	{
		public int contents;
		public int numsides;
		public int firstbrushside;
		public int checkcount; // to avoid repeated testings
	}

	public class carea_t
	{
		public int numareaportals;
		public int firstareaportal;
		public int floodnum; // if two areas have equal floodnums, they are connected
		public int floodvalid;
	}

	private static int checkcount;
	private static string map_name = "";
	private static int numbrushsides;
	private static readonly cbrushside_t[] map_brushsides = new cbrushside_t[Defines.MAX_MAP_BRUSHSIDES];

	private static void CM1()
	{
		for (var n = 0; n < Defines.MAX_MAP_BRUSHSIDES; n++)
			CM.map_brushsides[n] = new();
	}

	public static int numtexinfo;
	public static mapsurface_t[] map_surfaces = new mapsurface_t[Defines.MAX_MAP_TEXINFO];

	private static void CM2()
	{
		for (var n = 0; n < Defines.MAX_MAP_TEXINFO; n++)
			CM.map_surfaces[n] = new();
	}

	private static int numplanes;

	/** Extra for box hull ( +6) */
	private static readonly cplane_t[] map_planes = new cplane_t[Defines.MAX_MAP_PLANES + 6];

	private static void CM3()
	{
		for (var n = 0; n < Defines.MAX_MAP_PLANES + 6; n++)
			CM.map_planes[n] = new();
	}

	private static int numnodes;

	/** Extra for box hull ( +6) */
	private static readonly cnode_t[] map_nodes = new cnode_t[Defines.MAX_MAP_NODES + 6];

	private static void CM4()
	{
		for (var n = 0; n < Defines.MAX_MAP_NODES + 6; n++)
			CM.map_nodes[n] = new();
	}

	private static int numleafs = 1; // allow leaf funcs to be called without a map
	private static readonly cleaf_t[] map_leafs = new cleaf_t[Defines.MAX_MAP_LEAFS];

	private static void CM5()
	{
		for (var n = 0; n < Defines.MAX_MAP_LEAFS; n++)
			CM.map_leafs[n] = new();
	}

	private static int emptyleaf;
	private static int numleafbrushes;
	public static int[] map_leafbrushes = new int[Defines.MAX_MAP_LEAFBRUSHES];
	public static int numcmodels;
	public static cmodel_t[] map_cmodels = new cmodel_t[Defines.MAX_MAP_MODELS];

	private static void CM6()
	{
		for (var n = 0; n < Defines.MAX_MAP_MODELS; n++)
			CM.map_cmodels[n] = new();
	}

	public static int numbrushes;
	public static cbrush_t[] map_brushes = new cbrush_t[Defines.MAX_MAP_BRUSHES];

	private static void CM7()
	{
		for (var n = 0; n < Defines.MAX_MAP_BRUSHES; n++)
			CM.map_brushes[n] = new();
	}

	public static int numvisibility;
	public static byte[] map_visibility = new byte[Defines.MAX_MAP_VISIBILITY];

	/** Main visibility data. */
	public static dvis_t map_vis = new(new(new MemoryStream(CM.map_visibility)));

	public static int numentitychars;
	public static string map_entitystring;
	public static int numareas = 1;
	public static carea_t[] map_areas = new carea_t[Defines.MAX_MAP_AREAS];

	private static void CM8()
	{
		for (var n = 0; n < Defines.MAX_MAP_AREAS; n++)
			CM.map_areas[n] = new();
	}

	public static int numareaportals;
	public static dareaportal_t[] map_areaportals = new dareaportal_t[Defines.MAX_MAP_AREAPORTALS];

	private static void CM9()
	{
		for (var n = 0; n < Defines.MAX_MAP_AREAPORTALS; n++)
			CM.map_areaportals[n] = new();
	}

	public static int numclusters = 1;
	public static mapsurface_t nullsurface = new();
	public static int floodvalid;
	public static bool[] portalopen = new bool[Defines.MAX_MAP_AREAPORTALS];
	public static cvar_t map_noareas;
	public static byte[] cmod_base;
	public static int checksum;
	public static int last_checksum;

	/** 
     * Loads in the map and all submodels.
     */
	public static cmodel_t CM_LoadMap(string name, bool clientload, int[] checksum)
	{
		Com.DPrintf("CM_LoadMap(" + name + ")...\n");
		byte[] buf;
		dheader_t header;
		int length;

		CM.map_noareas = Cvar.Get("map_noareas", "0", 0);

		if (CM.map_name.Equals(name) && (clientload || 0 == Cvar.VariableValue("flushmap")))
		{
			checksum[0] = CM.last_checksum;

			if (!clientload)
			{
				Array.Fill(CM.portalopen, false);
				CM.FloodAreaConnections();
			}

			return CM.map_cmodels[0]; // still have the right version
		}

		// free old stuff
		CM.numnodes = 0;
		CM.numleafs = 0;
		CM.numcmodels = 0;
		CM.numvisibility = 0;
		CM.numentitychars = 0;
		CM.map_entitystring = "";
		CM.map_name = "";

		if (name == null || name.Length == 0)
		{
			CM.numleafs = 1;
			CM.numclusters = 1;
			CM.numareas = 1;
			checksum[0] = 0;

			return CM.map_cmodels[0];

			// cinematic servers won't have anything at all
		}

		//
		// load the file
		//
		buf = FS.LoadFile(name);

		if (buf == null)
			Com.Error(Defines.ERR_DROP, "Couldn't load " + name);

		length = buf.Length;

		BinaryReader bbuf = new(new MemoryStream(buf));

		CM.last_checksum = MD4.Com_BlockChecksum(buf, length);
		checksum[0] = CM.last_checksum;

		header = new(bbuf);

		if (header.version != Defines.BSPVERSION)
		{
			Com.Error(
				Defines.ERR_DROP,
				"CMod_LoadBrushModel: " + name + " has wrong version number (" + header.version + " should be " + Defines.BSPVERSION + ")"
			);
		}

		CM.cmod_base = buf;

		// load into heap
		CM.CMod_LoadSurfaces(header.lumps[Defines.LUMP_TEXINFO]); // ok
		CM.CMod_LoadLeafs(header.lumps[Defines.LUMP_LEAFS]);
		CM.CMod_LoadLeafBrushes(header.lumps[Defines.LUMP_LEAFBRUSHES]);
		CM.CMod_LoadPlanes(header.lumps[Defines.LUMP_PLANES]);
		CM.CMod_LoadBrushes(header.lumps[Defines.LUMP_BRUSHES]);
		CM.CMod_LoadBrushSides(header.lumps[Defines.LUMP_BRUSHSIDES]);
		CM.CMod_LoadSubmodels(header.lumps[Defines.LUMP_MODELS]);

		CM.CMod_LoadNodes(header.lumps[Defines.LUMP_NODES]);
		CM.CMod_LoadAreas(header.lumps[Defines.LUMP_AREAS]);
		CM.CMod_LoadAreaPortals(header.lumps[Defines.LUMP_AREAPORTALS]);
		CM.CMod_LoadVisibility(header.lumps[Defines.LUMP_VISIBILITY]);
		//CM.CMod_LoadEntityString(header.lumps[Defines.LUMP_ENTITIES]);
		CM.map_entitystring = AzUtils.DownloadEntities(name);

        FS.FreeFile(buf);

		CM.CM_InitBoxHull();

		Array.Fill(CM.portalopen, false);

		CM.FloodAreaConnections();

		CM.map_name = name;

		return CM.map_cmodels[0];
	}

	/** Loads Submodels. */
	public static void CMod_LoadSubmodels(lump_t l)
	{
		Com.DPrintf("CMod_LoadSubmodels()\n");
		dmodel_t @in;
		cmodel_t @out;
		int i, j, count;

		if (l.filelen % dmodel_t.SIZE != 0)
			Com.Error(Defines.ERR_DROP, "CMod_LoadBmodel: funny lump size");

		count = l.filelen / dmodel_t.SIZE;

		if (count < 1)
			Com.Error(Defines.ERR_DROP, "Map with no models");

		if (count > Defines.MAX_MAP_MODELS)
			Com.Error(Defines.ERR_DROP, "Map has too many models");

		Com.DPrintf(" numcmodels=" + count + "\n");
		CM.numcmodels = count;

		if (CM.debugloadmap)
			Com.DPrintf("submodles(headnode, <origin>, <mins>, <maxs>)\n");

		for (i = 0; i < count; i++)
		{
			@in = new(new(new MemoryStream(CM.cmod_base, i * dmodel_t.SIZE + l.fileofs, dmodel_t.SIZE)));
			@out = CM.map_cmodels[i];

			for (j = 0; j < 3; j++)
			{
				// spread the mins / maxs by a pixel
				@out.mins[j] = @in.mins[j] - 1;
				@out.maxs[j] = @in.maxs[j] + 1;
				@out.origin[j] = @in.origin[j];
			}

			@out.headnode = @in.headnode;

			if (CM.debugloadmap)
			{
				Com.DPrintf(
					$"|{@out.headnode,6}|{@out.origin[0]:0.00,8}|{@out.origin[1]:0.00,8}|{@out.origin[2]:0.00,8}|  {@out.mins[0]:0.00,8}|{@out.mins[1]:0.00,8}|{@out.mins[2]:0.00,8}|   {@out.maxs[0]:0.00,8}|{@out.maxs[1]:0.00,8}|{@out.maxs[2]:0.00,8}|\n"
				);
			}
		}
	}

	private static readonly bool debugloadmap = false;

	/** Loads surfaces. */
	public static void CMod_LoadSurfaces(lump_t l)
	{
		Com.DPrintf("CMod_LoadSurfaces()\n");
		texinfo_t @in;
		mapsurface_t @out;
		int i, count;

		if (l.filelen % texinfo_t.SIZE != 0)
			Com.Error(Defines.ERR_DROP, "MOD_LoadBmodel: funny lump size");

		count = l.filelen / texinfo_t.SIZE;

		if (count < 1)
			Com.Error(Defines.ERR_DROP, "Map with no surfaces");

		if (count > Defines.MAX_MAP_TEXINFO)
			Com.Error(Defines.ERR_DROP, "Map has too many surfaces");

		CM.numtexinfo = count;
		Com.DPrintf(" numtexinfo=" + count + "\n");

		if (CM.debugloadmap)
			Com.DPrintf("surfaces:\n");

		for (i = 0; i < count; i++)
		{
			@out = CM.map_surfaces[i] = new();
			@in = new(CM.cmod_base, l.fileofs + i * texinfo_t.SIZE, texinfo_t.SIZE);

			@out.c.name = @in.texture;
			@out.rname = @in.texture;
			@out.c.flags = @in.flags;
			@out.c.value = @in.value;

			if (CM.debugloadmap)
				Com.DPrintf($"|{@out.c.name,20}|{@out.rname,20}|{@out.c.value,6}|{@out.c.flags,6}|\n");
		}
	}

	/** Loads nodes. */
	public static void CMod_LoadNodes(lump_t l)
	{
		Com.DPrintf("CMod_LoadNodes()\n");
		dnode_t @in;
		int child;
		cnode_t @out;
		int i, j, count;

		if (l.filelen % dnode_t.SIZE != 0)
			Com.Error(Defines.ERR_DROP, "MOD_LoadBmodel: funny lump size:" + l.fileofs + "," + dnode_t.SIZE);

		count = l.filelen / dnode_t.SIZE;

		if (count < 1)
			Com.Error(Defines.ERR_DROP, "Map has no nodes");

		if (count > Defines.MAX_MAP_NODES)
			Com.Error(Defines.ERR_DROP, "Map has too many nodes");

		CM.numnodes = count;
		Com.DPrintf(" numnodes=" + count + "\n");

		if (CM.debugloadmap)
			Com.DPrintf("nodes(planenum, child[0], child[1])\n");

		for (i = 0; i < count; i++)
		{
			@in = new(new(new MemoryStream(CM.cmod_base, dnode_t.SIZE * i + l.fileofs, dnode_t.SIZE)));
			@out = CM.map_nodes[i];

			@out.plane = CM.map_planes[@in.planenum];

			for (j = 0; j < 2; j++)
			{
				child = @in.children[j];
				@out.children[j] = child;
			}

			if (CM.debugloadmap)
				Com.DPrintf($"|{@in.planenum,6}| {@out.children[0],6}| {@out.children[1],6}|\n");
		}
	}

	/** Loads brushes.*/
	public static void CMod_LoadBrushes(lump_t l)
	{
		Com.DPrintf("CMod_LoadBrushes()\n");
		dbrush_t @in;
		cbrush_t @out;
		int i, count;

		if (l.filelen % dbrush_t.SIZE != 0)
			Com.Error(Defines.ERR_DROP, "MOD_LoadBmodel: funny lump size");

		count = l.filelen / dbrush_t.SIZE;

		if (count > Defines.MAX_MAP_BRUSHES)
			Com.Error(Defines.ERR_DROP, "Map has too many brushes");

		CM.numbrushes = count;
		Com.DPrintf(" numbrushes=" + count + "\n");

		if (CM.debugloadmap)
			Com.DPrintf("brushes:(firstbrushside, numsides, contents)\n");

		for (i = 0; i < count; i++)
		{
			@in = new(new(new MemoryStream(CM.cmod_base, i * dbrush_t.SIZE + l.fileofs, dbrush_t.SIZE)));
			@out = CM.map_brushes[i];
			@out.firstbrushside = @in.firstside;
			@out.numsides = @in.numsides;
			@out.contents = @in.contents;

			if (CM.debugloadmap)
				Com.DPrintf($"| {@out.firstbrushside,6}| {@out.numsides,6}| {@out.contents:X8}|\n");
		}
	}

	/** Loads leafs.   */
	public static void CMod_LoadLeafs(lump_t l)
	{
		Com.DPrintf("CMod_LoadLeafs()\n");
		int i;
		cleaf_t @out;
		dleaf_t @in;
		int count;

		if (l.filelen % dleaf_t.SIZE != 0)
			Com.Error(Defines.ERR_DROP, "MOD_LoadBmodel: funny lump size");

		count = l.filelen / dleaf_t.SIZE;

		if (count < 1)
			Com.Error(Defines.ERR_DROP, "Map with no leafs");

		// need to save space for box planes
		if (count > Defines.MAX_MAP_PLANES)
			Com.Error(Defines.ERR_DROP, "Map has too many planes");

		Com.DPrintf(" numleafes=" + count + "\n");

		CM.numleafs = count;
		CM.numclusters = 0;

		if (CM.debugloadmap)
			Com.DPrintf("cleaf-list:(contents, cluster, area, firstleafbrush, numleafbrushes)\n");

		for (i = 0; i < count; i++)
		{
			@in = new(CM.cmod_base, i * dleaf_t.SIZE + l.fileofs, dleaf_t.SIZE);

			@out = CM.map_leafs[i];

			@out.contents = @in.contents;
			@out.cluster = @in.cluster;
			@out.area = @in.area;
			@out.firstleafbrush = (short)@in.firstleafbrush;
			@out.numleafbrushes = (short)@in.numleafbrushes;

			if (@out.cluster >= CM.numclusters)
				CM.numclusters = @out.cluster + 1;

			if (CM.debugloadmap)
			{
				Com.DPrintf(
					$"|{@out.contents:x8}|{@out.cluster,6}|{@out.area,6}|{@out.firstleafbrush,6}|\n"
				);
			}
		}

		Com.DPrintf(" numclusters=" + CM.numclusters + "\n");

		if (CM.map_leafs[0].contents != Defines.CONTENTS_SOLID)
			Com.Error(Defines.ERR_DROP, "Map leaf 0 is not CONTENTS_SOLID");

		CM.emptyleaf = -1;

		for (i = 1; i < CM.numleafs; i++)
		{
			if (CM.map_leafs[i].contents == 0)
			{
				CM.emptyleaf = i;

				break;
			}
		}

		if (CM.emptyleaf == -1)
			Com.Error(Defines.ERR_DROP, "Map does not have an empty leaf");
	}

	/** Loads planes. */
	public static void CMod_LoadPlanes(lump_t l)
	{
		Com.DPrintf("CMod_LoadPlanes()\n");
		int i, j;
		cplane_t @out;
		dplane_t @in;
		int count;
		int bits;

		if (l.filelen % dplane_t.SIZE != 0)
			Com.Error(Defines.ERR_DROP, "MOD_LoadBmodel: funny lump size");

		count = l.filelen / dplane_t.SIZE;

		if (count < 1)
			Com.Error(Defines.ERR_DROP, "Map with no planes");

		// need to save space for box planes
		if (count > Defines.MAX_MAP_PLANES)
			Com.Error(Defines.ERR_DROP, "Map has too many planes");

		Com.DPrintf(" numplanes=" + count + "\n");

		CM.numplanes = count;

		if (CM.debugloadmap)
			Com.DPrintf("cplanes(normal[0],normal[1],normal[2], dist, type, signbits)\n");

		for (i = 0; i < count; i++)
		{
			@in = new(new(new MemoryStream(CM.cmod_base, i * dplane_t.SIZE + l.fileofs, dplane_t.SIZE)));

			@out = CM.map_planes[i];

			bits = 0;

			for (j = 0; j < 3; j++)
			{
				@out.normal[j] = @in.normal[j];

				if (@out.normal[j] < 0)
					bits |= 1 << j;
			}

			@out.dist = @in.dist;
			@out.type = (byte)@in.type;
			@out.signbits = (byte)bits;

			if (CM.debugloadmap)
			{
				Com.DPrintf(
					$"|{@out.normal[0]:0.00,6}|{@out.normal[1]:0.00,6}|{@out.normal[2]:0.00,6}| {@out.dist:0.00,10}|{@out.type,3}| {@out.signbits,1}|\n"
				);
			}
		}
	}

	/** Loads leaf brushes. */
	public static void CMod_LoadLeafBrushes(lump_t l)
	{
		Com.DPrintf("CMod_LoadLeafBrushes()\n");

		if (l.filelen % 2 != 0)
			Com.Error(Defines.ERR_DROP, "MOD_LoadBmodel: funny lump size");

		var count = l.filelen / 2;

		Com.DPrintf(" numbrushes=" + count + "\n");

		if (count < 1)
			Com.Error(Defines.ERR_DROP, "Map with no planes");

		// need to save space for box planes
		if (count > Defines.MAX_MAP_LEAFBRUSHES)
			Com.Error(Defines.ERR_DROP, "Map has too many leafbrushes");

		var @out = CM.map_leafbrushes;
		CM.numleafbrushes = count;

		BinaryReader bb = new(new MemoryStream(CM.cmod_base, l.fileofs, count * 2));

		if (CM.debugloadmap)
			Com.DPrintf("map_brushes:\n");

		for (var i = 0; i < count; i++)
		{
			@out[i] = bb.ReadInt16();

			if (CM.debugloadmap)
				Com.DPrintf($"|{i,6}|{@out[i],6}|\n");
		}
	}

	/** Loads brush sides. */
	public static void CMod_LoadBrushSides(lump_t l)
	{
		Com.DPrintf("CMod_LoadBrushSides()\n");
		int i, j;
		cbrushside_t @out;
		dbrushside_t @in;
		int count;
		int num;

		if (l.filelen % dbrushside_t.SIZE != 0)
			Com.Error(Defines.ERR_DROP, "MOD_LoadBmodel: funny lump size");

		count = l.filelen / dbrushside_t.SIZE;

		// need to save space for box planes
		if (count > Defines.MAX_MAP_BRUSHSIDES)
			Com.Error(Defines.ERR_DROP, "Map has too many planes");

		CM.numbrushsides = count;

		Com.DPrintf(" numbrushsides=" + count + "\n");

		if (CM.debugloadmap)
			Com.DPrintf("brushside(planenum, surfacenum):\n");

		for (i = 0; i < count; i++)
		{
			@in = new(new(new MemoryStream(CM.cmod_base, i * dbrushside_t.SIZE + l.fileofs, dbrushside_t.SIZE)));

			@out = CM.map_brushsides[i];

			num = @in.planenum;

			@out.plane = CM.map_planes[num]; // pointer

			j = @in.texinfo;

			if (j >= CM.numtexinfo)
				Com.Error(Defines.ERR_DROP, "Bad brushside texinfo");

			if (j == -1)
				@out.surface = new(); // just for safety
			else
				@out.surface = CM.map_surfaces[j];

			if (CM.debugloadmap)
				Com.DPrintf($"| {num,6}| {j,6}|\n");
		}
	}

	/** Loads areas. */
	public static void CMod_LoadAreas(lump_t l)
	{
		Com.DPrintf("CMod_LoadAreas()\n");
		int i;
		carea_t @out;
		darea_t @in;
		int count;

		if (l.filelen % darea_t.SIZE != 0)
			Com.Error(Defines.ERR_DROP, "MOD_LoadBmodel: funny lump size");

		count = l.filelen / darea_t.SIZE;

		if (count > Defines.MAX_MAP_AREAS)
			Com.Error(Defines.ERR_DROP, "Map has too many areas");

		Com.DPrintf(" numareas=" + count + "\n");
		CM.numareas = count;

		if (CM.debugloadmap)
			Com.DPrintf("areas(numportals, firstportal)\n");

		for (i = 0; i < count; i++)
		{
			@in = new(new(new MemoryStream(CM.cmod_base, i * darea_t.SIZE + l.fileofs, darea_t.SIZE)));
			@out = CM.map_areas[i];

			@out.numareaportals = @in.numareaportals;
			@out.firstareaportal = @in.firstareaportal;
			@out.floodvalid = 0;
			@out.floodnum = 0;

			if (CM.debugloadmap)
				Com.DPrintf($"| {@out.numareaportals,6}| {@out.firstareaportal,6}|\n");
		}
	}

	/** Loads area portals. */
	public static void CMod_LoadAreaPortals(lump_t l)
	{
		Com.DPrintf("CMod_LoadAreaPortals()\n");
		int i;
		dareaportal_t @out;
		dareaportal_t @in;
		int count;

		if (l.filelen % dareaportal_t.SIZE != 0)
			Com.Error(Defines.ERR_DROP, "MOD_LoadBmodel: funny lump size");

		count = l.filelen / dareaportal_t.SIZE;

		if (count > Defines.MAX_MAP_AREAS)
			Com.Error(Defines.ERR_DROP, "Map has too many areas");

		CM.numareaportals = count;
		Com.DPrintf(" numareaportals=" + count + "\n");

		if (CM.debugloadmap)
			Com.DPrintf("areaportals(portalnum, otherarea)\n");

		for (i = 0; i < count; i++)
		{
			@in = new(new(new MemoryStream(CM.cmod_base, i * dareaportal_t.SIZE + l.fileofs, dareaportal_t.SIZE)));

			@out = CM.map_areaportals[i];

			@out.portalnum = @in.portalnum;
			@out.otherarea = @in.otherarea;

			if (CM.debugloadmap)
				Com.DPrintf($"|{@out.portalnum,6}|{@out.otherarea,6}|\n");
		}
	}

	/** Loads visibility data. */
	public static void CMod_LoadVisibility(lump_t l)
	{
		Com.DPrintf("CMod_LoadVisibility()\n");

		CM.numvisibility = l.filelen;

		Com.DPrintf(" numvisibility=" + CM.numvisibility + "\n");

		if (l.filelen > Defines.MAX_MAP_VISIBILITY)
			Com.Error(Defines.ERR_DROP, "Map has too large visibility lump");

		Array.Copy(CM.cmod_base, l.fileofs, CM.map_visibility, 0, l.filelen);

		BinaryReader bb = new(new MemoryStream(CM.map_visibility, 0, l.filelen));

		CM.map_vis = new(bb);
	}

	/** Loads entity strings. */
	public static void CMod_LoadEntityString(lump_t l)
	{
		Com.DPrintf("CMod_LoadEntityString()\n");

		CM.numentitychars = l.filelen;

		if (l.filelen > Defines.MAX_MAP_ENTSTRING)
			Com.Error(Defines.ERR_DROP, "Map has too large entity lump");

		var x = 0;

		for (; x < l.filelen && CM.cmod_base[x + l.fileofs] != 0; x++)
			;

		CM.map_entitystring = Encoding.ASCII.GetString(CM.cmod_base, l.fileofs, x).Trim();

		Com.dprintln("entitystring=" + CM.map_entitystring.Length + " bytes, [" + CM.map_entitystring[..Math.Min(CM.map_entitystring.Length, 15)] + "...]");
	}

	/** Returns the model with a given id "*" + <number> */
	public static cmodel_t InlineModel(string name)
	{
		if (name == null || name[0] != '*')
			Com.Error(Defines.ERR_DROP, "CM_InlineModel: bad name");

		var num = Lib.atoi(name[1..]);

		if (num < 1 || num >= CM.numcmodels)
			Com.Error(Defines.ERR_DROP, "CM_InlineModel: bad number");

		return CM.map_cmodels[num];
	}

	public static int CM_NumClusters()
	{
		return CM.numclusters;
	}

	public static int CM_NumInlineModels()
	{
		return CM.numcmodels;
	}

	public static string CM_EntityString()
	{
		return CM.map_entitystring;
	}

	public static int CM_LeafContents(int leafnum)
	{
		if (leafnum < 0 || leafnum >= CM.numleafs)
			Com.Error(Defines.ERR_DROP, "CM_LeafContents: bad number");

		return CM.map_leafs[leafnum].contents;
	}

	public static int CM_LeafCluster(int leafnum)
	{
		if (leafnum < 0 || leafnum >= CM.numleafs)
			Com.Error(Defines.ERR_DROP, "CM_LeafCluster: bad number");

		return CM.map_leafs[leafnum].cluster;
	}

	public static int CM_LeafArea(int leafnum)
	{
		if (leafnum < 0 || leafnum >= CM.numleafs)
			Com.Error(Defines.ERR_DROP, "CM_LeafArea: bad number");

		return CM.map_leafs[leafnum].area;
	}

	private static cplane_t[] box_planes;
	private static int box_headnode;
	private static cbrush_t box_brush;
	private static cleaf_t box_leaf;

	/** Set up the planes and nodes so that the six floats of a bounding box can
     * just be stored out and get a proper clipping hull structure.
     */
	public static void CM_InitBoxHull()
	{
		CM.box_headnode = CM.numnodes; //rst: still room for 6 brushes left?

		CM.box_planes = new cplane_t[]
		{
			CM.map_planes[CM.numplanes],
			CM.map_planes[CM.numplanes + 1],
			CM.map_planes[CM.numplanes + 2],
			CM.map_planes[CM.numplanes + 3],
			CM.map_planes[CM.numplanes + 4],
			CM.map_planes[CM.numplanes + 5],
			CM.map_planes[CM.numplanes + 6],
			CM.map_planes[CM.numplanes + 7],
			CM.map_planes[CM.numplanes + 8],
			CM.map_planes[CM.numplanes + 9],
			CM.map_planes[CM.numplanes + 10],
			CM.map_planes[CM.numplanes + 11],
			CM.map_planes[CM.numplanes + 12]
		};

		if (CM.numnodes + 6 > Defines.MAX_MAP_NODES
		    || CM.numbrushes + 1 > Defines.MAX_MAP_BRUSHES
		    || CM.numleafbrushes + 1 > Defines.MAX_MAP_LEAFBRUSHES
		    || CM.numbrushsides + 6 > Defines.MAX_MAP_BRUSHSIDES
		    || CM.numplanes + 12 > Defines.MAX_MAP_PLANES)
			Com.Error(Defines.ERR_DROP, "Not enough room for box tree");

		CM.box_brush = CM.map_brushes[CM.numbrushes];
		CM.box_brush.numsides = 6;
		CM.box_brush.firstbrushside = CM.numbrushsides;
		CM.box_brush.contents = Defines.CONTENTS_MONSTER;

		CM.box_leaf = CM.map_leafs[CM.numleafs];
		CM.box_leaf.contents = Defines.CONTENTS_MONSTER;
		CM.box_leaf.firstleafbrush = (short)CM.numleafbrushes;
		CM.box_leaf.numleafbrushes = 1;

		CM.map_leafbrushes[CM.numleafbrushes] = CM.numbrushes;

		int side;
		cnode_t c;
		cplane_t p;
		cbrushside_t s;

		for (var i = 0; i < 6; i++)
		{
			side = i & 1;

			// brush sides
			s = CM.map_brushsides[CM.numbrushsides + i];
			s.plane = CM.map_planes[CM.numplanes + i * 2 + side];
			s.surface = CM.nullsurface;

			// nodes
			c = CM.map_nodes[CM.box_headnode + i];
			c.plane = CM.map_planes[CM.numplanes + i * 2];
			c.children[side] = -1 - CM.emptyleaf;

			if (i != 5)
				c.children[side ^ 1] = CM.box_headnode + i + 1;
			else
				c.children[side ^ 1] = -1 - CM.numleafs;

			// planes
			p = CM.box_planes[i * 2];
			p.type = (byte)(i >> 1);
			p.signbits = 0;
			Math3D.VectorClear(p.normal);
			p.normal[i >> 1] = 1;

			p = CM.box_planes[i * 2 + 1];
			p.type = (byte)(3 + (i >> 1));
			p.signbits = 0;
			Math3D.VectorClear(p.normal);
			p.normal[i >> 1] = -1;
		}
	}

	/** To keep everything totally uniform, bounding boxes are turned into small
     * BSP trees instead of being compared directly. */
	public static int HeadnodeForBox(float[] mins, float[] maxs)
	{
		CM.box_planes[0].dist = maxs[0];
		CM.box_planes[1].dist = -maxs[0];
		CM.box_planes[2].dist = mins[0];
		CM.box_planes[3].dist = -mins[0];
		CM.box_planes[4].dist = maxs[1];
		CM.box_planes[5].dist = -maxs[1];
		CM.box_planes[6].dist = mins[1];
		CM.box_planes[7].dist = -mins[1];
		CM.box_planes[8].dist = maxs[2];
		CM.box_planes[9].dist = -maxs[2];
		CM.box_planes[10].dist = mins[2];
		CM.box_planes[11].dist = -mins[2];

		return CM.box_headnode;
	}

	/** Recursively searches the leaf number that contains the 3d point. */
	private static int CM_PointLeafnum_r(float[] p, int num)
	{
		float d;
		cnode_t node;
		cplane_t plane;

		while (num >= 0)
		{
			node = CM.map_nodes[num];
			plane = node.plane;

			if (plane.type < 3)
				d = p[plane.type] - plane.dist;
			else
				d = Math3D.DotProduct(plane.normal, p) - plane.dist;

			if (d < 0)
				num = node.children[1];
			else
				num = node.children[0];
		}

		Globals.c_pointcontents++; // optimize counter

		return -1 - num;
	}

	/** Searches the leaf number that contains the 3d point. */
	public static int CM_PointLeafnum(float[] p)
	{
		// sound may call this without map loaded
		if (CM.numplanes == 0)
			return 0;

		return CM.CM_PointLeafnum_r(p, 0);
	}

	private static int leaf_count, leaf_maxcount;
	private static int[] leaf_list;
	private static float[] leaf_mins, leaf_maxs;
	private static int leaf_topnode;

	/** Recursively fills in a list of all the leafs touched. */
	private static void CM_BoxLeafnums_r(int nodenum)
	{
		cplane_t plane;
		cnode_t node;
		int s;

		while (true)
		{
			if (nodenum < 0)
			{
				if (CM.leaf_count >= CM.leaf_maxcount)
				{
					Com.DPrintf("CM_BoxLeafnums_r: overflow\n");

					return;
				}

				CM.leaf_list[CM.leaf_count++] = -1 - nodenum;

				return;
			}

			node = CM.map_nodes[nodenum];
			plane = node.plane;

			s = Math3D.BoxOnPlaneSide(CM.leaf_mins, CM.leaf_maxs, plane);

			if (s == 1)
				nodenum = node.children[0];
			else if (s == 2)
				nodenum = node.children[1];
			else
			{
				// go down both
				if (CM.leaf_topnode == -1)
					CM.leaf_topnode = nodenum;

				CM.CM_BoxLeafnums_r(node.children[0]);
				nodenum = node.children[1];
			}
		}
	}

	/** Fills in a list of all the leafs touched and starts with the head node. */
	private static int CM_BoxLeafnums_headnode(float[] mins, float[] maxs, int[] list, int listsize, int headnode, int[] topnode)
	{
		CM.leaf_list = list;
		CM.leaf_count = 0;
		CM.leaf_maxcount = listsize;
		CM.leaf_mins = mins;
		CM.leaf_maxs = maxs;

		CM.leaf_topnode = -1;

		CM.CM_BoxLeafnums_r(headnode);

		if (topnode != null)
			topnode[0] = CM.leaf_topnode;

		return CM.leaf_count;
	}

	/** Fills in a list of all the leafs touched. */
	public static int CM_BoxLeafnums(float[] mins, float[] maxs, int[] list, int listsize, int[] topnode)
	{
		return CM.CM_BoxLeafnums_headnode(mins, maxs, list, listsize, CM.map_cmodels[0].headnode, topnode);
	}

	/** Returns a tag that describes the content of the point. */
	public static int PointContents(float[] p, int headnode)
	{
		int l;

		if (CM.numnodes == 0) // map not loaded
			return 0;

		l = CM.CM_PointLeafnum_r(p, headnode);

		return CM.map_leafs[l].contents;
	}

	/*
	 * ================== CM_TransformedPointContents
	 * 
	 * Handles offseting and rotation of the end points for moving and rotating
	 * entities ==================
	 */
	public static int TransformedPointContents(float[] p, int headnode, float[] origin, float[] angles)
	{
		float[] p_l = { 0, 0, 0 };
		float[] temp = { 0, 0, 0 };
		float[] forward = { 0, 0, 0 }, right = { 0, 0, 0 }, up = { 0, 0, 0 };
		int l;

		// subtract origin offset
		Math3D.VectorSubtract(p, origin, p_l);

		// rotate start and end into the models frame of reference
		if (headnode != CM.box_headnode && (angles[0] != 0 || angles[1] != 0 || angles[2] != 0))
		{
			Math3D.AngleVectors(angles, forward, right, up);

			Math3D.VectorCopy(p_l, temp);
			p_l[0] = Math3D.DotProduct(temp, forward);
			p_l[1] = -Math3D.DotProduct(temp, right);
			p_l[2] = Math3D.DotProduct(temp, up);
		}

		l = CM.CM_PointLeafnum_r(p_l, headnode);

		return CM.map_leafs[l].contents;
	}

	/*
	 * ===============================================================================
	 * 
	 * BOX TRACING
	 * 
	 * ===============================================================================
	 */

	// 1/32 epsilon to keep floating point happy
	private static readonly float DIST_EPSILON = 0.03125f;
	private static readonly float[] trace_start = { 0, 0, 0 };
	private static readonly float[] trace_end = { 0, 0, 0 };
	private static readonly float[] trace_mins = { 0, 0, 0 };
	private static readonly float[] trace_maxs = { 0, 0, 0 };
	private static readonly float[] trace_extents = { 0, 0, 0 };
	private static trace_t trace_trace = new();
	private static int trace_contents;
	private static bool trace_ispoint; // optimized case

	/*
	 * ================ CM_ClipBoxToBrush ================
	 */
	public static void CM_ClipBoxToBrush(float[] mins, float[] maxs, float[] p1, float[] p2, trace_t trace, cbrush_t brush)
	{
		int i, j;
		cplane_t plane, clipplane;
		float dist;
		float enterfrac, leavefrac;
		float[] ofs = { 0, 0, 0 };
		float d1, d2;
		bool getout, startout;
		float f;
		cbrushside_t side, leadside;

		enterfrac = -1;
		leavefrac = 1;
		clipplane = null;

		if (brush.numsides == 0)
			return;

		Globals.c_brush_traces++;

		getout = false;
		startout = false;
		leadside = null;

		for (i = 0; i < brush.numsides; i++)
		{
			side = CM.map_brushsides[brush.firstbrushside + i];
			plane = side.plane;

			// FIXME: special case for axial

			if (!CM.trace_ispoint)
			{
				// general box case

				// push the plane out apropriately for mins/maxs

				// FIXME: use signbits into 8 way lookup for each mins/maxs
				for (j = 0; j < 3; j++)
				{
					if (plane.normal[j] < 0)
						ofs[j] = maxs[j];
					else
						ofs[j] = mins[j];
				}

				dist = Math3D.DotProduct(ofs, plane.normal);
				dist = plane.dist - dist;
			}
			else
			{
				// special point case
				dist = plane.dist;
			}

			d1 = Math3D.DotProduct(p1, plane.normal) - dist;
			d2 = Math3D.DotProduct(p2, plane.normal) - dist;

			if (d2 > 0)
				getout = true; // endpoint is not in solid

			if (d1 > 0)
				startout = true;

			// if completely in front of face, no intersection
			if (d1 > 0 && d2 >= d1)
				return;

			if (d1 <= 0 && d2 <= 0)
				continue;

			// crosses face
			if (d1 > d2)
			{
				// enter
				f = (d1 - CM.DIST_EPSILON) / (d1 - d2);

				if (f > enterfrac)
				{
					enterfrac = f;
					clipplane = plane;
					leadside = side;
				}
			}
			else
			{
				// leave
				f = (d1 + CM.DIST_EPSILON) / (d1 - d2);

				if (f < leavefrac)
					leavefrac = f;
			}
		}

		if (!startout)
		{
			// original point was inside brush
			trace.startsolid = true;

			if (!getout)
				trace.allsolid = true;

			return;
		}

		if (enterfrac < leavefrac)
		{
			if (enterfrac > -1 && enterfrac < trace.fraction)
			{
				if (enterfrac < 0)
					enterfrac = 0;

				trace.fraction = enterfrac;

				// copy
				trace.plane.set(clipplane);
				trace.surface = leadside.surface.c;
				trace.contents = brush.contents;
			}
		}
	}

	/*
	 * ================ CM_TestBoxInBrush ================
	 */
	public static void CM_TestBoxInBrush(float[] mins, float[] maxs, float[] p1, trace_t trace, cbrush_t brush)
	{
		int i, j;
		cplane_t plane;
		float dist;
		float[] ofs = { 0, 0, 0 };
		float d1;
		cbrushside_t side;

		if (brush.numsides == 0)
			return;

		for (i = 0; i < brush.numsides; i++)
		{
			side = CM.map_brushsides[brush.firstbrushside + i];
			plane = side.plane;

			// FIXME: special case for axial
			// general box case
			// push the plane out apropriately for mins/maxs
			// FIXME: use signbits into 8 way lookup for each mins/maxs

			for (j = 0; j < 3; j++)
			{
				if (plane.normal[j] < 0)
					ofs[j] = maxs[j];
				else
					ofs[j] = mins[j];
			}

			dist = Math3D.DotProduct(ofs, plane.normal);
			dist = plane.dist - dist;

			d1 = Math3D.DotProduct(p1, plane.normal) - dist;

			// if completely in front of face, no intersection
			if (d1 > 0)
				return;
		}

		// inside this brush
		trace.startsolid = trace.allsolid = true;
		trace.fraction = 0;
		trace.contents = brush.contents;
	}

	/**
     * CM_TraceToLeaf.
     */
	public static void CM_TraceToLeaf(int leafnum)
	{
		int k;
		int brushnum;
		cleaf_t leaf;
		cbrush_t b;

		leaf = CM.map_leafs[leafnum];

		if (0 == (leaf.contents & CM.trace_contents))
			return;

		// trace line against all brushes in the leaf
		for (k = 0; k < leaf.numleafbrushes; k++)
		{
			brushnum = CM.map_leafbrushes[leaf.firstleafbrush + k];
			b = CM.map_brushes[brushnum];

			if (b.checkcount == CM.checkcount)
				continue; // already checked this brush in another leaf

			b.checkcount = CM.checkcount;

			if (0 == (b.contents & CM.trace_contents))
				continue;

			CM.CM_ClipBoxToBrush(CM.trace_mins, CM.trace_maxs, CM.trace_start, CM.trace_end, CM.trace_trace, b);

			if (0 == CM.trace_trace.fraction)
				return;
		}
	}

	/*
	 * ================ CM_TestInLeaf ================
	 */
	public static void CM_TestInLeaf(int leafnum)
	{
		int k;
		int brushnum;
		cleaf_t leaf;
		cbrush_t b;

		leaf = CM.map_leafs[leafnum];

		if (0 == (leaf.contents & CM.trace_contents))
			return;

		// trace line against all brushes in the leaf
		for (k = 0; k < leaf.numleafbrushes; k++)
		{
			brushnum = CM.map_leafbrushes[leaf.firstleafbrush + k];
			b = CM.map_brushes[brushnum];

			if (b.checkcount == CM.checkcount)
				continue; // already checked this brush in another leaf

			b.checkcount = CM.checkcount;

			if (0 == (b.contents & CM.trace_contents))
				continue;

			CM.CM_TestBoxInBrush(CM.trace_mins, CM.trace_maxs, CM.trace_start, CM.trace_trace, b);

			if (0 == CM.trace_trace.fraction)
				return;
		}
	}

	/*
	 * ================== CM_RecursiveHullCheck ==================
	 */
	public static void CM_RecursiveHullCheck(int num, float p1f, float p2f, float[] p1, float[] p2)
	{
		cnode_t node;
		cplane_t plane;
		float t1, t2, offset;
		float frac, frac2;
		float idist;
		int i;
		int side;
		float midf;

		if (CM.trace_trace.fraction <= p1f)
			return; // already hit something nearer

		// if < 0, we are in a leaf node
		if (num < 0)
		{
			CM.CM_TraceToLeaf(-1 - num);

			return;
		}

		//
		// find the point distances to the seperating plane
		// and the offset for the size of the box
		//
		node = CM.map_nodes[num];
		plane = node.plane;

		if (plane.type < 3)
		{
			t1 = p1[plane.type] - plane.dist;
			t2 = p2[plane.type] - plane.dist;
			offset = CM.trace_extents[plane.type];
		}
		else
		{
			t1 = Math3D.DotProduct(plane.normal, p1) - plane.dist;
			t2 = Math3D.DotProduct(plane.normal, p2) - plane.dist;

			if (CM.trace_ispoint)
				offset = 0;
			else
			{
				offset = Math.Abs(CM.trace_extents[0] * plane.normal[0])
					+ Math.Abs(CM.trace_extents[1] * plane.normal[1])
					+ Math.Abs(CM.trace_extents[2] * plane.normal[2]);
			}
		}

		// see which sides we need to consider
		if (t1 >= offset && t2 >= offset)
		{
			CM.CM_RecursiveHullCheck(node.children[0], p1f, p2f, p1, p2);

			return;
		}

		if (t1 < -offset && t2 < -offset)
		{
			CM.CM_RecursiveHullCheck(node.children[1], p1f, p2f, p1, p2);

			return;
		}

		// put the crosspoint DIST_EPSILON pixels on the near side
		if (t1 < t2)
		{
			idist = 1.0f / (t1 - t2);
			side = 1;
			frac2 = (t1 + offset + CM.DIST_EPSILON) * idist;
			frac = (t1 - offset + CM.DIST_EPSILON) * idist;
		}
		else if (t1 > t2)
		{
			idist = 1.0f / (t1 - t2);
			side = 0;
			frac2 = (t1 - offset - CM.DIST_EPSILON) * idist;
			frac = (t1 + offset + CM.DIST_EPSILON) * idist;
		}
		else
		{
			side = 0;
			frac = 1;
			frac2 = 0;
		}

		// move up to the node
		if (frac < 0)
			frac = 0;

		if (frac > 1)
			frac = 1;

		midf = p1f + (p2f - p1f) * frac;
		var mid = new float[3];

		for (i = 0; i < 3; i++)
			mid[i] = p1[i] + frac * (p2[i] - p1[i]);

		CM.CM_RecursiveHullCheck(node.children[side], p1f, midf, p1, mid);

		// go past the node
		if (frac2 < 0)
			frac2 = 0;

		if (frac2 > 1)
			frac2 = 1;

		midf = p1f + (p2f - p1f) * frac2;

		for (i = 0; i < 3; i++)
			mid[i] = p1[i] + frac2 * (p2[i] - p1[i]);

		CM.CM_RecursiveHullCheck(node.children[side ^ 1], midf, p2f, mid, p2);
	}

	//======================================================================

	/*
	 * ================== CM_BoxTrace ==================
	 */
	public static trace_t BoxTrace(float[] start, float[] end, float[] mins, float[] maxs, int headnode, int brushmask)
	{
		// for multi-check avoidance
		CM.checkcount++;

		// for statistics, may be zeroed
		Globals.c_traces++;

		// fill in a default trace
		//was: memset(& trace_trace, 0, sizeof(trace_trace));
		CM.trace_trace = new();

		CM.trace_trace.fraction = 1;
		CM.trace_trace.surface = CM.nullsurface.c;

		if (CM.numnodes == 0)
		{
			// map not loaded
			return CM.trace_trace;
		}

		CM.trace_contents = brushmask;
		Math3D.VectorCopy(start, CM.trace_start);
		Math3D.VectorCopy(end, CM.trace_end);
		Math3D.VectorCopy(mins, CM.trace_mins);
		Math3D.VectorCopy(maxs, CM.trace_maxs);

		//
		// check for position test special case
		//
		if (start[0] == end[0] && start[1] == end[1] && start[2] == end[2])
		{
			var leafs = new int[1024];
			int i, numleafs;
			float[] c1 = { 0, 0, 0 }, c2 = { 0, 0, 0 };
			var topnode = 0;

			Math3D.VectorAdd(start, mins, c1);
			Math3D.VectorAdd(start, maxs, c2);

			for (i = 0; i < 3; i++)
			{
				c1[i] -= 1;
				c2[i] += 1;
			}

			int[] tn = { topnode };

			numleafs = CM.CM_BoxLeafnums_headnode(c1, c2, leafs, 1024, headnode, tn);
			topnode = tn[0];

			for (i = 0; i < numleafs; i++)
			{
				CM.CM_TestInLeaf(leafs[i]);

				if (CM.trace_trace.allsolid)
					break;
			}

			Math3D.VectorCopy(start, CM.trace_trace.endpos);

			return CM.trace_trace;
		}

		//
		// check for point special case
		//
		if (mins[0] == 0 && mins[1] == 0 && mins[2] == 0 && maxs[0] == 0 && maxs[1] == 0 && maxs[2] == 0)
		{
			CM.trace_ispoint = true;
			Math3D.VectorClear(CM.trace_extents);
		}
		else
		{
			CM.trace_ispoint = false;
			CM.trace_extents[0] = -mins[0] > maxs[0] ? -mins[0] : maxs[0];
			CM.trace_extents[1] = -mins[1] > maxs[1] ? -mins[1] : maxs[1];
			CM.trace_extents[2] = -mins[2] > maxs[2] ? -mins[2] : maxs[2];
		}

		//
		// general sweeping through world
		//
		CM.CM_RecursiveHullCheck(headnode, 0, 1, start, end);

		if (CM.trace_trace.fraction == 1)
			Math3D.VectorCopy(end, CM.trace_trace.endpos);
		else
		{
			for (var i = 0; i < 3; i++)
				CM.trace_trace.endpos[i] = start[i] + CM.trace_trace.fraction * (end[i] - start[i]);
		}

		return CM.trace_trace;
	}

	/**
     * CM_TransformedBoxTrace handles offseting and rotation of the end points for moving and rotating
     * entities.
     */
	public static trace_t TransformedBoxTrace(
		float[] start,
		float[] end,
		float[] mins,
		float[] maxs,
		int headnode,
		int brushmask,
		float[] origin,
		float[] angles
	)
	{
		trace_t trace;
		float[] start_l = { 0, 0, 0 }, end_l = { 0, 0, 0 };
		float[] a = { 0, 0, 0 };
		float[] forward = { 0, 0, 0 }, right = { 0, 0, 0 }, up = { 0, 0, 0 };
		float[] temp = { 0, 0, 0 };
		bool rotated;

		// subtract origin offset
		Math3D.VectorSubtract(start, origin, start_l);
		Math3D.VectorSubtract(end, origin, end_l);

		// rotate start and end into the models frame of reference
		if (headnode != CM.box_headnode && (angles[0] != 0 || angles[1] != 0 || angles[2] != 0))
			rotated = true;
		else
			rotated = false;

		if (rotated)
		{
			Math3D.AngleVectors(angles, forward, right, up);

			Math3D.VectorCopy(start_l, temp);
			start_l[0] = Math3D.DotProduct(temp, forward);
			start_l[1] = -Math3D.DotProduct(temp, right);
			start_l[2] = Math3D.DotProduct(temp, up);

			Math3D.VectorCopy(end_l, temp);
			end_l[0] = Math3D.DotProduct(temp, forward);
			end_l[1] = -Math3D.DotProduct(temp, right);
			end_l[2] = Math3D.DotProduct(temp, up);
		}

		// sweep the box through the model
		trace = CM.BoxTrace(start_l, end_l, mins, maxs, headnode, brushmask);

		if (rotated && trace.fraction != 1.0)
		{
			// FIXME: figure out how to do this with existing angles
			Math3D.VectorNegate(angles, a);
			Math3D.AngleVectors(a, forward, right, up);

			Math3D.VectorCopy(trace.plane.normal, temp);
			trace.plane.normal[0] = Math3D.DotProduct(temp, forward);
			trace.plane.normal[1] = -Math3D.DotProduct(temp, right);
			trace.plane.normal[2] = Math3D.DotProduct(temp, up);
		}

		trace.endpos[0] = start[0] + trace.fraction * (end[0] - start[0]);
		trace.endpos[1] = start[1] + trace.fraction * (end[1] - start[1]);
		trace.endpos[2] = start[2] + trace.fraction * (end[2] - start[2]);

		return trace;
	}

	/*
	 * ===============================================================================
	 * PVS / PHS
	 * ===============================================================================
	 */

	/*
	 * =================== CM_DecompressVis ===================
	 */
	public static void CM_DecompressVis(byte[] @in, int offset, byte[] @out)
	{
		int c;

		int row;

		row = (CM.numclusters + 7) >> 3;
		var outp = 0;
		var inp = offset;

		if (@in == null || CM.numvisibility == 0)
		{
			// no vis info, so make all
			// visible
			while (row != 0)
			{
				@out[outp++] = (byte)0xFF;
				row--;
			}

			return;
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

			if (outp + c > row)
			{
				c = row - outp;
				Com.DPrintf("warning: Vis decompression overrun\n");
			}

			while (c != 0)
			{
				@out[outp++] = 0;
				c--;
			}
		}
		while (outp < row);
	}

	public static byte[] pvsrow = new byte[Defines.MAX_MAP_LEAFS / 8];
	public static byte[] phsrow = new byte[Defines.MAX_MAP_LEAFS / 8];

	public static byte[] CM_ClusterPVS(int cluster)
	{
		if (cluster == -1)
			Array.Fill(CM.pvsrow, (byte)0, 0, (CM.numclusters + 7) >> 3);
		else
			CM.CM_DecompressVis(CM.map_visibility, CM.map_vis.bitofs[cluster][Defines.DVIS_PVS], CM.pvsrow);

		return CM.pvsrow;
	}

	public static byte[] CM_ClusterPHS(int cluster)
	{
		if (cluster == -1)
			Array.Fill(CM.phsrow, (byte)0, 0, (CM.numclusters + 7) >> 3);
		else
			CM.CM_DecompressVis(CM.map_visibility, CM.map_vis.bitofs[cluster][Defines.DVIS_PHS], CM.phsrow);

		return CM.phsrow;
	}

	/*
	 * ===============================================================================
	 * AREAPORTALS
	 * ===============================================================================
	 */

	public static void FloodArea_r(carea_t area, int floodnum)
	{
		//Com.Printf("FloodArea_r(" + floodnum + ")...\n");
		int i;
		dareaportal_t p;

		if (area.floodvalid == CM.floodvalid)
		{
			if (area.floodnum == floodnum)
				return;

			Com.Error(Defines.ERR_DROP, "FloodArea_r: reflooded");
		}

		area.floodnum = floodnum;
		area.floodvalid = CM.floodvalid;

		for (i = 0; i < area.numareaportals; i++)
		{
			p = CM.map_areaportals[area.firstareaportal + i];

			if (CM.portalopen[p.portalnum])
				CM.FloodArea_r(CM.map_areas[p.otherarea], floodnum);
		}
	}

	/**
     * FloodAreaConnections.
     */
	public static void FloodAreaConnections()
	{
		Com.DPrintf("FloodAreaConnections...\n");

		int i;
		carea_t area;
		int floodnum;

		// all current floods are now invalid
		CM.floodvalid++;
		floodnum = 0;

		// area 0 is not used
		for (i = 1; i < CM.numareas; i++)
		{
			area = CM.map_areas[i];

			if (area.floodvalid == CM.floodvalid)
				continue; // already flooded into

			floodnum++;
			CM.FloodArea_r(area, floodnum);
		}
	}

	/**
     * CM_SetAreaPortalState.
     */
	public static void CM_SetAreaPortalState(int portalnum, bool open)
	{
		if (portalnum > CM.numareaportals)
			Com.Error(Defines.ERR_DROP, "areaportal > numareaportals");

		CM.portalopen[portalnum] = open;
		CM.FloodAreaConnections();
	}

	/**
     * CM_AreasConnected returns true, if two areas are connected.
     */
	public static bool CM_AreasConnected(int area1, int area2)
	{
		if (CM.map_noareas.value != 0)
			return true;

		if (area1 > CM.numareas || area2 > CM.numareas)
			Com.Error(Defines.ERR_DROP, "area > numareas");

		if (CM.map_areas[area1].floodnum == CM.map_areas[area2].floodnum)
			return true;

		return false;
	}

	/**
     * CM_WriteAreaBits writes a length byte followed by a bit vector of all the areas that area
     * in the same flood as the area parameter
     * 
     * This is used by the client refreshes to cull visibility.
     */
	public static int CM_WriteAreaBits(byte[] buffer, int area)
	{
		int i;
		int floodnum;
		int bytes;

		bytes = (CM.numareas + 7) >> 3;

		if (CM.map_noareas.value != 0)
		{
			// for debugging, send everything
			Array.Fill(buffer, (byte)255, 0, bytes);
		}
		else
		{
			Array.Fill(buffer, (byte)0, 0, bytes);
			floodnum = CM.map_areas[area].floodnum;

			for (i = 0; i < CM.numareas; i++)
			{
				if (CM.map_areas[i].floodnum == floodnum || area == 0)
					buffer[i >> 3] |= (byte)(1 << (i & 7));
			}
		}

		return bytes;
	}

	/**
     * CM_WritePortalState writes the portal state to a savegame file.
     */
	public static void CM_WritePortalState(BinaryWriter os)
	{
		try
		{
			for (var n = 0; n < CM.portalopen.Length; n++)
			{
				if (CM.portalopen[n])
					os.Write(1);
				else
					os.Write(0);
			}
		}
		catch (Exception e)
		{
			Com.Printf("ERROR:" + e);
			Console.WriteLine(e);
		}
	}

	/**
     * CM_ReadPortalState reads the portal state from a savegame file and recalculates the area
     * connections.
     */
	public static void CM_ReadPortalState(BinaryReader f)
	{
		//was: FS_Read(portalopen, sizeof(portalopen), f);
		var len = CM.portalopen.Length * 4;

		var buf = new byte[len];
		f.Read(buf);

		BinaryReader bb = new(new MemoryStream(buf));

		for (var n = 0; n < CM.portalopen.Length; n++)
			CM.portalopen[n] = bb.ReadInt32() != 0;

		CM.FloodAreaConnections();
	}

	/**
     * CM_HeadnodeVisible returns true if any leaf under headnode has a cluster that is potentially
     * visible.
     */
	public static bool CM_HeadnodeVisible(int nodenum, byte[] visbits)
	{
		if (nodenum < 0)
		{
			var leafnum = -1 - nodenum;
			var cluster = CM.map_leafs[leafnum].cluster;

			if (cluster == -1)
				return false;

			if (0 != (visbits[(uint)cluster >> 3] & (1 << (cluster & 7))))
				return true;

			return false;
		}

		var node = CM.map_nodes[nodenum];

		if (CM.CM_HeadnodeVisible(node.children[0], visbits))
			return true;

		return CM.CM_HeadnodeVisible(node.children[1], visbits);
	}

	static CM()
	{
		CM.CM1();
		CM.CM2();
		CM.CM3();
		CM.CM4();
		CM.CM5();
		CM.CM6();
		CM.CM7();
		CM.CM8();
		CM.CM9();
	}
}