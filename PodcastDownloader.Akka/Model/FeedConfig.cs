// <copyright file="FeedConfig.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader
{
    using System.Collections.Generic;

    /// <summary>
    /// Configutation of feeds.
    /// </summary>
    public class FeedConfig
    {
        /// <summary>
        /// Gets or sets the base path where the podcasts will be stored.
        /// </summary>
        /// <value>
        /// The base path.
        /// </value>
        public string BasePath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use feed-specific folders for downloaded files.
        /// </summary>
        /// <value>
        ///   <c>true</c> if separate folders should be used; otherwise, <c>false</c>.
        /// </value>
        public bool UseSeparateFolders { get; set; }

        /// <summary>
        /// Gets or sets the list of feeds.
        /// </summary>
        /// <value>
        /// The feeds.
        /// </value>
        public List<FeedDefinition> Feeds { get; set; } = new List<FeedDefinition>();
    }
}
