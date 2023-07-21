/*
 * Copyright (C) 1997-2001 Id Software, Inc.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or (at
 * your option) any later version.
 *
 * This program is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 *
 * See the GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA
 * 02111-1307, USA.
 */

using Quake2Sharp;
using Quake2Sharp.client;
using Quake2Sharp.game.types;
using Quake2Sharp.qcommon;
using Quake2Sharp.sys;
using System.IO.Compression;
using System.Text;

namespace IdTech.common;

/*
 * =======================================================================
 *
 * The Quake II file system, implements generic file system operations
 * as well as the .pak file format and support for .pk3 files.
 *
 * =======================================================================
 */
public static class filesystem
{
	private const string IDPAKHEADER = "PACK";

	private class Link
	{
		public required string From { get; init; }
		public required string To { get; init; }
	}

	private class PakFile
	{
		public required string Name { get; init; }
		public required int Offset { get; init; }
		public required int Size { get; init; }
	}

	public class SearchPath
	{
		public required string Path { get; init; }
	}

	private abstract class PackSearchPath : SearchPath
	{
		public bool Protected;
	}

	private class PakSearchPath : PackSearchPath
	{
		public required Stream Stream { get; init; }
		public required PakFile[] Files { get; init; }
	}

	private class ZipSearchPath : PackSearchPath
	{
		public required ZipArchive Zip { get; init; }
	}

	private enum PackFormat
	{
		Pak,
		Zip
	}

	private static List<Link> fs_links = new();
	public static List<SearchPath> fs_searchPaths = new();
	private static List<SearchPath> fs_baseSearchPaths = new();

	/* Pack formats / suffixes. */
	private static Dictionary<string, PackFormat> fs_packtypes = new()
	{
		["pak"] = PackFormat.Pak,
		["pk2"] = PackFormat.Zip,
		["pk3"] = PackFormat.Zip,
		["pkz"] = PackFormat.Zip,
		["zip"] = PackFormat.Zip
	};

	public static string datadir = string.Empty;
	private static string fs_gamedir = string.Empty;
	public static bool file_from_protected_pack;

	public static readonly cvar_t fs_basedir = cvar.Cvar_Get("basedir", ".", Defines.CVAR_NOSET);
	private static readonly cvar_t fs_cddir = cvar.Cvar_Get("cddir", string.Empty, Defines.CVAR_NOSET);
	public static readonly cvar_t fs_gamedirvar = cvar.Cvar_Get("game", string.Empty, Defines.CVAR_LATCH | Defines.CVAR_SERVERINFO);
	private static readonly cvar_t fs_debug = cvar.Cvar_Get("fs_debug", "0", 0);

	// --------

	// Raw search path, the actual search path is build from this one.
	private static List<string> fs_rawPath = new();

	// --------

	/*
	 * All of Quake's data access is through a hierchal file system, but the
	 * contents of the file system can be transparently merged from several
	 * sources.
	 *
	 * The "base directory" is the path to the directory holding the quake.exe and
	 * all game directories.  The sys_* files pass this to host_init in
	 * quakeparms_t->basedir.  This can be overridden with the "-basedir" command
	 * line parm to allow code debugging in a different directory.  The base
	 * directory is only used during filesystem initialization.
	 *
	 * The "game directory" is the first tree on the search path and directory that
	 * all generated files (savegames, screenshots, demos, config files) will be
	 * saved to.  This can be overridden with the "-game" command line parameter.
	 * The game directory can never be changed while quake is executing.  This is
	 * a precacution against having a malicious server instruct clients to write
	 * files over areas they shouldn't.
	 *
	 */

	/*
	 * Returns the path up to, but not including the last '/'.
	 */
	private static string Com_FilePath(string path)
	{
		return path.Contains('/') ? path[..path.LastIndexOf('/')] : path;
	}

	public static int FS_FileLength(string path)
	{
		using var stream = FS_FOpenFile(path, false);

		if (stream == null)
			return 0;

		return (int)stream.Length;
	}

	/*
	 * Creates any directories needed to store the given filename.
	 */
	public static void FS_CreatePath(string path)
	{
		FS_DPrintf($"FS_CreatePath({path})\n");

		if (path.Contains(".."))
			clientserver.Com_Printf($"WARNING: refusing to create relative path '{path}'.\n");
		else
			Directory.CreateDirectory(Com_FilePath(path));
	}

	private static void FS_DPrintf(string msg)
	{
		if (fs_debug.value != 0)
			clientserver.Com_Printf(msg);
	}

	public static string FS_Gamedir()
	{
		return fs_gamedir;
	}

	/*
	 * Finds the file in the search path. Returns filesize and an open FILE *. Used
	 * for streaming data out of either a pak file or a separate file.
	 */
	public static Stream? FS_FOpenFile(string rawname, bool gamedir_only)
	{
		// Remove self references and empty dirs from the requested path.
		// ZIPs and PAKs don't support them, but they may be hardcoded in
		// some custom maps or models.
		var name = string.Empty;

		for (int input = 0, output = 0; input < rawname.Length; input++)
		{
			// Remove self reference.
			if (rawname[input] == '.')
			{
				if (output > 0)
				{
					// Inside the path.
					if (name[output - 1] == '/' && input + 1 < rawname.Length && rawname[input + 1] == '/')
					{
						input++;
						continue;
					}
				}

				// At the beginning. Note: This is save because the Quake II
				// VFS doesn't have a current working dir. Paths are always
				// absolute.
				else if (input + 1 < rawname.Length && rawname[input + 1] == '/')
					continue;
			}

			// Empty dir.
			if (rawname[input] == '/' && input + 1 < rawname.Length && rawname[input + 1] == '/')
				continue;

			// Paths starting with a /. I'm not sure if this is
			// a problem. It shouldn't hurt to remove the leading
			// slash, though.
			if (rawname[input] == '/' && output == 0)
				continue;

			name += rawname[input];
			output++;
		}

		file_from_protected_pack = false;

		foreach (var search in fs_searchPaths)
		{
			if (gamedir_only && !search.Path.Contains(FS_Gamedir()))
				continue;

			// Evil hack for maps.lst and players/
			// TODO: A flag to ignore paks would be better
			if (fs_gamedirvar.@string.Length == 0 && search is PackSearchPath)
			{
				if (name == "maps.lst" || name.StartsWith("players/"))
					continue;
			}

			switch (search)
			{
				/* Search inside a pack file. */
				case PakSearchPath pak:
					foreach (var pakFile in pak.Files)
					{
						if (!pakFile.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
							continue;

						/* Found it! */
						if (fs_debug.value != 0)
							clientserver.Com_Printf($"FS_FOpenFile: '{rawname}' (found in '{pak.Path}').\n");

						if (pak.Protected)
							file_from_protected_pack = true;

						pak.Stream.Position = pakFile.Offset;

						return new MemoryStream(new BinaryReader(pak.Stream).ReadBytes(pakFile.Size));
					}

					break;

				/* Search inside a pack file. */
				case ZipSearchPath zip:
					foreach (var zipFile in zip.Zip.Entries)
					{
						if (!zipFile.FullName.Equals(name, StringComparison.OrdinalIgnoreCase))
							continue;

						/* Found it! */
						if (fs_debug.value != 0)
							clientserver.Com_Printf($"FS_FOpenFile: '{rawname}' (found in '{zip.Path}').\n");

						if (zip.Protected)
							file_from_protected_pack = true;

						return new MemoryStream(new BinaryReader(zipFile.Open()).ReadBytes((int)zipFile.Length));
					}

					break;

				/* Search in a directory tree. */
				default:
					var path = $"{search.Path}/{rawname}";

					try
					{
						var stream = File.Open(path, FileMode.Open, FileAccess.Read);

						if (fs_debug.value != 0)
							clientserver.Com_Printf($"FS_FOpenFile: '{rawname}' (found in '{search.Path}').\n");

						return stream;
					}
					catch
					{
						// ignored
					}

					break;
			}
		}

		if (fs_debug.value != 0)
			clientserver.Com_Printf($"FS_FOpenFile: couldn't find '{rawname}'.\n");

		/* Couldn't open. */
		return null;
	}

	/*
	 * Filename are relative to the quake search path. A null buffer will just
	 * return the file length without loading.
	 */
	public static byte[]? FS_LoadFile(string path)
	{
		var stream = FS_FOpenFile(path, false);

		if (stream == null)
			return null;

		var data = new BinaryReader(stream).ReadBytes((int)stream.Length);
		stream.Dispose();

		return data;
	}

	private static void FS_FreeSearchPaths(List<SearchPath> searchPaths)
	{
		foreach (var searchPath in searchPaths)
		{
			(searchPath as PakSearchPath)?.Stream.Dispose();
			(searchPath as ZipSearchPath)?.Zip.Dispose();
		}

		searchPaths.Clear();
	}

	/*
	 * Takes an explicit (not game tree related) path to a pak file.
	 *
	 * Loads the header and directory, adding the files at the beginning of the
	 * list so they override previous pack files.
	 */
	private static PakSearchPath? FS_LoadPAK(string path)
	{
		Stream stream;

		try
		{
			stream = File.Open(path, FileMode.Open, FileAccess.Read);
		}
		catch
		{
			return null;
		}

		var reader = new BinaryReader(stream);

		var ident = Encoding.ASCII.GetString(reader.ReadBytes(4));

		if (ident != IDPAKHEADER)
		{
			stream.Dispose();
			clientserver.Com_Error(Defines.ERR_FATAL, $"FS_LoadPAK: '{path}' is not a pak file");
		}

		var directoryOffset = reader.ReadInt32();
		var directoryLength = reader.ReadInt32();

		stream.Position = directoryOffset;

		var pak = new PakSearchPath
		{
			Path = path,
			Stream = stream,
			Files = new PakFile[directoryLength / 64]
		};

		/* Parse the directory. */
		for (var i = 0; i < pak.Files.Length; i++)
		{
			pak.Files[i] = new PakFile
			{
				Name = Encoding.UTF8.GetString(reader.ReadBytes(56)).Split('\0', 2)[0],
				Offset = reader.ReadInt32(),
				Size = reader.ReadInt32()
			};
		}

		clientserver.Com_Printf($"Added packfile '{path}' ({pak.Files.Length} files).\n");

		return pak;
	}

	/*
	 * Takes an explicit (not game tree related) path to a pack file.
	 *
	 * Loads the header and directory, adding the files at the beginning of the list
	 * so they override previous pack files.
	 */
	private static ZipSearchPath? FS_LoadPK2(string path)
	{
		ZipArchive zip;

		try
		{
			zip = new ZipArchive(File.Open(path, FileMode.Open, FileAccess.Read));
		}
		catch
		{
			return null;
		}

		var pk2 = new ZipSearchPath
		{
			Path = path,
			Zip = zip
		};

		clientserver.Com_Printf($"Added packfile '{pk2.Path}' ({pk2.Zip.Entries.Count} files).\n");

		return pk2;
	}

	/*
	 * Allows enumerating all of the directories in the search path.
	 */
	public static string? FS_NextPath(string? lastPath)
	{
		if (lastPath == null)
			return fs_gamedir;

		var prev = fs_gamedir;

		foreach (var searchPath in fs_searchPaths)
		{
			if (searchPath is PackSearchPath)
				continue;

			if (lastPath == prev)
				return searchPath.Path;

			prev = searchPath.Path;
		}

		return null;
	}

	private static void FS_Path_f()
	{
		var totalFiles = 0;

		clientserver.Com_Printf("Current search path:\n");

		foreach (var searchPath in fs_searchPaths)
		{
			switch (searchPath)
			{
				case PakSearchPath pak:
					clientserver.Com_Printf($"{pak.Path} ({pak.Files.Length} files)\n");
					totalFiles += pak.Files.Length;
					break;

				case ZipSearchPath pk2:
					clientserver.Com_Printf($"{pk2.Path} ({pk2.Zip.Entries.Count} files)\n");
					totalFiles += pk2.Zip.Entries.Count;
					break;

				default:
					clientserver.Com_Printf($"{searchPath.Path}\n");
					break;
			}
		}

		clientserver.Com_Printf("\n");

		for (var i = 0; i < fs_links.Count; i++)
			clientserver.Com_Printf($"Link {i}: '{fs_links[i].From}' . '{fs_links[i].To}'.\n");


		clientserver.Com_Printf("----------------------\n");

		clientserver.Com_Printf($"{totalFiles} files in PAK/PK2/PK3/ZIP files.\n");
	}

	/*
	 * Creates a filelink_t.
	 */
	private static void FS_Link_f()
	{
		if (cmdparser.Cmd_Argc() != 3)
		{
			clientserver.Com_Printf("USAGE: link <from> <to>\n");
			return;
		}

		/* See if the link already exists. */
		foreach (var link in fs_links.ToArray())
		{
			if (link.From != cmdparser.Cmd_Argv(1))
				continue;

			/* Delete it. */
			fs_links.Remove(link);
			break;
		}

		/* Create a new link. */
		fs_links.Add(new Link
		{
			From = cmdparser.Cmd_Argv(1),
			To = cmdparser.Cmd_Argv(2)
		});
	}

	/*
	 * Create a list of files that match a criteria.
	 */
	public static string[] FS_ListFiles(string findname)
	{
		var list = new List<string>();
		var s = Sys.FindFirst(findname, 0, 0);

		while (s != null)
		{
			list.Add(s);
			s = Sys.FindNext();
		}

		Sys.FindClose();

		return list.ToArray();
	}

	/*
	 * Create a list of files that match a criteria.
	 * Searches are relative to the game directory and use all the search paths
	 * including .pak and .pk3 files.
	 */
	public static string[] FS_ListFiles2(string findname)
	{
		var list = new List<string>();

		foreach (var searchPath in fs_searchPaths)
		{
			switch (searchPath)
			{
				case PakSearchPath pak:
					foreach (var entry in pak.Files)
					{
						if (glob.glob_match(findname, entry.Name))
							list.Add(entry.Name);
					}

					break;

				case ZipSearchPath pk2:
					foreach (var entry in pk2.Zip.Entries)
					{
						if (glob.glob_match(findname, entry.FullName))
							list.Add(entry.FullName);
					}

					break;

				default:
					list.AddRange(FS_ListFiles($"{searchPath.Path}/{findname}"));
					break;
			}
		}

		list.Sort();

		/* Delete duplicates. */
		return list.Distinct().ToArray();
	}

	/*
	 * Comparator for mod sorting
	 */
	private static int Q_sort_modcmp(string p1, string p2)
	{
		var first_mods = new[] { Globals.BASEDIRNAME, "xatrix", "rogue", "ctf" };

		foreach (var first_mod in first_mods)
		{
			if (first_mod == p1)
				return -1;

			if (first_mod == p2)
				return 1;
		}

		return string.CompareOrdinal(p1, p2);
	}

	/*
	 * Combs all Raw search paths to find game dirs containing PAK/PK2/PK3 files.
	 * Returns an alphabetized array of unique relative dir names.
	 */
	private static string[] FS_ListMods()
	{
		var modnames = new List<string>();

		// iterate over all Raw paths
		foreach (var search in fs_rawPath)
		{
			if (search.Length == 0)
				continue;

			var searchPath = search;

			// make sure this Raw path ends with a '/' otherwise FS_ListFiles will open its parent dir
			if (!searchPath.EndsWith('/'))
				searchPath += '/';

			// iterate over the children of this Raw path (unless we've already got enough mods)
			foreach (var directory in FS_ListFiles($"{searchPath}*"))
			{
				var modname = directory[(directory.LastIndexOf('/') + 1)..];

				if (modnames.Contains(modname))
					continue;

				// iterate over supported pack types
				foreach (var suffix in fs_packtypes.Keys)
				{
					var packsinchilddir = FS_ListFiles($"{directory}/*.{suffix}");

					// if this dir has some pack files, add it
					if (packsinchilddir.Length <= 0)
						continue;

					modnames.Add(modname);
					break;
				}
			}
		}

		modnames.Sort(Q_sort_modcmp);

		return modnames.ToArray();
	}

	/*
	 * Directory listing.
	 */
	private static void FS_Dir_f()
	{
		/* Check for pattern in arguments. */
		var filter = cmdparser.Cmd_Argc() != 1 ? cmdparser.Cmd_Argv(1) : "*.*";

		/* Scan search paths and list files. */
		for (var path = FS_NextPath(null); path != null; path = FS_NextPath(path))
		{
			var filteredPath = $"{path}/{filter}";
			clientserver.Com_Printf($"Directory of '{filteredPath}'.\n");
			clientserver.Com_Printf("----\n");

			foreach (var file in FS_ListFiles(filteredPath))
				clientserver.Com_Printf($"{(file.Contains('/') ? file[(file.LastIndexOf('/') + 1)..] : file)}\n");

			clientserver.Com_Printf("\n");
		}
	}

	// --------

	/*
	 * This function returns true if a real file (e.g. not something
	 * in a pak, somthing in the file system itself) exists in the
	 * current gamedir.
	 */

	private static bool FS_FileInGamedir(string file)
	{
		return File.Exists($"{fs_gamedir}/{file}");
	}

	/*
	 * This function loads the given .pak / .pk3 File from the
	 * fs_gamedir. There's no need to load from other dirs since
	 * fs_gamedir is the only dir written to at runtime.
	 */
	private static bool FS_AddPackFromGamedir(string pack)
	{
		var path = $"{fs_gamedir}/{pack}";

		// Depending on filetype we must load it as .pak or .pk3.
		foreach (var (suffix, format) in fs_packtypes)
		{
			// Not the current filetype, next one please.
			if (!pack.EndsWith(suffix))
				continue;

			PackSearchPath? packFile = null;

			switch (format)
			{
				case PackFormat.Pak:
					packFile = FS_LoadPAK(path);
					break;

				case PackFormat.Zip:
					packFile = FS_LoadPK2(path);

					break;
			}

			// Couldn't load it.
			if (packFile == null)
				return false;

			// Add it.
			fs_searchPaths.Insert(0, packFile);
		}

		// Apparently we didn't load anything.
		return false;
	}

	private static string? FS_GetNextRawPath(string? lastRawPath)
	{
		string? prev = null;

		foreach (var rawPath in fs_rawPath)
		{
			if (lastRawPath == prev)
				return rawPath;

			prev = rawPath;
		}

		return null;
	}

	private static void FS_AddDirToSearchPath(string dir)
	{
		// The directory must not end with an /. It would
		// f*ck up the logic in other parts of the game...
		if (dir.EndsWith('/') || dir.EndsWith('\\'))
			dir = dir[..^1];

		// Set the current directory as game directory. This
		// is somewhat fragile since the game directory MUST
		// be the last directory added to the search path.
		fs_gamedir = dir;

		FS_CreatePath(fs_gamedir);

		// Add the directory itself.
		fs_searchPaths.Insert(0, new SearchPath
		{
			Path = dir
		});

		// Numbered packs contain the official game data, they
		// need to be added first and are marked protected.
		// Files from protected packs are never offered for
		// download.
		var basePacks = new List<string>();

		for (var i = 0;; i++)
		{
			var any = false;

			foreach (var (suffix, format) in fs_packtypes)
			{
				var path = $"{dir}/pak{i}.{suffix}";
				PackSearchPath? pack = null;

				switch (format)
				{
					case PackFormat.Pak:
						pack = FS_LoadPAK(path);
						break;

					case PackFormat.Zip:
						pack = FS_LoadPK2(path);
						break;
				}

				if (pack == null)
					continue;

				pack.Protected = true;
				basePacks.Add(path);
				fs_searchPaths.Insert(0, pack);

				any = true;
			}

			if (!any)
				break;
		}

		// All other pack files are added after the numbered packs.
		// They aren't sorted in any way, but added in the same
		// sequence as they're returned by FS_ListFiles. This is
		// fragile and file system dependent. We cannot change
		// this, since it might break existing installations.
		foreach (var file in FS_ListFiles($"{dir}/*"))
		{
			foreach (var (suffix, format) in fs_packtypes)
			{
				// Nothing here, next pak type please.
				if (!file.EndsWith(suffix))
					continue;

				// Sort out numbered packs. This is as inefficient as
				// it can be, but it doesn't matter. This is done only
				// once at client or game startup.
				if (basePacks.Contains(file))
					continue;

				PackSearchPath? pack = null;

				switch (format)
				{
					case PackFormat.Pak:
						pack = FS_LoadPAK(file);
						break;

					case PackFormat.Zip:
						pack = FS_LoadPK2(file);
						break;
				}

				if (pack == null)
					continue;

				pack.Protected = false;
				fs_searchPaths.Insert(0, pack);
			}
		}
	}

	private static void FS_BuildGenericSearchPath()
	{
		foreach (var search in fs_rawPath)
			FS_AddDirToSearchPath($"{search}/{Globals.BASEDIRNAME}");

		// Until here we've added the generic directories to the
		// search path. Save the current head node so we can
		// distinguish generic and specialized directories.
		fs_baseSearchPaths = fs_searchPaths.ToList();
		fs_searchPaths.Clear();

		// We need to create the game directory.
		Directory.CreateDirectory(fs_gamedir);

		// We need to create the screenshot directory since the
		// render dll doesn't link the filesystem stuff.
		var path = $"{fs_gamedir}/scrnshot";
		Directory.CreateDirectory(path);
	}

	public static void FS_BuildGameSpecificSearchPath(string dir)
	{
		// Write the config. Otherwise changes made by the
		// current mod are lost.
		if (!Globals.DEDICATED_ONLY)
			Cl.WriteConfiguration();

		// empty string means baseq2
		if (dir.Length == 0)
			dir = Globals.BASEDIRNAME;

		// This is against PEBCAK. The user may give us paths like
		// xatrix/ or even /home/stupid/quake2/xatrix.
		if (dir.StartsWith('.') || dir.Contains("..") || dir.Contains('/') || dir.Contains('\\'))
		{
			clientserver.Com_Printf("Gamedir should be a single filename, not a path.\n");
			return;
		}

		// We may already have specialised directories in our search
		// path. This can happen if the server changes the mod. Let's
		// remove them.
		FS_FreeSearchPaths(fs_searchPaths);
		fs_searchPaths.AddRange(fs_baseSearchPaths);

		// Enforce a renderer and sound backend restart to
		// purge all internal caches. This is rather hacky
		// but Quake II doesn't have a better mechanism...
		if (frame.dedicated == null || frame.dedicated.value == 0)
			cmdparser.Cbuf_AddText("vid_restart\nsnd_restart\n");

		// The game was reset to baseq2. Nothing to do here.
		if (dir == Globals.BASEDIRNAME)
		{
			cvar.Cvar_FullSet("gamedir", string.Empty, Defines.CVAR_SERVERINFO | Defines.CVAR_NOSET);
			cvar.Cvar_FullSet("game", string.Empty, Defines.CVAR_LATCH | Defines.CVAR_SERVERINFO);

			// fs_gamedir must be reset to the last
			// dir of the generic search path.
			fs_gamedir = fs_baseSearchPaths[0].Path;
		}
		else
		{
			cvar.Cvar_FullSet("gamedir", dir, Defines.CVAR_SERVERINFO | Defines.CVAR_NOSET);

			foreach (var search in fs_rawPath)
				FS_AddDirToSearchPath($"{search}/{dir}");
		}

		// Create the game directory.
		Directory.CreateDirectory(fs_gamedir);

		// We need to create the screenshot directory since the
		// render dll doesn't link the filesystem stuff.
		Directory.CreateDirectory($"{fs_gamedir}/scrnshot");

		// the gamedir has changed, so read in the corresponding configs
		frame.Qcommon_ExecConfigs(false);

		if (Globals.DEDICATED_ONLY)
			return;

		// This function is called whenever the game cvar changes =>
		// the player wants to switch to another mod. In that case the
		// list of music tracks needs to be loaded again (=> tracks
		// are possibly from the new mod dir)
		ogg.OGG_InitTrackList();

		// ...and the current list of maps in the "start network server" menu is
		// cleared so that it will be re-initialized when the menu is accessed
		Menu.mapnames = new string[Menu.nummaps = 0];

		// Start the demoloop, if requested. This is kind of hacky: Normaly the
		// demo loop would be started by the menu, after changeing the 'game'
		// cvar. However, the demo loop is implemented by aliases. Since the
		// game isn't changed right after the cvar is set (but when the control
		// flow enters this function) the aliases evaulate to the wrong game.
		// Work around that by injection the demo loop into the command buffer
		// here, right after the game was changed. Do it only when the game was
		// changed though the menu, otherwise we might break into the demo loop
		// after we've received a latched cvar from the server.
		if (!Menu.menu_startdemoloop)
			return;

		cmdparser.Cbuf_AddText("d1\n");
		Menu.menu_startdemoloop = false;
	}

	// --------

	private static void FS_AddDirToRawPath(string rawDir, bool required)
	{
		var dir = Sys.Realpath(rawDir);

		// Get the realpath.
		if (dir == null)
		{
			if (required)
				clientserver.Com_Error(Defines.ERR_FATAL, $"Couldn't add required directory {rawDir} to search path\n");

			return;
		}

		// Convert backslashes to forward slashes.
		dir = dir.Replace('\\', '/');

		// Make sure that the dir doesn't end with a slash.
		if (dir.EndsWith('/'))
			dir = dir[..^1];

		// Bail out if the dir was already added.
		foreach (var search in fs_rawPath)
		{
			if (search == dir)
				return;
		}

		// Add the directory.
		fs_rawPath.Insert(0, dir);
	}

	private static void FS_BuildRawPath()
	{
		// Add $HOME/.yq2, MUST be the last dir! Required,
		// otherwise the config cannot be written.
		if (!frame.is_portable)
		{
			var homeDir = Sys.GetHomeDir();

			if (homeDir != null)
				FS_AddDirToRawPath(homeDir, true);
		}

		// Add binary dir. Required, because the renderer
		// libraries are loaded from it.
		var binaryDir = Sys.GetWorkDir();

		if (binaryDir is { Length: > 0 })
			FS_AddDirToRawPath(binaryDir, true);

		// Add data dir. Required, when the user gives us
		// a data dir he expects it in a working state.
		FS_AddDirToRawPath(datadir, true);

		// The CD must be the last directory of the path,
		// otherwise we cannot be sure that the game won't
		// stream the videos from the CD. Required, if the
		// user sets a CD path, he expects data getting
		// read from the CD.
		if (fs_cddir.@string.Length > 0)
			FS_AddDirToRawPath(fs_cddir.@string, true);
	}

	// --------

	public static void FS_InitFilesystem()
	{
		// Register FS commands.
		cmdparser.Cmd_AddCommand("path", FS_Path_f);
		cmdparser.Cmd_AddCommand("link", FS_Link_f);
		cmdparser.Cmd_AddCommand("dir", FS_Dir_f);

		// Deprecation warning, can be removed at a later time.
		if (fs_basedir.@string != ".")
		{
			clientserver.Com_Printf("+set basedir is deprecated, use -datadir instead\n");
			datadir = fs_basedir.@string;
		}
		else if (datadir.Length == 0)
			datadir = ".";

		// Build search path
		FS_BuildRawPath();
		FS_BuildGenericSearchPath();
		FS_BuildGameSpecificSearchPath(fs_gamedirvar.@string);

		// we still need to get the list of OGG tracks for background music
		if (!Globals.DEDICATED_ONLY)
			ogg.OGG_InitTrackList();

		// Debug output
		clientserver.Com_Printf($"Using '{fs_gamedir}' for writing.\n");
	}

	public static void FS_ShutdownFilesystem()
	{
		FS_FreeSearchPaths(fs_searchPaths);
		FS_FreeSearchPaths(fs_baseSearchPaths);
	}
}
