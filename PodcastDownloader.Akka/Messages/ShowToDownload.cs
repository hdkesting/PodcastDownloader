// <copyright file="ShowToDownload.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Messages
{
    using System;

    /// <summary>
    /// Message specifying the show to download.
    /// </summary>
    public class ShowToDownload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShowToDownload" /> class.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="publishDate">The publish date.</param>
        /// <param name="targetFolder">The target folder.</param>
        /// <param name="feedname">The feed's name.</param>
        public ShowToDownload(Uri uri, DateTimeOffset publishDate, string targetFolder, string feedname)
        {
            this.Uri = uri;
            this.PublishDate = publishDate;
            this.TargetFolder = targetFolder;
            this.Feedname = feedname;
        }

        /// <summary>
        /// Gets the source URI.
        /// </summary>
        /// <value>
        /// The URI.
        /// </value>
        public Uri Uri { get; }

        /// <summary>
        /// Gets the publish date.
        /// </summary>
        /// <value>
        /// The publish date.
        /// </value>
        public DateTimeOffset PublishDate { get; }

        /// <summary>
        /// Gets the target folder.
        /// </summary>
        /// <value>
        /// The target folder.
        /// </value>
        public string TargetFolder { get; }

        /// <summary>
        /// Gets the feed's name.
        /// </summary>
        /// <value>
        /// The feedname.
        /// </value>
        public string Feedname { get; }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{nameof(ShowToDownload)} from {this.Uri} to {this.TargetFolder} for {this.Feedname}.";
        }
    }
}
