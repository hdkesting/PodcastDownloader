// <copyright file="LogMessage.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Logging
{
    using System;

    /// <summary>
    /// A message to write to the log.
    /// </summary>
    internal class LogMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogMessage"/> class.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="pageName">Name of the page.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception that occurred.</param>
        public LogMessage(LogLevel level, string pageName, string message = null, Exception exception = null)
        {
            this.TimeStamp = DateTime.Now;
            this.Level = level;
            this.PageName = pageName;
            this.Message = message;
            this.Exception = exception;
        }

        /// <summary>
        /// Gets the time stamp.
        /// </summary>
        /// <value>
        /// The time stamp.
        /// </value>
        public DateTime TimeStamp { get; }

        /// <summary>
        /// Gets the level of the message.
        /// </summary>
        /// <value>
        /// The level.
        /// </value>
        public LogLevel Level { get; }

        /// <summary>
        /// Gets the name of the page that originated this message.
        /// </summary>
        /// <value>
        /// The name of the page.
        /// </value>
        public string PageName { get; }

        /// <summary>
        /// Gets the message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        public string Message { get; }

        /// <summary>
        /// Gets the exception.
        /// </summary>
        /// <value>
        /// The exception.
        /// </value>
        public Exception Exception { get; }

        /// <summary>
        /// Converts this <see cref="LogMessage"/> to a string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            string lvl;
            switch (this.Level)
            {
                case LogLevel.Debug: lvl = "DBG"; break;
                case LogLevel.Information: lvl = "INF"; break;
                case LogLevel.Warning: lvl = "WRN"; break;
                case LogLevel.Error: lvl = "ERR"; break;
                default: lvl = this.Level.ToString(); break;
            }

            return $"{this.TimeStamp:HH:mm:ss} [{lvl}] {this.PageName} - {this.Message}" + DumpException(this.Exception);
        }

        private static string DumpException(Exception exception)
        {
            if (exception == null)
            {
                return string.Empty;
            }

            return Environment.NewLine
                + new string('-', 20) + Environment.NewLine
                + exception.ToString() + DumpException(exception.InnerException);
        }
    }
}
