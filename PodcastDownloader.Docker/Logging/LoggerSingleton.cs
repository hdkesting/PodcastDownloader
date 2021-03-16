namespace PodcastDownloader.Logging
{
    /// <summary>
    /// Singleton instance of logger.
    /// </summary>
    public static class LoggerSingleton
    {
        private static Logger logger;

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public static ILogger Value => logger;

        /// <summary>
        /// Initializes the specified configuration.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public static void Initialize(LoggingConfig config)
        {
            logger = new Logger(config);
        }
    }
}
