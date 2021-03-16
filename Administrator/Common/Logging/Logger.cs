﻿using System;
using Microsoft.Extensions.Logging;

namespace Administrator.Common
{
    public sealed class Logger<T> : ILogger<T>
    {
        private readonly ILogger _logger;

        public Logger(ILoggerFactory factory)
        {
            _logger = factory.CreateLogger(typeof(T).Name);
        }
        
        IDisposable ILogger.BeginScope<TState>(TState state)
            => _logger.BeginScope(state);

        bool ILogger.IsEnabled(LogLevel logLevel)
            => _logger.IsEnabled(logLevel);

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            => _logger.Log(logLevel, eventId, state, exception, formatter);
    }
}