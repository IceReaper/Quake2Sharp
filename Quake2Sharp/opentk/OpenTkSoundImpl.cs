namespace Quake2Sharp.opentk
{
	using Quake2Sharp;
	using game;
	using qcommon;
	using sound;
	using util;
	using OpenTK.Audio.OpenAL;
	using OpenTK.Audio.OpenAL.Extensions.Creative.EFX;
	using System;
	using System.IO;

	/**
 * JOALSoundImpl
 */
	public sealed class OpenTkSoundImpl : Sound
	{
		private static void OpenTkSoundImpl1()
		{
			S.register(new OpenTkSoundImpl());
		}

		private static ALDevice device;
		private static bool efx;
		private static int efxEffectGeneric;
		private static int efxEffectUnderwater;
		private static int efxSlot;

		private cvar_t s_volume;

		private readonly int[] buffers = new int[Sound.MAX_SFX + Sound.STREAM_QUEUE];

		public bool Init()
		{
			try
			{
				var deviceName = ALC.GetString(ALDevice.Null, AlcGetString.DefaultDeviceSpecifier);
				OpenTkSoundImpl.device = ALC.OpenDevice(deviceName);
				var context = ALC.CreateContext(OpenTkSoundImpl.device, (int[]) null);
				ALC.MakeContextCurrent(context);
				this.checkError();
				this.initOpenALExtensions();
			}
			catch (Exception e)
			{
				Com.Printf(e.Message + '\n');

				return false;
			}

			// set the master volume
			this.s_volume = Cvar.Get("s_volume", "0.7", Defines.CVAR_ARCHIVE);

			AL.GenBuffers(this.buffers.Length, this.buffers);
			var count = Channel.init(this.buffers, OpenTkSoundImpl.efxSlot);
			Com.Printf("... using " + count + " channels\n");
			AL.DistanceModel(ALDistanceModel.InverseDistanceClamped);

			Cmd.AddCommand(
				"play",
				() =>
				{
					this.Play();
				}
			);

			Cmd.AddCommand(
				"stopsound",
				() =>
				{
					this.StopAllSounds();
				}
			);

			Cmd.AddCommand(
				"soundlist",
				() =>
				{
					this.SoundList();
				}
			);

			Cmd.AddCommand(
				"soundinfo",
				() =>
				{
					this.SoundInfo_f();
				}
			);

			OpenTkSoundImpl.num_sfx = 0;

			Com.Println("sound sampling rate: 44100Hz");

			this.StopAllSounds();
			Com.Println("------------------------------------");

			return true;
		}

		private void initOpenALExtensions()
		{
			if (EFX.IsExtensionPresent(OpenTkSoundImpl.device))
			{
				OpenTkSoundImpl.efx = true;
				OpenTkSoundImpl.efxEffectGeneric = this.createEffect(ReverbPresets.Generic);
				OpenTkSoundImpl.efxEffectUnderwater = this.createEffect(ReverbPresets.Underwater);
				EFX.GenAuxiliaryEffectSlot(out OpenTkSoundImpl.efxSlot);
				this.currentEnv = OpenTkSoundImpl.efxEffectGeneric;
				Com.Println("... using EFX");
			}
			else
			{
				Com.Println("... EFX not found");
				OpenTkSoundImpl.efx = false;
			}
		}

		private int createEffect(ReverbProperties reverbProperties)
		{
			EFX.GenEffect(out var efxEffect);
			EFX.Effect(efxEffect, EffectInteger.EffectType, (int) EffectType.EaxReverb);
			EFX.Effect(efxEffect, EffectFloat.EaxReverbDensity, reverbProperties.Density);
			EFX.Effect(efxEffect, EffectFloat.EaxReverbDiffusion, reverbProperties.Diffusion);
			EFX.Effect(efxEffect, EffectFloat.EaxReverbGain, reverbProperties.Gain);
			EFX.Effect(efxEffect, EffectFloat.EaxReverbGainHF, reverbProperties.GainHF);
			EFX.Effect(efxEffect, EffectFloat.EaxReverbGainLF, reverbProperties.GainLF);
			EFX.Effect(efxEffect, EffectFloat.EaxReverbDecayTime, reverbProperties.DecayTime);
			EFX.Effect(efxEffect, EffectFloat.EaxReverbDecayHFRatio, reverbProperties.DecayHFRatio);
			EFX.Effect(efxEffect, EffectFloat.EaxReverbDecayLFRatio, reverbProperties.DecayLFRatio);
			EFX.Effect(efxEffect, EffectFloat.EaxReverbReflectionsGain, reverbProperties.ReflectionsGain);
			EFX.Effect(efxEffect, EffectFloat.EaxReverbReflectionsDelay, reverbProperties.ReflectionsDelay);

			EFX.Effect(
				efxEffect,
				EffectVector3.EaxReverbReflectionsPan,
				new[] {reverbProperties.ReflectionsPan.X, reverbProperties.ReflectionsPan.Y, reverbProperties.ReflectionsPan.Z}
			);

			EFX.Effect(efxEffect, EffectFloat.EaxReverbLateReverbGain, reverbProperties.LateReverbGain);
			EFX.Effect(efxEffect, EffectFloat.EaxReverbLateReverbDelay, reverbProperties.LateReverbDelay);

			EFX.Effect(
				efxEffect,
				EffectVector3.EaxReverbLateReverbPan,
				new[] {reverbProperties.LateReverbPan.X, reverbProperties.LateReverbPan.Y, reverbProperties.LateReverbPan.Z}
			);

			EFX.Effect(efxEffect, EffectFloat.EaxReverbEchoTime, reverbProperties.EchoTime);
			EFX.Effect(efxEffect, EffectFloat.EaxReverbEchoDepth, reverbProperties.EchoDepth);
			EFX.Effect(efxEffect, EffectFloat.EaxReverbModulationTime, reverbProperties.ModulationTime);
			EFX.Effect(efxEffect, EffectFloat.EaxReverbModulationDepth, reverbProperties.ModulationDepth);
			EFX.Effect(efxEffect, EffectFloat.EaxReverbAirAbsorptionGainHF, reverbProperties.AirAbsorptionGainHF);
			EFX.Effect(efxEffect, EffectFloat.EaxReverbHFReference, reverbProperties.HFReference);
			EFX.Effect(efxEffect, EffectFloat.EaxReverbLFReference, reverbProperties.LFReference);
			EFX.Effect(efxEffect, EffectFloat.EaxReverbRoomRolloffFactor, reverbProperties.RoomRolloffFactor);
			EFX.Effect(efxEffect, EffectInteger.EaxReverbDecayHFLimit, reverbProperties.DecayHFLimit);

			return efxEffect;
		}

		private void initBuffer(byte[] samples, int bufferId, int freq)
		{
			AL.BufferData(this.buffers[bufferId], ALFormat.Mono16, samples, freq);
		}

		private void checkError()
		{
			Com.DPrintf("AL Error: " + this.alErrorString() + '\n');
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

			if (OpenTkSoundImpl.efx)
			{
				EFX.DeleteEffect(OpenTkSoundImpl.efxEffectGeneric);
				EFX.DeleteEffect(OpenTkSoundImpl.efxEffectUnderwater);
				EFX.DeleteAuxiliaryEffectSlot(OpenTkSoundImpl.efxSlot);
			}

			Cmd.RemoveCommand("play");
			Cmd.RemoveCommand("stopsound");
			Cmd.RemoveCommand("soundlist");
			Cmd.RemoveCommand("soundinfo");

			// free all sounds
			for (var i = 0; i < OpenTkSoundImpl.num_sfx; i++)
			{
				if (OpenTkSoundImpl.known_sfx[i].name == null)
					continue;

				OpenTkSoundImpl.known_sfx[i].clear();
			}

			OpenTkSoundImpl.num_sfx = 0;
		}

		public void StartSound(float[] origin, int entnum, int entchannel, sfx_t sfx, float fvol, float attenuation, float timeofs)
		{
			if (sfx == null)
				return;

			if (sfx.name[0] == '*')
				sfx = this.RegisterSexedSound(Globals.cl_entities[entnum].Current, sfx.name);

			if (this.LoadSound(sfx) == null)
				return; // can't load sound

			if (attenuation != Defines.ATTN_STATIC)
				attenuation *= 0.5f;

			PlaySound.allocate(origin, entnum, entchannel, this.buffers[sfx.bufferId], fvol, attenuation, timeofs);
		}

		private readonly float[] listenerOrigin = {0, 0, 0};
		private readonly float[] listenerOrientation = {0, 0, 0, 0, 0, 0};
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

			if (OpenTkSoundImpl.efx)
			{
				if ((GameBase.gi.pointcontents(origin) & Defines.MASK_WATER) != 0)
				{
					this.changeEnv = this.currentEnv != OpenTkSoundImpl.efxEffectUnderwater;
					this.currentEnv = OpenTkSoundImpl.efxEffectUnderwater;
				}
				else
				{
					this.changeEnv = this.currentEnv != OpenTkSoundImpl.efxEffectGeneric;
					this.currentEnv = OpenTkSoundImpl.efxEffectGeneric;
				}

				if (this.changeEnv)
				{
					this.changeEnv = false;
					EFX.AuxiliaryEffectSlot(OpenTkSoundImpl.efxSlot, EffectSlotInteger.Effect, this.currentEnv);
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
			for (i = 0; i < OpenTkSoundImpl.num_sfx; i++)
			{
				sfx = OpenTkSoundImpl.known_sfx[i];

				if (sfx.name == null)
					continue;

				if (sfx.registration_sequence != this.s_registration_sequence)
				{
					// don't need this sound
					sfx.clear();
				}
			}

			// load everything in
			for (i = 0; i < OpenTkSoundImpl.num_sfx; i++)
			{
				sfx = OpenTkSoundImpl.known_sfx[i];

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
					model = Globals.cl.configstrings[n].Substring(p);

					//strcpy(model, p);
					p = model.IndexOf('/');

					if (p > 0)
						model = model.Substring(0, p);
				}
			}

			// if we can't figure it out, they're male
			if (model == null || model.Length == 0)
				model = "male";

			// see if we already know of the model specific sound
			var sexedFilename = "#players/" + model + "/" + @base.Substring(1);

			//Com_sprintf (sexedFilename, sizeof(sexedFilename), "#players/%s/%s", model, base+1);
			sfx = this.FindName(sexedFilename, false);

			if (sfx != null)
				return sfx;

			//
			// fall back strategies
			//
			// not found , so see if it exists
			if (FS.FileLength(sexedFilename.Substring(1)) > 0)
			{
				// yes, register it
				return this.RegisterSound(sexedFilename);
			}

			// try it with the female sound in the pak0.pak
			if (model.Equals("female", StringComparison.OrdinalIgnoreCase))
			{
				var femaleFilename = "player/female/" + @base.Substring(1);

				if (FS.FileLength("sound/" + femaleFilename) > 0)
					return this.AliasName(sexedFilename, femaleFilename);
			}

			// no chance, revert to the male sound in the pak0.pak
			var maleFilename = "player/male/" + @base.Substring(1);

			return this.AliasName(sexedFilename, maleFilename);
		}

		private static readonly sfx_t[] known_sfx = new sfx_t[Sound.MAX_SFX];

		private static void OpenTkSoundImpl2()
		{
			for (var i = 0; i < OpenTkSoundImpl.known_sfx.Length; i++)
				OpenTkSoundImpl.known_sfx[i] = new sfx_t();
		}

		private static int num_sfx;

		private sfx_t FindName(string name, bool create)
		{
			int i;
			sfx_t sfx = null;

			if (name == null)
				Com.Error(Defines.ERR_FATAL, "S_FindName: NULL\n");

			if (name.Length == 0)
				Com.Error(Defines.ERR_FATAL, "S_FindName: empty name\n");

			if (name.Length >= Defines.MAX_QPATH)
				Com.Error(Defines.ERR_FATAL, "Sound name too long: " + name);

			// see if already loaded
			for (i = 0; i < OpenTkSoundImpl.num_sfx; i++)
				if (name.Equals(OpenTkSoundImpl.known_sfx[i].name))
				{
					return OpenTkSoundImpl.known_sfx[i];
				}

			if (!create)
				return null;

			// find a free sfx
			for (i = 0; i < OpenTkSoundImpl.num_sfx; i++)
				if (OpenTkSoundImpl.known_sfx[i].name == null)

					// registration_sequence < s_registration_sequence)
					break;

			if (i == OpenTkSoundImpl.num_sfx)
			{
				if (OpenTkSoundImpl.num_sfx == Sound.MAX_SFX)
					Com.Error(Defines.ERR_FATAL, "S_FindName: out of sfx_t");

				OpenTkSoundImpl.num_sfx++;
			}

			sfx = OpenTkSoundImpl.known_sfx[i];
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

			s = new string(truename);

			// find a free sfx
			for (i = 0; i < OpenTkSoundImpl.num_sfx; i++)
				if (OpenTkSoundImpl.known_sfx[i].name == null)
					break;

			if (i == OpenTkSoundImpl.num_sfx)
			{
				if (OpenTkSoundImpl.num_sfx == Sound.MAX_SFX)
					Com.Error(Defines.ERR_FATAL, "S_FindName: out of sfx_t");

				OpenTkSoundImpl.num_sfx++;
			}

			sfx = OpenTkSoundImpl.known_sfx[i];
			sfx.clear();
			sfx.name = new string(aliasname);
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
				Com.Printf("S_StartLocalSound: can't cache " + sound + "\n");

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
				{
					writer.Write((short) (data.Read() - 128));
				}
			}
			else
			{
				data.Read(pcm);
			}

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

			while (i < Cmd.Argc())
			{
				name = new string(Cmd.Argv(i));

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

			for (i = 0; i < OpenTkSoundImpl.num_sfx; i++)
			{
				sfx = OpenTkSoundImpl.known_sfx[i];

				if (sfx.registration_sequence == 0)
					continue;

				sc = sfx.cache;

				if (sc != null)
				{
					size = sc.length * sc.width * (sc.stereo + 1);
					total += size;

					if (sc.loopstart >= 0)
						Com.Printf("L");
					else
						Com.Printf(" ");

					Com.Printf("(%2db) %6i : %s\n", new Vargs(3).add(sc.width * 8).add(size).add(sfx.name));
				}
				else
				{
					if (sfx.name[0] == '*')
						Com.Printf("  placeholder : " + sfx.name + "\n");
					else
						Com.Printf("  not loaded  : " + sfx.name + "\n");
				}
			}

			Com.Printf("Total resident: " + total + "\n");
		}

		private void SoundInfo_f()
		{
			Com.Printf("%5d stereo\n", new Vargs(1).add(1));
			Com.Printf("%5d samples\n", new Vargs(1).add(22050));
			Com.Printf("%5d samplebits\n", new Vargs(1).add(16));
			Com.Printf("%5d speed\n", new Vargs(1).add(44100));
		}

		static OpenTkSoundImpl()
		{
			OpenTkSoundImpl.OpenTkSoundImpl1();
			OpenTkSoundImpl.OpenTkSoundImpl2();
		}
	}
}
