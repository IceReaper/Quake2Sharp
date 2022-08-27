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
using qcommon.types;
using sys;
using types;
using util;

/**
 * CL_input
 */
public class CL_input
{
	private static long frame_msec;
	private static long old_sys_frame_time;

	private static cvar_t cl_nodelta;
	/*
	 * ===============================================================================
	 * 
	 * KEY BUTTONS
	 * 
	 * Continuous button event tracking is complicated by the fact that two
	 * different input sources (say, mouse button 1 and the control key) can
	 * both press the same button, but the button should only be released when
	 * both of the pressing key have been released.
	 * 
	 * When a key event issues a button command (+forward, +attack, etc), it
	 * appends its key number as a parameter to the command so it can be matched
	 * up with the release.
	 * 
	 * state bit 0 is the current state of the key state bit 1 is edge triggered
	 * on the up to down transition state bit 2 is edge triggered on the down to
	 * up transition
	 * 
	 * 
	 * Key_Event (int key, qboolean down, unsigned time);
	 * 
	 * +mlook src time
	 * 
	 * ===============================================================================
	 */

	private static readonly kbutton_t in_klook = new();
	private static readonly kbutton_t in_left = new();
	private static readonly kbutton_t in_right = new();
	private static readonly kbutton_t in_forward = new();
	private static readonly kbutton_t in_back = new();
	private static readonly kbutton_t in_lookup = new();
	private static readonly kbutton_t in_lookdown = new();
	private static readonly kbutton_t in_moveleft = new();
	private static readonly kbutton_t in_moveright = new();
	public static kbutton_t in_strafe = new();
	private static readonly kbutton_t in_speed = new();
	private static readonly kbutton_t in_use = new();
	private static readonly kbutton_t in_attack = new();
	private static readonly kbutton_t in_up = new();
	private static readonly kbutton_t in_down = new();
	private static int in_impulse;

	private static void KeyDown(kbutton_t b)
	{
		int k;
		string c;
		c = Cmd.Argv(1);

		if (c.Length > 0)
			k = Lib.atoi(c);
		else
			k = -1; // typed manually at the console for continuous down

		if (k == b.down[0] || k == b.down[1])
			return; // repeating key

		if (b.down[0] == 0)
			b.down[0] = k;
		else if (b.down[1] == 0)
			b.down[1] = k;
		else
		{
			Com.Printf("Three keys down for a button!\n");

			return;
		}

		if ((b.state & 1) != 0)
			return; // still down

		// save timestamp
		c = Cmd.Argv(2);
		b.downtime = Lib.atoi(c);

		if (b.downtime == 0)
			b.downtime = Globals.sys_frame_time - 100;

		b.state |= 3; // down + impulse down
	}

	private static void KeyUp(kbutton_t b)
	{
		int k;
		string c;
		int uptime;
		c = Cmd.Argv(1);

		if (c.Length > 0)
			k = Lib.atoi(c);
		else
		{
			// typed manually at the console, assume for unsticking, so clear
			// all
			b.down[0] = b.down[1] = 0;
			b.state = 4; // impulse up

			return;
		}

		if (b.down[0] == k)
			b.down[0] = 0;
		else if (b.down[1] == k)
			b.down[1] = 0;
		else
			return; // key up without coresponding down (menu pass through)

		if (b.down[0] != 0 || b.down[1] != 0)
			return; // some other key is still holding it down

		if ((b.state & 1) == 0)
			return; // still up (this should not happen)

		// save timestamp
		c = Cmd.Argv(2);
		uptime = Lib.atoi(c);

		if (uptime != 0)
			b.msec += uptime - b.downtime;
		else
			b.msec += 10;

		b.state &= ~1; // now up
		b.state |= 4; // impulse up
	}

	private static void IN_KLookDown()
	{
		CL_input.KeyDown(CL_input.in_klook);
	}

	private static void IN_KLookUp()
	{
		CL_input.KeyUp(CL_input.in_klook);
	}

	private static void IN_UpDown()
	{
		CL_input.KeyDown(CL_input.in_up);
	}

	private static void IN_UpUp()
	{
		CL_input.KeyUp(CL_input.in_up);
	}

	private static void IN_DownDown()
	{
		CL_input.KeyDown(CL_input.in_down);
	}

	private static void IN_DownUp()
	{
		CL_input.KeyUp(CL_input.in_down);
	}

	private static void IN_LeftDown()
	{
		CL_input.KeyDown(CL_input.in_left);
	}

	private static void IN_LeftUp()
	{
		CL_input.KeyUp(CL_input.in_left);
	}

	private static void IN_RightDown()
	{
		CL_input.KeyDown(CL_input.in_right);
	}

	private static void IN_RightUp()
	{
		CL_input.KeyUp(CL_input.in_right);
	}

	private static void IN_ForwardDown()
	{
		CL_input.KeyDown(CL_input.in_forward);
	}

	private static void IN_ForwardUp()
	{
		CL_input.KeyUp(CL_input.in_forward);
	}

	private static void IN_BackDown()
	{
		CL_input.KeyDown(CL_input.in_back);
	}

	private static void IN_BackUp()
	{
		CL_input.KeyUp(CL_input.in_back);
	}

	private static void IN_LookupDown()
	{
		CL_input.KeyDown(CL_input.in_lookup);
	}

	private static void IN_LookupUp()
	{
		CL_input.KeyUp(CL_input.in_lookup);
	}

	private static void IN_LookdownDown()
	{
		CL_input.KeyDown(CL_input.in_lookdown);
	}

	private static void IN_LookdownUp()
	{
		CL_input.KeyUp(CL_input.in_lookdown);
	}

	private static void IN_MoveleftDown()
	{
		CL_input.KeyDown(CL_input.in_moveleft);
	}

	private static void IN_MoveleftUp()
	{
		CL_input.KeyUp(CL_input.in_moveleft);
	}

	private static void IN_MoverightDown()
	{
		CL_input.KeyDown(CL_input.in_moveright);
	}

	private static void IN_MoverightUp()
	{
		CL_input.KeyUp(CL_input.in_moveright);
	}

	private static void IN_SpeedDown()
	{
		CL_input.KeyDown(CL_input.in_speed);
	}

	private static void IN_SpeedUp()
	{
		CL_input.KeyUp(CL_input.in_speed);
	}

	private static void IN_StrafeDown()
	{
		CL_input.KeyDown(CL_input.in_strafe);
	}

	private static void IN_StrafeUp()
	{
		CL_input.KeyUp(CL_input.in_strafe);
	}

	private static void IN_AttackDown()
	{
		CL_input.KeyDown(CL_input.in_attack);
	}

	private static void IN_AttackUp()
	{
		CL_input.KeyUp(CL_input.in_attack);
	}

	private static void IN_UseDown()
	{
		CL_input.KeyDown(CL_input.in_use);
	}

	private static void IN_UseUp()
	{
		CL_input.KeyUp(CL_input.in_use);
	}

	private static void IN_Impulse()
	{
		CL_input.in_impulse = Lib.atoi(Cmd.Argv(1));
	}

	/*
	 * =============== CL_KeyState
	 * 
	 * Returns the fraction of the frame that the key was down ===============
	 */
	private static float KeyState(kbutton_t key)
	{
		float val;
		long msec;
		key.state &= 1; // clear impulses
		msec = key.msec;
		key.msec = 0;

		if (key.state != 0)
		{
			// still down
			msec += Globals.sys_frame_time - key.downtime;
			key.downtime = Globals.sys_frame_time;
		}

		val = (float)msec / CL_input.frame_msec;

		if (val < 0)
			val = 0;

		if (val > 1)
			val = 1;

		return val;
	}

	//	  ==========================================================================

	/*
	 * ================ CL_AdjustAngles
	 * 
	 * Moves the local angle positions ================
	 */
	private static void AdjustAngles()
	{
		float speed;
		float up, down;

		if ((CL_input.in_speed.state & 1) != 0)
			speed = Globals.cls.frametime * Globals.cl_anglespeedkey.value;
		else
			speed = Globals.cls.frametime;

		if ((CL_input.in_strafe.state & 1) == 0)
		{
			Globals.cl.viewangles[Defines.YAW] -= speed * Globals.cl_yawspeed.value * CL_input.KeyState(CL_input.in_right);
			Globals.cl.viewangles[Defines.YAW] += speed * Globals.cl_yawspeed.value * CL_input.KeyState(CL_input.in_left);
		}

		if ((CL_input.in_klook.state & 1) != 0)
		{
			Globals.cl.viewangles[Defines.PITCH] -= speed * Globals.cl_pitchspeed.value * CL_input.KeyState(CL_input.in_forward);
			Globals.cl.viewangles[Defines.PITCH] += speed * Globals.cl_pitchspeed.value * CL_input.KeyState(CL_input.in_back);
		}

		up = CL_input.KeyState(CL_input.in_lookup);
		down = CL_input.KeyState(CL_input.in_lookdown);
		Globals.cl.viewangles[Defines.PITCH] -= speed * Globals.cl_pitchspeed.value * up;
		Globals.cl.viewangles[Defines.PITCH] += speed * Globals.cl_pitchspeed.value * down;
	}

	/*
	 * ================ CL_BaseMove
	 * 
	 * Send the intended movement message to the server ================
	 */
	private static void BaseMove(usercmd_t cmd)
	{
		CL_input.AdjustAngles();

		//memset (cmd, 0, sizeof(*cmd));
		cmd.clear();
		Math3D.VectorCopy(Globals.cl.viewangles, cmd.angles);

		if ((CL_input.in_strafe.state & 1) != 0)
		{
			cmd.sidemove += (short)(Globals.cl_sidespeed.value * CL_input.KeyState(CL_input.in_right));
			cmd.sidemove -= (short)(Globals.cl_sidespeed.value * CL_input.KeyState(CL_input.in_left));
		}

		cmd.sidemove += (short)(Globals.cl_sidespeed.value * CL_input.KeyState(CL_input.in_moveright));
		cmd.sidemove -= (short)(Globals.cl_sidespeed.value * CL_input.KeyState(CL_input.in_moveleft));
		cmd.upmove += (short)(Globals.cl_upspeed.value * CL_input.KeyState(CL_input.in_up));
		cmd.upmove -= (short)(Globals.cl_upspeed.value * CL_input.KeyState(CL_input.in_down));

		if ((CL_input.in_klook.state & 1) == 0)
		{
			cmd.forwardmove += (short)(Globals.cl_forwardspeed.value * CL_input.KeyState(CL_input.in_forward));
			cmd.forwardmove -= (short)(Globals.cl_forwardspeed.value * CL_input.KeyState(CL_input.in_back));
		}

		//
		//	   adjust for speed key / running
		//
		if (((CL_input.in_speed.state & 1) ^ (int)Globals.cl_run.value) != 0)
		{
			cmd.forwardmove *= 2;
			cmd.sidemove *= 2;
			cmd.upmove *= 2;
		}
	}

	private static void ClampPitch()
	{
		float pitch;
		pitch = Math3D.SHORT2ANGLE(Globals.cl.frame.playerstate.pmove.delta_angles[Defines.PITCH]);

		if (pitch > 180)
			pitch -= 360;

		if (Globals.cl.viewangles[Defines.PITCH] + pitch < -360)
			Globals.cl.viewangles[Defines.PITCH] += 360; // wrapped

		if (Globals.cl.viewangles[Defines.PITCH] + pitch > 360)
			Globals.cl.viewangles[Defines.PITCH] -= 360; // wrapped

		if (Globals.cl.viewangles[Defines.PITCH] + pitch > 89)
			Globals.cl.viewangles[Defines.PITCH] = 89 - pitch;

		if (Globals.cl.viewangles[Defines.PITCH] + pitch < -89)
			Globals.cl.viewangles[Defines.PITCH] = -89 - pitch;
	}

	/*
	 * ============== CL_FinishMove ==============
	 */
	private static void FinishMove(usercmd_t cmd)
	{
		int ms;
		int i;

		//
		//	   figure button bits
		//	
		if ((CL_input.in_attack.state & 3) != 0)
			cmd.buttons |= (byte)Defines.BUTTON_ATTACK;

		CL_input.in_attack.state &= ~2;

		if ((CL_input.in_use.state & 3) != 0)
			cmd.buttons |= (byte)Defines.BUTTON_USE;

		CL_input.in_use.state &= ~2;

		if (Key.anykeydown != 0 && Globals.cls.key_dest == Defines.key_game)
			cmd.buttons |= (byte)Defines.BUTTON_ANY;

		// send milliseconds of time to apply the move
		ms = (int)(Globals.cls.frametime * 1000);

		if (ms > 250)
			ms = 100; // time was unreasonable

		cmd.msec = (byte)ms;
		CL_input.ClampPitch();

		for (i = 0; i < 3; i++)
			cmd.angles[i] = (short)Math3D.ANGLE2SHORT(Globals.cl.viewangles[i]);

		cmd.impulse = (byte)CL_input.in_impulse;
		CL_input.in_impulse = 0;

		// send the ambient light level at the player's current position
		cmd.lightlevel = (byte)Globals.cl_lightlevel.value;
	}

	/*
	 * ================= CL_CreateCmd =================
	 */
	private static void CreateCmd(usercmd_t cmd)
	{
		//usercmd_t cmd = new usercmd_t();
		CL_input.frame_msec = Globals.sys_frame_time - CL_input.old_sys_frame_time;

		if (CL_input.frame_msec < 1)
			CL_input.frame_msec = 1;

		if (CL_input.frame_msec > 200)
			CL_input.frame_msec = 200;

		// get basic movement from keyboard
		CL_input.BaseMove(cmd);

		// allow mice or other external controllers to add to the move
		IN.Move(cmd);
		CL_input.FinishMove(cmd);
		CL_input.old_sys_frame_time = Globals.sys_frame_time;

		//return cmd;
	}

	/*
	 * ============ CL_InitInput ============
	 */
	public static void InitInput()
	{
		Cmd.AddCommand("centerview", () => { IN.CenterView(); });
		Cmd.AddCommand("+moveup", () => { CL_input.IN_UpDown(); });
		Cmd.AddCommand("-moveup", () => { CL_input.IN_UpUp(); });
		Cmd.AddCommand("+movedown", () => { CL_input.IN_DownDown(); });
		Cmd.AddCommand("-movedown", () => { CL_input.IN_DownUp(); });
		Cmd.AddCommand("+left", () => { CL_input.IN_LeftDown(); });
		Cmd.AddCommand("-left", () => { CL_input.IN_LeftUp(); });
		Cmd.AddCommand("+right", () => { CL_input.IN_RightDown(); });
		Cmd.AddCommand("-right", () => { CL_input.IN_RightUp(); });
		Cmd.AddCommand("+forward", () => { CL_input.IN_ForwardDown(); });
		Cmd.AddCommand("-forward", () => { CL_input.IN_ForwardUp(); });
		Cmd.AddCommand("+back", () => { CL_input.IN_BackDown(); });
		Cmd.AddCommand("-back", () => { CL_input.IN_BackUp(); });
		Cmd.AddCommand("+lookup", () => { CL_input.IN_LookupDown(); });
		Cmd.AddCommand("-lookup", () => { CL_input.IN_LookupUp(); });
		Cmd.AddCommand("+lookdown", () => { CL_input.IN_LookdownDown(); });
		Cmd.AddCommand("-lookdown", () => { CL_input.IN_LookdownUp(); });
		Cmd.AddCommand("+strafe", () => { CL_input.IN_StrafeDown(); });
		Cmd.AddCommand("-strafe", () => { CL_input.IN_StrafeUp(); });
		Cmd.AddCommand("+moveleft", () => { CL_input.IN_MoveleftDown(); });
		Cmd.AddCommand("-moveleft", () => { CL_input.IN_MoveleftUp(); });
		Cmd.AddCommand("+moveright", () => { CL_input.IN_MoverightDown(); });
		Cmd.AddCommand("-moveright", () => { CL_input.IN_MoverightUp(); });
		Cmd.AddCommand("+speed", () => { CL_input.IN_SpeedDown(); });
		Cmd.AddCommand("-speed", () => { CL_input.IN_SpeedUp(); });
		Cmd.AddCommand("+attack", () => { CL_input.IN_AttackDown(); });
		Cmd.AddCommand("-attack", () => { CL_input.IN_AttackUp(); });
		Cmd.AddCommand("+use", () => { CL_input.IN_UseDown(); });
		Cmd.AddCommand("-use", () => { CL_input.IN_UseUp(); });
		Cmd.AddCommand("impulse", () => { CL_input.IN_Impulse(); });
		Cmd.AddCommand("+klook", () => { CL_input.IN_KLookDown(); });
		Cmd.AddCommand("-klook", () => { CL_input.IN_KLookUp(); });
		CL_input.cl_nodelta = Cvar.Get("cl_nodelta", "0", 0);
	}

	private static readonly sizebuf_t buf = new();
	private static readonly byte[] data = new byte[128];
	private static readonly usercmd_t nullcmd = new();

	/*
	 * ================= CL_SendCmd =================
	 */
	public static void SendCmd()
	{
		int i;
		usercmd_t cmd, oldcmd;
		int checksumIndex;

		// build a command even if not connected

		// save this command off for prediction
		i = Globals.cls.netchan.outgoing_sequence & (Defines.CMD_BACKUP - 1);
		cmd = Globals.cl.cmds[i];
		Globals.cl.cmd_time[i] = (int)Globals.cls.realtime; // for netgraph

		// ping calculation

		// fill the cmd
		CL_input.CreateCmd(cmd);
		Globals.cl.cmd.set(cmd);

		if (Globals.cls.state == Defines.ca_disconnected || Globals.cls.state == Defines.ca_connecting)
			return;

		if (Globals.cls.state == Defines.ca_connected)
		{
			if (Globals.cls.netchan.message.cursize != 0 || Globals.curtime - Globals.cls.netchan.last_sent > 1000)
				Netchan.Transmit(Globals.cls.netchan, 0, new byte[0]);

			return;
		}

		// send a userinfo update if needed
		if (Globals.userinfo_modified)
		{
			Cl.FixUpGender();
			Globals.userinfo_modified = false;
			MSG.WriteByte(Globals.cls.netchan.message, Defines.clc_userinfo);
			MSG.WriteString(Globals.cls.netchan.message, Cvar.Userinfo());
		}

		SZ.Init(CL_input.buf, CL_input.data, CL_input.data.Length);

		if (cmd.buttons != 0 && Globals.cl.cinematictime > 0 && !Globals.cl.attractloop && Globals.cls.realtime - Globals.cl.cinematictime > 1000)
		{
			// skip
			// the
			// rest
			// of
			// the
			// cinematic
			SCR.FinishCinematic();
		}

		// begin a client move command
		MSG.WriteByte(CL_input.buf, Defines.clc_move);

		// save the position for a checksum byte
		checksumIndex = CL_input.buf.cursize;
		MSG.WriteByte(CL_input.buf, 0);

		// let the server know what the last frame we
		// got was, so the next message can be delta compressed
		if (CL_input.cl_nodelta.value != 0.0f || !Globals.cl.frame.valid || Globals.cls.demowaiting)
			MSG.WriteLong(CL_input.buf, -1); // no compression
		else
			MSG.WriteLong(CL_input.buf, Globals.cl.frame.serverframe);

		// send this and the previous cmds in the message, so
		// if the last packet was dropped, it can be recovered
		i = (Globals.cls.netchan.outgoing_sequence - 2) & (Defines.CMD_BACKUP - 1);
		cmd = Globals.cl.cmds[i];

		//memset (nullcmd, 0, sizeof(nullcmd));
		CL_input.nullcmd.clear();
		MSG.WriteDeltaUsercmd(CL_input.buf, CL_input.nullcmd, cmd);
		oldcmd = cmd;
		i = (Globals.cls.netchan.outgoing_sequence - 1) & (Defines.CMD_BACKUP - 1);
		cmd = Globals.cl.cmds[i];
		MSG.WriteDeltaUsercmd(CL_input.buf, oldcmd, cmd);
		oldcmd = cmd;
		i = Globals.cls.netchan.outgoing_sequence & (Defines.CMD_BACKUP - 1);
		cmd = Globals.cl.cmds[i];
		MSG.WriteDeltaUsercmd(CL_input.buf, oldcmd, cmd);

		// calculate a checksum over the move commands
		CL_input.buf.data[checksumIndex] = Com.BlockSequenceCRCByte(
			CL_input.buf.data,
			checksumIndex + 1,
			CL_input.buf.cursize - checksumIndex - 1,
			Globals.cls.netchan.outgoing_sequence
		);

		//
		// deliver the message
		//
		Netchan.Transmit(Globals.cls.netchan, CL_input.buf.cursize, CL_input.buf.data);
	}
}