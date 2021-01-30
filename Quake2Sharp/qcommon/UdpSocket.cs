namespace Quake2Sharp.qcommon
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Net.Sockets;

	public class UdpSocket
	{
		private readonly Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		private readonly List<Packet> packets = new();

		private class Packet
		{
			public readonly IPEndPoint EndPoint;
			public readonly byte[] Buffer;
			public readonly int Length;

			public Packet(IPEndPoint endPoint, byte[] buffer, int length)
			{
				this.EndPoint = endPoint;
				this.Buffer = buffer;
				this.Length = length;
			}
		}

		public UdpSocket(EndPoint endpoint)
		{
			this.socket.Blocking = false;
			this.socket.EnableBroadcast = true;
			this.socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
			this.socket.Bind(endpoint);
			this.Listen();
		}

		public void Send(byte[] data, IPEndPoint target)
		{
			this.socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, target, asyncResult => { this.socket.EndSendTo(asyncResult); }, null);
		}

		public IPEndPoint Receive(MemoryStream receiveBuffer)
		{
			var packet = this.packets.FirstOrDefault();

			if (packet == null)
				return null;

			receiveBuffer.Write(packet.Buffer, 0, packet.Length);
			this.packets.Remove(packet);

			return packet.EndPoint;
		}

		private void Listen()
		{
			var buffer = new byte[Defines.MAX_MSGLEN];
			EndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);

			this.socket.BeginReceiveFrom(
				buffer,
				0,
				Defines.MAX_MSGLEN,
				SocketFlags.None,
				ref endpoint,
				asyncResult =>
				{
					try
					{
						var length = this.socket.EndReceiveFrom(asyncResult, ref endpoint);
						this.packets.Add(new((IPEndPoint)endpoint, buffer, length));
						this.Listen();
					}
					catch (Exception)
					{
						// ignored
					}
				},
				null
			);
		}

		public void Close()
		{
			this.socket.Close();
		}
	}
}
