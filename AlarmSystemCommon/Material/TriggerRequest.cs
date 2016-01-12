using System;
using System.Xml.Linq;

namespace AlarmSystem.Common.Material
{
    /// <summary>
    /// A Request to execute a programmed trigger. Triggers are usually taken from the Database and trigger messages are then created.
    /// </summary>
    public class TriggerRequest
    {
        /// <summary>
        /// Gets or sets the trigger text.
        /// The trigger text indicates what trigger shall be executed. Each programmed trigger has an unique, human-readable descriptor, e.g.
        /// TEST1 or GPIO0. This is programmed in the database and is usually not equal to the trigger description.
        /// </summary>
        /// <value>
        /// The trigger text.
        /// </value>
        public string TriggerText { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to send the default programmed text of the trigger or the one given in Message.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [send default message]; otherwise, <c>false</c>.
        /// </value>
        public bool SendDefaultMessage { get; set; }

        /// <summary>
        /// Gets or sets the message sent with the trigger request. SendDefaultMessage determines wether its used or not.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        public string Message { get; set; }

        /// <summary>
        /// A string literal describing the source of the Trigger Request, e.g. HTTP, Web-Interface, SMS, GPIO etc.
        /// </summary>
        /// <value>
        /// The source.
        /// </value>
        public string Source { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TriggerRequest"/> class.
        /// </summary>
        /// <param name="triggername">The triggername.</param>
        /// <param name="sendDefaultMessage">if set to <c>true</c> [send default message].</param>
        /// <param name="triggermessage">The triggermessage.</param>
        /// <param name="source">The source.</param>
        public TriggerRequest(string triggername, bool sendDefaultMessage, string triggermessage, string source)
        {
            TriggerText = triggername;
            SendDefaultMessage = sendDefaultMessage;
            Message = triggermessage;
            Source = source;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance in form of an XML node.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            XDocument doc = new XDocument();
            XElement trig;
            doc.Add(trig = new XElement("TriggerRequest"));
            trig.Add(new XElement("SendDefaultMessage", SendDefaultMessage.ToString()));
            trig.Add(new XElement("TriggerText", TriggerText));

            if (Message != String.Empty)
            {
                trig.Add(new XElement("Message", Message));
            }

            trig.Add(new XElement("Source", Source));

            return doc.ToString();
        }

        /// <summary>
        /// Parses the specified input XML as a TriggerRequest. See additional documentation for XML format.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        /// <exception cref="System.FormatException">
        /// No Trigger Name Element given in request XML.
        /// or
        /// No correct root Element given in request XML.
        /// </exception>
        public static TriggerRequest Parse(string input)
        {
            XDocument doc = XDocument.Parse(input);
            XElement element = doc.Element("TriggerRequest");

            if (element != null)
            {
                string message = String.Empty;

                XElement nameElement = element.Element("TriggerText");

                if (nameElement == null)
                {
                    throw new FormatException("No Trigger Name Element given in request XML.");
                }
                string name = nameElement.Value;

                XElement sendDefaultElement = element.Element("SendDefaultMessage");
                bool sendDefault = true;
                if (sendDefaultElement != null)
                {
                    sendDefault = bool.Parse(sendDefaultElement.Value);
                }
                
                XElement messageElement = element.Element("Message");
                if (messageElement != null)
                {
                    message = messageElement.Value;
                }

                string source = string.Empty;
                XElement sourceElement = element.Element("Source");
                if (sourceElement != null)
                {
                    source = sourceElement.Value;
                }

                return new TriggerRequest(name, sendDefault, message, source);
            }
            else
            {
                throw new FormatException("No correct root Element given in request XML.");
            }

            
        }
    }
}
