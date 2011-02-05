using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MindTouch.Dream;

namespace MindTouch.dream {
    /// <summary>
    /// Interface for <see cref="IDreamService"/> implementations that require a service license.
    /// </summary>
    public interface IDreamServiceLicense {

        //--- Properties ---

        /// <summary>
        /// License for service.
        /// </summary>
        string ServiceLicense { get; }
    }
}
