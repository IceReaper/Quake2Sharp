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
namespace Quake2Sharp.sound.types;

public class sfx_t
{
	public string name;
	public int registration_sequence;
	public sfxcache_t cache;
	public string truename;

	// is used for AL buffers
	public int bufferId = -1;
	public bool isCached;

	public void clear()
	{
		this.name = this.truename = null;
		this.cache = null;
		this.registration_sequence = 0;
		this.bufferId = -1;
		this.isCached = false;
	}
}