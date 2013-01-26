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

namespace MindTouch.Dream {
    public abstract class TenantRepository<T> : ITenantRepository where T : ITenant {
        protected readonly IDictionary<string, T> _tenantsByName = new Dictionary<string, T>();

        public IRequestContainer GetRequestContainer(ILifetimeScope serviceLifetimeScope, DreamContext context) {
            var name = GetTenantName(context);
            T tenant;
            lock(_tenantsByName) {
                if(!_tenantsByName.TryGetValue(name, out tenant)) {
                    _tenantsByName[name] = tenant = CreateTenant(name, serviceLifetimeScope.BeginLifetimeScope(DreamContainerScope.Tenant));
                }
            }
            var tenantScopeManager = new TenantScopeManager();
            var requestLifetimeScope = tenant.LifetimeScope.BeginLifetimeScope(DreamContainerScope.Request, builder => {
                builder.RegisterInstance(context).ExternallyOwned();
                builder.RegisterInstance(tenantScopeManager).ExternallyOwned();
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

        protected abstract string GetTenantName(DreamContext context);

        protected abstract T CreateTenant(string name, ILifetimeScope lifetimeScope);
    }
}