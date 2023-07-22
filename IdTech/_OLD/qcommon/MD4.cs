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

public class MD4
{
	// MD4 specific object variables
	//...........................................................................

	/**
	 * The size in bytes of the input block to the tranformation algorithm.
	 */
	private static readonly int BLOCK_LENGTH = 64; //    = 512 / 8;

	/**
	 * 4 32-bit words (interim result)
	 */
	private readonly int[] context = new int[4];

	/**
	 * Number of bytes processed so far mod. 2 power of 64.
	 */
	private long count;

	/**
	 * 512 bits input buffer = 16 x 32-bit words holds until reaches 512 bits.
	 */
	private readonly byte[] buffer = new byte[MD4.BLOCK_LENGTH];

	/**
	 * 512 bits work buffer = 16 x 32-bit words
	 */
	private readonly int[] X = new int[16];

	// Constructors
	//...........................................................................

	public MD4()
	{
		this.engineReset();
	}

	/**
	 *    This constructor is here to implement cloneability of this class.
	 */
	private MD4(MD4 md)
		: this()
	{
		this.context = (int[])md.context.Clone();
		this.buffer = (byte[])md.buffer.Clone();
		this.count = md.count;
	}

	// Cloneable method implementation
	//...........................................................................

	/**
	 * Returns a copy of this MD object.
	 */
	public object clone()
	{
		return new MD4(this);
	}

	// JCE methods
	//...........................................................................

	/**
	 * Resets this object disregarding any temporary data present at the
	 * time of the invocation of this call.
	 */
	public void engineReset()
	{
		// initial values of MD4 i.e. A, B, C, D
		// as per rfc-1320; they are low-order byte first
		this.context[0] = 0x67452301;
		this.context[1] = unchecked((int)0xEFCDAB89);
		this.context[2] = unchecked((int)0x98BADCFE);
		this.context[3] = 0x10325476;
		this.count = 0L;

		for (var i = 0; i < MD4.BLOCK_LENGTH; i++)
			this.buffer[i] = 0;
	}

	/**
	 * Continues an MD4 message digest using the input byte.
	 */
	public void engineUpdate(byte b)
	{
		// compute number of bytes still unhashed; ie. present in buffer
		var i = (int)(this.count % MD4.BLOCK_LENGTH);
		this.count++; // update number of bytes
		this.buffer[i] = b;

		if (i == MD4.BLOCK_LENGTH - 1)
			this.transform(this.buffer, 0);
	}

	/**
	 * MD4 block update operation.
	 * <p>
	 * Continues an MD4 message digest operation, by filling the buffer,
	 * transform(ing) data in 512-bit message block(s), updating the variables
	 * context and count, and leaving (buffering) the remaining bytes in buffer
	 * for the next update or finish.
	 *
	 * @param    input    input block
	 * @param    offset    start of meaningful bytes in input
	 * @param    len        count of bytes in input block to consider
	 */
	public void engineUpdate(byte[] input, int offset, int len)
	{
		// make sure we don't exceed input's allocated size/length
		if (offset < 0 || len < 0 || (long)offset + len > input.Length)
			throw new();

		// compute number of bytes still unhashed; ie. present in buffer
		var bufferNdx = (int)(this.count % MD4.BLOCK_LENGTH);
		this.count += len; // update number of bytes
		var partLen = MD4.BLOCK_LENGTH - bufferNdx;
		var i = 0;

		if (len >= partLen)
		{
			Array.Copy(input, offset, this.buffer, bufferNdx, partLen);

			this.transform(this.buffer, 0);

			for (i = partLen; i + MD4.BLOCK_LENGTH - 1 < len; i += MD4.BLOCK_LENGTH)
				this.transform(input, offset + i);

			bufferNdx = 0;
		}

		// buffer remaining input
		if (i < len)
			Array.Copy(input, offset + i, this.buffer, bufferNdx, len - i);
	}

	/**
	 * Completes the hash computation by performing final operations such
	 * as padding. At the return of this engineDigest, the MD engine is
	 * reset.
	 *
	 * @return the array of bytes for the resulting hash value.
	 */
	public byte[] engineDigest()
	{
		// pad output to 56 mod 64; as RFC1320 puts it: congruent to 448 mod 512
		var bufferNdx = (int)(this.count % MD4.BLOCK_LENGTH);
		var padLen = bufferNdx < 56 ? 56 - bufferNdx : 120 - bufferNdx;

		// padding is alwas binary 1 followed by binary 0s
		var tail = new byte[padLen + 8];
		tail[0] = (byte)0x80;

		// append length before final transform:
		// save number of bits, casting the long to an array of 8 bytes
		// save low-order byte first.
		for (var i = 0; i < 8; i++)
			tail[padLen + i] = (byte)((uint)(this.count * 8) >> (8 * i));

		this.engineUpdate(tail, 0, tail.Length);

		var result = new byte[16];

		// cast this MD4's context (array of 4 ints) into an array of 16 bytes.
		for (var i = 0; i < 4; i++)
		for (var j = 0; j < 4; j++)
			result[i * 4 + j] = (byte)((uint)this.context[i] >> (8 * j));

		// reset the engine
		this.engineReset();

		return result;
	}

	// own methods
	//...........................................................................

	/**
	 *    MD4 basic transformation.
	 *    <p>
	 *    Transforms context based on 512 bits from input block starting
	 *    from the offset'th byte.
	 *
	 *    @param    block    input sub-array.
	 *    @param    offset    starting position of sub-array.
	 */
	private void transform(byte[] block, int offset)
	{
		// encodes 64 bytes from input block into an array of 16 32-bit
		// entities. Use A as a temp var.
		for (var i = 0; i < 16; i++)
			this.X[i] = (block[offset++] & 0xFF) | ((block[offset++] & 0xFF) << 8) | ((block[offset++] & 0xFF) << 16) | ((block[offset++] & 0xFF) << 24);

		var A = this.context[0];
		var B = this.context[1];
		var C = this.context[2];
		var D = this.context[3];

		A = this.FF(A, B, C, D, this.X[0], 3);
		D = this.FF(D, A, B, C, this.X[1], 7);
		C = this.FF(C, D, A, B, this.X[2], 11);
		B = this.FF(B, C, D, A, this.X[3], 19);
		A = this.FF(A, B, C, D, this.X[4], 3);
		D = this.FF(D, A, B, C, this.X[5], 7);
		C = this.FF(C, D, A, B, this.X[6], 11);
		B = this.FF(B, C, D, A, this.X[7], 19);
		A = this.FF(A, B, C, D, this.X[8], 3);
		D = this.FF(D, A, B, C, this.X[9], 7);
		C = this.FF(C, D, A, B, this.X[10], 11);
		B = this.FF(B, C, D, A, this.X[11], 19);
		A = this.FF(A, B, C, D, this.X[12], 3);
		D = this.FF(D, A, B, C, this.X[13], 7);
		C = this.FF(C, D, A, B, this.X[14], 11);
		B = this.FF(B, C, D, A, this.X[15], 19);

		A = this.GG(A, B, C, D, this.X[0], 3);
		D = this.GG(D, A, B, C, this.X[4], 5);
		C = this.GG(C, D, A, B, this.X[8], 9);
		B = this.GG(B, C, D, A, this.X[12], 13);
		A = this.GG(A, B, C, D, this.X[1], 3);
		D = this.GG(D, A, B, C, this.X[5], 5);
		C = this.GG(C, D, A, B, this.X[9], 9);
		B = this.GG(B, C, D, A, this.X[13], 13);
		A = this.GG(A, B, C, D, this.X[2], 3);
		D = this.GG(D, A, B, C, this.X[6], 5);
		C = this.GG(C, D, A, B, this.X[10], 9);
		B = this.GG(B, C, D, A, this.X[14], 13);
		A = this.GG(A, B, C, D, this.X[3], 3);
		D = this.GG(D, A, B, C, this.X[7], 5);
		C = this.GG(C, D, A, B, this.X[11], 9);
		B = this.GG(B, C, D, A, this.X[15], 13);

		A = this.HH(A, B, C, D, this.X[0], 3);
		D = this.HH(D, A, B, C, this.X[8], 9);
		C = this.HH(C, D, A, B, this.X[4], 11);
		B = this.HH(B, C, D, A, this.X[12], 15);
		A = this.HH(A, B, C, D, this.X[2], 3);
		D = this.HH(D, A, B, C, this.X[10], 9);
		C = this.HH(C, D, A, B, this.X[6], 11);
		B = this.HH(B, C, D, A, this.X[14], 15);
		A = this.HH(A, B, C, D, this.X[1], 3);
		D = this.HH(D, A, B, C, this.X[9], 9);
		C = this.HH(C, D, A, B, this.X[5], 11);
		B = this.HH(B, C, D, A, this.X[13], 15);
		A = this.HH(A, B, C, D, this.X[3], 3);
		D = this.HH(D, A, B, C, this.X[11], 9);
		C = this.HH(C, D, A, B, this.X[7], 11);
		B = this.HH(B, C, D, A, this.X[15], 15);

		this.context[0] += A;
		this.context[1] += B;
		this.context[2] += C;
		this.context[3] += D;
	}

	// The basic MD4 atomic functions.

	private int FF(int a, int b, int c, int d, int x, int s)
	{
		var t = a + ((b & c) | (~b & d)) + x;

		return (int)(((uint)t << s) | ((uint)t >> (32 - s)));
	}

	private int GG(int a, int b, int c, int d, int x, int s)
	{
		var t = a + ((b & (c | d)) | (c & d)) + x + 0x5A827999;

		return (int)(((uint)t << s) | ((uint)t >> (32 - s)));
	}

	private int HH(int a, int b, int c, int d, int x, int s)
	{
		var t = a + (b ^ c ^ d) + x + 0x6ED9EBA1;

		return (int)(((uint)t << s) | ((uint)t >> (32 - s)));
	}

	/**
	 * Bugfixed, now works prima (RST).
	 */
	public static int Com_BlockChecksum(byte[] buffer, int length)
	{
		int val;
		MD4 md4 = new();

		md4.engineUpdate(buffer, 0, length);
		var data = md4.engineDigest();
		BinaryReader bb = new(new MemoryStream(data));
		val = bb.ReadInt32() ^ bb.ReadInt32() ^ bb.ReadInt32() ^ bb.ReadInt32();

		return val;
	}
}
