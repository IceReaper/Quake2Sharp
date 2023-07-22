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

using IdTech.common;
using Quake2Sharp.qcommon;
using Quake2Sharp.qcommon.types;
using Quake2Sharp.util;
using System.Net;
using System.Text;

namespace Quake2Sharp.sys;

public class NET
{
	private static readonly int MAX_LOOPBACK = 4;

	/** Local loopback adress. */
	private static readonly netadr_t net_local_adr = new();

	public class loopmsg_t
	{
		public byte[] data = new byte[Defines.MAX_MSGLEN];
		public int datalen;
	}

	public class loopback_t
	{
		public loopback_t()
		{
			this.msgs = new loopmsg_t[NET.MAX_LOOPBACK];

			for (var n = 0; n < NET.MAX_LOOPBACK; n++)
				this.msgs[n] = new();
		}

		public loopmsg_t[] msgs;
		public int get;
		public int send;
	}

	public static loopback_t[] loopbacks = new loopback_t[2];

	static NET()
	{
		NET.loopbacks[0] = new();
		NET.loopbacks[1] = new();
	}

	private static readonly UdpSocket[] ip_sockets = { null, null };

	/**
     * Compares ip address and port.
     */
	public static bool CompareAdr(netadr_t a, netadr_t b)
	{
		return a.ip[0] == b.ip[0] && a.ip[1] == b.ip[1] && a.ip[2] == b.ip[2] && a.ip[3] == b.ip[3] && a.port == b.port;
	}

	/**
     * Compares ip address without the port.
     */
	public static bool CompareBaseAdr(netadr_t a, netadr_t b)
	{
		if (a.type != b.type)
			return false;

		if (a.type == Defines.NA_LOOPBACK)
			return true;

		if (a.type == Defines.NA_IP)
			return a.ip[0] == b.ip[0] && a.ip[1] == b.ip[1] && a.ip[2] == b.ip[2] && a.ip[3] == b.ip[3];

		return false;
	}

	/**
     * Returns a string holding ip address and port like "ip0.ip1.ip2.ip3:port".
     */
	public static string AdrToString(netadr_t a)
	{
		StringBuilder sb = new();
		sb.Append(a.ip[0] & 0xFF).Append('.').Append(a.ip[1] & 0xFF);
		sb.Append('.');
		sb.Append(a.ip[2] & 0xFF).Append('.').Append(a.ip[3] & 0xFF);
		sb.Append(':').Append(a.port);

		return sb.ToString();
	}

	/**
     * Returns IP address without the port as string.
     */
	public static string BaseAdrToString(netadr_t a)
	{
		StringBuilder sb = new();
		sb.Append(a.ip[0] & 0xFF).Append('.').Append(a.ip[1] & 0xFF);
		sb.Append('.');
		sb.Append(a.ip[2] & 0xFF).Append('.').Append(a.ip[3] & 0xFF);

		return sb.ToString();
	}

	/**
     * Creates an netadr_t from an string.
     */
	public static bool StringToAdr(string s, netadr_t a)
	{
		if (s.Equals("localhost", StringComparison.OrdinalIgnoreCase) || s.Equals("loopback", StringComparison.OrdinalIgnoreCase))
		{
			a.set(NET.net_local_adr);

			return true;
		}

		try
		{
			var address = s.Split(":");
			var ia = IPAddress.Parse(address[0]);
			a.ip = ia.GetAddressBytes();
			a.type = Defines.NA_IP;

			if (address.Length == 2)
				a.port = Lib.atoi(address[1]);

			return true;
		}
		catch (Exception e)
		{
			Com.Println(e.Message);

			return false;
		}
	}

	/**
     * Seems to return true, if the address is is on 127.0.0.1.
     */
	public static bool IsLocalAddress(netadr_t adr)
	{
		return NET.CompareAdr(adr, NET.net_local_adr);
	}

	/*
	 * ==================================================
	 *
	 * LOOPBACK BUFFERS FOR LOCAL PLAYER
	 *
	 * ==================================================
	 */

	/**
     * Gets a packet from internal loopback.
     */
	public static bool GetLoopPacket(int sock, netadr_t net_from, sizebuf_t net_message)
	{
		var loop = NET.loopbacks[sock];

		if (loop.send - loop.get > NET.MAX_LOOPBACK)
			loop.get = loop.send - NET.MAX_LOOPBACK;

		if (loop.get >= loop.send)
			return false;

		var i = loop.get & (NET.MAX_LOOPBACK - 1);
		loop.get++;

		Array.Copy(loop.msgs[i].data, 0, net_message.data, 0, loop.msgs[i].datalen);
		net_message.cursize = loop.msgs[i].datalen;

		net_from.set(NET.net_local_adr);

		return true;
	}

	/**
     * Sends a packet via internal loopback.
     */
	public static void SendLoopPacket(int sock, int length, byte[] data, netadr_t to)
	{
		int i;
		loopback_t loop;

		loop = NET.loopbacks[sock ^ 1];

		// modulo 4
		i = loop.send & (NET.MAX_LOOPBACK - 1);
		loop.send++;

		Array.Copy(data, 0, loop.msgs[i].data, 0, length);
		loop.msgs[i].datalen = length;
	}

	/**
     * Gets a packet from a network channel
     */
	public static bool GetPacket(int sock, netadr_t net_from, sizebuf_t net_message)
	{
		if (NET.GetLoopPacket(sock, net_from, net_message))
			return true;

		if (NET.ip_sockets[sock] == null)
			return false;

		try
		{
			MemoryStream receiveBuffer = new(net_message.data);

			var srcSocket = NET.ip_sockets[sock].Receive(receiveBuffer);

			if (srcSocket == null)
				return false;

			net_from.ip = srcSocket.Address.GetAddressBytes();
			net_from.port = srcSocket.Port;
			net_from.type = Defines.NA_IP;

			var packetLength = (int)receiveBuffer.Position;

			if (packetLength > net_message.maxsize)
			{
				Com.Println("Oversize packet from " + NET.AdrToString(net_from));

				return false;
			}

			// set the size
			net_message.cursize = packetLength;

			// set the sentinel
			net_message.data[packetLength] = 0;

			return true;
		}
		catch (Exception e)
		{
			clientserver.Com_DPrintf("NET_GetPacket: " + e + " from " + NET.AdrToString(net_from) + "\n");

			return false;
		}
	}

	/**
     * Sends a Packet.
     */
	public static void SendPacket(int sock, int length, byte[] data, netadr_t to)
	{
		if (to.type == Defines.NA_LOOPBACK)
		{
			NET.SendLoopPacket(sock, length, data, to);

			return;
		}

		if (NET.ip_sockets[sock] == null)
			return;

		if (to.type != Defines.NA_BROADCAST && to.type != Defines.NA_IP)
		{
			clientserver.Com_Error(Defines.ERR_FATAL, "NET_SendPacket: bad address type");

			return;
		}

		try
		{
			IPEndPoint dstSocket = new(to.getInetAddress(), to.port);
			var tmp = new byte[length];
			Array.Copy(data, 0, tmp, 0, length);
			NET.ip_sockets[sock].Send(tmp, dstSocket);
		}
		catch (Exception e)
		{
			Com.Println("NET_SendPacket ERROR: " + e + " to " + NET.AdrToString(to));
		}
	}

	/**
     * Config multi or singlepalyer - A single player game will only use the loopback code.
     */
	public static void ConfigServer(bool on)
	{
		if (NET.ip_sockets[Defines.NS_SERVER] != null)
		{
			NET.ip_sockets[Defines.NS_SERVER].Close();
			NET.ip_sockets[Defines.NS_SERVER] = null;
		}

		if (!on)
			return;

		var port = cvar.Cvar_Get("port", "" + Defines.PORT_SERVER, Defines.CVAR_NOSET);
		var ip = cvar.Cvar_Get("ip", "localhost", Defines.CVAR_NOSET);

		NET.ip_sockets[Defines.NS_SERVER] = NET.Socket(ip.@string, (int)port.value);
	}

	public static void ConfigClient(bool on)
	{
		if (NET.ip_sockets[Defines.NS_CLIENT] != null)
		{
			NET.ip_sockets[Defines.NS_CLIENT].Close();
			NET.ip_sockets[Defines.NS_CLIENT] = null;
		}

		if (!on)
			return;

		var ip = cvar.Cvar_Get("ip", "localhost", Defines.CVAR_NOSET);
		var clientport = cvar.Cvar_Get("clientport", "" + Defines.PORT_CLIENT, Defines.CVAR_NOSET);

		try
		{
			NET.ip_sockets[Defines.NS_CLIENT] = NET.Socket(ip.@string, (int)clientport.value);
		}
		catch (Exception)
		{
			NET.ip_sockets[Defines.NS_CLIENT] = NET.Socket(ip.@string, Defines.PORT_ANY);
		}
	}

	/**
     * Init
     */
	public static void Init()
	{
		// nothing to do
	}

	/*
	 * Socket
	 */
	private static UdpSocket Socket(string ip, int port)
	{
		UdpSocket newsocket;

		try
		{
			if (string.IsNullOrEmpty(ip) || ip.Equals("localhost"))
				newsocket = port == Defines.PORT_ANY ? new(new IPEndPoint(IPAddress.Any, 0)) : new UdpSocket(new IPEndPoint(IPAddress.Any, port));
			else
				newsocket = new(new IPEndPoint(IPAddress.Parse(ip), port));
		}
		catch (Exception e)
		{
			Com.Println("Error: " + e);
			newsocket = null;
		}

		return newsocket;
	}

	/**
     * Shutdown - closes the sockets
     */
	public static void Shutdown()
	{
		// close sockets
		NET.ConfigClient(false);
		NET.ConfigServer(false);
	}
}
