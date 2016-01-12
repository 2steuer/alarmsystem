namespace AlarmSystem.Common.Logging.Material
{
    /// <summary>
    /// Log Levels of log entries. Each of these levels represents an importance of a message. This enum must stay in this order to make to 
    /// Log module function in the expected way.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Error messages, critical for application execution. Usually called when Exceptions are raised.
        /// </summary>
        Error,

        /// <summary>
        /// Warning messages are not critical for application execution, but the user should be aware
        /// of the fact they occoured.
        /// </summary>
        Warning,

        /// <summary>
        /// Info messages are general application informations.
        /// </summary>
        Info,

        /// <summary>
        /// Debug messages are useful to find errors.
        /// </summary>
        Debug,

        /// <summary>
        /// Verbose messages are raised if Debug messages are not enaugh to find every error. Very much information.
        /// </summary>
        Verbose
    }
}
