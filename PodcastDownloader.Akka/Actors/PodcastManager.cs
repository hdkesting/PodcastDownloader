// <copyright file="PodcastManager.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Actors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Akka.Actor;
    using Newtonsoft.Json;

    /// <summary>
    /// Actor responsible for managing the downloads of all configured feeds.
    /// </summary>
    /// <seealso cref="Akka.Actor.ReceiveActor" />
    public class PodcastManager : UntypedActor
    {
        private const string ConfigurationLoadedMessage = "ConfigurationLoaded";

        private readonly string configFile;
        private readonly List<IActorRef> feedReaders = new List<IActorRef>();
        private FeedConfig currentConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="PodcastManager"/> class.
        /// </summary>
        /// <param name="configFile">The path to the configuration file.</param>
        public PodcastManager(string configFile)
        {
            this.configFile = configFile;
        }

        /// <summary>
        /// User overridable callback.
        /// <p />
        /// Is called when an Actor is started.
        /// Actors are automatically started asynchronously when created.
        /// Empty default implementation.
        /// </summary>
        protected override void PreStart()
        {
            // start reading the config file
            if (File.Exists(this.configFile))
            {
                var json = File.ReadAllText(this.configFile);
                this.currentConfig = JsonConvert.DeserializeObject<FeedConfig>(json);
            }
            else
            {
                // create one!
                this.currentConfig = new FeedConfig();
                this.InitializeConfig(this.currentConfig);
                this.SaveCurrentConfig();
            }

            this.Self.Tell(ConfigurationLoadedMessage);
        }

        /// <summary>
        /// This defines the behavior of the UntypedActor.
        /// This method is called for every message received by the actor.
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case null:
                    break;

                case ConfigurationLoadedMessage:
                    this.ProcessConfiguration();
                    break;
            }
        }

        private void ProcessConfiguration()
        {
            // for each show in config, start (and activate) an actor
            foreach (var feed in this.currentConfig.Feeds.Where(f => !f.Disabled))
            {
                var actor = Context.ActorOf<FeedDownloader>(feed.Name);
                this.feedReaders.Add(actor);
                actor.Tell(new Messages.FeedConfiguration(feed, this.currentConfig.BasePath), this.Self);
            }
        }

        private void SaveCurrentConfig()
        {
            if (this.configFile == null)
            {
                throw new InvalidOperationException("No current config to save!");
            }

            var json = JsonConvert.SerializeObject(this.currentConfig, Formatting.Indented);
            File.WriteAllText(this.configFile, json);
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
