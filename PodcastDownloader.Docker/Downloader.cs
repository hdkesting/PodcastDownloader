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
    using Microsoft.SyndicationFeed;
    using Microsoft.SyndicationFeed.Rss;
    using PodcastDownloader.Logging;

    /// <summary>
    /// Manager for downloading a feed.
    /// </summary>
    internal sealed class Downloader : IDisposable
    {
        private readonly FeedDefinition feed;
        private readonly string baseDownloadPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="Downloader"/> class.
        /// </summary>
        /// <param name="feed">The feed.</param>
        /// <param name="basePath">The base path.</param>
        /// <exception cref="ArgumentNullException">feed cannot be null.</exception>
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
                LoggerSingleton.Value.Log(LogLevel.Warning, nameof(Downloader), "Skipping because URL is empty.");
                return;
            }

            this.feed.LatestError = null;
            if (this.feed.LatestDownload.Year < 2000)
            {
                this.feed.LatestDownload = DateTimeOffset.Now.AddMonths(-1);
            }

            bool retry = true;
            while (retry)
            {
                retry = false;
                try
                {
                    using (XmlReader reader = XmlReader.Create(this.feed.Url, new XmlReaderSettings { Async = true, }))
                    {
                        await this.ProcessFeed(reader);
                    }
                }
                catch (WebException wex) when (new[] { HttpStatusCode.Moved, HttpStatusCode.MovedPermanently }.Contains(((HttpWebResponse)wex.Response).StatusCode))
                {
                    // apparently moved to another location
                    this.feed.Url = ((System.Net.HttpWebResponse)wex.Response).Headers["Location"];

                    retry = true;
                }
                catch (Exception ex)
                {
                    LoggerSingleton.Value.Log(LogLevel.Error, nameof(Downloader), nameof(this.Process), ex);
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            LoggerSingleton.Value.Log(LogLevel.Information, nameof(Downloader), DateTime.Now.ToString(CultureInfo.CurrentCulture) + " " + new string('=', 20));
        }

        private async Task ProcessFeed(XmlReader reader)
        {
            // https://github.com/dotnet/SyndicationFeedReaderWriter
            var feedReader = new RssFeedReader(reader);

            var latest = this.feed.LatestDownload;

            while (await feedReader.Read())
            {
                switch (feedReader.ElementType)
                {
                    // Read category
                    case SyndicationElementType.Category:
                        _ = await feedReader.ReadCategory();
                        break;

                    // Read Image
                    case SyndicationElementType.Image:
                        _ = await feedReader.ReadImage();
                        break;

                    // Read Item
                    case SyndicationElementType.Item:
                        ISyndicationItem item = await feedReader.ReadItem();
                        var lnk = item.Links.FirstOrDefault(l => l.RelationshipType == "enclosure");
                        var pubdate = item.Published.Year > 2000 ? item.Published : item.LastUpdated;
                        if (lnk != null && pubdate > this.feed.LatestDownload)
                        {
                            await this.DownloadFile(lnk.Uri, this.feed.Name, pubdate, item.Title);
                            if (pubdate > latest)
                            {
                                latest = item.Published;
                            }
                        }

                        break;

                    // Read link
                    case SyndicationElementType.Link:
                        _ = await feedReader.ReadLink();
                        break;

                    // Read Person
                    case SyndicationElementType.Person:
                        _ = await feedReader.ReadPerson();
                        break;

                    // Read content
                    default:
                        ISyndicationContent content = await feedReader.ReadContent();
                        if (string.IsNullOrWhiteSpace(this.feed.Name) && content.Name == "title")
                        {
                            // only set if not already set
                            this.feed.Name = content.Value;
                        }

                        break;
                }
            }

            this.feed.LatestDownload = latest;
        }

        private async Task DownloadFile(Uri linkUri, string feedName, DateTimeOffset pubdate, string itemTitle)
        {
            var folder = this.baseDownloadPath;

            this.EnsureFolderExists(folder);

            // get file name from URL and prefix with feed name and pubdate
            var file = this.BuildFilename(linkUri, feedName, pubdate, itemTitle);

            var path = Path.Combine(folder, file);
            var fi = new FileInfo(path);

            /*
            // a) doesn't exist -- great, download & write
            // b) already exists  -- fine, skip
            */

            if (fi.Exists)
            {
                LoggerSingleton.Value.Log(LogLevel.Information, nameof(Downloader), $"File already downloaded: {file}, skipping.");
            }
            else
            {
                LoggerSingleton.Value.Log(LogLevel.Information, nameof(Downloader), $"{this.feed.Name}: {file}");
                await this.DownloadFileToLocal(linkUri, path, pubdate);
            }
        }

        private string BuildFilename(Uri linkUri, string feedName, DateTimeOffset pubdate, string itemTitle)
        {
            const int maxLength = 50;

            if (itemTitle.Length > maxLength)
            {
                itemTitle = itemTitle.Substring(0, maxLength);
            }

            itemTitle = itemTitle.Replace(".", "·").Trim();

            // GetExtension includes the .
            var file = $"{feedName} - {pubdate:yyyy-MM-dd} - {itemTitle}{Path.GetExtension(linkUri.Segments.Last())}";
            file = this.CleanupFilename(file);

            return file;
        }

        private async Task DownloadFileToLocal(Uri sourceUri, string targetPath, DateTimeOffset pubdate)
        {
            try
            {
                var request = WebRequest.Create(sourceUri);
                using (var response = request.GetResponse())
                {
                    using (var wrt = File.OpenWrite(targetPath))
                    {
                        await response.GetResponseStream()?.CopyToAsync(wrt);
                    }

#pragma warning disable IDE0017 // Simplify object initialization
                    var fi = new FileInfo(targetPath);
#pragma warning restore IDE0017 // Simplify object initialization
                    fi.CreationTimeUtc = pubdate.UtcDateTime;
                    fi.LastWriteTimeUtc = pubdate.UtcDateTime;

                    LoggerSingleton.Value.Log(LogLevel.Debug, nameof(Downloader), $"Downloaded file {targetPath}.");
                }
            }
            catch (Exception)
            {
                // remove possibly partially downloaded file
                File.Delete(targetPath);
                throw;
            }
        }

        private string CleanupFilename(string file)
        {
            // GetInvalidFileNameChars works for local filesystem == Linux
            var invalidLocal = Path.GetInvalidFileNameChars();

            // but mounted/viewed on windows, so also check that explicitly
            var invalidWindows = new[] { '\\', '/', ':', '*', '?', '"', '\'', '<', '>', '|' };
            var newname = new string(file.Where(c => !invalidLocal.Contains(c) && !invalidWindows.Contains(c)).ToArray());
            newname = newname.TrimStart('.');

            if (newname != file)
            {
                LoggerSingleton.Value.Log(LogLevel.Debug, nameof(Downloader), $"Changing '{file}' into '{newname}'.");
            }

            return newname;
        }

        private void EnsureFolderExists(string folderName)
                => Directory.CreateDirectory(folderName);
    }
}
