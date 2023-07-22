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

using Quake2Sharp.game.types;
using Quake2Sharp.util;

namespace Quake2Sharp.game;

public class PlayerTrail
{
	/*
	 * ==============================================================================
	 *
	 * PLAYER TRAIL
	 *
	 * ==============================================================================
	 *
	 * This is a circular list containing the a list of points of where the
	 * player has been recently. It is used by monsters for pursuit.
	 *
	 * .origin the spot .owner forward link .aiment backward link
	 */
	private static readonly int TRAIL_LENGTH = 8;
	private static readonly edict_t[] trail = new edict_t[PlayerTrail.TRAIL_LENGTH];
	private static int trail_head;
	private static bool trail_active;

	static PlayerTrail()
	{
		//TODO: potential error
		for (var n = 0; n < PlayerTrail.TRAIL_LENGTH; n++)
			PlayerTrail.trail[n] = new(n);
	}

	private static int NEXT(int n)
	{
		return (n + 1) % PlayerTrail.TRAIL_LENGTH;
	}

	private static int PREV(int n)
	{
		return (n + PlayerTrail.TRAIL_LENGTH - 1) % PlayerTrail.TRAIL_LENGTH;
	}

	public static void Init()
	{
		// FIXME || coop
		if (GameBase.deathmatch.value != 0)
			return;

		for (var n = 0; n < PlayerTrail.TRAIL_LENGTH; n++)
		{
			PlayerTrail.trail[n] = GameUtil.G_Spawn();
			PlayerTrail.trail[n].classname = "player_trail";
		}

		PlayerTrail.trail_head = 0;
		PlayerTrail.trail_active = true;
	}

	public static void Add(float[] spot)
	{
		float[] temp = { 0, 0, 0 };

		if (!PlayerTrail.trail_active)
			return;

		Math3D.VectorCopy(spot, PlayerTrail.trail[PlayerTrail.trail_head].s.origin);

		PlayerTrail.trail[PlayerTrail.trail_head].timestamp = GameBase.level.time;

		Math3D.VectorSubtract(spot, PlayerTrail.trail[PlayerTrail.PREV(PlayerTrail.trail_head)].s.origin, temp);
		PlayerTrail.trail[PlayerTrail.trail_head].s.angles[1] = Math3D.vectoyaw(temp);

		PlayerTrail.trail_head = PlayerTrail.NEXT(PlayerTrail.trail_head);
	}

	private static void New(float[] spot)
	{
		if (!PlayerTrail.trail_active)
			return;

		PlayerTrail.Init();
		PlayerTrail.Add(spot);
	}

	public static edict_t PickFirst(edict_t self)
	{
		if (!PlayerTrail.trail_active)
			return null;

		var marker = PlayerTrail.trail_head;

		for (var n = PlayerTrail.TRAIL_LENGTH; n > 0; n--)
		{
			if (PlayerTrail.trail[marker].timestamp <= self.monsterinfo.trail_time)
				marker = PlayerTrail.NEXT(marker);
			else
				break;
		}

		if (GameUtil.visible(self, PlayerTrail.trail[marker]))
			return PlayerTrail.trail[marker];

		if (GameUtil.visible(self, PlayerTrail.trail[PlayerTrail.PREV(marker)]))
			return PlayerTrail.trail[PlayerTrail.PREV(marker)];

		return PlayerTrail.trail[marker];
	}

	public static edict_t PickNext(edict_t self)
	{
		int marker;
		int n;

		if (!PlayerTrail.trail_active)
			return null;

		for (marker = PlayerTrail.trail_head, n = PlayerTrail.TRAIL_LENGTH; n > 0; n--)
		{
			if (PlayerTrail.trail[marker].timestamp <= self.monsterinfo.trail_time)
				marker = PlayerTrail.NEXT(marker);
			else
				break;
		}

		return PlayerTrail.trail[marker];
	}

	public static edict_t LastSpot()
	{
		return PlayerTrail.trail[PlayerTrail.PREV(PlayerTrail.trail_head)];
	}
}
