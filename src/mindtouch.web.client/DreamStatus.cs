/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2009 MindTouch, Inc.
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
namespace MindTouch.Dream {
    /// <summary>
    /// Enumerates the possible Http response codes.
    /// </summary>
    /// <remarks>
    /// Codes below 100 are specific to <see cref="Plug"/> and not part of Http.
    /// </remarks>
    public enum DreamStatus {

        /// <summary>
        /// Unable to connect (0).
        /// </summary>
        UnableToConnect = 0,

        /// <summary>
        /// No <see cref="IPlugEndpoint"/> could be found for Uri. (1).
        /// </summary>
        NoEndpointFound = 1,

        /// <summary>
        /// Request is null (10).
        /// </summary>
        RequestIsNull = 10,

        /// <summary>
        /// Request Failed (11).
        /// </summary>
        RequestFailed = 11,

        /// <summary>
        /// Request failed because the connection timed out (12).
        /// </summary>
        RequestConnectionTimeout = 12,

        /// <summary>
        /// Response is null (20).
        /// </summary>
        ResponseIsNull = 20,

        /// <summary>
        /// Response Failed (21).
        /// </summary>
        ResponseFailed = 21,

        /// <summary>
        /// Response failed because the data transfer timed out (22).
        /// </summary>
        ResponseDataTransferTimeout = 22,

        /// <summary>
        /// Ok (200).
        /// </summary>
        Ok = 200,

        /// <summary>
        /// Created (201).
        /// </summary>
        Created = 201,

        /// <summary>
        /// Accepted (202).
        /// </summary>
        Accepted = 202,

        /// <summary>
        /// Non-authoritative Information (203).
        /// </summary>
        NonAuthoritativeInformation = 203,

        /// <summary>
        /// No content (204).
        /// </summary>
        NoContent = 204,

        /// <summary>
        /// Reset content (205).
        /// </summary>
        ResetContent = 205,

        /// <summary>
        /// Partial content (206).
        /// </summary>
        PartialContent = 206,

        /// <summary>
        /// Multi status (207).
        /// </summary>
        MultiStatus = 207,

        /// <summary>
        /// Multiple choices (300).
        /// </summary>
        MultipleChoices = 300,

        /// <summary>
        /// Moved permanently (301).
        /// </summary>
        MovedPermanently = 301,

        /// <summary>
        /// Found (302).
        /// </summary>
        Found = 302,

        /// <summary>
        /// See other (303).
        /// </summary>
        SeeOther = 303,

        /// <summary>
        /// Not modified (304).
        /// </summary>
        NotModified = 304,

        /// <summary>
        /// Use proxy (305).
        /// </summary>
        UseProxy = 305,

        /// <summary>
        /// Temporary redirct (307).
        /// </summary>
        TemporaryRedirect = 307,

        /// <summary>
        /// Bad Request (400).
        /// </summary>
        BadRequest = 400,

        /// <summary>
        /// Unauthorized (401).
        /// </summary>
        Unauthorized = 401,

        /// <summary>
        /// License required (402).
        /// </summary>
        LicenseRequired = 402,

        /// <summary>
        /// Forbidden (403).
        /// </summary>
        Forbidden = 403,

        /// <summary>
        /// Not found (404).
        /// </summary>
        NotFound = 404,

        /// <summary>
        /// Method is not allowed (405).
        /// </summary>
        MethodNotAllowed = 405,

        /// <summary>
        /// Not acceptable (406).
        /// </summary>
        NotAcceptable = 406,

        /// <summary>
        /// Proxy authentication required (407).
        /// </summary>
        ProxyAuthenticationRequired = 407,

        /// <summary>
        /// Request timeout (408).
        /// </summary>
        RequestTimeout = 408,

        /// <summary>
        /// Conflict (409).
        /// </summary>
        Conflict = 409,

        /// <summary>
        /// Gone (410).
        /// </summary>
        Gone = 410,

        /// <summary>
        /// Length required (411).
        /// </summary>
        LengthRequired = 411,

        /// <summary>
        /// Precondition Failed (412).
        /// </summary>
        PreconditionFailed = 412,

        /// <summary>
        /// Request entity too large (413).
        /// </summary>
        RequestEntityTooLarge = 413,

        /// <summary>
        /// Request Uri is too long (414).
        /// </summary>
        RequestURIToLong = 414,

        /// <summary>
        /// Unsupported media type (415).
        /// </summary>
        UnsupportedMediaType = 415,

        /// <summary>
        /// Request range not satisfiable (416).
        /// </summary>
        RequestedRangeNotSatisfiable = 416,

        /// <summary>
        /// Expecation failed (417).
        /// </summary>
        ExpectationFailed = 417,

        /// <summary>
        /// Unprocessable entity (422).
        /// </summary>
        UnprocessableEntity = 422,

        /// <summary>
        /// Locked (423).
        /// </summary>
        Locked = 423,

        /// <summary>
        /// Failed dependency (424).
        /// </summary>
        FailedDependency = 424,

        /// <summary>
        /// Internal error (500).
        /// </summary>
        InternalError = 500,

        /// <summary>
        /// Not implemented (501).
        /// </summary>
        NotImplemented = 501,

        /// <summary>
        /// Bad Gateway (502).
        /// </summary>
        BadGateway = 502,

        /// <summary>
        /// Service unavailable (503).
        /// </summary>
        ServiceUnavailable = 503,

        /// <summary>
        /// Gateway timeout (504).
        /// </summary>
        GatewayTimeout = 504,

        /// <summary>
        /// Http version not supported (505).
        /// </summary>
        HTTPVersionNotSupported = 505,

        /// <summary>
        /// Insuffient storage (507).
        /// </summary>
        InsufficientStorage = 507
    }
}
