// <copyright file="Program.cs" company="Hans Kesting">
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
        private static readonly DirectoryInfo LocalPath = new DirectoryInfo("/feeds");

        /// <summary>
        /// The main application entry point.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>A Task.</returns>
        public static async Task Main(string[] args)
        {
            while (true)
            {
                Logger.Initialize(LocalPath);

                await ProcessConfig(LocalPath);

                await Task.Delay(TimeSpan.FromHours(12));
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
                Logger.Log(LogLevel.Error, nameof(Program), nameof(ProcessConfig), ex);
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
                using (var dl = new Downloader(feed, basePath))
                {
                    await dl.Process();
                }

                return true;
            }
            catch (Exception ex)
            {
                feed.LatestError = ex.Message;

                Logger.Log(LogLevel.Error, nameof(Program), nameof(ProcessFeed), ex);
            }

            return false;
        }
    }
}
