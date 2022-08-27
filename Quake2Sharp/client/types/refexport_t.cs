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
namespace Quake2Sharp.client.types;

using render;
using render.types;
using sys;
using System;
using System.Drawing;

public interface refexport_t
{
	// ============================================================================
	// public interface for Renderer implementations
	//
	// ref.h, refexport_t
	// ============================================================================
	//
	// these are the functions exported by the refresh module
	//
	// called when the library is loaded
	bool Init(int vid_xpos, int vid_ypos);

	// called before the library is unloaded
	void Shutdown();

	// All data that will be used in a level should be
	// registered before rendering any frames to prevent disk hits,
	// but they can still be registered at a later time
	// if necessary.
	//
	// EndRegistration will free any remaining data that wasn't registered.
	// Any model_s or skin_s pointers from before the BeginRegistration
	// are no longer valid after EndRegistration.
	//
	// Skins and images need to be differentiated, because skins
	// are flood filled to eliminate mip map edge errors, and pics have
	// an implicit "pics/" prepended to the name. (a pic name that starts with a
	// slash will not use the "pics/" prefix or the ".pcx" postfix)
	void BeginRegistration(string map);
	model_t RegisterModel(string name);
	image_t RegisterSkin(string name);
	image_t RegisterPic(string name);

	void SetSky(
		string name,
		float rotate, /* vec3_t */
		float[] axis
	);

	void EndRegistration();
	void RenderFrame(refdef_t fd);
	void DrawGetPicSize(ref Size dim /* int *w, *h */, string name);

	// will return 0 0 if not found
	void DrawPic(int x, int y, string name);
	void DrawStretchPic(int x, int y, int w, int h, string name);
	void DrawChar(int x, int y, int num); // num is 8 bit ASCII
	void DrawTileClear(int x, int y, int w, int h, string name);
	void DrawFill(int x, int y, int w, int h, int c);
	void DrawFadeScreen();

	// Draw images for cinematic rendering (which can have a different palette). Note that calls
	void DrawStretchRaw(int x, int y, int w, int h, int cols, int rows, byte[] data);

	/*
	** video mode and refresh state management entry points
	*/
	/* 256 r,g,b values;	null = game palette, size = 768 bytes */
	void CinematicSetPalette(byte[] palette);
	void BeginFrame(float camera_separation);
	void EndFrame();

	/**
	 * 
	 *
	 */
	void updateScreen(Action callback);

	int apiVersion();
	DisplayMode[] getModeList();
	KBD getKeyboardHandler();
}