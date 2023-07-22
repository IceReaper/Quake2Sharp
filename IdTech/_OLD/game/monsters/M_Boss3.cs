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

using Quake2Sharp.game.adapters;
using Quake2Sharp.game.types;
using Quake2Sharp.util;

namespace Quake2Sharp.game.monsters;

public class M_Boss3
{
	private static readonly EntUseAdapter Use_Boss3 = new("Use_Boss3", (ent, other, activator) =>
	{
		GameBase.gi.WriteByte(Defines.svc_temp_entity);
		GameBase.gi.WriteByte(Defines.TE_BOSSTPORT);
		GameBase.gi.WritePosition(ent.s.origin);
		GameBase.gi.multicast(ent.s.origin, Defines.MULTICAST_PVS);
		GameUtil.G_FreeEdict(ent);
	});

	private static readonly EntThinkAdapter Think_Boss3Stand = new("Think_Boss3Stand", ent =>
	{
		if (ent.s.frame == M_Boss32.FRAME_stand260)
			ent.s.frame = M_Boss32.FRAME_stand201;
		else
			ent.s.frame++;

		ent.nextthink = GameBase.level.time + Defines.FRAMETIME;

		return true;
	});

	/*
	 * QUAKED monster_boss3_stand (1 .5 0) (-32 -32 0) (32 32 90)
	 *
	 * Just stands and cycles in one place until targeted, then teleports away.
	 */
	public static void SP_monster_boss3_stand(edict_t self)
	{
		if (GameBase.deathmatch.value != 0)
		{
			GameUtil.G_FreeEdict(self);

			return;
		}

		self.movetype = Defines.MOVETYPE_STEP;
		self.solid = Defines.SOLID_BBOX;
		self.model = "models/monsters/boss3/rider/tris.md2";
		self.s.modelindex = GameBase.gi.modelindex(self.model);
		self.s.frame = M_Boss32.FRAME_stand201;

		GameBase.gi.soundindex("misc/bigtele.wav");

		Math3D.VectorSet(self.mins, -32, -32, 0);
		Math3D.VectorSet(self.maxs, 32, 32, 90);

		self.use = M_Boss3.Use_Boss3;
		self.think = M_Boss3.Think_Boss3Stand;
		self.nextthink = GameBase.level.time + Defines.FRAMETIME;
		GameBase.gi.linkentity(self);
	}
}
