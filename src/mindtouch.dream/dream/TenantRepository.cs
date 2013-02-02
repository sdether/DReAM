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

namespace MindTouch.Dream {
    public abstract class TenantRepository<T> : IDisposable, ITenantRepository where T : class, IDisposable {

        private class Tenant {
            public T Data;
            public ILifetimeScope LifetimeScope;
            public string Name;
        }

        private readonly IDictionary<string, Tenant> _tenantsByName = new Dictionary<string, Tenant>();

        public IRequestContainer GetRequestContainer(ILifetimeScope serviceLifetimeScope, DreamContext context) {
            var name = GetTenantName(context);
            Tenant tenant;
            lock(_tenantsByName) {
                if(!_tenantsByName.TryGetValue(name, out tenant)) {
                    var tenantData = CreateTenantData(name);
                    _tenantsByName[name] = tenant = new Tenant {
                        Name = name,
                        LifetimeScope = serviceLifetimeScope.BeginLifetimeScope(
                            DreamContainerScope.Tenant, 
                            builder => builder.RegisterInstance(tenantData).As<T>().SingleInstance()
                        ),
                        Data = tenantData
                    };

                    // have to prime resolution of tenant or it won't get disposed with the scope
                    tenant.LifetimeScope.Resolve<T>();
                }
            }
            var tenantScopeManager = new TenantScopeManager();
            var requestLifetimeScope = tenant.LifetimeScope.BeginLifetimeScope(DreamContainerScope.Request, builder => {
                builder.RegisterInstance(context).ExternallyOwned();
                builder.RegisterInstance(tenantScopeManager).As<ITentantScopeManager>().ExternallyOwned();
            });
            return new RequestContainer(requestLifetimeScope, () => {
                if(!tenantScopeManager.DisposalRequested) {
                    return;
                }
                lock(_tenantsByName) {
                    _tenantsByName.Remove(tenant.Name);
                }
                tenant.LifetimeScope.Dispose();
            });
        }

        public T this[string name] {
            get {
                Tenant t;
                lock(_tenantsByName) {
                    _tenantsByName.TryGetValue(name, out t);
                }
                return t == null ? null : t.Data;
            }
        }

        protected abstract string GetTenantName(DreamContext context);

        protected abstract T CreateTenantData(string name);

        public virtual void Dispose() {
            lock(_tenantsByName) {
                foreach(var t in _tenantsByName.Values) {
                    t.LifetimeScope.Dispose();
                }
                _tenantsByName.Clear();
            }
        }
    }
}