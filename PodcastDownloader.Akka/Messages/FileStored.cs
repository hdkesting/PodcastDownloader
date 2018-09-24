// <copyright file="FileStored.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Messages
{
    using System;

    /// <summary>
    /// Message telling that a file is stored.
    /// </summary>
    internal sealed class FileStored
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileStored"/> class.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="pubdate">The original publish date.</param>
        public FileStored(string path, DateTimeOffset pubdate)
        {
            this.Path = path;
            this.Pubdate = pubdate;
        }

        /// <summary>
        /// Gets the path to the file.
        /// </summary>
        /// <value>
        /// The path.
        /// </value>
        public string Path { get; }

        /// <summary>
        /// Gets the publish date.
        /// </summary>
        /// <value>
        /// The pubdate.
        /// </value>
        public DateTimeOffset Pubdate { get; }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{nameof(FileStored)}: {this.Path} @ {this.Pubdate}.";
        }
    }
}