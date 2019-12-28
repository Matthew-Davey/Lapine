namespace Lapine {
    using System;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;

    public static class Log {
        static public ILoggerFactory LoggerFactory { get; set; } = new NullLoggerFactory();

        static public ILogger CreateLogger(String categoryName) =>
            LoggerFactory.CreateLogger(categoryName);

        static public ILogger CreateLogger(Type type) =>
            LoggerFactory.CreateLogger(type);

        static public ILogger<T> CreateLogger<T>() =>
            LoggerFactory.CreateLogger<T>();
    }
}
