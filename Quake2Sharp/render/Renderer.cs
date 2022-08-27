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
namespace Quake2Sharp.render;

using client.types;
using opengl;
using opentk;
using System.Runtime.CompilerServices;

public class Renderer
{
	private static readonly RenderAPI renderApi = new OpenGLRenderApi();
	private static readonly List<Ref> drivers = new();

	static Renderer()
	{
		try
		{
			try
			{
				RuntimeHelpers.RunClassConstructor(typeof(OpenTkRenderer).TypeHandle);
			}
			catch (Exception)
			{
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}
	}

	public static void register(Ref impl)
	{
		if (impl == null)
			throw new("Ref implementation can't be null");

		if (!Renderer.drivers.Contains(impl))
			Renderer.drivers.Add(impl);
	}

	public static refexport_t getDriver(string driverName)
	{
		Ref driver = null;
		var count = Renderer.drivers.Count;

		for (var i = 0; i < count; i++)
		{
			driver = Renderer.drivers[i];

			if (driver.getName().Equals(driverName))
				return driver.GetRefAPI(Renderer.renderApi);
		}

		return null;
	}

	public static string getDefaultName()
	{
		return Renderer.drivers.Count == 0 ? null : Renderer.drivers[0].getName();
	}

	public static string getPreferedName()
	{
		return Renderer.drivers.Count == 0 ? null : Renderer.drivers[0].getName();
	}

	public static string[] getDriverNames()
	{
		if (Renderer.drivers.Count == 0)
			return null;

		var count = Renderer.drivers.Count;
		var names = new string[count];

		for (var i = 0; i < count; i++)
			names[i] = Renderer.drivers[i].getName();

		return names;
	}
}