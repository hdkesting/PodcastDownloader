﻿using System;
using System.Linq;
using System.Threading.Tasks;

namespace PodcastDownloader
{
    public static class Program
    {
        static void Main(string[] args)
        {
            bool errors = false;
            try
            {
                // load config
                var config = ConfigManager.Instance.GetCurrentConfig();

                // process feeds
                // Parallel.ForEach(config.Feeds.Where(f => !f.Disabled), ProcessFeed);
                foreach(var feed in config.Feeds.Where(f => !f.Disabled))
                {
                    errors |= ProcessFeed(feed);
                    ConfigManager.Instance.SaveCurrentConfig();
                }

#if DEBUG
                Console.Write("Press return to exit >");
                Console.ReadLine();
#endif
            }
            catch (Exception ex)
            {
                while (ex != null)
                {
                    Console.WriteLine(new string('-', 20));
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    ex = ex.InnerException;
                }

                errors = true;
            }
            finally
            {
                ConfigManager.Instance.SaveCurrentConfig();
            }

            if (errors)
            {
                Console.Write("Press return to exit >");
                Console.ReadLine();
            }
        }

        private static bool ProcessFeed(FeedDefinition feed)
        {
            try
            {
                using (var dl = new Downloader(feed))
                {
                    dl.Process();
                }

                return true;

            }
            catch (Exception ex)
            {
                while (ex != null)
                {
                    Console.WriteLine(new string('-', 20));
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    ex = ex.InnerException;
                }
            }

            return false;
        }
    }
}
