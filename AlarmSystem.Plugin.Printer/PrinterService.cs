using System;
using System.IO.Ports;
using AlarmSystem.Common.Logging;
using AlarmSystem.Common.Logging.Material;
using AlarmSystem.Common.Material;
using AlarmSystem.Common.Plugins;
using AlarmSystem.Common.Plugins.Interfaces;
using ThermalDotNet;

namespace AlarmSystem.Plugin.Printer
{
    class PrinterService : AlarmSystemPluginBase, IMessageSink, IFreetextSink
    {
        #region Plugin information
        public override string PluginName
        {
            get { return "ThermalPrinterPlugin"; }
        }

        public override string PluginAuthor
        {
            get { return "Merlin Steuer"; }
        }

        public override string PluginDescription
        {
            get { return "A thermal printer plugin for AlarmSystem using ESC/POS Commands."; }
        }

        public override string PluginVersion
        {
            get { return "1.0"; }
        }
        #endregion

        private SerialPort _port;
        private ThermalPrinter _printer;

        private int _lineLength = 42;

        public int LineLength
        {
            get { return _lineLength; }
            set { _lineLength = value; }
        }

        protected override void InitRoutine()
        {
            _port = new SerialPort(GetConfigString("Port"), GetConfigInteger("Baud"));
            Log.Add(LogLevel.Debug, "Printer", String.Format("Initialised on Port {0} at {1} baud", _port.PortName, _port.BaudRate));
        }

        public override void Start()
        {
            Log.Add(LogLevel.Info, "Printer", "Opening port to printer");
            try
            {
                _port.Open();
                _printer = new ThermalPrinter(_port);
                _printer.Reset();
            }
            catch (Exception ex)
            {
                Log.AddException("Printer", ex);
            }
        }

        public override void Stop()
        {
            Log.Add(LogLevel.Info, "Printer", "Closing port to printer");
            _port.Close();
        }

        public void HandleMessage(object sender, Message message, bool cut)
        {
            Log.Add(LogLevel.Info, "Printer", "Printing received message");

            string fromline = String.Format("Von: {0}", message.From);
            string timeLine = String.Format("Um:  {0:00}:{1:00}", message.Date.Hour, message.Date.Minute);
            string dateLine = String.Format("{0:00}.{1:00}.{2:0000}", message.Date.Day, message.Date.Month, message.Date.Year);

            try
            {
                PrintHorizontalLine(_lineLength);
                _printer.WriteLine(dateLine);
                _printer.WriteLine_Big(fromline);
                _printer.WriteLine_Big(timeLine);

                if (message.ArrivalTimeAvailable)
                {
                    _printer.WriteLine_Big(string.Format("Ankunft: {0}:{1}", message.ArrivalTime.Hour, message.ArrivalTime.Minute));
                }

                _printer.LineFeed();
                _printer.WriteLine(message.Text);

                if (cut)
                {
                    Log.Add(LogLevel.Verbose, "Printer", "Cutting");
                    _printer.LineFeed(5);
                    _printer.Cut();
                }
                else
                {
                    _printer.LineFeed(1);
                }

                Log.Add(LogLevel.Verbose, "Printer", "Finished printing");
            }
            catch (Exception ex)
            {
                Log.AddException("Printer", ex);
            }
        }

        public void HandleMessage(object sender, Message message)
        {
            HandleMessage(sender, message, true);
        }

        private void PrintHorizontalLine(int count, char chr)
        {
            string str = String.Empty;

            for (int i = 0; i < count; i++)
            {
                str += chr;
            }

            _printer.WriteLine(str);
        }

        private void PrintHorizontalLine(int count)
        {
            PrintHorizontalLine(count, '=');
        }

        private void PrintHorizontalLine(char chr)
        {
            PrintHorizontalLine(32, chr);
        }

        private void PrintHorizontalLine()
        {
            PrintHorizontalLine(32, '=');
        }

        public void HandleFreetext(object source, string caption, string message)
        {
            Log.Add(LogLevel.Debug, "Printer", "Printing free text");
            Log.Add(LogLevel.Verbose, "Printer", String.Format("{0}:{1}", caption, message));
            if (caption != String.Empty)
            {
                _printer.WriteLine_Big(caption);
            }
            
            _printer.WriteLine(message);
            _printer.LineFeed(5);
            _printer.Cut();
        }

        public void HandleFreetext(string caption, string message)
        {
            HandleFreetext(this, caption, message);
        }
    }
}
