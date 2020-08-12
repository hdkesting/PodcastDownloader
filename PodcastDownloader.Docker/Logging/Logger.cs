// <copyright file="Logger.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Logging
{
    using System;
    using System.IO;

    /// <summary>
    /// Provides logging to file.
    /// </summary>
    public static class Logger
    {
        private const int MaxEmptyFlushes = 20; // @ 2 secs each (see flushTime)

        private static TimeSpan flushTime = TimeSpan.FromSeconds(2);
        private static LogWriter logWriter;
        private static System.Threading.Timer flushTimer;
        private static int emptyFlushCount;
        private static bool stoppedFlushing;

        /// <summary>
        /// Gets the folder that the logs are written to. Set through <see cref="Initialize(DirectoryInfo)"/>.
        /// </summary>
        /// <value>
        /// The log folder.
        /// </value>
        public static string LogFolder { get; private set; }

        /// <summary>
        /// Gets or sets the minimum level to log.
        /// </summary>
        /// <value>
        /// The minimum log level.
        /// </value>
        public static LogLevel MinLogLevel { get; set; } = LogLevel.Debug;

        /// <summary>
        /// (Re-)initializes this instance.
        /// </summary>
        /// <param name="basePath">The base path, the "logs" folder will be below this.</param>
        public static void Initialize(DirectoryInfo basePath)
        {
            if (basePath is null)
            {
                throw new ArgumentNullException(nameof(basePath));
            }

            if (!basePath.Exists)
            {
                basePath.Create();
            }

            // initialize, but don't start. Wait for the first message to arrive.
            flushTimer = new System.Threading.Timer(_ => Flusher());
            emptyFlushCount = 0;
            stoppedFlushing = true;

            // if re-initialized, flush old writer
            logWriter?.Flush();

            LogFolder = Path.Combine(basePath.FullName, "logs");
            logWriter = new LogWriter(LogFolder);

            Log(LogLevel.Information, nameof(Logger), "App startup");
            System.Threading.Tasks.Task.Run(() => Cleanup());
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
            Log(LogLevel.Information, nameof(Logger), "App shutdown");
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
            if (logWriter == null)
            {
                if (string.IsNullOrEmpty(LogFolder))
                {
                    // cannot initialize, just forget about it
                    return;
                }

                Initialize(new DirectoryInfo(LogFolder));
            }

            if (level >= MinLogLevel)
            {
                var msg = new LogMessage(level, pageName, message, exception);
                logWriter.Add(msg);

                RestartIfStopped();
            }
        }

        private static void RestartIfStopped()
        {
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

        private static void PauseFlushing()
        {
            lock (logWriter)
            {
                flushTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                logWriter.Flush();

                stoppedFlushing = true;
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
                    PauseFlushing();
                }
            }
        }
    }
}
