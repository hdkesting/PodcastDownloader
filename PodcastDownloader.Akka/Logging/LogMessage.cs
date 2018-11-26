// <copyright file="LogMessage.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Logging
{
    using System;
    using System.Globalization;

    /// <summary>
    /// A message to log.
    /// </summary>
    public class LogMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogMessage" /> class.
        /// </summary>
        /// <param name="severity">The severity.</param>
        /// <param name="category">The category.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <exception cref="ArgumentException">message should not be empty.</exception>
        public LogMessage(LogSeverity severity, string category, string message, Exception exception = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("message", nameof(message));
            }

            this.Severity = severity;
            this.Category = category;
            this.Message = message;
            this.Exception = exception;
            this.LogTime = DateTimeOffset.Now;
        }

        /// <summary>
        /// Gets the severity.
        /// </summary>
        /// <value>
        /// The severity.
        /// </value>
        public LogSeverity Severity { get; }

        /// <summary>
        /// Gets the log time.
        /// </summary>
        /// <value>
        /// The log time.
        /// </value>
        public DateTimeOffset LogTime { get; }

        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>
        /// The category.
        /// </value>
        public string Category { get; }

        /// <summary>
        /// Gets the message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        public string Message { get; }

        /// <summary>
        /// Gets the exception (if any).
        /// </summary>
        /// <value>
        /// The exception.
        /// </value>
        public Exception Exception { get; }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var msg = $"{this.LogTime.ToString("HH:mm:ss.ff", CultureInfo.CurrentCulture)} {ToCode(this.Severity)} ({this.Category}): {this.Message}";
            if (this.Exception is null)
            {
                return msg;
            }
            else
            {
                return msg + ExceptionMessage(this.Exception);
            }

            string ExceptionMessage(Exception ex)
            {
                if (ex is null)
                {
                    return string.Empty;
                }

                return Environment.NewLine + new string('-', 10) + Environment.NewLine + ex.ToString()
                    + ExceptionMessage(ex.InnerException);
            }
        }

        private static string ToCode(LogSeverity severity)
        {
            switch (severity)
            {
                case LogSeverity.Debug: return "DBUG";
                case LogSeverity.Information: return "INFO";
                case LogSeverity.Warning: return "WARN";
                case LogSeverity.Error: return "ERR";
                case LogSeverity.Failure: return "FAIL";
                default: throw new InvalidOperationException("Unknown log severity: " + severity);
            }
        }
    }
}
