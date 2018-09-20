// <copyright file="FeedDefinition.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader
{
    using System;

    /// <summary>
    /// Definition of a single feed source.
    /// </summary>
    public class FeedDefinition
    {
        /// <summary>
        /// Gets or sets the name of the feed.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the URL of the feed.
        /// </summary>
        /// <value>
        /// The URL.
        /// </value>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the time+date of the latest download.
        /// </summary>
        /// <value>
        /// The latest download.
        /// </value>
        public DateTimeOffset LatestDownload { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore this feed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if disabled; otherwise, <c>false</c>.
        /// </value>
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets the latest error message (if any).
        /// </summary>
        /// <value>
        /// The latest error.
        /// </value>
        public string LatestError { get; set; }
    }
}
