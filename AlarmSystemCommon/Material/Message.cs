﻿using System;
using System.Text.RegularExpressions;

namespace AlarmSystem.Common.Material
{
    /// <summary>
    /// Message from an outside person to the System.
    /// Messages are generated by IMessageSource Plugins and handled by IMessageSink plugins.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// A String literal describing the sender of the message, e.g. Phone Number, real name, user name...
        /// </summary>
        /// <value>
        /// The sender of the message.
        /// </value>
        public string From { get; set; }

        /// <summary>
        /// The date when the message was received.
        /// </summary>
        /// <value>
        /// The date.
        /// </value>
        public DateTime Date { get; set; }

        /// <summary>
        /// The text of the message.
        /// </summary>
        /// <value>
        /// The text.
        /// </value>
        public string Text { get; set; }

        private bool _arrTimeAvail = false;

        /// <summary>
        /// Gets a value indicating whether an arrival time could be parsed within the message or not..
        /// </summary>
        /// <value>
        /// <c>true</c> if an arrival time could be parsed; otherwise, <c>false</c>.
        /// </value>
        public bool ArrivalTimeAvailable
        {
            get
            {
                ParseArrivalTime();
                return _arrTimeAvail;
            }
        }

        /// <summary>
        /// Gets or sets the arrival time.
        /// </summary>
        /// <value>
        /// The arrival time parsed from the message text.
        /// </value>
        public DateTime ArrivalTime { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Message"/> class.
        /// </summary>
        public Message()
        {
            From = String.Empty;
            Date = DateTime.Now;
            Text = String.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Message"/> class.
        /// </summary>
        /// <param name="from">The sender of the message</param>
        /// <param name="time">The time the message was received.</param>
        /// <param name="text">The text of the message.</param>
        public Message(string from, DateTime time, string text)
        {
            From = from;
            Date = time;
            Text = text;
        }

        /// <summary>
        /// Parses the arrival time from the message text.
        /// This method should not be called directly, since it is implicitly called when checking for the availability of 
        /// an arrival time. The following formats are allowed for time formats:
        /// Relative:
        ///    - 5 min[uten]
        ///    - +5
        /// Absolute:
        ///    - 19:50
        /// All checks are case-insensitive
        /// </summary>
        protected void ParseArrivalTime()
        {
            _arrTimeAvail = false;

            string[] relativeRegexes =
            {
                @"(\d+)\s?[Mm](in)?",
                @"\+(\d+)"
            };

            string timeRegex = @"(\d{1,2})[\:\.\s](\d{1,2})";

            Regex absRegex = new Regex(timeRegex, RegexOptions.IgnoreCase);

            if (absRegex.IsMatch(Text))
            {
                _arrTimeAvail = true;

                Match m = absRegex.Match(Text);

                ArrivalTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), 0);
            }
            else 
            {
                foreach (string relativeRegex in relativeRegexes)
                {
                    Regex r = new Regex(relativeRegex, RegexOptions.IgnoreCase);

                    if (r.IsMatch(Text))
                    {
                        _arrTimeAvail = true;
                        Match m = r.Match(Text);

                        ArrivalTime = DateTime.Now.AddMinutes(int.Parse(m.Groups[1].Value));
                        break;
                    }
                }
            }
        }
    }
}
