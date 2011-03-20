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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;
using MindTouch.Collections;
using MindTouch.dream;
using MindTouch.IO;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Dream.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Dream Storage", "Copyright (c) 2006-2011 MindTouch, Inc.",
        Info = "http://developer.mindtouch.com/Dream/Reference/Services/Storage",
        SID = new string[] { 
            "sid://mindtouch.com/2007/03/dream/storage",
            "sid://mindtouch.com/2007/07/dream/storage.private",
            "http://services.mindtouch.com/dream/stable/2007/03/storage",
            "http://services.mindtouch.com/dream/draft/2007/07/storage.private" 
        }
    )]
    [DreamServiceConfig("folder", "path", "Rooted path to the folder managed by the storeage service.")]
    internal class StorageService : DreamService {

        //--- Constants ---
        private const string STATE_FILENAME = "storage.state.xml";

        // --- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private string _path;
        private bool _private;
        private bool _dirty;
        private bool _privateRoot;
        private ExpiringHashSet<string> _expirationEntries;

        //--- Features ---
        [DreamFeature("GET://*", "Retrieve a file or a list of all files and folders at the specified path")]
        [DreamFeature("HEAD://*", "Retrieve information about a file or folder from the storage folder")]
        public Yield GetFileOrFolderListing(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            bool head = StringUtil.EqualsInvariant(context.Verb, "HEAD");
            string path = GetPath(context);

            DreamMessage result;
            if(File.Exists(path)) {

                // dealing with a file request 
                // update expiration time
                _expirationEntries.RefreshExpiration(path);

                // check if request contains a 'if-modified-since' header
                DateTime lastmodified = File.GetLastWriteTime(path);
                if(request.CheckCacheRevalidation(lastmodified) && (lastmodified.Year >= 1900)) {
                    response.Return(DreamMessage.NotModified());
                    yield break;
                }

                // retrieve file
                try {
                    result = DreamMessage.FromFile(path, head);
                } catch(FileNotFoundException) {
                    result = DreamMessage.NotFound("file not found");
                } catch(Exception) {
                    result = DreamMessage.BadRequest("invalid path");
                }

                // add caching headers if file was found
                if(!head && result.IsSuccessful) {

                    // add caching information; this will avoid unnecessary data transfers by user-agents with caches
                    result.SetCacheMustRevalidate(File.GetLastWriteTime(path));
                }
            } else if(Directory.Exists(path)) {

                // dealing with a directory request
                if(head) {

                    // HEAD for a directory doesn't really mean anything, so we just return ok, to indicate that it exists
                    result = DreamMessage.Ok();
                } else {
                    XDoc doc = new XDoc("files");

                    // list directory contents
                    string[] directories = Directory.GetDirectories(path);
                    foreach(string s in directories) {
                        doc.Start("folder")
                            .Elem("name", Path.GetFileName(s))
                            .End();
                    }
                    foreach(string filepath in Directory.GetFiles(path)) {
                        FileInfo file = new FileInfo(filepath);
                        doc.Start("file")
                            .Elem("name", file.Name)
                            .Elem("size", file.Length)
                            .Elem("date.created", file.CreationTimeUtc)
                            .Elem("date.modified", file.LastWriteTimeUtc);
                        var entry = _expirationEntries[path];
                        if(entry != null) {
                            doc.Elem("date.expire", entry.When);
                            doc.Elem("date.ttl", entry.TTL);
                        }
                        doc.End();
                    }
                    result = DreamMessage.Ok(doc);
                }
            } else {

                // nothin here
                result = DreamMessage.NotFound("no such file or folder");
            }

            response.Return(result);
            yield break;

        }

        [DreamFeature("PUT://*", "Add a file at a specified path")]
        [DreamFeatureParam("ttl", "int", "time-to-live in seconds for the posted event")]
        public Yield PutFile(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string filepath = GetPath(context);
            string folderpath = Path.GetDirectoryName(filepath);
            double ttl = context.GetParam<double>("ttl", 0.0);
            TimeSpan timeToLive = (ttl > 0.0) ? TimeSpan.FromSeconds(ttl) : TimeSpan.MaxValue;
            if(Directory.Exists(filepath)) {

                // filepath is actually an existing directory
                response.Return(DreamMessage.Conflict("there exists a directory at the specified file path"));
                yield break;
            }

            // create folder if need be
            if(!Directory.Exists(folderpath)) {
                Directory.CreateDirectory(folderpath);
            }

            // save request stream in target file
            DreamMessage result;
            try {
                request.ToStream().CopyToFile(filepath, request.ContentLength);

                // schedule event deletion
                if(timeToLive == TimeSpan.MaxValue) {
                    _expirationEntries.Delete(filepath);
                } else {
                    _expirationEntries.SetExpiration(filepath, timeToLive);

                }
                result = DreamMessage.Ok();
            } catch(DirectoryNotFoundException) {
                result = DreamMessage.NotFound("directory not found");
            } catch(PathTooLongException) {
                result = DreamMessage.BadRequest("path too long");
            } catch(NotSupportedException) {
                result = DreamMessage.BadRequest("not supported");
            }
            response.Return(result);
            yield break;
        }

        [DreamFeature("DELETE://*", "Delete file from the storage folder")]
        public Yield DeleteFile(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string path = GetPath(context);
            DreamMessage result = DreamMessage.Ok();
            if(Directory.Exists(path)) {

                // folder delete
                try {
                    Directory.Delete(path, true);
                } catch { }
            } else if(File.Exists(path)) {

                // delete target file
                try {
                    _expirationEntries.Delete(path);
                    try {
                        File.Delete(path);
                    } catch {
                    }
                } catch(FileNotFoundException) {
                } catch(DirectoryNotFoundException) {
                } catch(PathTooLongException) {
                    result = DreamMessage.BadRequest("path too long");
                } catch(NotSupportedException) {
                    result = DreamMessage.BadRequest("not supported");
                }

                // try to clean up empty directory
                string folderpath = Path.GetDirectoryName(path);
                if(Directory.Exists(folderpath) && (Directory.GetFileSystemEntries(folderpath).Length == 0)) {
                    try {
                        Directory.Delete(folderpath);
                    } catch {
                    }
                }
            }
            response.Return(result);
            yield break;
        }

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            // are we a private storage service?
            _private = config["sid"].Contents == "sid://mindtouch.com/2007/07/dream/storage.private";
            _log.DebugFormat("storage is {0}", _private ? "private" : "public");

            // is the root blocked from access?
            _privateRoot = config["private-root"].AsBool.GetValueOrDefault();
            _log.DebugFormat("storage root is {0}accessible", _privateRoot ? "not " : "");
            _expirationEntries = new ExpiringHashSet<string>(TimerFactory);
            _expirationEntries.EntryExpired += OnDelete;
            _expirationEntries.CollectionChanged += OnCollectionChanged;
            _dirty = false;

            // check if folder exists
            _path = Environment.ExpandEnvironmentVariables(config["folder"].Contents);
            _log.DebugFormat("storage path: {0}", _path);
            if(!Path.IsPathRooted(_path)) {
                throw new ArgumentException(string.Format("storage path must be absolute: {0}", _path));
            }

            // make sure path ends with a '\' as it makes processing simpler later on
            if((_path.Length != 0) && ((_path[_path.Length - 1] != '/') || (_path[_path.Length - 1] != '\\'))) {
                _path += Path.DirectorySeparatorChar;
            }

            if(!_private && !Directory.Exists(_path)) {
                throw new ArgumentException(string.Format("storage path does not exist: {0}", _path));
            }

            // check if state file exists
            string statefile = Path.Combine(_path, STATE_FILENAME);
            DateTime now = DateTime.UtcNow;
            if(File.Exists(statefile)) {
                XDoc state = XDocFactory.LoadFrom(statefile, MimeType.XML);

                // restore file expiration list
                foreach(XDoc entry in state["file"]) {
                    var filepath = Path.Combine(_path, entry["path"].Contents);
                    var when = entry["expire"].AsDate ?? DateTime.MaxValue;
                    if(!File.Exists(filepath)) {
                        _dirty = true;
                        continue;
                    }
                    _log.DebugFormat("File initialized for deletion at {0}, now: {1}", when, DateTime.UtcNow);
                    if(when != DateTime.MaxValue) {
                        _expirationEntries.SetExpiration(filepath, when);
                    }
                }
            }

            // schedule expirations
            TimerFactory.New(DateTime.UtcNow.AddSeconds(60), OnDirtyTimer, null, TaskEnv.Clone());
            result.Return();
        }

        protected override Yield Stop(Result result) {
            StoreEntries();
            _expirationEntries.Dispose();
            _expirationEntries.EntryExpired -= OnDelete;
            _expirationEntries.CollectionChanged -= OnCollectionChanged;
            _expirationEntries = null;
            _dirty = false;
            _path = null;
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }

        public override DreamFeatureStage[] Prologues {
            get {
                return new DreamFeatureStage[] { 
                    new DreamFeatureStage("check-private-storage-access", this.ProloguePrivateStorage, DreamAccess.Public), 
                };
            }
        }

        private Yield ProloguePrivateStorage(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            //check if this services is private
            if(_private) {
                DreamCookie cookie = DreamCookie.GetCookie(request.Cookies, "service-key");
                if(cookie == null || cookie.Value != PrivateAccessKey) {
                    throw new DreamForbiddenException("insufficient access privileges");
                }
            }
            response.Return(request);
            yield break;
        }

        private string GetPath(DreamContext context) {
            string[] parts = context.GetSuffixes(UriPathFormat.Decoded);
            string path = _path;
            foreach(string part in parts) {
                if(StringUtil.EqualsInvariant(part, "..")) {
                    throw new DreamBadRequestException("paths cannot contain '..'");
                }
                path = Path.Combine(path, part);
            }
            if(_privateRoot && (parts.Length == 0 || (parts.Length == 1 && !Directory.Exists(path)))) {
                throw new DreamForbiddenException("Root level access is forbidden for this storage service");
            }
            return path;
        }

        private void StoreEntries() {
            XDoc state = new XDoc("storage");
            // collect state information
            foreach(var entry in _expirationEntries) {
                state.Start("file")
                    .Elem("path", Path.GetFileName(entry.Value))
                    .Elem("expire", entry.When)
                    .Elem("ttl", entry.TTL.TotalSeconds)
                .End();
            }

            // attempt to write state, ignore if it fails
            try {
                if(Directory.Exists(_path)) {
                    state.Save(Path.Combine(_path, STATE_FILENAME));
                }
            } catch { }
        }

        private void OnDelete(object sender, ExpirationEventArgs<string> e) {
            var filepathEntry = e.Entry;
            if(!File.Exists(filepathEntry.Value)) {
                return;
            }
            try {
                File.Delete(filepathEntry.Value);
            } catch {
                // ignore file deletion exception

                // BUG #806: we should try again in a few seconds; however, we need to be smart about it and count how often
                //           we tried, otherwise we run the risk of bogging down the system b/c we're attempting to delete undeletable files.
            }
        }

        void OnCollectionChanged(object sender, EventArgs e) {
            _dirty = true;
        }

        private void OnDirtyTimer(TaskTimer timer) {
            if(_dirty) {
                _dirty = false;
                StoreEntries();
            }
            timer.Change(DateTime.UtcNow.AddSeconds(60), TaskEnv.Clone());
        }
    }
}
