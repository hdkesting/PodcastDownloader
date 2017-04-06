using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace PodcastDownloader
{
    internal sealed class Downloader : IDisposable
    {
        private readonly FeedDefinition feed;
        private readonly string baseDownloadPath;
        private readonly bool useSeparateFeedFolder;
        private readonly TextWriter logger;

        public Downloader(FeedDefinition feed)
        {
            this.feed = feed ?? throw new ArgumentNullException(nameof(feed));

            this.baseDownloadPath = ConfigManager.Instance.GetCurrentConfig().BasePath;
            this.useSeparateFeedFolder = ConfigManager.Instance.GetCurrentConfig().UseSeparateFolders;

            var logpath = Path.Combine(this.baseDownloadPath, "__Logging");
            EnsureFolderExists(logpath);

            logpath = Path.Combine(logpath, CleanupFilename(feed.Name) + ".txt");

            logger = File.AppendText(logpath);
            logger.WriteLine(new string('=', 20) + " " + DateTime.Now.ToString(CultureInfo.CurrentCulture));
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
                        logger.WriteLine($"No podcast found at {this.feed.Url}.");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                // might happen because of url encoding in url
                //     <atom:link href="http://www.pwop.com%2ffeed.aspx%3fshow%3dHanselminutes" rel="self" type="application/rss+xml" />
                try
                {
                    podcast = TryDecodingUrl(this.feed.Url);
                }
                catch (Exception ex2)
                {
                    this.feed.LatestError = ex2.Message;
                    WriteException(ex2);
                    return;
                }

                if (podcast == null)
                {
                    logger.WriteLine($"Error loading podcast {this.feed.Name}.");
                    this.feed.LatestError = ex.Message;
                    WriteException(ex);
                    return;
                }
            }

            this.feed.Name = podcast.Title.Text;

            DateTimeOffset latest = this.feed.LatestDownload;
            foreach (var item in podcast.Items.Where(it => it.PublishDate > this.feed.LatestDownload).OrderBy(it => it.PublishDate))
            {
                foreach (var link in item.Links.Where(l => l.RelationshipType == "enclosure"))
                {
                    DownloadFile(link.Uri, item.PublishDate);
                }

                if (item.PublishDate > latest)
                {
                    latest = item.PublishDate;
                }
            }

            this.feed.LatestDownload = latest;
            this.feed.LatestError = String.Empty;
        }

        private void DownloadFile(Uri linkUri, DateTimeOffset pubdate)
        {
            var folder = this.baseDownloadPath;
            if (this.useSeparateFeedFolder)
            {
                folder = Path.Combine(folder, this.feed.Name);
            }
            else
            {
                folder = Path.Combine(folder, "__Files");
            }

            EnsureFolderExists(folder);

            var file = linkUri.Segments.Last();
            if (!this.useSeparateFeedFolder)
            {
                // not a separate folder, so prefix with feed name
                file = this.feed.Name + " ◆ " + file;
            }

            file = CleanupFilename(file);

            var path = Path.Combine(folder, file);

            if (File.Exists(path))
            {
                logger.WriteLine($"File already downloaded: {file}, skipping.");
            }
            else
            {
                Console.WriteLine($"{this.feed.Name}: {file}");

                try
                {
                    var request = WebRequest.Create(linkUri);
                    using (var response = request.GetResponse())
                    {
                        using (var wrt = File.OpenWrite(path))
                        {
                            response.GetResponseStream()?.CopyTo(wrt);
                        }

                        var fi = new FileInfo(path);
                        fi.CreationTimeUtc = pubdate.UtcDateTime;

                        logger.WriteLine($"Downloaded file {path}.");
                    }

                }
                catch (Exception)
                {
                    // remove possibly partially downloaded file
                    File.Delete(path);
                    throw;
                }
            }
        }

        private string CleanupFilename(string file)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var newname = new string(file.Where(c => !invalid.Contains(c)).ToArray());
            if (newname != file && logger != null)
            {
                logger.WriteLine($"Changing '{file}' into '{newname}'.");
            }

            return newname;
        }

        private void EnsureFolderExists(string folderName)
        {
            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
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
                logger.WriteLine($"Fixing feed {this.feed.Name} didn't work.");
                WriteException(ex);
                return null;
            }
        }

        private void WriteException(Exception ex)
        {
            while (ex != null)
            {
                logger.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    logger.WriteLine(new String('-', 20));
                }

                ex = ex.InnerException;
            }
        }

        public void Dispose()
        {
            logger.WriteLine(DateTime.Now.ToString(CultureInfo.CurrentCulture) + " " + new string('=', 20));
            logger.Flush();
            logger.Close();
            logger.Dispose();
        }
    }
}
