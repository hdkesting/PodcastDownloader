// <copyright file="FeedConfiguration.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Messages
{
    using System;

    /// <summary>
    /// Message specifying the configuration of the feed to process.
    /// </summary>
    internal class FeedConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FeedConfiguration" /> class.
        /// </summary>
        /// <param name="feed">The feed.</param>
        /// <param name="targetFolder">The target folder.</param>
        public FeedConfiguration(FeedDefinition feed, string targetFolder)
        {
            this.Name = feed.Name;
            this.LatestDownload = feed.LatestDownload;
            this.Url = feed.Url;
            this.TargetFolder = targetFolder;
        }

        /// <summary>
        /// Gets the name of the feed.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Gets the date/time of the latest download.
        /// </summary>
        /// <value>
        /// The latest download.
        /// </value>
        public DateTimeOffset LatestDownload { get; }

        /// <summary>
        /// Gets the URL of the rss/atom feed.
        /// </summary>
        /// <value>
        /// The URL.
        /// </value>
        public string Url { get; }

        /// <summary>
        /// Gets the target folder.
        /// </summary>
        /// <value>
        /// The target folder.
        /// </value>
        public string TargetFolder { get; }
    }
}