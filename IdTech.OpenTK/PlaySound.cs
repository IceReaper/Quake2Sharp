using Quake2Sharp.util;

namespace Quake2Sharp.opentk;

public class PlaySound
{
	private static readonly int MAX_PLAYSOUNDS = 128;

	// list with sentinel
	private static readonly PlaySound freeList;
	private static readonly PlaySound playableList;
	private static readonly PlaySound[] backbuffer = new PlaySound[MAX_PLAYSOUNDS];

	static PlaySound()
	{
		for (var i = 0; i < backbuffer.Length; i++)
			backbuffer[i] = new();

		// init the sentinels
		freeList = new();
		playableList = new();

		// reset the lists
		reset();
	}

	// sound attributes
	public int type;
	public int entnum;
	public int entchannel;
	public int bufferId;
	public float volume;
	public float attenuation;
	public float[] origin = { 0, 0, 0 };

	// begin time in ms
	private long beginTime;

	// for linked list
	private PlaySound prev, next;

	private PlaySound()
	{
		this.prev = this.next = null;
		this.clear();
	}

	private void clear()
	{
		this.type = this.bufferId = this.entnum = this.entchannel = -1;

		// volume = attenuation = beginTime = 0;
		this.attenuation = this.beginTime = 0;

		// Math3D.VectorClear(origin);
	}

	public static void reset()
	{
		// init the sentinels
		freeList.next = freeList.prev = freeList;
		playableList.next = playableList.prev = playableList;

		// concat the the freeList
		PlaySound ps;

		for (var i = 0; i < backbuffer.Length; i++)
		{
			ps = backbuffer[i];
			ps.clear();
			ps.prev = freeList;
			ps.next = freeList.next;
			ps.prev.next = ps;
			ps.next.prev = ps;
		}
	}

	public static PlaySound nextPlayableSound()
	{
		PlaySound ps = null;

		while (true)
		{
			ps = playableList.next;

			if (ps == playableList || ps.beginTime > Globals.cl.time)
				return null;

			release(ps);

			return ps;
		}
	}

	private static PlaySound get()
	{
		var ps = freeList.next;

		if (ps == freeList)
			return null;

		ps.prev.next = ps.next;
		ps.next.prev = ps.prev;

		return ps;
	}

	private static void add(PlaySound ps)
	{
		var sort = playableList.next;

		for (; sort != playableList && sort.beginTime < ps.beginTime; sort = sort.next)
			;

		ps.next = sort;
		ps.prev = sort.prev;
		ps.next.prev = ps;
		ps.prev.next = ps;
	}

	private static void release(PlaySound ps)
	{
		ps.prev.next = ps.next;
		ps.next.prev = ps.prev;

		// add to free list
		ps.next = freeList.next;
		freeList.next.prev = ps;
		ps.prev = freeList;
		freeList.next = ps;
	}

	public static void allocate(float[] origin, int entnum, int entchannel, int bufferId, float volume, float attenuation, float timeoffset)
	{
		var ps = get();

		if (ps != null)
		{
			// find the right sound type
			if (entnum == Globals.cl.playernum + 1)
				ps.type = Channel.LISTENER;
			else if (origin != null)
			{
				ps.type = Channel.FIXED;
				Math3D.VectorCopy(origin, ps.origin);
			}
			else
				ps.type = Channel.DYNAMIC;

			ps.entnum = entnum;
			ps.entchannel = entchannel;
			ps.bufferId = bufferId;
			ps.volume = volume;
			ps.attenuation = attenuation;
			ps.beginTime = Globals.cl.time + (long)(timeoffset * 1000);
			add(ps);
		}
		else
			Console.Error.WriteLine("PlaySounds out of Limit");
	}
}