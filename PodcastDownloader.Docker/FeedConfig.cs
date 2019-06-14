// <copyright file="FeedConfig.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader
{
    using System.Collections.Generic;

    /// <summary>
    /// Configuration of feeds.
    /// </summary>
    public class FeedConfig
    {
        /// <summary>
        /// Gets or sets the list of feeds.
        /// </summary>
        /// <value>
        /// The feeds.
        /// </value>
        public List<FeedDefinition> Feeds { get; set; } = new List<FeedDefinition>();
    }
}
