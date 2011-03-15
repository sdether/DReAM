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
using System.Net.Sockets;
using System.Text;

namespace MindTouch.Statsd {

    public class StatsdConfiguration {
        public static readonly StatsdConfiguration Global = new StatsdConfiguration();
        public string Host { get; set; }
        public int Port { get; set; }
        public string Prefix { get; set; }
    }

    public class StatsdLogger {
        private static readonly StatsdLogger _global = new StatsdLogger(StatsdConfiguration.Global);

        public static StatsdLogger Global { get { return _global; } }

        private readonly StatsdConfiguration _configuration;
        private readonly UdpClient _client;

        public StatsdLogger(StatsdConfiguration configuration) {
            _configuration = configuration;
            _client = new UdpClient();
        }

        public void Increment(params string[] stat) {

        }

        public void Decrement(params string[] stat) {

        }

        public void Increment(IEnumerable<string> stats, double sampling) {

        }

        public void Update(params CountingStat[] stat) {
            Send(stat);
        }

        public void Send(IEnumerable<AStat> stats) {
            foreach(var stat in stats) {
                var bytes = stat.ToBytes();
                _client.Send(bytes, bytes.Length, _configuration.Host, _configuration.Port);
            }
        }
    }

    public abstract class AStat {
        public string Name;
        public double Sampling = 1;
        public byte[] ToBytes() {
            return Encoding.ASCII.GetBytes(string.Format("{0}:{1}{2}", Name, Value, GetSampling()));
        }

        private string GetSampling() {
            return Sampling == 1 ? "" : "@" + Sampling;
        }

        protected abstract string Value { get; }
    }

    public class TimingStat : AStat {
        public int Count;
        protected override string Value { get { return Count + "|c"; } }
    }

    public class CountingStat : AStat {
        public TimeSpan Time;
        protected override string Value { get { return Time.TotalMilliseconds.ToString("0") + "|ms"; } }
    }
}
