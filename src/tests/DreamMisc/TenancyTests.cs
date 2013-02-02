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
using Autofac;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;
using log4net;

namespace MindTouch.Dream.Test {
    using Yield = IEnumerator<IYield>;

    [TestFixture]
    public class TenancyTests {

        public class TestTenantRepository : TenantRepository<TenantData> {
            public bool IsDisposed;

            protected override string GetTenantName(DreamContext context) {
                return context.Request.Headers["tenant-id"];
            }

            protected override TenantData CreateTenantData(string name) {
                return new TenantData {
                    Name = name
                };
            }

            public override void Dispose() {
                base.Dispose();
                IsDisposed = true;
            }
        }

        public interface ILifetimeMarker { Guid Id { get; } }

        public interface IServiceLevel : ILifetimeMarker { }
        public interface IRequestLevel : ILifetimeMarker { }
        public interface ITenantLevel : ILifetimeMarker { TenantData TenantData { get; } }

        public abstract class LifetimeMarker {
            private readonly Guid _id = Guid.NewGuid();
            public Guid Id { get { return _id; } }
        }

        public class ServiceLevel : LifetimeMarker, IServiceLevel { }
        public class RequestLevel : LifetimeMarker, IRequestLevel { }
        public class TenantLevel : LifetimeMarker, ITenantLevel {
            public TenantLevel(TenantData tenantData) {
                TenantData = tenantData;
            }
            public TenantData TenantData { get; private set; }
        }


        public class TenantData : LifetimeMarker, IDisposable {
            public ILifetimeScope LifetimeScope { get; set; }
            public string Name { get; set; }
            public void Dispose() {
                IsDisposed = true;
            }
            public bool IsDisposed { get; private set; }
        }

        private DreamHostInfo _hostInfo;
        private Plug _plugA;
        private Plug _plugB;
        private static readonly Dictionary<string, TestTenantRepository> _repositories = new Dictionary<string, TestTenantRepository>();
        private static TestTenantRepository _repositoryA;
        private static TestTenantRepository _repositoryB;

        [TestFixtureSetUp]
        public void Setup() {
            _repositoryA = new TestTenantRepository();
            _repositories["a"] = _repositoryA;
            _repositoryB = new TestTenantRepository();
            _repositories["b"] = _repositoryB;
            var builder = new ContainerBuilder();
            builder.RegisterType<ServiceLevel>().As<IServiceLevel>().ServiceScoped();
            builder.RegisterType<TenantLevel>().As<ITenantLevel>().TenantScoped();
            builder.RegisterType<RequestLevel>().As<IRequestLevel>().RequestScoped();
            _hostInfo = DreamTestHelper.CreateRandomPortHost(new XDoc("config"), builder.Build());
            _hostInfo.Host.Self.At("load").With("name", "test.mindtouch.dream").Post(DreamMessage.Ok());
            var configA = new XDoc("config")
               .Elem("path", "a")
               .Elem("sid", "http://services.mindtouch.com/dream/test/2010/07/tenancytestservice");
            var result = _hostInfo.LocalHost.At("host", "services").With("apikey", _hostInfo.ApiKey).PostAsync(configA).Wait();
            Assert.IsTrue(result.IsSuccessful, result.ToText());
            _plugA = Plug.New(_hostInfo.LocalHost.Uri.WithoutQuery()).At("a");
            var configB = new XDoc("config")
               .Elem("path", "b")
               .Elem("sid", "http://services.mindtouch.com/dream/test/2010/07/tenancytestservice");
            result = _hostInfo.LocalHost.At("host", "services").With("apikey", _hostInfo.ApiKey).PostAsync(configB).Wait();
            Assert.IsTrue(result.IsSuccessful, result.ToText());
            _plugB = Plug.New(_hostInfo.LocalHost.Uri.WithoutQuery()).At("b");
        }

        [TestFixtureTearDown]
        public void Teardown() {
            _hostInfo.Dispose();
        }

        [Test]
        public void Service_level_instances_are_the_same_regardless_of_tenant_request() {
            var request1 = GetDocument(_plugA.At("servicelevel").WithHeader("tenant-id", "x"));
            var request2 = GetDocument(_plugA.At("servicelevel").WithHeader("tenant-id", "y"));
            Assert.AreEqual(
                request1["@id"].AsText ?? "--",
                request2["@id"].AsText
            );
        }

        [Test]
        public void Tenant_level_instances_are_different_for_different_tenant_requests() {
            var request1 = GetDocument(_plugA.At("tenantlevel").WithHeader("tenant-id", "x"));
            var request2 = GetDocument(_plugA.At("tenantlevel").WithHeader("tenant-id", "y"));
            Assert.AreNotEqual(
                request1["@id"].AsText,
                request2["@id"].AsText
            );
        }

        [Test]
        public void Tenant_level_instances_are_the_same_for_same_tenant_requests() {
            var request1 = GetDocument(_plugA.At("tenantlevel").WithHeader("tenant-id", "x"));
            var request2 = GetDocument(_plugA.At("tenantlevel").WithHeader("tenant-id", "x"));
            Assert.AreEqual(
                request1["@id"].AsText ?? "--",
                request2["@id"].AsText
            );
        }

        [Test]
        public void TenantData_resolved_as_feature_argument_and_dependency_are_the_same() {
            var tenant = GetDocument(_plugA.At("tenant").WithHeader("tenant-id", "x"));
            var tenantLevel = GetDocument(_plugA.At("tenantlevel").WithHeader("tenant-id", "x"));
            Assert.AreNotEqual(
                tenant["@id"].AsText,
                tenantLevel["@tentant-id"].AsText,
                "tenant id's don't match between feature vs. ctor resolved instances"
            );
        }

        [Test]
        public void TenantData_resolved_in_different_tenants_are_correct() {
            var tenantX = GetDocument(_plugA.At("tenant").WithHeader("tenant-id", "x"));
            var tenantY = GetDocument(_plugA.At("tenant").WithHeader("tenant-id", "y"));
            Assert.AreEqual(
                "x",
                tenantX["@name"].AsText,
                "wrong tenant"
            );
            Assert.AreEqual(
                "y",
                tenantY["@name"].AsText,
                "wrong tenant"
            );
        }

        [Test]
        public void Request_level_instances_are_always_different() {
            var request1 = GetDocument(_plugA.At("requestlevel").WithHeader("tenant-id", "x"));
            var request2 = GetDocument(_plugA.At("requestlevel").WithHeader("tenant-id", "x"));
            var request3 = GetDocument(_plugA.At("requestlevel").WithHeader("tenant-id", "y"));
            Assert.AreNotEqual(
                request1["@id"].AsText,
                request2["@id"].AsText,
                "r1 vs. r2"
            );
            Assert.AreNotEqual(
                request1["@id"].AsText,
                request3["@id"].AsText,
                "r1 vs. r3"
            );
            Assert.AreNotEqual(
                request2["@id"].AsText,
                request3["@id"].AsText,
                "r2 vs. r3"
            );
        }

        [Test]
        public void Same_id_tenants_in_separate_service_instances_are_separate() {
            var serviceATenant = GetDocument(_plugA.At("tenant").WithHeader("tenant-id", "x"));
            var serviceBTenant = GetDocument(_plugB.At("tenant").WithHeader("tenant-id", "x"));
            Assert.AreEqual(
                "x",
                serviceATenant["@name"].AsText,
                "wrong tenant"
            );
            Assert.AreEqual(
                "x",
                serviceBTenant["@name"].AsText,
                "wrong tenant"
            );
            Assert.AreNotEqual(
                serviceATenant["@id"].AsText,
                serviceBTenant["@id"].AsText,
                "tenants from different services were the same instances"
            );
        }

        [Test]
        public void TenantData_is_stored_in_repository() {
            var tenant = GetDocument(_plugA.At("tenant").WithHeader("tenant-id", "x"));
            var tenantData = _repositoryA["x"];
            Assert.AreEqual(
                tenant["@id"].AsText,
                tenantData.Id.ToString(),
                "tenant from request and from repository don't match"
            );
        }

        [Test]
        public void Can_shutdown_tenant() {
            var tenant1 = GetDocument(_plugA.At("tenant").WithHeader("tenant-id", "x"));
            var tenantData = _repositoryA["x"];
            var tenant2 = GetDocument(_plugA.At("shutdown").WithHeader("tenant-id", "x"));
            Assert.AreEqual(
                tenant1["@id"].AsText ?? "--",
                tenant2["@id"].AsText,
                "tenant id from shutdown request does not match active tenant"
            );
            Assert.IsTrue(tenantData.IsDisposed, "tenant was not disposed");
            Assert.IsNull(_repositoryA["x"], "repository still contained tenant");
        }


        [Test]
        public void Can_restart_tenant() {
            var tenantStart = GetDocument(_plugA.At("tenant").WithHeader("tenant-id", "x"));
            var tenantShutdown = GetDocument(_plugA.At("shutdown").WithHeader("tenant-id", "x"));
            var tenantRestart = GetDocument(_plugA.At("tenant").WithHeader("tenant-id", "x"));
            Assert.AreNotEqual(
                tenantStart["@id"].AsText,
                tenantRestart["@id"].AsText,
                "tenant id after restart is still the same"
            );
            var restarted = _repositoryA["x"];
            Assert.AreEqual(
                tenantRestart["@id"].AsText,
                restarted.Id.ToString(),
                "tenant from restart request and from repository don't match"
            );
        }

        [Test]
        public void Shutting_down_service_disposes_repository_and_all_its_tenants() {
            var repository = new TestTenantRepository();
            _repositories["foo"] = repository;
            var config = new XDoc("config")
               .Elem("path", "foo")
               .Elem("sid", "http://services.mindtouch.com/dream/test/2010/07/tenancytestservice");
            var service = _hostInfo.CreateService(config);
            var plug = service.WithoutKeys().AtLocalHost;
            var request1 = GetDocument(plug.At("requestlevel").WithHeader("tenant-id", "x"));
            var request2 = GetDocument(plug.At("requestlevel").WithHeader("tenant-id", "y"));
            var request3 = GetDocument(plug.At("requestlevel").WithHeader("tenant-id", "z"));
            var tenantX = repository["x"];
            var tenantY = repository["y"];
            var tenantZ = repository["z"];
            Assert.IsNotNull(tenantX, "didn't find tenant x");
            Assert.IsFalse(tenantX.IsDisposed, "tenant x was disposed");
            Assert.IsNotNull(tenantY, "didn't find tenant y");
            Assert.IsFalse(tenantY.IsDisposed, "tenant y was disposed");
            Assert.IsNotNull(tenantZ, "didn't find tenant z");
            Assert.IsFalse(tenantZ.IsDisposed, "tenant z was disposed");
            Assert.IsFalse(repository.IsDisposed, "repository was marked as disposed");
            Assert.IsTrue(service.WithPrivateKey().AtLocalHost.Delete().IsSuccessful, "delete failed");
            Assert.IsTrue(tenantX.IsDisposed, "tenant x wasn't disposed");
            Assert.IsTrue(tenantY.IsDisposed, "tenant y wasn't disposed");
            Assert.IsTrue(tenantZ.IsDisposed, "tenant z wasn't disposed");
            Assert.IsTrue(repository.IsDisposed, "repository wasn't disposed");
        }

        private XDoc GetDocument(Plug plug) {
            var msg = plug.Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(msg.IsSuccessful, msg.ToText());
            return msg.ToDocument();
        }

        [DreamService("MindTouch Tenancy Test Service", "Copyright (c) 2006-2013 MindTouch, Inc.",
            Info = "http://www.mindtouch.com",
            SID = new[] { "http://services.mindtouch.com/dream/test/2010/07/tenancytestservice" }
            )]
        public class TenancyTestService : DreamService {

            // --- Class Fields ---
            private static readonly ILog _log = LogUtils.CreateLog();

            //-- Fields ---
            private Plug _inner;

            //--- Features ---
            [DreamFeature("GET:tenant", "")]
            public XDoc GetTenant(TenantData tenant) {
                return new XDoc("tenant")
                    .Attr("id", tenant.Id.ToString())
                    .Attr("name", tenant.Name);
            }

            [DreamFeature("GET:servicelevel", "")]
            public XDoc GetServiceLevel(IServiceLevel serviceLevel) {
                return new XDoc("servicelevel")
                    .Attr("id", serviceLevel.Id.ToString());
            }

            [DreamFeature("GET:tenantlevel", "")]
            public XDoc GetTenantLevel(ITenantLevel tenantLevel) {
                return new XDoc("tenantlevel")
                    .Attr("id", tenantLevel.Id.ToString())
                    .Attr("tenant-id", tenantLevel.TenantData.Id.ToString());
            }

            [DreamFeature("GET:requestlevel", "")]
            public XDoc GetRequestLevel(IRequestLevel requestLevel) {
                return new XDoc("requestLevel")
                    .Attr("id", requestLevel.Id.ToString());
            }

            [DreamFeature("GET:shutdown", "")]
            public XDoc Shutdowntenant(TenantData tenant, ITentantScopeManager scopeManager) {
                scopeManager.RequestDisposal();
                return new XDoc("tenant")
                    .Attr("id", tenant.Id.ToString())
                    .Attr("name", tenant.Name);
            }

            protected override void InitializeLifetimeScope(IRegistrationInspector inspector, ContainerBuilder lifetimeScopeBuilder, XDoc config) {
                var path = config["path"].AsText;
                lifetimeScopeBuilder.Register(c => _repositories[path]).As<ITenantRepository>().ServiceScoped();
            }
        }
    }

}
