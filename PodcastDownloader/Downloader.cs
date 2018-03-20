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

            this.logger = File.AppendText(logpath);
            this.logger.WriteLine(new string('=', 20) + " " + DateTime.Now.ToString(CultureInfo.CurrentCulture));
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
                        this.logger.WriteLine($"No podcast found at {this.feed.Url}.");
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
                    this.logger.WriteLine($"Error loading podcast {this.feed.Name}.");
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
                    this.feed.LatestDownload = latest;
                    this.feed.LatestError = String.Empty;
                }
            }
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
                file = this.feed.Name + " — " + file;
            }

            file = CleanupFilename(file);

            var path = Path.Combine(folder, file);
            var fi = new FileInfo(path);

            // a) bestaat nog niet -- prima, schrijven
            // b) bestaat al met ~ zelfde tijd -- al gedaan, overslaan
            // c) bestaat met andere tijd -- schrijf onder nieuwe naam

            if (fi.Exists)
            {
                if (Math.Abs((pubdate - fi.CreationTimeUtc).TotalHours) < 1.0)
                {
                    Console.WriteLine($"File already downloaded: {file}, skipping.");
                }
                else
                {
                    var ext = Path.GetExtension(file);
                    file = Path.GetFileNameWithoutExtension(file);
                    file = file + pubdate.ToString("-yyyy-MM-dd") + ext;
                    path = Path.Combine(folder, file);

                    Console.WriteLine($"{this.feed.Name}: {file}");
                    DownloadFileToLocal(linkUri, path, pubdate);
                }
            }
            else
            {
                Console.WriteLine($"{this.feed.Name}: {file}");
                DownloadFileToLocal(linkUri, path, pubdate);
            }
        }

        private void DownloadFileToLocal(Uri uri, string path, DateTimeOffset pubdate)
        {
            try
            {
                var request = WebRequest.Create(uri);
                using (var response = request.GetResponse())
                {
                    using (var wrt = File.OpenWrite(path))
                    {
                        response.GetResponseStream()?.CopyTo(wrt);
                    }

#pragma warning disable IDE0017 // Simplify object initialization
                    var fi = new FileInfo(path);
#pragma warning restore IDE0017 // Simplify object initialization
                    fi.CreationTimeUtc = pubdate.UtcDateTime;

                    this.logger.WriteLine($"Downloaded file {path}.");
                }

            }
            catch (Exception)
            {
                // remove possibly partially downloaded file
                File.Delete(path);
                throw;
            }

        }

        private string CleanupFilename(string file)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var newname = new string(file.Where(c => !invalid.Contains(c)).ToArray());
            if (newname.StartsWith("."))
            {
                newname = newname.TrimStart('.');
            }

            if (newname != file && this.logger != null)
            {
                this.logger.WriteLine($"Changing '{file}' into '{newname}'.");
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
                this.logger.WriteLine($"Fixing feed {this.feed.Name} didn't work.");
                WriteException(ex);
                return null;
            }
        }

        private void WriteException(Exception ex)
        {
            while (ex != null)
            {
                this.logger.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    this.logger.WriteLine(new String('-', 20));
                }

                ex = ex.InnerException;
            }
        }

        public void Dispose()
        {
            this.logger.WriteLine(DateTime.Now.ToString(CultureInfo.CurrentCulture) + " " + new string('=', 20));
            this.logger.Flush();
            this.logger.Close();
            this.logger.Dispose();
        }
    }
}
