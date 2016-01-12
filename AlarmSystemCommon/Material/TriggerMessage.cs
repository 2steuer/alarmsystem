using System;

namespace AlarmSystem.Common.Material
{
    /// <summary>
    /// Messages to persons, usually created as a result of a trigger request.
    /// Destination is usually a phone number, but according to use-case can be anything you want, as long as you handle ist.
    /// </summary>
    public class TriggerMessage
    {
        /// <summary>
        /// The destination of the message.
        /// </summary>
        /// <value>
        /// The destination.
        /// </value>
        public string Destination { get; set; }

        /// <summary>
        /// Gets or sets the text of the trigger message to be sent.
        /// </summary>
        /// <value>
        /// The text.
        /// </value>
        public string Text { get; set; }

        /// <summary>
        /// This determines wether it is a high-priority message. This is taken from SMS messages,
        /// where a flash message is immediately delivered to the display of a mobile device. Can be used
        /// for other purpouses, of course.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [flash message]; otherwise, <c>false</c>.
        /// </value>
        public bool FlashMessage { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TriggerMessage"/> class.
        /// </summary>
        /// <param name="destination">The destination.</param>
        /// <param name="text">The text.</param>
        public TriggerMessage(string destination, string text)
        {
            Destination = destination;
            Text = text;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TriggerMessage"/> class.
        /// </summary>
        public TriggerMessage()
            : this(String.Empty, String.Empty)
        {
            
        }
    }
}
