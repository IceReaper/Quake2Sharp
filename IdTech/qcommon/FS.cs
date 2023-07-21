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

using game;
using game.types;
using sys;
using System.Text;
using types;

public class FS
{
	/*
	 * ==================================================
	 * 
	 * QUAKE FILESYSTEM
	 * 
	 * ==================================================
	 */

	public class packfile_t
	{
		public static readonly int SIZE = 64;
		public static readonly int NAME_SIZE = 56;
		public string name; // char name[56]
		public int filepos;
		public int filelen;

		public string toString()
		{
			return this.name + " [ length: " + this.filelen + " pos: " + this.filepos + " ]";
		}
	}

	public class pack_t
	{
		public string filename;
		public Stream handle;
		public int numfiles;
		public Dictionary<string, packfile_t> files; // with packfile_t entries
	}

	public static string fs_gamedir;
	private static string fs_userdir;
	public static cvar_t fs_basedir;
	public static cvar_t fs_gamedirvar;

	public class filelink_t
	{
		public string from;
		public int fromlength;
		public string to;
	}

	// with filelink_t entries
	public static List<filelink_t> fs_links = new();

	public class searchpath_t
	{
		public string filename;
		public pack_t pack; // only one of filename or pack will be used
		public searchpath_t next;
	}

	public static searchpath_t fs_searchpaths;

	// without gamedirs
	public static searchpath_t fs_base_searchpaths;
	/*
	 * All of Quake's data access is through a hierchal file system, but the
	 * contents of the file system can be transparently merged from several
	 * sources.
	 * 
	 * The "base directory" is the path to the directory holding the quake.exe
	 * and all game directories. The sys_* files pass this to host_init in
	 * quakeparms_t->basedir. This can be overridden with the "-basedir" command
	 * line parm to allow code debugging in a different directory. The base
	 * directory is only used during filesystem initialization.
	 * 
	 * The "game directory" is the first tree on the search path and directory
	 * that all generated files (savegames, screenshots, demos, config files)
	 * will be saved to. This can be overridden with the "-game" command line
	 * parameter. The game directory can never be changed while quake is
	 * executing. This is a precacution against having a malicious server
	 * instruct clients to write files over areas they shouldn't.
	 *  
	 */

	/*
	 * CreatePath
	 * 
	 * Creates any directories needed to store the given filename.
	 */
	public static void CreatePath(string path)
	{
		var index = path.LastIndexOf('/');

		// -1 if not found and 0 means write to root
		if (index > 0)
		{
			if (!Directory.CreateDirectory(path[..index]).Exists)
				Com.Printf("can't create path \"" + path + '"' + "\n");
		}
	}

	public static int FileLength(string filename)
	{
		searchpath_t search;
		string netpath;
		pack_t pak;
		filelink_t link;

		FS.file_from_pak = 0;

		// check for links first
		for (var i = 0; i < FS.fs_links.Count; i++)
		{
			link = (filelink_t)FS.fs_links[i];

			if (filename[..link.fromlength] == link.@from[..link.fromlength])
			{
				netpath = link.to + filename[link.fromlength..];

				if (File.Exists(netpath))
				{
					Com.DPrintf("link file: " + netpath + '\n');

					return (int)new FileInfo(netpath).Length;
				}

				return -1;
			}
		}

		// search through the path, one element at a time

		for (search = FS.fs_searchpaths; search != null; search = search.next)
		{
			// is the element a pak file?
			if (search.pack != null)
			{
				// look through all the pak file elements
				pak = search.pack;
				filename = filename.ToLower();
				pak.files.TryGetValue(filename, out var entry);

				if (entry != null)
				{
					// found it!
					FS.file_from_pak = 1;
					Com.DPrintf("PackFile: " + pak.filename + " : " + filename + '\n');

					// open a new file on the pakfile
					if (!File.Exists(pak.filename))
						Com.Error(Defines.ERR_FATAL, "Couldn't reopen " + pak.filename);

					return entry.filelen;
				}
			}
			else
			{
				// check a file in the directory tree
				netpath = search.filename + '/' + filename;

				if (!File.Exists(netpath))
					continue;

				Com.DPrintf("FindFile: " + netpath + '\n');

				return (int)new FileInfo(netpath).Length;
			}
		}

		Com.DPrintf("FindFile: can't find " + filename + '\n');

		return -1;
	}

	public static int file_from_pak;

	/*
	 * FOpenFile
	 * 
	 * Finds the file in the search path. returns a RadomAccesFile. Used for
	 * streaming data out of either a pak file or a seperate file.
	 */
	public static BinaryReader FOpenFile(string filename)
	{
		searchpath_t search;
		string netpath;
		pack_t pak;
		filelink_t link;

		FS.file_from_pak = 0;

		// check for links first
		for (var i = 0; i < FS.fs_links.Count; i++)
		{
			link = (filelink_t)FS.fs_links[i];

			//			if (!strncmp (filename, link->from, link->fromlength))
			if (filename[..link.fromlength] == link.@from[..link.fromlength])
			{
				netpath = link.to + filename[link.fromlength..];

				if (File.Exists(netpath))
				{
					//Com.DPrintf ("link file: " + netpath +'\n');
					return new(File.OpenRead(netpath));
				}

				return null;
			}
		}

		//
		// search through the path, one element at a time
		//
		for (search = FS.fs_searchpaths; search != null; search = search.next)
		{
			// is the element a pak file?
			if (search.pack != null)
			{
				// look through all the pak file elements
				pak = search.pack;
				filename = filename.ToLower();
				pak.files.TryGetValue(filename, out var entry);

				if (entry != null)
				{
					// found it!
					FS.file_from_pak = 1;

					//Com.DPrintf ("PackFile: " + pak.filename + " : " +
					// filename + '\n');
					if (!File.Exists(pak.filename))
						Com.Error(Defines.ERR_FATAL, "Couldn't reopen " + pak.filename);

					if (pak.handle == null)
					{
						// hold the pakfile handle open
						pak.handle = File.OpenRead(pak.filename);
					}

					// open a new file on the pakfile

					pak.handle.Position = entry.filepos;
					var file = new byte[entry.filelen];
					pak.handle.Read(file);

					return new(new MemoryStream(file));
				}
			}
			else
			{
				// check a file in the directory tree
				netpath = search.filename + '/' + filename;

				if (!File.Exists(netpath))
					continue;

				//Com.DPrintf("FindFile: " + netpath +'\n');

				return new(File.OpenRead(netpath));
			}
		}

		//Com.DPrintf ("FindFile: can't find " + filename + '\n');
		return null;
	}

	// read in blocks of 64k
	public static readonly int MAX_READ = 0x10000;

	/*
	 * LoadFile
	 * 
	 * Filename are reletive to the quake search path a null buffer will just
	 * return the file content as byte[]
	 */
	public static byte[] LoadFile(string path)
	{
		BinaryReader file;

		byte[] buf = null;
		var len = 0;

		var index = path.IndexOf('\0');

		if (index != -1)
			path = path[..index];

		// look for it in the filesystem or pack files
		len = FS.FileLength(path);

		if (len < 1)
			return null;

		try
		{
			file = FS.FOpenFile(path);

			//Read(buf = new byte[len], len, h);
			buf = new byte[len];
			file.Read(buf);
			file.Close();
		}
		catch (Exception e)
		{
			Com.Error(Defines.ERR_FATAL, e.ToString());
		}

		return buf;
	}

	/*
	 * LoadMappedFile
	 * 
	 * Filename are reletive to the quake search path a null buffer will just
	 * return the file content as ByteBuffer (memory mapped)
	 */
	public static BinaryReader LoadMappedFile(string filename)
	{
		searchpath_t search;
		string netpath;
		pack_t pak;
		filelink_t link;

		var fileLength = 0;
		Stream input = null;
		BinaryReader buffer = null;

		FS.file_from_pak = 0;

		try
		{
			// check for links first
			for (var i = 0; i < FS.fs_links.Count; i++)
			{
				link = (filelink_t)FS.fs_links[i];

				if (filename[..link.fromlength] == link.@from[..link.fromlength])
				{
					netpath = link.to + filename[link.fromlength..];

					if (File.Exists(netpath))
					{
						input = File.OpenRead(netpath);
						fileLength = (int)input.Length;
						var tmp = new byte[fileLength];
						input.Read(tmp);
						buffer = new(new MemoryStream(tmp));
						input.Close();

						return buffer;
					}

					return null;
				}
			}

			//
			// search through the path, one element at a time
			//
			for (search = FS.fs_searchpaths; search != null; search = search.next)
			{
				// is the element a pak file?
				if (search.pack != null)
				{
					// look through all the pak file elements
					pak = search.pack;
					filename = filename.ToLower();
					pak.files.TryGetValue(filename, out var entry);

					if (entry != null)
					{
						// found it!
						FS.file_from_pak = 1;

						//Com.DPrintf ("PackFile: " + pak.filename + " : " +
						// filename + '\n');
						if (!File.Exists(pak.filename))
							Com.Error(Defines.ERR_FATAL, "Couldn't reopen " + pak.filename);

						if (pak.handle == null)
						{
							// hold the pakfile handle open
							pak.handle = File.OpenRead(pak.filename);
						}

						pak.handle.Position = entry.filepos;
						var tmp = new byte[entry.filelen];
						pak.handle.Read(tmp);

						return new(new MemoryStream(tmp));
					}
				}
				else
				{
					// check a file in the directory tree
					netpath = search.filename + '/' + filename;

					if (!File.Exists(netpath))
						continue;

					//Com.DPrintf("FindFile: " + netpath +'\n');
					input = File.OpenRead(netpath);
					fileLength = (int)input.Length;
					var tmp = new byte[fileLength];
					input.Read(tmp);
					buffer = new(new MemoryStream(tmp));
					input.Close();

					return buffer;
				}
			}
		}
		catch (Exception)
		{
		}

		try
		{
			if (input != null)
				input.Close();
		}
		catch (Exception)
		{
		}

		return null;
	}

	/*
	 * FreeFile
	 */
	public static void FreeFile(byte[] buffer)
	{
		buffer = null;
	}

	private static readonly int IDPAKHEADER = ('K' << 24) + ('C' << 16) + ('A' << 8) + 'P';
	private static readonly int MAX_FILES_IN_PACK = 4096;

	// buffer for C-Strings char[56]
	private static readonly byte[] tmpText = new byte[packfile_t.NAME_SIZE];

	/*
	 * LoadPackFile
	 * 
	 * Takes an explicit (not game tree related) path to a pak file.
	 * 
	 * Loads the header and directory, adding the files at the beginning of the
	 * list so they override previous pack files.
	 */
	private static pack_t LoadPackFile(string packfile)
	{
		dpackheader_t header;
		Dictionary<string, packfile_t> newfiles;
		Stream file;
		var numpackfiles = 0;
		pack_t pack = null;

		//		unsigned checksum;
		//
		try
		{
			file = File.OpenRead(packfile);
			BinaryReader packhandle = new(file);

			if (packhandle == null || file.Length < 1)
				return null;

			//
			header = new();
			header.ident = packhandle.ReadInt32();
			header.dirofs = packhandle.ReadInt32();
			header.dirlen = packhandle.ReadInt32();

			if (header.ident != FS.IDPAKHEADER)
				Com.Error(Defines.ERR_FATAL, packfile + " is not a packfile");

			numpackfiles = header.dirlen / packfile_t.SIZE;

			if (numpackfiles > FS.MAX_FILES_IN_PACK)
				Com.Error(Defines.ERR_FATAL, packfile + " has " + numpackfiles + " files");

			newfiles = new();

			file.Position = header.dirofs;

			// parse the directory
			packfile_t entry = null;

			for (var i = 0; i < numpackfiles; i++)
			{
				packhandle.Read(FS.tmpText);

				entry = new();
				entry.name = Encoding.ASCII.GetString(FS.tmpText).Split((char)0)[0];
				entry.filepos = packhandle.ReadInt32();
				entry.filelen = packhandle.ReadInt32();

				newfiles.Add(entry.name.ToLower(), entry);
			}
		}
		catch (Exception e)
		{
			Com.DPrintf(e.Message + '\n');

			return null;
		}

		pack = new();
		pack.filename = new(packfile);
		pack.handle = file;
		pack.numfiles = numpackfiles;
		pack.files = newfiles;

		Com.Printf("Added packfile " + packfile + " (" + numpackfiles + " files)\n");

		return pack;
	}

	/*
	 * AddGameDirectory
	 * 
	 * Sets fs_gamedir, adds the directory to the head of the path, then loads
	 * and adds pak1.pak pak2.pak ...
	 */
	private static void AddGameDirectory(string dir)
	{
		int i;
		searchpath_t search;
		searchpath_t pakSearch;
		pack_t pak;
		string pakfile;

		FS.fs_gamedir = new(dir);

		//
		// add the directory to the search path
		// ensure fs_userdir is first in searchpath
		search = new();
		search.filename = new(dir);

		if (FS.fs_searchpaths != null)
		{
			search.next = FS.fs_searchpaths.next;
			FS.fs_searchpaths.next = search;
		}
		else
			FS.fs_searchpaths = search;

		//
		// add any pak files in the format pak0.pak pak1.pak, ...
		//
		for (i = 0; i < 10; i++)
		{
			pakfile = dir + "/pak" + i + ".pak";

			if (!File.Exists(pakfile))
				continue;

			pak = FS.LoadPackFile(pakfile);

			if (pak == null)
				continue;

			pakSearch = new();
			pakSearch.pack = pak;
			pakSearch.filename = "";
			pakSearch.next = search.next;
			search.next = pakSearch;
		}
	}

	/*
	 * Gamedir
	 * 
	 * Called to find where to write a file (demos, savegames, etc)
	 */
	public static string Gamedir()
	{
		return FS.fs_userdir != null ? FS.fs_userdir : Globals.BASEDIRNAME;
	}

	/*
	 * ExecAutoexec
	 */
	public static void ExecAutoexec()
	{
		var dir = FS.fs_userdir;

		string name;

		if (dir != null && dir.Length > 0)
			name = dir + "/autoexec.cfg";
		else
			name = FS.fs_basedir.@string + '/' + Globals.BASEDIRNAME + "/autoexec.cfg";

		var canthave = Defines.SFF_SUBDIR | Defines.SFF_HIDDEN | Defines.SFF_SYSTEM;

		if (Sys.FindAll(name, 0, canthave) != null)
			Cbuf.AddText("exec autoexec.cfg\n");
	}

	/*
	 * SetGamedir
	 * 
	 * Sets the gamedir and path to a different directory.
	 */
	public static void SetGamedir(string dir)
	{
		searchpath_t next;

		if (dir.IndexOf("..") != -1 || dir.IndexOf("/") != -1 || dir.IndexOf("\\") != -1 || dir.IndexOf(":") != -1)
		{
			Com.Printf("Gamedir should be a single filename, not a path\n");

			return;
		}

		//
		// free up any current game dir info
		//
		while (FS.fs_searchpaths != FS.fs_base_searchpaths)
		{
			if (FS.fs_searchpaths.pack != null)
			{
				try
				{
					FS.fs_searchpaths.pack.handle.Close();
				}
				catch (Exception e)
				{
					Com.DPrintf(e.Message + '\n');
				}

				// clear the hashtable
				FS.fs_searchpaths.pack.files.Clear();
				FS.fs_searchpaths.pack.files = null;
				FS.fs_searchpaths.pack = null;
			}

			next = FS.fs_searchpaths.next;
			FS.fs_searchpaths = null;
			FS.fs_searchpaths = next;
		}

		//
		// flush all data, so it will be forced to reload
		//
		if (Globals.dedicated != null && Globals.dedicated.value == 0.0f)
			Cbuf.AddText("vid_restart\nsnd_restart\n");

		FS.fs_gamedir = FS.fs_basedir.@string + '/' + dir;

		if (dir.Equals(Globals.BASEDIRNAME) || dir.Length == 0)
		{
			Cvar.FullSet("gamedir", "", Defines.CVAR_SERVERINFO | Defines.CVAR_NOSET);
			Cvar.FullSet("game", "", Defines.CVAR_LATCH | Defines.CVAR_SERVERINFO);
		}
		else
		{
			Cvar.FullSet("gamedir", dir, Defines.CVAR_SERVERINFO | Defines.CVAR_NOSET);
			FS.AddGameDirectory(FS.fs_basedir.@string + '/' + dir);
		}
	}

	/*
	 * Link_f
	 * 
	 * Creates a filelink_t
	 */
	public static void Link_f()
	{
		filelink_t entry = null;

		if (Cmd.Argc() != 3)
		{
			Com.Printf("USAGE: link <from> <to>\n");

			return;
		}

		// see if the link already exists
		for (var i = 0; i < FS.fs_links.Count; i++)
		{
			entry = (filelink_t)FS.fs_links[i];

			if (entry.from.Equals(Cmd.Argv(1)))
			{
				if (Cmd.Argv(2).Length < 1)
				{
					// delete it
					FS.fs_links.RemoveAt(i);

					return;
				}

				entry.to = new(Cmd.Argv(2));

				return;
			}
		}

		// create a new link if the <to> is not empty
		if (Cmd.Argv(2).Length > 0)
		{
			entry = new();
			entry.from = new(Cmd.Argv(1));
			entry.fromlength = entry.from.Length;
			entry.to = new(Cmd.Argv(2));
			FS.fs_links.Add(entry);
		}
	}

	/*
	 * ListFiles
	 */
	public static string[] ListFiles(string findname, int musthave, int canthave)
	{
		string[] list = null;

		var files = Sys.FindAll(findname, musthave, canthave);

		if (files != null)
		{
			list = new string[files.Length];

			for (var i = 0; i < files.Length; i++)
				list[i] = files[i];
		}

		return list;
	}

	/*
	 * Dir_f
	 */
	public static void Dir_f()
	{
		string path = null;
		string findname = null;
		var wildcard = "*.*";
		string[] dirnames;

		if (Cmd.Argc() != 1)
			wildcard = Cmd.Argv(1);

		while ((path = FS.NextPath(path)) != null)
		{
			var tmp = findname;

			findname = path + '/' + wildcard;

			if (tmp != null)
				tmp.Replace("\\", "/");

			Com.Printf("Directory of " + findname + '\n');
			Com.Printf("----\n");

			dirnames = FS.ListFiles(findname, 0, 0);

			if (dirnames != null)
			{
				var index = 0;

				for (var i = 0; i < dirnames.Length; i++)
				{
					if ((index = dirnames[i].LastIndexOf('/')) > 0)
						Com.Printf(dirnames[i][(index + 1)..] + '\n');
					else
						Com.Printf(dirnames[i] + '\n');
				}
			}

			Com.Printf("\n");
		}
	}

	/*
	 * Path_f
	 */
	public static void Path_f()
	{
		searchpath_t s;
		filelink_t link;

		Com.Printf("Current search path:\n");

		for (s = FS.fs_searchpaths; s != null; s = s.next)
		{
			if (s == FS.fs_base_searchpaths)
				Com.Printf("----------\n");

			if (s.pack != null)
				Com.Printf(s.pack.filename + " (" + s.pack.numfiles + " files)\n");
			else
				Com.Printf(s.filename + '\n');
		}

		Com.Printf("\nLinks:\n");

		for (var i = 0; i < FS.fs_links.Count; i++)
		{
			link = (filelink_t)FS.fs_links[i];
			Com.Printf(link.from + " : " + link.to + '\n');
		}
	}

	/*
	 * NextPath
	 * 
	 * Allows enumerating all of the directories in the search path
	 */
	public static string NextPath(string prevpath)
	{
		searchpath_t s;
		string prev;

		if (prevpath == null || prevpath.Length == 0)
			return FS.fs_gamedir;

		prev = null;

		for (s = FS.fs_searchpaths; s != null; s = s.next)
		{
			if (s.pack != null)
				continue;

			if (prevpath == prev)
				return s.filename;

			prev = s.filename;
		}

		return null;
	}

	/*
	 * InitFilesystem
	 */
	public static void InitFilesystem()
	{
		Cmd.AddCommand("path", () => { FS.Path_f(); });

		Cmd.AddCommand("link", () => { FS.Link_f(); });

		Cmd.AddCommand("dir", () => { FS.Dir_f(); });

		FS.fs_userdir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.Quake2Sharp";
		FS.CreatePath(FS.fs_userdir + "/");
		FS.AddGameDirectory(FS.fs_userdir);

		//
		// basedir <path>
		// allows the game to run from outside the data tree
		//
		FS.fs_basedir = Cvar.Get("basedir", ".", Defines.CVAR_NOSET);

		//
		// start up with baseq2 by default
		//
		FS.AddGameDirectory(FS.fs_basedir.@string + '/' + Globals.BASEDIRNAME);

		// any set gamedirs will be freed up to here
		FS.markBaseSearchPaths();

		// check for game override
		FS.fs_gamedirvar = Cvar.Get("game", "", Defines.CVAR_LATCH | Defines.CVAR_SERVERINFO);

		if (FS.fs_gamedirvar.@string.Length > 0)
			FS.SetGamedir(FS.fs_gamedirvar.@string);
	}

	public static void markBaseSearchPaths()
	{
		// any set gamedirs will be freed up to here
		FS.fs_base_searchpaths = FS.fs_searchpaths;
	}

	//	RAFAEL
	/*
	 * Developer_searchpath
	 */
	public static int Developer_searchpath(int who)
	{
		// PMM - warning removal
		//	 char *start;
		searchpath_t s;

		for (s = FS.fs_searchpaths; s != null; s = s.next)
		{
			if (s.filename.IndexOf("xatrix") != -1)
				return 1;

			if (s.filename.IndexOf("rogue") != -1)
				return 2;
		}

		return 0;
	}
}