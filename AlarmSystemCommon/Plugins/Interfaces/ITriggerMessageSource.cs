using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmSystem.Common.Material;

namespace AlarmSystem.Common.Plugins.Interfaces
{
    /// <summary>
    /// Handles s trigger message.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="message">The TriggerMessage to be handled..</param>
    public delegate void TriggerMessageDelegate(object sender, TriggerMessage message);

    public interface ITriggerMessageSource
    {
        /// <summary>
        /// Thrown when a new trigger message to be handled is raised.
        /// </summary>
        event TriggerMessageDelegate OnTriggerMessage;
    }
}
