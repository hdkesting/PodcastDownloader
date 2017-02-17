using System;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace PodcastDownloader
{
    class Downloader
    {
        private readonly FeedDefinition feed;
        private readonly string basepath;

        public Downloader(FeedDefinition feed)
        {
            if (feed == null) throw new ArgumentNullException(nameof(feed));

            Console.WriteLine($"Using feed {feed.Name}.");
            this.feed = feed;
            this.basepath = ConfigManager.Instance.GetCurrentConfig().BasePath;
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
                // might happen because of url encoding in url
                //     <atom:link href="http://www.pwop.com%2ffeed.aspx%3fshow%3dHanselminutes" rel="self" type="application/rss+xml" />
                podcast = TryDecodingUrl(this.feed.Url);
                if (podcast == null)
                {
                    Console.WriteLine($"Error loading podcast {this.feed.Name}.");
                    WriteException(ex);
                    return;
                }
            }

            DateTimeOffset latest = this.feed.LatestDownload;
            foreach (var item in podcast.Items.Where(it => it.PublishDate > this.feed.LatestDownload))
            {
                foreach (var link in item.Links.Where(l => l.RelationshipType == "enclosure"))
                {
                    DownloadFile(link.Uri);
                }

                if (item.PublishDate > latest)
                {
                    latest = item.PublishDate;
                }
            }

            this.feed.LatestDownload = latest;
            ConfigManager.Instance.SaveCurrentConfig();
        }

        private void DownloadFile(Uri linkUri)
        {
            var folder = Path.Combine(this.basepath, this.feed.Name);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var file = linkUri.Segments.Last();
            var path = Path.Combine(folder, file);

            if (File.Exists(path))
            {
                Console.WriteLine($"File already downloaded: {file}.");
            }
            else
            {
                var request = WebRequest.Create(linkUri);
                using (var response = request.GetResponse())
                {
                    using (var wrt = File.OpenWrite(path))
                    {
                        response.GetResponseStream().CopyTo(wrt);
                    }

                    Console.WriteLine($"Create file {path}.");
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
