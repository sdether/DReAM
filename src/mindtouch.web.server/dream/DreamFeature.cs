using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MindTouch.Dream;

namespace MindTouch.Dream {
    /// <summary>
    /// Encapsulation of processing chain for a feature in an <see cref="IDreamService"/>
    /// </summary>
    public class DreamFeature {

        //--- Class Methods ---
        private static void ParseFeatureSignature(XUri baseUri, string signature, out string[] pathSegments, out KeyValuePair<int, string>[] paramNames, out int optional) {
            List<string> segments = new List<string>(baseUri.GetSegments(UriPathFormat.Normalized));
            List<KeyValuePair<int, string>> names = new List<KeyValuePair<int, string>>();
            optional = 0;

            // normalize and remove any leading and trailing '/'
            signature = signature.ToLowerInvariant().Trim();
            if((signature.Length > 0) && (signature[0] == '/')) {
                signature = signature.Substring(1);
            }
            if((signature.Length > 0) && (signature[signature.Length - 1] == '/')) {
                signature = signature.Substring(0, signature.Length - 1);
            }
            if(signature.Length > 0) {
                string[] parts = signature.Split('/');

                // loop over all parts
                for(int i = 0; i < parts.Length; ++i) {

                    // check if part is empty (this only happens for "//")
                    if(parts[i].Length == 0) {

                        // we found two slashes in a row; the next token MUST be the final token
                        if((i != (parts.Length - 2)) || (parts[i + 1] != "*")) {
                            throw new ArgumentException("invalid feature signature", signature);
                        }
                        optional = int.MaxValue;
                        break;
                    } else {
                        string part = parts[i].Trim();
                        if((part.Length >= 2) && (part[0] == '{') && (part[part.Length - 1] == '}')) {

                            // we have a path variable (e.g. /{foo}/)
                            if(optional != 0) {
                                throw new ArgumentException("invalid feature signature", signature);
                            }
                            segments.Add(SysUtil.NameTable.Add("*"));
                            names.Add(new KeyValuePair<int, string>(baseUri.Segments.Length + i, SysUtil.NameTable.Add(part.Substring(1, part.Length - 2))));
                        } else if(part == "*") {

                            // we have a path wildcard (e.g. /*/)
                            if(optional != 0) {
                                throw new ArgumentException("invalid feature signature", signature);
                            }
                            segments.Add(SysUtil.NameTable.Add(part));
                            names.Add(new KeyValuePair<int, string>(baseUri.Segments.Length + i, SysUtil.NameTable.Add(i.ToString())));
                        } else if(part == "?") {

                            // we have an optional path (e.g. /?/)
                            ++optional;
                            segments.Add(SysUtil.NameTable.Add(part));
                        } else {

                            // we have a path constant (e.g. /foo/)
                            if(optional != 0) {
                                throw new ArgumentException("invalid feature signature", signature);
                            }
                            segments.Add(SysUtil.NameTable.Add(part));
                        }
                    }
                }
            }
            pathSegments = segments.ToArray();
            paramNames = names.ToArray();
        }

        //--- Fields ---

        /// <summary>
        /// Owning Service.
        /// </summary>
        public readonly IDreamService Service;

        /// <summary>
        /// Uri for Service.
        /// </summary>
        public readonly XUri ServiceUri;

        /// <summary>
        /// Request Verb.
        /// </summary>
        public readonly string Verb;

        /// <summary>
        /// Request stages.
        /// </summary>
        public readonly DreamFeatureStage[] Stages;

        /// <summary>
        /// Request path segments.
        /// </summary>
        public readonly string[] PathSegments;

        /// <summary>
        /// Number of optional segments
        /// </summary>
        public readonly int OptionalSegments;

        /// <summary>
        /// Index into <see cref="Stages"/> for the <see cref="DreamFeatureAttribute"/> marked stage for this request.
        /// </summary>
        public readonly int MainStageIndex;

        /// <summary>
        /// Exception translators for this request.
        /// </summary>
        public readonly ExceptionTranslator[] ExceptionTranslators;

        private KeyValuePair<int, string>[] _paramNames;
        private int _counter;

        //--- Constructors ---

        /// <summary>
        /// Create a new feature instance.
        /// </summary>
        /// <param name="service">Owning Service.</param>
        /// <param name="serviceUri">Service Uri.</param>
        /// <param name="mainStageIndex">Main stage index.</param>
        /// <param name="stages">Feature stages.</param>
        /// <param name="verb">Request verb.</param>
        /// <param name="signature">Feature signature.</param>
        public DreamFeature(IDreamService service, XUri serviceUri, int mainStageIndex, DreamFeatureStage[] stages, string verb, string signature) {
            this.Service = service;
            this.ServiceUri = serviceUri;
            this.Stages = stages;
            this.MainStageIndex = mainStageIndex;
            this.Verb = verb;
            this.ExceptionTranslators = service.ExceptionTranslators;
            ParseFeatureSignature(serviceUri, signature, out this.PathSegments, out _paramNames, out this.OptionalSegments);
        }

        //--- Properties ---

        /// <summary>
        /// Feature signature.
        /// </summary>
        public string Signature { get { return string.Join("/", PathSegments, ServiceUri.Segments.Length, PathSegments.Length - ServiceUri.Segments.Length); } }

        /// <summary>
        /// Feature path.
        /// </summary>
        public string Path { get { return string.Join("/", PathSegments); } }

        /// <summary>
        /// <see cref="Verb"/> + ":" + <see cref="Signature"/>.
        /// </summary>
        public string VerbSignature { get { return Verb + ":" + Signature; } }

        /// <summary>
        /// <see cref="Verb"/> + ":" + <see cref="Path"/>. 
        /// </summary>
        public string VerbPath { get { return Verb + ":" + Path; } }

        /// <summary>
        /// Number of times this Feature has been called in current instance.
        /// </summary>
        public int HitCounter { get { return _counter; } }

        /// <summary>
        /// Main feature Stage.
        /// </summary>
        public DreamFeatureStage MainStage { get { return Stages[MainStageIndex]; } }

        //--- Methods ---

        /// <summary>
        /// Extract a list of suffixes and a dictionary of arguments from the request.
        /// </summary>
        /// <param name="uri">Request Uri.</param>
        /// <param name="suffixes">Extracted suffixes.</param>
        /// <param name="pathParams">Extracted path parameters.</param>
        public void ExtractArguments(XUri uri, out string[] suffixes, out Dictionary<string, string[]> pathParams) {
            Dictionary<string, List<string>> tmpPathParams = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            List<string> suffixesList = new List<string>(_paramNames.Length + (uri.Segments.Length - PathSegments.Length));
            for(int i = 0; i < _paramNames.Length; ++i) {
                string value = uri.Segments[_paramNames[i].Key];
                suffixesList.Add(value);
                List<string> values;
                if(!tmpPathParams.TryGetValue(_paramNames[i].Value, out values)) {
                    values = new List<string>(1);
                    tmpPathParams.Add(_paramNames[i].Value, values);
                }
                values.Add(XUri.Decode(value));
            }
            pathParams = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            foreach(KeyValuePair<string, List<string>> tmpPathParam in tmpPathParams) {
                pathParams.Add(tmpPathParam.Key, tmpPathParam.Value.ToArray());
            }
            for(int i = PathSegments.Length; i < uri.Segments.Length; ++i) {
                suffixesList.Add(uri.Segments[i]);
            }
            suffixes = suffixesList.ToArray();
        }

        /// <summary>
        /// Increment the feature hit counter.
        /// </summary>
        public void IncreaseHitCounter() {
            System.Threading.Interlocked.Increment(ref _counter);
        }
    }
}
