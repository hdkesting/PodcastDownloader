﻿// <copyright file="Program.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using PodcastDownloader.Logging;

    /// <summary>
    /// The main application entry point.
    /// </summary>
    public static class Program
    {
        // docker must be configured to mount an external folder on this path.
        private static readonly DirectoryInfo LocalPath = new DirectoryInfo("/feeds");

        /// <summary>
        /// The main application entry point.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>A Task.</returns>
        public static async Task Main(string[] args)
        {
            var logconfig = new Logging.LoggingConfig
            {
                LogFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Logs"),
                LogfilePrefix = "PCDL",
                FilesToKeep = 20,
                MinLogLevel = Logging.LogLevel.Debug,
            };

            LoggerSingleton.Initialize(logconfig);

#if !DEBUG
            logconfig.MinLogLevel = LogLevel.Information;
#endif
            while (true)
            {
                LoggerSingleton.Value.Log(LogLevel.Information, "Main", "Version " + typeof(Program).Assembly.GetName().Version.ToString());

                await ProcessConfig(LocalPath);

                LoggerSingleton.Value.Cleanup();

#if DEBUG
                break;
#else
                await Task.Delay(TimeSpan.FromHours(12));
#endif
            }
        }

        private static async Task ProcessConfig(DirectoryInfo basePath)
        {
            // load config
            var configMgr = new ConfigManager(basePath);
            var config = configMgr.GetCurrentConfig();

            try
            {
                // process all feeds
                foreach (var feed in config.Feeds.Where(f => !f.Disabled))
                {
                    await ProcessFeed(feed, basePath);
                    configMgr.SaveCurrentConfig();
                }
            }
            catch (Exception ex)
            {
                LoggerSingleton.Value.Log(LogLevel.Error, nameof(Program), nameof(ProcessConfig), ex);
            }
            finally
            {
                configMgr.SaveCurrentConfig();
            }
        }

        private static async Task<bool> ProcessFeed(FeedDefinition feed, DirectoryInfo basePath)
        {
            try
            {
                LoggerSingleton.Value.Log(LogLevel.Information, nameof(Program), $">> Starting on feed '{feed.Name}' from {feed.LatestDownload:yyyy-MM-dd}.");
                using (var dl = new Downloader(feed, basePath))
                {
                    await dl.Process();
                }

                LoggerSingleton.Value.Log(LogLevel.Information, nameof(Program), $"<< Finished feed {feed.Name}. Up to date until {feed.LatestDownload:yyyy-MM-dd}.");

                return true;
            }
            catch (Exception ex)
            {
                feed.LatestError = ex.Message;

                LoggerSingleton.Value.Log(LogLevel.Error, nameof(Program), nameof(ProcessFeed), ex);
            }

            return false;
        }
    }
}
