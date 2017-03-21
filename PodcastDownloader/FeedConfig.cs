using System.Collections.Generic;

namespace PodcastDownloader
{
    public class FeedConfig
    {
        /// <summary>
        /// Gets the base path where the podcasts will be stored.
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
        /// Gets the list of feeds.
        /// </summary>
        /// <value>
        /// The feeds.
        /// </value>
        public List<FeedDefinition> Feeds { get; set; } = new List<FeedDefinition>();
    }
}
