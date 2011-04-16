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
using MindTouch.Statsd;
using NUnit.Framework;
using MindTouch.Extensions.Time;

namespace MindTouch.Dream.Test.Statsd {

    [TestFixture]
    public class TimingStatisticTests {
        [Test]
        public void Can_Convert_to_bytes_without_sampling() {
            var stat = new TimingStatistic("test.foo", 15.Milliseconds());
            var bytes = stat.ToBytes(1);
            Assert.AreEqual("test.foo:15|ms", bytes.FromBytes());
        }

        [Test]
        public void Can_Convert_to_bytes_with_10th_sampling() {
            var stat = new TimingStatistic("test.foo", 15.Milliseconds());
            var bytes = stat.ToBytes(0.1);
            Assert.AreEqual("test.foo:15|ms|@0.1", bytes.FromBytes());
        }

        [Test]
        public void Can_Convert_to_bytes_with_100th_sampling() {
            var stat = new TimingStatistic("test.foo", 15.Milliseconds());
            var bytes = stat.ToBytes(0.01);
            Assert.AreEqual("test.foo:15|ms|@0.01", bytes.FromBytes());
        }
    }
}
