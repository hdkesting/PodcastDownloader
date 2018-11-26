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
    public class Logger
    {
        private static readonly List<LogMessage> LogMessages = new List<LogMessage>();
        private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private static Task outputTask;

        private static List<ILogTarget> logTargets = new List<ILogTarget>();

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
        /// <exception cref="ArgumentNullException">target</exception>
        public static void AddTarget(ILogTarget target)
        {
            logTargets.Add(target ?? throw new ArgumentNullException(nameof(target)));
        }

        /// <summary>
        /// Starts the logging to the logfile.
        /// </summary>
        public static void StartLogging()
        {
            // task needs to run in parallel, continuously
            outputTask = Task.Run(ProcessLogQueue, cancellationTokenSource.Token);
        }

        /// <summary>
        /// Stops the logging and flushes the queue.
        /// </summary>
        /// <returns>A Task.</returns>
        public static async Task StopLogging()
        {
            try
            {
                cancellationTokenSource.Cancel();

                await outputTask;
            }
            catch (OperationCanceledException)
            {
            }

            // flush
            await ProcessBatch().ConfigureAwait(false);
        }

        private static async Task ProcessLogQueue()
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                await ProcessBatch().ConfigureAwait(false);

                // Wait before writing the next batch
                await Task.Delay(Interval, cancellationTokenSource.Token).ConfigureAwait(false);
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
                var tasks = logTargets.Select(t => t.WriteBatchAsync(batch)).ToList();
                await Task.WhenAll(tasks);
            }
        }
    }
}
