// <copyright file="Logger.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Logging
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides logging to file.
    /// </summary>
    public static partial class Logger
    {
        private const string Name = "System";

        private static TimeSpan flushTime = TimeSpan.FromSeconds(1);
        private static LogWriter logWriter;
        private static System.Threading.Timer timer;

        /// <summary>
        /// (Re-)initializes this instance.
        /// </summary>
        /// <param name="basePath">The base path, the "logs" folder will be below this.</param>
        public static void Initialize(DirectoryInfo basePath)
        {
            logWriter?.Flush();

            logWriter = new LogWriter(Path.Combine(basePath.FullName, "logs"));

            Log(LogLevel.Information, Name, "App startup");

            timer = new System.Threading.Timer(_ => logWriter.Flush(), null, flushTime, flushTime);
            Task.Run(() => logWriter.Cleanup());
        }

        /// <summary>
        /// Shuts this instance down.
        /// </summary>
        public static void Shutdown()
        {
            Log(LogLevel.Information, Name, "App shutdown");
            timer.Change(-1, -1);
            timer.Dispose();
            logWriter.Flush();
            logWriter = null;
        }

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="pageName">Name of the page or class.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception (optional).</param>
        public static void Log(LogLevel level, string pageName, string message, Exception exception = null)
        {
            var msg = new LogMessage(level, pageName, message, exception);
            logWriter.Add(msg);
        }
    }
}
