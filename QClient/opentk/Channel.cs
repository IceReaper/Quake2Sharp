namespace QClient.opentk;

using client;
using game.types;
using OpenTK.Audio.OpenAL;
using OpenTK.Audio.OpenAL.Extensions.Creative.EFX;
using qcommon;
using sound.types;
using util;

public class Channel
{
	public const int LISTENER = 0;
	public const int FIXED = 1;
	public const int DYNAMIC = 2;
	private static readonly int MAX_CHANNELS = 32;
	private static readonly float[] NULLVECTOR = { 0, 0, 0 };
	private static readonly Channel[] channels = new Channel[Channel.MAX_CHANNELS];
	private static readonly int[] sources = new int[Channel.MAX_CHANNELS];

	// a reference of JOALSoundImpl.buffers
	private static int[] buffers;
	private static readonly Dictionary<int, Channel> looptable = new();
	private static int numChannels;

	// stream handling
	private static bool streamingEnabled;
	private static int streamQueue;

	// sound attributes
	private int type;
	private int entnum;
	private int entchannel;
	private int bufferId;
	private readonly int sourceId;
	private float volume;
	private float rolloff;
	private readonly float[] origin = { 0, 0, 0 };

	// update flags
	private bool autosound;
	private bool active;
	private bool modified;
	private bool bufferChanged;
	private bool volumeChanged;

	private Channel(int sourceId)
	{
		this.sourceId = sourceId;
		this.clear();
		this.volumeChanged = false;
		this.volume = 1.0f;
	}

	private void clear()
	{
		this.entnum = this.entchannel = this.bufferId = -1;
		this.bufferChanged = false;
		this.rolloff = 0;
		this.autosound = false;
		this.active = false;
		this.modified = false;
	}

	private static readonly int[] tmp = new int[1];

	public static int init(int[] buffers, int efxSlot)
	{
		Channel.buffers = buffers;

		// create channels
		int sourceId;
		Channel.numChannels = 0;

		for (var i = 0; i < Channel.MAX_CHANNELS; i++)
		{
			try
			{
				AL.GenSources(1, Channel.tmp);
				sourceId = Channel.tmp[0];

				// can't generate more sources
				if (sourceId <= 0)
					break;
			}
			catch (Exception)
			{
				// can't generate more sources
				break;
			}

			Channel.sources[i] = sourceId;

			Channel.channels[i] = new(sourceId);
			Channel.numChannels++;

			// set default values for AL sources
			AL.Source(sourceId, ALSourcef.Gain, 1.0f);
			AL.Source(sourceId, ALSourcef.Pitch, 1.0f);
			AL.Source(sourceId, ALSourceb.SourceRelative, false);
			AL.Source(sourceId, ALSource3f.Velocity, Channel.NULLVECTOR[0], Channel.NULLVECTOR[1], Channel.NULLVECTOR[2]);
			AL.Source(sourceId, ALSourceb.Looping, false);
			AL.Source(sourceId, ALSourcef.ReferenceDistance, 200.0f);
			AL.Source(sourceId, ALSourcef.MinGain, 0.0005f);
			AL.Source(sourceId, ALSourcef.MaxGain, 1.0f);

			if (efxSlot != 0)
				EFX.Source(sourceId, EFXSourceInteger3.AuxiliarySendFilter, efxSlot, 0, 0);
		}

		return Channel.numChannels;
	}

	public static void reset()
	{
		for (var i = 0; i < Channel.numChannels; i++)
		{
			AL.SourceStop(Channel.sources[i]);
			AL.Source(Channel.sources[i], ALSourcei.Buffer, 0);
			Channel.channels[i].clear();
		}
	}

	public static void shutdown()
	{
		AL.DeleteSources(Channel.sources);
		Channel.numChannels = 0;
	}

	private static void enableStreaming()
	{
		if (Channel.streamingEnabled)
			return;

		// use the last source
		Channel.numChannels--;
		Channel.streamingEnabled = true;
		Channel.streamQueue = 0;

		var source = Channel.channels[Channel.numChannels].sourceId;
		AL.Source(source, ALSourceb.SourceRelative, true);
		AL.Source(source, ALSourcef.Gain, 1.0f);
		Channel.channels[Channel.numChannels].volumeChanged = true;

		Com.DPrintf("streaming enabled\n");
	}

	public static void disableStreaming()
	{
		if (!Channel.streamingEnabled)
			return;

		Channel.unqueueStreams();
		var source = Channel.channels[Channel.numChannels].sourceId;
		AL.Source(source, ALSourceb.SourceRelative, false);

		// free the last source
		Channel.numChannels++;
		Channel.streamingEnabled = false;
		Com.DPrintf("streaming disabled\n");
	}

	private static void unqueueStreams()
	{
		if (!Channel.streamingEnabled)
			return;

		var source = Channel.channels[Channel.numChannels].sourceId;

		// stop streaming
		AL.SourceStop(source);
		int count;
		AL.GetSource(source, ALGetSourcei.BuffersQueued, out count);
		Com.DPrintf("unqueue " + count + " buffers\n");

		while (count-- > 0)
			AL.SourceUnqueueBuffers(source, 1, Channel.tmp);

		Channel.streamQueue = 0;
	}

	public static void updateStream(byte[] samples, ALFormat format, int rate)
	{
		Channel.enableStreaming();
		var buffer = new int[] { 0 };
		var source = Channel.channels[Channel.numChannels].sourceId;

		int processed;
		AL.GetSource(source, ALGetSourcei.BuffersProcessed, out processed);
		int state;
		AL.GetSource(source, ALGetSourcei.SourceState, out state);
		var playing = state == (int)ALSourceState.Playing;
		var interupted = !playing && Channel.streamQueue > 2;

		if (interupted)
		{
			Channel.unqueueStreams();
			buffer[0] = Channel.buffers[OpenTkSound.MAX_SFX + Channel.streamQueue++];
			Com.DPrintf("queue " + (Channel.streamQueue - 1) + '\n');
		}
		else if (processed < 2)
		{
			// check queue overrun
			if (Channel.streamQueue >= OpenTkSound.STREAM_QUEUE)
				return;

			buffer[0] = Channel.buffers[OpenTkSound.MAX_SFX + Channel.streamQueue++];
			Com.DPrintf("queue " + (Channel.streamQueue - 1) + '\n');
		}
		else
		{
			// reuse the buffer
			AL.SourceUnqueueBuffers(source, 1, buffer);
		}

		AL.BufferData(buffer[0], format, samples, rate);
		AL.SourceQueueBuffers(source, 1, buffer);

		if (Channel.streamQueue > 1 && !playing)
		{
			Com.DPrintf("start sound\n");
			AL.SourcePlay(source);
		}
	}

	public static void addPlaySounds()
	{
		while (Channel.assign(PlaySound.nextPlayableSound()))
			;
	}

	private static bool assign(PlaySound ps)
	{
		if (ps == null)
			return false;

		Channel ch = null;
		int i;

		for (i = 0; i < Channel.numChannels; i++)
		{
			ch = Channel.channels[i];

			if (ps.entchannel != 0 && ch.entnum == ps.entnum && ch.entchannel == ps.entchannel)
			{
				// always override sound from same entity
				if (ch.bufferId != ps.bufferId)
					AL.SourceStop(ch.sourceId);

				break;
			}

			// don't let monster sounds override player sounds
			if (ch.entnum == Globals.cl.playernum + 1 && ps.entnum != Globals.cl.playernum + 1 && ch.bufferId != -1)
				continue;

			// looking for a free AL source
			if (!ch.active)
				break;
		}

		if (i == Channel.numChannels)
			return false;

		ch.type = ps.type;

		if (ps.type == Channel.FIXED)
			Math3D.VectorCopy(ps.origin, ch.origin);

		ch.entnum = ps.entnum;
		ch.entchannel = ps.entchannel;
		ch.bufferChanged = ch.bufferId != ps.bufferId;
		ch.bufferId = ps.bufferId;
		ch.volumeChanged = ch.volume != ps.volume;
		ch.volume = ps.volume;
		ch.rolloff = ps.attenuation * 2;
		ch.active = true;
		ch.modified = true;

		return true;
	}

	private static Channel pickForLoop(int bufferId, float attenuation)
	{
		Channel ch;

		for (var i = 0; i < Channel.numChannels; i++)
		{
			ch = Channel.channels[i];

			// looking for a free AL source
			if (!ch.active)
			{
				ch.entnum = 0;
				ch.entchannel = 0;
				ch.bufferChanged = ch.bufferId != bufferId;
				ch.bufferId = bufferId;
				ch.volumeChanged = ch.volume < 1.0f;
				ch.volume = 1.0f;
				ch.rolloff = attenuation * 2;
				ch.active = true;
				ch.modified = true;

				return ch;
			}
		}

		return null;
	}

	// stack variables
	private static readonly float[] entityOrigin = { 0, 0, 0 };
	private static readonly float[] sourceOrigin = { 0, 0, 0 };

	public static void playAllSounds(float[] listenerOrigin)
	{
		Channel ch;
		int sourceId;
		int state;
		var tmp = new int[] { 0 };

		for (var i = 0; i < Channel.numChannels; i++)
		{
			ch = Channel.channels[i];

			if (ch.active)
			{
				sourceId = ch.sourceId;

				switch (ch.type)
				{
					case Channel.LISTENER:
						Math3D.VectorCopy(listenerOrigin, Channel.sourceOrigin);

						break;

					case Channel.DYNAMIC:
						CL_ents.GetEntitySoundOrigin(ch.entnum, Channel.entityOrigin);
						Channel.convertVector(Channel.entityOrigin, Channel.sourceOrigin);

						break;

					case Channel.FIXED:
						Channel.convertVector(ch.origin, Channel.sourceOrigin);

						break;
				}

				if (ch.modified)
				{
					if (ch.bufferChanged)
					{
						try
						{
							AL.Source(sourceId, ALSourcei.Buffer, ch.bufferId);
						}
						catch (Exception)
						{
							// fallback for buffer changing
							AL.SourceStop(sourceId);
							AL.Source(sourceId, ALSourcei.Buffer, ch.bufferId);
						}
					}

					if (ch.volumeChanged)
						AL.Source(sourceId, ALSourcef.Gain, ch.volume);

					AL.Source(sourceId, ALSourcef.RolloffFactor, ch.rolloff);
					AL.Source(sourceId, ALSource3f.Position, Channel.sourceOrigin[0], Channel.sourceOrigin[1], Channel.sourceOrigin[2]);
					AL.SourcePlay(sourceId);
					ch.modified = false;
				}
				else
				{
					AL.GetSource(sourceId, ALGetSourcei.SourceState, out state);

					if (state == (int)ALSourceState.Playing)
						AL.Source(sourceId, ALSource3f.Position, Channel.sourceOrigin[0], Channel.sourceOrigin[1], Channel.sourceOrigin[2]);
					else
						ch.clear();
				}

				ch.autosound = false;
			}
		}
	}

	/*
	 * 	adddLoopSounds
	 * 	Entities with a ->sound field will generated looped sounds
	 * 	that are automatically started, stopped, and merged together
	 * 	as the entities are sent to the client
	 */
	public static void addLoopSounds()
	{
		if (Globals.cl_paused.value != 0.0f || Globals.cls.state != Defines.ca_active || !Globals.cl.sound_prepped)
		{
			Channel.removeUnusedLoopSounds();

			return;
		}

		Channel ch;
		sfx_t sfx;
		sfxcache_t sc;
		int num;
		entity_state_t ent;
		int key;
		var sound = 0;

		for (var i = 0; i < Globals.cl.frame.num_entities; i++)
		{
			num = (Globals.cl.frame.parse_entities + i) & (Defines.MAX_PARSE_ENTITIES - 1);
			ent = Globals.cl_parse_entities[num];
			sound = ent.sound;

			if (sound == 0)
				continue;

			key = ent.number;
			Channel.looptable.TryGetValue(key, out ch);

			if (ch != null)
			{
				// keep on looping
				ch.autosound = true;
				Math3D.VectorCopy(ent.origin, ch.origin);

				continue;
			}

			sfx = Globals.cl.sound_precache[sound];

			if (sfx == null)
				continue; // bad sound effect

			sc = sfx.cache;

			if (sc == null)
				continue;

			// allocate a channel
			ch = Channel.pickForLoop(Channel.buffers[sfx.bufferId], 6);

			if (ch == null)
				break;

			ch.type = Channel.FIXED;
			Math3D.VectorCopy(ent.origin, ch.origin);
			ch.autosound = true;

			Channel.looptable.Add(key, ch);
			AL.Source(ch.sourceId, ALSourceb.Looping, true);
		}

		Channel.removeUnusedLoopSounds();
	}

	private static void removeUnusedLoopSounds()
	{
		Channel ch;

		// stop unused loopsounds
		foreach (var k in Channel.looptable.Keys.ToArray())
		{
			ch = Channel.looptable[k];

			if (!ch.autosound)
			{
				AL.SourceStop(ch.sourceId);
				AL.Source(ch.sourceId, ALSourceb.Looping, false);
				Channel.looptable.Remove(k);
				ch.clear();
			}
		}
	}

	public static void convertVector(float[] from, float[] to)
	{
		to[0] = from[0];
		to[1] = from[2];
		to[2] = -from[1];
	}

	public static void convertOrientation(float[] forward, float[] up, float[] orientation)
	{
		orientation[0] = forward[0];
		orientation[1] = forward[2];
		orientation[2] = -forward[1];
		orientation[3] = up[0];
		orientation[4] = up[2];
		orientation[5] = -up[1];
	}
}