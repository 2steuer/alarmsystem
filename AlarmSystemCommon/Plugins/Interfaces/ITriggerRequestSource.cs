using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmSystem.Common.Material;

namespace AlarmSystem.Common.Plugins.Interfaces
{
    /// <summary>
    /// Handles the trigger request.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="request">The TriggerRequest.</param>
    public delegate void TriggerRequestDelegate(object sender, TriggerRequest request);

    /// <summary>
    /// Plugins implementing this interface can be used as Trigger Request source
    /// </summary>
    public interface ITriggerRequestSource
    {
        /// <summary>
        /// Thrown on new trigger requests by this plugin.
        /// </summary>
        event TriggerRequestDelegate OnTriggerRequest;
    }
}
