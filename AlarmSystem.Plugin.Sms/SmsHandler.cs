using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using AlarmSystem.Common.Material;
using AlarmSystem.Common.Plugins;
using AlarmSystem.Common.Plugins.Interfaces;
using AlarmSystem.Common.Logging;
using GsmComm.PduConverter;
using GsmComm.PduConverter.SmartMessaging;
using LogLevel = AlarmSystem.Common.Logging.Material.LogLevel;
using Log = AlarmSystem.Common.Logging.Log;
using GsmComm.GsmCommunication;

namespace AlarmSystem.Plugin.Sms
{
    class SmsHandler : AlarmSystemPluginBase, IMessageSource, ITriggerMessageSink
    {
        #region Plugin information
        public override string PluginName
        {
            get { return "SmsPlugin"; }
        }

        public override string PluginAuthor
        {
            get { return "Merlin Steuer"; }
        }

        public override string PluginDescription
        {
            get { return "SMS Communication Plugin for AlarmSystem"; }
        }

        public override string PluginVersion
        {
            get { return "1.0"; }
        }
        #endregion

        public event ReceivedMessageDelegate OnMessageReceived;

        public GsmCommMain _gsm;

        Timer _rcvTimer = new Timer();

        public bool NormalizeNumbers { get; set; }

        private bool _resolveNumbers = false;

        public bool ResolveNumbers
        {
            get { return _resolveNumbers; }
        }

        Queue<TriggerMessage> _messagesToSend = new Queue<TriggerMessage>();

        private int _messagesPerTimerTick = 2;

        Dictionary<int, IList<SmsPdu>> _concatPdus = new Dictionary<int, IList<SmsPdu>>();

        protected override void InitRoutine()
        {
            string port = GetConfigString("Port");
            int baud = GetConfigInteger("Baud");
            _messagesPerTimerTick = GetConfigInteger("MessagesPerTick");

            NormalizeNumbers = true;
            _resolveNumbers = true;

            _gsm = new GsmCommMain(port, baud);
            Log.Add(LogLevel.Debug, "SMS", String.Format("Initialising on port {0}:{1}baud", port, baud));
        }

        public override void Start()
        {
            try
            {
                Log.Add(LogLevel.Info, "SMS", "Opening device to port.");
                _gsm.Open();
                StartReceiving();
            }
            catch (Exception ex)
            {
                Log.AddException("SMS", ex);
            }
            
        }

        public override void Stop()
        {
            StopReceiving();
            _gsm.Close();
        }

        public void StartReceiving(int interval)
        {
            if (_gsm.IsConnected())
            {
                _rcvTimer = new Timer(interval);
                _rcvTimer.AutoReset = true;
                _rcvTimer.Elapsed += _rcvTimer_Elapsed;
                _rcvTimer.Start();
                Log.Add(LogLevel.Info, "SMS", "SMS receiving started.");
            }
            else
            {
                Log.Add(LogLevel.Error, "SMS", "Cannot start receiving SMS, not connected to Phone.");
            }

            
        }

        public void StartReceiving()
        {
            StartReceiving(1000);
        }

        public void StopReceiving()
        {
            if (_rcvTimer.Enabled)
            {
                Log.Add(LogLevel.Info, "SMS", "SMS receiving stopped.");
                _rcvTimer.Stop();
            }
        }

        void _rcvTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_messagesToSend.Count > 0)
            {
                SendMessages();
            }
            else
            {
                ReceiveMessages();
            }
        }

        void ReceiveMessages()
        {
            Log.Add(LogLevel.Verbose, "SMS", "Reading SMS messages from device...");
            Stopwatch sw = new Stopwatch();
            sw.Start();

            DecodedShortMessage[] msgs = _gsm.ReadMessages(PhoneMessageStatus.All, "MT");

            Log.Add(LogLevel.Verbose, "SMS", String.Format("{0} messages in storage", msgs.Length));

            foreach (DecodedShortMessage msg in msgs)
            {
                string from = string.Empty;
                string text = string.Empty;
                DateTime received = DateTime.Now;

                bool _fullMessageReceived = false;

                SmsDeliverPdu pdu = (SmsDeliverPdu)msg.Data;

                if (SmartMessageDecoder.IsPartOfConcatMessage(pdu))
                {
                    IConcatenationInfo info = SmartMessageDecoder.GetConcatenationInfo(pdu);

                    Log.Add(LogLevel.Debug, "SMS", string.Format("Received multi-part message {0}: {1}/{2}", info.ReferenceNumber, info.CurrentNumber, info.TotalMessages));

                    if (_concatPdus.ContainsKey(info.ReferenceNumber))
                    {
                        _concatPdus[info.ReferenceNumber].Add(pdu);
                    }
                    else
                    {
                        _concatPdus.Add(info.ReferenceNumber, new List<SmsPdu>() { pdu });
                    }

                    if (SmartMessageDecoder.AreAllConcatPartsPresent(_concatPdus[info.ReferenceNumber]))
                    {
                        _fullMessageReceived = true;

                        from = pdu.OriginatingAddress;
                        received = pdu.SCTimestamp.ToDateTime();
                        text = SmartMessageDecoder.CombineConcatMessageText(_concatPdus[info.ReferenceNumber]);
                    }

                    
                }
                else
                {
                    Log.Add(LogLevel.Debug, "SMS", "Received single-part SMS.");

                    _fullMessageReceived = true;

                    from = String.Format("{0}", pdu.OriginatingAddress);
                    received = pdu.SCTimestamp.ToDateTime();
                    text = pdu.UserDataText;
                }

                if (_fullMessageReceived)
                {
                    Log.Add(LogLevel.Info, "SMS", String.Format("Incoming SMS from {0}", from));

                    if (NormalizeNumbers)
                    {
                        from = NormalizeNumber(from);
                    }

                    if (_resolveNumbers)
                    {
                        from = ResolveNameByNumber(from);
                    }

                    Log.Add(LogLevel.Debug, "SMS", String.Format("Message from {0} at {1}: {2}", from, received, text));

                    if (OnMessageReceived != null)
                    {
                        OnMessageReceived(this, new Message(from, received, text));
                    }
                }

                _gsm.DeleteMessage(msg.Index, msg.Storage);
                               
            }
            sw.Stop();

            Log.Add(LogLevel.Verbose, "SMS", String.Format("Reading took {0}ms", sw.Elapsed.TotalMilliseconds));
        }

        void SendMessages()
        {
            Log.Add(LogLevel.Verbose, "SMS", "Sending SMS...");

            List<TriggerMessage> msgs = new List<TriggerMessage>();

            for (int i = 0; i < _messagesPerTimerTick && _messagesToSend.Count > 0; i++)
            {
                msgs.Add(_messagesToSend.Dequeue());
            }

            foreach (TriggerMessage msg in msgs)
            {
                Log.Add(LogLevel.Info, "SMS", string.Format("Sending Message to {0}", msg.Destination));
                Log.Add(LogLevel.Verbose, "SMS",
                    string.Format("Message: {0} ({2}): {1}", msg.Destination, msg.Text, msg.FlashMessage));

                SmsSubmitPdu[] pdus = SmartMessageFactory.CreateConcatTextMessage(msg.Text, msg.Destination);

                if (msg.FlashMessage)
                {
                    foreach (var pdu in pdus)
                    {

                        pdu.DataCodingScheme = DataCodingScheme.Class0_7Bit;
                    }
                }

                _gsm.SendMessages(pdus);
            }
        }

        string NormalizeNumber(string number)
        {
            string allowedChars = "0123456789";
            string ret = String.Empty;

            foreach (char c in number)
            {
                if (allowedChars.Contains(c))
                {
                    ret += c;
                }
            }

            if (ret.StartsWith("49"))
            {
                ret = "0" + ret.Substring(2);
            }

            return ret;
        }

        string ResolveNameByNumber(string number)
        {
            IDbCommand cmd;
            IDataReader reader;
            try
            {
                cmd = DatabaseService.TryCreateCommand();
                cmd.CommandText = String.Format("SELECT name FROM persons WHERE number='{0}'", number);
                reader = cmd.ExecuteReader();
            }
            catch (Exception ex)
            {
                Log.AddException("SMS", ex);
                return number;
            }
            

            Log.Add(LogLevel.Verbose, "SMS", cmd.CommandText);
            string ret;
            if (reader.Read())
            {
                ret = (string)reader["name"];
            }
            else
            {
                Log.Add(LogLevel.Warning, "SMS", String.Format("Number {0} not found in db.", number));
                ret = number;
            }

            reader.Close();
            return ret;
        }

        public void HandleTriggerMessage(object sender, TriggerMessage message)
        {
            Log.Add(LogLevel.Debug, "SMS", string.Format("Enqueing Message to {0}", message.Destination));
            _messagesToSend.Enqueue(message);
        }
    }
}
