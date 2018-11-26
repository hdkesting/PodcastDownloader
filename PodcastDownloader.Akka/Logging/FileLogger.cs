// <copyright file="FileLogger.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Logging
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Logs to a file.
    /// </summary>
    /// <seealso cref="PodcastDownloader.Logging.ILogTarget" />
    public class FileLogger : ILogTarget
    {
        private readonly FileInfo logFile;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLogger"/> class.
        /// </summary>
        /// <param name="logFolder">The log folder.</param>
        public FileLogger(DirectoryInfo logFolder)
        {
            Directory.CreateDirectory(logFolder.FullName);
            this.logFile = new FileInfo(Path.Combine(logFolder.FullName, $"{DateTime.Now.ToString("yyyyMMdd-HHmm")}.log"));
        }

        /// <summary>
        /// Writes a batch of messages.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns>
        /// A Task.
        /// </returns>
        public async Task WriteBatchAsync(IEnumerable<LogMessage> messages)
        {
            using (var sw = new StreamWriter(this.logFile.FullName, true))
            {
                foreach (var msg in messages)
                {
                    await sw.WriteLineAsync(msg.ToString());
                }
            }
        }
    }
}
