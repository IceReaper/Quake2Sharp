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
namespace QClient.render;

using client.types;
using sys;
using System.Drawing;
using types;

public class DummyRenderer : refexport_t
{
	public bool Init(int vid_xpos, int vid_ypos)
	{
		return false;
	}

	public void Shutdown()
	{
	}

	public void BeginRegistration(string map)
	{
	}

	public model_t RegisterModel(string name)
	{
		return null;
	}

	public image_t RegisterSkin(string name)
	{
		return null;
	}

	public image_t RegisterPic(string name)
	{
		return null;
	}

	public void SetSky(string name, float rotate, float[] axis)
	{
	}

	public void EndRegistration()
	{
	}

	public void RenderFrame(refdef_t fd)
	{
	}

	public void DrawGetPicSize(ref Size dim, string name)
	{
	}

	public void DrawPic(int x, int y, string name)
	{
	}

	public void DrawStretchPic(int x, int y, int w, int h, string name)
	{
	}

	public void DrawChar(int x, int y, int num)
	{
	}

	public void DrawTileClear(int x, int y, int w, int h, string name)
	{
	}

	public void DrawFill(int x, int y, int w, int h, int c)
	{
	}

	public void DrawFadeScreen()
	{
	}

	public void DrawStretchRaw(int x, int y, int w, int h, int cols, int rows, byte[] data)
	{
	}

	public void CinematicSetPalette(byte[] palette)
	{
	}

	public void BeginFrame(float camera_separation)
	{
	}

	public void EndFrame()
	{
	}

	public void updateScreen(Action callback)
	{
		callback();
	}

	public int apiVersion()
	{
		return 0;
	}

	public DisplayMode[] getModeList()
	{
		return null;
	}

	public KBD getKeyboardHandler()
	{
		return null;
	}
}