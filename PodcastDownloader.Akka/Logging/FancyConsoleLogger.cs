// <copyright file="FancyConsoleLogger.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Log to console, with text colors.
    /// </summary>
    /// <seealso cref="PodcastDownloader.Logging.ILogTarget" />
    public class FancyConsoleLogger : ILogTarget
    {
        /// <summary>
        /// Writes a batch of messages.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns>
        /// A Task.
        /// </returns>
        public Task WriteBatchAsync(IEnumerable<LogMessage> messages)
        {
            var col = Console.ForegroundColor;
            foreach (var msg in messages)
            {
                switch (msg.Severity)
                {
                    case LogSeverity.Debug:
                        Console.ForegroundColor = ConsoleColor.Gray;
                        break;

                    case LogSeverity.Information:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;

                    case LogSeverity.Warning:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        break;

                    case LogSeverity.Error:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;

                    case LogSeverity.Failure:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                }

                Console.WriteLine(msg);
            }

            Console.ForegroundColor = col;

            return Task.CompletedTask;
        }
    }
}
