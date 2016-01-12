namespace AlarmSystem.Common.Plugins.Interfaces
{
    /// <summary>
    /// Plugins implementing this interface can be used as a Free Text sink.
    /// </summary>
    public interface IFreetextSink
    {
        /// <summary>
        /// Handles the Freetext.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="caption">The caption of the freetext.</param>
        /// <param name="message">The message of the freetext.</param>
        void HandleFreetext(object sender, string caption, string message);
    }
}
