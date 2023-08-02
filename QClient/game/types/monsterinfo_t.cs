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

using adapters;
using util;

public class monsterinfo_t
{
	public mmove_t currentmove;
	public int aiflags;
	public int nextframe;
	public float scale;
	public EntThinkAdapter stand;
	public EntThinkAdapter idle;
	public EntThinkAdapter search;
	public EntThinkAdapter walk;
	public EntThinkAdapter run;
	public EntDodgeAdapter dodge;
	public EntThinkAdapter attack;
	public EntThinkAdapter melee;
	public EntInteractAdapter sight;
	public EntThinkAdapter checkattack;
	public float pausetime;
	public float attack_finished;
	public float[] saved_goal = { 0, 0, 0 };
	public float search_time;
	public float trail_time;
	public float[] last_sighting = { 0, 0, 0 };
	public int attack_state;
	public int lefty;
	public float idle_time;
	public int linkcount;
	public int power_armor_type;
	public int power_armor_power;

	/** Writes the monsterinfo to the file.*/
	public void write(BinaryWriter f)
	{
		f.Write(this.currentmove != null);

		if (this.currentmove != null)
			this.currentmove.write(f);

		f.Write(this.aiflags);
		f.Write(this.nextframe);
		f.Write(this.scale);
		f.Write(this.stand);
		f.Write(this.idle);
		f.Write(this.search);
		f.Write(this.walk);
		f.Write(this.run);

		f.Write(this.dodge);

		f.Write(this.attack);
		f.Write(this.melee);

		f.Write(this.sight);

		f.Write(this.checkattack);

		f.Write(this.pausetime);
		f.Write(this.attack_finished);

		f.Write(this.saved_goal);

		f.Write(this.search_time);
		f.Write(this.trail_time);

		f.Write(this.last_sighting);

		f.Write(this.attack_state);
		f.Write(this.lefty);

		f.Write(this.idle_time);
		f.Write(this.linkcount);

		f.Write(this.power_armor_power);
		f.Write(this.power_armor_type);
	}

	/** Writes the monsterinfo to the file.*/
	public void read(BinaryReader f)
	{
		if (f.ReadBoolean())
		{
			this.currentmove = new();
			this.currentmove.read(f);
		}
		else
			this.currentmove = null;

		this.aiflags = f.ReadInt32();
		this.nextframe = f.ReadInt32();
		this.scale = f.ReadSingle();
		this.stand = (EntThinkAdapter)f.ReadAdapter();
		this.idle = (EntThinkAdapter)f.ReadAdapter();
		this.search = (EntThinkAdapter)f.ReadAdapter();
		this.walk = (EntThinkAdapter)f.ReadAdapter();
		this.run = (EntThinkAdapter)f.ReadAdapter();

		this.dodge = (EntDodgeAdapter)f.ReadAdapter();

		this.attack = (EntThinkAdapter)f.ReadAdapter();
		this.melee = (EntThinkAdapter)f.ReadAdapter();

		this.sight = (EntInteractAdapter)f.ReadAdapter();

		this.checkattack = (EntThinkAdapter)f.ReadAdapter();

		this.pausetime = f.ReadSingle();
		this.attack_finished = f.ReadSingle();

		this.saved_goal = f.ReadVector();

		this.search_time = f.ReadSingle();
		this.trail_time = f.ReadSingle();

		this.last_sighting = f.ReadVector();

		this.attack_state = f.ReadInt32();
		this.lefty = f.ReadInt32();

		this.idle_time = f.ReadSingle();
		this.linkcount = f.ReadInt32();

		this.power_armor_power = f.ReadInt32();
		this.power_armor_type = f.ReadInt32();
	}
}