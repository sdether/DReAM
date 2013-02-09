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
using System.Reflection;
using Autofac;
using Autofac.Builder;
using log4net;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Dream.Test {
    using Yield = IEnumerator<IYield>;

    // ReSharper disable InconsistentNaming
    [TestFixture]
    public class DreamPrologueEpilogueTests {
        private static readonly ILog _log = LogUtils.CreateLog();
        private DreamHostInfo _hostInfo;

        public interface IFoo { }
        public class Foo : IFoo { }

        [TestFixtureSetUp]
        public void Init() {
        }

        [TestFixtureTearDown]
        public void Teardown() {
            _hostInfo.Dispose();
        }

        [Test]
        public void Can_resolve_instance_in_prologue() {
            var builder = new ContainerBuilder();
            builder.RegisterType<Foo>().As<IFoo>().RequestScoped();
            _hostInfo = DreamTestHelper.CreateRandomPortHost(new XDoc("config"), builder.Build(ContainerBuildOptions.Default));
            _hostInfo.Host.Self.At("load").With("name", "test.mindtouch.dream").Post(DreamMessage.Ok());
            var config = new XDoc("config")
               .Elem("path", "test")
               .Elem("sid", "http://services.mindtouch.com/dream/test/2013/02/prologuetestserver");
            DreamMessage result = _hostInfo.LocalHost.At("host", "services").With("apikey", _hostInfo.ApiKey).PostAsync(config).Wait();
            Assert.IsTrue(result.IsSuccessful, result.ToText());
            var plug = Plug.New(_hostInfo.LocalHost.Uri.WithoutQuery()).At("test");
            var response = plug.At("ping").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(typeof(Foo).FullName, response.ToDocument()["class"].AsText);
        }

        [Test]
        public void Can_resolve_instance_in_epilogue() {
            var builder = new ContainerBuilder();
            builder.RegisterType<Foo>().As<IFoo>().RequestScoped();
            _hostInfo = DreamTestHelper.CreateRandomPortHost(new XDoc("config"), builder.Build(ContainerBuildOptions.Default));
            _hostInfo.Host.Self.At("load").With("name", "test.mindtouch.dream").Post(DreamMessage.Ok());
            var config = new XDoc("config")
               .Elem("path", "test")
               .Elem("sid", "http://services.mindtouch.com/dream/test/2013/02/epiloguetestserver");
            DreamMessage result = _hostInfo.LocalHost.At("host", "services").With("apikey", _hostInfo.ApiKey).PostAsync(config).Wait();
            Assert.IsTrue(result.IsSuccessful, result.ToText());
            var plug = Plug.New(_hostInfo.LocalHost.Uri.WithoutQuery()).At("test");
            var response = plug.At("ping").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(typeof(Foo).FullName, response.ToDocument()["class"].AsText);
        }

        [DreamService("MindTouch Prologue Test Service", "Copyright (c) 2006-2013 MindTouch, Inc.",
            Info = "http://www.mindtouch.com",
            SID = new[] { "http://services.mindtouch.com/dream/test/2013/02/prologuetestserver" }
        )]
        public class PrologueTestService : DreamService {

            //--- Features ---
            [DreamFeature("GET:ping", "")]
            public XDoc Ping(DreamContext context) {
                return context.GetState<XDoc>("body");
            }

            //--- Methods ---

            public override DreamFeatureStage[] Prologues {
                get { return new[] { new DreamFeatureStage(this, "Prologue", DreamAccess.Public) }; }
            }

            private DreamMessage Prologue(string path, IFoo foo, DreamContext context) {
                _log.DebugFormat("prologue: {0}", foo);
                context.SetState("body", new XDoc("body").Elem("class", foo.GetType().FullName));
                return DreamMessage.Ok();
            }
        }

        [DreamService("MindTouch Prologue Test Service", "Copyright (c) 2006-2013 MindTouch, Inc.",
            Info = "http://www.mindtouch.com",
            SID = new[] { "http://services.mindtouch.com/dream/test/2013/02/epiloguetestserver" }
        )]
        public class EpilogueTestService : DreamService {

            //--- Features ---
            [DreamFeature("GET:ping", "")]
            public DreamMessage Ping(DreamContext context) {
                return DreamMessage.Ok();
            }

            //--- Methods ---

            public override DreamFeatureStage[] Epilogues {
                get { return new[] { new DreamFeatureStage(this, "Epilogue", DreamAccess.Public) }; }
            }

            private DreamMessage Epilogue(string path, IFoo foo, DreamContext context) {
                return DreamMessage.Ok(new XDoc("body").Elem("class", foo.GetType().FullName));
            }
        }
    }
    // ReSharper restore InconsistentNaming
}
