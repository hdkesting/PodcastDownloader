// <copyright file="Downloader.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;
    using Microsoft.SyndicationFeed;
    using Microsoft.SyndicationFeed.Rss;
    using PodcastDownloader.Logging;

    internal sealed class Downloader : IDisposable
    {
        private readonly FeedDefinition feed;
        private readonly string baseDownloadPath;

        public Downloader(FeedDefinition feed, DirectoryInfo basePath)
        {
            this.feed = feed ?? throw new ArgumentNullException(nameof(feed));

            this.baseDownloadPath = Path.Combine(basePath.FullName, "files");
        }

        /// <summary>
        /// Processes this feed.
        /// </summary>
        /// <returns>A Task.</returns>
        public async Task Process()
        {
            if (string.IsNullOrWhiteSpace(this.feed.Url))
            {
                return;
            }

            try
            {
                using (XmlReader reader = XmlReader.Create(this.feed.Url, new XmlReaderSettings { Async = true }))
                {
                    await this.ProcessFeed(reader);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, nameof(Downloader), nameof(this.Process), ex);

                // might happen because of url encoding in url
                //     <atom:link href="http://www.pwop.com%2ffeed.aspx%3fshow%3dHanselminutes" rel="self" type="application/rss+xml" />
                /*
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
                */
            }

            /*
            this.feed.Name = feedReader.

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
            */
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Logger.Log(LogLevel.Information, nameof(Downloader), DateTime.Now.ToString(CultureInfo.CurrentCulture) + " " + new string('=', 20));
        }

        private async Task ProcessFeed(XmlReader reader)
        {
            // https://github.com/dotnet/SyndicationFeedReaderWriter
            var feedReader = new RssFeedReader(reader);

            while (await feedReader.Read())
            {
                switch (feedReader.ElementType)
                {
                    // Read category
                    case SyndicationElementType.Category:
                        ISyndicationCategory category = await feedReader.ReadCategory();
                        break;

                    // Read Image
                    case SyndicationElementType.Image:
                        ISyndicationImage image = await feedReader.ReadImage();
                        break;

                    // Read Item
                    case SyndicationElementType.Item:
                        ISyndicationItem item = await feedReader.ReadItem();
                        // item.Links ...
                        break;

                    // Read link
                    case SyndicationElementType.Link:
                        ISyndicationLink link = await feedReader.ReadLink();
                        await this.DownloadFile(link.Uri, link.LastUpdated);
                        break;

                    // Read Person
                    case SyndicationElementType.Person:
                        ISyndicationPerson person = await feedReader.ReadPerson();
                        break;

                    // Read content
                    default:
                        ISyndicationContent content = await feedReader.ReadContent();
                        break;
                }
            }
        }

        private async Task DownloadFile(Uri linkUri, DateTimeOffset pubdate)
        {
            var folder = this.baseDownloadPath;

            this.EnsureFolderExists(folder);

            // get file name from URL and prefix with feed name
            var file = linkUri.Segments.Last();
            file = this.feed.Name + " - " + file;

            file = this.CleanupFilename(file);

            var path = Path.Combine(folder, file);
            var fi = new FileInfo(path);

            // a) bestaat nog niet -- prima, schrijven
            // b) bestaat al met ~ zelfde tijd -- al gedaan, overslaan
            // c) bestaat met andere tijd -- schrijf onder nieuwe naam

            if (fi.Exists)
            {
                if (Math.Abs((pubdate - fi.CreationTimeUtc).TotalHours) < 1.0)
                {
                    Logger.Log(LogLevel.Information, nameof(Downloader), $"File already downloaded: {file}, skipping.");
                }
                else
                {
                    var ext = Path.GetExtension(file);
                    file = Path.GetFileNameWithoutExtension(file);
                    file = file + pubdate.ToString("-yyyy-MM-dd") + ext;
                    path = Path.Combine(folder, file);

                    Logger.Log(LogLevel.Information, nameof(Downloader), $"{this.feed.Name}: {file}");
                    await this.DownloadFileToLocal(linkUri, path, pubdate);
                }
            }
            else
            {
                Logger.Log(LogLevel.Information, nameof(Downloader), $"{this.feed.Name}: {file}");
                await this.DownloadFileToLocal(linkUri, path, pubdate);
            }
        }

        private async Task DownloadFileToLocal(Uri uri, string path, DateTimeOffset pubdate)
        {
            try
            {
                var request = WebRequest.Create(uri);
                using (var response = request.GetResponse())
                {
                    using (var wrt = File.OpenWrite(path))
                    {
                        await response.GetResponseStream()?.CopyToAsync(wrt);
                    }

#pragma warning disable IDE0017 // Simplify object initialization
                    var fi = new FileInfo(path);
#pragma warning restore IDE0017 // Simplify object initialization
                    fi.CreationTimeUtc = pubdate.UtcDateTime;

                    Logger.Log(LogLevel.Debug, nameof(Downloader), $"Downloaded file {path}.");
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

            if (newname != file)
            {
                Logger.Log(LogLevel.Debug, nameof(Downloader), $"Changing '{file}' into '{newname}'.");
            }

            return newname;
        }

        private void EnsureFolderExists(string folderName)
                => Directory.CreateDirectory(folderName);

        /*
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
        */
    }
}
