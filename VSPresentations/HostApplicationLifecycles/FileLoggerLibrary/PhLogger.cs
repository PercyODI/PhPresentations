using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace FileLoggerLibrary
{
    public class PhLogger : ILogger
    {
        public PhLogger()
        {
            Directory.CreateDirectory(@"C:\HostApplicationLogs\");
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            File.AppendAllText($@"C:\HostApplicationLogs\{Assembly.GetEntryAssembly().GetName().Name}.log",
                DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff | ") + formatter(state, exception) + Environment.NewLine);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return state as IDisposable;
        }
    }

    public class PhLoggingProvider : ILoggerProvider
    {
        public static PhLogger MyLogger { get; set; }

        public PhLoggingProvider()
        {
            MyLogger = new PhLogger();
        }

        public void Dispose()
        {
            return;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return MyLogger;
        }
    }
}