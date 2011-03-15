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
using System.Collections.Generic;
using System.Linq;

namespace MindTouch.Statsd {
    public static class StatsLoggerEx {

        //--- Extension Methods ---
        public static void Increment(this IStatsLogger logger, params string[] stat) {
            logger.UpdateCounter(stat.Select(x => new CountingStatistic(x, 1)), 1);
        }

        public static void Increment(this IStatsLogger logger, IEnumerable<string> stats, double sampling) {
            logger.UpdateCounter(stats.Select(x => new CountingStatistic(x, 1)), sampling);
        }

        public static void Decrement(this IStatsLogger logger, params string[] stat) {
            logger.UpdateCounter(stat.Select(x => new CountingStatistic(x, -1)), 1);
        }

        public static void Decrement(this IStatsLogger logger, IEnumerable<string> stats, double sampling) {
            logger.UpdateCounter(stats.Select(x => new CountingStatistic(x, -1)), sampling);
        }

        public static void UpdateCounter(this IStatsLogger logger, string stat, int counter) {
            logger.UpdateCounter(new[] { new CountingStatistic(stat, counter) }, 1);
        }

        public static void UpdateCounter(this IStatsLogger logger, string stat, int counter, double sampling) {
            logger.UpdateCounter(new[] { new CountingStatistic(stat, counter) }, sampling);
        }

        public static void UpdateCounter(this IStatsLogger logger, params CountingStatistic[] stat) {
            logger.UpdateCounter(stat, 1);
        }

        public static void Timing(this IStatsLogger logger, string stat, TimeSpan time) {
            logger.Timing(new[] { new TimingStatistic(stat, time) }, 1);
        }

        public static void Timing(this IStatsLogger logger, string stat, TimeSpan time, double sampling) {
            logger.Timing(new[] { new TimingStatistic(stat, time) }, sampling);
        }

        public static void Timing(this IStatsLogger logger, params TimingStatistic[] statistic) {
            logger.Timing(statistic, 1);
        }

    }
}