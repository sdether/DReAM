using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MindTouch.Traum {
    internal class DateTimeUtil {
        //--- Class Fields ---

        /// <summary>
        /// The Unix Epoch time, i.e. seconds since January 1, 1970 (UTC).
        /// </summary>
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        //---- Class Methods ---

        /// <summary>
        /// Get a DateTime instance from utc-based unix epoch time.
        /// </summary>
        /// <param name="secondsSinceEpoch">Seconds since January 1, 1970 (UTC).</param>
        /// <returns>DateTime instance.</returns>
        public static DateTime FromEpoch(uint secondsSinceEpoch) {
            return Epoch.AddSeconds(secondsSinceEpoch);
        }

        /// <summary>
        /// Parse a date using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="value">Source datetime string.</param>
        /// <returns>DateTime</returns>
        public static DateTime ParseInvariant(string value) {
            return DateTime.Parse(value, CultureInfo.InvariantCulture.DateTimeFormat);
        }

        /// <summary>
        /// Parse a date using <see cref="CultureInfo.InvariantCulture"/> and an exact date format.
        /// </summary>
        /// <param name="value">Source datetime string.</param>
        /// <param name="format">DateTime format string.</param>
        /// <returns>DateTime</returns>
        public static DateTime ParseExactInvariant(string value, string format) {
            return DateTime.ParseExact(value, format, CultureInfo.InvariantCulture.DateTimeFormat);
        }

        /// <summary>
        /// Try to parse a date using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="value">Source datetime string.</param>
        /// <param name="date">Output location</param>
        /// <returns><see langword="True"/> if a date was successfully parsed.</returns>
        public static bool TryParseInvariant(string value, out DateTime date) {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeUniversal, out date);
        }

        /// <summary>
        /// Try to parse a date using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="value">Source datetime string.</param>
        /// <param name="format">DateTime format string.</param>
        /// <param name="date">Output location</param>
        /// <returns><see langword="True"/> if a date was successfully parsed.</returns>
        public static bool TryParseExactInvariant(string value, string format, out DateTime date) {
            return DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeUniversal, out date);
        }
    }
}
