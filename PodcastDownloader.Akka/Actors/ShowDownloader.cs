// <copyright file="ShowDownloader.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Actors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Akka.Actor;
    using PodcastDownloader.Messages;

    /// <summary>
    /// Download a single show (at a time).
    /// </summary>
    /// <seealso cref="Akka.Actor.UntypedActor" />
    public class ShowDownloader : UntypedActor
    {
        /// <summary>
        /// This defines the behavior of the UntypedActor.
        /// This method is called for every message received by the actor.
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case null:
                    break;

                case ShowToDownload todo:
                    this.DownloadFile(todo);
                    break;
            }
        }

        private void DownloadFile(ShowToDownload show)
        {
            var folder = show.TargetFolder;
            ////if (this.useSeparateFeedFolder)
            ////{
            ////    folder = Path.Combine(folder, this.feed.Name);
            ////}
            ////else
            ////{
            folder = Path.Combine(folder, "__Files");
            ////}

            this.EnsureFolderExists(folder);

            var file = show.Uri.Segments.Last();
            ////if (!this.useSeparateFeedFolder)
            ////{
            // not a separate folder, so prefix with feed name
            file = show.Feedname + " - " + file;
            ////}

            file = this.CleanupFilename(file);

            var targetpath = Path.Combine(folder, file);
            var fi = new FileInfo(targetpath);

            // a) bestaat nog niet -- prima, schrijven
            // b) bestaat al met ~ zelfde tijd -- al gedaan, overslaan
            // c) bestaat met andere tijd -- schrijf onder nieuwe naam

            // TODO remove Console.Writeline - post message to parent
            if (fi.Exists)
            {
                if (Math.Abs((show.PublishDate - fi.CreationTimeUtc).TotalHours) < 1.0)
                {
                    Console.WriteLine($"File already downloaded: {file}, skipping.");
                }
                else
                {
                    var ext = Path.GetExtension(file);
                    file = Path.GetFileNameWithoutExtension(file);
                    file = file + show.PublishDate.ToString("-yyyy-MM-dd") + ext;
                    targetpath = Path.Combine(folder, file);

                    Console.WriteLine($"{show.Feedname}: {file}");
                    this.DownloadFileToLocal(show.Uri, targetpath, show.PublishDate);
                }
            }
            else
            {
                Console.WriteLine($"{show.Feedname}: {file}");
                this.DownloadFileToLocal(show.Uri, targetpath, show.PublishDate);
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

                    //// this.logger.WriteLine($"Downloaded file {path}.");
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

            ////if (newname != file && this.logger != null)
            ////{
            ////    this.logger.WriteLine($"Changing '{file}' into '{newname}'.");
            ////}

            return newname;
        }

        private void EnsureFolderExists(string folderName)
        {
            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }
        }
    }
}
