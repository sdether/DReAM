using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Core;

namespace MindTouch.Dream {
    public interface IRequestContainer : IComponentContext, IDisposable { }

    public class RequestContainer : IRequestContainer, ITentantScopeManager {
        private readonly ILifetimeScope _parentScope;
        private readonly Action<ILifetimeScope> _disposalCallback;
        private bool _isDisposed;
        private 

        public RequestContainer(DreamContext context, ITenantRepository tenantRepository) {
            _parentScope = te

            _requestScore = _parentScope.BeginLifetimeScope(builder => {
                builder.RegisterInstance(context).ExternallyOwned();
                builder.RegisterInstance(this).As<ITentantScopeManager>().ExternallyOwned();
            })
            _disposalCallback = disposalCallback;
        }

        public object ResolveComponent(IComponentRegistration registration, IEnumerable<Parameter> parameters) {
            CheckDisposed();
            return _parentScope.ResolveComponent(registration, parameters);
        }

        public IComponentRegistry ComponentRegistry { get { CheckDisposed(); return _parentScope.ComponentRegistry; } }

        public void Dispose() {
            if(_isDisposed) {
                return;
            }
            _isDisposed = true;
            _parentScope.Dispose();
            _disposalCallback(_parentScope);
        }

        private void CheckDisposed() {
            if(_isDisposed) {
                throw new ObjectDisposedException("Request Container has already been disposed");
            }
        }

        public void RequestDisposal() {
            throw new NotImplementedException();
        }
    }
}