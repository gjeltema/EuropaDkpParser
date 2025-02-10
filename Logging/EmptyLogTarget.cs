// -----------------------------------------------------------------------
// EmptyLogTarget.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace Gjeltema.Logging
{
    /// <summary>
    /// A LogTarget that dumps the incoming message.
    /// </summary>
    public sealed class EmptyLogTarget : ILogTarget
    {
        /// <inheritdoc/>
        public LogLevel LoggingLevel { get; set; } = LogLevel.Critical;

        /// <inheritdoc/>
        public void Log(LogLevel level, string message)
        {
        }

        /// <inheritdoc/>
        public void Log(LogLevel level, string format, params object[] args)
        {
        }
    }
}
