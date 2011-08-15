/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2011 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit wiki.developer.mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MindTouch.Traum.Webclient {


    /// <summary>
    /// Static utility class containing extension and helper methods for working with strings.
    /// </summary>
    public static class StringEx {

        //--- Class Fields ---

        /// <summary>
        /// An empty string array. Array counterpart to <see cref="string.Empty"/>.
        /// </summary>
        public static readonly string[] EmptyArray = new string[0];

        //--- Extension Methods ---

        /// <summary>
        /// Replace all occurences of a number of strings.
        /// </summary>
        /// <param name="source">Source string.</param>
        /// <param name="replacements">Array of strings to match and their replacements. Each string to be replaced at odd index i must have a replacement value at index i+1.</param>
        /// <returns>String with replacements performed on it.</returns>
        public static string ReplaceAll(this string source, params string[] replacements) {
            if((replacements.Length & 1) != 0) {
                throw new ArgumentException("length of string replacements must be even", "replacements");
            }
            if(string.IsNullOrEmpty(source) || (replacements.Length == 0)) {
                return source;
            }

            // loop over each character in source string
            StringBuilder result = null;
            int currentIndex = 0;
            int lastIndex = 0;
            for(; currentIndex < source.Length; ++currentIndex) {
                
                // loop over all replacement strings
                for(int j = 0; j < replacements.Length; j += 2) {

                    // check if we found a matching replacement at the current position
                    if(string.CompareOrdinal(source, currentIndex, replacements[j], 0, replacements[j].Length) == 0) {
                        result = result ?? new StringBuilder(source.Length * 2);

                        // append any text we've skipped over so far
                        if(lastIndex < currentIndex) {
                            result.Append(source, lastIndex, currentIndex - lastIndex);
                        }

                        // append replacement
                        result.Append(replacements[j + 1]);
                        currentIndex += replacements[j].Length - 1;
                        lastIndex = currentIndex + 1;
                        goto next;
                    }
                }
            next:
                continue;
            }

            // append any text we've skipped over so far
            if(lastIndex < currentIndex) {

                // check if nothing has been replaced; in that case, return the original string
                if(lastIndex == 0) {
                    return source;
                }
                result = result ?? new StringBuilder(source.Length * 2);
                result.Append(source, lastIndex, currentIndex - lastIndex);
            }
            return result.ToString();
        }

        /// <summary>
        /// Escape string.
        /// </summary>
        /// <param name="text">Sources string.</param>
        /// <returns>Escaped string.</returns>
        public static string EscapeString(this string text) {
            if(string.IsNullOrEmpty(text)) {
                return string.Empty;
            }

            // escape any special characters
            StringBuilder result = new StringBuilder(2 * text.Length);
            foreach(char c in text) {
                switch(c) {
                case '\a':
                    result.Append("\\a");
                    break;
                case '\b':
                    result.Append("\\b");
                    break;
                case '\f':
                    result.Append("\\f");
                    break;
                case '\n':
                    result.Append("\\n");
                    break;
                case '\r':
                    result.Append("\\r");
                    break;
                case '\t':
                    result.Append("\\t");
                    break;
                case '\v':
                    result.Append("\\v");
                    break;
                case '"':
                    result.Append("\\\"");
                    break;
                case '\'':
                    result.Append("\\'");
                    break;
                case '\\':
                    result.Append("\\\\");
                    break;
                default:
                    if((c < 32) || (c >= 127)) {
                        result.Append("\\u");
                        result.Append(((int)c).ToString("x4"));
                    } else {
                        result.Append(c);
                    }
                    break;
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// Unescape string.
        /// </summary>
        /// <param name="text">Escaped string.</param>
        /// <returns>Unescaped string.</returns>
        public static string UnescapeString(this string text) {
            var result = new StringBuilder(text.Length);
            for(int i = 0; i < text.Length; ++i) {
                char c = text[i];
                if((c == '\\') && (++i < text.Length)) {
                    switch(text[i]) {
                    case 'a':
                        result.Append('\a');
                        break;
                    case 'b':
                        result.Append("\b");
                        break;
                    case 'f':
                        result.Append('\f');
                        break;
                    case 'n':
                        result.Append("\n");
                        break;
                    case 'r':
                        result.Append("\r");
                        break;
                    case 't':
                        result.Append("\t");
                        break;
                    case 'u':
                        string code = text.Substring(i + 1, 4);
                        if(code.Length != 4) {
                            throw new FormatException("illegal \\u escape sequence");
                        }
                        result.Append((char)int.Parse(code, NumberStyles.AllowHexSpecifier));
                        i += 4;
                        break;
                    case 'v':
                        result.Append('\v');
                        break;
                    default:
                        result.Append(text[i]);
                        break;
                    }
                } else {
                    result.Append(c);
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// Create a string by repeating a pattern.
        /// </summary>
        /// <param name="pattern">Pattern to repeat.</param>
        /// <param name="count">Repetitions of pattern.</param>
        /// <returns>Pattern string.</returns>
        public static string RepeatPattern(this string pattern, int count) {
            if(pattern == null) {
                throw new ArgumentNullException("pattern");
            }
            var result = new StringBuilder();
            for(int i = 0; i < count; ++i) {
                result.Append(pattern);
            }
            return result.ToString();
        }

        /// <summary>
        /// Shortcut for invariant <see cref="string.Equals(string)"/>
        /// </summary>
        /// <param name="left">Left-hand string to compare.</param>
        /// <param name="right">Right-hand string to compare.</param>
        /// <param name="ignoreCase"><see langword="True"/> if case should not be considered in comparison.</param>
        /// <returns><see langword="True"/> if left- and right-hand sides are equal.</returns>
        public static bool EqualsInvariant(this string left, string right, bool ignoreCase) {
            return string.Equals(left, right, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        /// <summary>
        /// Shortcut for invariant <see cref="string.Equals(string)"/>
        /// </summary>
        /// <param name="left">Left-hand string to compare.</param>
        /// <param name="right">Right-hand string to compare.</param>
        /// <returns><see langword="True"/> if left- and right-hand sides are equal.</returns>
        public static bool EqualsInvariant(this string left, string right) {
            return string.Equals(left, right, StringComparison.Ordinal);
        }

        /// <summary>
        /// Shortcut for case-insensitive, invariant <see cref="string.Equals(string)"/>
        /// </summary>
        /// <param name="left">Left-hand string to compare.</param>
        /// <param name="right">Right-hand string to compare.</param>
        /// <returns><see langword="True"/> if left- and right-hand sides are equal.</returns>
        public static bool EqualsInvariantIgnoreCase(this string left, string right) {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Shortcut for invariant <see cref="string.Compare(string,string)"/>
        /// </summary>
        /// <param name="left">Left-hand string to compare.</param>
        /// <param name="right">Right-hand string to compare.</param>
        /// <param name="ignoreCase"><see langword="True"/> if case should not be considered in comparison.</param>
        /// <returns>
        /// A 32-bit signed integer indicating the lexical relationship between the two comparands.  Value Condition Less than zero 
        /// left is less than right. Zero left equals right. Greater than zero left is greater than right.
        /// </returns>
        public static int CompareInvariant(this string left, string right, bool ignoreCase) {
            return string.Compare(left, right, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        /// <summary>
        /// Shortcut for invariant <see cref="string.Compare(string,string)"/>
        /// </summary>
        /// <param name="left">Left-hand string to compare.</param>
        /// <param name="right">Right-hand string to compare.</param>
        /// <returns>
        /// A 32-bit signed integer indicating the lexical relationship between the two comparands.  Value Condition Less than zero 
        /// left is less than right. Zero left equals right. Greater than zero left is greater than right.
        /// </returns>
        public static int CompareInvariant(this string left, string right) {
            return string.Compare(left, right, StringComparison.Ordinal);
        }

        /// <summary>
        /// Shortcut for case-insensitive, invariant <see cref="string.Compare(string,string)"/>
        /// </summary>
        /// <param name="left">Left-hand string to compare.</param>
        /// <param name="right">Right-hand string to compare.</param>
        /// <returns>
        /// A 32-bit signed integer indicating the lexical relationship between the two comparands.  Value Condition Less than zero 
        /// left is less than right. Zero left equals right. Greater than zero left is greater than right.
        /// </returns>
        public static int CompareInvariantIgnoreCase(this string left, string right) {
            return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Shortcut for invariant <see cref="string.StartsWith(string)"/>
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to compare.</param>
        /// <param name="ignoreCase"><see langword="True"/> if case should not be considered in comparison.</param>
        /// <returns><see langword="True"/> if value matches the beginning of the input string; otherwise, <see langword="False"/>.</returns>
        public static bool StartsWithInvariant(this string text, string value, bool ignoreCase) {
            return text.StartsWith(value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        /// <summary>
        /// Shortcut for invariant <see cref="string.StartsWith(string)"/>
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to compare.</param>
        /// <returns><see langword="True"/> if value matches the beginning of the input string; otherwise, <see langword="False"/>.</returns>
        public static bool StartsWithInvariant(this string text, string value) {
            return text.StartsWith(value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Shortcut for case-insensitive, invariant <see cref="string.StartsWith(string)"/>
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to compare.</param>
        /// <returns><see langword="True"/> if value matches the beginning of the input string; otherwise, <see langword="False"/>.</returns>
        public static bool StartsWithInvariantIgnoreCase(this string text, string value) {
            return text.StartsWith(value, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Shortcut for invariant <see cref="string.EndsWith(string)"/>
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to compare.</param>
        /// <param name="ignoreCase"><see langword="True"/> if case should not be considered in comparison.</param>
        /// <returns><see langword="True"/> if value matches the end of the input string; otherwise, <see langword="False"/>.</returns>
        public static bool EndsWithInvariant(this string text, string value, bool ignoreCase) {
            return text.EndsWith(value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        /// <summary>
        /// Shortcut for invariant <see cref="string.EndsWith(string)"/>
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to compare.</param>
        /// <returns><see langword="True"/> if value matches the end of the input string; otherwise, <see langword="False"/>.</returns>
        public static bool EndsWithInvariant(this string text, string value) {
            return text.EndsWith(value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Shortcut for case-insensitive, invariant <see cref="string.EndsWith(string)"/>
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to compare.</param>
        /// <returns><see langword="True"/> if value matches the end of the input string; otherwise, <see langword="False"/>.</returns>
        public static bool EndsWithInvariantIgnoreCase(this string text, string value) {
            return text.EndsWith(value, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Shortcut for invariant <see cref="string.IndexOf(string)"/>
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to find.</param>
        /// <param name="ignoreCase"><see langword="True"/> if case should not be considered in comparison.</param>
        /// <returns>
        /// The zero-based index position of value if that string is found, or -1 if it is not. If value is <see cref="string.Empty"/>, the return value is 0.
        /// </returns>
        public static int IndexOfInvariant(this string text, string value, bool ignoreCase) {
            return text.IndexOf(value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        /// <summary>
        /// Shortcut for invariant <see cref="string.IndexOf(string)"/>
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to find.</param>
        /// <returns>
        /// The zero-based index position of value if that string is found, or -1 if it is not. If value is <see cref="string.Empty"/>, the return value is 0.
        /// </returns>
        public static int IndexOfInvariant(this string text, string value) {
            return text.IndexOf(value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Shortcut for case-insensitive, invariant <see cref="string.IndexOf(string)"/>
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to find.</param>
        /// <returns>
        /// The zero-based index position of value if that string is found, or -1 if it is not. If value is <see cref="string.Empty"/>, the return value is 0.
        /// </returns>
        public static int IndexOfInvariantIgnoreCase(this string text, string value) {
            return text.IndexOf(value, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Shortcut for invariant <see cref="string.LastIndexOf(string)"/>
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to find.</param>
        /// <param name="ignoreCase"><see langword="True"/> if case should not be considered in comparison.</param>
        /// <returns>
        /// The index position of the value parameter if that string is found, or -1 if it is not. If value is <see cref="string.Empty"/>, the return value is the last index position in this instance.
        /// </returns>
        public static int LastIndexOfInvariant(this string text, string value, bool ignoreCase) {
            return text.LastIndexOf(value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        /// <summary>
        /// Shortcut for invariant <see cref="string.LastIndexOf(string)"/>
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to find.</param>
        /// <returns>
        /// The index position of the value parameter if that string is found, or -1 if it is not. If value is <see cref="string.Empty"/>, the return value is the last index position in this instance.
        /// </returns>
        public static int LastIndexOfInvariant(this string text, string value) {
            return text.LastIndexOf(value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Shortcut for case-insensitive, invariant <see cref="string.LastIndexOf(string)"/>
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to find.</param>
        /// <returns>
        /// The index position of the value parameter if that string is found, or -1 if it is not. If value is <see cref="string.Empty"/>, the return value is the last index position in this instance.
        /// </returns>
        public static int LastIndexOfInvariantIgnoreCase(this string text, string value) {
            return text.LastIndexOf(value, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determine whether a string is contained in another string using invariant comparison.
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to find.</param>
        /// <returns><see langword="True"/> if value is found in the input string; otherwise, <see langword="False"/>.</returns>
        public static bool ContainsInvariant(this string text, string value) {
            return IndexOfInvariant(text, value) >= 0;
        }

        /// <summary>
        /// Determine whether a string is contained in another string using case-insensitive, invariant comparison.
        /// </summary>
        /// <param name="text">Text to examine</param>
        /// <param name="value">The System.String to find.</param>
        /// <returns><see langword="True"/> if value is found in the input string; otherwise, <see langword="False"/>.</returns>
        public static bool ContainsInvariantIgnoreCase(this string text, string value) {
            return IndexOfInvariantIgnoreCase(text, value) >= 0;
        }

        /// <summary>
        /// Get Hashcode for a string using the invariant comparer.
        /// </summary>
        /// <param name="text">Text to examine.</param>
        /// <param name="ignoreCase"><see langword="True"/> if case should not be considered in comparison.</param>
        /// <returns>Hashcode for the input string.</returns>
        public static int GetHashCodeInvariant(this string text, bool ignoreCase) {
            return (ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal).GetHashCode(text);
        }

        /// <summary>
        /// Get Hashcode for a string using the invariant comparer.
        /// </summary>
        /// <param name="text">Text to examine.</param>
        /// <returns>Hashcode for the input string.</returns>
        public static int GetHashCodeInvariant(this string text) {
            return StringComparer.Ordinal.GetHashCode(text);
        }

        /// <summary>
        /// Get Hashcode for a string using the case-insensitive, invariant comparer.
        /// </summary>
        /// <param name="text">Text to examine.</param>
        /// <returns>Hashcode for the input string.</returns>
        public static int GetHashCodeInvariantIgnoreCase(this string text) {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(text);
        }
    }
}
