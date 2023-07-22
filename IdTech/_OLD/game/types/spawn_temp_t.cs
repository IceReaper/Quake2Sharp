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

using Quake2Sharp.util;

namespace Quake2Sharp.game.types;

public class spawn_temp_t
{
	// world vars
	public string sky = "";
	public float skyrotate;
	public float[] skyaxis = { 0, 0, 0 };
	public string nextmap = "";
	public int lip;
	public int distance;
	public int height;
	public string noise = "";
	public float pausetime;
	public string item = "";
	public string gravity = "";
	public float minyaw;
	public float maxyaw;
	public float minpitch;
	public float maxpitch;

	public bool set(string key, string value)
	{
		if (key.Equals("lip"))
		{
			this.lip = Lib.atoi(value);

			return true;
		} // F_INT, FFL_SPAWNTEMP),

		if (key.Equals("distance"))
		{
			this.distance = Lib.atoi(value);

			return true;
		} // F_INT, FFL_SPAWNTEMP),

		if (key.Equals("height"))
		{
			this.height = Lib.atoi(value);

			return true;
		} // F_INT, FFL_SPAWNTEMP),

		if (key.Equals("noise"))
		{
			this.noise = GameSpawn.ED_NewString(value);

			return true;
		} // F_LSTRING, FFL_SPAWNTEMP),

		if (key.Equals("pausetime"))
		{
			this.pausetime = Lib.atof(value);

			return true;
		} // F_FLOAT, FFL_SPAWNTEMP),

		if (key.Equals("item"))
		{
			this.item = GameSpawn.ED_NewString(value);

			return true;
		} // F_LSTRING, FFL_SPAWNTEMP),

		if (key.Equals("gravity"))
		{
			this.gravity = GameSpawn.ED_NewString(value);

			return true;
		} // F_LSTRING, FFL_SPAWNTEMP),

		if (key.Equals("sky"))
		{
			this.sky = GameSpawn.ED_NewString(value);

			return true;
		} // F_LSTRING, FFL_SPAWNTEMP),

		if (key.Equals("skyrotate"))
		{
			this.skyrotate = Lib.atof(value);

			return true;
		} // F_FLOAT, FFL_SPAWNTEMP),

		if (key.Equals("skyaxis"))
		{
			this.skyaxis = Lib.atov(value);

			return true;
		} // F_VECTOR, FFL_SPAWNTEMP),

		if (key.Equals("minyaw"))
		{
			this.minyaw = Lib.atof(value);

			return true;
		} // F_FLOAT, FFL_SPAWNTEMP),

		if (key.Equals("maxyaw"))
		{
			this.maxyaw = Lib.atof(value);

			return true;
		} // F_FLOAT, FFL_SPAWNTEMP),

		if (key.Equals("minpitch"))
		{
			this.minpitch = Lib.atof(value);

			return true;
		} // F_FLOAT, FFL_SPAWNTEMP),

		if (key.Equals("maxpitch"))
		{
			this.maxpitch = Lib.atof(value);

			return true;
		} // F_FLOAT, FFL_SPAWNTEMP),

		if (key.Equals("nextmap"))
		{
			this.nextmap = GameSpawn.ED_NewString(value);

			return true;
		} // F_LSTRING, FFL_SPAWNTEMP),

		return false;
	}
}