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
namespace QClient.game.types;

using util;

public class entity_state_t
{
	/** entity_state_t is the information conveyed from the server
		in an update message about entities that the client will
		need to render in some way. */
	public entity_state_t(edict_t ent)
	{
		this.surrounding_ent = ent;

		if (ent != null)
			this.number = ent.index;
	}

	/** edict index. TODO: this is critical. The index has to be proper managed. */
	public int number;

	public edict_t surrounding_ent;
	public float[] origin = { 0, 0, 0 };
	public float[] angles = { 0, 0, 0 };

	/** for lerping. */
	public float[] old_origin = { 0, 0, 0 };

	public int modelindex;

	/** weapons, CTF flags, etc. */
	public int modelindex2, modelindex3, modelindex4;

	public int frame;
	public int skinnum;

	/** PGM - we're filling it, so it needs to be unsigned. */
	public int effects;

	public int renderfx;
	public int solid;

	// for client side prediction, 8*(bits 0-4) is x/y radius
	// 8*(bits 5-9) is z down distance, 8(bits10-15) is z up
	// gi.linkentity sets this properly
	public int sound; // for looping sounds, to guarantee shutoff
	public int @event; // impulse events -- muzzle flashes, footsteps, etc

	// events only go out for a single frame, they
	// are automatically cleared each frame

	/** Writes the entity state to the file. */
	public void write(BinaryWriter f)
	{
		f.Write(this.surrounding_ent);
		f.Write(this.origin);
		f.Write(this.angles);
		f.Write(this.old_origin);

		f.Write(this.modelindex);

		f.Write(this.modelindex2);
		f.Write(this.modelindex3);
		f.Write(this.modelindex4);

		f.Write(this.frame);
		f.Write(this.skinnum);

		f.Write(this.effects);
		f.Write(this.renderfx);
		f.Write(this.solid);

		f.Write(this.sound);
		f.Write(this.@event);
	}

	/** Reads the entity state from the file. */
	public void read(BinaryReader f)
	{
		this.surrounding_ent = f.ReadEdictRef();
		this.origin = f.ReadVector();
		this.angles = f.ReadVector();
		this.old_origin = f.ReadVector();

		this.modelindex = f.ReadInt32();

		this.modelindex2 = f.ReadInt32();
		this.modelindex3 = f.ReadInt32();
		this.modelindex4 = f.ReadInt32();

		this.frame = f.ReadInt32();
		this.skinnum = f.ReadInt32();

		this.effects = f.ReadInt32();
		this.renderfx = f.ReadInt32();
		this.solid = f.ReadInt32();

		this.sound = f.ReadInt32();
		this.@event = f.ReadInt32();
	}

	public entity_state_t getClone()
	{
		entity_state_t @out = new(this.surrounding_ent);
		@out.set(this);

		return @out;
	}

	public void set(entity_state_t from)
	{
		this.number = from.number;
		Math3D.VectorCopy(from.origin, this.origin);
		Math3D.VectorCopy(from.angles, this.angles);
		Math3D.VectorCopy(from.old_origin, this.old_origin);

		this.modelindex = from.modelindex;
		this.modelindex2 = from.modelindex2;
		this.modelindex3 = from.modelindex3;
		this.modelindex4 = from.modelindex4;

		this.frame = from.frame;
		this.skinnum = from.skinnum;
		this.effects = from.effects;
		this.renderfx = from.renderfx;
		this.solid = from.solid;
		this.sound = from.sound;
		this.@event = from.@event;
	}

	public void clear()
	{
		//TODO: this is critical. The index has to be proper managed.
		this.number = 0;
		this.surrounding_ent = null;
		Math3D.VectorClear(this.origin);
		Math3D.VectorClear(this.angles);
		Math3D.VectorClear(this.old_origin);
		this.modelindex = 0;
		this.modelindex2 = this.modelindex3 = this.modelindex4 = 0;
		this.frame = 0;
		this.skinnum = 0;
		this.effects = 0;
		this.renderfx = 0;
		this.solid = 0;
		this.sound = 0;
		this.@event = 0;
	}
}