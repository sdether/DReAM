using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MindTouch.dream {
    /// <summary>
    /// Dream Feature Access level.
    /// </summary>
    public enum DreamAccess {

        /// <summary>
        /// Feature can be called by anyone
        /// </summary>
        Public,

        /// <summary>
        /// Feature access requries the internal or private service key.
        /// </summary>
        Internal,

        /// <summary>
        /// Feature access requires the private service key.
        /// </summary>
        Private
    }
}
