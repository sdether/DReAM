using System;
using System.Collections.Generic;
using System.Linq;

namespace MindTouch.Statsd {
    public static class StatsLoggerEx {
        public static void Increment(this IStatsLogger logger, params string[] stat) {
            logger.UpdateCounter(stat.Select(x => new CountingStat(x, 1)), 1);
        }

        public static void Increment(this IStatsLogger logger, IEnumerable<string> stats, double sampling) {
            logger.UpdateCounter(stats.Select(x => new CountingStat(x, 1)), sampling);
        }

        public static void Decrement(this IStatsLogger logger, params string[] stat) {
            logger.UpdateCounter(stat.Select(x => new CountingStat(x, -1)), 1);
        }

        public static void Decrement(this IStatsLogger logger, IEnumerable<string> stats, double sampling) {
            logger.UpdateCounter(stats.Select(x => new CountingStat(x, -1)), sampling);
        }

        public static void UpdateCounter(this IStatsLogger logger, string stat, int counter) {
            logger.UpdateCounter(new[] { new CountingStat(stat, counter) }, 1);
        }

        public static void UpdateCounter(this IStatsLogger logger, string stat, int counter, double sampling) {
            logger.UpdateCounter(new[] { new CountingStat(stat, counter) }, sampling);
        }

        public static void UpdateCounter(this IStatsLogger logger, params CountingStat[] stat) {
            logger.UpdateCounter(stat, 1);
        }

        public static void Timing(this IStatsLogger logger, string stat, TimeSpan time) {
            logger.Timing(new[] { new TimingStat(stat, time) }, 1);
        }

        public static void Timing(this IStatsLogger logger, string stat, TimeSpan time, double sampling) {
            logger.Timing(new[] { new TimingStat(stat, time) }, sampling);
        }

        public static void Timing(this IStatsLogger logger, params TimingStat[] stat) {
            logger.Timing(stat, 1);
        }

    }
}