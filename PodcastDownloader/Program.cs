using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodcastDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            // load config
            var config = ConfigManager.Instance.GetCurrentConfig();

            // process feeds
            Parallel.ForEach(config.Feeds, ProcessFeed);
        }

        private static void ProcessFeed(FeedDefinition feed)
        {
            var dl = new Downloader(feed);
            dl.Process();
        }
    }
}
