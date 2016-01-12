using System;
using System.Diagnostics;
using System.Text;
using System.Timers;
using AlarmSystem.Common.Logging;
using AlarmSystem.Common.Logging.Material;
using AlarmSystem.Common.Plugins;
using AlarmSystem.Common.Plugins.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AlarmSystem.Plugin.Telegram
{
    class TelegramHandler : AlarmSystemPluginBase, IMessageSource, IMessageSink, IFreetextSink
    {
        #region Plugin information
        public override string PluginName
        {
            get { return "TelegramPlugin"; }
        }

        public override string PluginAuthor
        {
            get { return "Merlin Steuer"; }
        }

        public override string PluginDescription
        {
            get { return "A Telegram Bot plugin for AlarmSystem"; }
        }

        public override string PluginVersion
        {
            get { return "1.0"; }
        }
        #endregion

        public event ReceivedMessageDelegate OnMessageReceived;

        private Api _api;
        private int _telegramOffset = 0;

        private Timer _rcvTimer;

        public int ChatId { get; set; }

        protected override void InitRoutine()
        {
            _api = new Api(GetConfigString("ApiKey"));
            ChatId = GetConfigInteger("ForwardChatId");

            Log.Add(LogLevel.Debug, "Telegram", "Initialising with bot key " + _api);
        }

        public void StartReceiving(int interval)
        {
            _rcvTimer = new Timer(interval);
            _rcvTimer.AutoReset = true;
            _rcvTimer.Elapsed += _rcvTimer_Elapsed;
            _rcvTimer.Start();
            Log.Add(LogLevel.Info, "Telegram", "Receiving started");
            
        }

        public override void Start()
        {
            try
            {
                User u = GetMe();

                Log.Add(LogLevel.Info, "Telegram", string.Format("Logged in as Bot {0} {1} / {2}", u.FirstName, u.LastName, u.Username));
            }
            catch (AggregateException e)
            {
                throw e.InnerException;
            }

            StartReceiving(1000);
        }

        public override void Stop()
        {
            _rcvTimer.Stop();
            Log.Add(LogLevel.Info, "Telegram", "Receiving stopped");
        }

        public User GetMe()
        {
            return _api.GetMe().Result;
        }

        public void HandleMessage(object sender, Common.Material.Message message)
        {
            if (ChatId != 0)
            {
                Log.Add(LogLevel.Info, "Telegram", "Handling received message");
                try
                {
                    StringBuilder strB = new StringBuilder();
                    strB.AppendLine(String.Format("Nachricht von {0}", message.From));
                    strB.AppendLine(String.Format("Um {0:00}:{1:00}", message.Date.Hour, message.Date.Minute));

                    if (message.ArrivalTimeAvailable)
                    {
                        strB.AppendLine(String.Format("Ankunft: {0}:{1}", message.ArrivalTime.Hour,
                            message.ArrivalTime.Minute));
                    }

                    strB.AppendLine();

                    strB.AppendLine(message.Text);

                    Message msg = _api.SendTextMessage(ChatId, strB.ToString()).Result;
                }
                catch (Exception ex)
                {
                    Log.AddException("Telegram", ex);
                }
            }
            else
            {
                Log.Add(LogLevel.Warning, "Telegram", "Message handling requested, but no Chat-ID set. Ignoring.");
            }
            
            
        }

        void _rcvTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Log.Add(LogLevel.Verbose, "Telegram", "Now querying for new updates...");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            
            Update[] updates = {};

            try
            {
                updates = _api.GetUpdates(_telegramOffset).Result;
                
            }
            catch (Exception ex)
            {
                Log.AddException("Telegram", ex);
            }
            

            Log.Add(LogLevel.Verbose, "Telegram", String.Format("Got {0} new updates", updates.Length));

            foreach (Update update in updates)
            {
                Console.WriteLine(update.Message.Type);
                Log.Add(LogLevel.Debug, "Telegram", String.Format("Processing {0}...", update.Message.Type));
                try
                {
                    if (update.Message.Type == MessageType.TextMessage)
                    {
                        string name = String.Format("{0} {1}", update.Message.From.FirstName, update.Message.From.LastName);

                        Log.Add(LogLevel.Info, "Telegram", String.Format("Incoming message from {0}", name));
                        Log.Add(LogLevel.Debug, "Telegram", String.Format("Message from {0}: {1}", name, update.Message.Text));

                        if (OnMessageReceived != null)
                        {
                            OnMessageReceived(this, new Common.Material.Message(name, DateTime.Now, update.Message.Text));
                        }
                    }
                    
                }
                catch (Exception ex)
                {
                    Log.AddException("Telegram", ex);
                }

                _telegramOffset = update.Id + 1;
                
            }
            sw.Stop();
            Log.Add(LogLevel.Verbose, "Telegram", String.Format("Update handling took {0}ms", sw.Elapsed.TotalMilliseconds));
            
        }

        public void HandleFreetext(object source, string caption, string message)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("*{0}*", caption);
            sb.AppendLine();
            sb.AppendLine();
            sb.Append(message);

            try
            {
                Log.Add(LogLevel.Debug, "Telegram", "Sending free text message");
                Log.Add(LogLevel.Verbose, "Telegram", sb.ToString());

                Message msg = _api.SendTextMessage(ChatId, sb.ToString(), false, 0, null, true).Result;
            }
            catch (Exception ex)
            {
                Log.AddException("Telegram", ex);
            }
        }

        public void HandleFreetext(string caption, string message)
        {
            HandleFreetext(this, caption, message);
        }
    }
}
