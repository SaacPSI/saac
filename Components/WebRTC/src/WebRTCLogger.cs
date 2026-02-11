// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.WebRTC
{
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Logger implementation for WebRTC components.
    /// </summary>
    internal class WebRTCLogger : IDisposable, Microsoft.Extensions.Logging.ILogger
    {
        /// <summary>
        /// Gets or sets the log level.
        /// </summary>
        public LogLevel LogLevel { get; set; } = LogLevel.Debug;

        /// <inheritdoc/>
        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            return true;
        }

        /// <inheritdoc/>
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel <= this.LogLevel)
            {
                Console.WriteLine("[" + eventId + "] " + formatter(state, exception));
            }
        }

        /// <summary>
        /// Logs a message with the specified log level.
        /// </summary>
        /// <param name="logLevel">The log level.</param>
        /// <param name="message">The message to log.</param>
        public void Log(Microsoft.Extensions.Logging.LogLevel logLevel, string message)
        {
            if (logLevel >= this.LogLevel)
            {
                Console.WriteLine($"[{logLevel}] {message}");
            }
        }
    }
}
