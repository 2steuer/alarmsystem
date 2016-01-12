using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmSystem.Common.Material;

namespace AlarmSystem.Common.Plugins.Interfaces
{
    /// <summary>
    /// Plugins implementing this interface can be used as a trigger request sink.
    /// </summary>
    public interface ITriggerRequestSink
    {
        /// <summary>
        /// Handles the trigger request.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="request">The TriggerRequest.</param>
        void HandleTriggerRequest(object sender, TriggerRequest request);
    }
}
