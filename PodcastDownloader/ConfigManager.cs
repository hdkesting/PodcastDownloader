using System;
using System.IO;
using Newtonsoft.Json;

namespace PodcastDownloader
{
    public class ConfigManager
    {
        private static ConfigManager _instance;
        private static readonly object SaveLock = new object();
        private const string ConfigName = "FeedConfig.json";
        private readonly string configPath;

        private FeedConfig currentConfig;

        private ConfigManager()
        {
            configPath = Path.Combine(Path.GetDirectoryName(this.GetType().Assembly.Location), ConfigName);
        }

        public static ConfigManager Instance => _instance ?? (_instance = new ConfigManager());

        public FeedConfig GetCurrentConfig()
        {
            // already loaded one?
            if (this.currentConfig != null)
            {
                return this.currentConfig;
            }

            // does the file exist?
            if (File.Exists(this.configPath))
            {
                this.currentConfig = JsonConvert.DeserializeObject<FeedConfig>(this.configPath);
            }
            else
            {
                // create one!
                this.currentConfig = new FeedConfig();
                InitializeConfig(this.currentConfig);
                SaveCurrentConfig();
            }

            return this.currentConfig;
        }

        public void SaveCurrentConfig()
        {
            if (this.currentConfig == null)
            {
                throw new InvalidOperationException("No current config to save!");
            }

            lock (SaveLock)
            {
                var json = JsonConvert.SerializeObject(this.currentConfig);
                File.WriteAllText(this.configPath, json);
            }
        }

        private void InitializeConfig(FeedConfig config)
        {
            config.BasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "Feeds");
            config.Feeds.Add(new FeedDefinition
            {
                Name = "dotnetrocks",
                LatestDownload = DateTime.Today.AddDays(-7),
                Url = "http://www.pwop.com/feed.aspx?show=dotnetrocks&filetype=master"
            });
        }

    }
}
