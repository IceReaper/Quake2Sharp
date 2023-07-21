using IdTech.common;

namespace Quake2Sharp.opentk;

using game;
using game.types;
using OpenTK.Audio.OpenAL;
using qcommon;
using sound;
using sound.types;

public class OpenTkSound : Sound
{
	public static readonly int MAX_SFX = Defines.MAX_SOUNDS * 2;
	public static readonly int STREAM_QUEUE = 8;
	private static ALDevice device;
	private static bool efx;
	private static int efxEffectGeneric;
	private static int efxEffectUnderwater;
	private static int efxSlot;
	private cvar_t s_volume;
	private readonly int[] buffers = new int[OpenTkSound.MAX_SFX + OpenTkSound.STREAM_QUEUE];

	public bool Init()
	{
		try
		{
			var deviceName = ALC.GetString(ALDevice.Null, AlcGetString.DefaultDeviceSpecifier);
			OpenTkSound.device = ALC.OpenDevice(deviceName);
			var context = ALC.CreateContext(OpenTkSound.device, (int[])null);
			ALC.MakeContextCurrent(context);
			this.checkError();
			this.initOpenALExtensions();
		}
		catch (Exception e)
		{
			clientserver.Com_Printf(e.Message + '\n');

			return false;
		}

		// set the master volume
		this.s_volume = cvar.Cvar_Get("s_volume", "0.7", Defines.CVAR_ARCHIVE);

		AL.GenBuffers(this.buffers.Length, this.buffers);
		var count = Channel.init(this.buffers, OpenTkSound.efxSlot);
		clientserver.Com_Printf("... using " + count + " channels\n");
		AL.DistanceModel(ALDistanceModel.InverseDistanceClamped);

		cmdparser.Cmd_AddCommand("play", () => { this.Play(); });

		cmdparser.Cmd_AddCommand("stopsound", () => { this.StopAllSounds(); });

		cmdparser.Cmd_AddCommand("soundlist", () => { this.SoundList(); });

		cmdparser.Cmd_AddCommand("soundinfo", () => { this.SoundInfo_f(); });

		OpenTkSound.num_sfx = 0;

		Com.Println("sound sampling rate: 44100Hz");

		this.StopAllSounds();
		Com.Println("------------------------------------");

		return true;
	}

	private void initOpenALExtensions()
	{
		if (ALC.EFX.IsExtensionPresent(OpenTkSound.device))
		{
			OpenTkSound.efx = true;
			OpenTkSound.efxEffectGeneric = this.createEffect(ReverbPresets.Generic);
			OpenTkSound.efxEffectUnderwater = this.createEffect(ReverbPresets.Underwater);
			ALC.EFX.GenAuxiliaryEffectSlot(out OpenTkSound.efxSlot);
			this.currentEnv = OpenTkSound.efxEffectGeneric;
			Com.Println("... using EFX");
		}
		else
		{
			Com.Println("... EFX not found");
			OpenTkSound.efx = false;
		}
	}

	private int createEffect(ReverbProperties reverbProperties)
	{
		ALC.EFX.GenEffect(out var efxEffect);
		ALC.EFX.Effect(efxEffect, EffectInteger.EffectType, (int)EffectType.EaxReverb);
		ALC.EFX.Effect(efxEffect, EffectFloat.EaxReverbDensity, reverbProperties.Density);
		ALC.EFX.Effect(efxEffect, EffectFloat.EaxReverbDiffusion, reverbProperties.Diffusion);
		ALC.EFX.Effect(efxEffect, EffectFloat.EaxReverbGain, reverbProperties.Gain);
		ALC.EFX.Effect(efxEffect, EffectFloat.EaxReverbGainHF, reverbProperties.GainHF);
		ALC.EFX.Effect(efxEffect, EffectFloat.EaxReverbGainLF, reverbProperties.GainLF);
		ALC.EFX.Effect(efxEffect, EffectFloat.EaxReverbDecayTime, reverbProperties.DecayTime);
		ALC.EFX.Effect(efxEffect, EffectFloat.EaxReverbDecayHFRatio, reverbProperties.DecayHFRatio);
		ALC.EFX.Effect(efxEffect, EffectFloat.EaxReverbDecayLFRatio, reverbProperties.DecayLFRatio);
		ALC.EFX.Effect(efxEffect, EffectFloat.EaxReverbReflectionsGain, reverbProperties.ReflectionsGain);
		ALC.EFX.Effect(efxEffect, EffectFloat.EaxReverbReflectionsDelay, reverbProperties.ReflectionsDelay);

		ALC.EFX.Effect(
			efxEffect,
			EffectVector3.EaxReverbReflectionsPan,
			new[] { reverbProperties.ReflectionsPan.X, reverbProperties.ReflectionsPan.Y, reverbProperties.ReflectionsPan.Z }
		);

		ALC.EFX.Effect(efxEffect, EffectFloat.EaxReverbLateReverbGain, reverbProperties.LateReverbGain);
		ALC.EFX.Effect(efxEffect, EffectFloat.EaxReverbLateReverbDelay, reverbProperties.LateReverbDelay);

		ALC.EFX.Effect(
			efxEffect,
			EffectVector3.EaxReverbLateReverbPan,
			new[] { reverbProperties.LateReverbPan.X, reverbProperties.LateReverbPan.Y, reverbProperties.LateReverbPan.Z }
		);

		ALC.EFX.Effect(efxEffect, EffectFloat.EaxReverbEchoTime, reverbProperties.EchoTime);
		ALC.EFX.Effect(efxEffect, EffectFloat.EaxReverbEchoDepth, reverbProperties.EchoDepth);
		ALC.EFX.Effect(efxEffect, EffectFloat.EaxReverbModulationTime, reverbProperties.ModulationTime);
		ALC.EFX.Effect(efxEffect, EffectFloat.EaxReverbModulationDepth, reverbProperties.ModulationDepth);
		ALC.EFX.Effect(efxEffect, EffectFloat.EaxReverbAirAbsorptionGainHF, reverbProperties.AirAbsorptionGainHF);
		ALC.EFX.Effect(efxEffect, EffectFloat.EaxReverbHFReference, reverbProperties.HFReference);
		ALC.EFX.Effect(efxEffect, EffectFloat.EaxReverbLFReference, reverbProperties.LFReference);
		ALC.EFX.Effect(efxEffect, EffectFloat.EaxReverbRoomRolloffFactor, reverbProperties.RoomRolloffFactor);
		ALC.EFX.Effect(efxEffect, EffectInteger.EaxReverbDecayHFLimit, reverbProperties.DecayHFLimit);

		return efxEffect;
	}

	private void initBuffer(byte[] samples, int bufferId, int freq)
	{
		AL.BufferData(this.buffers[bufferId], ALFormat.Mono16, samples, freq);
	}

	private void checkError()
	{
		clientserver.Com_DPrintf("AL Error: " + this.alErrorString() + '\n');
	}

	private string alErrorString()
	{
		ALError error;
		var message = "";

		if ((error = AL.GetError()) != ALError.NoError)
		{
			switch (error)
			{
				case ALError.InvalidOperation:
					message = "invalid operation";

					break;

				case ALError.InvalidValue:
					message = "invalid value";

					break;

				case ALError.InvalidEnum:
					message = "invalid enum";

					break;

				case ALError.InvalidName:
					message = "invalid name";

					break;

				default:
					message = "" + error;

					break;
			}
		}

		return message;
	}

	public void Shutdown()
	{
		this.StopAllSounds();
		Channel.shutdown();
		AL.DeleteBuffers(this.buffers.Length, this.buffers);

		if (OpenTkSound.efx)
		{
			ALC.EFX.DeleteEffect(OpenTkSound.efxEffectGeneric);
			ALC.EFX.DeleteEffect(OpenTkSound.efxEffectUnderwater);
			ALC.EFX.DeleteAuxiliaryEffectSlot(OpenTkSound.efxSlot);
		}

		cmdparser.Cmd_RemoveCommand("play");
		cmdparser.Cmd_RemoveCommand("stopsound");
		cmdparser.Cmd_RemoveCommand("soundlist");
		cmdparser.Cmd_RemoveCommand("soundinfo");

		// free all sounds
		for (var i = 0; i < OpenTkSound.num_sfx; i++)
		{
			if (OpenTkSound.known_sfx[i].name == null)
				continue;

			OpenTkSound.known_sfx[i].clear();
		}

		OpenTkSound.num_sfx = 0;
	}

	public void StartSound(float[] origin, int entnum, int entchannel, sfx_t sfx, float fvol, float attenuation, float timeofs)
	{
		if (sfx == null)
			return;

		if (sfx.name[0] == '*')
			sfx = this.RegisterSexedSound(Globals.cl_entities[entnum].current, sfx.name);

		if (this.LoadSound(sfx) == null)
			return; // can't load sound

		if (attenuation != Defines.ATTN_STATIC)
			attenuation *= 0.5f;

		PlaySound.allocate(origin, entnum, entchannel, this.buffers[sfx.bufferId], fvol, attenuation, timeofs);
	}

	private readonly float[] listenerOrigin = { 0, 0, 0 };
	private readonly float[] listenerOrientation = { 0, 0, 0, 0, 0, 0 };
	private int currentEnv;
	private bool changeEnv = true;

	public void Update(float[] origin, float[] forward, float[] right, float[] up)
	{
		Channel.convertVector(origin, this.listenerOrigin);
		AL.Listener(ALListener3f.Position, this.listenerOrigin[0], this.listenerOrigin[1], this.listenerOrigin[2]);

		Channel.convertOrientation(forward, up, this.listenerOrientation);
		AL.Listener(ALListenerfv.Orientation, this.listenerOrientation);

		// set the listener (master) volume
		AL.Listener(ALListenerf.Gain, this.s_volume.value);

		if (OpenTkSound.efx)
		{
			if ((GameBase.gi.pointcontents(origin) & Defines.MASK_WATER) != 0)
			{
				this.changeEnv = this.currentEnv != OpenTkSound.efxEffectUnderwater;
				this.currentEnv = OpenTkSound.efxEffectUnderwater;
			}
			else
			{
				this.changeEnv = this.currentEnv != OpenTkSound.efxEffectGeneric;
				this.currentEnv = OpenTkSound.efxEffectGeneric;
			}

			if (this.changeEnv)
			{
				this.changeEnv = false;
				ALC.EFX.AuxiliaryEffectSlot(OpenTkSound.efxSlot, EffectSlotInteger.Effect, this.currentEnv);
			}
		}

		Channel.addLoopSounds();
		Channel.addPlaySounds();
		Channel.playAllSounds(this.listenerOrigin);
	}

	public void StopAllSounds()
	{
		// mute the listener (master)
		AL.Listener(ALListenerf.Gain, 0);
		PlaySound.reset();
		Channel.reset();
	}

	public string getName()
	{
		return "OpenTK";
	}

	private int s_registration_sequence;
	private bool s_registering;

	public void BeginRegistration()
	{
		this.s_registration_sequence++;
		this.s_registering = true;
	}

	public sfx_t RegisterSound(string name)
	{
		var sfx = this.FindName(name, true);
		sfx.registration_sequence = this.s_registration_sequence;

		if (!this.s_registering)
			this.LoadSound(sfx);

		return sfx;
	}

	public void EndRegistration()
	{
		int i;
		sfx_t sfx;

		// free any sounds not from this registration sequence
		for (i = 0; i < OpenTkSound.num_sfx; i++)
		{
			sfx = OpenTkSound.known_sfx[i];

			if (sfx.name == null)
				continue;

			if (sfx.registration_sequence != this.s_registration_sequence)
			{
				// don't need this sound
				sfx.clear();
			}
		}

		// load everything in
		for (i = 0; i < OpenTkSound.num_sfx; i++)
		{
			sfx = OpenTkSound.known_sfx[i];

			if (sfx.name == null)
				continue;

			this.LoadSound(sfx);
		}

		this.s_registering = false;
	}

	private sfx_t RegisterSexedSound(entity_state_t ent, string @base)
	{
		sfx_t sfx = null;

		// determine what model the client is using
		string model = null;
		var n = Defines.CS_PLAYERSKINS + ent.number - 1;

		if (Globals.cl.configstrings[n] != null)
		{
			var p = Globals.cl.configstrings[n].IndexOf('\\');

			if (p >= 0)
			{
				p++;
				model = Globals.cl.configstrings[n][p..];

				//strcpy(model, p);
				p = model.IndexOf('/');

				if (p > 0)
					model = model[..p];
			}
		}

		// if we can't figure it out, they're male
		if (model == null || model.Length == 0)
			model = "male";

		// see if we already know of the model specific sound
		var sexedFilename = "#players/" + model + "/" + @base[1..];

		//Com_sprintf (sexedFilename, sizeof(sexedFilename), "#players/%s/%s", model, base+1);
		sfx = this.FindName(sexedFilename, false);

		if (sfx != null)
			return sfx;

		//
		// fall back strategies
		//
		// not found , so see if it exists
		if (filesystem.FS_FileLength(sexedFilename[1..]) > 0)
		{
			// yes, register it
			return this.RegisterSound(sexedFilename);
		}

		// try it with the female sound in the pak0.pak
		if (model.Equals("female", StringComparison.OrdinalIgnoreCase))
		{
			var femaleFilename = "player/female/" + @base[1..];

			if (filesystem.FS_FileLength("sound/" + femaleFilename) > 0)
				return this.AliasName(sexedFilename, femaleFilename);
		}

		// no chance, revert to the male sound in the pak0.pak
		var maleFilename = "player/male/" + @base[1..];

		return this.AliasName(sexedFilename, maleFilename);
	}

	private static readonly sfx_t[] known_sfx = new sfx_t[OpenTkSound.MAX_SFX];
	private static int num_sfx;

	private sfx_t FindName(string name, bool create)
	{
		int i;
		sfx_t sfx = null;

		if (name == null)
			clientserver.Com_Error(Defines.ERR_FATAL, "S_FindName: NULL\n");

		if (name.Length == 0)
			clientserver.Com_Error(Defines.ERR_FATAL, "S_FindName: empty name\n");

		if (name.Length >= Defines.MAX_QPATH)
			clientserver.Com_Error(Defines.ERR_FATAL, "Sound name too long: " + name);

		// see if already loaded
		for (i = 0; i < OpenTkSound.num_sfx; i++)
		{
			if (name.Equals(OpenTkSound.known_sfx[i].name))
				return OpenTkSound.known_sfx[i];
		}

		if (!create)
			return null;

		// find a free sfx
		for (i = 0; i < OpenTkSound.num_sfx; i++)
		{
			if (OpenTkSound.known_sfx[i].name == null)

				// registration_sequence < s_registration_sequence)
				break;
		}

		if (i == OpenTkSound.num_sfx)
		{
			if (OpenTkSound.num_sfx == OpenTkSound.MAX_SFX)
				clientserver.Com_Error(Defines.ERR_FATAL, "S_FindName: out of sfx_t");

			OpenTkSound.num_sfx++;
		}

		sfx = OpenTkSound.known_sfx[i];
		sfx.clear();
		sfx.name = name;
		sfx.registration_sequence = this.s_registration_sequence;
		sfx.bufferId = i;

		return sfx;
	}

	/*
	==================
	S_AliasName

	==================
	*/
	private sfx_t AliasName(string aliasname, string truename)
	{
		sfx_t sfx = null;
		string s;
		int i;

		s = new(truename);

		// find a free sfx
		for (i = 0; i < OpenTkSound.num_sfx; i++)
		{
			if (OpenTkSound.known_sfx[i].name == null)
				break;
		}

		if (i == OpenTkSound.num_sfx)
		{
			if (OpenTkSound.num_sfx == OpenTkSound.MAX_SFX)
				clientserver.Com_Error(Defines.ERR_FATAL, "S_FindName: out of sfx_t");

			OpenTkSound.num_sfx++;
		}

		sfx = OpenTkSound.known_sfx[i];
		sfx.clear();
		sfx.name = new(aliasname);
		sfx.registration_sequence = this.s_registration_sequence;
		sfx.truename = s;

		// set the AL bufferId
		sfx.bufferId = i;

		return sfx;
	}

	/*
	==============
	S_LoadSound
	==============
	*/
	public sfxcache_t LoadSound(sfx_t s)
	{
		if (s.isCached)
			return s.cache;

		var sc = WaveLoader.LoadSound(s);

		if (sc != null)
		{
			this.initBuffer(sc.data, s.bufferId, sc.speed);
			s.isCached = true;

			// free samples for GC
			s.cache.data = null;
		}

		return sc;
	}

	public void StartLocalSound(string sound)
	{
		var sfx = this.RegisterSound(sound);

		if (sfx == null)
		{
			clientserver.Com_Printf("S_StartLocalSound: can't cache " + sound + "\n");

			return;
		}

		this.StartSound(null, Globals.cl.playernum + 1, 0, sfx, 1, 1, 0.0f);
	}

	public void RawSamples(int samples, int rate, int width, int channels, BinaryReader data)
	{
		var format = channels == 2 ? ALFormat.Stereo16 : ALFormat.Mono16;

		var pcm = new byte[samples * channels * 2];

		// convert to signed 16 bit samples
		if (width == 1)
		{
			var writer = new BinaryWriter(new MemoryStream(pcm));

			for (var i = 0; i < samples * channels; i++)
				writer.Write((short)(data.Read() - 128));
		}
		else
			data.Read(pcm);

		Channel.updateStream(pcm, format, rate);
	}

	public void disableStreaming()
	{
		Channel.disableStreaming();
	}

	/*
	===============================================================================

	console functions

	===============================================================================
	*/

	private void Play()
	{
		var i = 1;
		string name;

		while (i < cmdparser.Cmd_Argc())
		{
			name = new(cmdparser.Cmd_Argv(i));

			if (name.IndexOf('.') == -1)
				name += ".wav";

			this.RegisterSound(name);
			this.StartLocalSound(name);
			i++;
		}
	}

	private void SoundList()
	{
		int i;
		sfx_t sfx;
		sfxcache_t sc;
		int size, total;

		total = 0;

		for (i = 0; i < OpenTkSound.num_sfx; i++)
		{
			sfx = OpenTkSound.known_sfx[i];

			if (sfx.registration_sequence == 0)
				continue;

			sc = sfx.cache;

			if (sc != null)
			{
				size = sc.length * sc.width * (sc.stereo + 1);
				total += size;

				if (sc.loopstart >= 0)
					clientserver.Com_Printf("L");
				else
					clientserver.Com_Printf(" ");

				clientserver.Com_Printf($"({sc.width * 8,2}b) {size,6} : {sfx.name}\n");
			}
			else
			{
				if (sfx.name[0] == '*')
					clientserver.Com_Printf("  placeholder : " + sfx.name + "\n");
				else
					clientserver.Com_Printf("  not loaded  : " + sfx.name + "\n");
			}
		}

		clientserver.Com_Printf("Total resident: " + total + "\n");
	}

	private void SoundInfo_f()
	{
		clientserver.Com_Printf($"{1,5} stereo\n");
		clientserver.Com_Printf($"{22050,5} samples\n");
		clientserver.Com_Printf($"{16,5} samplebits\n");
		clientserver.Com_Printf($"{44100,5} speed\n");
	}

	static OpenTkSound()
	{
		for (var i = 0; i < OpenTkSound.known_sfx.Length; i++)
			OpenTkSound.known_sfx[i] = new();
	}
}
