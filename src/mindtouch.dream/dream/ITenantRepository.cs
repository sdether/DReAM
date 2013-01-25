using System;
using Autofac;
using Autofac.Core.Lifetime;

namespace MindTouch.Dream {
    public interface ITenantRepository {
        ILifetimeScope GetTenantScope(DreamContext context);
        void DisposeTenantScope(ILifetimeScope scope);
    }

    public interface ITentantScopeManager {
        void RequestDisposal();
    }

    public class LifetimeScopeContainer : ITentantScopeManager, IDisposable {
        public readonly ILifetimeScope LifetimeScope;
        private readonly ITenantRepository _repository;
        private bool _shutdownRequested;
        public LifetimeScopeContainer(ILifetimeScope scope, ITenantRepository repository) {
            LifetimeScope = scope;
            _repository = repository;
        }

        void ITentantScopeManager.RequestShutdown() {
            throw new System.NotImplementedException();
        }

        public void Dispose() {
            LifetimeScope.Dispose();
        }
    }
}