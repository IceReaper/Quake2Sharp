﻿/*
 * Copyright (C) 1997-2001 Id Software, Inc.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or (at
 * your option) any later version.
 *
 * This program is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 *
 * See the GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA
 * 02111-1307, USA.
 */

using Quake2Sharp;
using Quake2Sharp.client;
using Quake2Sharp.game.types;
using Quake2Sharp.qcommon;
using Quake2Sharp.render;
using Quake2Sharp.server;
using Quake2Sharp.sys;
using Timer = Quake2Sharp.sys.Timer;

namespace IdTech.common;

/*
 * =======================================================================
 *
 * Platform independent initialization, main loop and frame handling.
 *
 * =======================================================================
 */
public static class frame
{
	public static readonly cvar_t developer = cvar.Cvar_Get("developer", "0", 0);
	public static readonly cvar_t modder = cvar.Cvar_Get("modder", "0", 0);
	private static readonly cvar_t timescale = cvar.Cvar_Get("timescale", "1", 0);
	private static readonly cvar_t fixedtime = cvar.Cvar_Get("fixedtime", "0", 0);
	public static readonly cvar_t cl_maxfps = cvar.Cvar_Get("cl_maxfps", "-1", Defines.CVAR_ARCHIVE);
	public static readonly cvar_t dedicated = cvar.Cvar_Get("dedicated", Globals.DEDICATED_ONLY ? "1" : "0", Defines.CVAR_NOSET);

	public static Stream? log_stats_file;
	private static readonly cvar_t busywait = cvar.Cvar_Get("busywait", "1", Defines.CVAR_ARCHIVE);
	private static readonly cvar_t cl_async = cvar.Cvar_Get("cl_async", "1", Defines.CVAR_ARCHIVE);
	public static readonly cvar_t cl_timedemo = cvar.Cvar_Get("timedemo", "0", 0);
	private static readonly cvar_t vid_maxfps = cvar.Cvar_Get("vid_maxfps", "300", Defines.CVAR_ARCHIVE);
	public static readonly cvar_t host_speeds = cvar.Cvar_Get("host_speeds", "0", 0);
	public static readonly cvar_t log_stats = cvar.Cvar_Get("log_stats", "0", 0);
	private static readonly cvar_t showtrace = cvar.Cvar_Get("showtrace", "0", 0);

	/* host_speeds times */
	public static int time_before_game;
	public static int time_after_game;
	private static int time_before_ref;
	private static int time_after_ref;

	// Used in the network- and input pathes.
	public static int curtime;

	// Is the game portable?
	public static bool is_portable;

	// Game given by user
	public static string userGivenGame = string.Empty;

	// Game should quit next frame.
	// Hack for the signal handlers.
	private static bool quitnextframe;

	private static int oldtime = Timer.Sys_Milliseconds();

	// ----

	private static void Qcommon_Buildstring()
	{
		var versionString = $"Yamagi Quake II v{Globals.YQ2VERSION}";

		Sys.ConsoleOutput($"\n{versionString}\n");

		for (var i = 0; i < versionString.Length; ++i)
			Sys.ConsoleOutput("=");

		Sys.ConsoleOutput("\n");

		Sys.ConsoleOutput($"Platform: {Globals.YQ2OSTYPE}\n");
		Sys.ConsoleOutput($"Architecture: {Globals.YQ2ARCH}\n");
	}

	public static void Qcommon_Mainloop()
	{
		if (Globals.DEDICATED_ONLY)
			Thread.Sleep(TimeSpan.FromMilliseconds(1));

		// Save global time for network- und input code.
		curtime = Timer.Sys_Milliseconds();

		Qcommon_Frame((int)(curtime - oldtime));

		oldtime = curtime;
	}

	public static void Qcommon_ExecConfigs(bool gameStartUp)
	{
		cmdparser.Cbuf_AddText("exec default.cfg\n");
		cmdparser.Cbuf_AddText("exec config.cfg\n");
		cmdparser.Cbuf_AddText("exec autoexec.cfg\n");

		/* Process cmd arguments only startup. */
		if (gameStartUp)
			cmdparser.Cbuf_AddEarlyCommands(true);

		cmdparser.Cbuf_Execute();
	}

	static bool checkForHelp(string[] argv)
	{
		var helpArgs = new[] { "--help", "-h", "-help", "-?", "/?" };

		for (var i = 1; i < argv.Length; ++i)
		{
			var arg = argv[i];

			foreach (var helpArg in helpArgs)
			{
				if (arg != helpArg)
					continue;

				Sys.ConsoleOutput($"Yamagi Quake II v{Globals.YQ2VERSION}\n");
				Sys.ConsoleOutput("Most interesting commandline arguments:\n");
				Sys.ConsoleOutput("-h or --help: Show this help\n");
				Sys.ConsoleOutput("-cfgdir <path>\n");
				Sys.ConsoleOutput("  set the name of your config directory\n");
				Sys.ConsoleOutput("-datadir <path>\n");
				Sys.ConsoleOutput("  set path to your Quake2 game data (the directory baseq2/ is in)\n");
				Sys.ConsoleOutput("-portable\n");
				Sys.ConsoleOutput("  Write (savegames, configs, ...) in the binary directory\n");
				Sys.ConsoleOutput("+exec <config>\n");
				Sys.ConsoleOutput("  execute the given config (mainly relevant for dedicated servers)\n");
				Sys.ConsoleOutput("+set <cvarname> <value>\n");
				Sys.ConsoleOutput("  Set the given cvar to the given value, e.g. +set vid_fullscreen 0\n");

				Sys.ConsoleOutput("\nSome interesting cvars:\n");
				Sys.ConsoleOutput("+set game <gamename>\n");
				Sys.ConsoleOutput("  start the given addon/mod, e.g. +set game xatrix\n");

				if (!Globals.DEDICATED_ONLY)
				{
					Sys.ConsoleOutput("+set vid_fullscreen <0 or 1>\n");
					Sys.ConsoleOutput("  start game in windowed (0) or desktop fullscreen (1)\n");
					Sys.ConsoleOutput("  or classic fullscreen (2) mode\n");
					Sys.ConsoleOutput("+set r_mode <modenumber>\n");
					Sys.ConsoleOutput("  start game in resolution belonging to <modenumber>,\n");
					Sys.ConsoleOutput("  use -1 for custom resolutions:\n");
					Sys.ConsoleOutput("+set r_customwidth <size in pixels>\n");
					Sys.ConsoleOutput("+set r_customheight <size in pixels>\n");
					Sys.ConsoleOutput("  if r_mode is set to -1, these cvars allow you to specify the\n");
					Sys.ConsoleOutput("  width/height of your custom resolution\n");
					Sys.ConsoleOutput("+set vid_renderer <renderer>\n");
					Sys.ConsoleOutput("  Selects the render backend. Currently available:\n");
					Sys.ConsoleOutput("    'gl1'  (the OpenGL 1.x renderer),\n");
					Sys.ConsoleOutput("    'gl3'  (the OpenGL 3.2 renderer),\n");
					Sys.ConsoleOutput("    'soft' (the software renderer)\n");
				}

				Sys.ConsoleOutput("\nSee https://github.com/yquake2/yquake2/blob/master/doc/04_cvarlist.md\nfor some more cvars\n");

				return true;
			}
		}

		return false;
	}

	public static void Qcommon_Init(string[] argv)
	{
		try
		{
			if (checkForHelp(argv))
			{
				// ok, --help or similar commandline option was given
				// and info was printed, exit the game now
				Environment.Exit(0);
			}

			// Print the build and version string
			Qcommon_Buildstring();

			// Start early subsystems.
			argproc.COM_InitArgv(argv);
			cmdparser.Cbuf_Init();
			cmdparser.Cmd_Init();
			cvar.Cvar_Init();

			if (!Globals.DEDICATED_ONLY)
				Key.Init();

			/* we need to add the early commands twice, because
			   a basedir or cddir needs to be set before execing
			   config files, but we want other parms to override
			   the settings of the config files */
			cmdparser.Cbuf_AddEarlyCommands(false);
			cmdparser.Cbuf_Execute();

			// remember the initial game name that might have been set on commandline
			var gameCvar = cvar.Cvar_Get("game", string.Empty, Defines.CVAR_LATCH | Defines.CVAR_SERVERINFO);
			var game = string.Empty;

			if (gameCvar is { @string.Length: > 0 })
				game = gameCvar.@string;

			userGivenGame = game;

			// The filesystems needs to be initialized after the cvars.
			filesystem.FS_InitFilesystem();

			// Add and execute configuration files.
			Qcommon_ExecConfigs(true);

			// cvars
			cvar.Cvar_Get("version", $"{Globals.YQ2VERSION} {Globals.YQ2ARCH} {Globals.BUILD_DATE} {Globals.YQ2OSTYPE}",
				Defines.CVAR_SERVERINFO | Defines.CVAR_NOSET);

			// We can't use the clients "quit" command when running dedicated.
			if (dedicated.value != 0)
				cmdparser.Cmd_AddCommand("quit", clientserver.Com_Quit);

			// Start late subsystem.
			NET.Init();
			Netchan.Netchan_Init();
			SV_MAIN.SV_Init();

			if (!Globals.DEDICATED_ONLY)
				Cl.Init();

			// Everythings up, let's add + cmds from command line.
			if (!cmdparser.Cbuf_AddLateCommands())
			{
				// Start demo loop...
				if (dedicated.value == 0)
					cmdparser.Cbuf_AddText("d1\n");

				// ...or dedicated server.
				else
					cmdparser.Cbuf_AddText("dedicated_start\n");

				cmdparser.Cbuf_Execute();
			}
			/* the user asked for something explicit
			   so drop the loading plaque */
			else if (!Globals.DEDICATED_ONLY)
				SCR.EndLoadingPlaque();

			clientserver.Com_Printf("==== Quake II Initialized ====\n\n");
			clientserver.Com_Printf("******************************\n\n");
		}
		catch
		{
			// Jump point used in emergency situations.
			Sys.Error("Error during initialization");
		}

		// Call the main loop
		Qcommon_Mainloop();
	}

	// Time since last packetframe in millisec.
	private static int packetdelta = 1000;

	// Time since last renderframe in millisec.
	private static int renderdelta = 1000;

	// Accumulated time since last client run.
	private static int clienttimedelta;

	// Accumulated time since last server run.
	private static int servertimedelta;

	private static void Qcommon_Frame(int msec)
	{
		try
		{
			// Target packetframerate.
			float pfps;

			// Target renderframerate.
			float rfps;

			/* A packetframe runs the server and the client,
			   but not the renderer. The minimal interval of
			   packetframes is about 10.000 microsec. If run
			   more often the movement prediction in pmove.c
			   breaks. That's the Q2 variant if the famous
			   125hz bug. */
			var packetFrame = true;

			/* A rendererframe runs the renderer, but not the
			   client or the server. The minimal interval is
			   about 1000 microseconds. */
			var renderframe = true;

			/* Tells the client to shutdown.
			   Used by the signal handlers. */
			if (quitnextframe)
				cmdparser.Cbuf_AddText("quit");

			if (!Globals.DEDICATED_ONLY)
			{
				if (log_stats.modified)
				{
					log_stats.modified = false;
					log_stats_file?.Dispose();

					if (log_stats.value != 0)
					{
						log_stats_file = File.Open("stats.log", FileMode.Create, FileAccess.Write);
						log_stats_file.Write("entities,dlights,parts,frame time\n"u8);
					}
					else
						log_stats_file = null;
				}
			}

			// Timing debug crap. Just for historical reasons.
			if (fixedtime.value != 0)
				msec = (int)fixedtime.value;
			else if (timescale.value != 0)
				msec = (int)(msec * timescale.value);

			if (!Globals.DEDICATED_ONLY)
			{
				if (showtrace.value != 0)
				{
					clientserver.Com_Printf($"{Globals.c_traces:####} traces  {Globals.c_pointcontents:####} points\n");
					Globals.c_traces = 0;
					Globals.c_brush_traces = 0;
					Globals.c_pointcontents = 0;
				}

				/* We can render 1000 frames at maximum, because the minimum
				   frametime of the client is 1 millisecond. And of course we
				   need to render something, the framerate can never be less
				   then 1. Cap vid_maxfps between 1 and 999. */
				if (vid_maxfps.value is > 999 or < 1)
					cvar.Cvar_SetValue("vid_maxfps", 999);

				if (cl_maxfps.value > 250)
					cvar.Cvar_SetValue("cl_maxfps", 250);

				// Calculate target and renderframerate.
				if (Renderer.renderApi.R_IsVSyncActive())
				{
					var refreshRate = Renderer.renderApi.GL_GetRefreshRate();

					// using refreshRate - 2, because targeting a value slightly below the
					// (possibly not 100% correctly reported) refreshRate would introduce jittering, so only
					// use vid_maxfps if it looks like the user really means it to be different from refreshRate
					if (vid_maxfps.value < refreshRate - 2)
					{
						rfps = vid_maxfps.value;

						// we can't have more packet frames than render frames, so limit pfps to rfps
						pfps = cl_maxfps.value > rfps ? rfps : cl_maxfps.value;
					}

					// target refresh rate, not vid_maxfps
					else
					{
						/* if vsync is active, we increase the target framerate a bit for two reasons
						   1. On Windows, GLimp_GetFrefreshRate() (or the SDL counterpart, or even
						      the underlying WinAPI function) often returns a too small value,
						      like 58 or 59 when it's really 59.95 and thus (as integer) should be 60
						   2. vsync will throttle us to refreshrate anyway, so there is no harm
						      in starting the frame *a bit* earlier, instead of risking starting
						      it too late */
						rfps = refreshRate * 1.2f;

						// we can't have more packet frames than render frames, so limit pfps to rfps
						// but in this case use tolerance for comparison and assign rfps with tolerance
						pfps = cl_maxfps.value < refreshRate - 2 ? cl_maxfps.value : rfps;
					}
				}
				else
				{
					rfps = vid_maxfps.value;

					// we can't have more packet frames than render frames, so limit pfps to rfps
					pfps = cl_maxfps.value > rfps ? rfps : cl_maxfps.value;
				}

				// cl_maxfps <= 0 means: automatically choose a packet framerate that should work
				// well with the render framerate, which is the case if rfps is a multiple of pfps
				if (cl_maxfps.value <= 0 && cl_async.value != 0)
				{
					// packet framerates between about 45 and 90 should be ok,
					// with other values the game (esp. movement/clipping) can become glitchy
					// as pfps must be <= rfps, for rfps < 90 just use that as pfps
					if (rfps < 90.0f)
						pfps = rfps;
					/* we want an integer divider, so every div-th renderframe is a packetframe.
					   this formula gives nice dividers that keep pfps as close as possible
					   to 60 (which seems to be ideal):
					   - for < 150 rfps div will be 2, so pfps will be between 45 and ~75
					     => exactly every second renderframe we also run a packetframe
					   - for < 210 rfps div will be 3, so pfps will be between 50 and ~70
					     => exactly every third renderframe we also run a packetframe
					   - etc, the higher the rfps, the closer the pfps-range will be to 60
					     (and you probably get the very best results by running at a
					      render framerate that's a multiple of 60) */
					else
					{
						var div = MathF.Round(rfps / 60);
						pfps = rfps / div;
					}
				}

				// Calculate timings.
				packetdelta += msec;
				renderdelta += msec;
				clienttimedelta += msec;
				servertimedelta += msec;

				if (cl_timedemo.value != 0)
				{
					if (cl_async.value != 0)
					{
						// Render frames.
						if (renderdelta < 1000 / rfps)
							renderframe = false;

						// Network frames.
						var packettargetdelta = 1000 / pfps;

						// "packetdelta + renderdelta/2 >= packettargetdelta" if now we're
						// closer to when we want to run the next packetframe than we'd
						// (probably) be after the next render frame
						// also, we only run packetframes together with renderframes,
						// because we must have at least one render frame between two packet frames
						// TODO: does it make sense to use the average renderdelta of the last X frames
						//       instead of just the last renderdelta?
						if (!renderframe || packetdelta + renderdelta / 2f < packettargetdelta)
							packetFrame = false;
					}

					// Cap frames at target framerate.
					else if (renderdelta < 1000 / rfps)
					{
						renderframe = false;
						packetFrame = false;
					}
				}
			}
			else
			{
				// Target framerate.
				pfps = (int)cl_maxfps.value;

				// Calculate timings.
				packetdelta += msec;
				servertimedelta += msec;

				// Network frame time.
				if (packetdelta < 1000 / pfps)
					packetFrame = false;
			}

			// Dedicated server terminal console.
			while (true)
			{
				var input = Sys.Sys_ConsoleInput();

				if (input == null)
					break;

				cmdparser.Cbuf_AddText($"{input}\n");
			}

			cmdparser.Cbuf_Execute();

			var timeBefore = Timer.Sys_Milliseconds();

			// Run the serverframe.
			if (packetFrame)
			{
				SV_MAIN.SV_Frame(servertimedelta);
				servertimedelta = 0;
			}

			if (!Globals.DEDICATED_ONLY)
			{
				var timeBetween = Timer.Sys_Milliseconds();

				// Run the client frame.
				if (packetFrame || renderframe)
				{
					Cl.Frame(packetdelta, renderdelta, clienttimedelta, packetFrame, renderframe);
					clienttimedelta = 0;
				}

				// Statistics.
				if (host_speeds.value != 0)
				{
					var timeAfter = Timer.Sys_Milliseconds();
					var all = timeAfter - timeBefore;
					var sv = timeBetween - timeBefore;
					var cl = timeAfter - timeBetween;
					var gm = time_after_game - time_before_game;
					var rf = time_after_ref - time_before_ref;

					sv -= gm;
					cl -= rf;

					clientserver.Com_Printf($"all:{all:###} sv:{sv:###} gm:{gm:###} cl:{cl:###} rf:{rf:###}\n");
				}
			}

			// Reset deltas and mark frame.
			if (packetFrame)
				packetdelta = 0;

			if (renderframe)
				renderdelta = 0;
		}
		catch
		{
			/* In case of ERR_DROP we're jumping here. Don't know
			   if that's really save but it seems to work. So leave
			   it alone. */
		}
	}

	public static void Qcommon_Shutdown()
	{
		filesystem.FS_ShutdownFilesystem();
		cvar.Cvar_Fini();

		if (!Globals.DEDICATED_ONLY)
			Key.Shutdown();

		cmdparser.Cmd_Shutdown();
	}
}