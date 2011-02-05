using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using Autofac;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Dream {
    /// <summary>
    /// Provides the interface for the Dream host environment.
    /// </summary>
    public interface IDreamEnvironment {

        //--- Properties ---

        /// <summary>
        /// Host Globally Unique Identifier.
        /// </summary>
        Guid GlobalId { get; }

        /// <summary>
        /// <see langword="True"/> if the host is running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// The host's local uri.
        /// </summary>
        XUri LocalMachineUri { get; }

        /// <summary>
        /// <see cref="Plug"/> for host.
        /// </summary>
        Plug Self { get; }

        /// <summary>
        /// Current Activity messages.
        /// </summary>
        Tuplet<DateTime, string>[] ActivityMessages { get; }

        //--- Methods ---

        /// <summary>
        /// Initialize the host.
        /// </summary>
        /// <param name="config">Configuration document.</param>
        void Initialize(XDoc config);

        /// <summary>
        /// Shut down the host.
        /// </summary>
        void Deinitialize();

        /// <summary>
        /// Asynchronously submit a request to the host.
        /// </summary>
        /// <param name="verb">Request Http verb.</param>
        /// <param name="uri">Request Uri.</param>
        /// <param name="user">Request user, if applicable.</param>
        /// <param name="request">Request message.</param>
        /// <param name="response">The response message synchronization instance to be returned by this method.</param>
        /// <returns>Synchronization handle for request.</returns>
        Result<DreamMessage> SubmitRequestAsync(string verb, XUri uri, IPrincipal user, DreamMessage request, Result<DreamMessage> response);

        /// <summary>
        /// Block execution until host has shut down.
        /// </summary>
        void WaitUntilShutdown();

        /// <summary>
        /// Add an activity.
        /// </summary>
        /// <param name="key">Activity key.</param>
        /// <param name="description">Activity description.</param>
        void AddActivityDescription(object key, string description);

        /// <summary>
        /// Remove an activity.
        /// </summary>
        /// <param name="key">Activity key.</param>
        void RemoveActivityDescription(object key);

        /// <summary>
        /// Update the information message for a source.
        /// </summary>
        /// <param name="source">Source to update.</param>
        /// <param name="message">Info message.</param>
        void UpdateInfoMessage(string source, string message);

        /// <summary>
        /// Check response cache for a service.
        /// </summary>
        /// <param name="service">Service whose cache to check.</param>
        /// <param name="key">Cache key.</param>
        void CheckResponseCache(IDreamService service, object key);

        /// <summary>
        /// Remove an item from a service's cache.
        /// </summary>
        /// <param name="service">Service whose cache to check.</param>
        /// <param name="key">Cache key.</param>
        void RemoveResponseCache(IDreamService service, object key);

        /// <summary>
        /// Empty entire cache for a service.
        /// </summary>
        /// <param name="service">Service to clear the cache for.</param>
        void EmptyResponseCache(IDreamService service);

        /// <summary>
        /// Called by <see cref="IDreamService"/> on startup to have the environment create and initialize a service level container.
        /// </summary>
        /// <remarks>
        /// Returned instance should only be used to configure the container. For any type resolution, <see cref="DreamContext.Container"/> should be used
        /// instead.
        /// </remarks>
        /// <param name="service"></param>
        /// <returns></returns>
        IContainer CreateServiceContainer(IDreamService service);

        /// <summary>
        /// Must be called at <see cref="IDreamService"/> shutdown to dispose of the service level container.
        /// </summary>
        /// <param name="service"></param>
        void DisposeServiceContainer(IDreamService service);
    }
}
