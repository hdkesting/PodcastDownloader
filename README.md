# PodcastDownloader
An app to download postcasts from RSS feeds.

It reads the config for RSS feeds to follow and downloads any attachments newer than the previously downloaded one. 
It downloads all and exits.

# Variants
* A console app (PodcastDownloader), meant to be used in a scheduled task.
* An Akka.Net version of the console app
* A simple console app (PodcastDownloader.Docker) to be installed in a Docker container. It can run on a NAS and checks every 12 hours for podcasts to download.
