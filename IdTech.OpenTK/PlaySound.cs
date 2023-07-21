namespace Quake2Sharp.opentk;

using util;

public class PlaySound
{
	private static readonly int MAX_PLAYSOUNDS = 128;

	// list with sentinel
	private static readonly PlaySound freeList;
	private static readonly PlaySound playableList;
	private static readonly PlaySound[] backbuffer = new PlaySound[PlaySound.MAX_PLAYSOUNDS];

	static PlaySound()
	{
		for (var i = 0; i < PlaySound.backbuffer.Length; i++)
			PlaySound.backbuffer[i] = new();

		// init the sentinels
		PlaySound.freeList = new();
		PlaySound.playableList = new();

		// reset the lists
		PlaySound.reset();
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
		PlaySound.freeList.next = PlaySound.freeList.prev = PlaySound.freeList;
		PlaySound.playableList.next = PlaySound.playableList.prev = PlaySound.playableList;

		// concat the the freeList
		PlaySound ps;

		for (var i = 0; i < PlaySound.backbuffer.Length; i++)
		{
			ps = PlaySound.backbuffer[i];
			ps.clear();
			ps.prev = PlaySound.freeList;
			ps.next = PlaySound.freeList.next;
			ps.prev.next = ps;
			ps.next.prev = ps;
		}
	}

	public static PlaySound nextPlayableSound()
	{
		PlaySound ps = null;

		while (true)
		{
			ps = PlaySound.playableList.next;

			if (ps == PlaySound.playableList || ps.beginTime > Globals.cl.time)
				return null;

			PlaySound.release(ps);

			return ps;
		}
	}

	private static PlaySound get()
	{
		var ps = PlaySound.freeList.next;

		if (ps == PlaySound.freeList)
			return null;

		ps.prev.next = ps.next;
		ps.next.prev = ps.prev;

		return ps;
	}

	private static void add(PlaySound ps)
	{
		var sort = PlaySound.playableList.next;

		for (; sort != PlaySound.playableList && sort.beginTime < ps.beginTime; sort = sort.next)
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
		ps.next = PlaySound.freeList.next;
		PlaySound.freeList.next.prev = ps;
		ps.prev = PlaySound.freeList;
		PlaySound.freeList.next = ps;
	}

	public static void allocate(float[] origin, int entnum, int entchannel, int bufferId, float volume, float attenuation, float timeoffset)
	{
		var ps = PlaySound.get();

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
			PlaySound.add(ps);
		}
		else
			Console.Error.WriteLine("PlaySounds out of Limit");
	}
}