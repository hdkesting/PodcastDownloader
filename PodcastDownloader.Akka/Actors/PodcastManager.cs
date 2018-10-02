// <copyright file="PodcastManager.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Actors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Akka.Actor;
    using Newtonsoft.Json;
    using PodcastDownloader.Messages;

    /// <summary>
    /// Actor responsible for managing the downloads of all configured feeds.
    /// </summary>
    /// <seealso cref="Akka.Actor.ReceiveActor" />
    public class PodcastManager : UntypedActor
    {
        /// <summary>
        /// The message to signal a feed is done.
        /// </summary>
        public const string FeedIsDoneMessage = "FeedIsDone";

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
                Console.WriteLine("Loading existing config");
                var json = File.ReadAllText(this.configFile);
                this.currentConfig = JsonConvert.DeserializeObject<FeedConfig>(json);
            }
            else
            {
                // create one!
                Console.WriteLine("Creating fresh config");
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
#if DEBUG
            Console.WriteLine($"{nameof(PodcastManager)}: received message '{message}'.");
#endif

            switch (message)
            {
                case null:
                    break;

                case ConfigurationLoadedMessage:
                    this.ProcessConfiguration();
                    Console.WriteLine("Configuration is processed, child actors are started.");
                    break;

                case ShowProgressMessage spm:
                    Console.WriteLine($"{spm.FeedName}: {spm.FileName} ({spm.BytesRead:N0}), '{spm.Message}'");
                    break;

                // find the correct config entry, update its LatestDownload (if needed) and save
                case FileStored stored:
                    var entry = this.currentConfig.Feeds.FirstOrDefault(f => Support.Cleanup.MakeActorName(f.Name) == Support.Cleanup.MakeActorName(stored.FeedName));
                    if (entry != null && entry.LatestDownload < stored.Pubdate)
                    {
                        entry.LatestDownload = stored.Pubdate;
                        this.SaveCurrentConfig();
                    }

                    break;

                case FeedIsDoneMessage:
                    this.feedReaders.RemoveAll(actor => actor.Path == Context.Sender.Path);
                    Context.Stop(Context.Sender);
                    Console.WriteLine($"#feeds left == {this.feedReaders.Count}: {string.Join(", ", this.feedReaders.Select(a => a.Path.Name))}.");

                    if (this.feedReaders.Count == 0)
                    {
                        // exit
                        Console.WriteLine("terminating the ActorSystem ...");
                        Context.System.Terminate();
                    }

                    break;
            }
        }

        private void ProcessConfiguration()
        {
            // for each show in config, start (and activate) an actor
            var delay = TimeSpan.FromSeconds(2);
            foreach (var feed in this.currentConfig.Feeds.Where(f => !f.Disabled))
            {
                var feedname = Support.Cleanup.MakeActorName(feed.Name);
                var actor = Context.ActorOf<FeedDownloader>(feedname);
                this.feedReaders.Add(actor);

                Context.System.Scheduler.ScheduleTellOnce(
                    delay,
                    actor,
                    new FeedConfiguration(feed, this.currentConfig.BasePath),
                    this.Self);

                delay = delay.Add(TimeSpan.FromSeconds(4));
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
