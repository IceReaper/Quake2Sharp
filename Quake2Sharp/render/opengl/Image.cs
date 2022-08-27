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
namespace Quake2Sharp.render.opengl;

using client;
using client.types;
using game.types;
using qcommon;
using qcommon.types;
using render.types;
using System;
using System.Drawing;
using System.IO;
using types;
using util;

public partial class OpenGLRenderApi
{
	protected image_t draw_chars;
	protected image_t[] gltextures = new image_t[Base.MAX_GLTEXTURES];

	//Map gltextures = new Hashtable(MAX_GLTEXTURES); // image_t
	protected int numgltextures;
	private readonly byte[] intensitytable = new byte[256];
	private readonly byte[] gammatable = new byte[256];
	private cvar_t intensity;

	//
	//	qboolean GL_Upload8 (byte *data, int width, int height,  qboolean mipmap, qboolean is_sky );
	//	qboolean GL_Upload32 (unsigned *data, int width, int height,  qboolean mipmap);
	//
	private readonly int gl_solid_format = 3;
	private readonly int gl_alpha_format = 4;
	protected int gl_tex_solid_format = 3;
	protected int gl_tex_alpha_format = 4;
	protected int gl_filter_min = OpenGL.GL_LINEAR_MIPMAP_NEAREST;
	protected int gl_filter_max = OpenGL.GL_LINEAR;

	protected void ImageConstructor()
	{
		// init the texture cache
		for (var i = 0; i < this.gltextures.Length; i++)
			this.gltextures[i] = new(i);

		this.numgltextures = 0;

		for (var i = 0; i < this.scrap_allocated.Length; i++)
			this.scrap_allocated[i] = new int[OpenGLRenderApi.IMG_BLOCK_WIDTH];

		for (var i = 0; i < this.scrap_texels.Length; i++)
			this.scrap_texels[i] = new byte[OpenGLRenderApi.IMG_BLOCK_WIDTH * OpenGLRenderApi.IMG_BLOCK_HEIGHT];
	}

	protected void GL_SetTexturePalette(int[] palette)
	{
		if (!(palette != null && palette.Length == 256))
			throw new("int palette[256] bug");

		if (this.qglColorTableEXT && this.gl_ext_palettedtexture.value != 0.0f)
		{
			var temptable = new byte[768];

			for (var i = 0; i < 256; i++)
			{
				temptable[i * 3 + 0] = (byte)((palette[i] >> 0) & 0xff);
				temptable[i * 3 + 1] = (byte)((palette[i] >> 8) & 0xff);
				temptable[i * 3 + 2] = (byte)((palette[i] >> 16) & 0xff);
			}

			this.gl.glColorTable(OpenGL.GL_SHARED_TEXTURE_PALETTE_EXT, OpenGL.GL_RGB, 256, OpenGL.GL_RGB, OpenGL.GL_UNSIGNED_BYTE, temptable);
		}
	}

	protected void GL_EnableMultitexture(bool enable)
	{
		if (!this.qglSelectTextureSGIS && !this.qglActiveTextureARB)
			return;

		if (enable)
		{
			this.GL_SelectTexture(OpenGL.GL_TEXTURE1);
			this.gl.glEnable(OpenGL.GL_TEXTURE_2D);
			this.GL_TexEnv(OpenGL.GL_REPLACE);
		}
		else
		{
			this.GL_SelectTexture(OpenGL.GL_TEXTURE1);
			this.gl.glDisable(OpenGL.GL_TEXTURE_2D);
			this.GL_TexEnv(OpenGL.GL_REPLACE);
		}

		this.GL_SelectTexture(OpenGL.GL_TEXTURE0);
		this.GL_TexEnv(OpenGL.GL_REPLACE);
	}

	protected void GL_SelectTexture(int texture /* GLenum */)
	{
		int tmu;

		if (!this.qglSelectTextureSGIS && !this.qglActiveTextureARB)
			return;

		if (texture == OpenGL.GL_TEXTURE0)
			tmu = 0;
		else
			tmu = 1;

		if (tmu == this.gl_state.currenttmu)
			return;

		this.gl_state.currenttmu = tmu;

		if (this.qglSelectTextureSGIS)
			this.gl.glActiveTextureARB(texture);
		else if (this.qglActiveTextureARB)
		{
			this.gl.glActiveTextureARB(texture);
			this.gl.glClientActiveTextureARB(texture);
		}
	}

	private readonly int[] lastmodes = { -1, -1 };

	protected void GL_TexEnv(
		int mode /* GLenum */
	)
	{
		if (mode != this.lastmodes[this.gl_state.currenttmu])
		{
			this.gl.glTexEnvi(OpenGL.GL_TEXTURE_ENV, OpenGL.GL_TEXTURE_ENV_MODE, mode);
			this.lastmodes[this.gl_state.currenttmu] = mode;
		}
	}

	protected void GL_Bind(int texnum)
	{
		if (this.gl_nobind.value != 0 && this.draw_chars != null)
		{
			// performance evaluation option
			texnum = this.draw_chars.texnum;
		}

		if (this.gl_state.currenttextures[this.gl_state.currenttmu] == texnum)
			return;

		this.gl_state.currenttextures[this.gl_state.currenttmu] = texnum;
		this.gl.glBindTexture(OpenGL.GL_TEXTURE_2D, texnum);
	}

	protected void GL_MBind(int target /* GLenum */, int texnum)
	{
		this.GL_SelectTexture(target);

		if (target == OpenGL.GL_TEXTURE0)
		{
			if (this.gl_state.currenttextures[0] == texnum)
				return;
		}
		else
		{
			if (this.gl_state.currenttextures[1] == texnum)
				return;
		}

		this.GL_Bind(texnum);
	}

	private static readonly glmode_t[] modes =
	{
		new("GL_NEAREST", OpenGL.GL_NEAREST, OpenGL.GL_NEAREST),
		new("GL_LINEAR", OpenGL.GL_LINEAR, OpenGL.GL_LINEAR),
		new("GL_NEAREST_MIPMAP_NEAREST", OpenGL.GL_NEAREST_MIPMAP_NEAREST, OpenGL.GL_NEAREST),
		new("GL_LINEAR_MIPMAP_NEAREST", OpenGL.GL_LINEAR_MIPMAP_NEAREST, OpenGL.GL_LINEAR),
		new("GL_NEAREST_MIPMAP_LINEAR", OpenGL.GL_NEAREST_MIPMAP_LINEAR, OpenGL.GL_NEAREST),
		new("GL_LINEAR_MIPMAP_LINEAR", OpenGL.GL_LINEAR_MIPMAP_LINEAR, OpenGL.GL_LINEAR)
	};

	private static readonly int NUM_GL_MODES = OpenGLRenderApi.modes.Length;

	private static readonly gltmode_t[] gl_alpha_modes =
	{
		new("default", 4),
		new("GL_RGBA", OpenGL.GL_RGBA),
		new("GL_RGBA8", OpenGL.GL_RGBA8),
		new("GL_RGB5_A1", OpenGL.GL_RGB5_A1),
		new("GL_RGBA4", OpenGL.GL_RGBA4),
		new("GL_RGBA2", OpenGL.GL_RGBA2)
	};

	private static readonly int NUM_GL_ALPHA_MODES = OpenGLRenderApi.gl_alpha_modes.Length;

	private static readonly gltmode_t[] gl_solid_modes =
	{
		new("default", 3),
		new("GL_RGB", OpenGL.GL_RGB),
		new("GL_RGB8", OpenGL.GL_RGB8),
		new("GL_RGB5", OpenGL.GL_RGB5),
		new("GL_RGB4", OpenGL.GL_RGB4),
		new("GL_R3_G3_B2", OpenGL.GL_R3_G3_B2)
	};

	private static readonly int NUM_GL_SOLID_MODES = OpenGLRenderApi.gl_solid_modes.Length;

	/*
	===============
	GL_TextureMode
	===============
	*/
	protected void GL_TextureMode(string @string)
	{
		int i;

		for (i = 0; i < OpenGLRenderApi.NUM_GL_MODES; i++)
		{
			if (OpenGLRenderApi.modes[i].name.Equals(@string, StringComparison.OrdinalIgnoreCase))
				break;
		}

		if (i == OpenGLRenderApi.NUM_GL_MODES)
		{
			VID.Printf(Defines.PRINT_ALL, "bad filter name: [" + @string + "]\n");

			return;
		}

		this.gl_filter_min = OpenGLRenderApi.modes[i].minimize;
		this.gl_filter_max = OpenGLRenderApi.modes[i].maximize;

		image_t glt;

		// change all the existing mipmap texture objects
		for (i = 0; i < this.numgltextures; i++)
		{
			glt = this.gltextures[i];

			if (glt.type != Base.it_pic && glt.type != Base.it_sky)
			{
				this.GL_Bind(glt.texnum);
				this.gl.glTexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, this.gl_filter_min);
				this.gl.glTexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, this.gl_filter_max);
			}
		}
	}

	/*
	===============
	GL_TextureAlphaMode
	===============
	*/
	protected void GL_TextureAlphaMode(string @string)
	{
		int i;

		for (i = 0; i < OpenGLRenderApi.NUM_GL_ALPHA_MODES; i++)
		{
			if (OpenGLRenderApi.gl_alpha_modes[i].name.Equals(@string, StringComparison.OrdinalIgnoreCase))
				break;
		}

		if (i == OpenGLRenderApi.NUM_GL_ALPHA_MODES)
		{
			VID.Printf(Defines.PRINT_ALL, "bad alpha texture mode name: [" + @string + "]\n");

			return;
		}

		this.gl_tex_alpha_format = OpenGLRenderApi.gl_alpha_modes[i].mode;
	}

	/*
	===============
	GL_TextureSolidMode
	===============
	*/
	protected void GL_TextureSolidMode(string @string)
	{
		int i;

		for (i = 0; i < OpenGLRenderApi.NUM_GL_SOLID_MODES; i++)
		{
			if (OpenGLRenderApi.gl_solid_modes[i].name.Equals(@string, StringComparison.OrdinalIgnoreCase))
				break;
		}

		if (i == OpenGLRenderApi.NUM_GL_SOLID_MODES)
		{
			VID.Printf(Defines.PRINT_ALL, "bad solid texture mode name: [" + @string + "]\n");

			return;
		}

		this.gl_tex_solid_format = OpenGLRenderApi.gl_solid_modes[i].mode;
	}

	/*
	===============
	GL_ImageList_f
	===============
	*/
	public void GL_ImageList_f()
	{
		image_t image;
		int texels;
		string[] palstrings = { "RGB", "PAL" };

		VID.Printf(Defines.PRINT_ALL, "------------------\n");
		texels = 0;

		for (var i = 0; i < this.numgltextures; i++)
		{
			image = this.gltextures[i];

			if (image.texnum <= 0)
				continue;

			texels += image.upload_width * image.upload_height;

			switch (image.type)
			{
				case Base.it_skin:
					VID.Printf(Defines.PRINT_ALL, "M");

					break;

				case Base.it_sprite:
					VID.Printf(Defines.PRINT_ALL, "S");

					break;

				case Base.it_wall:
					VID.Printf(Defines.PRINT_ALL, "W");

					break;

				case Base.it_pic:
					VID.Printf(Defines.PRINT_ALL, "P");

					break;

				default:
					VID.Printf(Defines.PRINT_ALL, " ");

					break;
			}

			VID.Printf(
				Defines.PRINT_ALL,
				" %3i %3i %s: %s\n",
				image.upload_width, image.upload_height, palstrings[image.paletted ? 1 : 0], image.name
			);
		}

		VID.Printf(Defines.PRINT_ALL, "Total texel count (not counting mipmaps): " + texels + '\n');
	}

	/*
	=============================================================================
	
	  scrap allocation
	
	  Allocate all the little status bar objects into a single texture
	  to crutch up inefficient hardware / drivers
	
	=============================================================================
	*/
	private static readonly int MAX_SCRAPS = 1;
	private static readonly int IMG_BLOCK_WIDTH = 256;
	private static readonly int IMG_BLOCK_HEIGHT = 256;
	private readonly int[][] scrap_allocated = new int[OpenGLRenderApi.MAX_SCRAPS][];
	private readonly byte[][] scrap_texels = new byte[OpenGLRenderApi.MAX_SCRAPS][];
	protected bool scrap_dirty;

	protected class pos_t
	{
		public int x;
		public int y;

		public pos_t(int x, int y)
		{
			this.x = x;
			this.y = y;
		}
	}

	// returns a texture number and the position inside it
	private int Scrap_AllocBlock(int w, int h, pos_t pos)
	{
		int i, j;
		int best, best2;
		int texnum;

		for (texnum = 0; texnum < OpenGLRenderApi.MAX_SCRAPS; texnum++)
		{
			best = OpenGLRenderApi.IMG_BLOCK_HEIGHT;

			for (i = 0; i < OpenGLRenderApi.IMG_BLOCK_WIDTH - w; i++)
			{
				best2 = 0;

				for (j = 0; j < w; j++)
				{
					if (this.scrap_allocated[texnum][i + j] >= best)
						break;

					if (this.scrap_allocated[texnum][i + j] > best2)
						best2 = this.scrap_allocated[texnum][i + j];
				}

				if (j == w)
				{
					// this is a valid spot
					pos.x = i;
					pos.y = best = best2;
				}
			}

			if (best + h > OpenGLRenderApi.IMG_BLOCK_HEIGHT)
				continue;

			for (i = 0; i < w; i++)
				this.scrap_allocated[texnum][pos.x + i] = best + h;

			return texnum;
		}

		return -1;

		// Sys_Error ("Scrap_AllocBlock: full");
	}

	private int scrap_uploads;

	protected void Scrap_Upload()
	{
		this.scrap_uploads++;
		this.GL_Bind(Base.TEXNUM_SCRAPS);
		this.GL_Upload8(this.scrap_texels[0], OpenGLRenderApi.IMG_BLOCK_WIDTH, OpenGLRenderApi.IMG_BLOCK_HEIGHT, false, false);
		this.scrap_dirty = false;
	}

	/*
	=================================================================
	
	PCX LOADING
	
	=================================================================
	*/

	/*
	==============
	LoadPCX
	==============
	*/
	private byte[] LoadPCX(string filename, byte[][] palette, ref Size dim)
	{
		pcx_t pcx;

		//
		// load the file
		//
		var raw = FS.LoadFile(filename);

		if (raw == null)
		{
			VID.Printf(Defines.PRINT_DEVELOPER, "Bad pcx file " + filename + '\n');

			return null;
		}

		//
		// parse the PCX file
		//
		pcx = new(raw);

		if (pcx.manufacturer != 0x0a || pcx.version != 5 || pcx.encoding != 1 || pcx.bits_per_pixel != 8 || pcx.xmax >= 640 || pcx.ymax >= 480)
		{
			VID.Printf(Defines.PRINT_ALL, "Bad pcx file " + filename + '\n');

			return null;
		}

		var width = pcx.xmax - pcx.xmin + 1;
		var height = pcx.ymax - pcx.ymin + 1;

		var pix = new byte[width * height];

		if (palette != null)
		{
			palette[0] = new byte[768];
			Array.Copy(raw, raw.Length - 768, palette[0], 0, 768);
		}

		dim.Width = width;
		dim.Height = height;

		//
		// decode pcx
		//
		var count = 0;
		byte dataByte = 0;
		var runLength = 0;
		int x, y;
		var i = 0;

		for (y = 0; y < height; y++)
		{
			for (x = 0; x < width;)
			{
				dataByte = pcx.data[i++];

				if ((dataByte & 0xC0) == 0xC0)
				{
					runLength = dataByte & 0x3F;
					dataByte = pcx.data[i++];

					// write runLength pixel
					while (runLength-- > 0)
					{
						pix[count++] = dataByte;
						x++;
					}
				}
				else
				{
					// write one pixel
					pix[count++] = dataByte;
					x++;
				}
			}
		}

		return pix;
	}

	//	/*
	//	=========================================================
	//
	//	TARGA LOADING
	//
	//	=========================================================
	//	*/
	/*
	=============
	LoadTGA
	=============
	*/
	private byte[] LoadTGA(string name, ref Size dim)
	{
		int columns, rows, numPixels;
		int pixbuf; // index into pic
		int row, column;
		byte[] raw;
		BinaryReader buf_p;
		tga_t targa_header;
		byte[] pic = null;

		//
		// load the file
		//
		raw = FS.LoadFile(name);

		if (raw == null)
		{
			VID.Printf(Defines.PRINT_DEVELOPER, "Bad tga file " + name + '\n');

			return null;
		}

		targa_header = new(raw);

		if (targa_header.image_type != 2 && targa_header.image_type != 10)
			Com.Error(Defines.ERR_DROP, "LoadTGA: Only type 2 and 10 targa RGB images supported\n");

		if (targa_header.colormap_type != 0 || (targa_header.pixel_size != 32 && targa_header.pixel_size != 24))
			Com.Error(Defines.ERR_DROP, "LoadTGA: Only 32 or 24 bit images supported (no colormaps)\n");

		columns = targa_header.width;
		rows = targa_header.height;
		numPixels = columns * rows;

		dim.Width = columns;
		dim.Height = rows;

		pic = new byte[numPixels * 4]; // targa_rgba;

		buf_p = new(new MemoryStream(targa_header.data, targa_header.id_length, targa_header.data.Length - targa_header.id_length));

		byte red, green, blue, alphabyte;
		red = green = blue = alphabyte = 0;
		int packetHeader, packetSize, j;

		if (targa_header.image_type == 2)
		{
			// Uncompressed, RGB images
			for (row = rows - 1; row >= 0; row--)
			{
				pixbuf = row * columns * 4;

				for (column = 0; column < columns; column++)
				{
					switch (targa_header.pixel_size)
					{
						case 24:

							blue = buf_p.ReadByte();
							green = buf_p.ReadByte();
							red = buf_p.ReadByte();
							pic[pixbuf++] = red;
							pic[pixbuf++] = green;
							pic[pixbuf++] = blue;
							pic[pixbuf++] = (byte)255;

							break;

						case 32:
							blue = buf_p.ReadByte();
							green = buf_p.ReadByte();
							red = buf_p.ReadByte();
							alphabyte = buf_p.ReadByte();
							pic[pixbuf++] = red;
							pic[pixbuf++] = green;
							pic[pixbuf++] = blue;
							pic[pixbuf++] = alphabyte;

							break;
					}
				}
			}
		}
		else if (targa_header.image_type == 10)
		{
			// Runlength encoded RGB images
			for (row = rows - 1; row >= 0; row--)
			{
				pixbuf = row * columns * 4;

				var stop = false;

				for (column = 0; column < columns && !stop;)
				{
					packetHeader = buf_p.ReadByte();
					packetSize = 1 + (packetHeader & 0x7f);

					if ((packetHeader & 0x80) != 0)
					{
						// run-length packet
						switch (targa_header.pixel_size)
						{
							case 24:
								blue = buf_p.ReadByte();
								green = buf_p.ReadByte();
								red = buf_p.ReadByte();
								alphabyte = (byte)255;

								break;

							case 32:
								blue = buf_p.ReadByte();
								green = buf_p.ReadByte();
								red = buf_p.ReadByte();
								alphabyte = buf_p.ReadByte();

								break;
						}

						for (j = 0; j < packetSize && !stop; j++)
						{
							pic[pixbuf++] = red;
							pic[pixbuf++] = green;
							pic[pixbuf++] = blue;
							pic[pixbuf++] = alphabyte;
							column++;

							if (column == columns)
							{
								// run spans across rows
								column = 0;

								if (row > 0)
								{
									row--;
									pixbuf = row * columns * 4;
								}
								else
									stop = true;
							}
						}
					}
					else
					{
						// non run-length packet
						for (j = 0; j < packetSize && !stop; j++)
						{
							switch (targa_header.pixel_size)
							{
								case 24:
									blue = buf_p.ReadByte();
									green = buf_p.ReadByte();
									red = buf_p.ReadByte();
									pic[pixbuf++] = red;
									pic[pixbuf++] = green;
									pic[pixbuf++] = blue;
									pic[pixbuf++] = (byte)255;

									break;

								case 32:
									blue = buf_p.ReadByte();
									green = buf_p.ReadByte();
									red = buf_p.ReadByte();
									alphabyte = buf_p.ReadByte();
									pic[pixbuf++] = red;
									pic[pixbuf++] = green;
									pic[pixbuf++] = blue;
									pic[pixbuf++] = alphabyte;

									break;
							}

							column++;

							if (column == columns)
							{
								// pixel packet run spans across rows
								column = 0;

								if (row > 0)
								{
									row--;
									pixbuf = row * columns * 4;
								}
								else
									stop = true;
							}
						}
					}
				}
			}
		}

		return pic;
	}

	/*
	====================================================================
	
	IMAGE FLOOD FILLING
	
	====================================================================
	*/
	/*
	=================
	Mod_FloodFillSkin
	
	Fill background pixels so mipmapping doesn't have haloes
	=================
	*/

	// must be a power of 2
	private static readonly int FLOODFILL_FIFO_SIZE = 0x1000;
	private static readonly int FLOODFILL_FIFO_MASK = OpenGLRenderApi.FLOODFILL_FIFO_SIZE - 1;

	//
	//	#define FLOODFILL_STEP( off, dx, dy ) \
	//	{ \
	//		if (pos[off] == fillcolor) \
	//		{ \
	//			pos[off] = 255; \
	//			fifo[inpt].x = x + (dx), fifo[inpt].y = y + (dy); \
	//			inpt = (inpt + 1) & FLOODFILL_FIFO_MASK; \
	//		} \
	//		else if (pos[off] != 255) fdc = pos[off]; \
	//	}

	//	void FLOODFILL_STEP( int off, int dx, int dy )
	//	{
	//		if (pos[off] == fillcolor)
	//		{
	//			pos[off] = 255;
	//			fifo[inpt].x = x + dx; fifo[inpt].y = y + dy;
	//			inpt = (inpt + 1) & FLOODFILL_FIFO_MASK;
	//		}
	//		else if (pos[off] != 255) fdc = pos[off];
	//	}
	private static readonly floodfill_t[] fifo = new floodfill_t[OpenGLRenderApi.FLOODFILL_FIFO_SIZE];

	static OpenGLRenderApi()
	{
		for (var j = 0; j < OpenGLRenderApi.fifo.Length; j++)
			OpenGLRenderApi.fifo[j] = new();
	}

	private void R_FloodFillSkin(byte[] skin, int skinwidth, int skinheight)
	{
		//		byte				fillcolor = *skin; // assume this is the pixel to fill
		var fillcolor = skin[0] & 0xff;

		//		floodfill_t[] fifo = new floodfill_t[FLOODFILL_FIFO_SIZE];
		int inpt = 0, outpt = 0;
		var filledcolor = -1;
		int i;

		//		for (int j = 0; j < fifo.length; j++) {
		//			fifo[j] = new floodfill_t();
		//		}

		if (filledcolor == -1)
		{
			filledcolor = 0;

			// attempt to find opaque black
			for (i = 0; i < 256; ++i)
			{
				if ((uint)OpenGLRenderApi.d_8to24table[i] == 0xFF000000)
				{
					// alpha 1.0
					//if (d_8to24table[i] == (255 << 0)) // alpha 1.0
					filledcolor = i;

					break;
				}
			}
		}

		// can't fill to filled color or to transparent color (used as visited marker)
		if (fillcolor == filledcolor || fillcolor == 255)
			return;

		OpenGLRenderApi.fifo[inpt].x = 0;
		OpenGLRenderApi.fifo[inpt].y = 0;
		inpt = (inpt + 1) & OpenGLRenderApi.FLOODFILL_FIFO_MASK;

		while (outpt != inpt)
		{
			int x = OpenGLRenderApi.fifo[outpt].x;
			int y = OpenGLRenderApi.fifo[outpt].y;
			var fdc = filledcolor;

			//			byte		*pos = &skin[x + skinwidth * y];
			var pos = x + skinwidth * y;

			//
			outpt = (outpt + 1) & OpenGLRenderApi.FLOODFILL_FIFO_MASK;

			int off, dx, dy;

			if (x > 0)
			{
				// FLOODFILL_STEP( -1, -1, 0 );
				off = -1;
				dx = -1;
				dy = 0;

				if (skin[pos + off] == (byte)fillcolor)
				{
					skin[pos + off] = (byte)255;
					OpenGLRenderApi.fifo[inpt].x = (short)(x + dx);
					OpenGLRenderApi.fifo[inpt].y = (short)(y + dy);
					inpt = (inpt + 1) & OpenGLRenderApi.FLOODFILL_FIFO_MASK;
				}
				else if (skin[pos + off] != (byte)255)
					fdc = skin[pos + off] & 0xff;
			}

			if (x < skinwidth - 1)
			{
				// FLOODFILL_STEP( 1, 1, 0 );
				off = 1;
				dx = 1;
				dy = 0;

				if (skin[pos + off] == (byte)fillcolor)
				{
					skin[pos + off] = (byte)255;
					OpenGLRenderApi.fifo[inpt].x = (short)(x + dx);
					OpenGLRenderApi.fifo[inpt].y = (short)(y + dy);
					inpt = (inpt + 1) & OpenGLRenderApi.FLOODFILL_FIFO_MASK;
				}
				else if (skin[pos + off] != (byte)255)
					fdc = skin[pos + off] & 0xff;
			}

			if (y > 0)
			{
				// FLOODFILL_STEP( -skinwidth, 0, -1 );
				off = -skinwidth;
				dx = 0;
				dy = -1;

				if (skin[pos + off] == (byte)fillcolor)
				{
					skin[pos + off] = (byte)255;
					OpenGLRenderApi.fifo[inpt].x = (short)(x + dx);
					OpenGLRenderApi.fifo[inpt].y = (short)(y + dy);
					inpt = (inpt + 1) & OpenGLRenderApi.FLOODFILL_FIFO_MASK;
				}
				else if (skin[pos + off] != (byte)255)
					fdc = skin[pos + off] & 0xff;
			}

			if (y < skinheight - 1)
			{
				// FLOODFILL_STEP( skinwidth, 0, 1 );
				off = skinwidth;
				dx = 0;
				dy = 1;

				if (skin[pos + off] == (byte)fillcolor)
				{
					skin[pos + off] = (byte)255;
					OpenGLRenderApi.fifo[inpt].x = (short)(x + dx);
					OpenGLRenderApi.fifo[inpt].y = (short)(y + dy);
					inpt = (inpt + 1) & OpenGLRenderApi.FLOODFILL_FIFO_MASK;
				}
				else if (skin[pos + off] != (byte)255)
					fdc = skin[pos + off] & 0xff;
			}

			skin[x + skinwidth * y] = (byte)fdc;
		}
	}

	// ==================================================
	private readonly int[] p1 = new int[1024];
	private readonly int[] p2 = new int[1024];

	/*
	 * GL_ResampleTexture
	 */
	private void GL_ResampleTexture(int[] @in, int inwidth, int inheight, int[] @out, int outwidth, int outheight)
	{
		Array.Fill(this.p1, 0);
		Array.Fill(this.p2, 0);

		var fracstep = inwidth * 0x10000 / outwidth;

		int i, j;
		var frac = fracstep >> 2;

		for (i = 0; i < outwidth; i++)
		{
			this.p1[i] = frac >> 16;
			frac += fracstep;
		}

		frac = 3 * (fracstep >> 2);

		for (i = 0; i < outwidth; i++)
		{
			this.p2[i] = frac >> 16;
			frac += fracstep;
		}

		var outp = 0;
		int r, g, b, a;
		int inrow, inrow2;
		int pix1, pix2, pix3, pix4;

		for (i = 0; i < outheight; i++)
		{
			inrow = inwidth * (int)((i + 0.25f) * inheight / outheight);
			inrow2 = inwidth * (int)((i + 0.75f) * inheight / outheight);
			frac = fracstep >> 1;

			for (j = 0; j < outwidth; j++)
			{
				pix1 = @in[inrow + this.p1[j]];
				pix2 = @in[inrow + this.p2[j]];
				pix3 = @in[inrow2 + this.p1[j]];
				pix4 = @in[inrow2 + this.p2[j]];

				r = (((pix1 >> 0) & 0xFF) + ((pix2 >> 0) & 0xFF) + ((pix3 >> 0) & 0xFF) + ((pix4 >> 0) & 0xFF)) >> 2;
				g = (((pix1 >> 8) & 0xFF) + ((pix2 >> 8) & 0xFF) + ((pix3 >> 8) & 0xFF) + ((pix4 >> 8) & 0xFF)) >> 2;
				b = (((pix1 >> 16) & 0xFF) + ((pix2 >> 16) & 0xFF) + ((pix3 >> 16) & 0xFF) + ((pix4 >> 16) & 0xFF)) >> 2;
				a = (((pix1 >> 24) & 0xFF) + ((pix2 >> 24) & 0xFF) + ((pix3 >> 24) & 0xFF) + ((pix4 >> 24) & 0xFF)) >> 2;

				@out[outp++] = (a << 24) | (b << 16) | (g << 8) | r;
			}
		}
	}

	/*
	================
	GL_LightScaleTexture
	
	Scale up the pixel values in a texture to increase the
	lighting range
	================
	*/
	private void GL_LightScaleTexture(int[] @in, int inwidth, int inheight, bool only_gamma)
	{
		if (only_gamma)
		{
			int i, c;
			int r, g, b, color;

			c = inwidth * inheight;

			for (i = 0; i < c; i++)
			{
				color = @in[i];
				r = (color >> 0) & 0xFF;
				g = (color >> 8) & 0xFF;
				b = (color >> 16) & 0xFF;

				r = this.gammatable[r] & 0xFF;
				g = this.gammatable[g] & 0xFF;
				b = this.gammatable[b] & 0xFF;

				@in[i] = (int)(((uint)r << 0) | ((uint)g << 8) | ((uint)b << 16) | ((uint)color & 0xFF000000));
			}
		}
		else
		{
			int i, c;
			int r, g, b, color;

			c = inwidth * inheight;

			for (i = 0; i < c; i++)
			{
				color = @in[i];
				r = (color >> 0) & 0xFF;
				g = (color >> 8) & 0xFF;
				b = (color >> 16) & 0xFF;

				r = this.gammatable[this.intensitytable[r] & 0xFF] & 0xFF;
				g = this.gammatable[this.intensitytable[g] & 0xFF] & 0xFF;
				b = this.gammatable[this.intensitytable[b] & 0xFF] & 0xFF;

				@in[i] = (int)(((uint)r << 0) | ((uint)g << 8) | ((uint)b << 16) | ((uint)color & 0xFF000000));
			}
		}
	}

	/*
	================
	GL_MipMap
	
	Operates in place, quartering the size of the texture
	================
	*/
	private void GL_MipMap(int[] @in, int width, int height)
	{
		int i, j;
		int[] @out;

		@out = @in;

		var inIndex = 0;
		var outIndex = 0;

		int r, g, b, a;
		int p1, p2, p3, p4;

		for (i = 0; i < height; i += 2, inIndex += width)
		{
			for (j = 0; j < width; j += 2, outIndex += 1, inIndex += 2)
			{
				p1 = @in[inIndex + 0];
				p2 = @in[inIndex + 1];
				p3 = @in[inIndex + width + 0];
				p4 = @in[inIndex + width + 1];

				r = (((p1 >> 0) & 0xFF) + ((p2 >> 0) & 0xFF) + ((p3 >> 0) & 0xFF) + ((p4 >> 0) & 0xFF)) >> 2;
				g = (((p1 >> 8) & 0xFF) + ((p2 >> 8) & 0xFF) + ((p3 >> 8) & 0xFF) + ((p4 >> 8) & 0xFF)) >> 2;
				b = (((p1 >> 16) & 0xFF) + ((p2 >> 16) & 0xFF) + ((p3 >> 16) & 0xFF) + ((p4 >> 16) & 0xFF)) >> 2;
				a = (((p1 >> 24) & 0xFF) + ((p2 >> 24) & 0xFF) + ((p3 >> 24) & 0xFF) + ((p4 >> 24) & 0xFF)) >> 2;

				@out[outIndex] = (r << 0) | (g << 8) | (b << 16) | (a << 24);
			}
		}
	}

	/*
	===============
	GL_Upload32
	
	Returns has_alpha
	===============
	*/
	private void GL_BuildPalettedTexture(byte[] paletted_texture, int[] scaled, int scaled_width, int scaled_height)
	{
		int r, g, b, c;
		var size = scaled_width * scaled_height;

		for (var i = 0; i < size; i++)
		{
			r = (scaled[i] >> 3) & 31;
			g = (scaled[i] >> 10) & 63;
			b = (scaled[i] >> 19) & 31;

			c = r | (g << 5) | (b << 11);

			paletted_texture[i] = this.gl_state.d_16to8table[c];
		}
	}

	private int upload_width, upload_height;
	private bool uploaded_paletted;

	/*
	===============
	GL_Upload32
	
	Returns has_alpha
	===============
	*/
	private readonly int[] scaled = new int[256 * 256];

	//byte[] paletted_texture = new byte[256 * 256];
	private readonly byte[] paletted_texture = new byte[256 * 256];
	private readonly int[] tex = new int[512 * 256];

	private bool GL_Upload32(int[] data, int width, int height, bool mipmap)
	{
		int samples;
		int scaled_width, scaled_height;
		int i, c;
		int comp;

		Array.Fill(this.scaled, 0);

		// Arrays.fill(paletted_texture, (byte)0);
		Array.Clear(this.paletted_texture, 0, this.paletted_texture.Length);

		this.uploaded_paletted = false;

		for (scaled_width = 1; scaled_width < width; scaled_width <<= 1)
			;

		if (this.gl_round_down.value > 0.0f && scaled_width > width && mipmap)
			scaled_width >>= 1;

		for (scaled_height = 1; scaled_height < height; scaled_height <<= 1)
			;

		if (this.gl_round_down.value > 0.0f && scaled_height > height && mipmap)
			scaled_height >>= 1;

		// let people sample down the world textures for speed
		if (mipmap)
		{
			scaled_width >>= (int)this.gl_picmip.value;
			scaled_height >>= (int)this.gl_picmip.value;
		}

		// don't ever bother with >256 textures
		if (scaled_width > 256)
			scaled_width = 256;

		if (scaled_height > 256)
			scaled_height = 256;

		if (scaled_width < 1)
			scaled_width = 1;

		if (scaled_height < 1)
			scaled_height = 1;

		this.upload_width = scaled_width;
		this.upload_height = scaled_height;

		if (scaled_width * scaled_height > 256 * 256)
			Com.Error(Defines.ERR_DROP, "GL_Upload32: too big");

		// scan the texture for any non-255 alpha
		c = width * height;
		samples = this.gl_solid_format;

		for (i = 0; i < c; i++)
		{
			if ((data[i] & 0xff000000) != 0xff000000)
			{
				samples = this.gl_alpha_format;

				break;
			}
		}

		if (samples == this.gl_solid_format)
			comp = this.gl_tex_solid_format;
		else if (samples == this.gl_alpha_format)
			comp = this.gl_tex_alpha_format;
		else
		{
			VID.Printf(Defines.PRINT_ALL, "Unknown number of texture components " + samples + '\n');
			comp = samples;
		}

		var stop = false;

		if (scaled_width == width && scaled_height == height)
		{
			if (!mipmap)
			{
				if (this.qglColorTableEXT && this.gl_ext_palettedtexture.value != 0.0f && samples == this.gl_solid_format)
				{
					this.uploaded_paletted = true;
					this.GL_BuildPalettedTexture(this.paletted_texture, data, scaled_width, scaled_height);

					this.gl.glTexImage2D(
						OpenGL.GL_TEXTURE_2D,
						0,
						OpenGL.GL_COLOR_INDEX,
						scaled_width,
						scaled_height,
						0,
						OpenGL.GL_COLOR_INDEX,
						OpenGL.GL_UNSIGNED_BYTE,
						this.paletted_texture
					);
				}
				else
				{
					Array.Clear(this.tex, 0, this.tex.Length);
					Array.Copy(data, 0, this.tex, 0, scaled_width * scaled_height);

					this.gl.glTexImage2D(OpenGL.GL_TEXTURE_2D, 0, comp, scaled_width, scaled_height, 0, OpenGL.GL_RGBA, OpenGL.GL_UNSIGNED_BYTE, this.tex);
				}

				stop = true;
			}

			//memcpy (scaled, data, width*height*4); were bytes
			if (!stop)
				Array.Copy(data, 0, this.scaled, 0, width * height);
		}
		else
			this.GL_ResampleTexture(data, width, height, this.scaled, scaled_width, scaled_height);

		if (!stop)
		{
			this.GL_LightScaleTexture(this.scaled, scaled_width, scaled_height, !mipmap);

			if (this.qglColorTableEXT && this.gl_ext_palettedtexture.value != 0.0f && samples == this.gl_solid_format)
			{
				this.uploaded_paletted = true;
				this.GL_BuildPalettedTexture(this.paletted_texture, this.scaled, scaled_width, scaled_height);

				this.gl.glTexImage2D(
					OpenGL.GL_TEXTURE_2D,
					0,
					OpenGL.GL_COLOR_INDEX,
					scaled_width,
					scaled_height,
					0,
					OpenGL.GL_COLOR_INDEX,
					OpenGL.GL_UNSIGNED_BYTE,
					this.paletted_texture
				);
			}
			else
			{
				Array.Clear(this.tex, 0, this.tex.Length);
				Array.Copy(this.scaled, 0, this.tex, 0, scaled_width * scaled_height);

				this.gl.glTexImage2D(OpenGL.GL_TEXTURE_2D, 0, comp, scaled_width, scaled_height, 0, OpenGL.GL_RGBA, OpenGL.GL_UNSIGNED_BYTE, this.tex);
			}

			if (mipmap)
			{
				int miplevel;
				miplevel = 0;

				while (scaled_width > 1 || scaled_height > 1)
				{
					this.GL_MipMap(this.scaled, scaled_width, scaled_height);
					scaled_width >>= 1;
					scaled_height >>= 1;

					if (scaled_width < 1)
						scaled_width = 1;

					if (scaled_height < 1)
						scaled_height = 1;

					miplevel++;

					if (this.qglColorTableEXT && this.gl_ext_palettedtexture.value != 0.0f && samples == this.gl_solid_format)
					{
						this.uploaded_paletted = true;
						this.GL_BuildPalettedTexture(this.paletted_texture, this.scaled, scaled_width, scaled_height);

						this.gl.glTexImage2D(
							OpenGL.GL_TEXTURE_2D,
							miplevel,
							OpenGL.GL_COLOR_INDEX,
							scaled_width,
							scaled_height,
							0,
							OpenGL.GL_COLOR_INDEX,
							OpenGL.GL_UNSIGNED_BYTE,
							this.paletted_texture
						);
					}
					else
					{
						Array.Clear(this.tex, 0, this.tex.Length);
						Array.Copy(this.scaled, 0, this.tex, 0, scaled_width * scaled_height);

						this.gl.glTexImage2D(
							OpenGL.GL_TEXTURE_2D,
							miplevel,
							comp,
							scaled_width,
							scaled_height,
							0,
							OpenGL.GL_RGBA,
							OpenGL.GL_UNSIGNED_BYTE,
							this.tex
						);
					}
				}
			}
		}

		if (mipmap)
		{
			this.gl.glTexParameterf(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, this.gl_filter_min);
			this.gl.glTexParameterf(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, this.gl_filter_max);
		}
		else
		{
			this.gl.glTexParameterf(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, this.gl_filter_max);
			this.gl.glTexParameterf(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, this.gl_filter_max);
		}

		return samples == this.gl_alpha_format;
	}

	/*
	===============
	GL_Upload8
	
	Returns has_alpha
	===============
	*/
	private readonly int[] trans = new int[512 * 256];

	private bool GL_Upload8(byte[] data, int width, int height, bool mipmap, bool is_sky)
	{
		Array.Fill(this.trans, 0);

		var s = width * height;

		if (s > this.trans.Length)
			Com.Error(Defines.ERR_DROP, "GL_Upload8: too large");

		if (this.qglColorTableEXT && this.gl_ext_palettedtexture.value != 0.0f && is_sky)
		{
			this.gl.glTexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_COLOR_INDEX, width, height, 0, OpenGL.GL_COLOR_INDEX, OpenGL.GL_UNSIGNED_BYTE, data);

			this.gl.glTexParameterf(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, this.gl_filter_max);
			this.gl.glTexParameterf(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, this.gl_filter_max);

			return false;
		}

		int p;

		for (var i = 0; i < s; i++)
		{
			p = data[i] & 0xff;
			this.trans[i] = OpenGLRenderApi.d_8to24table[p];

			if (p == 255)
			{
				// transparent, so scan around for another color
				// to avoid alpha fringes
				// FIXME: do a full flood fill so mips work...
				if (i > width && (data[i - width] & 0xff) != 255)
					p = data[i - width] & 0xff;
				else if (i < s - width && (data[i + width] & 0xff) != 255)
					p = data[i + width] & 0xff;
				else if (i > 0 && (data[i - 1] & 0xff) != 255)
					p = data[i - 1] & 0xff;
				else if (i < s - 1 && (data[i + 1] & 0xff) != 255)
					p = data[i + 1] & 0xff;
				else
					p = 0;

				// copy rgb components

				// ((byte *)&trans[i])[0] = ((byte *)&d_8to24table[p])[0];
				// ((byte *)&trans[i])[1] = ((byte *)&d_8to24table[p])[1];
				// ((byte *)&trans[i])[2] = ((byte *)&d_8to24table[p])[2];

				this.trans[i] = OpenGLRenderApi.d_8to24table[p] & 0x00FFFFFF; // only rgb
			}
		}

		return this.GL_Upload32(this.trans, width, height, mipmap);
	}

	/*
	================
	GL_LoadPic
	
	This is also used as an entry point for the generated r_notexture
	================
	*/
	protected image_t GL_LoadPic(string name, byte[] pic, int width, int height, int type, int bits)
	{
		image_t image;
		int i;

		// find a free image_t
		for (i = 0; i < this.numgltextures; i++)
		{
			image = this.gltextures[i];

			if (image.texnum == 0)
				break;
		}

		if (i == this.numgltextures)
		{
			if (this.numgltextures == Base.MAX_GLTEXTURES)
				Com.Error(Defines.ERR_DROP, "MAX_GLTEXTURES");

			this.numgltextures++;
		}

		image = this.gltextures[i];

		if (name.Length > Defines.MAX_QPATH)
			Com.Error(Defines.ERR_DROP, "Draw_LoadPic: \"" + name + "\" is too long");

		image.name = name;
		image.registration_sequence = this.registration_sequence;

		image.width = width;
		image.height = height;
		image.type = type;

		if (type == Base.it_skin && bits == 8)
			this.R_FloodFillSkin(pic, width, height);

		// load little pics into the scrap
		if (image.type == Base.it_pic && bits == 8 && image.width < 64 && image.height < 64)
		{
			pos_t pos = new(0, 0);
			int j, k;

			var texnum = this.Scrap_AllocBlock(image.width, image.height, pos);

			if (texnum == -1)
			{
				// replace goto nonscrap

				image.scrap = false;

				image.texnum = Base.TEXNUM_IMAGES + image.getId(); // image pos in array
				this.GL_Bind(image.texnum);

				if (bits == 8)
				{
					image.has_alpha = this.GL_Upload8(
						pic,
						width,
						height,
						image.type != Base.it_pic && image.type != Base.it_sky,
						image.type == Base.it_sky
					);
				}
				else
				{
					var tmp = new int[pic.Length / 4];

					for (i = 0; i < tmp.Length; i++)
					{
						tmp[i] = (pic[4 * i + 0] & 0xFF) << 0; // & 0x000000FF;
						tmp[i] |= (pic[4 * i + 1] & 0xFF) << 8; // & 0x0000FF00;
						tmp[i] |= (pic[4 * i + 2] & 0xFF) << 16; // & 0x00FF0000;
						tmp[i] |= (pic[4 * i + 3] & 0xFF) << 24; // & 0xFF000000;
					}

					image.has_alpha = this.GL_Upload32(tmp, width, height, image.type != Base.it_pic && image.type != Base.it_sky);
				}

				image.upload_width = this.upload_width; // after power of 2 and scales
				image.upload_height = this.upload_height;
				image.paletted = this.uploaded_paletted;
				image.sl = 0;
				image.sh = 1;
				image.tl = 0;
				image.th = 1;

				return image;
			}

			this.scrap_dirty = true;

			// copy the texels into the scrap block
			k = 0;

			for (i = 0; i < image.height; i++)
			for (j = 0; j < image.width; j++, k++)
				this.scrap_texels[texnum][(pos.y + i) * OpenGLRenderApi.IMG_BLOCK_WIDTH + pos.x + j] = pic[k];

			image.texnum = Base.TEXNUM_SCRAPS + texnum;
			image.scrap = true;
			image.has_alpha = true;
			image.sl = (pos.x + 0.01f) / (float)OpenGLRenderApi.IMG_BLOCK_WIDTH;
			image.sh = (pos.x + image.width - 0.01f) / (float)OpenGLRenderApi.IMG_BLOCK_WIDTH;
			image.tl = (pos.y + 0.01f) / (float)OpenGLRenderApi.IMG_BLOCK_WIDTH;
			image.th = (pos.y + image.height - 0.01f) / (float)OpenGLRenderApi.IMG_BLOCK_WIDTH;
		}
		else
		{
			// this was label nonscrap

			image.scrap = false;

			image.texnum = Base.TEXNUM_IMAGES + image.getId(); //image pos in array
			this.GL_Bind(image.texnum);

			if (bits == 8)
				image.has_alpha = this.GL_Upload8(pic, width, height, image.type != Base.it_pic && image.type != Base.it_sky, image.type == Base.it_sky);
			else
			{
				var tmp = new int[pic.Length / 4];

				for (i = 0; i < tmp.Length; i++)
				{
					tmp[i] = (pic[4 * i + 0] & 0xFF) << 0; // & 0x000000FF;
					tmp[i] |= (pic[4 * i + 1] & 0xFF) << 8; // & 0x0000FF00;
					tmp[i] |= (pic[4 * i + 2] & 0xFF) << 16; // & 0x00FF0000;
					tmp[i] |= (pic[4 * i + 3] & 0xFF) << 24; // & 0xFF000000;
				}

				image.has_alpha = this.GL_Upload32(tmp, width, height, image.type != Base.it_pic && image.type != Base.it_sky);
			}

			image.upload_width = this.upload_width; // after power of 2 and scales
			image.upload_height = this.upload_height;
			image.paletted = this.uploaded_paletted;
			image.sl = 0;
			image.sh = 1;
			image.tl = 0;
			image.th = 1;
		}

		return image;
	}

	/*
	================
	GL_LoadWal
	================
	*/
	private image_t GL_LoadWal(string name)
	{
		image_t image = null;

		var raw = FS.LoadFile(name);

		if (raw == null)
		{
			VID.Printf(Defines.PRINT_ALL, "GL_FindImage: can't load " + name + '\n');

			return this.r_notexture;
		}

		miptex_t mt = new(raw);

		var pix = new byte[mt.width * mt.height];
		Array.Copy(raw, mt.offsets[0], pix, 0, pix.Length);

		image = this.GL_LoadPic(name, pix, mt.width, mt.height, Base.it_wall, 8);

		return image;
	}

	/*
	===============
	GL_FindImage
	
	Finds or loads the given image
	===============
	*/
	protected image_t GL_FindImage(string name, int type)
	{
		image_t image = null;

		name = name.ToLower();

		// bughack for bad strings (fuck \0)
		var index = name.IndexOf('\0');

		if (index != -1)
			name = name[..index];

		if (name == null || name.Length < 5)
			return null; //	Com.Error (ERR_DROP, "GL_FindImage: NULL name");

		//	Com.Error (ERR_DROP, "GL_FindImage: bad name: %s", name);

		// look for it
		for (var i = 0; i < this.numgltextures; i++)
		{
			image = this.gltextures[i];

			if (name.Equals(image.name))
			{
				image.registration_sequence = this.registration_sequence;

				return image;
			}
		}

		//
		// load the pic from disk
		//

		byte[] pic = null;
		var dim = new Size();

		if (name.EndsWith(".pcx"))
		{
			pic = this.LoadPCX(name, null, ref dim);

			if (pic == null)
				return null;

			image = this.GL_LoadPic(name, pic, dim.Width, dim.Height, type, 8);
		}
		else if (name.EndsWith(".wal"))
			image = this.GL_LoadWal(name);
		else if (name.EndsWith(".tga"))
		{
			pic = this.LoadTGA(name, ref dim);

			if (pic == null)
				return null;

			image = this.GL_LoadPic(name, pic, dim.Width, dim.Height, type, 32);
		}
		else
			return null;

		return image;
	}

	/*
	===============
	R_RegisterSkin
	===============
	*/
	public override image_t R_RegisterSkin(string name)
	{
		return this.GL_FindImage(name, Base.it_skin);
	}

	private readonly int[] texnum = new int[1];

	/*
	================
	GL_FreeUnusedImages
	
	Any image that was not touched on this registration sequence
	will be freed.
	================
	*/
	protected void GL_FreeUnusedImages()
	{
		// never free r_notexture or particle texture
		this.r_notexture.registration_sequence = this.registration_sequence;
		this.r_particletexture.registration_sequence = this.registration_sequence;

		image_t image = null;

		for (var i = 0; i < this.numgltextures; i++)
		{
			image = this.gltextures[i];

			// used this sequence
			if (image.registration_sequence == this.registration_sequence)
				continue;

			// free image_t slot
			if (image.registration_sequence == 0)
				continue;

			// don't free pics
			if (image.type == Base.it_pic)
				continue;

			// free it
			this.texnum[0] = image.texnum;
			this.gl.glDeleteTextures(this.texnum);
			image.clear();
		}
	}

	/*
	===============
	Draw_GetPalette
	===============
	*/
	protected void Draw_GetPalette()
	{
		int r, g, b;
		var palette = new byte[1][]; //new byte[768];

		// get the palette
		var dim = new Size();
		this.LoadPCX("pics/colormap.pcx", palette, ref dim);

		if (palette[0] == null || palette[0].Length != 768)
			Com.Error(Defines.ERR_FATAL, "Couldn't load pics/colormap.pcx");

		var pal = palette[0];

		var j = 0;

		for (var i = 0; i < 256; i++)
		{
			r = pal[j++] & 0xFF;
			g = pal[j++] & 0xFF;
			b = pal[j++] & 0xFF;

			OpenGLRenderApi.d_8to24table[i] = (255 << 24) | (b << 16) | (g << 8) | (r << 0);
		}

		OpenGLRenderApi.d_8to24table[255] &= 0x00FFFFFF; // 255 is transparent

		particle_t.setColorPalette(OpenGLRenderApi.d_8to24table);
	}

	/*
	===============
	GL_InitImages
	===============
	*/
	protected void GL_InitImages()
	{
		int i, j;
		var g = this.vid_gamma.value;

		this.registration_sequence = 1;

		// init intensity conversions
		this.intensity = Cvar.Get("intensity", "2", 0);

		if (this.intensity.value <= 1)
			Cvar.Set("intensity", "1");

		this.gl_state.inverse_intensity = 1 / this.intensity.value;

		this.Draw_GetPalette();

		if (this.qglColorTableEXT)
		{
			this.gl_state.d_16to8table = FS.LoadFile("pics/16to8.dat");

			if (this.gl_state.d_16to8table == null)
				Com.Error(Defines.ERR_FATAL, "Couldn't load pics/16to8.pcx");
		}

		if ((this.gl_config.renderer & (Base.GL_RENDERER_VOODOO | Base.GL_RENDERER_VOODOO2)) != 0)
			g = 1.0F;

		for (i = 0; i < 256; i++)
		{
			if (g == 1.0f)
				this.gammatable[i] = (byte)i;
			else
			{
				var inf = (int)(255.0f * Math.Pow((i + 0.5) / 255.5, g) + 0.5);

				if (inf < 0)
					inf = 0;

				if (inf > 255)
					inf = 255;

				this.gammatable[i] = (byte)inf;
			}
		}

		for (i = 0; i < 256; i++)
		{
			j = (int)(i * this.intensity.value);

			if (j > 255)
				j = 255;

			this.intensitytable[i] = (byte)j;
		}
	}

	/*
	===============
	GL_ShutdownImages
	===============
	*/
	protected void GL_ShutdownImages()
	{
		image_t image;

		for (var i = 0; i < this.numgltextures; i++)
		{
			image = this.gltextures[i];

			if (image.registration_sequence == 0)
				continue; // free image_t slot

			// free it

			this.texnum[0] = image.texnum;
			this.gl.glDeleteTextures(this.texnum);
			image.clear();
		}
	}
}