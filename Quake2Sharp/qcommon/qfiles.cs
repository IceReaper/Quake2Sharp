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
namespace Quake2Sharp.qcommon;

public class qfiles
{
	public const int IDALIASHEADER = ('2' << 24) + ('P' << 16) + ('D' << 8) + 'I';
	public static readonly int ALIAS_VERSION = 8;
	public static readonly int MAX_TRIANGLES = 4096;
	public static readonly int MAX_VERTS = 2048;
	public static readonly int MAX_FRAMES = 512;
	public static readonly int MAX_MD2SKINS = 32;
	public static readonly int MAX_SKINNAME = 64;
	public static readonly int DTRIVERTX_V0 = 0;
	public static readonly int DTRIVERTX_V1 = 1;
	public static readonly int DTRIVERTX_V2 = 2;
	public static readonly int DTRIVERTX_LNI = 3;
	public static readonly int DTRIVERTX_SIZE = 4;
	public const int IDSPRITEHEADER = ('2' << 24) + ('S' << 16) + ('D' << 8) + 'I';
	public static readonly int SPRITE_VERSION = 2;
	public const int IDBSPHEADER = ('P' << 24) + ('S' << 16) + ('B' << 8) + 'I';
}