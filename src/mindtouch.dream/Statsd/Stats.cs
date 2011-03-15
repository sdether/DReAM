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
using System.Collections.Generic;
using System.Linq;

namespace MindTouch.Statsd {
    public class Stats {

        //--- Types ---
        private class NullStatsLogger : IStatsLogger {

            //--- Methods ---
            public void UpdateCounter(IEnumerable<CountingStatistic> stats, double sampling) { }
            public void Timing(IEnumerable<TimingStatistic> stats, double sampling) { }
        }

        private class ProxyStatsLogger : IStatsLogger {

            //--- Methods ---
            public void UpdateCounter(IEnumerable<CountingStatistic> stats, double sampling) {
                _global.UpdateCounter(stats, sampling);
            }

            public void Timing(IEnumerable<TimingStatistic> stats, double sampling) {
                _global.Timing(stats, sampling);
            }
        }

        private class PrefixedProxyStatsLogger : IStatsLogger {

            //--- Fields ---
            private readonly string _prefix;

            //--- Constructors ---
            public PrefixedProxyStatsLogger(string prefix) {
                _prefix = prefix;
            }

            //--- Methods ---
            public void UpdateCounter(IEnumerable<CountingStatistic> stats, double sampling) {
                _global.UpdateCounter(stats.Select(x => new CountingStatistic(PrefixedName(x.Name), x.Count)), sampling);
            }

            public void Timing(IEnumerable<TimingStatistic> stats, double sampling) {
                _global.Timing(stats.Select(x => new TimingStatistic(PrefixedName(x.Name), x.Time)), sampling);
            }

            private string PrefixedName(string stat) {
                return _prefix + "." + stat;
            }
        }

        //--- Class Fields ---
        public static readonly IStatsLogger Null = new NullStatsLogger();
        private static IStatsLogger _global = Null;

        //--- Class Properties ---
        public static IStatsLogger Global { get { return _global; } }

        //--- Class Methods ---
        public static void Configure(StatsConfiguration config) {
            _global = new StatsLogger(new StatsConfiguration(config));
        }

        public static IStatsLogger CreateLogger() {
            return new ProxyStatsLogger();
        }

        public static IStatsLogger CreateLogger(string prefix) {
            return new PrefixedProxyStatsLogger(prefix);
        }

    }
}