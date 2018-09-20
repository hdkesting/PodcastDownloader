// <copyright file="ShowResponse.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Messages
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// Response to a request for the show data.
    /// </summary>
    internal class ShowResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShowResponse"/> class.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="path">The path.</param>
        /// <param name="pubdate">The pubdate.</param>
        public ShowResponse(Task<WebResponse> response, string path, DateTimeOffset pubdate)
        {
            this.Response = response;
            this.Path = path;
            this.Pubdate = pubdate;
        }

        /// <summary>
        /// Gets the response.
        /// </summary>
        /// <value>
        /// The response.
        /// </value>
        public Task<WebResponse> Response { get; }

        /// <summary>
        /// Gets the path.
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
    }
}