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

namespace Quake2Sharp.sound;

using game.types;
using opentk;
using qcommon;
using System.Numerics;
using System.Runtime.CompilerServices;
using types;

/**
 * S
 */
public class S
{
	private static Sound impl;
	private static cvar_t s_impl;
	private static readonly List<Sound> drivers = new();

	/** 
	 * Searches for and initializes all known sound drivers.
	 */
	static S()
	{
		// dummy driver (no sound)
		try
		{
			RuntimeHelpers.RunClassConstructor(typeof(DummyDriver).TypeHandle);

			// initialize impl with the default value
			// this is  necessary for dedicated mode
			S.useDriver("dummy");
		}
		catch (Exception)
		{
			Com.DPrintf("could not init dummy sound driver class.");
		}

		// prefered driver
		try
		{
			RuntimeHelpers.RunClassConstructor(typeof(OpenTkSound).TypeHandle);
		}
		catch (Exception)
		{
			// ignore the OpenTK driver if runtime not in classpath
			Com.DPrintf("could not init OpenTK sound driver class.");
		}
	}

	/**
	 * Registers a new Sound Implementor.
	 */
	public static void register(Sound driver)
	{
		if (driver == null)
			throw new("Sound implementation can't be null");

		if (!S.drivers.Contains(driver))
			S.drivers.Add(driver);
	}

	/**
	 * Switches to the specific sound driver.
	 */
	public static void useDriver(string driverName)
	{
		Sound driver = null;
		var count = S.drivers.Count;

		for (var i = 0; i < count; i++)
		{
			driver = (Sound)S.drivers[i];

			if (driver.getName().Equals(driverName))
			{
				S.impl = driver;

				return;
			}
		}

		// if driver not found use dummy
		S.impl = (Sound)S.drivers[^1];
	}

	/**
	 * Initializes the sound module.
	 */
	public static void Init()
	{
		Com.Printf("\n------- sound initialization -------\n");

		var cv = Cvar.Get("s_initsound", "1", 0);

		if (cv.value == 0.0f)
		{
			Com.Printf("not initializing.\n");
			S.useDriver("dummy");

			return;
		}

		// set the last registered driver as default
		var defaultDriver = "dummy";

		if (S.drivers.Count > 1)
			defaultDriver = ((Sound)S.drivers[^1]).getName();

		S.s_impl = Cvar.Get("s_impl", defaultDriver, Defines.CVAR_ARCHIVE);
		S.useDriver(S.s_impl.@string);

		if (S.impl.Init())
		{
			// driver ok
			Cvar.Set("s_impl", S.impl.getName());
		}
		else
		{
			// fallback
			S.useDriver("dummy");
		}

		Com.Printf("\n------- use sound driver \"" + S.impl.getName() + "\" -------\n");
		S.StopAllSounds();
	}

	public static void Shutdown()
	{
		S.impl.Shutdown();
	}

	/**
	 * Called before the sounds are to be loaded and registered.
	 */
	public static void BeginRegistration()
	{
		S.impl.BeginRegistration();
	}

	/**
	 * Registers and loads a sound.
	 */
	public static sfx_t RegisterSound(string sample)
	{
		return S.impl.RegisterSound(sample);
	}

	/**
	 * Called after all sounds are registered and loaded.
	 */
	public static void EndRegistration()
	{
		S.impl.EndRegistration();
	}

	/**
	 * Starts a local sound.
	 */
	public static void StartLocalSound(string sound)
	{
		S.impl.StartLocalSound(sound);
	}

	/** 
	 * StartSound - Validates the parms and ques the sound up
	 * if pos is NULL, the sound will be dynamically sourced from the entity
	 * Entchannel 0 will never override a playing sound
	 */
	public static void StartSound(Vector3? origin, int entnum, int entchannel, sfx_t sfx, float fvol, float attenuation, float timeofs)
	{
		S.StartSound(origin == null ? null : new[] { origin.Value.X, origin.Value.Y, origin.Value.Z}, entnum, entchannel, sfx, fvol, attenuation, timeofs);
	}

	public static void StartSound(float[]? origin, int entnum, int entchannel, sfx_t sfx, float fvol, float attenuation, float timeofs)
	{
		S.impl.StartSound(origin, entnum, entchannel, sfx, fvol, attenuation, timeofs);
	}

	/**
	 * Updates the sound renderer according to the changes in the environment,
	 * called once each time through the main loop.
	 */
	public static void Update(float[] origin, float[] forward, float[] right, float[] up)
	{
		S.impl.Update(origin, forward, right, up);
	}

	/**
	 * Cinematic streaming and voice over network.
	 */
	public static void RawSamples(int samples, int rate, int width, int channels, BinaryReader data)
	{
		S.impl.RawSamples(samples, rate, width, channels, data);
	}

	/**
	 * Switches off the sound streaming.
	 */
	public static void disableStreaming()
	{
		S.impl.disableStreaming();
	}

	/**
	 * Stops all sounds. 
	 */
	public static void StopAllSounds()
	{
		S.impl.StopAllSounds();
	}

	public static string getDriverName()
	{
		return S.impl.getName();
	}

	/**
	 * Returns a string array containing all sound driver names.
	 */
	public static string[] getDriverNames()
	{
		var names = new string[S.drivers.Count];

		for (var i = 0; i < names.Length; i++)
		{
			if (S.drivers[i] is not DummyDriver)
				names[i] = ((Sound)S.drivers[i]).getName();
		}

		return names;
	}

	/**
	 * This is used, when resampling to this default sampling rate is activated 
	 * in the wavloader. It is placed here that sound implementors can override 
	 * this one day.
	 */
	public static int getDefaultSampleRate()
	{
		return 44100;
	}
}
