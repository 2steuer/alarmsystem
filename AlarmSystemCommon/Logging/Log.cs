using System;
using System.IO;
using System.Text;
using AlarmSystem.Common.Logging.Material;

namespace AlarmSystem.Common.Logging
{
    public delegate void LogMessageDelegate(DateTime time, LogLevel level, string module, string message);

    /// <summary>
    /// Offers functionality for logging application events to the console or to the file system.
    /// 
    /// To add a log message, the static function Log.Add() shall be used. To add an Exception log entry, use Log.AddException.
    /// One part of the application should instanciate Log and configure it's behaviour for printing or saving log entries.
    /// </summary>
    public class Log
    {
        /// <summary>
        /// Occurs when a new log messages was added with Log.Add() or Log.AddException().
        /// </summary>
        public static event LogMessageDelegate OnLogMessage;

        /// <summary>
        /// Adds a new Entry to the Logging service.
        /// </summary>
        /// <param name="level">The level of the log message-.</param>
        /// <param name="module">The module name given by a string to determine the source of a log entry.</param>
        /// <param name="message">The message.</param>
        public static void Add(LogLevel level, string module, string message)
        {
            DateTime time = DateTime.Now;

            if (OnLogMessage != null)
            {
                OnLogMessage(time, level, module, message);
            }
        }

        /// <summary>
        /// Adds an error to the Log and prints additional information about the given exception.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <param name="e">The exception of which the Type, Message and Stack trace shall be printed.</param>
        public static void AddException(string module, Exception e)
        {
            Add(LogLevel.Error, module, String.Format("Exception: {0} / {1} {2} {3}. Inner: {4}", e.GetType(), e.Message, Environment.NewLine, e.StackTrace, e.InnerException));
        }

        /// <summary>
        /// Gets or sets a value indicating whether log to console.
        /// </summary>
        /// <value>
        ///   <c>true</c> if logging to the console is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool LogToConsole { get; set; }

        /// <summary>
        /// Gets or sets the the level of the console log output. All messages having a log leven equal to or lower of this level will be printed to console.
        /// </summary>
        /// <value>
        /// The console level.
        /// </value>
        public LogLevel ConsoleLevel { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to write log entries to a file or not..
        /// </summary>
        /// <value>
        ///   <c>true</c> if [log to file]; otherwise, <c>false</c>.
        /// </value>
        public bool LogToFile { get; set; }

        /// <summary>
        /// Gets or sets the the level of the file log output. All messages having a log leven equal to or lower of this level will be saved to a file.
        /// </summary>
        /// <value>
        /// The console level.
        /// </value>
        public LogLevel FileLevel { get; set; }

        /// <summary>
        /// Gets the name of the log file to save log entries to.
        /// </summary>
        /// <value>
        /// The path to the log file.
        /// </value>
        public string LogFileName { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether to create the log file when it's not existing or exiting with an error in this case.
        /// </summary>
        /// <value>
        ///   <c>true</c> if creating nonexisting log files is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool CreateFile { get; set; }

        private FileStream _stream;
        private bool _fileLock = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Log"/> class. This instance is then responsible for logging entries to the console and the file.
        /// These entries are usually created by calling static methods Log.Add() or Log.AddException()
        /// </summary>
        /// <param name="consoleLog">if set to <c>true</c> Logging to console is enabled.</param>
        /// <param name="consoleLevel">The Maximum log level printed to console.</param>
        /// <param name="fileLog">if set to <c>true</c> logging to a file is enabled.</param>
        /// <param name="fileLevel">The maximum log level of entries written to the log file..</param>
        /// <param name="fileName">Path of the log file..</param>
        public Log(bool consoleLog, LogLevel consoleLevel, bool fileLog, LogLevel fileLevel, string fileName)
        {
            LogToConsole = consoleLog;
            ConsoleLevel = consoleLevel;

            LogToFile = fileLog;
            FileLevel = fileLevel;
            LogFileName = fileName;
            CreateFile = true;
        }

        /// <summary>
        /// Starts the logging process, i.e. registers to Log.OnLogMessage.
        /// </summary>
        /// <returns>true if successfull, false if not (i.e. log file was not existing but CreateFile was false)</returns>
        public bool StartLogging()
        {
            OnLogMessage += Log_OnLogMessage;

            if (LogToFile)
            {
                try
                {
                    if (File.Exists(LogFileName))
                    {
                        _stream = new FileStream(LogFileName, FileMode.Append);
                        return true;
                    }
                    else
                    {
                        if (CreateFile)
                        {
                            _stream = new FileStream(LogFileName, FileMode.CreateNew);
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Initialising Logging system failed...");
                    Console.WriteLine("{0}: {1}", ex.GetType(), ex.Message);
                    throw;
                }
            }

            return true;
        }

        /// <summary>
        /// Stops the logging.
        /// </summary>
        public void StopLogging()
        {
            OnLogMessage -= Log_OnLogMessage;

            if (LogToFile)
            {
                if (_stream.CanWrite)
                {
                    _stream.Flush(true);
                    _stream.Close();
                }
            }
        }


        void Log_OnLogMessage(DateTime time, LogLevel level, string module, string message)
        {
            string dateString = String.Format("[{0:00}.{1:00}.{2:0000} - {3:00}:{4:00}:{5:00}.{6:000}/{7}]", time.Day, time.Month, time.Year,
                        time.Hour, time.Minute, time.Second, time.Millisecond, level);
            string msgString = String.Format("({0}) {1}", module, message);

            string logStr = String.Format("{0} {1}", dateString, msgString);

            if (LogToConsole)
            {
                if (level <= ConsoleLevel)
                {
                    Console.WriteLine(logStr);
                }
            }

            if (LogToFile)
            {
                if (level <= FileLevel)
                {
                    try
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(logStr + Environment.NewLine);
                        _stream.Write(bytes, 0, bytes.Length);
                        //_stream.Flush(true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception while writing log: {0}: {1}", ex.GetType(), ex.Message);
                        throw;
                    }
                }
            }
        }

    }
}
