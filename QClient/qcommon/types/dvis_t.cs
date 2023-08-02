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
namespace QClient.qcommon.types;

public class dvis_t
{
	public dvis_t(BinaryReader bb)
	{
		this.numclusters = bb.ReadInt32();
		this.bitofs = new int[this.numclusters][];

		for (var i = 0; i < this.numclusters; i++)
		{
			this.bitofs[i] = new int[2];
			this.bitofs[i][0] = bb.ReadInt32();
			this.bitofs[i][1] = bb.ReadInt32();
		}
	}

	public int numclusters;
	public int[][] bitofs = new int[8][]; // bitofs[numclusters][2]
}