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

using IdTech.common;

namespace Quake2Sharp.render.opengl;

using client;
using qcommon;
using render.types;
using System.Drawing;

public partial class OpenGLRenderApi
{
	/*
	===============
	Draw_InitLocal
	===============
	*/
	protected void Draw_InitLocal()
	{
		// load console characters (don't bilerp characters)
		this.draw_chars = this.GL_FindImage("pics/conchars.pcx", Base.it_pic);
		this.GL_Bind(this.draw_chars.texnum);
		this.gl.glTexParameterf(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_NEAREST);
		this.gl.glTexParameterf(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_NEAREST);
	}

	/*
	================
	Draw_Char

	Draws one 8*8 graphics character with 0 being transparent.
	It can be clipped to the top of the screen to allow the console to be
	smoothly scrolled off.
	================
	*/
	public override void Draw_Char(int x, int y, int num)
	{
		num &= 255;

		if ((num & 127) == 32)
			return; // space

		if (y <= -8)
			return; // totally off screen

		var row = num >> 4;
		var col = num & 15;

		var frow = row * 0.0625f;
		var fcol = col * 0.0625f;
		var size = 0.0625f;

		this.GL_Bind(this.draw_chars.texnum);

		this.gl.glBegin(OpenGL.GL_QUADS);
		this.gl.glTexCoord2f(fcol, frow);
		this.gl.glVertex2f(x, y);
		this.gl.glTexCoord2f(fcol + size, frow);
		this.gl.glVertex2f(x + 8, y);
		this.gl.glTexCoord2f(fcol + size, frow + size);
		this.gl.glVertex2f(x + 8, y + 8);
		this.gl.glTexCoord2f(fcol, frow + size);
		this.gl.glVertex2f(x, y + 8);
		this.gl.glEnd();
	}

	/*
	=============
	Draw_FindPic
	=============
	*/
	public override image_t Draw_FindPic(string name)
	{
		if (!name.StartsWith("/") && !name.StartsWith("\\"))
		{
			var fullname = "pics/" + name + ".pcx";

			return this.GL_FindImage(fullname, Base.it_pic);
		}

		return this.GL_FindImage(name[1..], Base.it_pic);
	}

	/*
	=============
	Draw_GetPicSize
	=============
	*/
	public override void Draw_GetPicSize(ref Size dim, string pic)
	{
		var image = this.Draw_FindPic(pic);
		dim.Width = image?.width ?? -1;
		dim.Height = image?.height ?? -1;
	}

	/*
	=============
	Draw_StretchPic
	=============
	*/
	public override void Draw_StretchPic(int x, int y, int w, int h, string pic)
	{
		image_t image;

		image = this.Draw_FindPic(pic);

		if (image == null)
		{
			VID.Printf(Defines.PRINT_ALL, "Can't find pic: " + pic + '\n');

			return;
		}

		if (this.scrap_dirty)
			this.Scrap_Upload();

		if ((this.gl_config.renderer == Base.GL_RENDERER_MCD || (this.gl_config.renderer & Base.GL_RENDERER_RENDITION) != 0) && !image.has_alpha)
			this.gl.glDisable(OpenGL.GL_ALPHA_TEST);

		this.GL_Bind(image.texnum);
		this.gl.glBegin(OpenGL.GL_QUADS);
		this.gl.glTexCoord2f(image.sl, image.tl);
		this.gl.glVertex2f(x, y);
		this.gl.glTexCoord2f(image.sh, image.tl);
		this.gl.glVertex2f(x + w, y);
		this.gl.glTexCoord2f(image.sh, image.th);
		this.gl.glVertex2f(x + w, y + h);
		this.gl.glTexCoord2f(image.sl, image.th);
		this.gl.glVertex2f(x, y + h);
		this.gl.glEnd();

		if ((this.gl_config.renderer == Base.GL_RENDERER_MCD || (this.gl_config.renderer & Base.GL_RENDERER_RENDITION) != 0) && !image.has_alpha)
			this.gl.glEnable(OpenGL.GL_ALPHA_TEST);
	}

	/*
	=============
	Draw_Pic
	=============
	*/
	public override void Draw_Pic(int x, int y, string pic)
	{
		image_t image;

		image = this.Draw_FindPic(pic);

		if (image == null)
		{
			VID.Printf(Defines.PRINT_ALL, "Can't find pic: " + pic + '\n');

			return;
		}

		if (this.scrap_dirty)
			this.Scrap_Upload();

		if ((this.gl_config.renderer == Base.GL_RENDERER_MCD || (this.gl_config.renderer & Base.GL_RENDERER_RENDITION) != 0) && !image.has_alpha)
			this.gl.glDisable(OpenGL.GL_ALPHA_TEST);

		this.GL_Bind(image.texnum);

		this.gl.glBegin(OpenGL.GL_QUADS);
		this.gl.glTexCoord2f(image.sl, image.tl);
		this.gl.glVertex2f(x, y);
		this.gl.glTexCoord2f(image.sh, image.tl);
		this.gl.glVertex2f(x + image.width, y);
		this.gl.glTexCoord2f(image.sh, image.th);
		this.gl.glVertex2f(x + image.width, y + image.height);
		this.gl.glTexCoord2f(image.sl, image.th);
		this.gl.glVertex2f(x, y + image.height);
		this.gl.glEnd();

		if ((this.gl_config.renderer == Base.GL_RENDERER_MCD || (this.gl_config.renderer & Base.GL_RENDERER_RENDITION) != 0) && !image.has_alpha)
			this.gl.glEnable(OpenGL.GL_ALPHA_TEST);
	}

	/*
	=============
	Draw_TileClear

	This repeats a 64*64 tile graphic to fill the screen around a sized down
	refresh window.
	=============
	*/
	public override void Draw_TileClear(int x, int y, int w, int h, string pic)
	{
		image_t image;

		image = this.Draw_FindPic(pic);

		if (image == null)
		{
			VID.Printf(Defines.PRINT_ALL, "Can't find pic: " + pic + '\n');

			return;
		}

		if ((this.gl_config.renderer == Base.GL_RENDERER_MCD || (this.gl_config.renderer & Base.GL_RENDERER_RENDITION) != 0) && !image.has_alpha)
			this.gl.glDisable(OpenGL.GL_ALPHA_TEST);

		this.GL_Bind(image.texnum);
		this.gl.glBegin(OpenGL.GL_QUADS);
		this.gl.glTexCoord2f(x / 64.0f, y / 64.0f);
		this.gl.glVertex2f(x, y);
		this.gl.glTexCoord2f((x + w) / 64.0f, y / 64.0f);
		this.gl.glVertex2f(x + w, y);
		this.gl.glTexCoord2f((x + w) / 64.0f, (y + h) / 64.0f);
		this.gl.glVertex2f(x + w, y + h);
		this.gl.glTexCoord2f(x / 64.0f, (y + h) / 64.0f);
		this.gl.glVertex2f(x, y + h);
		this.gl.glEnd();

		if ((this.gl_config.renderer == Base.GL_RENDERER_MCD || (this.gl_config.renderer & Base.GL_RENDERER_RENDITION) != 0) && !image.has_alpha)
			this.gl.glEnable(OpenGL.GL_ALPHA_TEST);
	}

	/*
	=============
	Draw_Fill

	Fills a box of pixels with a single color
	=============
	*/
	public override void Draw_Fill(int x, int y, int w, int h, int colorIndex)
	{
		if (colorIndex > 255)
			clientserver.Com_Error(Defines.ERR_FATAL, "Draw_Fill: bad color");

		this.gl.glDisable(OpenGL.GL_TEXTURE_2D);

		var color = OpenGLRenderApi.d_8to24table[colorIndex];

		this.gl.glColor3ub(
			(byte)((color >> 0) & 0xff), // r
			(byte)((color >> 8) & 0xff), // g
			(byte)((color >> 16) & 0xff) // b
		);

		this.gl.glBegin(OpenGL.GL_QUADS);

		this.gl.glVertex2f(x, y);
		this.gl.glVertex2f(x + w, y);
		this.gl.glVertex2f(x + w, y + h);
		this.gl.glVertex2f(x, y + h);

		this.gl.glEnd();
		this.gl.glColor3f(1, 1, 1);
		this.gl.glEnable(OpenGL.GL_TEXTURE_2D);
	}

	//	  =============================================================================

	/*
	================
	Draw_FadeScreen
	================
	*/
	public override void Draw_FadeScreen()
	{
		this.gl.glEnable(OpenGL.GL_BLEND);
		this.gl.glDisable(OpenGL.GL_TEXTURE_2D);
		this.gl.glColor4f(0, 0, 0, 0.8f);
		this.gl.glBegin(OpenGL.GL_QUADS);

		this.gl.glVertex2f(0, 0);
		this.gl.glVertex2f(Base.vid.getWidth(), 0);
		this.gl.glVertex2f(Base.vid.getWidth(), Base.vid.getHeight());
		this.gl.glVertex2f(0, Base.vid.getHeight());

		this.gl.glEnd();
		this.gl.glColor4f(1, 1, 1, 1);
		this.gl.glEnable(OpenGL.GL_TEXTURE_2D);
		this.gl.glDisable(OpenGL.GL_BLEND);
	}

	// ====================================================================
	private readonly int[] image32 = new int[256 * 256];
	private readonly byte[] image8 = new byte[256 * 256];

	/*
	=============
	Draw_StretchRaw
	=============
	*/
	public override void Draw_StretchRaw(int x, int y, int w, int h, int cols, int rows, byte[] data)
	{
		int i, j, trows;
		int sourceIndex;
		int frac, fracstep;
		float hscale;
		int row;
		float t;

		this.GL_Bind(0);

		if (rows <= 256)
		{
			hscale = 1;
			trows = rows;
		}
		else
		{
			hscale = rows / 256.0f;
			trows = 256;
		}

		t = rows * hscale / 256;

		if (!this.qglColorTableEXT)
		{
			Array.Clear(this.image32, 0, this.image32.Length);
			var destIndex = 0;

			for (i = 0; i < trows; i++)
			{
				row = (int)(i * hscale);

				if (row > rows)
					break;

				sourceIndex = cols * row;
				destIndex = i * 256;
				fracstep = cols * 0x10000 / 256;
				frac = fracstep >> 1;

				for (j = 0; j < 256; j++)
				{
					this.image32[destIndex + j] = this.r_rawpalette[data[sourceIndex + (frac >> 16)] & 0xff];
					frac += fracstep;
				}
			}

			this.gl.glTexImage2D(OpenGL.GL_TEXTURE_2D, 0, this.gl_tex_solid_format, 256, 256, 0, OpenGL.GL_RGBA, OpenGL.GL_UNSIGNED_BYTE, this.image32);
		}
		else
		{
			Array.Clear(this.image8, 0, this.image8.Length);
			var destIndex = 0;
			;

			for (i = 0; i < trows; i++)
			{
				row = (int)(i * hscale);

				if (row > rows)
					break;

				sourceIndex = cols * row;
				destIndex = i * 256;
				fracstep = cols * 0x10000 / 256;
				frac = fracstep >> 1;

				for (j = 0; j < 256; j++)
				{
					this.image8[destIndex + j] = data[sourceIndex + (frac >> 16)];
					frac += fracstep;
				}
			}

			this.gl.glTexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_COLOR_INDEX, 256, 256, 0, OpenGL.GL_COLOR_INDEX, OpenGL.GL_UNSIGNED_BYTE, this.image8);
		}

		this.gl.glTexParameterf(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_LINEAR);
		this.gl.glTexParameterf(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_LINEAR);

		if (this.gl_config.renderer == Base.GL_RENDERER_MCD || (this.gl_config.renderer & Base.GL_RENDERER_RENDITION) != 0)
			this.gl.glDisable(OpenGL.GL_ALPHA_TEST);

		this.gl.glBegin(OpenGL.GL_QUADS);
		this.gl.glTexCoord2f(0, 0);
		this.gl.glVertex2f(x, y);
		this.gl.glTexCoord2f(1, 0);
		this.gl.glVertex2f(x + w, y);
		this.gl.glTexCoord2f(1, t);
		this.gl.glVertex2f(x + w, y + h);
		this.gl.glTexCoord2f(0, t);
		this.gl.glVertex2f(x, y + h);
		this.gl.glEnd();

		if (this.gl_config.renderer == Base.GL_RENDERER_MCD || (this.gl_config.renderer & Base.GL_RENDERER_RENDITION) != 0)
			this.gl.glEnable(OpenGL.GL_ALPHA_TEST);
	}

	public override bool R_IsVSyncActive()
	{
		return Base.glImpl.IsVSyncActive();
	}

	public override int GL_GetRefreshRate()
	{
		return Base.glImpl.GetRefreshRate();
	}
}
