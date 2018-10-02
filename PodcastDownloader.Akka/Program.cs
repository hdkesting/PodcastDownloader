// <copyright file="Program.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Akka.Actor;

    /// <summary>
    /// The main entry point of this console application.
    /// </summary>
    internal static class Program
    {
        private const string ConfigName = "FeedConfig.json";

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>A Task.</returns>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Starting system");
            using (var system = ActorSystem.Create("download-system"))
            {
                string configFile = Path.Combine(
                            System.Configuration.ConfigurationManager.AppSettings["BasePath"],
                            ConfigName);
                var feedDownloadActor = system.ActorOf(Props.Create(() => new Actors.PodcastManager(configFile)));

                // wait for system to finish, then automatically exit
                await system.WhenTerminated;
            }

#if DEBUG
            Console.WriteLine("Press <enter>.");
            Console.ReadLine();
#endif
        }
    }
}
