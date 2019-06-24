// <copyright file="Logger.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The logger (logging to console + a file).
    /// </summary>
    public static class Logger
    {
        private static readonly List<LogMessage> LogMessages = new List<LogMessage>();
        private static readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        private static readonly List<ILogTarget> LogTargets = new List<ILogTarget>();

        private static Task outputTask;

        private static TimeSpan Interval { get; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Adds the specified message to the log.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void Log(LogMessage message)
        {
            lock (LogMessages)
            {
                LogMessages.Add(message);
            }
        }

        /// <summary>
        /// Adds the specified message to the log.
        /// </summary>
        /// <param name="severity">The severity of the message.</param>
        /// <param name="category">The category.</param>
        /// <param name="message">The message text.</param>
        /// <param name="ex">The exception (if any).</param>
        public static void Log(LogSeverity severity, string category, string message, Exception ex = null)
        {
            Log(new LogMessage(severity, category, message, ex));
        }

        /// <summary>
        /// Adds the log target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <exception cref="ArgumentNullException">target cannot be null.</exception>
        public static void AddTarget(ILogTarget target)
        {
            LogTargets.Add(target ?? throw new ArgumentNullException(nameof(target)));
        }

        /// <summary>
        /// Starts the logging to the logfile.
        /// </summary>
        public static void StartLogging()
        {
            // task needs to run in parallel, continuously
            outputTask = Task.Run(ProcessLogQueue, CancellationTokenSource.Token);
        }

        /// <summary>
        /// Stops the logging and flushes the queue.
        /// </summary>
        /// <returns>A Task.</returns>
        public static async Task StopLogging()
        {
            try
            {
                CancellationTokenSource.Cancel();

                await outputTask;
            }
            catch (OperationCanceledException)
            {
                // ignore
            }

            // flush
            await ProcessBatch().ConfigureAwait(false);
        }

        private static async Task ProcessLogQueue()
        {
            while (!CancellationTokenSource.IsCancellationRequested)
            {
                await ProcessBatch().ConfigureAwait(false);

                // Wait before writing the next batch
                await Task.Delay(Interval, CancellationTokenSource.Token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Processes one batch of messages.
        /// </summary>
        /// <returns>A Task.</returns>
        private static async Task ProcessBatch()
        {
            var batch = new List<LogMessage>();

            lock (LogMessages)
            {
                batch.AddRange(LogMessages);
                LogMessages.Clear();
            }

            // Write the current batch out
            if (batch.Any())
            {
                var tasks = LogTargets.Select(t => t.WriteBatchAsync(batch)).ToList();
                await Task.WhenAll(tasks);
            }
        }
    }
}
