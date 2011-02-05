using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MindTouch.Dream;
using MindTouch.Dream.Web.Client;
using MindTouch.Xml;

namespace MindTouch.Dream {
    public static class XDocEx {
        /// <summary>
        /// Returns the contents as uri or null if contents could not be converted.
        /// </summary>
        public static XUri AsUri(this XDoc doc) {
            return doc.IsEmpty ? null : new XUri(doc.AsText).AsLocalUri();
        }

        /// <summary>
        /// Add an attribute to the XDoc instance.
        /// </summary>
        /// <param name="doc">The document to operate on</param>
        /// <param name="tag">Attribute name</param>
        /// <param name="value">Value of the attribute</param>
        /// <returns>Current XDoc instance</returns>
        public static XDoc Attr(this XDoc doc, string tag, XUri value) {
            if(tag == null) {
                throw new ArgumentNullException("tag");
            }
            if(value == null) {
                return doc;
            }
            value = value.AsPublicUri();
            return doc.Attr(tag, value.ToString());
        }


        /// <summary>
        /// Adds a complete child element.
        /// </summary>
        /// <param name="doc">The document to operate on</param>
        /// <param name="tag">Element name</param>
        /// <param name="value">Value to add</param>
        /// <returns>Current XDoc instance</returns>
        public static XDoc Elem(this XDoc doc, string tag, XUri value) {
            if(value == null) {
                return doc;
            }
            return doc.Start(tag).Value(value).End();
        }


        /// <summary>
        /// Adds a text node.
        /// </summary>
        /// <param name="doc">The document to operate on</param>
        /// <param name="value">Value to add</param>
        /// <returns>Current XDoc instance</returns>
        public static XDoc Value(this XDoc doc, XUri value) {
            if(value == null) {
                return doc;
            }
            value = value.AsPublicUri();
            return doc.Value(value.ToString());
        }


        /// <summary>
        /// Replaces the text node with a new text node.
        /// </summary>
        /// <param name="doc">The document to operate on</param>
        /// <param name="value">Replacement value</param>
        /// <returns></returns>
        public static XDoc ReplaceValue(this XDoc doc, XUri value) {
            if(value == null) {

                // TODO (steveb): how should we handle the null case?

                throw new ArgumentNullException("value");
            }
            value = value.AsPublicUri();
            return doc.ReplaceValue(value.ToString());
        }


        /// <summary>
        /// Adds a value after this XDoc instance.
        /// </summary>
        /// <param name="doc">The document to operate on</param>
        /// <param name="value">Value to add.</param>
        /// <returns></returns>
        public static XDoc AddAfter(this XDoc doc, XUri value) {
            if(value == null) {
                return doc;
            }
            value = value.AsPublicUri();
            return doc.AddAfter(value.ToString());
        }


        /// <summary>
        /// Adds a value before this XDoc instance.
        /// </summary>
        /// <param name="doc">The document to operate on</param>
        /// <param name="value">Value to add.</param>
        /// <returns></returns>
        public static XDoc AddBefore(this XDoc doc, XUri value) {
            if(value == null) {
                return doc;
            }
            value = value.AsPublicUri();
            return doc.AddBefore(value.ToString());
        }
    }
}

