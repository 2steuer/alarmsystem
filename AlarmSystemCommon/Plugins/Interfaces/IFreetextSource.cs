using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmSystem.Common.Plugins.Interfaces
{
    /// <summary>
    /// The Delegate of Freetext event handlers
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="caption">The caption of the freetext.</param>
    /// <param name="message">The message of the freetext.</param>
    public delegate void OnFreetextMessageDelegate(object sender, string caption, string message);

    /// <summary>
    /// Plugins implementing this interface can be used as Freetext sources
    /// </summary>
    public interface IFreetextSource
    {
        /// <summary>
        /// Thrown when a new Freetext to be handled is created by the plugin.
        /// </summary>
        event OnFreetextMessageDelegate OnFreetextMessage;
    }
}
