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

public class clientinfo_t
{
	public string name = "";
	public string cinfo = "";
	public image_t skin; // ptr
	public image_t icon; // ptr
	public string iconname = "";
	public model_t model; // ptr
	public model_t[] weaponmodel = new model_t[Defines.MAX_CLIENTWEAPONMODELS]; // arary of references

	//	public void reset()
	//	{
	//		set(new clientinfo_t());
	//	}

	public void set(clientinfo_t from)
	{
		this.name = from.name;
		this.cinfo = from.cinfo;
		this.skin = from.skin;
		this.icon = from.icon;
		this.iconname = from.iconname;
		this.model = from.model;
		Array.Copy(from.weaponmodel, 0, this.weaponmodel, 0, Defines.MAX_CLIENTWEAPONMODELS);
	}
}