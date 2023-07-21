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
namespace Quake2Sharp.qcommon.types;

using sys;
using System.Net;

public class netadr_t
{
	public int type;
	public int port;
	public byte[] ip;

	public netadr_t()
	{
		this.type = Defines.NA_LOOPBACK;
		this.port = 0; // any

		try
		{
			// localhost / 127.0.0.1
			this.ip = IPAddress.Loopback.GetAddressBytes();
		}
		catch (Exception)
		{
		}
	}

	public IPAddress getInetAddress()
	{
		switch (this.type)
		{
			case Defines.NA_BROADCAST:
				return IPAddress.Broadcast;

			case Defines.NA_LOOPBACK:
				// localhost / 127.0.0.1
				return IPAddress.Loopback;

			case Defines.NA_IP:
				return new(this.ip);

			default:
				return null;
		}
	}

	public void set(netadr_t from)
	{
		this.type = from.type;
		this.port = from.port;
		this.ip[0] = from.ip[0];
		this.ip[1] = from.ip[1];
		this.ip[2] = from.ip[2];
		this.ip[3] = from.ip[3];
	}

	public string toString()
	{
		return this.type == Defines.NA_LOOPBACK ? "loopback" : NET.AdrToString(this);
	}
}