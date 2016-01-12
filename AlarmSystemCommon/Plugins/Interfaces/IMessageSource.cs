using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmSystem.Common.Material;

namespace AlarmSystem.Common.Plugins.Interfaces
{
    /// <summary>
    /// Handles a message.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="message">The message to be handled..</param>
    public delegate void ReceivedMessageDelegate(object sender, Message message);

    /// <summary>
    /// Plugins implenting this interface can be used as message source.
    /// </summary>
    public interface IMessageSource
    {
        /// <summary>
        /// Thrown when a message is received by the plugin and shall be handled.
        /// </summary>
        event ReceivedMessageDelegate OnMessageReceived;
    }
}
