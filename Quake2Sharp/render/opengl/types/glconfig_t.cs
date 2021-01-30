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
namespace Quake2Sharp.render.opengl.types
{
	using System;
	using System.Globalization;

	public class glconfig_t
	{
		public int renderer;
		public string renderer_string;
		public string vendor_string;
		public string version_string;
		public string extensions_string;
		public bool allow_cds;
		private float version = 1.1f;

		public void parseOpenGLVersion()
		{
			try
			{
				this.version = float.Parse(this.version_string[..3], CultureInfo.InvariantCulture);
			}
			catch (Exception)
			{
				this.version = 1.1f;
			}
		}

		public float getOpenGLVersion()
		{
			return this.version;
		}
	}
}
