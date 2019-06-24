// <copyright file="Program.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Akka.Actor;
    using PodcastDownloader.Logging;

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

            Logger.AddTarget(new FileLogger(
                new DirectoryInfo(Path.Combine(
                    System.Configuration.ConfigurationManager.AppSettings["BasePath"],
                    "Log"))));
            Logger.AddTarget(new FancyConsoleLogger());
            Logger.StartLogging();

            using (var system = ActorSystem.Create("download-system"))
            {
                string configFile = Path.Combine(
                            System.Configuration.ConfigurationManager.AppSettings["BasePath"],
                            ConfigName);
                _ = system.ActorOf(Props.Create(() => new Actors.PodcastManager(configFile)));

                // wait for system to finish, then automatically exit
                await system.WhenTerminated;
            }

            await Logger.StopLogging();

            Countdown(7);

#if DEBUG
            Console.WriteLine("Press <enter>.");
            Console.ReadLine();
#endif
        }

        private static void Countdown(int count)
        {
            for (int i = count; i >= 0; i--)
            {
                Console.Write(new string('*', i) + " \r");
            }
        }
    }
}
