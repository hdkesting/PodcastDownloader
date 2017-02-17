using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodcastDownloader
{
    class Downloader
    {
        private FeedDefinition feed;

        public Downloader(FeedDefinition feed)
        {
            this.feed = feed;
        }

        public void Process()
        {
            // throw new NotImplementedException();
        }
    }
}
