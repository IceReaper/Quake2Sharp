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

namespace Quake2Sharp.qcommon
{
	using Quake2Sharp;
	using System.IO;
	using System.Text;

	/**
 * qfiles
 * 
 * @author cwei
 */
	public class qfiles
	{
		//
		// qfiles.h: quake file formats
		// This file must be identical in the quake and utils directories
		//

		/*
		========================================================================
		
		The .pak files are just a linear collapse of a directory tree
		
		========================================================================
		*/

		/*
		========================================================================
		
		PCX files are used for as many images as possible
		
		========================================================================
		*/
		public class pcx_t
		{
			// size of byte arrays
			private static readonly int PALETTE_SIZE = 48;
			private static readonly int FILLER_SIZE = 58;

			public byte manufacturer;
			public byte version;
			public byte encoding;
			public byte bits_per_pixel;
			public int xmin, ymin, xmax, ymax; // unsigned short
			public int hres, vres; // unsigned short
			public byte[] palette; //unsigned byte; size 48
			public byte reserved;
			public byte color_planes;
			public int bytes_per_line; // unsigned short
			public int palette_type; // unsigned short
			public byte[] filler; // size 58
			public byte[] data; //unbounded data

			public pcx_t(byte[] dataBytes)
				: this(new BinaryReader(new MemoryStream(dataBytes)))
			{
			}

			public pcx_t(BinaryReader b)
			{
				// fill header
				this.manufacturer = b.ReadByte();
				this.version = b.ReadByte();
				this.encoding = b.ReadByte();
				this.bits_per_pixel = b.ReadByte();
				this.xmin = b.ReadUInt16();
				this.ymin = b.ReadUInt16();
				this.xmax = b.ReadUInt16();
				this.ymax = b.ReadUInt16();
				this.hres = b.ReadUInt16();
				this.vres = b.ReadUInt16();
				b.Read(this.palette = new byte[pcx_t.PALETTE_SIZE]);
				this.reserved = b.ReadByte();
				this.color_planes = b.ReadByte();
				this.bytes_per_line = b.ReadUInt16();
				this.palette_type = b.ReadUInt16();
				b.Read(this.filler = new byte[pcx_t.FILLER_SIZE]);

				// fill data
				this.data = b.ReadBytes((int) (b.BaseStream.Length - b.BaseStream.Position));
			}
		}

		/*
		========================================================================
		
		TGA files are used for sky planes
		
		========================================================================
		*/
		public class tga_t
		{
			// targa header
			public int id_length, colormap_type, image_type; // unsigned char
			public int colormap_index, colormap_length; // unsigned short
			public int colormap_size; // unsigned char
			public int x_origin, y_origin, width, height; // unsigned short
			public int pixel_size, attributes; // unsigned char

			public byte[] data; // (un)compressed data

			public tga_t(byte[] dataBytes)
				: this(new BinaryReader(new MemoryStream(dataBytes)))
			{
			}

			public tga_t(BinaryReader b)
			{
				// fill header
				this.id_length = b.ReadByte();
				this.colormap_type = b.ReadByte();
				this.image_type = b.ReadByte();
				this.colormap_index = b.ReadUInt16();
				this.colormap_length = b.ReadUInt16();
				this.colormap_size = b.ReadByte();
				this.x_origin = b.ReadUInt16();
				this.y_origin = b.ReadUInt16();
				this.width = b.ReadUInt16();
				this.height = b.ReadUInt16();
				this.pixel_size = b.ReadByte();

				// fill data
				this.data = b.ReadBytes((int) (b.BaseStream.Length - b.BaseStream.Position));
			}
		}

		/*
		========================================================================
		
		.MD2 triangle model file format
		
		========================================================================
		*/

		public const int IDALIASHEADER = (('2' << 24) + ('P' << 16) + ('D' << 8) + 'I');
		public static readonly int ALIAS_VERSION = 8;

		public static readonly int MAX_TRIANGLES = 4096;
		public static readonly int MAX_VERTS = 2048;
		public static readonly int MAX_FRAMES = 512;
		public static readonly int MAX_MD2SKINS = 32;
		public static readonly int MAX_SKINNAME = 64;

		public class dstvert_t
		{
			public short s;
			public short t;

			public dstvert_t(BinaryReader b)
			{
				this.s = b.ReadInt16();
				this.t = b.ReadInt16();
			}
		}

		public class dtriangle_t
		{
			public short[] index_xyz = {0, 0, 0};
			public short[] index_st = {0, 0, 0};

			public dtriangle_t(BinaryReader b)
			{
				this.index_xyz[0] = b.ReadInt16();
				this.index_xyz[1] = b.ReadInt16();
				this.index_xyz[2] = b.ReadInt16();

				this.index_st[0] = b.ReadInt16();
				this.index_st[1] = b.ReadInt16();
				this.index_st[2] = b.ReadInt16();
			}
		}

		public static readonly int DTRIVERTX_V0 = 0;
		public static readonly int DTRIVERTX_V1 = 1;
		public static readonly int DTRIVERTX_V2 = 2;
		public static readonly int DTRIVERTX_LNI = 3;
		public static readonly int DTRIVERTX_SIZE = 4;

		public class daliasframe_t
		{
			public float[] scale = {0, 0, 0}; // multiply byte verts by this
			public float[] translate = {0, 0, 0}; // then add this
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
				this.name = Encoding.ASCII.GetString(nameBuf).Split((char) 0)[0];
			}
		}

		//	   the glcmd format:
		//	   a positive integer starts a tristrip command, followed by that many
		//	   vertex structures.
		//	   a negative integer starts a trifan command, followed by -x vertexes
		//	   a zero indicates the end of the command list.
		//	   a vertex consists of a floating point s, a floating point t,
		//	   and an integer vertex index.

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

			// wird extra gebraucht
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

		/*
		========================================================================
		
		.SP2 sprite file format
		
		========================================================================
		*/
		// little-endian "IDS2"
		public const int IDSPRITEHEADER = (('2' << 24) + ('S' << 16) + ('D' << 8) + 'I');
		public static readonly int SPRITE_VERSION = 2;

		public class dsprframe_t
		{
			public int width, height;
			public int origin_x, origin_y; // raster coordinates inside pic
			public string name; // name of pcx file (MAX_SKINNAME)

			public dsprframe_t(BinaryReader b)
			{
				this.width = b.ReadInt32();
				this.height = b.ReadInt32();
				this.origin_x = b.ReadInt32();
				this.origin_y = b.ReadInt32();

				var nameBuf = new byte[qfiles.MAX_SKINNAME];
				b.Read(nameBuf);
				this.name = Encoding.ASCII.GetString(nameBuf).Split((char) 0)[0];
			}
		}

		public class dsprite_t
		{
			public int ident;
			public int version;
			public int numframes;
			public dsprframe_t[] frames; // variable sized

			public dsprite_t(BinaryReader b)
			{
				this.ident = b.ReadInt32();
				this.version = b.ReadInt32();
				this.numframes = b.ReadInt32();

				this.frames = new dsprframe_t[this.numframes];

				for (var i = 0; i < this.numframes; i++)
				{
					this.frames[i] = new dsprframe_t(b);
				}
			}
		}

		/*
		==============================================================================
		
		  .WAL texture file format
		
		==============================================================================
		*/
		public class miptex_t
		{
			private static readonly int MIPLEVELS = 4;
			private static readonly int NAME_SIZE = 32;

			public string name; // char name[32];
			public int width, height;

			public int[] offsets = new int[miptex_t.MIPLEVELS]; // 4 mip maps stored

			// next frame in animation chain
			public string animname; //	char	animname[32];
			public int flags;
			public int contents;
			public int value;

			public miptex_t(byte[] dataBytes)
				: this(new BinaryReader(new MemoryStream(dataBytes)))
			{
			}

			public miptex_t(BinaryReader b)
			{
				var nameBuf = new byte[miptex_t.NAME_SIZE];

				// fill header
				b.Read(nameBuf);
				this.name = Encoding.ASCII.GetString(nameBuf).Split((char) 0)[0];
				this.width = b.ReadInt32();
				this.height = b.ReadInt32();
				this.offsets[0] = b.ReadInt32();
				this.offsets[1] = b.ReadInt32();
				this.offsets[2] = b.ReadInt32();
				this.offsets[3] = b.ReadInt32();
				b.Read(nameBuf);
				this.animname = Encoding.ASCII.GetString(nameBuf).Split((char) 0)[0];
				this.flags = b.ReadInt32();
				this.contents = b.ReadInt32();
				this.value = b.ReadInt32();
			}
		}

		/*
		==============================================================================
		
		  .BSP file format
		
		==============================================================================
		*/

		public const int IDBSPHEADER = (('P' << 24) + ('S' << 16) + ('B' << 8) + 'I');

		// =============================================================================

		public class dheader_t
		{
			public dheader_t(BinaryReader bb)
			{
				this.ident = bb.ReadInt32();
				this.version = bb.ReadInt32();

				for (var n = 0; n < Defines.HEADER_LUMPS; n++)
					this.lumps[n] = new lump_t(bb.ReadInt32(), bb.ReadInt32());
			}

			public int ident;
			public int version;
			public lump_t[] lumps = new lump_t[Defines.HEADER_LUMPS];
		}

		public class dmodel_t
		{
			public dmodel_t(BinaryReader bb)
			{
				for (var j = 0; j < 3; j++)
					this.mins[j] = bb.ReadSingle();

				for (var j = 0; j < 3; j++)
					this.maxs[j] = bb.ReadSingle();

				for (var j = 0; j < 3; j++)
					this.origin[j] = bb.ReadSingle();

				this.headnode = bb.ReadInt32();
				this.firstface = bb.ReadInt32();
				this.numfaces = bb.ReadInt32();
			}

			public float[] mins = {0, 0, 0};
			public float[] maxs = {0, 0, 0};
			public float[] origin = {0, 0, 0}; // for sounds or lights
			public int headnode;

			public int firstface, numfaces; // submodels just draw faces

			// without walking the bsp tree

			public static int SIZE = 3 * 4 + 3 * 4 + 3 * 4 + 4 + 8;
		}

		public class dvertex_t
		{
			public static readonly int SIZE = 3 * 4; // 3 mal 32 bit float 

			public float[] point = {0, 0, 0};

			public dvertex_t(BinaryReader b)
			{
				this.point[0] = b.ReadSingle();
				this.point[1] = b.ReadSingle();
				this.point[2] = b.ReadSingle();
			}
		}

		// planes (x&~1) and (x&~1)+1 are always opposites
		public class dplane_t
		{
			public dplane_t(BinaryReader bb)
			{
				this.normal[0] = (bb.ReadSingle());
				this.normal[1] = (bb.ReadSingle());
				this.normal[2] = (bb.ReadSingle());

				this.dist = (bb.ReadSingle());
				this.type = (bb.ReadInt32());
			}

			public float[] normal = {0, 0, 0};
			public float dist;
			public int type; // PLANE_X - PLANE_ANYZ ?remove? trivial to regenerate

			public static readonly int SIZE = 3 * 4 + 4 + 4;
		}

		public class dnode_t
		{
			public dnode_t(BinaryReader bb)
			{
				this.planenum = bb.ReadInt32();

				this.children[0] = bb.ReadInt32();
				this.children[1] = bb.ReadInt32();

				for (var j = 0; j < 3; j++)
					this.mins[j] = bb.ReadInt16();

				for (var j = 0; j < 3; j++)
					this.maxs[j] = bb.ReadInt16();

				this.firstface = bb.ReadUInt16();
				this.numfaces = bb.ReadUInt16();
			}

			public int planenum;

			public int[] children = {0, 0};

			// negative numbers are -(leafs+1), not nodes
			public short[] mins = {0, 0, 0}; // for frustom culling
			public short[] maxs = {0, 0, 0};

			/*
			unsigned short	firstface;
			unsigned short	numfaces;	// counting both sides
			*/

			public int firstface;
			public int numfaces;

			public static int SIZE = 4 + 8 + 6 + 6 + 2 + 2; // counting both sides
		}

		// note that edge 0 is never used, because negative edge nums are used for
		// counterclockwise use of the edge in a face

		public class dedge_t
		{
			// unsigned short v[2];
			private int[] v = {0, 0};
		}

		public class dface_t
		{
			public static readonly int SIZE = 4 * Defines.SIZE_OF_SHORT + 2 * Defines.SIZE_OF_INT + Defines.MAXLIGHTMAPS;

			//unsigned short	planenum;
			public int planenum;
			public short side;

			public int firstedge; // we must support > 64k edges
			public short numedges;
			public short texinfo;

			// lighting info
			public byte[] styles = new byte[Defines.MAXLIGHTMAPS];
			public int lightofs; // start of [numstyles*surfsize] samples

			public dface_t(BinaryReader b)
			{
				this.planenum = b.ReadUInt16();
				this.side = b.ReadInt16();
				this.firstedge = b.ReadInt32();
				this.numedges = b.ReadInt16();
				this.texinfo = b.ReadInt16();
				b.Read(this.styles);
				this.lightofs = b.ReadInt32();
			}
		}

		public class dleaf_t
		{
			public dleaf_t(byte[] cmod_base, int i, int j)
				: this(new BinaryReader(new MemoryStream(cmod_base, i, j)))
			{
			}

			public dleaf_t(BinaryReader bb)
			{
				this.contents = bb.ReadInt32();
				this.cluster = bb.ReadInt16();
				this.area = bb.ReadInt16();

				this.mins[0] = bb.ReadInt16();
				this.mins[1] = bb.ReadInt16();
				this.mins[2] = bb.ReadInt16();

				this.maxs[0] = bb.ReadInt16();
				this.maxs[1] = bb.ReadInt16();
				this.maxs[2] = bb.ReadInt16();

				this.firstleafface = bb.ReadUInt16();
				this.numleaffaces = bb.ReadUInt16();

				this.firstleafbrush = bb.ReadUInt16();
				this.numleafbrushes = bb.ReadUInt16();
			}

			public static readonly int SIZE = 4 + 8 * 2 + 4 * 2;

			public int contents; // OR of all brushes (not needed?)

			public short cluster;
			public short area;

			public short[] mins = {0, 0, 0}; // for frustum culling
			public short[] maxs = {0, 0, 0};

			public int firstleafface; // unsigned short
			public int numleaffaces; // unsigned short

			public int firstleafbrush; // unsigned short
			public int numleafbrushes; // unsigned short
		}

		public class dbrushside_t
		{
			public dbrushside_t(BinaryReader bb)
			{
				this.planenum = bb.ReadUInt16();
				this.texinfo = bb.ReadInt16();
			}

			//unsigned short planenum;
			public int planenum; // facing out of the leaf

			public short texinfo;

			public static int SIZE = 4;
		}

		public class dbrush_t
		{
			public dbrush_t(BinaryReader bb)
			{
				this.firstside = bb.ReadInt32();
				this.numsides = bb.ReadInt32();
				this.contents = bb.ReadInt32();
			}

			public static int SIZE = 3 * 4;

			public int firstside;
			public int numsides;
			public int contents;
		}

		//	#define	ANGLE_UP	-1
		//	#define	ANGLE_DOWN	-2

		// the visibility lump consists of a header with a count, then
		// byte offsets for the PVS and PHS of each cluster, then the raw
		// compressed bit vectors
		// #define	DVIS_PVS	0
		// #define	DVIS_PHS	1

		public class dvis_t
		{
			public dvis_t(BinaryReader bb)
			{
				this.numclusters = bb.ReadInt32();
				this.bitofs = new int[this.numclusters][];

				for (var i = 0; i < this.numclusters; i++)
				{
					this.bitofs[i] = new int[2];
					this.bitofs[i][0] = bb.ReadInt32();
					this.bitofs[i][1] = bb.ReadInt32();
				}
			}

			public int numclusters;
			public int[][] bitofs = new int[8][]; // bitofs[numclusters][2]	
		}

		// each area has a list of portals that lead into other areas
		// when portals are closed, other areas may not be visible or
		// hearable even if the vis info says that it should be

		public class dareaportal_t
		{
			public dareaportal_t()
			{
			}

			public dareaportal_t(BinaryReader bb)
			{
				this.portalnum = bb.ReadInt32();
				this.otherarea = bb.ReadInt32();
			}

			public int portalnum;
			public int otherarea;

			public static int SIZE = 8;
		}

		public class darea_t
		{
			public darea_t(BinaryReader bb)
			{
				this.numareaportals = bb.ReadInt32();
				this.firstareaportal = bb.ReadInt32();
			}

			public int numareaportals;
			public int firstareaportal;

			public static int SIZE = 8;
		}
	}
}
