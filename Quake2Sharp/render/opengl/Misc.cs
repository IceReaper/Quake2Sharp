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
using qcommon;
using System.Text;

public partial class OpenGLRenderApi
{
	////	   r_misc.c
	//
	//	#include "gl_local.h"
	//
	/*
	==================
	R_InitParticleTexture
	==================
	*/
	private readonly byte[][] dottexture =
	{
		new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 },
		new byte[] { 0, 0, 1, 1, 0, 0, 0, 0 },
		new byte[] { 0, 1, 1, 1, 1, 0, 0, 0 },
		new byte[] { 0, 1, 1, 1, 1, 0, 0, 0 },
		new byte[] { 0, 0, 1, 1, 0, 0, 0, 0 },
		new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 },
		new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 },
		new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }
	};

	protected void R_InitParticleTexture()
	{
		int x, y;
		var data = new byte[8 * 8 * 4];

		//
		// particle texture
		//
		for (x = 0; x < 8; x++)
		{
			for (y = 0; y < 8; y++)
			{
				data[y * 32 + x * 4 + 0] = (byte)255;
				data[y * 32 + x * 4 + 1] = (byte)255;
				data[y * 32 + x * 4 + 2] = (byte)255;
				data[y * 32 + x * 4 + 3] = (byte)(this.dottexture[x][y] * 255);
			}
		}

		this.r_particletexture = this.GL_LoadPic("***particle***", data, 8, 8, Base.it_sprite, 32);

		//
		// also use this for bad textures, but without alpha
		//
		for (x = 0; x < 8; x++)
		{
			for (y = 0; y < 8; y++)
			{
				data[y * 32 + x * 4 + 0] = (byte)(this.dottexture[x & 3][y & 3] * 255);
				data[y * 32 + x * 4 + 1] = 0; // dottexture[x&3][y&3]*255;
				data[y * 32 + x * 4 + 2] = 0; //dottexture[x&3][y&3]*255;
				data[y * 32 + x * 4 + 3] = (byte)255;
			}
		}

		this.r_notexture = this.GL_LoadPic("***r_notexture***", data, 8, 8, Base.it_wall, 32);
	}

	//	/*
	//	==============================================================================
	// 
	//							SCREEN SHOTS
	// 
	//	==============================================================================
	//	*/
	//
	//	typedef struct _TargaHeader {
	//		unsigned char 	id_length, colormap_type, image_type;
	//		unsigned short	colormap_index, colormap_length;
	//		unsigned char	colormap_size;
	//		unsigned short	x_origin, y_origin, width, height;
	//		unsigned char	pixel_size, attributes;
	//	} TargaHeader;

	/**
	 * GL_ScreenShot_f
	 */
	public override void GL_ScreenShot_f()
	{
		StringBuilder sb = new(FS.Gamedir() + "/scrshot/screenshot00.tga");
		FS.CreatePath(sb.ToString());
		var file = sb.ToString();

		// find a valid file name
		var i = 0;
		var offset = sb.Length - 6;

		while (File.Exists(file) && i++ < 100)
		{
			sb[offset] = (char)(i / 10 + '0');
			sb[offset + 1] = (char)(i % 10 + '0');
			file = sb.ToString();
		}

		if (i == 100)
		{
			VID.Printf(Defines.PRINT_ALL, "Clean up your screenshots\n");

			return;
		}

		try
		{
			Stream @out = File.OpenWrite(file);
			BinaryWriter image = new(@out);

			// write the TGA header
			image.Write((byte)0);
			image.Write((byte)0);
			image.Write((byte)2); // uncompressed type
			image.Write(new byte[9]);
			image.Write((short)Base.vid.getWidth()); // vid.getWidth()
			image.Write((short)Base.vid.getHeight()); // vid.getHeight()
			image.Write((byte)24); // pixel size
			image.Write(new byte[1]);

			var rgb = new byte[Base.vid.getWidth() * Base.vid.getHeight() * 3];

			// change pixel alignment for reading
			if (Base.vid.getWidth() % 4 != 0)
				this.gl.glPixelStorei(OpenGL.GL_PACK_ALIGNMENT, 1);

			// OpenGL 1.2+ supports the GL_BGR color format
			// check the GL_VERSION to use the TARGA BGR order if possible
			// e.g.: 1.5.2 NVIDIA 66.29
			if (this.gl_config.getOpenGLVersion() >= 1.2f)
			{
				// read the BGR values into the image buffer
				this.gl.glReadPixels(0, 0, Base.vid.getWidth(), Base.vid.getHeight(), OpenGL.GL_BGR, OpenGL.GL_UNSIGNED_BYTE, rgb);
			}
			else
			{
				// read the RGB values into the image buffer
				this.gl.glReadPixels(0, 0, Base.vid.getWidth(), Base.vid.getHeight(), OpenGL.GL_RGB, OpenGL.GL_UNSIGNED_BYTE, rgb);

				// flip RGB to BGR
				for (i = 0; i < rgb.Length; i += 3)
				{
					image.Write(rgb[i + 2]);
					image.Write(rgb[i + 1]);
					image.Write(rgb[i + 0]);
				}
			}

			// reset to default alignment
			this.gl.glPixelStorei(OpenGL.GL_PACK_ALIGNMENT, 4);

			// close the file channel
			@out.Close();
		}
		catch (Exception e)
		{
			VID.Printf(Defines.PRINT_ALL, e.Message + '\n');
		}

		VID.Printf(Defines.PRINT_ALL, "Wrote " + file + '\n');
	}

	/*
	** GL_Strings_f
	*/
	protected void GL_Strings_f()
	{
		VID.Printf(Defines.PRINT_ALL, "GL_VENDOR: " + this.gl_config.vendor_string + '\n');
		VID.Printf(Defines.PRINT_ALL, "GL_RENDERER: " + this.gl_config.renderer_string + '\n');
		VID.Printf(Defines.PRINT_ALL, "GL_VERSION: " + this.gl_config.version_string + '\n');
		VID.Printf(Defines.PRINT_ALL, "GL_EXTENSIONS: " + this.gl_config.extensions_string + '\n');
	}

	/*
	** GL_SetDefaultState
	*/
	protected void GL_SetDefaultState()
	{
		this.gl.glClearColor(1f, 0f, 0.5f, 0.5f); // original quake2

		//gl.glClearColor(0, 0, 0, 0); // replaced with black
		this.gl.glCullFace(OpenGL.GL_FRONT);
		this.gl.glEnable(OpenGL.GL_TEXTURE_2D);

		this.gl.glEnable(OpenGL.GL_ALPHA_TEST);
		this.gl.glAlphaFunc(OpenGL.GL_GREATER, 0.666f);

		this.gl.glDisable(OpenGL.GL_DEPTH_TEST);
		this.gl.glDisable(OpenGL.GL_CULL_FACE);
		this.gl.glDisable(OpenGL.GL_BLEND);

		this.gl.glColor4f(1, 1, 1, 1);

		this.gl.glPolygonMode(OpenGL.GL_FRONT_AND_BACK, OpenGL.GL_FILL);
		this.gl.glShadeModel(OpenGL.GL_FLAT);

		this.GL_TextureMode(this.gl_texturemode.@string);
		this.GL_TextureAlphaMode(this.gl_texturealphamode.@string);
		this.GL_TextureSolidMode(this.gl_texturesolidmode.@string);

		this.gl.glTexParameterf(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, this.gl_filter_min);
		this.gl.glTexParameterf(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, this.gl_filter_max);

		this.gl.glTexParameterf(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_S, OpenGL.GL_REPEAT);
		this.gl.glTexParameterf(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_T, OpenGL.GL_REPEAT);

		this.gl.glBlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA);

		this.GL_TexEnv(OpenGL.GL_REPLACE);

		if (this.qglPointParameterfEXT)
		{
			//float[] attenuations = { gl_particle_att_a.value, gl_particle_att_b.value, gl_particle_att_c.value };
			var attenuations = new float[4];
			attenuations[0] = this.gl_particle_att_a.value;
			attenuations[1] = this.gl_particle_att_b.value;
			attenuations[2] = this.gl_particle_att_c.value;

			this.gl.glEnable(OpenGL.GL_POINT_SMOOTH);
			this.gl.glPointParameterfEXT(OpenGL.GL_POINT_SIZE_MIN_EXT, this.gl_particle_min_size.value);
			this.gl.glPointParameterfEXT(OpenGL.GL_POINT_SIZE_MAX_EXT, this.gl_particle_max_size.value);
			this.gl.glPointParameterEXT(OpenGL.GL_DISTANCE_ATTENUATION_EXT, attenuations);
		}

		if (this.qglColorTableEXT && this.gl_ext_palettedtexture.value != 0.0f)
		{
			this.gl.glEnable(OpenGL.GL_SHARED_TEXTURE_PALETTE_EXT);

			this.GL_SetTexturePalette(OpenGLRenderApi.d_8to24table);
		}

		this.GL_UpdateSwapInterval();
	}

	protected void GL_UpdateSwapInterval()
	{
		if (this.gl_swapinterval.modified)
		{
			this.gl_swapinterval.modified = false;

			if (!this.gl_state.stereo_enabled)
				this.gl.setSwapInterval((int)this.gl_swapinterval.value);
		}
	}
}