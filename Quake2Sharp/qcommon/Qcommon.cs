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
namespace Quake2Sharp.qcommon;

using client;
using game;
using server;
using sys;

/**
 * Qcommon contains some  basic routines for the game engine
 * namely initialization, shutdown and frame generation.
 */
public class Qcommon
{
	public static readonly string BUILDSTRING = ".NET " + Environment.Version;
	public static readonly string CPUSTRING = Environment.OSVersion.ToString();

	/**
	 * This function initializes the different subsystems of
	 * the game engine. The setjmp/longjmp mechanism of the original
	 * was replaced with exceptions.
	 * @param args the original unmodified command line arguments
	 */
	public static void Init(string[] args)
	{
		try
		{
			// prepare enough of the subsystems to handle
			// cvar and command buffer management
			Com.InitArgv(args);

			Cbuf.Init();

			Cmd.Init();
			Cvar.Init();

			Key.Init();

			// we need to add the early commands twice, because
			// a basedir or cddir needs to be set before execing
			// config files, but we want other parms to override
			// the settings of the config files
			Cbuf.AddEarlyCommands(false);
			Cbuf.Execute();

			if (Globals.dedicated.value != 1.0f)
				Console.WriteLine("initializing filesystem...");

			FS.InitFilesystem();

			if (Globals.dedicated.value != 1.0f)
				Console.WriteLine("loading config...");

			Qcommon.reconfigure(false);

			FS.markBaseSearchPaths(); // mark the default search paths

			Qcommon.reconfigure(true); // reload default.cfg and config.cfg

			//
			// init commands and vars
			//
			Cmd.AddCommand("error", Com.Error_f);

			Globals.host_speeds = Cvar.Get("host_speeds", "0", 0);
			Globals.log_stats = Cvar.Get("log_stats", "0", 0);
			Globals.developer = Cvar.Get("developer", "0", Defines.CVAR_ARCHIVE);
			Globals.timescale = Cvar.Get("timescale", "0", 0);
			Globals.fixedtime = Cvar.Get("fixedtime", "0", 0);
			Globals.logfile_active = Cvar.Get("logfile", "0", 0);
			Globals.showtrace = Cvar.Get("showtrace", "0", 0);
			Globals.dedicated = Cvar.Get("dedicated", "0", Defines.CVAR_NOSET);
			var s = $"{Globals.VERSION:0.00,4} {Qcommon.CPUSTRING} {Globals.__DATE__} {Qcommon.BUILDSTRING}";

			Cvar.Get("version", s, Defines.CVAR_SERVERINFO | Defines.CVAR_NOSET);

			if (Globals.dedicated.value != 1.0f)
				Console.WriteLine("initializing network subsystem...");

			NET.Init(); //ok
			Netchan.Netchan_Init(); //ok

			if (Globals.dedicated.value != 1.0f)
				Console.WriteLine("initializing server subsystem...");

			SV_MAIN.SV_Init(); //ok

			if (Globals.dedicated.value != 1.0f)
				Console.WriteLine("initializing client subsystem...");

			Cl.Init();

			// add + commands from command line
			if (!Cbuf.AddLateCommands())
			{
				// if the user didn't give any commands, run default action
				if (Globals.dedicated.value == 0)
					Cbuf.AddText("d1\n");
				else
					Cbuf.AddText("dedicated_start\n");

				Cbuf.Execute();
			}
			else
			{
				// the user asked for something explicit
				// so drop the loading plaque
				SCR.EndLoadingPlaque();
			}

			Com.Printf("====== Quake2 Initialized ======\n\n");

			// save config when configuration is completed
			Cl.WriteConfiguration();
		}
		catch (Exception)
		{
			Sys.Error("Error during initialization");
		}
	}

	/**
	 * Trigger generation of a frame for the given time. The setjmp/longjmp
	 * mechanism of the original was replaced with exceptions.
	 * @param msec the current game time
	 */
	public static void FrameUpdate(int msec)
	{
		try
		{
			if (Globals.log_stats.modified)
			{
				Globals.log_stats.modified = false;

				if (Globals.log_stats.value != 0.0f)
				{
					if (Globals.log_stats_file != null)
					{
						try
						{
							Globals.log_stats_file.Close();
						}
						catch (Exception)
						{
						}

						Globals.log_stats_file = null;
					}

					try
					{
						Globals.log_stats_file = File.CreateText("stats.log");
					}
					catch (Exception)
					{
						Globals.log_stats_file = null;
					}

					if (Globals.log_stats_file != null)
					{
						try
						{
							Globals.log_stats_file.Write("entities,dlights,parts,frame time\n");
						}
						catch (Exception)
						{
						}
					}
				}
				else
				{
					if (Globals.log_stats_file != null)
					{
						try
						{
							Globals.log_stats_file.Close();
						}
						catch (Exception)
						{
						}

						Globals.log_stats_file = null;
					}
				}
			}

			if (Globals.fixedtime.value != 0.0f)
				msec = (int)Globals.fixedtime.value;
			else if (Globals.timescale.value != 0.0f)
			{
				msec = (int)(msec * Globals.timescale.value);

				if (msec < 1)
					msec = 1;
			}

			if (Globals.showtrace.value != 0.0f)
			{
				Com.Printf($"{Globals.c_traces,4} traces  {Globals.c_pointcontents,4} points\n");

				Globals.c_traces = 0;
				Globals.c_brush_traces = 0;
				Globals.c_pointcontents = 0;
			}

			Cbuf.Execute();

			var time_before = 0;
			var time_after = 0;

			if (Globals.host_speeds.value != 0.0f)
				time_before = Timer.Milliseconds();

			Com.debugContext = "SV:";
			SV_MAIN.SV_Frame(msec);

			if (Globals.host_speeds.value != 0.0f)
			{
				time_after = Timer.Milliseconds();

				var sv = time_after - time_before;
				var gm = Globals.time_after_game - Globals.time_before_game;
				var rf = Globals.time_after_ref - Globals.time_before_ref;
				sv -= gm;

				Com.Printf($"update:{sv,3} gm:{gm,3} rf:{rf,3}\n");
			}
		}
		catch (Exception e)
		{
			Com.DPrintf("exception:" + e);
		}
	}

	public static void FrameRender(int msec)
	{
		try
		{
			var time_before = 0;
			var time_after = 0;

			Com.debugContext = "CL:";
			Cl.Frame(msec);

			if (Globals.host_speeds.value != 0.0f)
			{
				time_after = Timer.Milliseconds();

				var cl = time_after - time_before;
				var gm = Globals.time_after_game - Globals.time_before_game;
				var rf = Globals.time_after_ref - Globals.time_before_ref;
				cl -= rf;

				Com.Printf($"render:{cl,3} gm:{gm,3} rf:{rf,3}\n");
			}
		}
		catch (Exception e)
		{
			Com.DPrintf("exception:" + e);
		}
	}

	private static void reconfigure(bool clear)
	{
		Cbuf.AddText("exec default.cfg\n");
		Cbuf.Execute();
		Cbuf.AddText("exec config.cfg\n");
		Cbuf.AddEarlyCommands(clear);
		Cbuf.Execute();
	}
}