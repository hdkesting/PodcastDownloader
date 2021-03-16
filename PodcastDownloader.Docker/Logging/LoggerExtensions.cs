// <copyright file="LoggerExtensions.cs" company="Hans Kesting">
// Copyright (c) Hans Kesting. All rights reserved.
// </copyright>

namespace PodcastDownloader.Logging
{
    using System;
    using System.Linq;

    /// <summary>
    /// Extensions for <see cref="ILogger"/>.
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Log the message at DEBUG level.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="containingType">The class where the message originates.</param>
        /// <param name="message">The text of the message.</param>
        public static void Debug(this ILogger logger, Type containingType, string message)
            => logger.Log(LogLevel.Debug, GetExpandedTypeName(containingType), message);

        /// <summary>
        /// Log the message at INFO level.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="containingType">The class where the message originates.</param>
        /// <param name="message">The text of the message.</param>
        public static void Info(this ILogger logger, Type containingType, string message)
            => logger.Log(LogLevel.Information, GetExpandedTypeName(containingType), message);

        /// <summary>
        /// Log the message at WARN level.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="containingType">The class where the message originates.</param>
        /// <param name="message">The text of the message.</param>
        public static void Warn(this ILogger logger, Type containingType, string message)
            => logger.Log(LogLevel.Warning, GetExpandedTypeName(containingType), message);

        /// <summary>
        /// Log the message at ERROR level.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        /// <param name="containingType">The class where the message originates.</param>
        /// <param name="message">The text of the message.</param>
        /// <param name="ex">The exception that occurred.</param>
        public static void Fatal(this ILogger logger, Type containingType, string message, Exception ex)
            => logger.Log(LogLevel.Error, GetExpandedTypeName(containingType), message, ex);

        private static string GetExpandedTypeName(Type type)
        {
            if (type is null)
            {
                return "null";
            }

            string name = string.Empty;
            if (type.DeclaringType is Type)
            {
                // nested type
                name += GetExpandedTypeName(type.DeclaringType) + "+";
            }

            if (type.IsGenericType)
            {
                name += type.Name.Substring(0, type.Name.IndexOf('`'))
                    + "<"
                    + string.Join(", ", type.GenericTypeArguments.Select(ta => GetExpandedTypeName(ta)))
                    + ">";
            }
            else
            {
                name += type.Name;
            }

            return name;
        }
    }
}
