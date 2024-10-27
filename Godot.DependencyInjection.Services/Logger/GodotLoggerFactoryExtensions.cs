﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Godot.DependencyInjection.Services.Logger
{

    /// <summary>
    /// Extension methods for the <see cref="ILoggerFactory"/> class.
    /// </summary>
    public static class GodotLoggerFactoryExtensions
    {
        /// <summary>
        /// Adds a debug logger named 'Debug' to the factory.
        /// </summary>
        /// <param name="builder">The extension method argument.</param>
        public static ILoggingBuilder AddGodot(this ILoggingBuilder builder)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DebugLoggerProvider>());

            return builder;
        }
    }
}
