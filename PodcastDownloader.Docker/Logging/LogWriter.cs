// <copyright file="LogWriter.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Caches log messages and writes them to file.
    /// </summary>
    internal class LogWriter
    {
        private readonly Queue<LogMessage> messageQueue = new Queue<LogMessage>();

        private readonly string logfilePath;
        private readonly string logFolder;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogWriter" /> class.
        /// </summary>
        /// <param name="logFolder">The log folder.</param>
        public LogWriter(string logFolder)
        {
            Directory.CreateDirectory(logFolder);
            this.logfilePath = Path.Combine(logFolder, "pcdl_" + DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".log");
            this.logFolder = logFolder;
        }

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
        /// Cleans up old log files.
        /// </summary>
        public void Cleanup()
        {
            try
            {
                DateTime oldestToKeep = DateTime.Today.AddDays(-20);
                var di = new DirectoryInfo(this.logFolder);
                var files = di.GetFiles();
                foreach (var file in files.Where(fi => fi.LastWriteTime < oldestToKeep))
                {
                    Logger.Log(LogLevel.Information, nameof(LogWriter), $"Removing old log file {file}.");
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
        /// <returns><c>true</c> when messages were flushed.</returns>
        internal bool Flush()
        {
            var logcopy = new List<LogMessage>();

            lock (this.messageQueue)
            {
                while (this.messageQueue.Count > 0)
                {
                    var msg = this.messageQueue.Dequeue();
                    logcopy.Add(msg);
                }
            }

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
