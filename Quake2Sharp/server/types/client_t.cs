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
namespace Quake2Sharp.server.types
{
	using game.types;
	using qcommon.types;

	public class client_t
	{
		public client_t()
		{
			for (var n = 0; n < Defines.UPDATE_BACKUP; n++)
				this.frames[n] = new();
		}

		public static readonly int LATENCY_COUNTS = 16;
		public static readonly int RATE_MESSAGES = 10;
		public int state;
		public string userinfo = "";
		public int lastframe; // for delta compression
		public usercmd_t lastcmd = new(); // for filling in big drops
		public int commandMsec; // every seconds this is reset, if user

		// commands exhaust it, assume time cheating
		public int[] frame_latency = new int[client_t.LATENCY_COUNTS];
		public int ping;
		public int[] message_size = new int[client_t.RATE_MESSAGES]; // used to rate drop packets
		public int rate;
		public int surpressCount; // number of messages rate supressed

		// pointer
		public edict_t edict; // EDICT_NUM(clientnum+1)

		//char				name[32];			// extracted from userinfo, high bits masked
		public string name = ""; // extracted from userinfo, high bits masked
		public int messagelevel; // for filtering printed messages

		// The datagram is written to by sound calls, prints, temp ents, etc.
		// It can be harmlessly overflowed.
		public sizebuf_t datagram = new();
		public byte[] datagram_buf = new byte[Defines.MAX_MSGLEN];
		public client_frame_t[] frames = new client_frame_t[Defines.UPDATE_BACKUP]; // updates can be delta'd from here
		public byte[] download; // file being downloaded
		public int downloadsize; // total bytes (can't use EOF because of paks)
		public int downloadcount; // bytes sent
		public int lastmessage; // sv.framenum when packet was last received
		public int lastconnect;
		public int challenge; // challenge of this user, randomly generated
		public netchan_t netchan = new();
		public int serverindex;
	}
}
