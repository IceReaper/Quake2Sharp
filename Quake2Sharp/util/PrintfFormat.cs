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
namespace Quake2Sharp.util
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Text;

	/**
 * PrintfFormat allows the formatting of an array of
 * objects embedded within a string.  Primitive types
 * must be passed using wrapper types.  The formatting
 * is controlled by a control string.
 *<p>
 * A control string is a string that contains a
 * control specification.  The control specification
 * starts at the first percent sign (%) in the string,
 * provided that this percent sign
 *<ol>
 *<li>is not escaped protected by a matching % or is
 * not an escape % character,
 *<li>is not at the end of the format string, and
 *<li>precedes a sequence of characters that parses as
 * a valid control specification.
 *</ol>
 *</p><p>
 * A control specification usually takes the form:
 *<pre> % ['-+ #0]* [0..9]* { . [0..9]* }+
 *                { [hlL] }+ [idfgGoxXeEcs]
 *</pre>
 * There are variants of this basic form that are
 * discussed below.</p>
 *<p>
 * The format is composed of zero or more directives
 * defined as follows:
 *<ul>
 *<li>ordinary characters, which are simply copied to
 * the output stream;
 *<li>escape sequences, which represent non-graphic
 * characters; and
 *<li>conversion specifications,  each of which
 * results in the fetching of zero or more arguments.
 *</ul></p>
 *<p>
 * The results are undefined if there are insufficient
 * arguments for the format.  Usually an unchecked
 * exception will be thrown.  If the format is
 * exhausted while arguments remain, the excess
 * arguments are evaluated but are otherwise ignored.
 * In format strings containing the % form of
 * conversion specifications, each argument in the
 * argument list is used exactly once.</p>
 * <p>
 * Conversions can be applied to the <code>n</code>th
 * argument after the format in the argument list,
 * rather than to the next unused argument.  In this
 * case, the conversion characer % is replaced by the
 * sequence %<code>n</code>$, where <code>n</code> is
 * a decimal integer giving the position of the
 * argument in the argument list.</p>
 * <p>
 * In format strings containing the %<code>n</code>$
 * form of conversion specifications, each argument
 * in the argument list is used exactly once.</p>
 *
 *<h4>Escape Sequences</h4>
 *<p>
 * The following table lists escape sequences and
 * associated actions on display devices capable of
 * the action.
 *<table>
 *<tr><th align=left>Sequence</th>
 *    <th align=left>Name</th>
 *    <th align=left>Description</th></tr>
 *<tr><td>\\</td><td>backlash</td><td>None.
 *</td></tr>
 *<tr><td>\a</td><td>alert</td><td>Attempts to alert
 *          the user through audible or visible
 *          notification.
 *</td></tr>
 *<tr><td>\b</td><td>backspace</td><td>Moves the
 *          printing position to one column before
 *          the current position, unless the
 *          current position is the start of a line.
 *</td></tr>
 *<tr><td>\f</td><td>form-feed</td><td>Moves the
 *          printing position to the initial 
 *          printing position of the next logical
 *          page.
 *</td></tr>
 *<tr><td>\n</td><td>newline</td><td>Moves the
 *          printing position to the start of the
 *          next line.
 *</td></tr>
 *<tr><td>\r</td><td>carriage-return</td><td>Moves
 *          the printing position to the start of
 *          the current line.
 *</td></tr>
 *<tr><td>\t</td><td>tab</td><td>Moves the printing
 *          position to the next implementation-
 *          defined horizontal tab position.
 *</td></tr>
 *<tr><td>\v</td><td>vertical-tab</td><td>Moves the
 *          printing position to the start of the
 *          next implementation-defined vertical
 *          tab position.
 *</td></tr>
 *</table></p>
 *<h4>Conversion Specifications</h4>
 *<p>
 * Each conversion specification is introduced by
 * the percent sign character (%).  After the character
 * %, the following appear in sequence:</p>
 *<p>
 * Zero or more flags (in any order), which modify the
 * meaning of the conversion specification.</p>
 *<p>
 * An optional minimum field width.  If the converted
 * value has fewer characters than the field width, it
 * will be padded with spaces by default on the left;
 * t will be padded on the right, if the left-
 * adjustment flag (-), described below, is given to
 * the field width.  The field width takes the form
 * of a decimal integer.  If the conversion character
 * is s, the field width is the the minimum number of
 * characters to be printed.</p>
 *<p>
 * An optional precision that gives the minumum number
 * of digits to appear for the d, i, o, x or X
 * conversions (the field is padded with leading
 * zeros); the number of digits to appear after the
 * radix character for the e, E, and f conversions,
 * the maximum number of significant digits for the g
 * and G conversions; or the maximum number of
 * characters to be written from a string is s and S
 * conversions.  The precision takes the form of an
 * optional decimal digit string, where a null digit
 * string is treated as 0.  If a precision appears
 * with a c conversion character the precision is
 * ignored.
 * </p>
 *<p>
 * An optional h specifies that a following d, i, o,
 * x, or X conversion character applies to a type 
 * short argument (the argument will be promoted
 * according to the integral promotions and its value
 * converted to type short before printing).</p>
 *<p>
 * An optional l (ell) specifies that a following
 * d, i, o, x, or X conversion character applies to a
 * type long argument.</p>
 *<p>
 * A field width or precision may be indicated by an
 * asterisk (*) instead of a digit string.  In this
 * case, an integer argument supplised the field width
 * precision.  The argument that is actually converted
 * is not fetched until the conversion letter is seen,
 * so the the arguments specifying field width or
 * precision must appear before the argument (if any)
 * to be converted.  If the precision argument is
 * negative, it will be changed to zero.  A negative
 * field width argument is taken as a - flag, followed
 * by a positive field width.</p>
 * <p>
 * In format strings containing the %<code>n</code>$
 * form of a conversion specification, a field width
 * or precision may be indicated by the sequence
 * *<code>m</code>$, where m is a decimal integer
 * giving the position in the argument list (after the
 * format argument) of an integer argument containing
 * the field width or precision.</p>
 * <p>
 * The format can contain either numbered argument
 * specifications (that is, %<code>n</code>$ and
 * *<code>m</code>$), or unnumbered argument
 * specifications (that is % and *), but normally not
 * both.  The only exception to this is that %% can
 * be mixed with the %<code>n</code>$ form.  The
 * results of mixing numbered and unnumbered argument
 * specifications in a format string are undefined.</p>
 *
 *<h4>Flag Characters</h4>
 *<p>
 * The flags and their meanings are:</p>
 *<dl>
 * <dt>'<dd> integer portion of the result of a
 *      decimal conversion (%i, %d, %f, %g, or %G) will
 *      be formatted with thousands' grouping
 *      characters.  For other conversions the flag
 *      is ignored.  The non-monetary grouping
 *      character is used.
 * <dt>-<dd> result of the conversion is left-justified
 *      within the field.  (It will be right-justified
 *      if this flag is not specified).</td></tr>
 * <dt>+<dd> result of a signed conversion always
 *      begins with a sign (+ or -).  (It will begin
 *      with a sign only when a negative value is
 *      converted if this flag is not specified.)
 * <dt>&lt;space&gt;<dd> If the first character of a
 *      signed conversion is not a sign, a space
 *      character will be placed before the result.
 *      This means that if the space character and +
 *      flags both appear, the space flag will be
 *      ignored.
 * <dt>#<dd> value is to be converted to an alternative
 *      form.  For c, d, i, and s conversions, the flag
 *      has no effect.  For o conversion, it increases
 *      the precision to force the first digit of the
 *      result to be a zero.  For x or X conversion, a
 *      non-zero result has 0x or 0X prefixed to it,
 *      respectively.  For e, E, f, g, and G
 *      conversions, the result always contains a radix
 *      character, even if no digits follow the radix
 *      character (normally, a decimal point appears in
 *      the result of these conversions only if a digit
 *      follows it).  For g and G conversions, trailing
 *      zeros will not be removed from the result as
 *      they normally are.
 * <dt>0<dd> d, i, o, x, X, e, E, f, g, and G
 *      conversions, leading zeros (following any
 *      indication of sign or base) are used to pad to
 *      the field width;  no space padding is
 *      performed.  If the 0 and - flags both appear,
 *      the 0 flag is ignored.  For d, i, o, x, and X
 *      conversions, if a precision is specified, the
 *      0 flag will be ignored. For c conversions,
 *      the flag is ignored.
 *</dl>
 *
 *<h4>Conversion Characters</h4>
 *<p>
 * Each conversion character results in fetching zero
 * or more arguments.  The results are undefined if
 * there are insufficient arguments for the format.
 * Usually, an unchecked exception will be thrown.
 * If the format is exhausted while arguments remain,
 * the excess arguments are ignored.</p>
 *
 *<p>
 * The conversion characters and their meanings are:
 *</p>
 *<dl>
 * <dt>d,i<dd>The int argument is converted to a
 *        signed decimal in the style [-]dddd.  The
 *        precision specifies the minimum number of
 *        digits to appear;  if the value being
 *        converted can be represented in fewer
 *        digits, it will be expanded with leading
 *        zeros.  The default precision is 1.  The
 *        result of converting 0 with an explicit
 *        precision of 0 is no characters.
 * <dt>o<dd> The int argument is converted to unsigned
 *        octal format in the style ddddd.  The
 *        precision specifies the minimum number of
 *        digits to appear;  if the value being
 *        converted can be represented in fewer
 *        digits, it will be expanded with leading
 *        zeros.  The default precision is 1.  The
 *        result of converting 0 with an explicit
 *        precision of 0 is no characters.
 * <dt>x<dd> The int argument is converted to unsigned
 *        hexadecimal format in the style dddd;  the
 *        letters abcdef are used.  The precision
 *        specifies the minimum numberof digits to
 *        appear; if the value being converted can be
 *        represented in fewer digits, it will be
 *        expanded with leading zeros.  The default
 *        precision is 1.  The result of converting 0
 *        with an explicit precision of 0 is no
 *        characters.
 * <dt>X<dd> Behaves the same as the x conversion
 *        character except that letters ABCDEF are
 *        used instead of abcdef.
 * <dt>f<dd> The floating point number argument is
 *        written in decimal notation in the style
 *        [-]ddd.ddd, where the number of digits after
 *        the radix character (shown here as a decimal
 *        point) is equal to the precision
 *        specification.  A Locale is used to determine
 *        the radix character to use in this format.
 *        If the precision is omitted from the
 *        argument, six digits are written after the
 *        radix character;  if the precision is
 *        explicitly 0 and the # flag is not specified,
 *        no radix character appears.  If a radix
 *        character appears, at least 1 digit appears
 *        before it.  The value is rounded to the
 *        appropriate number of digits.
 * <dt>e,E<dd>The floating point number argument is
 *        written in the style [-]d.ddde{+-}dd
 *        (the symbols {+-} indicate either a plus or
 *        minus sign), where there is one digit before
 *        the radix character (shown here as a decimal
 *        point) and the number of digits after it is
 *        equal to the precision.  A Locale is used to
 *        determine the radix character to use in this
 *        format.  When the precision is missing, six
 *        digits are written after the radix character;
 *        if the precision is 0 and the # flag is not
 *        specified, no radix character appears.  The
 *        E conversion will produce a number with E
 *        instead of e introducing the exponent.  The
 *        exponent always contains at least two digits.
 *        However, if the value to be written requires
 *        an exponent greater than two digits,
 *        additional exponent digits are written as
 *        necessary.  The value is rounded to the
 *        appropriate number of digits.
 * <dt>g,G<dd>The floating point number argument is
 *        written in style f or e (or in sytle E in the
 *        case of a G conversion character), with the
 *        precision specifying the number of
 *        significant digits.  If the precision is
 *        zero, it is taken as one.  The style used
 *        depends on the value converted:  style e
 *        (or E) will be used only if the exponent
 *        resulting from the conversion is less than
 *        -4 or greater than or equal to the precision.
 *        Trailing zeros are removed from the result.
 *        A radix character appears only if it is
 *        followed by a digit.
 * <dt>c,C<dd>The integer argument is converted to a
 *        char and the result is written.
 *
 * <dt>s,S<dd>The argument is taken to be a string and
 *        bytes from the string are written until the
 *        end of the string or the number of bytes 
 *        indicated by the precision specification of
 *        the argument is reached.  If the precision
 *        is omitted from the argument, it is taken to
 *        be infinite, so all characters up to the end
 *        of the string are written.
 * <dt>%<dd>Write a % character;  no argument is
 *        converted.
 *</dl>
 *<p>
 * If a conversion specification does not match one of
 * the above forms, an IllegalArgumentException is
 * thrown and the instance of PrintfFormat is not
 * created.</p>
 *<p>
 * If a floating point value is the internal
 * representation for infinity, the output is
 * [+]Infinity, where Infinity is either Infinity or
 * Inf, depending on the desired output string length.
 * Printing of the sign follows the rules described
 * above.</p>
 *<p>
 * If a floating point value is the internal
 * representation for "not-a-number," the output is
 * [+]NaN.  Printing of the sign follows the rules
 * described above.</p>
 *<p>
 * In no case does a non-existent or small field width
 * cause truncation of a field;  if the result of a
 * conversion is wider than the field width, the field
 * is simply expanded to contain the conversion result.
 *</p>
 *<p>
 * The behavior is like printf.  One exception is that
 * the minimum number of exponent digits is 3 instead
 * of 2 for e and E formats when the optional L is used
 * before the e, E, g, or G conversion character.  The
 * optional L does not imply conversion to a long long
 * double. </p>
 * <p>
 * The biggest divergence from the C printf
 * specification is in the use of 16 bit characters.
 * This allows the handling of characters beyond the
 * small ASCII character set and allows the utility to
 * interoperate correctly with the rest of the
 * runtime environment.</p>
 *<p>
 * Omissions from the C printf specification are
 * numerous.  All the known omissions are present
 * because never uses bytes to represent
 * characters and does not have pointers:</p>
 *<ul>
 * <li>%c is the same as %C.
 * <li>%s is the same as %S.
 * <li>u, p, and n conversion characters. 
 * <li>%ws format.
 * <li>h modifier applied to an n conversion character.
 * <li>l (ell) modifier applied to the c, n, or s
 * conversion characters.
 * <li>ll (ell ell) modifier to d, i, o, u, x, or X
 * conversion characters.
 * <li>ll (ell ell) modifier to an n conversion
 * character.
 * <li>c, C, d,i,o,u,x, and X conversion characters
 * apply to Byte, Character, Short, Integer, Long
 * types.
 * <li>f, e, E, g, and G conversion characters apply
 * to Float and Double types.
 * <li>s and S conversion characters apply to String
 * types.
 * <li>All other reference types can be formatted
 * using the s or S conversion characters only.
 *</ul>
 * <p>
 * Most of this specification is quoted from the Unix
 * man page for the sprintf utility.</p>
 *
 * @author Allan Jacobs
 * @version 1
 * Release 1: Initial release.
 * Release 2: Asterisk field widths and precisions    
 *            %n$ and *m$
 *            Bug fixes
 *              g format fix (2 digits in e form corrupt)
 *              rounding in f format implemented
 *              round up when digit not printed is 5
 *              formatting of -0.0f
 *              round up/down when last digits are 50000...
 */
	public class PrintfFormat
	{
		/**
	 * Constructs an array of control specifications
	 * possibly preceded, separated, or followed by
	 * ordinary strings.  Control strings begin with
	 * unpaired percent signs.  A pair of successive
	 * percent signs designates a single percent sign in
	 * the format.
	 * @param fmtArg  Control string.
	 * @exception IllegalArgumentException if the control
	 * string is null, zero length, or otherwise
	 * malformed.
	 */
		public PrintfFormat(string fmtArg)
		{
			var ePos = 0;
			ConversionSpecification sFmt = null;
			var unCS = this.nonControl(fmtArg, 0);

			if (unCS != null)
			{
				sFmt = new();
				sFmt.setLiteral(unCS);
				this.vFmt.Add(sFmt);
			}

			while (this.cPos != -1 && this.cPos < fmtArg.Length)
			{
				for (ePos = this.cPos + 1; ePos < fmtArg.Length; ePos++)
				{
					var c = (char)0;
					c = fmtArg[ePos];

					if (c == 'i')
						break;

					if (c == 'd')
						break;

					if (c == 'f')
						break;

					if (c == 'g')
						break;

					if (c == 'G')
						break;

					if (c == 'o')
						break;

					if (c == 'x')
						break;

					if (c == 'X')
						break;

					if (c == 'e')
						break;

					if (c == 'E')
						break;

					if (c == 'c')
						break;

					if (c == 's')
						break;

					if (c == '%')
						break;
				}

				ePos = Math.Min(ePos + 1, fmtArg.Length);
				sFmt = new(fmtArg.Substring(this.cPos, ePos - this.cPos));
				this.vFmt.Add(sFmt);
				unCS = this.nonControl(fmtArg, ePos);

				if (unCS != null)
				{
					sFmt = new();
					sFmt.setLiteral(unCS);
					this.vFmt.Add(sFmt);
				}
			}
		}

		/**
	 * Return a substring starting at
	 * <code>start</code> and ending at either the end
	 * of the String <code>s</code>, the next unpaired
	 * percent sign, or at the end of the String if the
	 * last character is a percent sign.
	 * @param s  Control string.
	 * @param start Position in the string
	 *     <code>s</code> to begin looking for the start
	 *     of a control string.
	 * @return the substring from the start position
	 *     to the beginning of the control string.
	 */
		private string nonControl(string s, int start)
		{
			this.cPos = s.IndexOf("%", start);

			if (this.cPos == -1)
				this.cPos = s.Length;

			return s.Substring(start, this.cPos - start);
		}

		/**
	 * Format an array of objects.  Byte, Short,
	 * Integer, Long, Float, Double, and Character
	 * arguments are treated as wrappers for primitive
	 * types.
	 * @param o The array of objects to format.
	 * @return  The formatted String.
	 */
		public string sprintf(object[] o)
		{
			var c = (char)0;
			var i = 0;
			StringBuilder sb = new();

			foreach (var cs in this.vFmt)
			{
				c = cs.getConversionCharacter();

				if (c == '\0')
					sb.Append(cs.getLiteral());
				else if (c == '%')
					sb.Append("%");
				else
				{
					if (cs.isPositionalSpecification())
					{
						i = cs.getArgumentPosition() - 1;

						if (cs.isPositionalFieldWidth())
						{
							var ifw = cs.getArgumentPositionForFieldWidth() - 1;
							cs.setFieldWidthWithArg((int)o[ifw]);
						}

						if (cs.isPositionalPrecision())
						{
							var ipr = cs.getArgumentPositionForPrecision() - 1;
							cs.setPrecisionWithArg((int)o[ipr]);
						}
					}
					else
					{
						if (cs.isVariableFieldWidth())
						{
							cs.setFieldWidthWithArg((int)o[i]);
							i++;
						}

						if (cs.isVariablePrecision())
						{
							cs.setPrecisionWithArg((int)o[i]);
							i++;
						}
					}

					if (o[i] is byte)
						sb.Append(cs.internalsprintf((byte)o[i]));
					else if (o[i] is short)
						sb.Append(cs.internalsprintf((short)o[i]));
					else if (o[i] is int)
						sb.Append(cs.internalsprintf((int)o[i]));
					else if (o[i] is long)
						sb.Append(cs.internalsprintf((long)o[i]));
					else if (o[i] is float)
						sb.Append(cs.internalsprintf((float)o[i]));
					else if (o[i] is double)
						sb.Append(cs.internalsprintf((double)o[i]));
					else if (o[i] is char)
						sb.Append(cs.internalsprintf((char)o[i]));
					else if (o[i] is string)
						sb.Append(cs.internalsprintf((string)o[i]));
					else
						sb.Append(cs.internalsprintf(o[i]));

					if (!cs.isPositionalSpecification())
						i++;
				}
			}

			return sb.ToString();
		}

		/**
	 * Format nothing.  Just use the control string.
	 * @return  the formatted String.
	 */
		public string sprintf()
		{
			var c = (char)0;
			StringBuilder sb = new();

			foreach (var cs in this.vFmt)
			{
				c = cs.getConversionCharacter();

				if (c == '\0')
					sb.Append(cs.getLiteral());
				else if (c == '%')
					sb.Append("%");
			}

			return sb.ToString();
		}

		/**
	 * Format an int.
	 * @param x The int to format.
	 * @return  The formatted String.
	 * @exception IllegalArgumentException if the
	 *     conversion character is f, e, E, g, G, s,
	 *     or S.
	 */
		public string sprintf(int x)
		{
			var c = (char)0;
			StringBuilder sb = new();

			foreach (var cs in this.vFmt)
			{
				c = cs.getConversionCharacter();

				if (c == '\0')
					sb.Append(cs.getLiteral());
				else if (c == '%')
					sb.Append("%");
				else
					sb.Append(cs.internalsprintf(x));
			}

			return sb.ToString();
		}

		/**
	 * Format an long.
	 * @param x The long to format.
	 * @return  The formatted String.
	 * @exception IllegalArgumentException if the
	 *     conversion character is f, e, E, g, G, s,
	 *     or S.
	 */
		public string sprintf(long x)
		{
			var c = (char)0;
			StringBuilder sb = new();

			foreach (var cs in this.vFmt)
			{
				c = cs.getConversionCharacter();

				if (c == '\0')
					sb.Append(cs.getLiteral());
				else if (c == '%')
					sb.Append("%");
				else
					sb.Append(cs.internalsprintf(x));
			}

			return sb.ToString();
		}

		/**
	 * Format a double.
	 * @param x The double to format.
	 * @return  The formatted String.
	 * @exception IllegalArgumentException if the
	 *     conversion character is c, C, s, S,
	 *     d, d, x, X, or o.
	 */
		public string sprintf(double x)
		{
			var c = (char)0;
			StringBuilder sb = new();

			foreach (var cs in this.vFmt)
			{
				c = cs.getConversionCharacter();

				if (c == '\0')
					sb.Append(cs.getLiteral());
				else if (c == '%')
					sb.Append("%");
				else
					sb.Append(cs.internalsprintf(x));
			}

			return sb.ToString();
		}

		/**
	 * Format a String.
	 * @param x The String to format.
	 * @return  The formatted String.
	 * @exception IllegalArgumentException if the
	 *   conversion character is neither s nor S.
	 */
		public string sprintf(string x)
		{
			var c = (char)0;
			StringBuilder sb = new();

			foreach (var cs in this.vFmt)
			{
				c = cs.getConversionCharacter();

				if (c == '\0')
					sb.Append(cs.getLiteral());
				else if (c == '%')
					sb.Append("%");
				else
					sb.Append(cs.internalsprintf(x));
			}

			return sb.ToString();
		}

		/**
	 * Format an Object.  Convert wrapper types to
	 * their primitive equivalents and call the
	 * appropriate internal formatting method. Convert
	 * Strings using an internal formatting method for
	 * Strings. Otherwise use the default formatter
	 * (use toString).
	 * @param x the Object to format.
	 * @return  the formatted String.
	 * @exception IllegalArgumentException if the
	 *    conversion character is inappropriate for
	 *    formatting an unwrapped value.
	 */
		public string sprintf(object x)
		{
			var c = (char)0;
			StringBuilder sb = new();

			foreach (var cs in this.vFmt)
			{
				c = cs.getConversionCharacter();

				if (c == '\0')
					sb.Append(cs.getLiteral());
				else if (c == '%')
					sb.Append("%");
				else
				{
					if (x is byte)
						sb.Append(cs.internalsprintf((byte)x));
					else if (x is short)
						sb.Append(cs.internalsprintf((short)x));
					else if (x is int)
						sb.Append(cs.internalsprintf((int)x));
					else if (x is long)
						sb.Append(cs.internalsprintf((long)x));
					else if (x is float)
						sb.Append(cs.internalsprintf((float)x));
					else if (x is double)
						sb.Append(cs.internalsprintf((double)x));
					else if (x is char)
						sb.Append(cs.internalsprintf((char)x));
					else if (x is string)
						sb.Append(cs.internalsprintf((string)x));
					else
						sb.Append(cs.internalsprintf(x));
				}
			}

			return sb.ToString();
		}

		/**
	 *<p>
	 * ConversionSpecification allows the formatting of
	 * a single primitive or object embedded within a
	 * string.  The formatting is controlled by a
	 * format string.  Only one primitive or
	 * object can be formatted at a time.
	 *<p>
	 * A format string is a string that contains
	 * a control string.  The control string starts at
	 * the first percent sign (%) in the string,
	 * provided that this percent sign
	 *<ol>
	 *<li>is not escaped protected by a matching % or
	 *     is not an escape % character,
	 *<li>is not at the end of the format string, and
	 *<li>precedes a sequence of characters that parses
	 *     as a valid control string.
	 *</ol>
	 *<p>
	 * A control string takes the form:
	 *<pre> % ['-+ #0]* [0..9]* { . [0..9]* }+
	 *                { [hlL] }+ [idfgGoxXeEcs]
	 *</pre>
	 *<p>
	 * The behavior is like printf.  One (hopefully the
	 * only) exception is that the minimum number of
	 * exponent digits is 3 instead of 2 for e and E
	 * formats when the optional L is used before the
	 * e, E, g, or G conversion character.  The 
	 * optional L does not imply conversion to a long
	 * long double.
	 */
		private class ConversionSpecification
		{
			/**
		 * Constructor.  Used to prepare an instance
		 * to hold a literal, not a control string.
		 */
			public ConversionSpecification()
			{
			}

			/**
		 * Constructor for a conversion specification.
		 * The argument must begin with a % and end
		 * with the conversion character for the
		 * conversion specification.
		  * @param fmtArg  String specifying the
		 *     conversion specification.
		  * @exception IllegalArgumentException if the
		 *     input string is null, zero length, or
		 *     otherwise malformed.
		 */
			public ConversionSpecification(string fmtArg)
			{
				if (fmtArg == null)
					throw new();

				if (fmtArg.Length == 0)
					throw new("Control strings must have positive" + " lengths.");

				if (fmtArg[0] == '%')
				{
					this.fmt = fmtArg;
					this.pos = 1;
					this.setArgPosition();
					this.setFlagCharacters();
					this.setFieldWidth();
					this.setPrecision();
					this.setOptionalHL();

					if (this.setConversionCharacter())
					{
						if (this.pos == fmtArg.Length)
						{
							if (this.leadingZeros && this.leftJustify)
								this.leadingZeros = false;

							if (this.precisionSet && this.leadingZeros)
							{
								if (this.conversionCharacter == 'd'
									|| this.conversionCharacter == 'i'
									|| this.conversionCharacter == 'o'
									|| this.conversionCharacter == 'x')
									this.leadingZeros = false;
							}
						}
						else
							throw new("Malformed conversion specification=" + fmtArg);
					}
					else
						throw new("Malformed conversion specification=" + fmtArg);
				}
				else
					throw new("Control strings must begin with %.");
			}

			/**
		 * Set the String for this instance.
		 * @param s the String to store.
		 */
			public void setLiteral(string s)
			{
				this.fmt = s;
			}

			/**
		 * Get the String for this instance.  Translate
		 * any escape sequences.
		 *
		 * @return s the stored String.
		 */
			public string getLiteral()
			{
				StringBuilder sb = new();
				var i = 0;

				while (i < this.fmt.Length)
				{
					if (this.fmt[i] == '\\')
					{
						i++;

						if (i < this.fmt.Length)
						{
							var c = this.fmt[i];

							switch (c)
							{
								case 'a':
									sb.Append((char)0x07);

									break;

								case 'b':
									sb.Append('\b');

									break;

								case 'f':
									sb.Append('\f');

									break;

								case 'n':
									sb.Append('\n');

									break;

								case 'r':
									sb.Append('\r');

									break;

								case 't':
									sb.Append('\t');

									break;

								case 'v':
									sb.Append((char)0x0b);

									break;

								case '\\':
									sb.Append('\\');

									break;
							}

							i++;
						}
						else
							sb.Append('\\');
					}
					else
						i++;
				}

				return this.fmt;
			}

			/**
		 * Get the conversion character that tells what
		 * type of control character this instance has.
		 *
		 * @return the conversion character.
		 */
			public char getConversionCharacter()
			{
				return this.conversionCharacter;
			}

			/**
		 * Check whether the specifier has a variable
		 * field width that is going to be set by an
		 * argument.
		 * @return <code>true</code> if the conversion
		 *   uses an * field width; otherwise
		 *   <code>false</code>.
		 */
			public bool isVariableFieldWidth()
			{
				return this.variableFieldWidth;
			}

			/**
		 * Set the field width with an argument.  A
		 * negative field width is taken as a - flag
		 * followed by a positive field width.
		 * @param fw the field width.
		 */
			public void setFieldWidthWithArg(int fw)
			{
				if (fw < 0)
					this.leftJustify = true;

				this.fieldWidthSet = true;
				this.fieldWidth = Math.Abs(fw);
			}

			/**
		 * Check whether the specifier has a variable
		 * precision that is going to be set by an
		 * argument.
		 * @return <code>true</code> if the conversion
		 *   uses an * precision; otherwise
		 *   <code>false</code>.
		 */
			public bool isVariablePrecision()
			{
				return this.variablePrecision;
			}

			/**
		 * Set the precision with an argument.  A
		 * negative precision will be changed to zero.
		 * @param pr the precision.
		 */
			public void setPrecisionWithArg(int pr)
			{
				this.precisionSet = true;
				this.precision = Math.Max(pr, 0);
			}

			/**
		 * Format an int argument using this conversion
		  * specification.
		 * @param s the int to format.
		 * @return the formatted String.
		 * @exception IllegalArgumentException if the
		 *     conversion character is f, e, E, g, or G.
		 */
			public string internalsprintf(int s)
			{
				var s2 = "";

				switch (this.conversionCharacter)
				{
					case 'd':
					case 'i':
						if (this.optionalh)
							s2 = this.printDFormat((short)s);
						else if (this.optionall)
							s2 = this.printDFormat((long)s);
						else
							s2 = this.printDFormat(s);

						break;

					case 'x':
					case 'X':
						if (this.optionalh)
							s2 = this.printXFormat((short)s);
						else if (this.optionall)
							s2 = this.printXFormat((long)s);
						else
							s2 = this.printXFormat(s);

						break;

					case 'o':
						if (this.optionalh)
							s2 = this.printOFormat((short)s);
						else if (this.optionall)
							s2 = this.printOFormat((long)s);
						else
							s2 = this.printOFormat(s);

						break;

					case 'c':
					case 'C':
						s2 = this.printCFormat((char)s);

						break;

					default:
						throw new("Cannot format a int with a format using a " + this.conversionCharacter + " conversion character.");
				}

				return s2;
			}

			/**
		 * Format a long argument using this conversion
		 * specification.
		 * @param s the long to format.
		 * @return the formatted String.
		 * @exception IllegalArgumentException if the
		 *     conversion character is f, e, E, g, or G.
		 */
			public string internalsprintf(long s)
			{
				var s2 = "";

				switch (this.conversionCharacter)
				{
					case 'd':
					case 'i':
						if (this.optionalh)
							s2 = this.printDFormat((short)s);
						else if (this.optionall)
							s2 = this.printDFormat(s);
						else
							s2 = this.printDFormat((int)s);

						break;

					case 'x':
					case 'X':
						if (this.optionalh)
							s2 = this.printXFormat((short)s);
						else if (this.optionall)
							s2 = this.printXFormat(s);
						else
							s2 = this.printXFormat((int)s);

						break;

					case 'o':
						if (this.optionalh)
							s2 = this.printOFormat((short)s);
						else if (this.optionall)
							s2 = this.printOFormat(s);
						else
							s2 = this.printOFormat((int)s);

						break;

					case 'c':
					case 'C':
						s2 = this.printCFormat((char)s);

						break;

					default:
						throw new("Cannot format a long with a format using a " + this.conversionCharacter + " conversion character.");
				}

				return s2;
			}

			/**
		 * Format a double argument using this conversion
		 * specification.
		 * @param s the double to format.
		 * @return the formatted String.
		 * @exception IllegalArgumentException if the
		 *     conversion character is c, C, s, S, i, d,
		 *     x, X, or o.
		 */
			public string internalsprintf(double s)
			{
				var s2 = "";

				switch (this.conversionCharacter)
				{
					case 'f':
						s2 = this.printFFormat(s);

						break;

					case 'E':
					case 'e':
						s2 = this.printEFormat(s);

						break;

					case 'G':
					case 'g':
						s2 = this.printGFormat(s);

						break;

					default:
						throw new("Cannot " + "format a double with a format using a " + this.conversionCharacter + " conversion character.");
				}

				return s2;
			}

			/**
		 * Format a String argument using this conversion
		 * specification.
		 * @param s the String to format.
		 * @return the formatted String.
		 * @exception IllegalArgumentException if the
		 *   conversion character is neither s nor S.
		 */
			public string internalsprintf(string s)
			{
				var s2 = "";

				if (this.conversionCharacter == 's' || this.conversionCharacter == 'S')
					s2 = this.printSFormat(s);
				else
					throw new("Cannot " + "format a String with a format using a " + this.conversionCharacter + " conversion character.");

				return s2;
			}

			/**
		 * Format an Object argument using this conversion
		 * specification.
		 * @param s the Object to format.
		 * @return the formatted String.
		 * @exception IllegalArgumentException if the
		 *     conversion character is neither s nor S.
		 */
			public string internalsprintf(object s)
			{
				var s2 = "";

				if (this.conversionCharacter == 's' || this.conversionCharacter == 'S')
					s2 = this.printSFormat(s.ToString());
				else
					throw new("Cannot format a String with a format using" + " a " + this.conversionCharacter + " conversion character.");

				return s2;
			}

			/**
		 * For f format, the flag character '-', means that
		 * the output should be left justified within the
		 * field.  The default is to pad with blanks on the
		 * left.  '+' character means that the conversion
		 * will always begin with a sign (+ or -).  The
		 * blank flag character means that a non-negative
		 * input will be preceded with a blank.  If both
		 * a '+' and a ' ' are specified, the blank flag
		 * is ignored.  The '0' flag character implies that
		 * padding to the field width will be done with
		 * zeros instead of blanks.
		 *
		 * The field width is treated as the minimum number
		 * of characters to be printed.  The default is to
		 * add no padding.  Padding is with blanks by
		 * default.
		 *
		 * The precision, if set, is the number of digits
		 * to appear after the radix character.  Padding is
		 * with trailing 0s.
		 */
			private char[] fFormatDigits(double x)
			{
				// int defaultDigits=6;
				string sx;
				int i, j, k;
				int n1In, n2In;
				var expon = 0;
				var minusSign = false;

				if (x > 0.0)
					sx = x.ToString(CultureInfo.InvariantCulture);
				else if (x < 0.0)
				{
					sx = (-x).ToString(CultureInfo.InvariantCulture);
					minusSign = true;
				}
				else
				{
					sx = x.ToString(CultureInfo.InvariantCulture);

					if (sx[0] == '-')
					{
						minusSign = true;
						sx = sx[1..];
					}
				}

				var ePos = sx.IndexOf('E');
				var rPos = sx.IndexOf('.');

				if (rPos != -1)
					n1In = rPos;
				else if (ePos != -1)
					n1In = ePos;
				else
					n1In = sx.Length;

				if (rPos != -1)
				{
					if (ePos != -1)
						n2In = ePos - rPos - 1;
					else
						n2In = sx.Length - rPos - 1;
				}
				else
					n2In = 0;

				if (ePos != -1)
				{
					var ie = ePos + 1;
					expon = 0;

					if (sx[ie] == '-')
					{
						for (++ie; ie < sx.Length; ie++)
						{
							if (sx[ie] != '0')
								break;
						}

						if (ie < sx.Length)
							expon = -int.Parse(sx[ie..]);
					}
					else
					{
						if (sx[ie] == '+')
							++ie;

						for (; ie < sx.Length; ie++)
						{
							if (sx[ie] != '0')
								break;
						}

						if (ie < sx.Length)
							expon = int.Parse(sx[ie..]);
					}
				}

				int p;

				if (this.precisionSet)
					p = this.precision;
				else
					p = ConversionSpecification.defaultDigits - 1;

				var ca1 = sx.ToCharArray();
				var ca2 = new char[n1In + n2In];
				char[] ca3, ca4, ca5;

				for (j = 0; j < n1In; j++)
					ca2[j] = ca1[j];

				i = j + 1;

				for (k = 0; k < n2In; j++, i++, k++)
					ca2[j] = ca1[i];

				if (n1In + expon <= 0)
				{
					ca3 = new char[-expon + n2In];

					for (j = 0, k = 0; k < -n1In - expon; k++, j++)
						ca3[j] = '0';

					for (i = 0; i < n1In + n2In; i++, j++)
						ca3[j] = ca2[i];
				}
				else
					ca3 = ca2;

				var carry = false;

				if (p < -expon + n2In)
				{
					if (expon < 0)
						i = p;
					else
						i = p + n1In;

					carry = this.checkForCarry(ca3, i);

					if (carry)
						carry = this.startSymbolicCarry(ca3, i - 1, 0);
				}

				if (n1In + expon <= 0)
				{
					ca4 = new char[2 + p];

					if (!carry)
						ca4[0] = '0';
					else
						ca4[0] = '1';

					if (this.alternateForm || !this.precisionSet || this.precision != 0)
					{
						ca4[1] = '.';

						for (i = 0, j = 2; i < Math.Min(p, ca3.Length); i++, j++)
							ca4[j] = ca3[i];

						for (; j < ca4.Length; j++)
							ca4[j] = '0';
					}
				}
				else
				{
					if (!carry)
					{
						if (this.alternateForm || !this.precisionSet || this.precision != 0)
							ca4 = new char[n1In + expon + p + 1];
						else
							ca4 = new char[n1In + expon];

						j = 0;
					}
					else
					{
						if (this.alternateForm || !this.precisionSet || this.precision != 0)
							ca4 = new char[n1In + expon + p + 2];
						else
							ca4 = new char[n1In + expon + 1];

						ca4[0] = '1';
						j = 1;
					}

					for (i = 0; i < Math.Min(n1In + expon, ca3.Length); i++, j++)
						ca4[j] = ca3[i];

					for (; i < n1In + expon; i++, j++)
						ca4[j] = '0';

					if (this.alternateForm || !this.precisionSet || this.precision != 0)
					{
						ca4[j] = '.';
						j++;

						for (k = 0; i < ca3.Length && k < p; i++, j++, k++)
							ca4[j] = ca3[i];

						for (; j < ca4.Length; j++)
							ca4[j] = '0';
					}
				}

				var nZeros = 0;

				if (!this.leftJustify && this.leadingZeros)
				{
					var xThousands = 0;

					if (this.thousands)
					{
						var xlead = 0;

						if (ca4[0] == '+' || ca4[0] == '-' || ca4[0] == ' ')
							xlead = 1;

						var xdp = xlead;

						for (; xdp < ca4.Length; xdp++)
						{
							if (ca4[xdp] == '.')
								break;
						}

						xThousands = (xdp - xlead) / 3;
					}

					if (this.fieldWidthSet)
						nZeros = this.fieldWidth - ca4.Length;

					if ((!minusSign && (this.leadingSign || this.leadingSpace)) || minusSign)
						nZeros--;

					nZeros -= xThousands;

					if (nZeros < 0)
						nZeros = 0;
				}

				j = 0;

				if ((!minusSign && (this.leadingSign || this.leadingSpace)) || minusSign)
				{
					ca5 = new char[ca4.Length + nZeros + 1];
					j++;
				}
				else
					ca5 = new char[ca4.Length + nZeros];

				if (!minusSign)
				{
					if (this.leadingSign)
						ca5[0] = '+';

					if (this.leadingSpace)
						ca5[0] = ' ';
				}
				else
					ca5[0] = '-';

				for (i = 0; i < nZeros; i++, j++)
					ca5[j] = '0';

				for (i = 0; i < ca4.Length; i++, j++)
					ca5[j] = ca4[i];

				var lead = 0;

				if (ca5[0] == '+' || ca5[0] == '-' || ca5[0] == ' ')
					lead = 1;

				var dp = lead;

				for (; dp < ca5.Length; dp++)
				{
					if (ca5[dp] == '.')
						break;
				}

				var nThousands = (dp - lead) / 3;

				// Localize the decimal point.
				if (dp < ca5.Length)
					ca5[dp] = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];

				var ca6 = ca5;

				if (this.thousands && nThousands > 0)
				{
					ca6 = new char[ca5.Length + nThousands + lead];
					ca6[0] = ca5[0];

					for (i = lead, k = lead; i < dp; i++)
					{
						if (i > 0 && (dp - i) % 3 == 0)
						{
							// ca6[k]=',';
							ca6[k] = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator[0];
							ca6[k + 1] = ca5[i];
							k += 2;
						}
						else
						{
							ca6[k] = ca5[i];
							k++;
						}
					}

					for (; i < ca5.Length; i++, k++)
						ca6[k] = ca5[i];
				}

				return ca6;
			}

			/**
		 * An intermediate routine on the way to creating
		 * an f format String.  The method decides whether
		 * the input double value is an infinity,
		 * not-a-number, or a finite double and formats
		 * each type of input appropriately.
		 * @param x the double value to be formatted.
		 * @return the converted double value.
		 */
			private string fFormatString(double x)
			{
				char[] ca6, ca7;

				if (double.IsInfinity(x))
				{
					if (x == double.PositiveInfinity)
					{
						if (this.leadingSign)
							ca6 = "+Inf".ToCharArray();
						else if (this.leadingSpace)
							ca6 = " Inf".ToCharArray();
						else
							ca6 = "Inf".ToCharArray();
					}
					else
						ca6 = "-Inf".ToCharArray();
				}
				else if (double.IsNaN(x))
				{
					if (this.leadingSign)
						ca6 = "+NaN".ToCharArray();
					else if (this.leadingSpace)
						ca6 = " NaN".ToCharArray();
					else
						ca6 = "NaN".ToCharArray();
				}
				else
					ca6 = this.fFormatDigits(x);

				ca7 = this.applyFloatPadding(ca6, false);

				return new(ca7);
			}

			/**
		 * For e format, the flag character '-', means that
		 * the output should be left justified within the
		 * field.  The default is to pad with blanks on the
		 * left.  '+' character means that the conversion
		 * will always begin with a sign (+ or -).  The
		 * blank flag character means that a non-negative
		 * input will be preceded with a blank.  If both a
		 * '+' and a ' ' are specified, the blank flag is
		 * ignored.  The '0' flag character implies that
		 * padding to the field width will be done with
		 * zeros instead of blanks.
		 *
		 * The field width is treated as the minimum number
		 * of characters to be printed.  The default is to
		 * add no padding.  Padding is with blanks by
		 * default.
		 *
		 * The precision, if set, is the minimum number of
		 * digits to appear after the radix character.
		 * Padding is with trailing 0s.
		 *
		 * The behavior is like printf.  One (hopefully the
		 * only) exception is that the minimum number of
		 * exponent digits is 3 instead of 2 for e and E
		 * formats when the optional L is used before the
		 * e, E, g, or G conversion character. The optional
		 * L does not imply conversion to a long long
		 * double.
		 */
			private char[] eFormatDigits(double x, char eChar)
			{
				char[] ca1, ca2, ca3;

				// int defaultDigits=6;
				string sx;
				int i, j, k, p;
				int n1In, n2In;
				var expon = 0;
				int ePos, rPos, eSize;
				var minusSign = false;

				if (x > 0.0)
					sx = x.ToString(CultureInfo.InvariantCulture);
				else if (x < 0.0)
				{
					sx = (-x).ToString(CultureInfo.InvariantCulture);
					minusSign = true;
				}
				else
				{
					sx = x.ToString(CultureInfo.InvariantCulture);

					if (sx[0] == '-')
					{
						minusSign = true;
						sx = sx[1..];
					}
				}

				ePos = sx.IndexOf('E');

				if (ePos == -1)
					ePos = sx.IndexOf('e');

				rPos = sx.IndexOf('.');

				if (rPos != -1)
					n1In = rPos;
				else if (ePos != -1)
					n1In = ePos;
				else
					n1In = sx.Length;

				if (rPos != -1)
				{
					if (ePos != -1)
						n2In = ePos - rPos - 1;
					else
						n2In = sx.Length - rPos - 1;
				}
				else
					n2In = 0;

				if (ePos != -1)
				{
					var ie = ePos + 1;
					expon = 0;

					if (sx[ie] == '-')
					{
						for (++ie; ie < sx.Length; ie++)
						{
							if (sx[ie] != '0')
								break;
						}

						if (ie < sx.Length)
							expon = -int.Parse(sx[ie..]);
					}
					else
					{
						if (sx[ie] == '+')
							++ie;

						for (; ie < sx.Length; ie++)
						{
							if (sx[ie] != '0')
								break;
						}

						if (ie < sx.Length)
							expon = int.Parse(sx[ie..]);
					}
				}

				if (rPos != -1)
					expon += rPos - 1;

				if (this.precisionSet)
					p = this.precision;
				else
					p = ConversionSpecification.defaultDigits - 1;

				if (rPos != -1 && ePos != -1)
					ca1 = (sx[..rPos] + sx.Substring(rPos + 1, ePos - (rPos + 1))).ToCharArray();
				else if (rPos != -1)
					ca1 = (sx[..rPos] + sx[(rPos + 1)..]).ToCharArray();
				else if (ePos != -1)
					ca1 = sx[..ePos].ToCharArray();
				else
					ca1 = sx.ToCharArray();

				var carry = false;
				var i0 = 0;

				if (ca1[0] != '0')
					i0 = 0;
				else
				{
					for (i0 = 0; i0 < ca1.Length; i0++)
					{
						if (ca1[i0] != '0')
							break;
					}
				}

				if (i0 + p < ca1.Length - 1)
				{
					carry = this.checkForCarry(ca1, i0 + p + 1);

					if (carry)
						carry = this.startSymbolicCarry(ca1, i0 + p, i0);

					if (carry)
					{
						ca2 = new char[i0 + p + 1];
						ca2[i0] = '1';

						for (j = 0; j < i0; j++)
							ca2[j] = '0';

						for (i = i0, j = i0 + 1; j < p + 1; i++, j++)
							ca2[j] = ca1[i];

						expon++;
						ca1 = ca2;
					}
				}

				if (Math.Abs(expon) < 100 && !this.optionalL)
					eSize = 4;
				else
					eSize = 5;

				if (this.alternateForm || !this.precisionSet || this.precision != 0)
					ca2 = new char[2 + p + eSize];
				else
					ca2 = new char[1 + eSize];

				if (ca1[0] != '0')
				{
					ca2[0] = ca1[0];
					j = 1;
				}
				else
				{
					for (j = 1; j < (ePos == -1 ? ca1.Length : ePos); j++)
					{
						if (ca1[j] != '0')
							break;
					}

					if ((ePos != -1 && j < ePos) || (ePos == -1 && j < ca1.Length))
					{
						ca2[0] = ca1[j];
						expon -= j;
						j++;
					}
					else
					{
						ca2[0] = '0';
						j = 2;
					}
				}

				if (this.alternateForm || !this.precisionSet || this.precision != 0)
				{
					ca2[1] = '.';
					i = 2;
				}
				else
					i = 1;

				for (k = 0; k < p && j < ca1.Length; j++, i++, k++)
					ca2[i] = ca1[j];

				for (; i < ca2.Length - eSize; i++)
					ca2[i] = '0';

				ca2[i++] = eChar;

				if (expon < 0)
					ca2[i++] = '-';
				else
					ca2[i++] = '+';

				expon = Math.Abs(expon);

				if (expon >= 100)
				{
					switch (expon / 100)
					{
						case 1:
							ca2[i] = '1';

							break;

						case 2:
							ca2[i] = '2';

							break;

						case 3:
							ca2[i] = '3';

							break;

						case 4:
							ca2[i] = '4';

							break;

						case 5:
							ca2[i] = '5';

							break;

						case 6:
							ca2[i] = '6';

							break;

						case 7:
							ca2[i] = '7';

							break;

						case 8:
							ca2[i] = '8';

							break;

						case 9:
							ca2[i] = '9';

							break;
					}

					i++;
				}

				switch (expon % 100 / 10)
				{
					case 0:
						ca2[i] = '0';

						break;

					case 1:
						ca2[i] = '1';

						break;

					case 2:
						ca2[i] = '2';

						break;

					case 3:
						ca2[i] = '3';

						break;

					case 4:
						ca2[i] = '4';

						break;

					case 5:
						ca2[i] = '5';

						break;

					case 6:
						ca2[i] = '6';

						break;

					case 7:
						ca2[i] = '7';

						break;

					case 8:
						ca2[i] = '8';

						break;

					case 9:
						ca2[i] = '9';

						break;
				}

				i++;

				switch (expon % 10)
				{
					case 0:
						ca2[i] = '0';

						break;

					case 1:
						ca2[i] = '1';

						break;

					case 2:
						ca2[i] = '2';

						break;

					case 3:
						ca2[i] = '3';

						break;

					case 4:
						ca2[i] = '4';

						break;

					case 5:
						ca2[i] = '5';

						break;

					case 6:
						ca2[i] = '6';

						break;

					case 7:
						ca2[i] = '7';

						break;

					case 8:
						ca2[i] = '8';

						break;

					case 9:
						ca2[i] = '9';

						break;
				}

				var nZeros = 0;

				if (!this.leftJustify && this.leadingZeros)
				{
					var xThousands = 0;

					if (this.thousands)
					{
						var xlead = 0;

						if (ca2[0] == '+' || ca2[0] == '-' || ca2[0] == ' ')
							xlead = 1;

						var xdp = xlead;

						for (; xdp < ca2.Length; xdp++)
						{
							if (ca2[xdp] == '.')
								break;
						}

						xThousands = (xdp - xlead) / 3;
					}

					if (this.fieldWidthSet)
						nZeros = this.fieldWidth - ca2.Length;

					if ((!minusSign && (this.leadingSign || this.leadingSpace)) || minusSign)
						nZeros--;

					nZeros -= xThousands;

					if (nZeros < 0)
						nZeros = 0;
				}

				j = 0;

				if ((!minusSign && (this.leadingSign || this.leadingSpace)) || minusSign)
				{
					ca3 = new char[ca2.Length + nZeros + 1];
					j++;
				}
				else
					ca3 = new char[ca2.Length + nZeros];

				if (!minusSign)
				{
					if (this.leadingSign)
						ca3[0] = '+';

					if (this.leadingSpace)
						ca3[0] = ' ';
				}
				else
					ca3[0] = '-';

				for (k = 0; k < nZeros; j++, k++)
					ca3[j] = '0';

				for (i = 0; i < ca2.Length && j < ca3.Length; i++, j++)
					ca3[j] = ca2[i];

				var lead = 0;

				if (ca3[0] == '+' || ca3[0] == '-' || ca3[0] == ' ')
					lead = 1;

				var dp = lead;

				for (; dp < ca3.Length; dp++)
				{
					if (ca3[dp] == '.')
						break;
				}

				var nThousands = dp / 3;

				// Localize the decimal point.
				if (dp < ca3.Length)
					ca3[dp] = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];

				var ca4 = ca3;

				if (this.thousands && nThousands > 0)
				{
					ca4 = new char[ca3.Length + nThousands + lead];
					ca4[0] = ca3[0];

					for (i = lead, k = lead; i < dp; i++)
					{
						if (i > 0 && (dp - i) % 3 == 0)
						{
							// ca4[k]=',';
							ca4[k] = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator[0];
							ca4[k + 1] = ca3[i];
							k += 2;
						}
						else
						{
							ca4[k] = ca3[i];
							k++;
						}
					}

					for (; i < ca3.Length; i++, k++)
						ca4[k] = ca3[i];
				}

				return ca4;
			}

			/**
		 * Check to see if the digits that are going to
		 * be truncated because of the precision should
		 * force a round in the preceding digits.
		 * @param ca1 the array of digits
		 * @param icarry the index of the first digit that
		 *     is to be truncated from the print
		 * @return <code>true</code> if the truncation forces
		 *     a round that will change the print
		 */
			private bool checkForCarry(char[] ca1, int icarry)
			{
				var carry = false;

				if (icarry < ca1.Length)
				{
					if (ca1[icarry] == '6' || ca1[icarry] == '7' || ca1[icarry] == '8' || ca1[icarry] == '9')
						carry = true;
					else if (ca1[icarry] == '5')
					{
						var ii = icarry + 1;

						for (; ii < ca1.Length; ii++)
						{
							if (ca1[ii] != '0')
								break;
						}

						carry = ii < ca1.Length;

						if (!carry && icarry > 0)
						{
							carry = ca1[icarry - 1] == '1'
								|| ca1[icarry - 1] == '3'
								|| ca1[icarry - 1] == '5'
								|| ca1[icarry - 1] == '7'
								|| ca1[icarry - 1] == '9';
						}
					}
				}

				return carry;
			}

			/**
		 * Start the symbolic carry process.  The process
		 * is not quite finished because the symbolic
		 * carry may change the length of the string and
		 * change the exponent (in e format).
		 * @param cLast index of the last digit changed
		 *     by the round
		 * @param cFirst index of the first digit allowed
		 *     to be changed by this phase of the round
		 * @return <code>true</code> if the carry forces
		 *     a round that will change the print still
		 *     more
		 */
			private bool startSymbolicCarry(char[] ca, int cLast, int cFirst)
			{
				var carry = true;

				for (var i = cLast; carry && i >= cFirst; i--)
				{
					carry = false;

					switch (ca[i])
					{
						case '0':
							ca[i] = '1';

							break;

						case '1':
							ca[i] = '2';

							break;

						case '2':
							ca[i] = '3';

							break;

						case '3':
							ca[i] = '4';

							break;

						case '4':
							ca[i] = '5';

							break;

						case '5':
							ca[i] = '6';

							break;

						case '6':
							ca[i] = '7';

							break;

						case '7':
							ca[i] = '8';

							break;

						case '8':
							ca[i] = '9';

							break;

						case '9':
							ca[i] = '0';
							carry = true;

							break;
					}
				}

				return carry;
			}

			/**
		 * An intermediate routine on the way to creating
		 * an e format String.  The method decides whether
		 * the input double value is an infinity,
		 * not-a-number, or a finite double and formats
		 * each type of input appropriately.
		 * @param x the double value to be formatted.
		 * @param eChar an 'e' or 'E' to use in the
		 *     converted double value.
		 * @return the converted double value.
		 */
			private string eFormatString(double x, char eChar)
			{
				char[] ca4, ca5;

				if (double.IsInfinity(x))
				{
					if (x == double.PositiveInfinity)
					{
						if (this.leadingSign)
							ca4 = "+Inf".ToCharArray();
						else if (this.leadingSpace)
							ca4 = " Inf".ToCharArray();
						else
							ca4 = "Inf".ToCharArray();
					}
					else
						ca4 = "-Inf".ToCharArray();
				}
				else if (double.IsNaN(x))
				{
					if (this.leadingSign)
						ca4 = "+NaN".ToCharArray();
					else if (this.leadingSpace)
						ca4 = " NaN".ToCharArray();
					else
						ca4 = "NaN".ToCharArray();
				}
				else
					ca4 = this.eFormatDigits(x, eChar);

				ca5 = this.applyFloatPadding(ca4, false);

				return new(ca5);
			}

			/**
		 * Apply zero or blank, left or right padding.
		 * @param ca4 array of characters before padding is
		 *     finished
		 * @param noDigits NaN or signed Inf
		 * @return a padded array of characters
		 */
			private char[] applyFloatPadding(char[] ca4, bool noDigits)
			{
				var ca5 = ca4;

				if (this.fieldWidthSet)
				{
					int i, j, nBlanks;

					if (this.leftJustify)
					{
						nBlanks = this.fieldWidth - ca4.Length;

						if (nBlanks > 0)
						{
							ca5 = new char[ca4.Length + nBlanks];

							for (i = 0; i < ca4.Length; i++)
								ca5[i] = ca4[i];

							for (j = 0; j < nBlanks; j++, i++)
								ca5[i] = ' ';
						}
					}
					else if (!this.leadingZeros || noDigits)
					{
						nBlanks = this.fieldWidth - ca4.Length;

						if (nBlanks > 0)
						{
							ca5 = new char[ca4.Length + nBlanks];

							for (i = 0; i < nBlanks; i++)
								ca5[i] = ' ';

							for (j = 0; j < ca4.Length; i++, j++)
								ca5[i] = ca4[j];
						}
					}
					else if (this.leadingZeros)
					{
						nBlanks = this.fieldWidth - ca4.Length;

						if (nBlanks > 0)
						{
							ca5 = new char[ca4.Length + nBlanks];
							i = 0;
							j = 0;

							if (ca4[0] == '-')
							{
								ca5[0] = '-';
								i++;
								j++;
							}

							for (var k = 0; k < nBlanks; i++, k++)
								ca5[i] = '0';

							for (; j < ca4.Length; i++, j++)
								ca5[i] = ca4[j];
						}
					}
				}

				return ca5;
			}

			/**
		 * Format method for the f conversion character.
		 * @param x the double to format.
		 * @return the formatted String.
		 */
			private string printFFormat(double x)
			{
				return this.fFormatString(x);
			}

			/**
		 * Format method for the e or E conversion
		 * character.
		 * @param x the double to format.
		 * @return the formatted String.
		 */
			private string printEFormat(double x)
			{
				if (this.conversionCharacter == 'e')
					return this.eFormatString(x, 'e');

				return this.eFormatString(x, 'E');
			}

			/**
		 * Format method for the g conversion character.
		 *
		 * For g format, the flag character '-', means that
		 *  the output should be left justified within the
		 * field.  The default is to pad with blanks on the
		 * left.  '+' character means that the conversion
		 * will always begin with a sign (+ or -).  The
		 * blank flag character means that a non-negative
		 * input will be preceded with a blank.  If both a
		 * '+' and a ' ' are specified, the blank flag is
		 * ignored.  The '0' flag character implies that
		 * padding to the field width will be done with
		 * zeros instead of blanks.
		 *
		 * The field width is treated as the minimum number
		 * of characters to be printed.  The default is to
		 * add no padding.  Padding is with blanks by
		 * default.
		 *
		 * The precision, if set, is the minimum number of
		 * digits to appear after the radix character.
		 * Padding is with trailing 0s.
		 * @param x the double to format.
		 * @return the formatted String.
		 */
			private string printGFormat(double x)
			{
				string sx, sy, sz, ret;
				var savePrecision = this.precision;
				int i;
				char[] ca4, ca5;

				if (double.IsInfinity(x))
				{
					if (x == double.PositiveInfinity)
					{
						if (this.leadingSign)
							ca4 = "+Inf".ToCharArray();
						else if (this.leadingSpace)
							ca4 = " Inf".ToCharArray();
						else
							ca4 = "Inf".ToCharArray();
					}
					else
						ca4 = "-Inf".ToCharArray();
				}
				else if (double.IsNaN(x))
				{
					if (this.leadingSign)
						ca4 = "+NaN".ToCharArray();
					else if (this.leadingSpace)
						ca4 = " NaN".ToCharArray();
					else
						ca4 = "NaN".ToCharArray();
				}
				else
				{
					if (!this.precisionSet)
						this.precision = ConversionSpecification.defaultDigits;

					if (this.precision == 0)
						this.precision = 1;

					var ePos = -1;

					if (this.conversionCharacter == 'g')
					{
						sx = this.eFormatString(x, 'e').Trim();
						ePos = sx.IndexOf('e');
					}
					else
					{
						sx = this.eFormatString(x, 'E').Trim();
						ePos = sx.IndexOf('E');
					}

					i = ePos + 1;
					var expon = 0;

					if (sx[i] == '-')
					{
						for (++i; i < sx.Length; i++)
						{
							if (sx[i] != '0')
								break;
						}

						if (i < sx.Length)
							expon = -int.Parse(sx[i..]);
					}
					else
					{
						if (sx[i] == '+')
							++i;

						for (; i < sx.Length; i++)
						{
							if (sx[i] != '0')
								break;
						}

						if (i < sx.Length)
							expon = int.Parse(sx[i..]);
					}

					// Trim trailing zeros.
					// If the radix character is not followed by
					// a digit, trim it, too.
					if (!this.alternateForm)
					{
						if (expon >= -4 && expon < this.precision)
							sy = this.fFormatString(x).Trim();
						else
							sy = sx[..ePos];

						i = sy.Length - 1;

						for (; i >= 0; i--)
						{
							if (sy[i] != '0')
								break;
						}

						if (i >= 0 && sy[i] == '.')
							i--;

						if (i == -1)
							sz = "0";
						else if (!char.IsDigit(sy[i]))
							sz = sy[..(i + 1)] + "0";
						else
							sz = sy[..(i + 1)];

						if (expon >= -4 && expon < this.precision)
							ret = sz;
						else
							ret = sz + sx[ePos..];
					}
					else
					{
						if (expon >= -4 && expon < this.precision)
							ret = this.fFormatString(x).Trim();
						else
							ret = sx;
					}

					// leading space was trimmed off during
					// construction
					if (this.leadingSpace)
					{
						if (x >= 0)
							ret = " " + ret;
					}

					ca4 = ret.ToCharArray();
				}

				// Pad with blanks or zeros.
				ca5 = this.applyFloatPadding(ca4, false);
				this.precision = savePrecision;

				return new(ca5);
			}

			/**
		 * Format method for the d conversion specifer and
		 * short argument.
		 *
		 * For d format, the flag character '-', means that
		 * the output should be left justified within the
		 * field.  The default is to pad with blanks on the
		 * left.  A '+' character means that the conversion
		 * will always begin with a sign (+ or -).  The
		 * blank flag character means that a non-negative
		 * input will be preceded with a blank.  If both a
		 * '+' and a ' ' are specified, the blank flag is
		 * ignored.  The '0' flag character implies that
		 * padding to the field width will be done with
		 * zeros instead of blanks.
		 *
		 * The field width is treated as the minimum number
		 * of characters to be printed.  The default is to
		 * add no padding.  Padding is with blanks by
		 * default.
		 *
		 * The precision, if set, is the minimum number of
		 * digits to appear.  Padding is with leading 0s.
		 * @param x the short to format.
		 * @return the formatted String.
		 */
			private string printDFormat(short x)
			{
				return this.printDFormat(x.ToString());
			}

			/**
		 * Format method for the d conversion character and
		 * long argument.
		 *
		 * For d format, the flag character '-', means that
		 * the output should be left justified within the
		 * field.  The default is to pad with blanks on the
		 * left.  A '+' character means that the conversion
		 * will always begin with a sign (+ or -).  The
		 * blank flag character means that a non-negative
		 * input will be preceded with a blank.  If both a
		 * '+' and a ' ' are specified, the blank flag is
		 * ignored.  The '0' flag character implies that
		 * padding to the field width will be done with
		 * zeros instead of blanks.
		 *
		 * The field width is treated as the minimum number
		 * of characters to be printed.  The default is to
		 * add no padding.  Padding is with blanks by
		 * default.
		 *
		 * The precision, if set, is the minimum number of
		 * digits to appear.  Padding is with leading 0s.
		 * @param x the long to format.
		 * @return the formatted String.
		 */
			private string printDFormat(long x)
			{
				return this.printDFormat(x.ToString());
			}

			/**
		 * Format method for the d conversion character and
		 * int argument.
		 *
		 * For d format, the flag character '-', means that
		 * the output should be left justified within the
		 * field.  The default is to pad with blanks on the
		 * left.  A '+' character means that the conversion
		 * will always begin with a sign (+ or -).  The
		 * blank flag character means that a non-negative
		 * input will be preceded with a blank.  If both a
		 * '+' and a ' ' are specified, the blank flag is
		 * ignored.  The '0' flag character implies that
		 * padding to the field width will be done with
		 * zeros instead of blanks.
		 *
		 * The field width is treated as the minimum number
		 * of characters to be printed.  The default is to
		 * add no padding.  Padding is with blanks by
		 * default.
		 *
		 * The precision, if set, is the minimum number of
		 * digits to appear.  Padding is with leading 0s.
		 * @param x the int to format.
		 * @return the formatted String.
		 */
			private string printDFormat(int x)
			{
				return this.printDFormat(x.ToString());
			}

			/**
		 * Utility method for formatting using the d
		 * conversion character.
		 * @param sx the String to format, the result of
		 *     converting a short, int, or long to a
		 *     String.
		 * @return the formatted String.
		 */
			private string printDFormat(string sx)
			{
				var nLeadingZeros = 0;
				int nBlanks = 0, n = 0;
				int i = 0, jFirst = 0;
				var neg = sx[0] == '-';

				if (sx.Equals("0") && this.precisionSet && this.precision == 0)
					sx = "";

				if (!neg)
				{
					if (this.precisionSet && sx.Length < this.precision)
						nLeadingZeros = this.precision - sx.Length;
				}
				else
				{
					if (this.precisionSet && sx.Length - 1 < this.precision)
						nLeadingZeros = this.precision - sx.Length + 1;
				}

				if (nLeadingZeros < 0)
					nLeadingZeros = 0;

				if (this.fieldWidthSet)
				{
					nBlanks = this.fieldWidth - nLeadingZeros - sx.Length;

					if (!neg && (this.leadingSign || this.leadingSpace))
						nBlanks--;
				}

				if (nBlanks < 0)
					nBlanks = 0;

				if (this.leadingSign)
					n++;
				else if (this.leadingSpace)
					n++;

				n += nBlanks;
				n += nLeadingZeros;
				n += sx.Length;
				var ca = new char[n];

				if (this.leftJustify)
				{
					if (neg)
						ca[i++] = '-';
					else if (this.leadingSign)
						ca[i++] = '+';
					else if (this.leadingSpace)
						ca[i++] = ' ';

					var csx = sx.ToCharArray();
					jFirst = neg ? 1 : 0;

					for (var j = 0; j < nLeadingZeros; i++, j++)
						ca[i] = '0';

					for (var j = jFirst; j < csx.Length; j++, i++)
						ca[i] = csx[j];

					for (var j = 0; j < nBlanks; i++, j++)
						ca[i] = ' ';
				}
				else
				{
					if (!this.leadingZeros)
					{
						for (i = 0; i < nBlanks; i++)
							ca[i] = ' ';

						if (neg)
							ca[i++] = '-';
						else if (this.leadingSign)
							ca[i++] = '+';
						else if (this.leadingSpace)
							ca[i++] = ' ';
					}
					else
					{
						if (neg)
							ca[i++] = '-';
						else if (this.leadingSign)
							ca[i++] = '+';
						else if (this.leadingSpace)
							ca[i++] = ' ';

						for (var j = 0; j < nBlanks; j++, i++)
							ca[i] = '0';
					}

					for (var j = 0; j < nLeadingZeros; j++, i++)
						ca[i] = '0';

					var csx = sx.ToCharArray();
					jFirst = neg ? 1 : 0;

					for (var j = jFirst; j < csx.Length; j++, i++)
						ca[i] = csx[j];
				}

				return new(ca);
			}

			/**
		 * Format method for the x conversion character and
		 * short argument.
		 *
		 * For x format, the flag character '-', means that
		 * the output should be left justified within the
		 * field.  The default is to pad with blanks on the
		 * left.  The '#' flag character means to lead with
		 * '0x'.
		 *
		 * The field width is treated as the minimum number
		 * of characters to be printed.  The default is to
		 * add no padding.  Padding is with blanks by
		 * default.
		 *
		 * The precision, if set, is the minimum number of
		 * digits to appear.  Padding is with leading 0s.
		 * @param x the short to format.
		 * @return the formatted String.
		 */
			private string printXFormat(short x)
			{
				string sx = null;

				if (x == short.MinValue)
					sx = "8000";
				else if (x < 0)
				{
					string t;

					if (x == short.MinValue)
						t = "0";
					else
					{
						t = Convert.ToString(~(-x - 1) ^ short.MinValue, 16);

						if (t[0] == 'F' || t[0] == 'f')
							t = t.Substring(16, 16);
					}

					switch (t.Length)
					{
						case 1:
							sx = "800" + t;

							break;

						case 2:
							sx = "80" + t;

							break;

						case 3:
							sx = "8" + t;

							break;

						case 4:
							switch (t[0])
							{
								case '1':
									sx = "9" + t.Substring(1, 3);

									break;

								case '2':
									sx = "a" + t.Substring(1, 3);

									break;

								case '3':
									sx = "b" + t.Substring(1, 3);

									break;

								case '4':
									sx = "c" + t.Substring(1, 3);

									break;

								case '5':
									sx = "d" + t.Substring(1, 3);

									break;

								case '6':
									sx = "e" + t.Substring(1, 3);

									break;

								case '7':
									sx = "f" + t.Substring(1, 3);

									break;
							}

							break;
					}
				}
				else
					sx = Convert.ToString((int)x, 16);

				return this.printXFormat(sx);
			}

			/**
		 * Format method for the x conversion character and
		 * long argument.
		 *
		 * For x format, the flag character '-', means that
		 * the output should be left justified within the
		 * field.  The default is to pad with blanks on the
		 * left.  The '#' flag character means to lead with
		 * '0x'.
		 *
		 * The field width is treated as the minimum number
		 * of characters to be printed.  The default is to
		 * add no padding.  Padding is with blanks by
		 * default.
		 *
		 * The precision, if set, is the minimum number of
		 * digits to appear.  Padding is with leading 0s.
		 * @param x the long to format.
		 * @return the formatted String.
		 */
			private string printXFormat(long x)
			{
				string sx = null;

				if (x == long.MinValue)
					sx = "8000000000000000";
				else if (x < 0)
				{
					var t = Convert.ToString(~(-x - 1) ^ long.MinValue, 16);

					switch (t.Length)
					{
						case 1:
							sx = "800000000000000" + t;

							break;

						case 2:
							sx = "80000000000000" + t;

							break;

						case 3:
							sx = "8000000000000" + t;

							break;

						case 4:
							sx = "800000000000" + t;

							break;

						case 5:
							sx = "80000000000" + t;

							break;

						case 6:
							sx = "8000000000" + t;

							break;

						case 7:
							sx = "800000000" + t;

							break;

						case 8:
							sx = "80000000" + t;

							break;

						case 9:
							sx = "8000000" + t;

							break;

						case 10:
							sx = "800000" + t;

							break;

						case 11:
							sx = "80000" + t;

							break;

						case 12:
							sx = "8000" + t;

							break;

						case 13:
							sx = "800" + t;

							break;

						case 14:
							sx = "80" + t;

							break;

						case 15:
							sx = "8" + t;

							break;

						case 16:
							switch (t[0])
							{
								case '1':
									sx = "9" + t.Substring(1, 15);

									break;

								case '2':
									sx = "a" + t.Substring(1, 15);

									break;

								case '3':
									sx = "b" + t.Substring(1, 15);

									break;

								case '4':
									sx = "c" + t.Substring(1, 15);

									break;

								case '5':
									sx = "d" + t.Substring(1, 15);

									break;

								case '6':
									sx = "e" + t.Substring(1, 15);

									break;

								case '7':
									sx = "f" + t.Substring(1, 15);

									break;
							}

							break;
					}
				}
				else
					sx = Convert.ToString(x, 16);

				return this.printXFormat(sx);
			}

			/**
		 * Format method for the x conversion character and
		 * int argument.
		 *
		 * For x format, the flag character '-', means that
		 * the output should be left justified within the
		 * field.  The default is to pad with blanks on the
		 * left.  The '#' flag character means to lead with
		 * '0x'.
		 *
		 * The field width is treated as the minimum number
		 * of characters to be printed.  The default is to
		 * add no padding.  Padding is with blanks by
		 * default.
		 *
		 * The precision, if set, is the minimum number of
		 * digits to appear.  Padding is with leading 0s.
		 * @param x the int to format.
		 * @return the formatted String.
		 */
			private string printXFormat(int x)
			{
				string sx = null;

				if (x == int.MinValue)
					sx = "80000000";
				else if (x < 0)
				{
					var t = Convert.ToString(~(-x - 1) ^ int.MinValue, 16);

					switch (t.Length)
					{
						case 1:
							sx = "8000000" + t;

							break;

						case 2:
							sx = "800000" + t;

							break;

						case 3:
							sx = "80000" + t;

							break;

						case 4:
							sx = "8000" + t;

							break;

						case 5:
							sx = "800" + t;

							break;

						case 6:
							sx = "80" + t;

							break;

						case 7:
							sx = "8" + t;

							break;

						case 8:
							switch (t[0])
							{
								case '1':
									sx = "9" + t.Substring(1, 7);

									break;

								case '2':
									sx = "a" + t.Substring(1, 7);

									break;

								case '3':
									sx = "b" + t.Substring(1, 7);

									break;

								case '4':
									sx = "c" + t.Substring(1, 7);

									break;

								case '5':
									sx = "d" + t.Substring(1, 7);

									break;

								case '6':
									sx = "e" + t.Substring(1, 7);

									break;

								case '7':
									sx = "f" + t.Substring(1, 7);

									break;
							}

							break;
					}
				}
				else
					sx = Convert.ToString(x, 16);

				return this.printXFormat(sx);
			}

			/**
		 * Utility method for formatting using the x
		 * conversion character.
		 * @param sx the String to format, the result of
		 *     converting a short, int, or long to a
		 *     String.
		 * @return the formatted String.
		 */
			private string printXFormat(string sx)
			{
				var nLeadingZeros = 0;
				var nBlanks = 0;

				if (sx.Equals("0") && this.precisionSet && this.precision == 0)
					sx = "";

				if (this.precisionSet)
					nLeadingZeros = this.precision - sx.Length;

				if (nLeadingZeros < 0)
					nLeadingZeros = 0;

				if (this.fieldWidthSet)
				{
					nBlanks = this.fieldWidth - nLeadingZeros - sx.Length;

					if (this.alternateForm)
						nBlanks = nBlanks - 2;
				}

				if (nBlanks < 0)
					nBlanks = 0;

				var n = 0;

				if (this.alternateForm)
					n += 2;

				n += nLeadingZeros;
				n += sx.Length;
				n += nBlanks;
				var ca = new char[n];
				var i = 0;

				if (this.leftJustify)
				{
					if (this.alternateForm)
					{
						ca[i++] = '0';
						ca[i++] = 'x';
					}

					for (var j = 0; j < nLeadingZeros; j++, i++)
						ca[i] = '0';

					var csx = sx.ToCharArray();

					for (var j = 0; j < csx.Length; j++, i++)
						ca[i] = csx[j];

					for (var j = 0; j < nBlanks; j++, i++)
						ca[i] = ' ';
				}
				else
				{
					if (!this.leadingZeros)
					{
						for (var j = 0; j < nBlanks; j++, i++)
							ca[i] = ' ';
					}

					if (this.alternateForm)
					{
						ca[i++] = '0';
						ca[i++] = 'x';
					}

					if (this.leadingZeros)
					{
						for (var j = 0; j < nBlanks; j++, i++)
							ca[i] = '0';
					}

					for (var j = 0; j < nLeadingZeros; j++, i++)
						ca[i] = '0';

					var csx = sx.ToCharArray();

					for (var j = 0; j < csx.Length; j++, i++)
						ca[i] = csx[j];
				}

				string caReturn = new(ca);

				if (this.conversionCharacter == 'X')
					caReturn = caReturn.ToUpper();

				return caReturn;
			}

			/**
		 * Format method for the o conversion character and
		 * short argument.
		 *
		 * For o format, the flag character '-', means that
		 * the output should be left justified within the
		 * field.  The default is to pad with blanks on the 
		 * left.  The '#' flag character means that the
		 * output begins with a leading 0 and the precision
		 * is increased by 1.
		 *
		 * The field width is treated as the minimum number
		 * of characters to be printed.  The default is to
		 * add no padding.  Padding is with blanks by
		 * default.
		 *
		 * The precision, if set, is the minimum number of
		 * digits to appear.  Padding is with leading 0s.
		 * @param x the short to format.
		 * @return the formatted String.
		 */
			private string printOFormat(short x)
			{
				string sx = null;

				if (x == short.MinValue)
					sx = "100000";
				else if (x < 0)
				{
					var t = Convert.ToString(~(-x - 1) ^ short.MinValue, 8);

					switch (t.Length)
					{
						case 1:
							sx = "10000" + t;

							break;

						case 2:
							sx = "1000" + t;

							break;

						case 3:
							sx = "100" + t;

							break;

						case 4:
							sx = "10" + t;

							break;

						case 5:
							sx = "1" + t;

							break;
					}
				}
				else
					sx = Convert.ToString((int)x, 8);

				return this.printOFormat(sx);
			}

			/**
		 * Format method for the o conversion character and
		 * long argument.
		 *
		 * For o format, the flag character '-', means that
		 * the output should be left justified within the
		 * field.  The default is to pad with blanks on the 
		 * left.  The '#' flag character means that the
		 * output begins with a leading 0 and the precision
		 * is increased by 1.
		 *
		 * The field width is treated as the minimum number
		 * of characters to be printed.  The default is to
		 * add no padding.  Padding is with blanks by
		 * default.
		 *
		 * The precision, if set, is the minimum number of
		 * digits to appear.  Padding is with leading 0s.
		 * @param x the long to format.
		 * @return the formatted String.
		 */
			private string printOFormat(long x)
			{
				string sx = null;

				if (x == long.MinValue)
					sx = "1000000000000000000000";
				else if (x < 0)
				{
					var t = Convert.ToString(~(-x - 1) ^ long.MinValue, 8);

					switch (t.Length)
					{
						case 1:
							sx = "100000000000000000000" + t;

							break;

						case 2:
							sx = "10000000000000000000" + t;

							break;

						case 3:
							sx = "1000000000000000000" + t;

							break;

						case 4:
							sx = "100000000000000000" + t;

							break;

						case 5:
							sx = "10000000000000000" + t;

							break;

						case 6:
							sx = "1000000000000000" + t;

							break;

						case 7:
							sx = "100000000000000" + t;

							break;

						case 8:
							sx = "10000000000000" + t;

							break;

						case 9:
							sx = "1000000000000" + t;

							break;

						case 10:
							sx = "100000000000" + t;

							break;

						case 11:
							sx = "10000000000" + t;

							break;

						case 12:
							sx = "1000000000" + t;

							break;

						case 13:
							sx = "100000000" + t;

							break;

						case 14:
							sx = "10000000" + t;

							break;

						case 15:
							sx = "1000000" + t;

							break;

						case 16:
							sx = "100000" + t;

							break;

						case 17:
							sx = "10000" + t;

							break;

						case 18:
							sx = "1000" + t;

							break;

						case 19:
							sx = "100" + t;

							break;

						case 20:
							sx = "10" + t;

							break;

						case 21:
							sx = "1" + t;

							break;
					}
				}
				else
					sx = Convert.ToString(x, 8);

				return this.printOFormat(sx);
			}

			/**
		 * Format method for the o conversion character and
		 * int argument.
		 *
		 * For o format, the flag character '-', means that
		 * the output should be left justified within the
		 * field.  The default is to pad with blanks on the
		 * left.  The '#' flag character means that the
		 * output begins with a leading 0 and the precision
		 * is increased by 1.
		 *
		 * The field width is treated as the minimum number
		 * of characters to be printed.  The default is to
		 * add no padding.  Padding is with blanks by
		 * default.
		 *
		 * The precision, if set, is the minimum number of
		 * digits to appear.  Padding is with leading 0s.
		 * @param x the int to format.
		 * @return the formatted String.
		 */
			private string printOFormat(int x)
			{
				string sx = null;

				if (x == int.MinValue)
					sx = "20000000000";
				else if (x < 0)
				{
					var t = Convert.ToString(~(-x - 1) ^ int.MinValue, 8);

					switch (t.Length)
					{
						case 1:
							sx = "2000000000" + t;

							break;

						case 2:
							sx = "200000000" + t;

							break;

						case 3:
							sx = "20000000" + t;

							break;

						case 4:
							sx = "2000000" + t;

							break;

						case 5:
							sx = "200000" + t;

							break;

						case 6:
							sx = "20000" + t;

							break;

						case 7:
							sx = "2000" + t;

							break;

						case 8:
							sx = "200" + t;

							break;

						case 9:
							sx = "20" + t;

							break;

						case 10:
							sx = "2" + t;

							break;

						case 11:
							sx = "3" + t[1..];

							break;
					}
				}
				else
					sx = Convert.ToString(x, 8);

				return this.printOFormat(sx);
			}

			/**
		 * Utility method for formatting using the o
		 * conversion character.
		 * @param sx the String to format, the result of
		 *     converting a short, int, or long to a
		 *     String.
		 * @return the formatted String.
		 */
			private string printOFormat(string sx)
			{
				var nLeadingZeros = 0;
				var nBlanks = 0;

				if (sx.Equals("0") && this.precisionSet && this.precision == 0)
					sx = "";

				if (this.precisionSet)
					nLeadingZeros = this.precision - sx.Length;

				if (this.alternateForm)
					nLeadingZeros++;

				if (nLeadingZeros < 0)
					nLeadingZeros = 0;

				if (this.fieldWidthSet)
					nBlanks = this.fieldWidth - nLeadingZeros - sx.Length;

				if (nBlanks < 0)
					nBlanks = 0;

				var n = nLeadingZeros + sx.Length + nBlanks;
				var ca = new char[n];
				int i;

				if (this.leftJustify)
				{
					for (i = 0; i < nLeadingZeros; i++)
						ca[i] = '0';

					var csx = sx.ToCharArray();

					for (var j = 0; j < csx.Length; j++, i++)
						ca[i] = csx[j];

					for (var j = 0; j < nBlanks; j++, i++)
						ca[i] = ' ';
				}
				else
				{
					if (this.leadingZeros)
					{
						for (i = 0; i < nBlanks; i++)
							ca[i] = '0';
					}
					else
					{
						for (i = 0; i < nBlanks; i++)
							ca[i] = ' ';
					}

					for (var j = 0; j < nLeadingZeros; j++, i++)
						ca[i] = '0';

					var csx = sx.ToCharArray();

					for (var j = 0; j < csx.Length; j++, i++)
						ca[i] = csx[j];
				}

				return new(ca);
			}

			/**
		 * Format method for the c conversion character and
		 * char argument.
		 *
		 * The only flag character that affects c format is
		 * the '-', meaning that the output should be left
		 * justified within the field.  The default is to
		 * pad with blanks on the left.
		 *
		 * The field width is treated as the minimum number
		 * of characters to be printed.  Padding is with
		 * blanks by default.  The default width is 1.
		 *
		 * The precision, if set, is ignored.
		 * @param x the char to format.
		 * @return the formatted String.
		 */
			private string printCFormat(char x)
			{
				var nPrint = 1;
				var width = this.fieldWidth;

				if (!this.fieldWidthSet)
					width = nPrint;

				var ca = new char[width];
				var i = 0;

				if (this.leftJustify)
				{
					ca[0] = x;

					for (i = 1; i <= width - nPrint; i++)
						ca[i] = ' ';
				}
				else
				{
					for (i = 0; i < width - nPrint; i++)
						ca[i] = ' ';

					ca[i] = x;
				}

				return new(ca);
			}

			/**
		 * Format method for the s conversion character and
		 * String argument.
		 *
		 * The only flag character that affects s format is
		 * the '-', meaning that the output should be left
		 * justified within the field.  The default is to
		 * pad with blanks on the left.
		 *
		 * The field width is treated as the minimum number
		 * of characters to be printed.  The default is the
		 * smaller of the number of characters in the the
		 * input and the precision.  Padding is with blanks
		 * by default.
		 *
		 * The precision, if set, specifies the maximum
		 * number of characters to be printed from the
		 * string.  A null digit string is treated
		 * as a 0.  The default is not to set a maximum
		 * number of characters to be printed.
		 * @param x the String to format.
		 * @return the formatted String.
		 */
			private string printSFormat(string x)
			{
				var nPrint = x.Length;
				var width = this.fieldWidth;

				if (this.precisionSet && nPrint > this.precision)
					nPrint = this.precision;

				if (!this.fieldWidthSet)
					width = nPrint;

				var n = 0;

				if (width > nPrint)
					n += width - nPrint;

				if (nPrint >= x.Length)
					n += x.Length;
				else
					n += nPrint;

				var ca = new char[n];
				var i = 0;

				if (this.leftJustify)
				{
					if (nPrint >= x.Length)
					{
						var csx = x.ToCharArray();

						for (i = 0; i < x.Length; i++)
							ca[i] = csx[i];
					}
					else
					{
						var csx = x[..nPrint].ToCharArray();

						for (i = 0; i < nPrint; i++)
							ca[i] = csx[i];
					}

					for (var j = 0; j < width - nPrint; j++, i++)
						ca[i] = ' ';
				}
				else
				{
					for (i = 0; i < width - nPrint; i++)
						ca[i] = ' ';

					if (nPrint >= x.Length)
					{
						var csx = x.ToCharArray();

						for (var j = 0; j < x.Length; i++, j++)
							ca[i] = csx[j];
					}
					else
					{
						var csx = x[..nPrint].ToCharArray();

						for (var j = 0; j < nPrint; i++, j++)
							ca[i] = csx[j];
					}
				}

				return new(ca);
			}

			/**
		 * Check for a conversion character.  If it is
		 * there, store it.
		 * @param x the String to format.
		 * @return <code>true</code> if the conversion
		 *     character is there, and
		 *     <code>false</code> otherwise.
		 */
			private bool setConversionCharacter()
			{
				/* idfgGoxXeEcs */
				var ret = false;
				this.conversionCharacter = '\0';

				if (this.pos < this.fmt.Length)
				{
					var c = this.fmt[this.pos];

					if (c == 'i'
						|| c == 'd'
						|| c == 'f'
						|| c == 'g'
						|| c == 'G'
						|| c == 'o'
						|| c == 'x'
						|| c == 'X'
						|| c == 'e'
						|| c == 'E'
						|| c == 'c'
						|| c == 's'
						|| c == '%')
					{
						this.conversionCharacter = c;
						this.pos++;
						ret = true;
					}
				}

				return ret;
			}

			/**
		 * Check for an h, l, or L in a format.  An L is
		 * used to control the minimum number of digits
		 * in an exponent when using floating point
		 * formats.  An l or h is used to control
		 * conversion of the input to a long or short,
		 * respectively, before formatting.  If any of
		 * these is present, store them.
		 */
			private void setOptionalHL()
			{
				this.optionalh = false;
				this.optionall = false;
				this.optionalL = false;

				if (this.pos < this.fmt.Length)
				{
					var c = this.fmt[this.pos];

					if (c == 'h')
					{
						this.optionalh = true;
						this.pos++;
					}
					else if (c == 'l')
					{
						this.optionall = true;
						this.pos++;
					}
					else if (c == 'L')
					{
						this.optionalL = true;
						this.pos++;
					}
				}
			}

			/**
		 * Set the precision.
		 */
			private void setPrecision()
			{
				var firstPos = this.pos;
				this.precisionSet = false;

				if (this.pos < this.fmt.Length && this.fmt[this.pos] == '.')
				{
					this.pos++;

					if (this.pos < this.fmt.Length && this.fmt[this.pos] == '*')
					{
						this.pos++;

						if (!this.setPrecisionArgPosition())
						{
							this.variablePrecision = true;
							this.precisionSet = true;
						}

						return;
					}

					while (this.pos < this.fmt.Length)
					{
						var c = this.fmt[this.pos];

						if (char.IsDigit(c))
							this.pos++;
						else
							break;
					}

					if (this.pos > firstPos + 1)
					{
						var sz = this.fmt.Substring(firstPos + 1, this.pos - (firstPos + 1));
						this.precision = int.Parse(sz);
						this.precisionSet = true;
					}
				}
			}

			/**
		 * Set the field width.
		 */
			private void setFieldWidth()
			{
				var firstPos = this.pos;
				this.fieldWidth = 0;
				this.fieldWidthSet = false;

				if (this.pos < this.fmt.Length && this.fmt[this.pos] == '*')
				{
					this.pos++;

					if (!this.setFieldWidthArgPosition())
					{
						this.variableFieldWidth = true;
						this.fieldWidthSet = true;
					}
				}
				else
				{
					while (this.pos < this.fmt.Length)
					{
						var c = this.fmt[this.pos];

						if (char.IsDigit(c))
							this.pos++;
						else
							break;
					}

					if (firstPos < this.pos && firstPos < this.fmt.Length)
					{
						var sz = this.fmt.Substring(firstPos, this.pos - firstPos);
						this.fieldWidth = int.Parse(sz);
						this.fieldWidthSet = true;
					}
				}
			}

			/**
		 * Store the digits <code>n</code> in %n$ forms.
		 */
			private void setArgPosition()
			{
				int xPos;

				for (xPos = this.pos; xPos < this.fmt.Length; xPos++)
				{
					if (!char.IsDigit(this.fmt[xPos]))
						break;
				}

				if (xPos > this.pos && xPos < this.fmt.Length)
				{
					if (this.fmt[xPos] == '$')
					{
						this.positionalSpecification = true;
						this.argumentPosition = int.Parse(this.fmt.Substring(this.pos, xPos - this.pos));
						this.pos = xPos + 1;
					}
				}
			}

			/**
		 * Store the digits <code>n</code> in *n$ forms.
		 */
			private bool setFieldWidthArgPosition()
			{
				var ret = false;
				int xPos;

				for (xPos = this.pos; xPos < this.fmt.Length; xPos++)
				{
					if (!char.IsDigit(this.fmt[xPos]))
						break;
				}

				if (xPos > this.pos && xPos < this.fmt.Length)
				{
					if (this.fmt[xPos] == '$')
					{
						this.positionalFieldWidth = true;
						this.argumentPositionForFieldWidth = int.Parse(this.fmt.Substring(this.pos, xPos - this.pos));
						this.pos = xPos + 1;
						ret = true;
					}
				}

				return ret;
			}

			/**
		 * Store the digits <code>n</code> in *n$ forms.
		 */
			private bool setPrecisionArgPosition()
			{
				var ret = false;
				int xPos;

				for (xPos = this.pos; xPos < this.fmt.Length; xPos++)
				{
					if (!char.IsDigit(this.fmt[xPos]))
						break;
				}

				if (xPos > this.pos && xPos < this.fmt.Length)
				{
					if (this.fmt[xPos] == '$')
					{
						this.positionalPrecision = true;
						this.argumentPositionForPrecision = int.Parse(this.fmt.Substring(this.pos, xPos - this.pos));
						this.pos = xPos + 1;
						ret = true;
					}
				}

				return ret;
			}

			public bool isPositionalSpecification()
			{
				return this.positionalSpecification;
			}

			public int getArgumentPosition()
			{
				return this.argumentPosition;
			}

			public bool isPositionalFieldWidth()
			{
				return this.positionalFieldWidth;
			}

			public int getArgumentPositionForFieldWidth()
			{
				return this.argumentPositionForFieldWidth;
			}

			public bool isPositionalPrecision()
			{
				return this.positionalPrecision;
			}

			public int getArgumentPositionForPrecision()
			{
				return this.argumentPositionForPrecision;
			}

			/**
		 * Set flag characters, one of '-+#0 or a space.
		 */
			private void setFlagCharacters()
			{
				/* '-+ #0 */
				this.thousands = false;
				this.leftJustify = false;
				this.leadingSign = false;
				this.leadingSpace = false;
				this.alternateForm = false;
				this.leadingZeros = false;

				for (; this.pos < this.fmt.Length; this.pos++)
				{
					var c = this.fmt[this.pos];

					if (c == '\'')
						this.thousands = true;
					else if (c == '-')
					{
						this.leftJustify = true;
						this.leadingZeros = false;
					}
					else if (c == '+')
					{
						this.leadingSign = true;
						this.leadingSpace = false;
					}
					else if (c == ' ')
					{
						if (!this.leadingSign)
							this.leadingSpace = true;
					}
					else if (c == '#')
						this.alternateForm = true;
					else if (c == '0')
					{
						if (!this.leftJustify)
							this.leadingZeros = true;
					}
					else
						break;
				}
			}

			/**
		 * The integer portion of the result of a decimal
		 * conversion (i, d, u, f, g, or G) will be
		 * formatted with thousands' grouping characters.
		 * For other conversions the flag is ignored.
		 */
			private bool thousands;

			/**
		 * The result of the conversion will be
		 * left-justified within the field.
		 */
			private bool leftJustify;

			/**
		 * The result of a signed conversion will always
		 * begin with a sign (+ or -).
		 */
			private bool leadingSign;

			/**
		 * Flag indicating that left padding with spaces is
		 * specified.
		 */
			private bool leadingSpace;

			/**
		 * For an o conversion, increase the precision to
		 * force the first digit of the result to be a
		 * zero.  For x (or X) conversions, a non-zero
		 * result will have 0x (or 0X) prepended to it.
		 * For e, E, f, g, or G conversions, the result
		 * will always contain a radix character, even if
		 * no digits follow the point.  For g and G
		 * conversions, trailing zeros will not be removed
		 * from the result.
		 */
			private bool alternateForm;

			/**
		 * Flag indicating that left padding with zeroes is
		 * specified.
		 */
			private bool leadingZeros;

			/**
		 * Flag indicating that the field width is *.
		 */
			private bool variableFieldWidth;

			/**
		 * If the converted value has fewer bytes than the
		 * field width, it will be padded with spaces or
		 * zeroes.
		 */
			private int fieldWidth;

			/**
		 * Flag indicating whether or not the field width
		 * has been set.
		 */
			private bool fieldWidthSet;

			/**
		 * The minimum number of digits to appear for the
		 * d, i, o, u, x, or X conversions.  The number of
		 * digits to appear after the radix character for
		 * the e, E, and f conversions.  The maximum number
		 *  of significant digits for the g and G 
		 * conversions.  The maximum number of bytes to be
		 * printed from a string in s and S conversions.
		 */
			private int precision;

			/** Default precision. */
			private static readonly int defaultDigits = 6;

			/**
		 * Flag indicating that the precision is *.
		 */
			private bool variablePrecision;

			/**
		 * Flag indicating whether or not the precision has
		 * been set.
		 */
			private bool precisionSet;

			/*
			 */
			private bool positionalSpecification;
			private int argumentPosition;
			private bool positionalFieldWidth;
			private int argumentPositionForFieldWidth;
			private bool positionalPrecision;
			private int argumentPositionForPrecision;

			/**
		 * Flag specifying that a following d, i, o, u, x,
		 * or X conversion character applies to a type
		 * short int.
		 */
			private bool optionalh;

			/**
		 * Flag specifying that a following d, i, o, u, x,
		 * or X conversion character applies to a type lont
		 * int argument.
		 */
			private bool optionall;

			/**
		 * Flag specifying that a following e, E, f, g, or
		 * G conversion character applies to a type double
		 * argument.  This is a noop.
		 */
			private bool optionalL;

			/** Control string type. */
			private char conversionCharacter = '\0';

			/**
		 * Position within the control string.  Used by
		 * the constructor.
		 */
			private int pos;

			/** Literal or control format string. */
			private string fmt;
		}

		/** Vector of control strings and format literals. */
		private readonly List<ConversionSpecification> vFmt = new();

		/** Character position.  Used by the constructor. */
		private int cPos;
	}
}
