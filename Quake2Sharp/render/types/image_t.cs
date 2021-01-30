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
namespace Quake2Sharp.render.types
{
	public class image_t
	{
		public static readonly int MAX_NAME_SIZE = Defines.MAX_QPATH;

		// used to get the pos in array
		private readonly int id;

		// quake 2 variables
		public string name = ""; // game path, including extension

		// enum imagetype_t
		public int type;
		public int width, height; // source image
		public int upload_width, upload_height; // after power of two and picmip
		public int registration_sequence; // 0 = free
		public msurface_t texturechain; // for sort-by-texture world drawing
		public int texnum; // gl texture binding
		public float sl, tl, sh, th; // 0,0 - 1,1 unless part of the scrap
		public bool scrap;
		public bool has_alpha;
		public bool paletted;

		public image_t(int id)
		{
			this.id = id;
		}

		public void clear()
		{
			// don't clear the id
			// wichtig !!!
			this.name = "";
			this.type = 0;
			this.width = this.height = 0;
			this.upload_width = this.upload_height = 0;
			this.registration_sequence = 0; // 0 = free
			this.texturechain = null;
			this.texnum = 0; // gl texture binding
			this.sl = this.tl = this.sh = this.th = 0;
			this.scrap = false;
			this.has_alpha = false;
			this.paletted = false;
		}

		public int getId()
		{
			return this.id;
		}

		public string toString()
		{
			return this.name + ":" + this.texnum;
		}
	}
}
