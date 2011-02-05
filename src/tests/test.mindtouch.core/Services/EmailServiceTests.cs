/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2009 MindTouch, Inc.
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

using System.IO;
using log4net;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Core.Test.Services {
    
    [TestFixture]
    public class EmailServiceTests {

        private static readonly ILog _log = LogUtils.CreateLog();

        private DreamHostInfo _hostInfo;
        private DreamServiceInfo _queueService;
        private Plug _plug;

        [TestFixtureSetUp]
        public void GlobalSetup() {
            _hostInfo = DreamTestHelper.CreateRandomPortHost();
            _queueService = DreamTestHelper.CreateService(_hostInfo, "sid://mindtouch.com/2009/01/dream/email", "email", new XDoc("config").Elem("folder", Path.GetTempPath()));
            _plug = _queueService.WithInternalKey().AtLocalHost;
        }

        [TestFixtureTearDown]
        public void GlobalTeardown() {
            _hostInfo.Dispose();
        }
        
        [Test]
        public void Can_send_email() {
            var email = XDocFactory.From(@"
                <email configuration=""default"">
                  <to>coreyk@d-tools.comx</to>
                  <from>support@d-tools.comx</from>
                  <subject>[D-Tools Documentation Wiki] A page has been updated</subject>
                  <pages>
                    <pageid>2185</pageid>
                  </pages>
                  <body>The following pages have changed:

                User:CoreyK
                [ http://support.d-tools.com/User%3ACoreyK ]

                 - 1 words added, 28 words removed by Adam Stone (Sun, 28 Feb 2010 22:34:55 -08:00)
                   [ http://support.d-tools.com/User%3ACoreyK?revision=8 ]

                </body>
                  <body html=""true"">
                    <h2>The following pages have changed:</h2>
                    <p>
                      <b>
                        <a href=""http://support.d-tools.com/User%3ACoreyK"">User:CoreyK</a>
                      </b> ( Last edited by <a href=""http://support.d-tools.com/User%3aAdam+Stone"">Adam Stone</a> )<br /><small><a href=""http://support.d-tools.com/User%3ACoreyK"">http://support.d-tools.com/User%3ACoreyK</a></small><br /><small><a href=""http://support.d-tools.com/index.php?title=Special%3APageAlerts&amp;id=2185"">Unsubscribe</a></small></p>
                    <p>
                      <ol>
                        <li>1 words added, 28 words removed ( <a href=""http://support.d-tools.com/User%3ACoreyK?revision=8"">Sun, 28 Feb 2010 22:34:55 -08:00</a> by <a href=""http://support.d-tools.com/User%3aAdam+Stone"">Adam Stone</a> )</li>
                      </ol>
                    </p>
                    <br />
                  </body>
                </email>", MimeType.TEXT_XML);
            var response = _plug.At("message").Post(email, new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);
        }
    }
}
