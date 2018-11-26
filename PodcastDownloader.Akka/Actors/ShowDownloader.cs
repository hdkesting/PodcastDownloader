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
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Akka.Actor;
    using PodcastDownloader.Logging;
    using PodcastDownloader.Messages;

    /// <summary>
    /// Download a single show (at a time, per feed).
    /// </summary>
    /// <seealso cref="Akka.Actor.UntypedActor" />
    public class ShowDownloader : UntypedActor
    {
        private const string ProcessQueueMessage = "ProcessQueue";
        private const string LogCategory = nameof(ShowDownloader);
        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

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
            Logger.Log(LogSeverity.Debug, LogCategory, $"Feed: {this.feedname}; message: {message}");

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
                        Context.Parent.Tell(FeedDownloader.QueueIsDoneMessage);
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
                    Context.Parent.Tell(stored);
                    break;

                case Exception ex:
                    Logger.Log(LogSeverity.Failure, LogCategory, "Error downloading", ex);

                    // abort
                    Context.Parent.Tell(FeedDownloader.QueueIsDoneMessage);
                    break;

                default:
                    Logger.Log(LogSeverity.Warning, LogCategory, "Ignoring unknown message in ShowDownloader: " + message);
                    break;
            }
        }

        private void DownloadFile(ShowToDownload show)
        {
            var folder = show.TargetFolder;
            folder = Path.Combine(folder, "__Files");

            this.EnsureFolderExists(folder);

            var file = show.Uri.Segments.Last();

            // Prefix with feed name
            file = show.Feedname + " - " + file;

            file = this.CleanupFilename(file, show.PublishDate);

            var targetpath = Path.Combine(folder, file);
            var fi = new FileInfo(targetpath);

            /*
            // a) doesn't exist yet -- great, write it
            // b) exists with about the same time -- already done, skip
            // c) exists with other time -- write under new name
            */

            if (fi.Exists)
            {
                if (Math.Abs((show.PublishDate - fi.CreationTimeUtc).TotalHours) < 1.0)
                {
                    // TODO move to separate message(-handler)
                    Context.Parent.Tell(new ShowProgressMessage(show.Feedname, file, fi.Length, "File already downloaded: skipping."), this.Self);
                    Context.Parent.Tell(new FileStored(file, show.PublishDate));
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
                request.GetResponseAsync()
                    .ContinueWith(
                        t =>
                            {
                                if (t.IsFaulted)
                                {
                                    return (object)t.Exception;
                                }
                                else if (t.IsCompleted)
                                {
                                    return (object)new ShowResponse(t.Result, path, pubdate);
                                }
                                else
                                {
                                    // cancelled, so apparently I need to exit
                                    return (object)FeedDownloader.QueueIsDoneMessage;
                                }
                            },
                        TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously)
                        .PipeTo(this.Self);
            }
            catch (Exception ex)
            {
                this.Self.Tell(ex);
            }
        }

        private void SaveFile(WebResponse response, string path, DateTimeOffset pubdate)
        {
            // download to temp location. Only move to final location when fully done
            var temppath = Path.GetTempFileName();
            long mbread = 0;
            var filename = Path.GetFileName(path);

            try
            {
                Context.Parent.Tell(new ShowProgressMessage(null, filename, 0, "About to download."), this.Self);

                using (var fileOutput = File.OpenWrite(temppath))
                {
                    // because of the Disposable 'fileOutput', do not use an async message
                    // https://stackoverflow.com/questions/230128/how-do-i-copy-the-contents-of-one-stream-to-another
                    var streamInput = response.GetResponseStream();
                    byte[] buffer = new byte[81_920];
                    int read;
                    long totalread = 0;
                    while ((read = streamInput.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileOutput.Write(buffer, 0, read);
                        totalread += read;
                        var newmbread = totalread / (1024 * 1024);

                        if (mbread != newmbread)
                        {
                            mbread = newmbread;
                            Context.Parent.Tell(new ShowProgressMessage(null, filename, totalread, $"Saving, {mbread} MBytes and counting."), this.Self);
                        }
                    }
                }

                File.Move(temppath, path);
                this.Self.Tell(new FileStored(path, pubdate));
            }
            catch (Exception ex)
            {
                File.Delete(temppath);
                this.Self.Tell(new Exception($"Downloading to {filename}, using temp '{temppath}'. Downloaded {mbread} MB.", ex));
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

        private string CleanupFilename(string file, DateTimeOffset pubdate)
        {
            if (!Regex.IsMatch(file, "[0-9]"))
            {
                // no numbers, so assume not unique
                var ext = Path.GetExtension(file);
                file = Path.GetFileNameWithoutExtension(file);
                file += "-" + pubdate.ToString("yyyyMMdd-HHmm");
                file += ext;
            }

            var newname = new string(file.Where(c => !InvalidFileNameChars.Contains(c)).ToArray());
            if (newname.StartsWith("."))
            {
                newname = newname.TrimStart('.');
            }

            return newname;
        }

        private void EnsureFolderExists(string folderName)
        {
            Directory.CreateDirectory(folderName);
        }
    }
}
