using System;

namespace PodcastDownloader
{
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
        public DateTime LatestDownload { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore this feed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if disabled; otherwise, <c>false</c>.
        /// </value>
        public bool Disabled { get; set; }
    }
}
