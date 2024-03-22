using Microsoft.Extensions.Logging;
using UnityEngine;
using System;

public class UnityLoggerFactory : IDisposable, ILoggerFactory
{
    /// <summary>
    /// Creates a new <see cref="ILogger"/> instance.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <returns>The <see cref="ILogger"/>.</returns>
    public virtual Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
    {
        return new UnityLogger();
    }

    /// <summary>
    /// Adds an <see cref="ILoggerProvider"/> to the logging system.
    /// </summary>
    /// <param name="provider">The <see cref="ILoggerProvider"/>.</param>
    public virtual void AddProvider(ILoggerProvider provider)
    { }

    public void Dispose()
    { }
}

public class UnityLogger : IDisposable, Microsoft.Extensions.Logging.ILogger
{
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
        Debug.Log("[" + eventId + "] " + formatter(state, exception));
        System.Diagnostics.Debug.WriteLine("[" + eventId + "] " + formatter(state, exception));
    }
}

