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
namespace QClient.client.types;

using render.types;
using util;

// ok!
public class entity_t
{
	//ptr
	public model_t? model; // opaque type outside refresh
	public float[] angles = { 0, 0, 0 };

	/*
	** most recent data
	*/
	public float[] origin = { 0, 0, 0 }; // also used as RF_BEAM's "from"
	public int frame; // also used as RF_BEAM's diameter

	/*
	** previous data for lerping
	*/
	public float[] oldorigin = { 0, 0, 0 }; // also used as RF_BEAM's "to"
	public int oldframe;

	/*
	** misc
	*/
	public float backlerp; // 0.0 = current, 1.0 = old
	public int skinnum; // also used as RF_BEAM's palette index
	public int lightstyle; // for flashing entities
	public float alpha; // ignore if RF_TRANSLUCENT isn't set

	// reference
	public image_t skin; // NULL for inline skin
	public int flags;

	public void set(entity_t src)
	{
		this.model = src.model;
		Math3D.VectorCopy(src.angles, this.angles);
		Math3D.VectorCopy(src.origin, this.origin);
		this.frame = src.frame;
		Math3D.VectorCopy(src.oldorigin, this.oldorigin);
		this.oldframe = src.oldframe;
		this.backlerp = src.backlerp;
		this.skinnum = src.skinnum;
		this.lightstyle = src.lightstyle;
		this.alpha = src.alpha;
		this.skin = src.skin;
		this.flags = src.flags;
	}

	public void clear()
	{
		this.model = null;
		Math3D.VectorClear(this.angles);
		Math3D.VectorClear(this.origin);
		this.frame = 0;
		Math3D.VectorClear(this.oldorigin);
		this.oldframe = 0;
		this.backlerp = 0;
		this.skinnum = 0;
		this.lightstyle = 0;
		this.alpha = 0;
		this.skin = null;
		this.flags = 0;
	}
}