// <copyright file="LoggingConfig.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Logging
{
    using System;

    /// <summary>
    /// Configuration for <see cref="Logger"/>. Set at startup.
    /// </summary>
    public class LoggingConfig
    {
        /// <summary>
        /// Gets or sets the prefix to use for log files.
        /// </summary>
        public string LogfilePrefix { get; set; } = "Log";

        /// <summary>
        /// Gets or sets the folder where logfiles should be created.
        /// </summary>
        public string LogFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        /// <summary>
        /// Gets or sets the number of logfiles to keep.
        /// </summary>
        public int FilesToKeep { get; set; } = 20;

        /// <summary>
        /// Gets or sets the minimum loglevel to really write.
        /// </summary>
        public LogLevel MinLogLevel { get; set; } = LogLevel.Debug;
    }
}
