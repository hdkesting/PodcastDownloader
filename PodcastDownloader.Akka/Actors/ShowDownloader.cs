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
    using System.Threading.Tasks;
    using Akka.Actor;
    using PodcastDownloader.Messages;

    /// <summary>
    /// Download a single show (at a time).
    /// </summary>
    /// <seealso cref="Akka.Actor.UntypedActor" />
    public class ShowDownloader : UntypedActor
    {
        private const string ProcessQueueMessage = "ProcessQueue";

        private readonly Queue<ShowToDownload> downloadQueue = new Queue<ShowToDownload>();
        private bool downloading = false;
        private string feedname = "?unknown?";

        /// <summary>
        /// This defines the behavior of the UntypedActor.
        /// This method is called for every message received by the actor.
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void OnReceive(object message)
        {
            Console.WriteLine($"Feed: {this.feedname}; message: {message}");

            switch (message)
            {
                case null:
                    break;

                // 1) message from parent
                case ShowToDownload todo:
                    this.feedname = todo.Feedname;
                    this.downloadQueue.Enqueue(todo);
                    if (!this.downloading)
                    {
                        this.downloading = true;
                        this.Self.Tell(ProcessQueueMessage);
                    }

                    break;

                // 2) start or continue processing queue: start downloading file
                case ProcessQueueMessage:
                    if (this.downloadQueue.Any())
                    {
                        var todo = this.downloadQueue.Dequeue();
                        Context.Parent.Tell(new ShowProgressMessage(todo.Feedname, null, 0, $"Processing one item from queue, {this.downloadQueue.Count} items left."), this.Self);
                        this.DownloadFile(todo);
                    }
                    else
                    {
                        this.downloading = false;
                        //// TODO message to parent "queue is done"
                    }

                    break;

                // 3) got the download, now store it
                case ShowResponse resp:
                    this.SaveFile(resp.Response, resp.Path, resp.Pubdate);
                    break;

                // 4) finish storing, continue with queue
                case FileStored stored:
                    this.FinishFileStore(stored);
                    this.Self.Tell(ProcessQueueMessage);
                    break;

                case Exception ex:
                    Console.WriteLine("Error downloading");
                    while (ex != null)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                        Console.WriteLine();
                        ex = ex.InnerException;
                    }

                    // go to next one (if any)
                    this.Self.Tell(ProcessQueueMessage);
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
                    Context.Parent.Tell(new ShowProgressMessage(show.Feedname, file, fi.Length, "File already downloaded: skipping."), this.Self);
                    this.Self.Tell(ProcessQueueMessage);
                }
                else
                {
                    var ext = Path.GetExtension(file);
                    file = Path.GetFileNameWithoutExtension(file);
                    file = file + show.PublishDate.ToString("-yyyy-MM-dd") + ext;
                    targetpath = Path.Combine(folder, file);

                    this.DownloadFileToLocal(show.Uri, targetpath, show.PublishDate);
                }
            }
            else
            {
                this.DownloadFileToLocal(show.Uri, targetpath, show.PublishDate);
            }
        }

        private void DownloadFileToLocal(Uri uri, string path, DateTimeOffset pubdate)
        {
            try
            {
                Context.Parent.Tell(new ShowProgressMessage(null, Path.GetFileName(path), 0, "About to read."), this.Self);
                var request = WebRequest.Create(uri);
                var task = request.GetResponseAsync();

                task.ContinueWith(x => x.Exception, TaskContinuationOptions.OnlyOnFaulted)
                    .PipeTo(this.Self);
                task.ContinueWith(res => new ShowResponse(res.Result, path, pubdate), TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously)
                    .PipeTo(this.Self);
            }
            catch (Exception ex)
            {
                this.Self.Tell(ex);
            }
        }

        private void SaveFile(WebResponse response, string path, DateTimeOffset pubdate)
        {
            try
            {
                Context.Parent.Tell(new ShowProgressMessage(null, Path.GetFileName(path), 0, "About to save."), this.Self);
                using (var wrt = File.OpenWrite(path))
                {
                    // because of the Disposable 'wrt', do not use an async message
                    response.GetResponseStream()?.CopyTo(wrt);

                    this.Self.Tell(new FileStored(path, pubdate));
                }
            }
            catch (Exception ex)
            {
                this.Self.Tell(ex);
            }
        }

        private void FinishFileStore(FileStored details)
        {
            try
            {
#pragma warning disable IDE0017 // Simplify object initialization
                var fi = new FileInfo(details.Path);
#pragma warning restore IDE0017 // Simplify object initialization
                fi.CreationTimeUtc = details.Pubdate.UtcDateTime;

                Context.Parent.Tell(new ShowProgressMessage(null, fi.Name, fi.Length, "Stored locally!"), this.Self);
            }
            catch (Exception ex)
            {
                this.Self.Tell(ex);
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
