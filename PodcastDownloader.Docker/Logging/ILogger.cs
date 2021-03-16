// <copyright file="ILogger.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Logging
{
    using System;

    /// <summary>
    /// Interface for logger component.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Gets the path to the current log file.
        /// </summary>
        string CurrentLogfilePath { get; }

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="level">The level of the message.</param>
        /// <param name="pageName">Name of the page or class.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception (optional).</param>
        void Log(LogLevel level, string pageName, string message, Exception exception = null);

        /// <summary>
        /// Cleans up the old logs.
        /// </summary>
        void Cleanup();
    }
}
