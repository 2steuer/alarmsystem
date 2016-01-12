using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmSystem.Common.Material;

namespace AlarmSystem.Common.Plugins.Interfaces
{
    /// <summary>
    /// Plugins implementing this interface can be used as message sinks-
    /// </summary>
    public interface IMessageSink
    {
        /// <summary>
        /// Handles a message.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="message">The message to be handled..</param>
        void HandleMessage(object sender, Message message);
    }
}
