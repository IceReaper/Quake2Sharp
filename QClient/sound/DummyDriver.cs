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
namespace QClient.sound;

using types;

public class DummyDriver : Sound
{
	static DummyDriver()
	{
		S.register(new DummyDriver());
	}

	private DummyDriver()
	{
	}

	public bool Init()
	{
		return true;
	}

	public void Shutdown()
	{
	}

	public void BeginRegistration()
	{
	}

	public sfx_t RegisterSound(string sample)
	{
		return null;
	}

	public void EndRegistration()
	{
	}

	public void StartLocalSound(string sound)
	{
	}

	public void StartSound(float[] origin, int entnum, int entchannel, sfx_t sfx, float fvol, float attenuation, float timeofs)
	{
	}

	public void Update(float[] origin, float[] forward, float[] right, float[] up)
	{
	}

	public void RawSamples(int samples, int rate, int width, int channels, BinaryReader data)
	{
	}

	public void disableStreaming()
	{
	}

	public void StopAllSounds()
	{
	}

	public string getName()
	{
		return "dummy";
	}
}