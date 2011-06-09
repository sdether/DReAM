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
using System.Threading.Tasks;
using MindTouch.Dream;

namespace MindTouch.Traum {

    /// <summary>
    /// Provides a handler contract for registering a <see cref="Plug2"/> request invocation endpoint.
    /// </summary>
    public interface IPlugEndpoint2 {

        //--- Methods ---

        /// <summary>
        /// Called by <see cref="Plug2"/> to let the endpoint determine whether it wants to handle the endpoint and to what level of priority.
        /// </summary>
        /// <remarks>
        /// Multiple endpoints can be candidates for handling an invocation. <see cref="Plug2"/> uses the returned score to determine which
        /// endpoint to dispatch to.
        /// </remarks>
        /// <param name="uri">Uri to match.</param>
        /// <param name="normalized">Output of the uri normalized into the form local to the endpoint.</param>
        /// <returns>
        /// Returning 0 or less indicates that the endpoint does not handle the uri, while <see cref="int.MaxValue"/>
        /// is usually used to force interception. Generally, a handler should use <see cref="XUri.Similarity(XUri)"/> to match
        /// an incoming uri to it's handled uri.
        /// </returns>
        int GetScoreWithNormalizedUri(XUri uri, out XUri normalized);

        /// <summary>
        /// Handle the invocation of a plug.
        /// </summary>
        /// <remarks>This method is a coroutine and should never be invoked directly.</remarks>
        /// <param name="plug">Plug to handle.</param>
        /// <param name="verb">Invocation verb.</param>
        /// <param name="uri">Invocation uri.</param>
        /// <param name="request">Request message.</param>
        /// <param name="timeout">timeout</param>
        /// <returns></returns>
        Task<DreamMessage2> Invoke(Plug2 plug, string verb, XUri uri, DreamMessage2 request, TimeSpan timeout);
    }
}
