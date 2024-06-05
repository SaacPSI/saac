using Microsoft.Extensions.Logging;

namespace SAAC.WebRTC
{
    internal class WebRTCLogger : IDisposable, Microsoft.Extensions.Logging.ILogger
    {
        public LogLevel LogLevel { get; set; } = LogLevel.Debug;

        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }

        public void Dispose()
        {
        }

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if(logLevel <= LogLevel)
                Console.WriteLine("[" + eventId + "] " + formatter(state, exception));
        }

        public void Log(Microsoft.Extensions.Logging.LogLevel logLevel, string message)
        {
            if (logLevel >= LogLevel)
                Console.WriteLine($"[{logLevel}] {message}");
        }
    }
}
