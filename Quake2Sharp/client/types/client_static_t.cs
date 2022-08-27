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
namespace Quake2Sharp.client.types;

using qcommon.types;
using System.IO;

public class client_static_t
{
	// was enum connstate_t
	public int state;

	// was enum keydest_t
	public int key_dest;
	public int framecount;
	public int realtime; // always increasing, no clamping, etc
	public float frametime; // seconds since last frame

	//	   screen rendering information
	public float disable_screen; // showing loading plaque between levels

	// or changing rendering dlls
	// if time gets > 30 seconds ahead, break it
	public int disable_servercount; // when we receive a frame and cl.servercount

	// > cls.disable_servercount, clear disable_screen

	//	   connection information
	public string servername = ""; // name of server from original connect
	public float connect_time; // for connection retransmits
	public int quakePort; // a 16 bit value that allows quake servers

	// to work around address translating routers
	public netchan_t netchan = new();
	public int serverProtocol; // in case we are doing some kind of version hack
	public int challenge; // from the server to use for connecting
	public Stream download; // file transfer from server
	public string downloadtempname = "";
	public string downloadname = "";
	public int downloadnumber;

	// was enum dltype_t
	public int downloadtype;
	public int downloadpercent;

	//	   demo recording info must be here, so it isn't cleared on level change
	public bool demorecording;
	public bool demowaiting; // don't record until a non-delta message is received
	public BinaryWriter demofile;
}