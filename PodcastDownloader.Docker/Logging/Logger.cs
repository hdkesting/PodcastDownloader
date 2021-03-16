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
    public class Logger : ILogger
    {
        private const int MaxEmptyFlushes = 20; // @ 2 secs each (see flushTime)

        private static LoggingConfig configuration;

        private TimeSpan flushTime = TimeSpan.FromSeconds(2);
        private LogWriter logWriter;
        private System.Threading.Timer flushTimer;
        private int emptyFlushCount;
        private bool stoppedFlushing;

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <exception cref="ArgumentNullException">config cannot be null.</exception>
        public Logger(LoggingConfig config)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            configuration = config;
            this.MinLogLevel = config.MinLogLevel;
            this.Initialize(config);
        }

        /// <summary>
        /// Gets the folder that the logs are written to. Set through <see cref="Initialize(DirectoryInfo)"/>.
        /// </summary>
        /// <value>
        /// The log folder.
        /// </value>
        public string LogFolder { get; private set; }

        /// <summary>
        /// Gets or sets the minimum level to log.
        /// </summary>
        /// <value>
        /// The minimum log level.
        /// </value>
        public LogLevel MinLogLevel { get; set; } = LogLevel.Debug;

        /// <summary>
        /// Gets the path to the current log file.
        /// </summary>
        public string CurrentLogfilePath => this.logWriter?.CurrentLogfile;

        /// <summary>
        /// Cleans up the old logs.
        /// </summary>
        public void Cleanup()
        {
            this.logWriter.Cleanup();
        }

        /// <summary>
        /// Shuts this instance down.
        /// </summary>
        public void Shutdown()
        {
            this.Log(LogLevel.Information, nameof(Logger), "App shutdown");
            this.flushTimer.Change(-1, -1);
            this.flushTimer.Dispose();
            this.logWriter.Flush();
            this.logWriter = null;
        }

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="pageName">Name of the page or class.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception (optional).</param>
        public void Log(LogLevel level, string pageName, string message, Exception exception = null)
        {
            if (this.logWriter == null)
            {
                if (configuration is null)
                {
                    // cannot initialize, just forget about it
                    return;
                }

                this.Initialize(configuration);
            }

            if (level >= this.MinLogLevel)
            {
                var msg = new LogMessage(level, pageName, message, exception);
                this.logWriter.Add(msg);

                this.RestartIfStopped();
            }
        }

        /// <summary>
        /// (Re-)initializes this instance.
        /// </summary>
        /// <param name="config">The configuration.</param>
        private void Initialize(LoggingConfig config)
        {
            // initialize, but don't start. Wait for the first message to arrive.
            this.flushTimer = new System.Threading.Timer(_ => this.Flusher());
            this.emptyFlushCount = 0;
            this.stoppedFlushing = true;

            // if re-initialized, flush old writer
            this.logWriter?.Flush();

            this.logWriter = new LogWriter(config);

            Log(LogLevel.Information, nameof(Logger), "App startup");
            System.Threading.Tasks.Task.Run(() => this.Cleanup());
        }

        private void RestartIfStopped()
        {
            lock (this.logWriter)
            {
                if (this.stoppedFlushing)
                {
                    // timer was stopped, but a new message arrived, so start up
                    this.stoppedFlushing = false;
                    this.emptyFlushCount = 0;
                    this.flushTimer.Change(this.flushTime, this.flushTime);
                }
            }
        }

        private void PauseFlushing()
        {
            lock (this.logWriter)
            {
                this.flushTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                this.logWriter.Flush();

                this.stoppedFlushing = true;
            }
        }

        private void Flusher()
        {
            if (this.logWriter.Flush())
            {
                this.emptyFlushCount = 0;
            }
            else
            {
                this.emptyFlushCount++;

                if (this.emptyFlushCount > MaxEmptyFlushes)
                {
                    // lots of empty flushes, so stop timer
                    this.PauseFlushing();
                }
            }
        }
    }
}
