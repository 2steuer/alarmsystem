using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmSystem.Common.Material;

namespace AlarmSystem.Common.Plugins.Interfaces
{
    /// <summary>
    /// Plugins implementing this interface can be used as a TriggerMessage Sink.
    /// </summary>
    public interface ITriggerMessageSink
    {
        /// <summary>
        /// Handles s trigger message.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="message">The TriggerMessage to be handled..</param>
        void HandleTriggerMessage(object sender, TriggerMessage message);
    }
}
