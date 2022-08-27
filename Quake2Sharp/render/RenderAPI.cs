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
namespace Quake2Sharp.render;

using client.types;
using System.Drawing;
using types;

public abstract class RenderAPI
{
	public abstract bool R_Init(int vid_xpos, int vid_ypos);
	public abstract bool R_Init2();
	public abstract void R_Shutdown();
	public abstract void R_BeginRegistration(string map);
	public abstract model_t R_RegisterModel(string name);
	public abstract image_t R_RegisterSkin(string name);
	public abstract image_t Draw_FindPic(string name);
	public abstract void R_SetSky(string name, float rotate, float[] axis);
	public abstract void R_EndRegistration();
	public abstract void R_RenderFrame(refdef_t fd);
	public abstract void Draw_GetPicSize(ref Size dim, string name);
	public abstract void Draw_Pic(int x, int y, string name);
	public abstract void Draw_StretchPic(int x, int y, int w, int h, string name);
	public abstract void Draw_Char(int x, int y, int num);
	public abstract void Draw_TileClear(int x, int y, int w, int h, string name);
	public abstract void Draw_Fill(int x, int y, int w, int h, int c);
	public abstract void Draw_FadeScreen();
	public abstract void Draw_StretchRaw(int x, int y, int w, int h, int cols, int rows, byte[] data);
	public abstract void R_SetPalette(byte[] palette);
	public abstract void R_BeginFrame(float camera_separation);
	public abstract void GL_ScreenShot_f();
}