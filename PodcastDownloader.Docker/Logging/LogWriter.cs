// <copyright file="LogWriter.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Logging
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Caches log messages and writes them to file.
    /// </summary>
    internal class LogWriter
    {
        private readonly Queue<LogMessage> messageQueue = new Queue<LogMessage>();

        private readonly int filesToKeep = 20;
        private readonly string logfilePath;
        private readonly string logFolder;
        private readonly string filePrefix;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogWriter" /> class.
        /// </summary>
        /// <param name="config">The logger configuration.</param>
        public LogWriter(LoggingConfig config)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            this.filePrefix = config.LogfilePrefix;
            this.logFolder = config.LogFolder;
            Directory.CreateDirectory(this.logFolder);
            this.logfilePath = Path.Combine(this.logFolder, $"{this.filePrefix}_{DateTime.Today:yyyy-MM-dd}.log.txt");
        }

        /// <summary>
        /// Gets the path to the current logfile.
        /// </summary>
        public string CurrentLogfile => this.logfilePath;

        /// <summary>
        /// Adds the specified message to the queue.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Add(LogMessage message)
        {
            lock (this.messageQueue)
            {
                this.messageQueue.Enqueue(message);
            }
        }

        /// <summary>
        /// Cleans up old log files, keeping a number of recents ones.
        /// </summary>
        public void Cleanup()
        {
            try
            {
                var di = new DirectoryInfo(this.logFolder);
                var files = di.GetFiles();
                foreach (var file in files.OrderByDescending(f => f.LastWriteTimeUtc).Skip(this.filesToKeep))
                {
                    this.Add(new LogMessage(LogLevel.Information, nameof(LogWriter), $"Removing old log file: {file}."));
                    file.Delete();
                }
            }
            catch (IOException)
            {
                // ignore any IOExceptions
            }
        }

        /// <summary>
        /// Flushes all queued messages to the file.
        /// </summary>
        /// <returns><c>true</c> when messages were flushed, <c>false</c> when queue was empty.</returns>
        internal bool Flush()
        {
            List<LogMessage> logcopy;

            // quickly get all messages from the queue, so it can be unlocked again
            lock (this.messageQueue)
            {
                logcopy = new List<LogMessage>(this.messageQueue.Count);
                while (this.messageQueue.Count > 0)
                {
                    var msg = this.messageQueue.Dequeue();
                    logcopy.Add(msg);
                }
            }

            // and now write all
            if (logcopy.Any())
            {
                using (var sw = File.AppendText(this.logfilePath))
                {
                    foreach (var msg in logcopy)
                    {
                        sw.WriteLine(msg.ToString());
                    }
                }

                return true;
            }

            return false;
        }
    }
}
