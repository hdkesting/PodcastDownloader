using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace PodcastDownloader
{
    class Downloader
    {
        private readonly FeedDefinition feed;

        public Downloader(FeedDefinition feed)
        {
            if (feed == null) throw new ArgumentNullException(nameof(feed));

            Console.WriteLine($"Using feed {feed.Name}.");
            this.feed = feed;
        }

        public void Process()
        {
            if (string.IsNullOrWhiteSpace(this.feed.Url))
            {
                return;
            }

            SyndicationFeed podcast;
            try
            {
                using (XmlReader reader = XmlReader.Create(this.feed.Url))
                {
                    podcast = SyndicationFeed.Load(reader);

                    if (podcast == null)
                    {
                        Console.WriteLine($"No podcast found at {this.feed.Url}.");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                // TODO might happen because of url encoding in url
                //     <atom:link href="http://www.pwop.com%2ffeed.aspx%3fshow%3dHanselminutes" rel="self" type="application/rss+xml" />
                podcast = TryDecodingUrl(this.feed.Url);
                if (podcast == null)
                {
                    Console.WriteLine($"Error loading podcast {this.feed.Name}.");
                    WriteException(ex);
                    return;
                }
            }

            foreach (var item in podcast.Items.Where(it => it.PublishDate > this.feed.LatestDownload))
            {
                foreach (var link in item.Links.Where(l => l.RelationshipType == "enclosure"))
                {
                    Console.WriteLine(link.Uri);
                }
            }
        }

        private SyndicationFeed TryDecodingUrl(string feedUrl)
        {
            //     <atom:link href="http://www.pwop.com%2ffeed.aspx%3fshow%3dHanselminutes" rel="self" type="application/rss+xml" />
            // need to "decode" that href

            XDocument xml;

            var request = WebRequest.Create(this.feed.Url);
            using (var response = request.GetResponse())
            {
                xml = XDocument.Load(response.GetResponseStream());
            }

            var atom = xml.Root.GetNamespaceOfPrefix("atom");

            foreach (var linknode in xml.Root.Descendants(atom + "link"))
            {
                var hrefAttr = linknode.Attributes("href").First();
                var href = hrefAttr.Value;
                hrefAttr.Value = HttpUtility.UrlDecode(href);
            }

            try
            {
                var podcast = SyndicationFeed.Load(xml.CreateReader());
                return podcast;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fixing feed {this.feed.Name} didn't work.");
                WriteException(ex);
                return null;
            }
        }

        private void WriteException(Exception ex)
        {
            while (ex != null)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(new String('-', 20));
                ex = ex.InnerException;
            }
        }
    }
}
