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
namespace Quake2Sharp.sound
{
	using qcommon;
	using System.Text;
	using types;

	/**
 * SND_MEM
 */
	public class WaveLoader
	{
		/**
	 * This is the maximum sample length in bytes which has to be replaced by 
	 * a configurable variable.
	 */
		private static readonly int maxsamplebytes = 2048 * 1024;

		/** 
	 * Loads a sound from a wav file. 
	 */
		public static sfxcache_t LoadSound(sfx_t s)
		{
			if (s.name[0] == '*')
				return null;

			// see if still in memory
			var sc = s.cache;

			if (sc != null)
				return sc;

			string name;

			// load it in
			if (s.truename != null)
				name = s.truename;
			else
				name = s.name;

			string namebuffer;

			if (name[0] == '#')
				namebuffer = name[1..];
			else
				namebuffer = "sound/" + name;

			var data = FS.LoadFile(namebuffer);

			if (data == null)
			{
				Com.DPrintf("Couldn't load " + namebuffer + "\n");

				return null;
			}

			var size = data.Length;

			var info = WaveLoader.GetWavinfo(s.name, data, size);

			if (info.channels != 1)
			{
				Com.Printf(s.name + " is a stereo sample - ignoring\n");

				return null;
			}

			float stepscale = 1;
			var len = (int)(info.samples / stepscale);
			len = len * info.width * info.channels;

			if (len >= WaveLoader.maxsamplebytes)
			{
				Com.Printf(s.name + " is too long: " + len + " bytes?! ignoring.\n");

				return null;
			}

			sc = s.cache = new(len);

			sc.length = info.samples;
			sc.loopstart = info.loopstart;
			sc.speed = info.rate;
			sc.width = info.width;
			sc.stereo = info.channels;

			WaveLoader.ResampleSfx(s, sc.speed, sc.width, data, info.dataofs);
			data = null;

			return sc;
		}

		public static void ResampleSfx(sfx_t sfx, int inrate, int inwidth, byte[] data, int offset)
		{
			int outcount;
			int srcsample;
			int i;
			int sample, samplefrac, fracstep;
			sfxcache_t sc;

			sc = sfx.cache;

			if (sc == null)
				return;

			// again calculate the stretching factor.
			// this is usually 0.5, 1, or 2

			float stepscale = 1;

			outcount = (int)(sc.length / stepscale);
			sc.length = outcount;

			if (sc.loopstart != -1)
				sc.loopstart = (int)(sc.loopstart / stepscale);

			sc.width = inwidth;
			sc.stereo = 0;
			samplefrac = 0;
			fracstep = (int)(stepscale * 256);

			for (i = 0; i < outcount; i++)
			{
				srcsample = samplefrac >> 8;
				samplefrac += fracstep;

				if (inwidth == 2)
					sample = (data[offset + srcsample * 2] & 0xff) + (data[offset + srcsample * 2 + 1] << 8);
				else
					sample = ((data[offset + srcsample] & 0xff) - 128) << 8;

				if (sc.width == 2)
				{
					if (Defines.LITTLE_ENDIAN)
					{
						sc.data[i * 2] = (byte)(sample & 0xff);
						sc.data[i * 2 + 1] = (byte)(((uint)sample >> 8) & 0xff);
					}
					else
					{
						sc.data[i * 2] = (byte)(((uint)sample >> 8) & 0xff);
						sc.data[i * 2 + 1] = (byte)(sample & 0xff);
					}
				}
				else
					sc.data[i] = (byte)(sample >> 8);
			}
		}

		private static byte[] data_b;
		private static int data_p;
		private static int iff_end;
		private static int last_chunk;
		private static int iff_data;
		private static int iff_chunk_len;

		private static short GetLittleShort()
		{
			var val = 0;
			val = WaveLoader.data_b[WaveLoader.data_p] & 0xFF;
			WaveLoader.data_p++;
			val |= (WaveLoader.data_b[WaveLoader.data_p] & 0xFF) << 8;
			WaveLoader.data_p++;

			return (short)val;
		}

		private static int GetLittleLong()
		{
			var val = 0;
			val = WaveLoader.data_b[WaveLoader.data_p] & 0xFF;
			WaveLoader.data_p++;
			val |= (WaveLoader.data_b[WaveLoader.data_p] & 0xFF) << 8;
			WaveLoader.data_p++;
			val |= (WaveLoader.data_b[WaveLoader.data_p] & 0xFF) << 16;
			WaveLoader.data_p++;
			val |= (WaveLoader.data_b[WaveLoader.data_p] & 0xFF) << 24;
			WaveLoader.data_p++;

			return val;
		}

		private static void FindNextChunk(string name)
		{
			while (true)
			{
				WaveLoader.data_p = WaveLoader.last_chunk;

				if (WaveLoader.data_p >= WaveLoader.iff_end)
				{
					// didn't find the chunk
					WaveLoader.data_p = 0;

					return;
				}

				WaveLoader.data_p += 4;

				WaveLoader.iff_chunk_len = WaveLoader.GetLittleLong();

				if (WaveLoader.iff_chunk_len < 0)
				{
					WaveLoader.data_p = 0;

					return;
				}

				if (WaveLoader.iff_chunk_len > 1024 * 1024)
					Com.Println(" Warning: FindNextChunk: length is past the 1 meg sanity limit");

				WaveLoader.data_p -= 8;
				WaveLoader.last_chunk = WaveLoader.data_p + 8 + ((WaveLoader.iff_chunk_len + 1) & ~1);
				var s = Encoding.ASCII.GetString(WaveLoader.data_b, WaveLoader.data_p, 4);

				if (s.Equals(name))
					return;
			}
		}

		private static void FindChunk(string name)
		{
			WaveLoader.last_chunk = WaveLoader.iff_data;
			WaveLoader.FindNextChunk(name);
		}

		/*
		============
		GetWavinfo
		============
		*/
		private static wavinfo_t GetWavinfo(string name, byte[] wav, int wavlength)
		{
			wavinfo_t info = new();
			int i;
			int format;
			int samples;

			if (wav == null)
				return info;

			WaveLoader.iff_data = 0;
			WaveLoader.iff_end = wavlength;
			WaveLoader.data_b = wav;

			// find "RIFF" chunk
			WaveLoader.FindChunk("RIFF");
			var s = Encoding.ASCII.GetString(WaveLoader.data_b, WaveLoader.data_p + 8, 4);

			if (!s.Equals("WAVE"))
			{
				Com.Printf("Missing RIFF/WAVE chunks\n");

				return info;
			}

			//	   get "fmt " chunk
			WaveLoader.iff_data = WaveLoader.data_p + 12;

			//	   DumpChunks ();

			WaveLoader.FindChunk("fmt ");

			if (WaveLoader.data_p == 0)
			{
				Com.Printf("Missing fmt chunk\n");

				return info;
			}

			WaveLoader.data_p += 8;
			format = WaveLoader.GetLittleShort();

			if (format != 1)
			{
				Com.Printf("Microsoft PCM format only\n");

				return info;
			}

			info.channels = WaveLoader.GetLittleShort();
			info.rate = WaveLoader.GetLittleLong();
			WaveLoader.data_p += 4 + 2;
			info.width = WaveLoader.GetLittleShort() / 8;

			//	   get cue chunk
			WaveLoader.FindChunk("cue ");

			if (WaveLoader.data_p != 0)
			{
				WaveLoader.data_p += 32;
				info.loopstart = WaveLoader.GetLittleLong();

				//			Com_Printf("loopstart=%d\n", sfx->loopstart);

				// if the next chunk is a LIST chunk, look for a cue length marker
				WaveLoader.FindNextChunk("LIST");

				if (WaveLoader.data_p != 0)
				{
					if (WaveLoader.data_b.Length >= WaveLoader.data_p + 32)
					{
						s = Encoding.ASCII.GetString(WaveLoader.data_b, WaveLoader.data_p + 28, 4);

						if (s.Equals("MARK"))
						{
							// this is not a proper parse, but
							// it works with cooledit...
							WaveLoader.data_p += 24;
							i = WaveLoader.GetLittleLong(); // samples in loop
							info.samples = info.loopstart + i;

							//					Com_Printf("looped length: %i\n", i);
						}
					}
				}
			}
			else
				info.loopstart = -1;

			//	   find data chunk
			WaveLoader.FindChunk("data");

			if (WaveLoader.data_p == 0)
			{
				Com.Printf("Missing data chunk\n");

				return info;
			}

			WaveLoader.data_p += 4;
			samples = WaveLoader.GetLittleLong() / info.width;

			if (info.samples != 0)
			{
				if (samples < info.samples)
					Com.Error(Defines.ERR_DROP, "Sound " + name + " has a bad loop length");
			}
			else
			{
				info.samples = samples;

				if (info.loopstart > 0)
					info.samples -= info.loopstart;
			}

			info.dataofs = WaveLoader.data_p;

			return info;
		}
	}
}
