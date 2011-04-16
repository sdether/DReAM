using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MindTouch.Statsd;
using NUnit.Framework;
using MindTouch.Extensions.Time;

namespace MindTouch.Dream.Test.Statsd {

    [TestFixture,Ignore("these tests talk to a live server")]
    public class LiveStatsLoggerTests {

        [Test]
        public void Can_log_single_timing_event() {
            var logger = new StatsLogger(new StatsConfiguration {
                Host = "50.17.109.171",
                Port = 8125
            });
            logger.Timing("test.timed", 105.Milliseconds());
        }

        [Test]
        public void Can_increment_counter() {
            var logger = new StatsLogger(new StatsConfiguration {
                Host = "50.17.109.171",
                Port = 8125
            });
            var counter = 0;
            while(true) {
                counter++;
                logger.Increment("test.counter");
                Thread.Sleep(500);
                Console.WriteLine("counter: {0}", counter);
            }
        }
    }
}
