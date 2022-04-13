// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Extension methods for the RateLimiting middleware.
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Enables rate limiting for the application.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        return app.UseMiddleware<RateLimitingMiddleware>();
    }

    /// <summary>
    /// Enables rate limiting for the application with the given options.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app, RateLimitingOptions options)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        return app.UseMiddleware<RateLimitingMiddleware>(Options.Create(options));
    }
}
