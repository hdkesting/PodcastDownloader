using System;
using System.Linq;
using System.Threading.Tasks;

namespace PodcastDownloader
{
    public static class Program
    {
        static void Main(string[] args)
        {
            // load config
            var config = ConfigManager.Instance.GetCurrentConfig();

            // process feeds
            Parallel.ForEach(config.Feeds.Where(f => !f.Disabled), ProcessFeed);

#if DEBUG
            Console.Write("Press return to exit >");
            Console.ReadLine();
#endif
        }

        private static void ProcessFeed(FeedDefinition feed)
        {
            using (var dl = new Downloader(feed))
            {
                dl.Process();
            }
        }
    }
}
