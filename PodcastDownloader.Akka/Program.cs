// <copyright file="Program.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader
{
    using System;
    using System.IO;
    using Akka.Actor;

    /// <summary>
    /// The main entry point of this console application.
    /// </summary>
    internal class Program
    {
        private const string ConfigName = "FeedConfig.json";

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        private static void Main(string[] args)
        {
            Console.WriteLine("Starting system");
            using (var system = ActorSystem.Create("download-system"))
            {
                string configFile = Path.Combine(
                            System.Configuration.ConfigurationManager.AppSettings["BasePath"],
                            ConfigName);
                var feedDownloadActor = system.ActorOf(Props.Create(() => new Actors.PodcastManager(configFile)));

                // TODO wait for system to finish, then automatically exit
                Console.WriteLine("Waiting for system to finish or <enter>.");
                Console.ReadLine();
            }
        }
    }
}
