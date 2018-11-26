// <copyright file="ILogTarget.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Logging
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Target to log to.
    /// </summary>
    public interface ILogTarget
    {
        /// <summary>
        /// Writes a batch of messages.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns>A Task.</returns>
        Task WriteBatchAsync(IEnumerable<LogMessage> messages);
    }
}
