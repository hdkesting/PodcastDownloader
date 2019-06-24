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
    public static class Logger
    {
        private const string Name = "System";
        private const int MaxEmptyFlushes = 10;

        private static TimeSpan flushTime = TimeSpan.FromSeconds(2);
        private static LogWriter logWriter;
        private static System.Threading.Timer flushTimer;
        private static int emptyFlushCount;
        private static bool stoppedFlushing;

        /// <summary>
        /// (Re-)initializes this instance.
        /// </summary>
        /// <param name="basePath">The base path, the "logs" folder will be below this.</param>
        public static void Initialize(DirectoryInfo basePath)
        {
            logWriter?.Flush();

            logWriter = new LogWriter(Path.Combine(basePath.FullName, "logs"));

            Log(LogLevel.Information, Name, "App startup");

            // initialize, but don't start. Wait for the first message to arrive.
            flushTimer = new System.Threading.Timer(_ => Flusher());
            emptyFlushCount = 0;
            stoppedFlushing = false;

            Task.Run(() => Cleanup());
        }

        /// <summary>
        /// Cleans up the old logs.
        /// </summary>
        public static void Cleanup()
        {
            logWriter.Cleanup();
        }

        /// <summary>
        /// Shuts this instance down.
        /// </summary>
        public static void Shutdown()
        {
            Log(LogLevel.Information, Name, "App shutdown");
            flushTimer.Change(-1, -1);
            flushTimer.Dispose();
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

            lock (logWriter)
            {
                if (stoppedFlushing)
                {
                    // timer was stopped, but a new message arrived, so start up
                    stoppedFlushing = false;
                    emptyFlushCount = 0;
                    flushTimer.Change(flushTime, flushTime);
                }
            }
        }

        private static void Flusher()
        {
            if (logWriter.Flush())
            {
                emptyFlushCount = 0;
            }
            else
            {
                emptyFlushCount++;

                if (emptyFlushCount > MaxEmptyFlushes)
                {
                    // lots of empty flushes, so stop timer
                    lock (logWriter)
                    {
                        stoppedFlushing = true;
                        flushTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                    }
                }
            }
        }
    }
}
