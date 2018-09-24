// <copyright file="Cleanup.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Support
{
    using System.Text.RegularExpressions;

    /// <summary>
    /// Cleanup functions.
    /// </summary>
    public static class Cleanup
    {
        /// <summary>
        /// Makes a valid name for an actor.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>A valid name.</returns>
        public static string MakeActorName(string name)
        {
            // Illegal actor name [.NET Rocks!]. Actor paths MUST: not start with `$`,
            // include only ASCII letters and can only contain these special characters: $"-_.*$+:@&=,!~';"().
            return Regex.Replace(name, "[^a-zA-Z0-9.+@~-]", string.Empty);
        }
    }
}
