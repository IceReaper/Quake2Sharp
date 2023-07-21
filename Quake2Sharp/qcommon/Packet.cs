namespace Quake2Sharp.qcommon;

using System.Net;

public class Packet
{
	public readonly IPEndPoint NetPeer;
	public readonly byte[] RawData;

	public Packet(IPEndPoint netPeer, byte[] rawData)
	{
		this.NetPeer = netPeer;
		this.RawData = rawData;
	}
}