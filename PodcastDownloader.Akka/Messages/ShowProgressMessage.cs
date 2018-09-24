// <copyright file="ShowProgressMessage.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Messages
{
    /// <summary>
    /// A message about progress of downloading one show.
    /// </summary>
    internal sealed class ShowProgressMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShowProgressMessage"/> class.
        /// </summary>
        /// <param name="feedName">Name of the feed.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="bytesRead">The number of bytes read (0=not started yet).</param>
        /// <param name="message">The message.</param>
        public ShowProgressMessage(string feedName, string fileName, long bytesRead, string message)
        {
            this.FeedName = feedName;
            this.FileName = fileName;
            this.BytesRead = bytesRead;
            this.Message = message;
        }

        /// <summary>
        /// Gets the name of the feed.
        /// </summary>
        /// <value>
        /// The name of the feed.
        /// </value>
        public string FeedName { get; }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <value>
        /// The name of the file.
        /// </value>
        public string FileName { get; }

        /// <summary>
        /// Gets the number of bytes read.
        /// </summary>
        /// <value>
        /// The bytes read.
        /// </value>
        public long BytesRead { get; }

        /// <summary>
        /// Gets the message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        public string Message { get; }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{nameof(ShowProgressMessage)} from {this.FeedName} about {this.FileName ?? "none"}: {this.Message}.";
        }
    }
}
