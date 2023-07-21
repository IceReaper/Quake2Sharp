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
namespace Quake2Sharp.client;

using game;
using game.types;
using qcommon;
using render.types;
using sound;
using sys;
using types;

/**
 * CL_parse
 */
public class CL_parse
{
	//// cl_parse.c -- parse a message received from the server

	public static string[] svc_strings =
	{
		"svc_bad",
		"svc_muzzleflash",
		"svc_muzzlflash2",
		"svc_temp_entity",
		"svc_layout",
		"svc_inventory",
		"svc_nop",
		"svc_disconnect",
		"svc_reconnect",
		"svc_sound",
		"svc_print",
		"svc_stufftext",
		"svc_serverdata",
		"svc_configstring",
		"svc_spawnbaseline",
		"svc_centerprint",
		"svc_download",
		"svc_playerinfo",
		"svc_packetentities",
		"svc_deltapacketentities",
		"svc_frame"
	};

	//	  =============================================================================

	public static string DownloadFileName(string fn)
	{
		return FS.Gamedir() + "/" + fn;
	}

	/**
     * CL_CheckOrDownloadFile returns true if the file exists, 
     * otherwise it attempts to start a
     * download from the server.
     */
	public static bool CheckOrDownloadFile(string filename)
	{
		if (filename.IndexOf("..") != -1)
		{
			Com.Printf("Refusing to download a path with ..\n");

			return true;
		}

		if (FS.FileLength(filename) > 0)
		{
			// it exists, no need to download
			return true;
		}

		Globals.cls.downloadname = filename;

		// download to a temp name, and only rename
		// to the real name when done, so if interrupted
		// a runt file wont be left
		Globals.cls.downloadtempname = Com.StripExtension(Globals.cls.downloadname);
		Globals.cls.downloadtempname += ".tmp";

		//	  ZOID
		// check to see if we already have a tmp for this file, if so, try to
		// resume
		// open the file if not opened yet
		var name = CL_parse.DownloadFileName(Globals.cls.downloadtempname);
		Stream fp = File.OpenWrite(name);

		if (fp != null)
		{
			// it exists
			long len = 0;

			try
			{
				len = fp.Length;
			}
			catch (IOException)
			{
			}

			Globals.cls.download = fp;

			// give the server an offset to start the download
			Com.Printf("Resuming " + Globals.cls.downloadname + "\n");
			MSG.WriteByte(Globals.cls.netchan.message, Defines.clc_stringcmd);
			MSG.WriteString(Globals.cls.netchan.message, "download " + Globals.cls.downloadname + " " + len);
		}
		else
		{
			Com.Printf("Downloading " + Globals.cls.downloadname + "\n");
			MSG.WriteByte(Globals.cls.netchan.message, Defines.clc_stringcmd);
			MSG.WriteString(Globals.cls.netchan.message, "download " + Globals.cls.downloadname);
		}

		Globals.cls.downloadnumber++;

		return false;
	}

	/*
	 * =============== CL_Download_f
	 * 
	 * Request a download from the server ===============
	 */
	public static Action Download_f = () =>
	{
		string filename;

		if (Cmd.Argc() != 2)
		{
			Com.Printf("Usage: download <filename>\n");

			return;
		}

		filename = Cmd.Argv(1);

		if (filename.IndexOf("..") != -1)
		{
			Com.Printf("Refusing to download a path with ..\n");

			return;
		}

		if (FS.LoadFile(filename) != null)
		{
			// it exists, no need to
			// download
			Com.Printf("File already exists.\n");

			return;
		}

		Globals.cls.downloadname = filename;
		Com.Printf("Downloading " + Globals.cls.downloadname + "\n");

		// download to a temp name, and only rename
		// to the real name when done, so if interrupted
		// a runt file wont be left
		Globals.cls.downloadtempname = Com.StripExtension(Globals.cls.downloadname);
		Globals.cls.downloadtempname += ".tmp";
		MSG.WriteByte(Globals.cls.netchan.message, Defines.clc_stringcmd);
		MSG.WriteString(Globals.cls.netchan.message, "download " + Globals.cls.downloadname);
		Globals.cls.downloadnumber++;
	};

	/*
	 * ====================== CL_RegisterSounds ======================
	 */
	public static void RegisterSounds()
	{
		S.BeginRegistration();
		TemporaryEntities.RegisterSounds();

		for (var i = 1; i < Defines.MAX_SOUNDS; i++)
		{
			if (Globals.cl.configstrings[Defines.CS_SOUNDS + i] == null
			    || Globals.cl.configstrings[Defines.CS_SOUNDS + i].Equals("")
			    || Globals.cl.configstrings[Defines.CS_SOUNDS + i].Equals("\0"))
				break;

			Globals.cl.sound_precache[i] = S.RegisterSound(Globals.cl.configstrings[Defines.CS_SOUNDS + i]);
			Sys.SendKeyEvents(); // pump message loop
		}

		S.EndRegistration();
	}

	/*
	 * ===================== CL_ParseDownload
	 * 
	 * A download message has been received from the server
	 * =====================
	 */
	public static void ParseDownload()
	{
		// read the data
		int size = MSG.ReadShort(Globals.net_message);
		var percent = MSG.ReadByte(Globals.net_message);

		if (size == -1)
		{
			Com.Printf("Server does not have this file.\n");

			if (Globals.cls.download != null)
			{
				// if here, we tried to resume a file but the server said no
				try
				{
					Globals.cls.download.Close();
				}
				catch (IOException)
				{
				}

				Globals.cls.download = null;
			}

			Cl.RequestNextDownload();

			return;
		}

		// open the file if not opened yet
		if (Globals.cls.download == null)
		{
			var name = CL_parse.DownloadFileName(Globals.cls.downloadtempname).ToLower();
			FS.CreatePath(name);
			Globals.cls.download = File.OpenWrite(name);

			if (Globals.cls.download == null)
			{
				Globals.net_message.readcount += size;
				Com.Printf("Failed to open " + Globals.cls.downloadtempname + "\n");
				Cl.RequestNextDownload();

				return;
			}
		}

		try
		{
			Globals.cls.download.Write(Globals.net_message.data, Globals.net_message.readcount, size);
		}
		catch (Exception)
		{
		}

		Globals.net_message.readcount += size;

		if (percent != 100)
		{
			// request next block
			//	   change display routines by zoid
			Globals.cls.downloadpercent = percent;
			MSG.WriteByte(Globals.cls.netchan.message, Defines.clc_stringcmd);
			SZ.Print(Globals.cls.netchan.message, "nextdl");
		}
		else
		{
			try
			{
				Globals.cls.download.Close();
			}
			catch (IOException)
			{
			}

			// rename the temp file to it's final name
			var oldn = CL_parse.DownloadFileName(Globals.cls.downloadtempname);
			var newn = CL_parse.DownloadFileName(Globals.cls.downloadname);
			File.Move(oldn, newn);
			Globals.cls.download = null;
			Globals.cls.downloadpercent = 0;

			// get another file if needed
			Cl.RequestNextDownload();
		}
	}

	/*
	 * =====================================================================
	 * 
	 * SERVER CONNECTING MESSAGES
	 * 
	 * =====================================================================
	 */

	/*
	 * ================== CL_ParseServerData ==================
	 */
	//checked once, was ok.
	public static void ParseServerData()
	{
		Com.DPrintf("ParseServerData():Serverdata packet received.\n");

		//
		//	   wipe the client_state_t struct
		//
		Cl.ClearState();
		Globals.cls.state = Defines.ca_connected;

		//	   parse protocol version number
		var i = MSG.ReadLong(Globals.net_message);
		Globals.cls.serverProtocol = i;

		// BIG HACK to let demos from release work with the 3.0x patch!!!
		if (Globals.server_state != 0 && Defines.PROTOCOL_VERSION == 34)
		{
		}
		else if (i != Defines.PROTOCOL_VERSION)
			Com.Error(Defines.ERR_DROP, "Server returned version " + i + ", not " + Defines.PROTOCOL_VERSION);

		Globals.cl.servercount = MSG.ReadLong(Globals.net_message);
		Globals.cl.attractloop = MSG.ReadByte(Globals.net_message) != 0;

		// game directory
		var str = MSG.ReadString(Globals.net_message);
		Globals.cl.gamedir = str;
		Com.dprintln("gamedir=" + str);

		// set gamedir
		if ((str.Length > 0 && (FS.fs_gamedirvar.@string == null || FS.fs_gamedirvar.@string.Length == 0 || FS.fs_gamedirvar.@string.Equals(str)))
		    || (str.Length == 0 && (FS.fs_gamedirvar.@string != null || FS.fs_gamedirvar.@string.Length == 0)))
			Cvar.Set("game", str);

		// parse player entity number
		Globals.cl.playernum = MSG.ReadShort(Globals.net_message);
		Com.dprintln("numplayers=" + Globals.cl.playernum);

		// get the full level name
		str = MSG.ReadString(Globals.net_message);
		Com.dprintln("levelname=" + str);

		if (Globals.cl.playernum == -1)
		{
			// playing a cinematic or showing a
			// pic, not a level
			SCR.PlayCinematic(str);
		}
		else
		{
			// seperate the printfs so the server message can have a color
			//			Com.Printf(
			//				"\n\n\35\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\36\37\n\n");
			//			Com.Printf('\02' + str + "\n");
			Com.Printf("Levelname:" + str + "\n");

			// need to prep refresh at next oportunity
			Globals.cl.refresh_prepped = false;
		}
	}

	/*
	 * ================== CL_ParseBaseline ==================
	 */
	public static void ParseBaseline()
	{
		entity_state_t nullstate = new(null);

		//memset(nullstate, 0, sizeof(nullstate));
		int[] bits = { 0 };
		var newnum = CL_ents.ParseEntityBits(bits);
		var es = Globals.cl_entities[newnum].baseline;
		CL_ents.ParseDelta(nullstate, es, newnum, bits[0]);
	}

	/*
	 * ================ CL_LoadClientinfo
	 * 
	 * ================
	 */
	public static void LoadClientinfo(clientinfo_t ci, string s)
	{
		//char model_name[MAX_QPATH];
		//char skin_name[MAX_QPATH];
		//char model_filename[MAX_QPATH];
		//char skin_filename[MAX_QPATH];
		//char weapon_filename[MAX_QPATH];
		string model_name, skin_name, model_filename, skin_filename, weapon_filename;
		ci.cinfo = s;

		//ci.cinfo[sizeof(ci.cinfo) - 1] = 0;

		// isolate the player's name
		ci.name = s;

		//ci.name[sizeof(ci.name) - 1] = 0;
		var t = s.IndexOf('\\');

		//t = strstr(s, "\\");

		if (t != -1)
		{
			ci.name = s[..t];
			s = s[(t + 1)..];

			//s = t + 1;
		}

		if (Globals.cl_noskins.value != 0 || s.Length == 0)
		{
			model_filename = "players/male/tris.md2";
			weapon_filename = "players/male/weapon.md2";
			skin_filename = "players/male/grunt.pcx";
			ci.iconname = "/players/male/grunt_i.pcx";
			ci.model = Globals.re.RegisterModel(model_filename);
			ci.weaponmodel = new model_t[Defines.MAX_CLIENTWEAPONMODELS];
			ci.weaponmodel[0] = Globals.re.RegisterModel(weapon_filename);
			ci.skin = Globals.re.RegisterSkin(skin_filename);
			ci.icon = Globals.re.RegisterPic(ci.iconname);
		}
		else
		{
			// isolate the model name
			var pos = s.IndexOf('/');

			if (pos == -1)
				pos = s.IndexOf('/');

			if (pos == -1)
			{
				pos = 0;
				Com.Error(Defines.ERR_FATAL, "Invalid model name:" + s);
			}

			model_name = s[..pos];

			// isolate the skin name
			skin_name = s[(pos + 1)..];

			// model file
			model_filename = "players/" + model_name + "/tris.md2";
			ci.model = Globals.re.RegisterModel(model_filename);

			if (ci.model == null)
			{
				model_name = "male";
				model_filename = "players/male/tris.md2";
				ci.model = Globals.re.RegisterModel(model_filename);
			}

			// skin file
			skin_filename = "players/" + model_name + "/" + skin_name + ".pcx";
			ci.skin = Globals.re.RegisterSkin(skin_filename);

			// if we don't have the skin and the model wasn't male,
			// see if the male has it (this is for CTF's skins)
			if (ci.skin == null && !model_name.Equals("male", StringComparison.OrdinalIgnoreCase))
			{
				// change model to male
				model_name = "male";
				model_filename = "players/male/tris.md2";
				ci.model = Globals.re.RegisterModel(model_filename);

				// see if the skin exists for the male model
				skin_filename = "players/" + model_name + "/" + skin_name + ".pcx";
				ci.skin = Globals.re.RegisterSkin(skin_filename);
			}

			// if we still don't have a skin, it means that the male model
			// didn't have
			// it, so default to grunt
			if (ci.skin == null)
			{
				// see if the skin exists for the male model
				skin_filename = "players/" + model_name + "/grunt.pcx";
				ci.skin = Globals.re.RegisterSkin(skin_filename);
			}

			// weapon file
			for (var i = 0; i < CL_view.num_cl_weaponmodels; i++)
			{
				weapon_filename = "players/" + model_name + "/" + CL_view.cl_weaponmodels[i];
				ci.weaponmodel[i] = Globals.re.RegisterModel(weapon_filename);

				if (null == ci.weaponmodel[i] && model_name.Equals("cyborg"))
				{
					// try male
					weapon_filename = "players/male/" + CL_view.cl_weaponmodels[i];
					ci.weaponmodel[i] = Globals.re.RegisterModel(weapon_filename);
				}

				if (0 == Globals.cl_vwep.value)
					break; // only one when vwep is off
			}

			// icon file
			ci.iconname = "/players/" + model_name + "/" + skin_name + "_i.pcx";
			ci.icon = Globals.re.RegisterPic(ci.iconname);
		}

		// must have loaded all data types to be valud
		if (ci.skin == null || ci.icon == null || ci.model == null || ci.weaponmodel[0] == null)
		{
			ci.skin = null;
			ci.icon = null;
			ci.model = null;
			ci.weaponmodel[0] = null;

			return;
		}
	}

	/*
	 * ================ CL_ParseClientinfo
	 * 
	 * Load the skin, icon, and model for a client ================
	 */
	public static void ParseClientinfo(int player)
	{
		var s = Globals.cl.configstrings[player + Defines.CS_PLAYERSKINS];
		var ci = Globals.cl.clientinfo[player];
		CL_parse.LoadClientinfo(ci, s);
	}

	/*
	 * ================ CL_ParseConfigString ================
	 */
	public static void ParseConfigString()
	{
		int i = MSG.ReadShort(Globals.net_message);

		if (i < 0 || i >= Defines.MAX_CONFIGSTRINGS)
			Com.Error(Defines.ERR_DROP, "configstring > MAX_CONFIGSTRINGS");

		var s = MSG.ReadString(Globals.net_message);
		var olds = Globals.cl.configstrings[i];
		Globals.cl.configstrings[i] = s;

		//Com.dprintln("ParseConfigString(): configstring[" + i + "]=<"+s+">");

		// do something apropriate

		if (i >= Defines.CS_LIGHTS && i < Defines.CS_LIGHTS + Defines.MAX_LIGHTSTYLES)
			CL_fx.SetLightstyle(i - Defines.CS_LIGHTS);
		else if (i >= Defines.CS_MODELS && i < Defines.CS_MODELS + Defines.MAX_MODELS)
		{
			if (Globals.cl.refresh_prepped)
			{
				Globals.cl.model_draw[i - Defines.CS_MODELS] = Globals.re.RegisterModel(Globals.cl.configstrings[i]);

				if (Globals.cl.configstrings[i].StartsWith("*"))
					Globals.cl.model_clip[i - Defines.CS_MODELS] = CM.InlineModel(Globals.cl.configstrings[i]);
				else
					Globals.cl.model_clip[i - Defines.CS_MODELS] = null;
			}
		}
		else if (i >= Defines.CS_SOUNDS && i < Defines.CS_SOUNDS + Defines.MAX_MODELS)
		{
			if (Globals.cl.refresh_prepped)
				Globals.cl.sound_precache[i - Defines.CS_SOUNDS] = S.RegisterSound(Globals.cl.configstrings[i]);
		}
		else if (i >= Defines.CS_IMAGES && i < Defines.CS_IMAGES + Defines.MAX_MODELS)
		{
			if (Globals.cl.refresh_prepped)
				Globals.cl.image_precache[i - Defines.CS_IMAGES] = Globals.re.RegisterPic(Globals.cl.configstrings[i]);
		}
		else if (i >= Defines.CS_PLAYERSKINS && i < Defines.CS_PLAYERSKINS + Defines.MAX_CLIENTS)
		{
			if (Globals.cl.refresh_prepped && !olds.Equals(s))
				CL_parse.ParseClientinfo(i - Defines.CS_PLAYERSKINS);
		}
	}

	/*
	 * =====================================================================
	 * 
	 * ACTION MESSAGES
	 * 
	 * =====================================================================
	 */
	private static readonly float[] pos_v = { 0, 0, 0 };

	/*
	 * ================== CL_ParseStartSoundPacket ==================
	 */
	public static void ParseStartSoundPacket()
	{
		var flags = MSG.ReadByte(Globals.net_message);
		var sound_num = MSG.ReadByte(Globals.net_message);
		float volume;

		if ((flags & Defines.SND_VOLUME) != 0)
			volume = MSG.ReadByte(Globals.net_message) / 255.0f;
		else
			volume = Defines.DEFAULT_SOUND_PACKET_VOLUME;

		float attenuation;

		if ((flags & Defines.SND_ATTENUATION) != 0)
			attenuation = MSG.ReadByte(Globals.net_message) / 64.0f;
		else
			attenuation = Defines.DEFAULT_SOUND_PACKET_ATTENUATION;

		float ofs;

		if ((flags & Defines.SND_OFFSET) != 0)
			ofs = MSG.ReadByte(Globals.net_message) / 1000.0f;
		else
			ofs = 0;

		int channel;
		int ent;

		if ((flags & Defines.SND_ENT) != 0)
		{
			// entity reletive
			channel = MSG.ReadShort(Globals.net_message);
			ent = channel >> 3;

			if (ent > Defines.MAX_EDICTS)
				Com.Error(Defines.ERR_DROP, "CL_ParseStartSoundPacket: ent = " + ent);

			channel &= 7;
		}
		else
		{
			ent = 0;
			channel = 0;
		}

		float[] pos;

		if ((flags & Defines.SND_POS) != 0)
		{
			// positioned in space
			MSG.ReadPos(Globals.net_message, CL_parse.pos_v);

			// is ok. sound driver copies
			pos = CL_parse.pos_v;
		}
		else

			// use entity number
			pos = null;

		if (null == Globals.cl.sound_precache[sound_num])
			return;

		S.StartSound(pos, ent, channel, Globals.cl.sound_precache[sound_num], volume, attenuation, ofs);
	}

	public static void SHOWNET(string s)
	{
		if (Globals.cl_shownet.value >= 2)
			Com.Printf(Globals.net_message.readcount - 1 + ":" + s + "\n");
	}

	/*
	 * ===================== CL_ParseServerMessage =====================
	 */
	public static void ParseServerMessage()
	{
		//
		//	   if recording demos, copy the message out
		//
		//if (cl_shownet.value == 1)
		//Com.Printf(net_message.cursize + " ");
		//else if (cl_shownet.value >= 2)
		//Com.Printf("------------------\n");

		//
		//	   parse the message
		//
		while (true)
		{
			if (Globals.net_message.readcount > Globals.net_message.cursize)
			{
				Com.Error(Defines.ERR_FATAL, "CL_ParseServerMessage: Bad server message:");

				break;
			}

			var cmd = MSG.ReadByte(Globals.net_message);

			if (cmd == -1)
			{
				CL_parse.SHOWNET("END OF MESSAGE");

				break;
			}

			if (Globals.cl_shownet.value >= 2)
			{
				if (null == CL_parse.svc_strings[cmd])
					Com.Printf(Globals.net_message.readcount - 1 + ":BAD CMD " + cmd + "\n");
				else
					CL_parse.SHOWNET(CL_parse.svc_strings[cmd]);
			}

			// other commands
			switch (cmd)
			{
				default:
					Com.Error(Defines.ERR_DROP, "CL_ParseServerMessage: Illegible server message\n");

					break;

				case Defines.svc_nop:
					//				Com.Printf ("svc_nop\n");
					break;

				case Defines.svc_disconnect:
					Com.Error(Defines.ERR_DISCONNECT, "Server disconnected\n");

					break;

				case Defines.svc_reconnect:
					Com.Printf("Server disconnected, reconnecting\n");

					if (Globals.cls.download != null)
					{
						//ZOID, close download
						try
						{
							Globals.cls.download.Close();
						}
						catch (IOException)
						{
						}

						Globals.cls.download = null;
					}

					Globals.cls.state = Defines.ca_connecting;
					Globals.cls.connect_time = -99999; // CL_CheckForResend() will

					// fire immediately
					break;

				case Defines.svc_print:
					var i = MSG.ReadByte(Globals.net_message);

					if (i == Defines.PRINT_CHAT)
					{
						S.StartLocalSound("misc/talk.wav");
						Globals.con.ormask = 128;
					}

					Com.Printf(MSG.ReadString(Globals.net_message));
					Globals.con.ormask = 0;

					break;

				case Defines.svc_centerprint:
					SCR.CenterPrint(MSG.ReadString(Globals.net_message));

					break;

				case Defines.svc_stufftext:
					var s = MSG.ReadString(Globals.net_message);
					Com.DPrintf("stufftext: " + s + "\n");
					Cbuf.AddText(s);

					break;

				case Defines.svc_serverdata:
					Cbuf.Execute(); // make sure any stuffed commands are done
					CL_parse.ParseServerData();

					break;

				case Defines.svc_configstring:
					CL_parse.ParseConfigString();

					break;

				case Defines.svc_sound:
					CL_parse.ParseStartSoundPacket();

					break;

				case Defines.svc_spawnbaseline:
					CL_parse.ParseBaseline();

					break;

				case Defines.svc_temp_entity:
					TemporaryEntities.Parse();

					break;

				case Defines.svc_muzzleflash:
					CL_fx.ParseMuzzleFlash();

					break;

				case Defines.svc_muzzleflash2:
					CL_fx.ParseMuzzleFlash2();

					break;

				case Defines.svc_download:
					CL_parse.ParseDownload();

					break;

				case Defines.svc_frame:
					CL_ents.ParseFrame();

					break;

				case Defines.svc_inventory:
					CL_inv.ParseInventory();

					break;

				case Defines.svc_layout:
					Globals.cl.layout = MSG.ReadString(Globals.net_message);

					break;

				case Defines.svc_playerinfo:
				case Defines.svc_packetentities:
				case Defines.svc_deltapacketentities:
					Com.Error(Defines.ERR_DROP, "Out of place frame data");

					break;
			}
		}

		CL_view.AddNetgraph();

		//
		// we don't know if it is ok to save a demo message until
		// after we have parsed the frame
		//
		if (Globals.cls.demorecording && !Globals.cls.demowaiting)
			Cl.WriteDemoMessage();
	}
}