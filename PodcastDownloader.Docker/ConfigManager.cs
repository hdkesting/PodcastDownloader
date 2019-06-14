// <copyright file="ConfigManager.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader
{
    using System;
    using System.IO;
    using Newtonsoft.Json;

    /// <summary>
    /// Configuration manager.
    /// </summary>
    public class ConfigManager
    {
        private const string ConfigName = "FeedConfig.json";
        private static readonly object SaveLock = new object();
        private readonly string configPath;
        private readonly DirectoryInfo basePath;

        private FeedConfig currentConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigManager"/> class.
        /// </summary>
        /// <param name="basePath">The base path.</param>
        public ConfigManager(DirectoryInfo basePath)
        {
            this.basePath = basePath ?? throw new ArgumentException("A basepath is required.", nameof(basePath));
            if (!basePath.Exists)
            {
                basePath.Create();
            }

            this.configPath = Path.Combine(this.basePath.FullName, ConfigName);
        }

        /// <summary>
        /// Gets the current configuration.
        /// </summary>
        /// <returns>A feed configuration.</returns>
        public FeedConfig GetCurrentConfig()
        {
            // already loaded one?
            if (this.currentConfig == null)
            {
                // does the file exist?
                if (File.Exists(this.configPath))
                {
                    var json = File.ReadAllText(this.configPath);
                    this.currentConfig = JsonConvert.DeserializeObject<FeedConfig>(json);
                }
                else
                {
                    // create one!
                    this.currentConfig = new FeedConfig();
                    this.InitializeConfig(this.currentConfig);
                    this.SaveCurrentConfig();
                }
            }

            return this.currentConfig;
        }

        /// <summary>
        /// Saves the current configuration.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">No current config to save.</exception>
        public void SaveCurrentConfig()
        {
            if (this.currentConfig == null)
            {
                throw new InvalidOperationException("No current config to save!");
            }

            lock (SaveLock)
            {
                var json = JsonConvert.SerializeObject(this.currentConfig, Formatting.Indented);
                File.WriteAllText(this.configPath, json);
            }
        }

        private void InitializeConfig(FeedConfig config)
        {
            config.Feeds.Add(new FeedDefinition
            {
                Name = "dotnetrocks",
                LatestDownload = DateTime.Today.AddDays(-7),
                Url = "http://www.pwop.com/feed.aspx?show=dotnetrocks&filetype=master",
            });
        }
    }
}
