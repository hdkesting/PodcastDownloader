﻿// <copyright file="FeedDownloader.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Actors
{
    using System;
    using System.Linq;
    using System.Net;
    using System.ServiceModel.Syndication;
    using System.Web;
    using System.Xml;
    using System.Xml.Linq;
    using Akka.Actor;
    using PodcastDownloader.Messages;

    /// <summary>
    /// Actor responsible for downloading the latest shows of single feed.
    /// </summary>
    /// <seealso cref="Akka.Actor.ReceiveActor" />
    public class FeedDownloader : UntypedActor
    {
        /// <summary>
        /// The message that the queue is done.
        /// </summary>
        public const string QueueIsDoneMessage = "QueueIsDone";

        private const string LoadCommand = "Load";
        private const string ProcessCommand = "Process";

        private FeedConfiguration config;
        private IActorRef downloader;

        private SyndicationFeed podcast;

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

                case FeedConfiguration cfg:
                    this.config = cfg;
                    this.downloader = Context.ActorOf<ShowDownloader>(Support.Cleanup.MakeActorName(cfg.Name + "-showdownloader"));
                    this.Self.Tell(LoadCommand);
                    break;

                case LoadCommand:
                    // read the feed and fire off the "Process"
                    this.podcast = this.LoadFeed();
                    this.Self.Tell(ProcessCommand);
                    break;

                case ProcessCommand:
                    // queue the various shows for download on the single downloader
                    this.ReadAllShows();
                    break;

                case ShowProgressMessage spm:
                    if (spm.FeedName is null)
                    {
                        spm = new ShowProgressMessage(this.config.Name, spm.FileName, spm.BytesRead, spm.Message);
                    }

                    Context.Parent.Tell(spm, this.Self);
                    break;

                case QueueIsDoneMessage:
                    Context.Stop(Context.Sender);
                    Context.Parent.Tell(new ShowProgressMessage(this.config.Name, "--", 0, "Feed is done"));
                    Context.Parent.Tell(PodcastManager.FeedIsDoneMessage);
                    break;

                default:
                    Console.WriteLine("Ignoring unknown message in FeedDownloader: " + message);
                    break;
            }
        }

        private void ReadAllShows()
        {
            DateTimeOffset latest = this.config.LatestDownload;
            foreach (var item in this.podcast.Items.Where(it => it.PublishDate > latest).OrderBy(it => it.PublishDate))
            {
                foreach (var link in item.Links.Where(l => l.RelationshipType == "enclosure"))
                {
                    var msg = new ShowToDownload(link.Uri, item.PublishDate, this.config.TargetFolder, this.config.Name);
                    this.downloader.Tell(msg, this.Self);
                }

                ////if (item.PublishDate > latest)
                ////{
                ////    latest = item.PublishDate;
                ////    this.feed.LatestDownload = latest;
                ////    this.feed.LatestError = String.Empty;
                ////}
            }
        }

        private SyndicationFeed LoadFeed()
        {
            SyndicationFeed podcast = null;

            try
            {
                using (XmlReader reader = XmlReader.Create(this.config.Url))
                {
                    podcast = SyndicationFeed.Load(reader);
                    ////if (podcast == null)
                    ////{
                    ////    // this.logger.WriteLine($"No podcast found at {this.feed.Url}.");
                    ////    return;
                    ////}
                }
            }
            catch (Exception)
            {
                // might happen because of url encoding in url
                //     <atom:link href="http://www.pwop.com%2ffeed.aspx%3fshow%3dHanselminutes" rel="self" type="application/rss+xml" />
                try
                {
                    podcast = this.TryDecodingUrl(this.config.Url);
                }
                catch (Exception ex2)
                {
                    this.Self.Tell(new ErrorMessage(ex2));
                }

                ////if (podcast == null)
                ////{
                ////    this.logger.WriteLine($"Error loading podcast {this.feed.Name}.");
                ////    this.feed.LatestError = ex.Message;
                ////    WriteException(ex);
                ////    return;
                ////}
            }

            return podcast;
        }

        private SyndicationFeed TryDecodingUrl(string feedUrl)
        {
            // <atom:link href="http://www.pwop.com%2ffeed.aspx%3fshow%3dHanselminutes" rel="self" type="application/rss+xml" />
            // need to "decode" that href
            XDocument xml;

            var request = WebRequest.Create(feedUrl);
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
                this.Self.Tell(new ErrorMessage(ex));

                ////this.logger.WriteLine($"Fixing feed {this.feed.Name} didn't work.");
                ////WriteException(ex);
                ////return null;
            }

            return null;
        }
    }
}
