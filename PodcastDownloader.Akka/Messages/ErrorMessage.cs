// <copyright file="ErrorMessage.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Messages
{
    using System;

    /// <summary>
    /// The error message.
    /// </summary>
    internal sealed class ErrorMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorMessage"/> class.
        /// </summary>
        /// <param name="ex">The ex.</param>
        public ErrorMessage(Exception ex)
        {
            this.Exception = ex;
        }

        /// <summary>
        /// Gets the exception.
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
            return "Exception: " + (this.Exception?.ToString() ?? "(null)");
        }
    }
}
