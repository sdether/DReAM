﻿/*
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
using log4net;
using MindTouch.Extensions.Time;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Dream.Services.PubSub {

    /// <summary>
    /// Provides a <see cref="PubSubSubscription"/> collection.
    /// </summary>
    public class PubSubSubscriptionSet {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        // --- Constants ---
        private const int MAX_FAILURES = 5;

        //--- Fields ---

        /// <summary>
        /// Set owner uri.
        /// </summary>
        public readonly XUri Owner;

        /// <summary>
        /// Pub sub service resource location uri postfix.
        /// </summary>
        public readonly string Location;

        /// <summary>
        /// Subscriptions in set.
        /// </summary>
        public readonly PubSubSubscription[] Subscriptions;

        /// <summary>
        /// Maximum allowed dispatch failures before the set is dropped. Only used if <see cref="UsesFailureDuration"/> is false.
        /// </summary>
        public readonly int MaxFailures;

        /// <summary>
        /// Access key for reading/modifying the set on the pub sub service.
        /// </summary>
        public readonly string AccessKey;

        /// <summary>
        /// Set version serial.
        /// </summary>
        public readonly long? Version;

        /// <summary>
        /// Maximum time period repeated failures are accepted before the set is dropped. Only used if <see cref="UsesFailureDuration"/> is true.
        /// </summary>
        public readonly TimeSpan MaxFailureDuration;

        //--- Constructors ---

        /// <summary>
        /// Create a new subscription set.
        /// </summary>
        /// <param name="owner">Owner uri.</param>
        /// <param name="version">Version serial number.</param>
        /// <param name="cookie">Pub sub location access cookie.</param>
        /// <param name="childSubscriptions">Subscriptions.</param>
        public PubSubSubscriptionSet(XUri owner, long version, DreamCookie cookie, params PubSubSubscription[] childSubscriptions) {
            Owner = owner;
            Version = version;
            Dictionary<string, PubSubSubscription> subs = new Dictionary<string, PubSubSubscription>();
            foreach(var sub in childSubscriptions) {
                foreach(var channel in sub.Channels) {
                    if(channel.Scheme == "pubsub") {

                        // pubsub scheme is for PubSubService internal use only, so it should never be aggregated
                        continue;
                    }
                    XUri[] resources = (sub.Resources == null || sub.Resources.Length == 0) ? new XUri[] { null } : sub.Resources;
                    foreach(XUri resource in resources) {
                        PubSubSubscription combo;
                        string key = channel + ":" + resource;
                        subs.TryGetValue(key, out combo);
                        subs[key] = PubSubSubscription.MergeForChannelAndResource(channel, resource, this, cookie, sub, combo);
                    }
                }
            }
            Subscriptions = new PubSubSubscription[subs.Count];
            subs.Values.CopyTo(Subscriptions, 0);
            MaxFailures = MAX_FAILURES;
        }

        /// <summary>
        /// Create a new subscription set from a subscription set document.
        /// </summary>
        /// <param name="setDoc">Set Xml document.</param>
        /// <param name="location">location id.</param>
        /// <param name="accessKey">secret key for accessing the set.</param>
        public PubSubSubscriptionSet(XDoc setDoc, string location, string accessKey) {
            try {
                // Note: not using AsUri to avoid automatic local:// translation
                Owner = new XUri(setDoc["uri.owner"].AsText);
                MaxFailureDuration = (setDoc["@max-failure-duration"].AsDouble ?? 0).Seconds();
                var subscriptions = new List<PubSubSubscription>();
                foreach(XDoc sub in setDoc["subscription"]) {
                    subscriptions.Add(new PubSubSubscription(sub, this));
                }
                Version = setDoc["@version"].AsLong;
                Subscriptions = subscriptions.ToArray();
                Location = location;
                AccessKey = accessKey;
                MaxFailures = UsesFailureDuration ? int.MaxValue : setDoc["@max-failures"].AsInt ?? MAX_FAILURES;
            } catch(Exception e) {
                throw new ArgumentException("Unable to parse subscription set: " + e.Message, e);
            }
        }

        //--- Properties ---

        /// <summary>
        /// Dispatch cookies.
        /// </summary>
        public List<DreamCookie> Cookies {
            get {
                var lookup = new HashSet<string>();
                var cookies = new List<DreamCookie>();
                foreach(var sub in Subscriptions) {
                    if(sub.Cookie != null) {
                        string h = sub.Cookie.ToString();
                        if(!lookup.Contains(h)) {
                            lookup.Add(h);
                            cookies.Add(sub.Cookie);
                        }
                    }
                }
                return cookies;
            }
        }

        /// <summary>
        /// True if this set upes <see cref="MaxFailureDuration"/> instead of <see cref="MaxFailures"/> to determine whether the set has exceeded
        /// its failure threshold and should be dropped.
        /// </summary>
        public bool UsesFailureDuration {
            get { return MaxFailureDuration != TimeSpan.Zero; }
        }

        //--- Methods ---

        /// <summary>
        /// Create a new subscription set document.
        /// </summary>
        /// <returns>Set Xml document.</returns>
        public XDoc AsDocument() {
            XDoc doc = new XDoc("subscription-set")
                .Attr("max-failures", MaxFailures)
                .Elem("uri.owner", Owner);
            if(Version.HasValue) {
                doc.Attr("version", Version.Value);
            }
            foreach(PubSubSubscription subscription in Subscriptions) {
                doc.Add(subscription.AsDocument());
            }
            return doc;
        }

        /// <summary>
        /// Derive a new set using a newer subscription set document.
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="accessKey">Optional new access key</param>
        /// <returns></returns>
        public PubSubSubscriptionSet Derive(XDoc doc, string accessKey) {
            accessKey = accessKey ?? AccessKey;
            var version = doc["@version"].AsLong;
            if(version.HasValue && Version.HasValue && version.Value <= Version.Value) {
                _log.DebugFormat("Supplied version is not newer than current: {0} <= {1}", version.Value, Version);
                return this;
            }
            return new PubSubSubscriptionSet(doc, Location, accessKey);
        }
    }
}
