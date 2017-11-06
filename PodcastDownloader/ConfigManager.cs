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
            var basePath = System.Configuration.ConfigurationManager.AppSettings["BasePath"];
            if (string.IsNullOrWhiteSpace(basePath))
            {
                basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "Feeds");
                System.Configuration.ConfigurationManager.AppSettings["BasePath"] = basePath;
            }

            this.configPath = Path.Combine(basePath, ConfigName);
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
                var json = File.ReadAllText(this.configPath);
                this.currentConfig = JsonConvert.DeserializeObject<FeedConfig>(json);
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
                var json = JsonConvert.SerializeObject(this.currentConfig, Formatting.Indented);
                File.WriteAllText(this.configPath, json);
            }
        }

        private void InitializeConfig(FeedConfig config)
        {
            config.BasePath = System.Configuration.ConfigurationManager.AppSettings["BasePath"];
            config.Feeds.Add(new FeedDefinition
            {
                Name = "dotnetrocks",
                LatestDownload = DateTime.Today.AddDays(-7),
                Url = "http://www.pwop.com/feed.aspx?show=dotnetrocks&filetype=master"
            });
        }

    }
}
