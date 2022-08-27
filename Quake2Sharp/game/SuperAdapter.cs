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
namespace Quake2Sharp.game;

using qcommon;

public abstract class SuperAdapter
{
	/** Constructor, does the adapter registration. */
	public SuperAdapter(string id)
	{
		this.getID = () => id;
		SuperAdapter.register(this, id);
	}

	/** Adapter registration. */
	private static void register(SuperAdapter sa, string id)
	{
		if (SuperAdapter.adapters.ContainsKey(id))
			SuperAdapter.adapters[id] = sa;
		else
			SuperAdapter.adapters.Add(id, sa);
	}

	/** Adapter repository. */
	private static readonly Dictionary<string, SuperAdapter> adapters = new();

	/** Returns the adapter from the repository given by its ID. */
	public static SuperAdapter getFromID(string key)
	{
		var sa = (SuperAdapter)SuperAdapter.adapters[key];

		// try to create the adapter
		if (sa == null)
			Com.DPrintf("SuperAdapter.getFromID():adapter not found->" + key + "\n");

		return sa;
	}

	/** Returns the Adapter-ID. */
	public Func<string> getID;
}