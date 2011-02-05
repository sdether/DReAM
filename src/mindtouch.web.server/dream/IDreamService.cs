using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MindTouch.dream;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Dream {
    /// <summary>
    /// Provides interface that all services hosted in Dream must implement.
    /// </summary>
    public interface IDreamService {

        //--- Properties ---

        /// <summary>
        /// <see cref="Plug"/> for Service's Host environment.
        /// </summary>
        Plug Env { get; }

        /// <summary>
        /// Service <see cref="Plug"/>
        /// </summary>
        Plug Self { get; }

        /// <summary>
        /// Service cookie jar.
        /// </summary>
        DreamCookieJar Cookies { get; }

        /// <summary>
        /// Prologue request stages to be executed before a Feature is executed.
        /// </summary>
        DreamFeatureStage[] Prologues { get; }

        /// <summary>
        /// Epilogue request stages to be executed after a Feature has completed.
        /// </summary>
        DreamFeatureStage[] Epilogues { get; }

        /// <summary>
        /// Exception translators given an opportunity to rewrite an exception before it is returned to the initiator of a request.
        /// </summary>
        ExceptionTranslator[] ExceptionTranslators { get; }

        //--- Methods ---

        /// <summary>
        /// Initialize a service instance.
        /// </summary>
        /// <param name="environment">Host environment.</param>
        /// <param name="blueprint">Service blueprint.</param>
        void Initialize(IDreamEnvironment environment, XDoc blueprint);

        /// <summary>
        /// Determine the access appropriate for an incoming request.
        /// </summary>
        /// <param name="context">Request context.</param>
        /// <param name="request">Request message.</param>
        /// <returns>Access level for request.</returns>
        DreamAccess DetermineAccess(DreamContext context, DreamMessage request);
    }
}
