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
namespace QClient.client;

using game;
using game.types;
using qcommon;
using qcommon.types;
using server;
using sound;
using sys;
using System.Text;
using types;
using util;

/**
	 * CL
	 */
public class Cl
{
	private static int precacheCheck; // for autodownload of precache items
	private static int precacheSpawncount;
	private static int precacheTex;
	private static int precacheModelSkin;
	private static byte[] precacheModel; // used for skin checking in alias models
	private const int PlayerMult = 5;

	private static readonly string[][] Cheatvarsinfo =
	{
		new[] { "timescale", "1" },
		new[] { "timedemo", "0" },
		new[] { "r_drawworld", "1" },
		new[] { "cl_testlights", "0" },
		new[] { "r_fullbright", "0" },
		new[] { "r_drawflat", "0" },
		new[] { "paused", "0" },
		new[] { "fixedtime", "0" },
		new[] { "sw_draworder", "0" },
		new[] { "gl_lightmap", "0" },
		new[] { "gl_saturatelighting", "0" },
		new string[] { null, null }
	};

	private static readonly cheatvar_t[] Cheatvars;

	static Cl()
	{
		Cl.Cheatvars = new cheatvar_t[Cl.Cheatvarsinfo.Length];

		for (var n = 0; n < Cl.Cheatvarsinfo.Length; n++)
			Cl.Cheatvars[n] = new() { name = Cl.Cheatvarsinfo[n][0], value = Cl.Cheatvarsinfo[n][1] };
	}

	private static int numcheatvars;

	/**
		 * Stop_f
		 * 
		 * Stop recording a demo.
		 */
	private static readonly Action StopF = () =>
	{
		try
		{
			int len;

			if (!Globals.cls.demorecording)
			{
				Com.Printf("Not recording a demo.\n");

				return;
			}

			//	   finish up
			len = -1;
			Globals.cls.demofile.Write(EndianHandler.swapInt(len));
			Globals.cls.demofile.Close();
			Globals.cls.demofile = null;
			Globals.cls.demorecording = false;
			Com.Printf("Stopped demo.\n");
		}
		catch (Exception)
		{
		}
	};

	private static readonly entity_state_t Nullstate = new(null);

	/**
		 * Record_f
		 * 
		 * record &lt;demoname&gt;
		 * Begins recording a demo from the current position.
		 */
	private static readonly Action RecordF = () =>
	{
		try
		{
			string name;
			var bufData = new byte[Defines.MAX_MSGLEN];
			sizebuf_t buf = new();
			int i;
			entity_state_t ent;

			if (Cmd.Argc() != 2)
			{
				Com.Printf("record <demoname>\n");

				return;
			}

			if (Globals.cls.demorecording)
			{
				Com.Printf("Already recording.\n");

				return;
			}

			if (Globals.cls.state != Defines.ca_active)
			{
				Com.Printf("You must be in a level to record.\n");

				return;
			}

			//
			// open the demo file
			//
			name = FS.Gamedir() + "/demos/" + Cmd.Argv(1) + ".dm2";
			Com.Printf("recording to " + name + ".\n");
			FS.CreatePath(name);
			Globals.cls.demofile = new(File.OpenWrite(name));

			if (Globals.cls.demofile == null)
			{
				Com.Printf("ERROR: couldn't open.\n");

				return;
			}

			Globals.cls.demorecording = true;

			// don't start saving messages until a non-delta compressed
			// message is received
			Globals.cls.demowaiting = true;

			//
			// write out messages to hold the startup information
			//
			SZ.Init(buf, bufData, Defines.MAX_MSGLEN);

			// send the serverdata
			MSG.WriteByte(buf, Defines.svc_serverdata);
			MSG.WriteInt(buf, Defines.PROTOCOL_VERSION);
			MSG.WriteInt(buf, 0x10000 + Globals.cl.servercount);
			MSG.WriteByte(buf, 1); // demos are always attract loops
			MSG.WriteString(buf, Globals.cl.gamedir);
			MSG.WriteShort(buf, Globals.cl.playernum);
			MSG.WriteString(buf, Globals.cl.configstrings[Defines.CS_NAME]);

			// configstrings
			for (i = 0; i < Defines.MAX_CONFIGSTRINGS; i++)
			{
				if (Globals.cl.configstrings[i].Length <= 0)
					continue;

				if (buf.cursize + Globals.cl.configstrings[i].Length + 32 > buf.maxsize)
				{
					// write it out
					Globals.cls.demofile.Write(EndianHandler.swapInt(buf.cursize));
					Globals.cls.demofile.Write(buf.data, 0, buf.cursize);
					buf.cursize = 0;
				}

				MSG.WriteByte(buf, Defines.svc_configstring);
				MSG.WriteShort(buf, i);
				MSG.WriteString(buf, Globals.cl.configstrings[i]);
			}

			// baselines
			Cl.Nullstate.clear();

			for (i = 0; i < Defines.MAX_EDICTS; i++)
			{
				ent = Globals.cl_entities[i].baseline;

				if (ent.modelindex == 0)
					continue;

				if (buf.cursize + 64 > buf.maxsize)
				{
					// write it out
					Globals.cls.demofile.Write(EndianHandler.swapInt(buf.cursize));
					Globals.cls.demofile.Write(buf.data, 0, buf.cursize);
					buf.cursize = 0;
				}

				MSG.WriteByte(buf, Defines.svc_spawnbaseline);
				MSG.WriteDeltaEntity(Cl.Nullstate, Globals.cl_entities[i].baseline, buf, true, true);
			}

			MSG.WriteByte(buf, Defines.svc_stufftext);
			MSG.WriteString(buf, "precache\n");

			// write it to the demo file
			Globals.cls.demofile.Write(EndianHandler.swapInt(buf.cursize));
			Globals.cls.demofile.Write(buf.data, 0, buf.cursize);

			// the rest of the demo file will be individual frames
		}
		catch (Exception)
		{
			// ignored
		}
	};

	/**
		 * ForwardToServer_f
		 */
	private static readonly Action ForwardToServerF = () =>
	{
		if (Globals.cls.state != Defines.ca_connected && Globals.cls.state != Defines.ca_active)
		{
			Com.Printf("Can't \"" + Cmd.Argv(0) + "\", not connected\n");

			return;
		}

		// don't forward the first argument
		if (Cmd.Argc() <= 1)
			return;

		MSG.WriteByte(Globals.cls.netchan.message, Defines.clc_stringcmd);
		SZ.Print(Globals.cls.netchan.message, Cmd.Args());
	};

	/**
		 * Pause_f
		 */
	private static readonly Action PauseF = () =>
	{
		// never pause in multiplayer

		if (Cvar.VariableValue("maxclients") > 1 || Globals.server_state == 0)
		{
			Cvar.SetValue("paused", 0);

			return;
		}

		Cvar.SetValue("paused", Globals.cl_paused.value);
	};

	/**
		 * Quit_f
		 */
	public static Action QuitF = () =>
	{
		Cl.Disconnect();
		Com.Quit();
	};

	/**
		 * Connect_f
		 */
	private static readonly Action ConnectF = () =>
	{
		string server;

		if (Cmd.Argc() != 2)
		{
			Com.Printf("usage: connect <server>\n");

			return;
		}

		// if running a local server, kill it and reissue
		if (Globals.server_state != 0)
			SV_MAIN.SV_Shutdown("Server quit\n", false);
		else
			Cl.Disconnect();

		server = Cmd.Argv(1);
		NET.ConfigClient(true); // allow remote
		Cl.Disconnect();
		Globals.cls.state = Defines.ca_connecting;

		//strncpy (cls.servername, server, sizeof(cls.servername)-1);
		Globals.cls.servername = server;
		Globals.cls.connect_time = -99999;

		// CL_CheckForResend() will fire immediately
	};

	/**
		 * Rcon_f
		 * 
		 * Send the rest of the command line over as an unconnected command.
		 */
	private static readonly Action RconF = () =>
	{
		if (Globals.rcon_client_password.@string.Length == 0)
		{
			Com.Printf("You must set 'rcon_password' before\nissuing an rcon command.\n");

			return;
		}

		StringBuilder message = new(1024);

		// connection less packet
		message.Append('\u00ff');
		message.Append('\u00ff');
		message.Append('\u00ff');
		message.Append('\u00ff');

		// allow remote
		NET.ConfigClient(true);
		message.Append("rcon ");
		message.Append(Globals.rcon_client_password.@string);
		message.Append(' ');

		for (var i = 1; i < Cmd.Argc(); i++)
		{
			message.Append(Cmd.Argv(i));
			message.Append(' ');
		}

		netadr_t to = new();

		if (Globals.cls.state >= Defines.ca_connected)
			to = Globals.cls.netchan.remote_address;
		else
		{
			if (Globals.rcon_address.@string.Length == 0)
			{
				Com.Printf("You must either be connected,\nor set the 'rcon_address' cvar\nto issue rcon commands\n");

				return;
			}

			NET.StringToAdr(Globals.rcon_address.@string, to);

			if (to.port == 0)
				to.port = Defines.PORT_SERVER;
		}

		message.Append('\0');
		var b = message.ToString();
		NET.SendPacket(Defines.NS_CLIENT, b.Length, Lib.stringToBytes(b), to);
	};

	private static readonly Action DisconnectF = () => { Com.Error(Defines.ERR_DROP, "Disconnected from server"); };

	/**
		 * Changing_f
		 * 
		 * Just sent as a hint to the client that they should drop to full console.
		 */
	private static readonly Action ChangingF = () =>
	{
		//ZOID
		//if we are downloading, we don't change!
		// This so we don't suddenly stop downloading a map

		if (Globals.cls.download != null)
			return;

		SCR.BeginLoadingPlaque();
		Globals.cls.state = Defines.ca_connected; // not active anymore, but

		// not disconnected
		Com.Printf("\nChanging map...\n");
	};

	/**
		 * Reconnect_f
		 * 
		 * The server is changing levels.
		 */
	private static readonly Action ReconnectF = () =>
	{
		//ZOID
		//if we are downloading, we don't change! This so we don't suddenly
		// stop downloading a map
		if (Globals.cls.download != null)
			return;

		S.StopAllSounds();

		if (Globals.cls.state == Defines.ca_connected)
		{
			Com.Printf("reconnecting...\n");
			Globals.cls.state = Defines.ca_connected;
			MSG.WriteChar(Globals.cls.netchan.message, Defines.clc_stringcmd);
			MSG.WriteString(Globals.cls.netchan.message, "new");

			return;
		}

		if (Globals.cls.servername == "")
			return;

		if (Globals.cls.state >= Defines.ca_connected)
		{
			Cl.Disconnect();
			Globals.cls.connect_time = Globals.cls.realtime - 1500;
		}
		else
			Globals.cls.connect_time = -99999; // fire immediately

		Globals.cls.state = Defines.ca_connecting;
		Com.Printf("reconnecting...\n");
	};

	/**
		 * PingServers_f
		 */
	public static Action PingServersF = () =>
	{
		int i;
		netadr_t adr = new();

		//char name[32];
		string name;
		string adrstring;
		cvar_t noudp;
		cvar_t noipx;
		NET.ConfigClient(true); // allow remote

		// send a broadcast packet
		Com.Printf("pinging broadcast...\n");
		noudp = Cvar.Get("noudp", "0", Defines.CVAR_NOSET);

		if (noudp.value == 0.0f)
		{
			adr.type = Defines.NA_BROADCAST;
			adr.port = Defines.PORT_SERVER;

			//adr.port = BigShort(PORT_SERVER);
			Netchan.OutOfBandPrint(Defines.NS_CLIENT, adr, "info " + Defines.PROTOCOL_VERSION);
		}

		// we use no IPX
		noipx = Cvar.Get("noipx", "1", Defines.CVAR_NOSET);

		if (noipx.value == 0.0f)
		{
			adr.type = Defines.NA_BROADCAST_IPX;

			//adr.port = BigShort(PORT_SERVER);
			adr.port = Defines.PORT_SERVER;
			Netchan.OutOfBandPrint(Defines.NS_CLIENT, adr, "info " + Defines.PROTOCOL_VERSION);
		}

		// send a packet to each address book entry
		for (i = 0; i < 16; i++)
		{
			//Com_sprintf (name, sizeof(name), "adr%i", i);
			name = "adr" + i;
			adrstring = Cvar.VariableString(name);

			if (adrstring == null || adrstring.Length == 0)
				continue;

			Com.Printf("pinging " + adrstring + "...\n");

			if (!NET.StringToAdr(adrstring, adr))
			{
				Com.Printf("Bad address: " + adrstring + "\n");

				continue;
			}

			if (adr.port == 0)

				//adr.port = BigShort(PORT_SERVER);
				adr.port = Defines.PORT_SERVER;

			Netchan.OutOfBandPrint(Defines.NS_CLIENT, adr, "info " + Defines.PROTOCOL_VERSION);
		}
	};

	/**
		 * Skins_f
		 * 
		 * Load or download any custom player skins and models.
		 */
	private static readonly Action SkinsF = () =>
	{
		int i;

		for (i = 0; i < Defines.MAX_CLIENTS; i++)
		{
			if (Globals.cl.configstrings[Defines.CS_PLAYERSKINS + i] == null)
				continue;

			Com.Printf("client " + i + ": " + Globals.cl.configstrings[Defines.CS_PLAYERSKINS + i] + "\n");
			SCR.UpdateScreen();
			Sys.SendKeyEvents(); // pump message loop
			CL_parse.ParseClientinfo(i);
		}
	};

	/**
		 * Userinfo_f
		 */
	private static readonly Action UserinfoF = () =>
	{
		Com.Printf("User info settings:\n");
		Info.Print(Cvar.Userinfo());
	};

	/**
		 * Snd_Restart_f
		 * 
		 * Restart the sound subsystem so it can pick up new parameters and flush
		 * all sounds.
		 */
	public static Action SndRestartF = () =>
	{
		S.Shutdown();
		S.Init();
		CL_parse.RegisterSounds();
	};

	//	   ENV_CNT is map load, ENV_CNT+1 is first env map
	public static readonly int EnvCnt = Defines.CS_PLAYERSKINS + Defines.MAX_CLIENTS * Cl.PlayerMult;
	public static readonly int TextureCnt = Cl.EnvCnt + 13;
	private static readonly string[] EnvSuf = { "rt", "bk", "lf", "ft", "up", "dn" };

	/**
		 * The server will send this command right before allowing the client into
		 * the server.
		 */
	private static readonly Action PrecacheF = () =>
	{
		// Yet another hack to let old demos work the old precache sequence.
		if (Cmd.Argc() < 2)
		{
			int[] iw = { 0 }; // for detecting cheater maps
			CM.CM_LoadMap(Globals.cl.configstrings[Defines.CS_MODELS + 1], true, iw);
			CL_parse.RegisterSounds();
			CL_view.PrepRefresh();

			return;
		}

		Cl.precacheCheck = Defines.CS_MODELS;
		Cl.precacheSpawncount = Lib.atoi(Cmd.Argv(1));
		Cl.precacheModel = null;
		Cl.precacheModelSkin = 0;
		Cl.RequestNextDownload();
	};

	private static int extratime;

	//	  ============================================================================

	/**
		 * Shutdown
		 * 
		 * FIXME: this is a callback from Sys_Quit and Com_Error. It would be better
		 * to run quit through here before the final handoff to the sys code.
		 */
	private static bool isdown;

	/**
		 * WriteDemoMessage
		 * 
		 * Dumps the current net message, prefixed by the length
		 */
	public static void WriteDemoMessage()
	{
		// the first eight bytes are just packet sequencing stuff
		var swlen = Globals.net_message.cursize - 8;

		try
		{
			Globals.cls.demofile.Write(EndianHandler.swapInt(swlen));
			Globals.cls.demofile.Write(Globals.net_message.data, 8, swlen);
		}
		catch (Exception)
		{
			// ignored
		}
	}

	/**
		 * SendConnectPacket
		 * 
		 * We have gotten a challenge from the server, so try and connect.
		 */
	private static void SendConnectPacket()
	{
		netadr_t adr = new();
		int port;

		if (!NET.StringToAdr(Globals.cls.servername, adr))
		{
			Com.Printf("Bad server address\n");
			Globals.cls.connect_time = 0;

			return;
		}

		if (adr.port == 0)
			adr.port = Defines.PORT_SERVER;

		//			adr.port = BigShort(PORT_SERVER);
		port = (int)Cvar.VariableValue("qport");
		Globals.userinfo_modified = false;

		Netchan.OutOfBandPrint(
			Defines.NS_CLIENT,
			adr,
			"connect " + Defines.PROTOCOL_VERSION + " " + port + " " + Globals.cls.challenge + " \"" + Cvar.Userinfo() + "\"\n"
		);
	}

	/**
		 * CheckForResend
		 * 
		 * Resend a connect message if the last one has timed out.
		 */
	private static void CheckForResend()
	{
		// if the local server is running and we aren't
		// then connect
		if (Globals.cls.state == Defines.ca_disconnected && Globals.server_state != 0)
		{
			Globals.cls.state = Defines.ca_connecting;
			Globals.cls.servername = "localhost";

			// we don't need a challenge on the localhost
			Cl.SendConnectPacket();

			return;
		}

		// resend if we haven't gotten a reply yet
		if (Globals.cls.state != Defines.ca_connecting)
			return;

		if (Globals.cls.realtime - Globals.cls.connect_time < 3000)
			return;

		netadr_t adr = new();

		if (!NET.StringToAdr(Globals.cls.servername, adr))
		{
			Com.Printf("Bad server address\n");
			Globals.cls.state = Defines.ca_disconnected;

			return;
		}

		if (adr.port == 0)
			adr.port = Defines.PORT_SERVER;

		// for retransmit requests
		Globals.cls.connect_time = Globals.cls.realtime;
		Com.Printf("Connecting to " + Globals.cls.servername + "...\n");
		Netchan.OutOfBandPrint(Defines.NS_CLIENT, adr, "getchallenge\n");
	}

	/**
		 * ClearState
		 * 
		 */
	public static void ClearState()
	{
		S.StopAllSounds();
		CL_fx.ClearEffects();
		TemporaryEntities.Clear();

		// wipe the entire cl structure
		Globals.cl = new();

		for (var i = 0; i < Globals.cl_entities.Length; i++)
			Globals.cl_entities[i] = new();

		SZ.Clear(Globals.cls.netchan.message);
	}

	/**
		 * Disconnect
		 * 
		 * Goes from a connected state to full screen console state Sends a
		 * disconnect message to the server This is also called on Com_Error, so it
		 * shouldn't cause any errors.
		 */
	private static void Disconnect()
	{
		string fin;

		if (Globals.cls.state == Defines.ca_disconnected)
			return;

		if (Globals.cl_timedemo != null && Globals.cl_timedemo.value != 0.0f)
		{
			int time;
			time = (int)(Timer.Milliseconds() - Globals.cl.timedemo_start);

			if (time > 0)
			{
				Com.Printf(
					$"{Globals.cl.timedemo_frames} frames, {time / 1000.0:0.0,3} seconds: {Globals.cl.timedemo_frames * 1000.0 / time:0.0,3} fps\n"
				);
			}
		}

		Math3D.VectorClear(Globals.cl.refdef.blend);
		Globals.re.CinematicSetPalette(null);
		Menu.ForceMenuOff();
		Globals.cls.connect_time = 0;
		SCR.StopCinematic();

		if (Globals.cls.demorecording)
			Cl.StopF();

		// send a disconnect message to the server
		fin = (char)Defines.clc_stringcmd + "disconnect";
		Netchan.Transmit(Globals.cls.netchan, fin.Length, Lib.stringToBytes(fin));
		Netchan.Transmit(Globals.cls.netchan, fin.Length, Lib.stringToBytes(fin));
		Netchan.Transmit(Globals.cls.netchan, fin.Length, Lib.stringToBytes(fin));
		Cl.ClearState();

		// stop download
		if (Globals.cls.download != null)
		{
			Globals.cls.download.Close();
			Globals.cls.download = null;
		}

		Globals.cls.state = Defines.ca_disconnected;
	}

	/**
		 * ParseStatusMessage
		 * 
		 * Handle a reply from a ping.
		 */
	private static void ParseStatusMessage()
	{
		var s = MSG.ReadString(Globals.net_message);
		Com.Printf(s + "\n");
		Menu.AddToServerList(Globals.net_from, s);
	}

	/**
		 * ConnectionlessPacket
		 * 
		 * Responses to broadcasts, etc
		 */
	private static void ConnectionlessPacket()
	{
		string s;
		string c;
		MSG.BeginReading(Globals.net_message);
		MSG.ReadLong(Globals.net_message); // skip the -1
		s = MSG.ReadStringLine(Globals.net_message);
		Cmd.TokenizeString(s.ToCharArray(), false);
		c = Cmd.Argv(0);
		Com.Println(Globals.net_from.toString() + ": " + c);

		// server connection
		if (c.Equals("client_connect"))
		{
			if (Globals.cls.state == Defines.ca_connected)
			{
				Com.Printf("Dup connect received.  Ignored.\n");

				return;
			}

			Netchan.Setup(Defines.NS_CLIENT, Globals.cls.netchan, Globals.net_from, Globals.cls.quakePort);
			MSG.WriteChar(Globals.cls.netchan.message, Defines.clc_stringcmd);
			MSG.WriteString(Globals.cls.netchan.message, "new");
			Globals.cls.state = Defines.ca_connected;

			return;
		}

		// server responding to a status broadcast
		if (c.Equals("info"))
		{
			Cl.ParseStatusMessage();

			return;
		}

		// remote command from gui front end
		if (c.Equals("cmd"))
		{
			if (!NET.IsLocalAddress(Globals.net_from))
			{
				Com.Printf("Command packet from remote host.  Ignored.\n");

				return;
			}

			s = MSG.ReadString(Globals.net_message);
			Cbuf.AddText(s);
			Cbuf.AddText("\n");

			return;
		}

		// print command from somewhere
		if (c.Equals("print"))
		{
			s = MSG.ReadString(Globals.net_message);

			if (s.Length > 0)
				Com.Printf(s);

			return;
		}

		// ping from somewhere
		if (c.Equals("ping"))
		{
			Netchan.OutOfBandPrint(Defines.NS_CLIENT, Globals.net_from, "ack");

			return;
		}

		// challenge from the server we are connecting to
		if (c.Equals("challenge"))
		{
			Globals.cls.challenge = Lib.atoi(Cmd.Argv(1));
			Cl.SendConnectPacket();

			return;
		}

		// echo request from server
		if (c.Equals("echo"))
		{
			Netchan.OutOfBandPrint(Defines.NS_CLIENT, Globals.net_from, Cmd.Argv(1));

			return;
		}

		Com.Printf("Unknown command.\n");
	}

	/**
		 * ReadPackets
		 */
	private static void ReadPackets()
	{
		while (NET.GetPacket(Defines.NS_CLIENT, Globals.net_from, Globals.net_message))
		{
			//
			// remote command packet
			//		
			if (Globals.net_message.data[0] == 255
			    && Globals.net_message.data[1] == 255
			    && Globals.net_message.data[2] == 255
			    && Globals.net_message.data[3] == 255)
			{
				//			if (*(int *)net_message.data == -1)
				Cl.ConnectionlessPacket();

				continue;
			}

			if (Globals.cls.state == Defines.ca_disconnected || Globals.cls.state == Defines.ca_connecting)
				continue; // dump it if not connected

			if (Globals.net_message.cursize < 8)
			{
				Com.Printf(NET.AdrToString(Globals.net_from) + ": Runt packet\n");

				continue;
			}

			//
			// packet from server
			//
			if (!NET.CompareAdr(Globals.net_from, Globals.cls.netchan.remote_address))
			{
				Com.DPrintf(NET.AdrToString(Globals.net_from) + ":sequenced packet without connection\n");

				continue;
			}

			if (!Netchan.Process(Globals.cls.netchan, Globals.net_message))
				continue; // wasn't accepted for some reason

			CL_parse.ParseServerMessage();
		}

		//
		// check timeout
		//
		if (Globals.cls.state >= Defines.ca_connected && Globals.cls.realtime - Globals.cls.netchan.last_received > Globals.cl_timeout.value * 1000)
		{
			if (++Globals.cl.timeoutcount > 5) // timeoutcount saves debugger
			{
				Com.Printf("\nServer connection timed out.\n");
				Cl.Disconnect();

				return;
			}
		}
		else
			Globals.cl.timeoutcount = 0;
	}

	//	  =============================================================================

	/**
		 * FixUpGender_f
		 */
	public static void FixUpGender()
	{
		string sk;

		if (Globals.gender_auto.value != 0.0f)
		{
			if (Globals.gender.modified)
			{
				// was set directly, don't override the user
				Globals.gender.modified = false;

				return;
			}

			sk = Globals.skin.@string;

			if (sk.StartsWith("male") || sk.StartsWith("cyborg"))
				Cvar.Set("gender", "male");
			else if (sk.StartsWith("female") || sk.StartsWith("crackhor"))
				Cvar.Set("gender", "female");
			else
				Cvar.Set("gender", "none");

			Globals.gender.modified = false;
		}
	}

	public static void RequestNextDownload()
	{
		var mapChecksum = 0; // for detecting cheater maps

		//char fn[MAX_OSPATH];
		string fn;
		dmdl_t pheader;

		if (Globals.cls.state != Defines.ca_connected)
			return;

		if (SV_MAIN.allow_download.value == 0 && Cl.precacheCheck < Cl.EnvCnt)
			Cl.precacheCheck = Cl.EnvCnt;

		//	  ZOID
		if (Cl.precacheCheck == Defines.CS_MODELS)
		{
			// confirm map
			Cl.precacheCheck = Defines.CS_MODELS + 2; // 0 isn't used

			if (SV_MAIN.allow_download_maps.value != 0)
			{
				if (!CL_parse.CheckOrDownloadFile(Globals.cl.configstrings[Defines.CS_MODELS + 1]))
					return; // started a download
			}
		}

		if (Cl.precacheCheck >= Defines.CS_MODELS && Cl.precacheCheck < Defines.CS_MODELS + Defines.MAX_MODELS)
		{
			if (SV_MAIN.allow_download_models.value != 0)
			{
				while (Cl.precacheCheck < Defines.CS_MODELS + Defines.MAX_MODELS && Globals.cl.configstrings[Cl.precacheCheck].Length > 0)
				{
					if (Globals.cl.configstrings[Cl.precacheCheck][0] == '*' || Globals.cl.configstrings[Cl.precacheCheck][0] == '#')
					{
						Cl.precacheCheck++;

						continue;
					}

					if (Cl.precacheModelSkin == 0)
					{
						if (!CL_parse.CheckOrDownloadFile(Globals.cl.configstrings[Cl.precacheCheck]))
						{
							Cl.precacheModelSkin = 1;

							return; // started a download
						}

						Cl.precacheModelSkin = 1;
					}

					// checking for skins in the model
					if (Cl.precacheModel == null)
					{
						Cl.precacheModel = FS.LoadFile(Globals.cl.configstrings[Cl.precacheCheck]);

						if (Cl.precacheModel == null)
						{
							Cl.precacheModelSkin = 0;
							Cl.precacheCheck++;

							continue; // couldn't load it
						}

						using var memoryStream = new MemoryStream(Cl.precacheModel);
						BinaryReader bb = new(memoryStream);
						var header = bb.ReadInt32();

						if (header != qfiles.IDALIASHEADER)
						{
							// not an alias model
							FS.FreeFile(Cl.precacheModel);
							Cl.precacheModel = null;
							Cl.precacheModelSkin = 0;
							Cl.precacheCheck++;

							continue;
						}

						pheader = new(bb);

						if (pheader.version != Defines.ALIAS_VERSION)
						{
							Cl.precacheCheck++;
							Cl.precacheModelSkin = 0;

							continue; // couldn't load it
						}
					}
					else
					{
						using var memoryStream = new MemoryStream(Cl.precacheModel);
						BinaryReader bb = new(memoryStream);
						pheader = new(bb);
					}

					var numSkins = pheader.num_skins;

					while (Cl.precacheModelSkin - 1 < numSkins)
					{
						//Com.Printf("critical code section because of endian
						// mess!\n");

						var name = Lib.CtoJava(
							Cl.precacheModel,
							pheader.ofs_skins + (Cl.precacheModelSkin - 1) * Defines.MAX_SKINNAME,
							Defines.MAX_SKINNAME * numSkins
						);

						if (!CL_parse.CheckOrDownloadFile(name))
						{
							Cl.precacheModelSkin++;

							return; // started a download
						}

						Cl.precacheModelSkin++;
					}

					if (Cl.precacheModel != null)
					{
						FS.FreeFile(Cl.precacheModel);
						Cl.precacheModel = null;
					}

					Cl.precacheModelSkin = 0;
					Cl.precacheCheck++;
				}
			}

			Cl.precacheCheck = Defines.CS_SOUNDS;
		}

		if (Cl.precacheCheck >= Defines.CS_SOUNDS && Cl.precacheCheck < Defines.CS_SOUNDS + Defines.MAX_SOUNDS)
		{
			if (SV_MAIN.allow_download_sounds.value != 0)
			{
				if (Cl.precacheCheck == Defines.CS_SOUNDS)
					Cl.precacheCheck++; // zero is blank

				while (Cl.precacheCheck < Defines.CS_SOUNDS + Defines.MAX_SOUNDS && Globals.cl.configstrings[Cl.precacheCheck].Length > 0)
				{
					if (Globals.cl.configstrings[Cl.precacheCheck][0] == '*')
					{
						Cl.precacheCheck++;

						continue;
					}

					fn = "sound/" + Globals.cl.configstrings[Cl.precacheCheck++];

					if (!CL_parse.CheckOrDownloadFile(fn))
						return; // started a download
				}
			}

			Cl.precacheCheck = Defines.CS_IMAGES;
		}

		if (Cl.precacheCheck >= Defines.CS_IMAGES && Cl.precacheCheck < Defines.CS_IMAGES + Defines.MAX_IMAGES)
		{
			if (Cl.precacheCheck == Defines.CS_IMAGES)
				Cl.precacheCheck++; // zero is blank

			while (Cl.precacheCheck < Defines.CS_IMAGES + Defines.MAX_IMAGES && Globals.cl.configstrings[Cl.precacheCheck].Length > 0)
			{
				fn = "pics/" + Globals.cl.configstrings[Cl.precacheCheck++] + ".pcx";

				if (!CL_parse.CheckOrDownloadFile(fn))
					return; // started a download
			}

			Cl.precacheCheck = Defines.CS_PLAYERSKINS;
		}

		// skins are special, since a player has three things to download:
		// model, weapon model and skin
		// so precache_check is now *3
		if (Cl.precacheCheck >= Defines.CS_PLAYERSKINS && Cl.precacheCheck < Defines.CS_PLAYERSKINS + Defines.MAX_CLIENTS * Cl.PlayerMult)
		{
			if (SV_MAIN.allow_download_players.value != 0)
			{
				while (Cl.precacheCheck < Defines.CS_PLAYERSKINS + Defines.MAX_CLIENTS * Cl.PlayerMult)
				{
					int i, n;

					//char model[MAX_QPATH], skin[MAX_QPATH], * p;
					string model, skin;
					i = (Cl.precacheCheck - Defines.CS_PLAYERSKINS) / Cl.PlayerMult;
					n = (Cl.precacheCheck - Defines.CS_PLAYERSKINS) % Cl.PlayerMult;

					if (Globals.cl.configstrings[Defines.CS_PLAYERSKINS + i].Length == 0)
					{
						Cl.precacheCheck = Defines.CS_PLAYERSKINS + (i + 1) * Cl.PlayerMult;

						continue;
					}

					var pos = Globals.cl.configstrings[Defines.CS_PLAYERSKINS + i].IndexOf('\\');

					if (pos != -1)
						pos++;
					else
						pos = 0;

					var pos2 = Globals.cl.configstrings[Defines.CS_PLAYERSKINS + i].IndexOf('\\', pos);

					if (pos2 == -1)
						pos2 = Globals.cl.configstrings[Defines.CS_PLAYERSKINS + i].IndexOf('/', pos);

					model = Globals.cl.configstrings[Defines.CS_PLAYERSKINS + i].Substring(pos, pos2 - pos);
					skin = Globals.cl.configstrings[Defines.CS_PLAYERSKINS + i][(pos2 + 1)..];

					if (n >= 0 && n <= 0) // model
					{
						fn = "players/" + model + "/tris.md2";

						if (!CL_parse.CheckOrDownloadFile(fn))
						{
							Cl.precacheCheck = Defines.CS_PLAYERSKINS + i * Cl.PlayerMult + 1;

							return; // started a download
						}

						n++;
						/* FALL THROUGH */
					}

					if (n >= 0 && n <= 1) // weapon model
					{
						fn = "players/" + model + "/weapon.md2";

						if (!CL_parse.CheckOrDownloadFile(fn))
						{
							Cl.precacheCheck = Defines.CS_PLAYERSKINS + i * Cl.PlayerMult + 2;

							return; // started a download
						}

						n++;
						/* FALL THROUGH */
					}

					if (n >= 0 && n <= 2) // weapon skin
					{
						fn = "players/" + model + "/weapon.pcx";

						if (!CL_parse.CheckOrDownloadFile(fn))
						{
							Cl.precacheCheck = Defines.CS_PLAYERSKINS + i * Cl.PlayerMult + 3;

							return; // started a download
						}

						n++;
						/* FALL THROUGH */
					}

					if (n >= 0 && n <= 3) // skin
					{
						fn = "players/" + model + "/" + skin + ".pcx";

						if (!CL_parse.CheckOrDownloadFile(fn))
						{
							Cl.precacheCheck = Defines.CS_PLAYERSKINS + i * Cl.PlayerMult + 4;

							return; // started a download
						}

						n++;
						/* FALL THROUGH */
					}

					if (n >= 0 && n <= 4) // skin_i
					{
						fn = "players/" + model + "/" + skin + "_i.pcx";

						if (!CL_parse.CheckOrDownloadFile(fn))
						{
							Cl.precacheCheck = Defines.CS_PLAYERSKINS + i * Cl.PlayerMult + 5;

							return; // started a download
						}

						// move on to next model
						Cl.precacheCheck = Defines.CS_PLAYERSKINS + (i + 1) * Cl.PlayerMult;
					}
				}
			}

			// precache phase completed
			Cl.precacheCheck = Cl.EnvCnt;
		}

		if (Cl.precacheCheck == Cl.EnvCnt)
		{
			Cl.precacheCheck = Cl.EnvCnt + 1;
			int[] iw = { mapChecksum };
			CM.CM_LoadMap(Globals.cl.configstrings[Defines.CS_MODELS + 1], true, iw);
			mapChecksum = iw[0];

			if ((mapChecksum ^ Lib.atoi(Globals.cl.configstrings[Defines.CS_MAPCHECKSUM])) != 0)
			{
				Com.Error(
					Defines.ERR_DROP,
					"Local map version differs from server: " + mapChecksum + " != '" + Globals.cl.configstrings[Defines.CS_MAPCHECKSUM] + "'\n"
				);

				return;
			}
		}

		if (Cl.precacheCheck > Cl.EnvCnt && Cl.precacheCheck < Cl.TextureCnt)
		{
			if (SV_MAIN.allow_download.value != 0 && SV_MAIN.allow_download_maps.value != 0)
			{
				while (Cl.precacheCheck < Cl.TextureCnt)
				{
					var n = Cl.precacheCheck++ - Cl.EnvCnt - 1;

					if ((n & 1) != 0)
						fn = "env/" + Globals.cl.configstrings[Defines.CS_SKY] + Cl.EnvSuf[n / 2] + ".pcx";
					else
						fn = "env/" + Globals.cl.configstrings[Defines.CS_SKY] + Cl.EnvSuf[n / 2] + ".tga";

					if (!CL_parse.CheckOrDownloadFile(fn))
						return; // started a download
				}
			}

			Cl.precacheCheck = Cl.TextureCnt;
		}

		if (Cl.precacheCheck == Cl.TextureCnt)
		{
			Cl.precacheCheck = Cl.TextureCnt + 1;
			Cl.precacheTex = 0;
		}

		// confirm existance of textures, download any that don't exist
		if (Cl.precacheCheck == Cl.TextureCnt + 1)
		{
			// from qcommon/cmodel.c
			// extern int numtexinfo;
			// extern mapsurface_t map_surfaces[];

			if (SV_MAIN.allow_download.value != 0 && SV_MAIN.allow_download_maps.value != 0)
			{
				while (Cl.precacheTex < CM.numtexinfo)
				{
					//char fn[MAX_OSPATH];
					fn = "textures/" + CM.map_surfaces[Cl.precacheTex++].rname + ".wal";

					if (!CL_parse.CheckOrDownloadFile(fn))
						return; // started a download
				}
			}

			Cl.precacheCheck = Cl.TextureCnt + 999;
		}

		//	  ZOID
		CL_parse.RegisterSounds();
		CL_view.PrepRefresh();
		MSG.WriteByte(Globals.cls.netchan.message, Defines.clc_stringcmd);
		MSG.WriteString(Globals.cls.netchan.message, "begin " + Cl.precacheSpawncount + "\n");
	}

	/**
		 * InitLocal
		 */
	private static void InitLocal()
	{
		Globals.cls.state = Defines.ca_disconnected;
		Globals.cls.realtime = Timer.Milliseconds();
		CL_input.InitInput();
		Cvar.Get("adr0", "", Defines.CVAR_ARCHIVE);
		Cvar.Get("adr1", "", Defines.CVAR_ARCHIVE);
		Cvar.Get("adr2", "", Defines.CVAR_ARCHIVE);
		Cvar.Get("adr3", "", Defines.CVAR_ARCHIVE);
		Cvar.Get("adr4", "", Defines.CVAR_ARCHIVE);
		Cvar.Get("adr5", "", Defines.CVAR_ARCHIVE);
		Cvar.Get("adr6", "", Defines.CVAR_ARCHIVE);
		Cvar.Get("adr7", "", Defines.CVAR_ARCHIVE);
		Cvar.Get("adr8", "", Defines.CVAR_ARCHIVE);

		//
		// register our variables
		//
		Globals.cl_stereo_separation = Cvar.Get("cl_stereo_separation", "0.4", Defines.CVAR_ARCHIVE);
		Globals.cl_stereo = Cvar.Get("cl_stereo", "0", 0);
		Globals.cl_add_blend = Cvar.Get("cl_blend", "1", 0);
		Globals.cl_add_lights = Cvar.Get("cl_lights", "1", 0);
		Globals.cl_add_particles = Cvar.Get("cl_particles", "1", 0);
		Globals.cl_add_entities = Cvar.Get("cl_entities", "1", 0);
		Globals.cl_gun = Cvar.Get("cl_gun", "1", 0);
		Globals.cl_footsteps = Cvar.Get("cl_footsteps", "1", 0);
		Globals.cl_noskins = Cvar.Get("cl_noskins", "0", 0);
		Globals.cl_autoskins = Cvar.Get("cl_autoskins", "0", 0);
		Globals.cl_predict = Cvar.Get("cl_predict", "1", 0);
		Globals.cl_maxfps = Cvar.Get("cl_maxfps", "90", 0);
		Globals.cl_upspeed = Cvar.Get("cl_upspeed", "200", 0);
		Globals.cl_forwardspeed = Cvar.Get("cl_forwardspeed", "200", 0);
		Globals.cl_sidespeed = Cvar.Get("cl_sidespeed", "200", 0);
		Globals.cl_yawspeed = Cvar.Get("cl_yawspeed", "140", 0);
		Globals.cl_pitchspeed = Cvar.Get("cl_pitchspeed", "150", 0);
		Globals.cl_anglespeedkey = Cvar.Get("cl_anglespeedkey", "1.5", 0);
		Globals.cl_run = Cvar.Get("cl_run", "1", Defines.CVAR_ARCHIVE);
		Globals.lookspring = Cvar.Get("lookspring", "0", Defines.CVAR_ARCHIVE);
		Globals.lookstrafe = Cvar.Get("lookstrafe", "0", Defines.CVAR_ARCHIVE);
		Globals.sensitivity = Cvar.Get("sensitivity", "3", Defines.CVAR_ARCHIVE);
		Globals.m_pitch = Cvar.Get("m_pitch", "0.022", Defines.CVAR_ARCHIVE);
		Globals.m_yaw = Cvar.Get("m_yaw", "0.022", 0);
		Globals.m_forward = Cvar.Get("m_forward", "1", 0);
		Globals.m_side = Cvar.Get("m_side", "1", 0);
		Globals.cl_shownet = Cvar.Get("cl_shownet", "0", 0);
		Globals.cl_showmiss = Cvar.Get("cl_showmiss", "0", 0);
		Globals.cl_showclamp = Cvar.Get("showclamp", "0", 0);
		Globals.cl_timeout = Cvar.Get("cl_timeout", "120", 0);
		Globals.cl_paused = Cvar.Get("paused", "0", 0);
		Globals.cl_timedemo = Cvar.Get("timedemo", "0", 0);
		Globals.rcon_client_password = Cvar.Get("rcon_password", "", 0);
		Globals.rcon_address = Cvar.Get("rcon_address", "", 0);
		Globals.cl_lightlevel = Cvar.Get("r_lightlevel", "0", 0);

		//
		// userinfo
		//
		Globals.info_password = Cvar.Get("password", "", Defines.CVAR_USERINFO);
		Globals.info_spectator = Cvar.Get("spectator", "0", Defines.CVAR_USERINFO);
		Globals.name = Cvar.Get("name", "unnamed", Defines.CVAR_USERINFO | Defines.CVAR_ARCHIVE);
		Globals.skin = Cvar.Get("skin", "male/grunt", Defines.CVAR_USERINFO | Defines.CVAR_ARCHIVE);
		Globals.rate = Cvar.Get("rate", "25000", Defines.CVAR_USERINFO | Defines.CVAR_ARCHIVE); // FIXME
		Globals.msg = Cvar.Get("msg", "1", Defines.CVAR_USERINFO | Defines.CVAR_ARCHIVE);
		Globals.hand = Cvar.Get("hand", "0", Defines.CVAR_USERINFO | Defines.CVAR_ARCHIVE);
		Globals.fov = Cvar.Get("fov", "90", Defines.CVAR_USERINFO | Defines.CVAR_ARCHIVE);
		Globals.gender = Cvar.Get("gender", "male", Defines.CVAR_USERINFO | Defines.CVAR_ARCHIVE);
		Globals.gender_auto = Cvar.Get("gender_auto", "1", Defines.CVAR_ARCHIVE);
		Globals.gender.modified = false; // clear this so we know when user sets

		// it manually
		Globals.cl_vwep = Cvar.Get("cl_vwep", "1", Defines.CVAR_ARCHIVE);

		//
		// register our commands
		//
		Cmd.AddCommand("cmd", Cl.ForwardToServerF);
		Cmd.AddCommand("pause", Cl.PauseF);
		Cmd.AddCommand("pingservers", Cl.PingServersF);
		Cmd.AddCommand("skins", Cl.SkinsF);
		Cmd.AddCommand("userinfo", Cl.UserinfoF);
		Cmd.AddCommand("snd_restart", Cl.SndRestartF);
		Cmd.AddCommand("changing", Cl.ChangingF);
		Cmd.AddCommand("disconnect", Cl.DisconnectF);
		Cmd.AddCommand("record", Cl.RecordF);
		Cmd.AddCommand("stop", Cl.StopF);
		Cmd.AddCommand("quit", Cl.QuitF);
		Cmd.AddCommand("connect", Cl.ConnectF);
		Cmd.AddCommand("reconnect", Cl.ReconnectF);
		Cmd.AddCommand("rcon", Cl.RconF);
		Cmd.AddCommand("precache", Cl.PrecacheF);
		Cmd.AddCommand("download", CL_parse.Download_f);

		//
		// forward to server commands
		//
		// the only thing this does is allow command completion
		// to work -- all unknown commands are automatically
		// forwarded to the server
		Cmd.AddCommand("wave", null);
		Cmd.AddCommand("inven", null);
		Cmd.AddCommand("kill", null);
		Cmd.AddCommand("use", null);
		Cmd.AddCommand("drop", null);
		Cmd.AddCommand("say", null);
		Cmd.AddCommand("say_team", null);
		Cmd.AddCommand("info", null);
		Cmd.AddCommand("prog", null);
		Cmd.AddCommand("give", null);
		Cmd.AddCommand("god", null);
		Cmd.AddCommand("notarget", null);
		Cmd.AddCommand("noclip", null);
		Cmd.AddCommand("invuse", null);
		Cmd.AddCommand("invprev", null);
		Cmd.AddCommand("invnext", null);
		Cmd.AddCommand("invdrop", null);
		Cmd.AddCommand("weapnext", null);
		Cmd.AddCommand("weapprev", null);
	}

	/**
		 * WriteConfiguration
		 * 
		 * Writes key bindings and archived cvars to config.cfg.
		 */
	public static void WriteConfiguration()
	{
		//        if (Globals.cls.state == Defines.ca_uninitialized)
		//            return;
		var path = FS.Gamedir() + "/config.cfg";

		try
		{
			var f = File.CreateText(path);
			f.Write("// generated by quake, do not modify\n");
			Key.WriteBindings(f);
			f.Close();
		}
		catch (Exception)
		{
			Com.Printf("Couldn't write config.cfg.\n");

			return;
		}

		Cvar.WriteVariables(path);
	}

	/**
		 * FixCvarCheats
		 */
	public static void FixCvarCheats()
	{
		int i;
		cheatvar_t var;

		if ("1".Equals(Globals.cl.configstrings[Defines.CS_MAXCLIENTS]) || 0 == Globals.cl.configstrings[Defines.CS_MAXCLIENTS].Length)
			return; // single player can cheat

		// find all the cvars if we haven't done it yet
		if (0 == Cl.numcheatvars)
		{
			while (Cl.Cheatvars[Cl.numcheatvars].name != null)
			{
				Cl.Cheatvars[Cl.numcheatvars].var = Cvar.Get(Cl.Cheatvars[Cl.numcheatvars].name, Cl.Cheatvars[Cl.numcheatvars].value, 0);
				Cl.numcheatvars++;
			}
		}

		// make sure they are all set to the proper values
		for (i = 0; i < Cl.numcheatvars; i++)
		{
			var = Cl.Cheatvars[i];

			if (!var.var.@string.Equals(var.value))
				Cvar.Set(var.name, var.value);
		}
	}

	//	  =============================================================

	/**
		 * SendCommand
		 */
	private static void SendCommand()
	{
		// get new key events
		Sys.SendKeyEvents();

		// process console commands
		Cbuf.Execute();

		// fix any cheating cvars
		Cl.FixCvarCheats();

		// send intentions now
		CL_input.SendCmd();

		// resend a connection request if necessary
		Cl.CheckForResend();
	}

	//	private static int lasttimecalled;

	/**
		 * Frame
		 */
	public static void Frame(int msec)
	{
		if (Globals.dedicated.value != 0)
			return;

		Cl.extratime += msec;

		if (Globals.cl_timedemo.value == 0.0f)
		{
			if (Globals.cls.state == Defines.ca_connected && Cl.extratime < 100)
				return; // don't flood packets out while connecting

			if (Cl.extratime < 1000 / Globals.cl_maxfps.value)
				return; // framerate is too high
		}

		// let the mouse activate or deactivate
		IN.Frame();

		// decide the simulation time
		Globals.cls.frametime = Cl.extratime / 1000.0f;
		Globals.cl.time += Cl.extratime;
		Globals.cls.realtime = Globals.curtime;
		Cl.extratime = 0;

		if (Globals.cls.frametime > 1.0f / 5)
			Globals.cls.frametime = 1.0f / 5;

		// if in the debugger last frame, don't timeout
		if (msec > 5000)
			Globals.cls.netchan.last_received = Timer.Milliseconds();

		// fetch results from server
		Cl.ReadPackets();

		// send a new command message to the server
		Cl.SendCommand();

		// predict all unacknowledged movements
		CL_pred.PredictMovement();

		// allow rendering DLL change
		VID.CheckChanges();

		if (!Globals.cl.refresh_prepped && Globals.cls.state == Defines.ca_active)
		{
			CL_view.PrepRefresh();

			// force GC after level loading
			// but not on playing a cinematic
			if (Globals.cl.cinematictime == 0)
				GC.Collect();
		}

		SCR.UpdateScreen();

		// update audio
		S.Update(Globals.cl.refdef.vieworg, Globals.cl.v_forward, Globals.cl.v_right, Globals.cl.v_up);

		// advance local effects for next frame
		CL_fx.RunDLights();
		CL_fx.RunLightStyles();
		SCR.RunCinematic();
		SCR.RunConsole();
		Globals.cls.framecount++;
	}

	/**
		 * Shutdown
		 */
	public static void Shutdown()
	{
		if (Cl.isdown)
		{
			Console.Write("recursive shutdown\n");

			return;
		}

		Cl.isdown = true;
		Cl.WriteConfiguration();
		S.Shutdown();
		IN.Shutdown();
		VID.Shutdown();
	}

	/**
		 * Initialize client subsystem.
		 */
	public static void Init()
	{
		if (Globals.dedicated.value != 0.0f)
			return; // nothing running on the client

		// all archived variables will now be loaded
		QConsole.Init(); //ok
		S.Init(); //empty
		VID.Init();
		V.Init();
		Globals.net_message.data = Globals.net_message_buffer;
		Globals.net_message.maxsize = Globals.net_message_buffer.Length;
		Menu.Init();
		SCR.Init();

		//Globals.cls.disable_screen = 1.0f; // don't draw yet
		Cl.InitLocal();
		IN.Init();
		FS.ExecAutoexec();
		Cbuf.Execute();
	}

	/**
		 * Called after an ERR_DROP was thrown.
		 */
	public static void Drop()
	{
		if (Globals.cls.state == Defines.ca_uninitialized)
			return;

		if (Globals.cls.state == Defines.ca_disconnected)
			return;

		Cl.Disconnect();

		// drop loading plaque unless this is the initial game start
		if (Globals.cls.disable_servercount != -1)
			SCR.EndLoadingPlaque(); // get rid of loading plaque
	}
}