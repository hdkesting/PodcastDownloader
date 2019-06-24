// <copyright file="LogSeverity.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Logging
{
    /// <summary>
    /// Severity of the log message.
    /// </summary>
    public enum LogSeverity
    {
        /// <summary>
        /// The debug message.
        /// </summary>
        Debug,

        /// <summary>
        /// The informational message.
        /// </summary>
        Information,

        /// <summary>
        /// The warning message.
        /// </summary>
        Warning,

        /// <summary>
        /// The error message.
        /// </summary>
        Error,

        /// <summary>
        /// The failure message (exception occurred).
        /// </summary>
        Failure,
    }
}
