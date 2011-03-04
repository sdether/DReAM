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
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class DreamMessageTests {

        [Test]
        public void Can_get_status_message_from_message() {
            var msg = DreamMessage.Conflict("huh?");
            Assert.AreEqual(string.Format("HTTP Status: {0}({1})", msg.Status, (int)msg.Status), DreamMessage.GetStatusStringOrNull(msg));
        }

        [Test]
        public void Trying_to_get_status_message_from_null_message_returns_null() {
            DreamMessage msg = null;
            Assert.IsNull(DreamMessage.GetStatusStringOrNull(msg));
        }

        [Test]
        public void DreamResponseException_from_message_contains_status_message_in_ToString() {
            var msg = DreamMessage.Conflict("huh?");
            var exception = new DreamResponseException(msg);
            Assert.IsTrue(exception.ToString().Contains(DreamMessage.GetStatusStringOrNull(msg)));
        }

        [Test]
        public void DreamResponseException_from_message_contains_status_message_as_exception_message() {
            var msg = DreamMessage.Conflict("huh?");
            var exception = new DreamResponseException(msg);
            Assert.AreEqual(DreamMessage.GetStatusStringOrNull(msg), exception.Message);
        }

        [Test]
        public void DreamResponseException_from_null_message_returns_default_exception_message() {
            DreamMessage msg = null;
            var exception = new DreamResponseException(msg);
            Assert.AreEqual("Exception of type 'MindTouch.Dream.DreamResponseException' was thrown.",exception.Message);
        }

        [Ignore("xri stuff not working right")]
        [Test]
        public void XriTest() {
            DreamMessage result = Plug.New("xri://=roy").GetAsync().Wait();
            Assert.AreEqual("XRDS", result.ToDocument().Name);
        }
    }
}
