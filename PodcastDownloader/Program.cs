using System;
using System.Linq;
using System.Threading.Tasks;

namespace PodcastDownloader
{
    public static class Program
    {
        static void Main(string[] args)
        {
            try
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
            catch (Exception ex)
            {
                while (ex != null)
                {
                    Console.WriteLine(new string('-', 20));
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    ex = ex.InnerException;
                }

                Console.Write("Press return to exit >");
                Console.ReadLine();
            }
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
