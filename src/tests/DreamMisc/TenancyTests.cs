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
using Autofac;
using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class TenancyTests {


        public class TestTenantRepository : TenantRepository<TestTenantRepository.Tenant> {

            public class Tenant : ITenant {
                public ILifetimeScope LifetimeScope { get; set; }
                public string Name { get; set; }
            }

            protected override string GetTenantName(DreamContext context) {
                return context.Request.Headers["tenant-id"];
            }

            protected override Tenant CreateTenant(string name, ILifetimeScope lifetimeScope) {
                return new Tenant {
                    LifetimeScope = lifetimeScope,
                    Name = name
                };
            }

            public IDictionary<string, Tenant> Tenants { get { return _tenantsByName; } }
        }


        [TestFixtureSetUp]
        public void Setup() {

        }
    }
}
