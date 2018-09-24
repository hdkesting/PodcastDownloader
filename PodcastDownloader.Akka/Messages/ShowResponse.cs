// <copyright file="ShowResponse.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Messages
{
    using System;
    using System.Net;

    /// <summary>
    /// Response to a request for the show data.
    /// </summary>
    internal sealed class ShowResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShowResponse"/> class.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="path">The path.</param>
        /// <param name="pubdate">The pubdate.</param>
        public ShowResponse(WebResponse response, string path, DateTimeOffset pubdate)
        {
            this.Response = response;
            this.Path = path;
            this.Pubdate = pubdate;
        }

        /// <summary>
        /// Gets the response from the file request.
        /// </summary>
        /// <value>
        /// The response.
        /// </value>
        public WebResponse Response { get; }

        /// <summary>
        /// Gets the path to store it.
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
            return $"{nameof(ShowResponse)} from {this.Path}.";
        }
    }
}