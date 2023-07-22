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

namespace IdTech.common;

/*
 * =======================================================================
 *
 * Global string matching
 *
 * =======================================================================
 */
public static class glob
{
	/*
	 * Like glob_match, but match PATTERN against any final segment of TEXT.
	 */
	private static bool glob_match_after_star(string pattern, string text)
	{
		var p = 0;
		var t = 0;
		var c = '\0';

		while (p < pattern.Length)
		{
			c = pattern[p];

			if (c != '?' && c != '*')
				break;

			if (c == '?' && t++ == text.Length)
				return false;

			p++;
		}

		if (p == pattern.Length)
			return true;

		var c1 = c == '\\' ? pattern[p] : c;

		while (t < text.Length)
		{
			if ((c == '[' || text[t] == c1) && glob.glob_match(pattern[(p - 1)..], text[t..]))
				return true;
		}

		return false;
	}

	/* Match the pattern PATTERN against the string TEXT;
	 * return 1 if it matches, 0 otherwise.
	 *
	 * A match means the entire string TEXT is used up in matching.
	 *
	 * In the pattern string, `*' matches any sequence of characters,
	 * `?' matches any character, [SET] matches any character in the specified set,
	 * [!SET] matches any character not in the specified set.
	 *
	 * A set is composed of characters or ranges; a range looks like
	 * character hyphen character (as in 0-9 or A-Z).
	 * [0-9a-zA-Z_] is the set of characters allowed in C identifiers.
	 * Any other character in the pattern must be matched exactly.
	 *
	 * To suppress the special syntactic significance of any of `[]*?!-\',
	 * and match the character exactly, precede it with a `\'.
	 */
	public static bool glob_match(string pattern, string text)
	{
		var p = 0;
		var t = 0;

		while (p < pattern.Length)
		{
			var c = pattern[p++];

			switch (c)
			{
				case '?':
					if (t == text.Length)
						return false;

					++t;

					break;

				case '\\':
					if (p == pattern.Length && t == text.Length)
						break;

					if (p == pattern.Length || t == text.Length)
						return false;

					if (pattern[p++] != text[t++])
						return false;

					break;

				case '*':
					return glob.glob_match_after_star(pattern[p..], text[t..]);

				case '[':
				{
					if (t == text.Length)
						return false;

					var c1 = text[t++];
					var invert = pattern[p] == '!' || pattern[p] == '^';

					if (invert)
						p++;

					if (p == pattern.Length)
						return false;

					c = pattern[p++];

					while (true)
					{
						var cstart = c;
						var cend = c;

						if (c == '\\')
						{
							if (p == pattern.Length)
								return false;

							cstart = pattern[p++];
							cend = cstart;
						}

						if (p == pattern.Length)
							return false;

						c = pattern[p++];

						if (c == '-' && (p == pattern.Length || pattern[p] != ']'))
						{
							if (p == pattern.Length)
								return false;

							cend = pattern[p++];

							if (cend == '\\')
							{
								if (p == pattern.Length)
									return false;

								cend = pattern[p++];
							}

							if (p == pattern.Length)
								return false;

							c = pattern[p++];
						}

						if (c1 >= cstart && c1 <= cend)
						{
							/* Skip the rest of the [...] construct that already matched.  */
							while (c != ']')
							{
								if (p == pattern.Length)
									return false;

								c = pattern[p++];

								if (c == '\\')
									p++;
							}

							if (invert)
								return false;

							break;
						}

						if (c == ']')
							break;
					}

					if (!invert)
						return false;

					break;
				}

				default:
					if (t == text.Length || c != text[t++])
						return false;

					break;
			}
		}

		return t == text.Length;
	}
}
