// <copyright file="LoadConfiguration.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Messages
{
    using System.IO;

    /// <summary>
    /// A message to start loading the defined configutaion file (and start acting on it).
    /// </summary>
    internal sealed class LoadConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoadConfiguration"/> class.
        /// </summary>
        /// <param name="configurationFile">The configuration file.</param>
        public LoadConfiguration(FileInfo configurationFile)
        {
            this.ConfigurationFile = configurationFile;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadConfiguration"/> class.
        /// </summary>
        /// <param name="configurationFile">The configuration file.</param>
        public LoadConfiguration(string configurationFile)
        {
            this.ConfigurationFile = new FileInfo(configurationFile);
        }

        /// <summary>
        /// Gets the configuration file that defines the feeds to download.
        /// </summary>
        /// <value>
        /// The configuration file.
        /// </value>
        public FileInfo ConfigurationFile { get; }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{nameof(LoadConfiguration)} from {this.ConfigurationFile.Name}.";
        }
    }
}
