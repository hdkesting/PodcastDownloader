// <copyright file="FeedDownloader.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Akka.Actor;

    /// <summary>
    /// Actor responsible for downloading the latest shows of single feed.
    /// </summary>
    /// <seealso cref="Akka.Actor.ReceiveActor" />
    public class FeedDownloader : ReceiveActor
    {
    }
}
