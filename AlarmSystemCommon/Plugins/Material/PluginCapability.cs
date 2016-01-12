namespace AlarmSystem.Common.Plugins.Material
{
    /// <summary>
    /// Representing capabilities of plugins. 
    /// </summary>
    public enum PluginCapability
    {
        /// <summary>
        /// Plugin can be used as a Message Source.
        /// </summary>
        MessageSource,

        /// <summary>
        /// Plugin can be used as a Message Sink
        /// </summary>
        MessageSink,

        /// <summary>
        /// Plugin can be used as a Freetext Source.
        /// </summary>
        FreetextSource,

        /// <summary>
        /// Plugin can be used as a Message Sink.
        /// </summary>
        FreetextSink,

        /// <summary>
        /// Plugin can be used as a Trigger Message Source
        /// </summary>
        TriggerMessageSource,

        /// <summary>
        /// Plugin can be used as a trigger message sink.
        /// </summary>
        TriggerMessageSink,

        /// <summary>
        /// Plugin can be used as a trigger request source
        /// </summary>
        TriggerRequestSource,

        /// <summary>
        /// Plugin can be used as a trigger request sink.
        /// </summary>
        TriggerRequestSink
    }
}